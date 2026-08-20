using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class DragInputController : BaseBehaviour
{
    [SerializeField]
    private SorterController sorter;

    [SerializeField]
    private float minTotalDistance = 40f;

    private AdvancedGestureRecognizer recognizer;
    private bool dragging;
    private readonly List<Vector2> points = new();

    private void Awake()
    {
        recognizer = new AdvancedGestureRecognizer();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        Vector2 pointerPosition = mouse.position.ReadValue();

        if (mouse.leftButton.wasPressedThisFrame)
            BeginDrag(pointerPosition);
        else if (mouse.leftButton.isPressed)
            TickDrag(pointerPosition);
        else if (mouse.leftButton.wasReleasedThisFrame)
            EndDrag(pointerPosition);
    }

    private void BeginDrag(Vector2 pointerPosition)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        dragging = true;
        points.Clear();
        points.Add(pointerPosition);
    }

    private void TickDrag(Vector2 pointerPosition)
    {
        if (!dragging)
            return;

        points.Add(pointerPosition);
    }

    private void EndDrag(Vector2 pointerPosition)
    {
        if (!dragging)
            return;

        dragging = false;
        points.Add(pointerPosition);

        float totalDist = 0f;
        for (int i = 1; i < points.Count; i++)
            totalDist += Vector2.Distance(points[i - 1], points[i]);

        if (totalDist < minTotalDistance)
            return;

        List<GestureMatch> matches = recognizer.RecognizeAll(points);
        Direction8 vecDir = ScreenDeltaToDirection(points[0], points[^1]);

        if (matches.Count == 0)
            return;

        for (int i = 0; i < matches.Count; i++)
        {
            float bonus = CalcVectorBonus(vecDir, matches[i].direction);
            float s = matches[i].score + bonus;
            if (s > 1f)
                s = 1f;
            matches[i] = new GestureMatch(matches[i].name, s, matches[i].direction);
        }

        matches.Sort((a, b) => b.score.CompareTo(a.score));

        Direction8 finalDir = matches[0].direction;

        string log = "Gesture top: ";
        for (int i = 0; i < matches.Count && i < 3; i++)
            log += (i + 1) + ") " + matches[i].name + " " + matches[i].score.ToString("F3") + "  ";
        log += " | vec=" + vecDir + " final=" + finalDir;
        Debug.Log(log);

        sorter.RouteCurrentBox(finalDir);
    }

    private float CalcVectorBonus(Direction8 vecDir, Direction8 algoDir)
    {
        if (vecDir == algoDir)
            return 0.1f;
        if (AreAdjacent(vecDir, algoDir))
            return 0.05f;
        return 0f;
    }

    private bool AreAdjacent(Direction8 a, Direction8 b)
    {
        int ia = DirToIndex(a);
        int ib = DirToIndex(b);
        int diff = Mathf.Abs(ia - ib);
        if (diff == 1)
            return true;
        if (diff == 7)
            return true;
        return false;
    }

    private int DirToIndex(Direction8 d)
    {
        return d switch
        {
            Direction8.East => 0,
            Direction8.NorthEast => 1,
            Direction8.North => 2,
            Direction8.NorthWest => 3,
            Direction8.West => 4,
            Direction8.SouthWest => 5,
            Direction8.South => 6,
            Direction8.SouthEast => 7,
            _ => 8,
        };
    }

    private Direction8 ScreenDeltaToDirection(Vector2 from, Vector2 to)
    {
        Vector2 delta = to - from;
        if (delta.sqrMagnitude < 0.0001f)
            return Direction8.East;

        float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
        if (angle < 0f)
            angle += 360f;

        if (angle >= 247.5f && angle < 292.5f)
            return Direction8.South;
        if (angle >= 202.5f && angle < 247.5f)
            return Direction8.SouthWest;
        if (angle >= 157.5f && angle < 202.5f)
            return Direction8.West;
        if (angle >= 112.5f && angle < 157.5f)
            return Direction8.NorthWest;
        if (angle >= 67.5f && angle < 112.5f)
            return Direction8.North;
        if (angle >= 22.5f && angle < 67.5f)
            return Direction8.NorthEast;
        if (angle >= 337.5f || angle < 22.5f)
            return Direction8.East;
        return Direction8.SouthEast;
    }
}
