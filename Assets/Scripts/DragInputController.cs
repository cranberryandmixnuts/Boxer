using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragInputController : MonoBehaviour
{
    [SerializeField]
    private SorterController sorter;

    [SerializeField]
    private float minTotalDistance = 40f;

    private GestureDirectionRecognizer recognizer;
    private bool dragging;
    private float dragStartTime;
    private List<Vector2> points = new List<Vector2>();

    private void Awake()
    {
        recognizer = new GestureDirectionRecognizer();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            BeginDrag();
        else if (Input.GetMouseButton(0))
            TickDrag();
        else if (Input.GetMouseButtonUp(0))
            EndDrag();
    }

    private void BeginDrag()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        dragging = true;
        dragStartTime = Time.time;
        points.Clear();
        points.Add(Input.mousePosition);
    }

    private void TickDrag()
    {
        if (!dragging)
            return;

        Vector2 p = Input.mousePosition;
        if (points.Count == 0 || Vector2.Distance(points[points.Count - 1], p) > 2f)
            points.Add(p);
    }

    private void EndDrag()
    {
        if (!dragging)
            return;

        dragging = false;
        points.Add(Input.mousePosition);

        float totalDist = 0f;
        for (int i = 1; i < points.Count; i++)
            totalDist += Vector2.Distance(points[i - 1], points[i]);

        if (totalDist < minTotalDistance)
            return;

        float dragTime = Time.time - dragStartTime;

        Direction8 dir = RecognizeDirection(points);
        sorter.RouteCurrentBox(dir, dragTime);
    }

    private Direction8 RecognizeDirection(List<Vector2> pts)
    {
        Direction8 fromTemplate = recognizer.Recognize(pts);
        Vector2 delta = pts[pts.Count - 1] - pts[0];
        if (delta.sqrMagnitude < 0.0001f)
            return fromTemplate;

        Direction8 fromVector = ScreenDeltaToDirection(delta);
        return fromVector;
    }

    private Direction8 ScreenDeltaToDirection(Vector2 delta)
    {
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