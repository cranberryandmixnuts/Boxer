using System.Collections.Generic;
using UnityEngine;

public class SouthEntryController : MonoBehaviour
{
    [SerializeField]
    private Transform[] slots;

    [SerializeField]
    private SorterController sorter;

    private readonly List<BoxController> queue = new();

    private void Start()
    {
        if (slots == null || slots.Length == 0)
            return;

        int n = slots.Length;
        for (int i = 0; i < n; i++)
        {
            BoxController box = BoxPool.Instance.Get();
            BoxPayloadType type = RollPayload();
            box.SetupForEntry(slots[i].position, sorter, type);
            queue.Add(box);
        }

        PromoteToSorter();
    }

    public void OnSorterFreed()
    {
        PromoteToSorter();
    }

    private void PromoteToSorter()
    {
        if (queue.Count == 0)
            return;

        BoxController top = queue[0];
        Vector3 sorterPos = sorter.transform.position;
        top.BeginAdvanceToSorter(sorterPos);

        for (int i = 1; i < queue.Count; i++)
        {
            BoxController b = queue[i];
            b.MoveToEntrySlot(slots[i - 1].position);
        }

        queue.RemoveAt(0);
        SpawnBottom();
    }

    private void SpawnBottom()
    {
        BoxController box = BoxPool.Instance.Get();
        BoxPayloadType type = RollPayload();
        Transform bottom = slots[^1];
        box.SetupForEntry(bottom.position, sorter, type);
        queue.Add(box);
    }

    private BoxPayloadType RollPayload()
    {
        float r = Random.value;
        if (r < 0.1f)
            return BoxPayloadType.Bomb;

        int shapeIndex = Random.Range(0, 7);
        return (BoxPayloadType)shapeIndex;
    }
}