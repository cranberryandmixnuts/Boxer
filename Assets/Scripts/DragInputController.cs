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
        Direction8 dir = recognizer.Recognize(points);
        sorter.RouteCurrentBox(dir, dragTime);
    }
}
