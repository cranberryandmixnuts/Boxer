using System.Collections.Generic;
using UnityEngine;

public struct GestureMatch
{
    public string name;
    public float score;
    public Direction8 direction;

    public GestureMatch(string name, float score, Direction8 direction)
    {
        this.name = name;
        this.score = score;
        this.direction = direction;
    }
}

public class AdvancedGestureRecognizer
{
    private const int SampleCount = 64;
    private const float SquareSize = 250f;
    private const float Epsilon = 0.0001f;
    private const float AngleWindow = 35f;

    private readonly List<GestureTemplateData> templates = new List<GestureTemplateData>();

    public AdvancedGestureRecognizer()
    {
        templates.Add(BuildLineTemplate("East", Direction8.East, new Vector2(0f, 0f), new Vector2(1f, 0f)));
        templates.Add(BuildLineTemplate("NorthEast", Direction8.NorthEast, new Vector2(0f, 0f), new Vector2(1f, 1f)));
        templates.Add(BuildLineTemplate("North", Direction8.North, new Vector2(0f, 0f), new Vector2(0f, 1f)));
        templates.Add(BuildLineTemplate("NorthWest", Direction8.NorthWest, new Vector2(0f, 0f), new Vector2(-1f, 1f)));
        templates.Add(BuildLineTemplate("West", Direction8.West, new Vector2(0f, 0f), new Vector2(-1f, 0f)));
        templates.Add(BuildLineTemplate("SouthWest", Direction8.SouthWest, new Vector2(0f, 0f), new Vector2(-1f, -1f)));
        templates.Add(BuildLineTemplate("South", Direction8.South, new Vector2(0f, 0f), new Vector2(0f, -1f)));
        templates.Add(BuildLineTemplate("SouthEast", Direction8.SouthEast, new Vector2(0f, 0f), new Vector2(1f, -1f)));
    }

    public List<GestureMatch> RecognizeAll(List<Vector2> rawPoints)
    {
        List<GestureMatch> result = new List<GestureMatch>();
        if (rawPoints == null || rawPoints.Count == 0)
            return result;

        float rawPath = PathLength(rawPoints);
        float chord = Vector2.Distance(rawPoints[0], rawPoints[rawPoints.Count - 1]);
        float straightness = rawPath > Epsilon ? Mathf.Clamp01(chord / rawPath) : 1f;
        float strokeAngle = CalcStrokeAngle(rawPoints[0], rawPoints[rawPoints.Count - 1]);

        List<Vector2> pts = Normalize(rawPoints);

        for (int i = 0; i < templates.Count; i++)
        {
            float mainAngle = TemplateMainAngle(templates[i].direction);
            float angDiff = AngleDiffDeg(strokeAngle, mainAngle);
            if (angDiff > AngleWindow)
                continue;

            float d = PathDistance(pts, templates[i].points);
            if (float.IsNaN(d) || float.IsInfinity(d))
                d = 9999f;

            float distScore = Mathf.Exp(-d / 35f);
            float straightScore = Mathf.Pow(straightness, 2.2f);
            float angleScore = Mathf.Exp(-(angDiff * angDiff) / (2f * 12.5f));

            float final = distScore * (0.6f + 0.4f * straightScore) * (0.6f + 0.4f * angleScore);
            result.Add(new GestureMatch(templates[i].name, final, templates[i].direction));
        }

        if (result.Count == 0)
        {
            for (int i = 0; i < templates.Count; i++)
            {
                float d = PathDistance(pts, templates[i].points);
                if (float.IsNaN(d) || float.IsInfinity(d))
                    d = 9999f;
                float distScore = Mathf.Exp(-d / 35f);
                float final = distScore * 0.6f;
                result.Add(new GestureMatch(templates[i].name, final, templates[i].direction));
            }
        }

        result.Sort((a, b) => b.score.CompareTo(a.score));
        return result;
    }

    public GestureMatch RecognizeBest(List<Vector2> rawPoints)
    {
        List<GestureMatch> all = RecognizeAll(rawPoints);
        if (all.Count == 0)
            return new GestureMatch("None", 0f, Direction8.East);
        return all[0];
    }

    private List<Vector2> Normalize(List<Vector2> pts)
    {
        List<Vector2> resampled = Resample(new List<Vector2>(pts), SampleCount);
        resampled = RotateToIndicativeAngle(resampled);
        resampled = ScaleToSquare(resampled, SquareSize);
        resampled = TranslateToOrigin(resampled);
        return resampled;
    }

    private GestureTemplateData BuildLineTemplate(string name, Direction8 dir, Vector2 from, Vector2 to)
    {
        List<Vector2> pts = new List<Vector2>();
        for (int i = 0; i < SampleCount; i++)
        {
            float t = (float)i / (SampleCount - 1);
            Vector2 p = Vector2.Lerp(from, to, t);
            pts.Add(p);
        }
        pts = RotateToIndicativeAngle(pts);
        pts = ScaleToSquare(pts, SquareSize);
        pts = TranslateToOrigin(pts);
        return new GestureTemplateData(name, dir, pts);
    }

