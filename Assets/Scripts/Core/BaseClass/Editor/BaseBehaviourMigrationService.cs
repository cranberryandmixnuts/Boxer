using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

internal sealed class MigrationReplacement
{
        internal int Index { get; }
        internal int Length { get; }
        internal string ClassName { get; }
        internal string PreviewLine { get; }

        internal MigrationReplacement(int index, int length, string className, string previewLine)
        {
            Index = index;
            Length = length;
            ClassName = className;
            PreviewLine = previewLine;
        }
}

internal sealed class MigrationCandidate
{
        internal string AssetPath { get; }
        internal string SourceHash { get; }
        internal IReadOnlyList<MigrationReplacement> Replacements { get; }
        internal bool Selected { get; set; }

        internal MigrationCandidate(
            string assetPath,
            string sourceHash,
            IReadOnlyList<MigrationReplacement> replacements)
        {
            AssetPath = assetPath;
            SourceHash = sourceHash;
            Replacements = replacements;
            Selected = true;
        }
}

internal static class BaseBehaviourMigrationService
{
        internal const string ReplacementTypeName = "BaseBehaviour";

        private static readonly string[] ExcludedPathFragments =
        {
            "/plugins/",
            "/thirdparty/",
            "/third-party/",
            "/third_party/",
            "/third party/",
            "/external/",
            "/vendor/",
            "/standard assets/",
            "/packages/",
            "/packagecache/",
            "/generated/"
        };

