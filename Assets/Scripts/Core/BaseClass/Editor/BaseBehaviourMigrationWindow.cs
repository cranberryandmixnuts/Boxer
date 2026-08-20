using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal sealed class BaseBehaviourMigrationWindow : EditorWindow
{
    private string scanRoot;
    private List<MigrationCandidate> candidates = new List<MigrationCandidate>();
    private Vector2 scrollPosition;

    [MenuItem("Tools/Object Pooling/Migrate User Scripts to BaseBehaviour...", priority = 20)]
    private static void Open()
    {
        var window = GetWindow<BaseBehaviourMigrationWindow>();
        window.titleContent = new GUIContent("BaseBehaviour Migration");
        window.minSize = new Vector2(680f, 420f);
        window.Show();
    }

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(scanRoot))
            scanRoot = "Assets";
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("User Script Migration", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This explicit tool scans the selected Assets folder and only previews direct " +
            "': MonoBehaviour' inheritance. Packages, common third-party/vendor folders, " +
            "and generated code are excluded. Review the list before applying.",
            MessageType.Info);

        DrawFolderSelector();
        DrawToolbar();
        EditorGUILayout.Space();
        DrawCandidates();
        DrawApplyButton();
    }

    private void DrawFolderSelector()
    {
        DefaultAsset currentFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(scanRoot);
        DefaultAsset chosenFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            "Scan Root",
            currentFolder,
            typeof(DefaultAsset),
            false);

        if (chosenFolder == null || chosenFolder == currentFolder)
            return;

        string chosenPath = AssetDatabase.GetAssetPath(chosenFolder);
        if (chosenPath.StartsWith("Assets", StringComparison.Ordinal) &&
            AssetDatabase.IsValidFolder(chosenPath))
        {
            scanRoot = chosenPath;
            candidates.Clear();
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Scan / Refresh", GUILayout.Width(120f)))
                Scan();

            using (new EditorGUI.DisabledScope(candidates.Count == 0))
            {
                if (GUILayout.Button("Select All", GUILayout.Width(90f)))
                {
                    for (int i = 0; i < candidates.Count; i++)
                        candidates[i].Selected = true;
                }

                if (GUILayout.Button("Select None", GUILayout.Width(90f)))
                {
                    for (int i = 0; i < candidates.Count; i++)
                        candidates[i].Selected = false;
                }
            }

            GUILayout.FlexibleSpace();
            int selectedCount = candidates.Count(candidate => candidate.Selected);
            EditorGUILayout.LabelField(
                $"{candidates.Count} file(s), {selectedCount} selected",
                GUILayout.Width(180f));
        }
    }

    private void DrawCandidates()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        for (int i = 0; i < candidates.Count; i++)
        {
            MigrationCandidate candidate = candidates[i];
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    candidate.Selected = EditorGUILayout.Toggle(
                        candidate.Selected,
                        GUILayout.Width(18f));
                    EditorGUILayout.LabelField(candidate.AssetPath, EditorStyles.boldLabel);
                }

                EditorGUI.indentLevel++;
                for (int j = 0; j < candidate.Replacements.Count; j++)
                {
                    MigrationReplacement replacement = candidate.Replacements[j];
                    EditorGUILayout.LabelField(
                        $"{replacement.ClassName}: {replacement.PreviewLine}",
                        EditorStyles.miniLabel);
                }
                EditorGUI.indentLevel--;
            }
        }

        if (candidates.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Run Scan / Refresh to build a preview. No files are changed by scanning.",
                MessageType.None);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawApplyButton()
    {
        List<MigrationCandidate> selected = candidates
            .Where(candidate => candidate.Selected)
            .ToList();

        using (new EditorGUI.DisabledScope(selected.Count == 0))
        {
            if (!GUILayout.Button(
                    $"Convert {selected.Count} Selected File(s)",
                    GUILayout.Height(32f)))
            {
                return;
            }
        }

        if (selected.Count == 0 ||
            !EditorUtility.DisplayDialog(
                "Convert to BaseBehaviour",
                $"Convert direct MonoBehaviour inheritance in {selected.Count} file(s)?\n\n" +
                "Unity will recompile scripts after the edit.",
                "Convert",
                "Cancel"))
        {
            return;
        }

        try
        {
            BaseBehaviourMigrationService.Apply(selected);
            Debug.Log($"Converted {selected.Count} script file(s) to BaseBehaviour.");
            candidates.Clear();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Migration Failed", exception.Message, "OK");
        }
    }

    private void Scan()
    {
        try
        {
            candidates = BaseBehaviourMigrationService.Scan(scanRoot);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog("Scan Failed", exception.Message, "OK");
        }
    }
}