    private List<Vector2> Resample(List<Vector2> pts, int n)
    {
        float interval = PathLength(pts) / (n - 1);
        float d = 0f;
        List<Vector2> newPts = new List<Vector2>();
        newPts.Add(pts[0]);

        for (int i = 1; i < pts.Count; i++)
        {
            float dist = Vector2.Distance(pts[i - 1], pts[i]);
            if (d + dist >= interval)
            {
                float t = (interval - d) / dist;
                Vector2 np = Vector2.Lerp(pts[i - 1], pts[i], t);
                newPts.Add(np);
                pts.Insert(i, np);
                d = 0f;
            }
            else
                d += dist;
        }

        while (newPts.Count < n)
            newPts.Add(pts[pts.Count - 1]);

        return newPts;
    }

    private List<Vector2> RotateToIndicativeAngle(List<Vector2> pts)
    {
        Vector2 c = Centroid(pts);
        float theta = Mathf.Atan2(pts[0].y - c.y, pts[0].x - c.x);
        return RotateBy(pts, -theta);
    }

    private List<Vector2> RotateBy(List<Vector2> pts, float rad)
    {
        List<Vector2> outPts = new List<Vector2>(pts.Count);
        Vector2 c = Centroid(pts);
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        for (int i = 0; i < pts.Count; i++)
        {
            float dx = pts[i].x - c.x;
            float dy = pts[i].y - c.y;
            float x = dx * cos - dy * sin + c.x;
            float y = dx * sin + dy * cos + c.y;
            outPts.Add(new Vector2(x, y));
        }

        return outPts;
    }

    private List<Vector2> ScaleToSquare(List<Vector2> pts, float size)
    {
        Rect box = BoundingBox(pts);
        float w = box.width;
        float h = box.height;
        if (w < Epsilon) w = 1f;
        if (h < Epsilon) h = 1f;

        List<Vector2> newPts = new List<Vector2>(pts.Count);
        for (int i = 0; i < pts.Count; i++)
        {
            float x = (pts[i].x - box.x) * (size / w);
            float y = (pts[i].y - box.y) * (size / h);
            newPts.Add(new Vector2(x, y));
        }
        return newPts;
    }

    private List<Vector2> TranslateToOrigin(List<Vector2> pts)
    {
        Vector2 c = Centroid(pts);
        List<Vector2> newPts = new List<Vector2>(pts.Count);
        for (int i = 0; i < pts.Count; i++)
            newPts.Add(pts[i] - c);
        return newPts;
    }

    private float PathDistance(List<Vector2> a, List<Vector2> b)
    {
        float d = 0f;
        for (int i = 0; i < a.Count; i++)
            d += Vector2.Distance(a[i], b[i]);
        return d / a.Count;
    }

    private float PathLength(List<Vector2> pts)
    {
        float d = 0f;
        for (int i = 1; i < pts.Count; i++)
            d += Vector2.Distance(pts[i - 1], pts[i]);
        return d;
    }

    private Vector2 Centroid(List<Vector2> pts)
    {
        float x = 0f;
        float y = 0f;
        for (int i = 0; i < pts.Count; i++)
        {
            x += pts[i].x;
            y += pts[i].y;
        }
        x /= pts.Count;
        y /= pts.Count;
        return new Vector2(x, y);
    }

    private Rect BoundingBox(List<Vector2> pts)
    {
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        for (int i = 0; i < pts.Count; i++)
        {
            if (pts[i].x < minX) minX = pts[i].x;
            if (pts[i].x > maxX) maxX = pts[i].x;
            if (pts[i].y < minY) minY = pts[i].y;
            if (pts[i].y > maxY) maxY = pts[i].y;
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private float CalcStrokeAngle(Vector2 from, Vector2 to)
    {
        Vector2 d = to - from;
        if (d.sqrMagnitude < Epsilon)
            return 0f;
        float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        if (a < 0f) a += 360f;
        return a;
    }

    private float AngleDiffDeg(float a, float b)
    {
        float d = Mathf.Abs(a - b);
        if (d > 180f) d = 360f - d;
        return d;
    }

    private float TemplateMainAngle(Direction8 dir)
    {
        switch (dir)
        {
            case Direction8.East: return 0f;
            case Direction8.NorthEast: return 45f;
            case Direction8.North: return 90f;
            case Direction8.NorthWest: return 135f;
            case Direction8.West: return 180f;
            case Direction8.SouthWest: return 225f;
            case Direction8.South: return 270f;
            case Direction8.SouthEast: return 315f;
        }
        return 0f;
    }

    private class GestureTemplateData
    {
        public string name;
        public Direction8 direction;
        public List<Vector2> points;

        public GestureTemplateData(string name, Direction8 direction, List<Vector2> points)
        {
            this.name = name;
            this.direction = direction;
            this.points = points;
        }
    }
}