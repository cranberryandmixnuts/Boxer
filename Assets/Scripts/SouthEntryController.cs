using System.Collections.Generic;
using UnityEngine;

public class SouthEntryController : MonoBehaviour
{
    [SerializeField]
    private Transform[] slots;

    [SerializeField]
    private SorterController sorter;

    [SerializeField]
    private int fillCount = 4;

    [SerializeField]
    private float spawnDelay = 0.2f;

    private float spawnTimer;
    private readonly List<BoxController> boxes = new List<BoxController>();

    private void Start()
    {
        for (int i = 0; i < fillCount; i++)
            TrySpawnToBottom();
    }

    private void Update()
    {
        if (boxes.Count < fillCount)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnDelay)
            {
                spawnTimer = 0f;
                TrySpawnToBottom();
            }
        }
    }

    private void TrySpawnToBottom()
    {
        if (slots == null || slots.Length == 0)
            return;

        BoxController box = BoxPool.Instance.Get();
        BoxPayloadType type = RollPayload();
        Transform bottomSlot = slots[slots.Length - 1];
        box.SetupForEntry(bottomSlot.position, sorter, type);
        boxes.Add(box);
    }

    private BoxPayloadType RollPayload()
    {
        float r = Random.value;
        if (r < 0.1f)
            return BoxPayloadType.Bomb;

        int shapeIndex = Random.Range(0, 7);
        return (BoxPayloadType)shapeIndex;
    }

    public void OnSorterFreed()
    {
        if (boxes.Count == 0)
            return;

        BoxController top = boxes[0];
        top.BeginAdvanceToSorter(slots[0].position);

        for (int i = 1; i < boxes.Count; i++)
        {
            BoxController b = boxes[i];
            Vector3 target = slots[i - 1].position;
            b.BeginAdvanceToSorter(target);
        }

        boxes.RemoveAt(0);
    }
}
