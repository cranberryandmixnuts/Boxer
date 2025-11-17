using System.Collections.Generic;
using UnityEngine;

public class GestureDirectionRecognizer
{
    private const int SampleCount = 64;
    private const float SquareSize = 250f;

    private readonly List<GestureTemplate> templates = new List<GestureTemplate>();

    public GestureDirectionRecognizer()
    {
        templates.Add(BuildLineTemplate(Direction8.East, new Vector2(0f, 0f), new Vector2(1f, 0f)));
        templates.Add(BuildLineTemplate(Direction8.NorthEast, new Vector2(0f, 0f), new Vector2(1f, 1f)));
        templates.Add(BuildLineTemplate(Direction8.North, new Vector2(0f, 0f), new Vector2(0f, 1f)));
        templates.Add(BuildLineTemplate(Direction8.NorthWest, new Vector2(0f, 0f), new Vector2(-1f, 1f)));
        templates.Add(BuildLineTemplate(Direction8.West, new Vector2(0f, 0f), new Vector2(-1f, 0f)));
        templates.Add(BuildLineTemplate(Direction8.SouthWest, new Vector2(0f, 0f), new Vector2(-1f, -1f)));
        templates.Add(BuildLineTemplate(Direction8.South, new Vector2(0f, 0f), new Vector2(0f, -1f)));
        templates.Add(BuildLineTemplate(Direction8.SouthEast, new Vector2(0f, 0f), new Vector2(1f, -1f)));
    }

    public Direction8 Recognize(List<Vector2> points)
    {
        if (points == null || points.Count == 0)
            return Direction8.East;

        List<Vector2> sampled = Resample(points, SampleCount);
        sampled = RotateToZero(sampled);
        sampled = ScaleToSquare(sampled, SquareSize);
        sampled = TranslateToOrigin(sampled);

        float best = float.MaxValue;
        Direction8 bestDir = Direction8.East;

        for (int i = 0; i < templates.Count; i++)
        {
            float d = PathDistance(sampled, templates[i].Points);
            if (d < best)
            {
                best = d;
                bestDir = templates[i].Direction;
            }
        }

        return bestDir;
    }

    private GestureTemplate BuildLineTemplate(Direction8 dir, Vector2 from, Vector2 to)
    {
        List<Vector2> pts = new List<Vector2>();
        for (int i = 0; i < SampleCount; i++)
        {
            float t = (float)i / (SampleCount - 1);
            Vector2 p = Vector2.Lerp(from, to, t);
            pts.Add(p);
        }

        pts = RotateToZero(pts);
        pts = ScaleToSquare(pts, SquareSize);
        pts = TranslateToOrigin(pts);

        return new GestureTemplate(dir, pts);
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

        if (newPts.Count < n)
            newPts.Add(pts[pts.Count - 1]);

        return newPts;
    }

    private List<Vector2> RotateToZero(List<Vector2> pts)
    {
        Vector2 c = Centroid(pts);
        float theta = Mathf.Atan2(c.y - pts[0].y, c.x - pts[0].x);
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
        List<Vector2> newPts = new List<Vector2>(pts.Count);
        for (int i = 0; i < pts.Count; i++)
        {
            float x = pts[i].x * (size / box.width);
            float y = pts[i].y * (size / box.height);
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

    private class GestureTemplate
    {
        public Direction8 Direction { get; private set; }
        public List<Vector2> Points { get; private set; }

        public GestureTemplate(Direction8 direction, List<Vector2> points)
        {
            Direction = direction;
            Points = points;
        }
    }
}