        internal static List<MigrationCandidate> Scan(string assetFolder)
        {
            if (string.IsNullOrEmpty(assetFolder) ||
                !assetFolder.StartsWith("Assets", StringComparison.Ordinal) ||
                !AssetDatabase.IsValidFolder(assetFolder))
            {
                throw new ArgumentException("The scan root must be a folder inside Assets.");
            }

            string[] guids = AssetDatabase.FindAssets("t:MonoScript", new[] { assetFolder });
            var results = new List<MigrationCandidate>();

            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]).Replace('\\', '/');
                if (!assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                    IsExcluded(assetPath))
                {
                    continue;
                }

                TextDocument document = TextDocument.Read(ToAbsolutePath(assetPath));
                List<MigrationReplacement> replacements =
                    DirectMonoBehaviourScanner.FindReplacements(document.Text);
                replacements.RemoveAll(replacement =>
                    string.Equals(
                        replacement.ClassName.TrimStart('@'),
                        ReplacementTypeName,
                        StringComparison.Ordinal));
                if (replacements.Count == 0)
                    continue;

                results.Add(new MigrationCandidate(
                    assetPath,
                    ComputeHash(document.Bytes),
                    replacements));
            }

            results.Sort((left, right) =>
                string.Compare(left.AssetPath, right.AssetPath, StringComparison.Ordinal));
            return results;
        }

        internal static void Apply(IReadOnlyList<MigrationCandidate> candidates)
        {
            if (candidates == null || candidates.Count == 0)
                throw new ArgumentException("No migration candidates were selected.");

            var currentDocuments = new Dictionary<MigrationCandidate, TextDocument>();
            for (int i = 0; i < candidates.Count; i++)
            {
                MigrationCandidate candidate = candidates[i];
                TextDocument current = TextDocument.Read(ToAbsolutePath(candidate.AssetPath));
                if (!string.Equals(
                        candidate.SourceHash,
                        ComputeHash(current.Bytes),
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"'{candidate.AssetPath}' changed after the preview was generated. " +
                        "Rescan before applying the migration.");
                }

                currentDocuments.Add(candidate, current);
            }

            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < candidates.Count; i++)
                {
                    MigrationCandidate candidate = candidates[i];
                    TextDocument document = currentDocuments[candidate];
                    string migrated = ApplyReplacements(document.Text, candidate.Replacements);
                    document.Write(ToAbsolutePath(candidate.AssetPath), migrated);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }
        }

        private static bool IsExcluded(string assetPath)
        {
            if (UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath) != null)
                return true;

            string normalized = "/" + assetPath.Replace('\\', '/').ToLowerInvariant() + "/";
            for (int i = 0; i < ExcludedPathFragments.Length; i++)
            {
                if (normalized.Contains(ExcludedPathFragments[i]))
                    return true;
            }

            return assetPath.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
                   assetPath.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase);
        }

        private static string ApplyReplacements(
            string source,
            IReadOnlyList<MigrationReplacement> replacements)
        {
            var builder = new StringBuilder(source);
            for (int i = replacements.Count - 1; i >= 0; i--)
            {
                MigrationReplacement replacement = replacements[i];
                builder.Remove(replacement.Index, replacement.Length);
                builder.Insert(replacement.Index, ReplacementTypeName);
            }

            return builder.ToString();
        }

        private static string ComputeHash(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
                return Convert.ToBase64String(sha.ComputeHash(bytes));
        }

        private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

        private static string ToAbsolutePath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(
                ProjectRoot,
                assetPath.Replace('/', Path.DirectorySeparatorChar)));
        }

        private sealed class TextDocument
        {
            internal byte[] Bytes { get; }
            internal string Text { get; }

            private readonly Encoding encoding;
            private readonly byte[] preamble;

            private TextDocument(byte[] bytes, string text, Encoding encoding, byte[] preamble)
            {
                Bytes = bytes;
                Text = text;
                this.encoding = encoding;
                this.preamble = preamble;
            }

            internal static TextDocument Read(string path)
            {
                byte[] bytes = File.ReadAllBytes(path);
                Encoding encoding;
                int preambleLength;

                if (StartsWith(bytes, new byte[] { 0xEF, 0xBB, 0xBF }))
                {
                    encoding = new UTF8Encoding(true);
                    preambleLength = 3;
                }
                else if (StartsWith(bytes, new byte[] { 0xFF, 0xFE, 0x00, 0x00 }))
                {
                    encoding = new UTF32Encoding(false, true);
                    preambleLength = 4;
                }
                else if (StartsWith(bytes, new byte[] { 0x00, 0x00, 0xFE, 0xFF }))
                {
                    encoding = new UTF32Encoding(true, true);
                    preambleLength = 4;
                }
                else if (StartsWith(bytes, new byte[] { 0xFF, 0xFE }))
                {
                    encoding = Encoding.Unicode;
                    preambleLength = 2;
                }
                else if (StartsWith(bytes, new byte[] { 0xFE, 0xFF }))
                {
                    encoding = Encoding.BigEndianUnicode;
                    preambleLength = 2;
                }
                else
                {
                    encoding = new UTF8Encoding(false);
                    preambleLength = 0;
                }

                string text = encoding.GetString(bytes, preambleLength, bytes.Length - preambleLength);
                var preamble = new byte[preambleLength];
                if (preambleLength > 0)
                    Buffer.BlockCopy(bytes, 0, preamble, 0, preambleLength);

                return new TextDocument(bytes, text, encoding, preamble);
            }

            internal void Write(string path, string text)
            {
                byte[] content = encoding.GetBytes(text);
                var output = new byte[preamble.Length + content.Length];
                if (preamble.Length > 0)
                    Buffer.BlockCopy(preamble, 0, output, 0, preamble.Length);
                Buffer.BlockCopy(content, 0, output, preamble.Length, content.Length);
                File.WriteAllBytes(path, output);
            }

            private static bool StartsWith(byte[] bytes, byte[] prefix)
            {
                if (bytes.Length < prefix.Length)
                    return false;

                for (int i = 0; i < prefix.Length; i++)
                {
                    if (bytes[i] != prefix[i])
                        return false;
                }

                return true;
            }
        }
}

