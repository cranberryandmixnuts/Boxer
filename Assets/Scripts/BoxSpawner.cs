using UnityEngine;

public class BoxSpawner : MonoBehaviour
{
    [SerializeField]
    private SorterController sorter;

    [SerializeField]
    private ConveyorLane spawnLane;

    [SerializeField]
    private float spawnInterval = 2.5f;

    [SerializeField, Range(0f, 1f)]
    private float bombRate = 0.1f;

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer -= spawnInterval;
            Spawn();
        }
    }

    private void Spawn()
    {
        BoxController box = BoxPool.Instance.Get();
        BoxPayloadType type = RollPayload();
        box.SpawnOnLane(spawnLane, type, sorter);
    }

    private BoxPayloadType RollPayload()
    {
        float r = Random.value;
        if (r < bombRate)
            return BoxPayloadType.Bomb;

        int shapeIndex = Random.Range(0, 7);
        return (BoxPayloadType)shapeIndex;
    }
}