internal static class DirectMonoBehaviourScanner
{
    private static readonly Regex DirectBaseRegex = new Regex(
        @"\bclass\s+(?<name>@?[A-Za-z_][A-Za-z0-9_]*)(?:\s*<[^>{};]+>)?\s*:\s*" +
        @"(?<base>(?:(?:global::)?UnityEngine\.)?MonoBehaviour)\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    internal static List<MigrationReplacement> FindReplacements(string source)
    {
        string masked = MaskCommentsAndStrings(source);
        MatchCollection matches = DirectBaseRegex.Matches(masked);
        var replacements = new List<MigrationReplacement>(matches.Count);

        for (int i = 0; i < matches.Count; i++)
        {
            Match match = matches[i];
            Group baseGroup = match.Groups["base"];
            string className = match.Groups["name"].Value;
            replacements.Add(new MigrationReplacement(
                baseGroup.Index,
                baseGroup.Length,
                className,
                GetPreviewLine(source, match.Index)));
        }

        return replacements;
    }

    private static string GetPreviewLine(string source, int index)
    {
        int start = source.LastIndexOf('\n', Mathf.Max(0, index - 1));
        start = start < 0 ? 0 : start + 1;
        int end = source.IndexOf('\n', index);
        if (end < 0)
            end = source.Length;

        string line = source.Substring(start, end - start).Trim();
        return line.Length <= 140 ? line : line.Substring(0, 137) + "...";
    }

    private static string MaskCommentsAndStrings(string source)
    {
        char[] result = source.ToCharArray();
        LexState state = LexState.Normal;
        int rawQuoteCount = 0;

        for (int i = 0; i < source.Length; i++)
        {
            char current = source[i];
            char next = i + 1 < source.Length ? source[i + 1] : '\0';

            switch (state)
            {
                case LexState.Normal:
                    if (current == '/' && next == '/')
                    {
                        Mask(result, i);
                        Mask(result, ++i);
                        state = LexState.LineComment;
                    }
                    else if (current == '/' && next == '*')
                    {
                        Mask(result, i);
                        Mask(result, ++i);
                        state = LexState.BlockComment;
                    }
                    else if (current == '\'')
                    {
                        Mask(result, i);
                        state = LexState.Character;
                    }
                    else if (current == '"')
                    {
                        int quoteCount = CountRun(source, i, '"');
                        if (quoteCount >= 3)
                        {
                            rawQuoteCount = quoteCount;
                            for (int j = 0; j < quoteCount; j++)
                                Mask(result, i + j);
                            i += quoteCount - 1;
                            state = LexState.RawString;
                        }
                        else
                        {
                            Mask(result, i);
                            bool verbatim = i > 0 && source[i - 1] == '@' ||
                                            i > 1 && source[i - 2] == '@' && source[i - 1] == '$';
                            state = verbatim ? LexState.VerbatimString : LexState.String;
                        }
                    }
                    break;

                case LexState.LineComment:
                    Mask(result, i);
                    if (current == '\n')
                        state = LexState.Normal;
                    break;

                case LexState.BlockComment:
                    Mask(result, i);
                    if (current == '*' && next == '/')
                    {
                        Mask(result, ++i);
                        state = LexState.Normal;
                    }
                    break;

                case LexState.String:
                    Mask(result, i);
                    if (current == '\\' && i + 1 < source.Length)
                        Mask(result, ++i);
                    else if (current == '"')
                        state = LexState.Normal;
                    break;

                case LexState.VerbatimString:
                    Mask(result, i);
                    if (current == '"' && next == '"')
                        Mask(result, ++i);
                    else if (current == '"')
                        state = LexState.Normal;
                    break;

                case LexState.Character:
                    Mask(result, i);
                    if (current == '\\' && i + 1 < source.Length)
                        Mask(result, ++i);
                    else if (current == '\'')
                        state = LexState.Normal;
                    break;

                case LexState.RawString:
                    Mask(result, i);
                    if (current == '"' && CountRun(source, i, '"') >= rawQuoteCount)
                    {
                        for (int j = 1; j < rawQuoteCount; j++)
                            Mask(result, i + j);
                        i += rawQuoteCount - 1;
                        state = LexState.Normal;
                    }
                    break;
            }
        }

        return new string(result);
    }

    private static int CountRun(string text, int start, char value)
    {
        int count = 0;
        while (start + count < text.Length && text[start + count] == value)
            count++;
        return count;
    }

    private static void Mask(char[] text, int index)
    {
        if (index < 0 || index >= text.Length)
            return;

        if (text[index] != '\r' && text[index] != '\n')
            text[index] = ' ';
    }

    private enum LexState
    {
        Normal,
        LineComment,
        BlockComment,
        String,
        VerbatimString,
        Character,
        RawString
    }
}
