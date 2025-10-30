using UnityEngine;

public enum BoxState
{
    IdleInPool,
    EntryWaiting,
    EntrySliding,
    EntryAdvancing,
    AtSorter,
    MovingOnLane,
    DroppingDown,
    Despawning
}

public class BoxController : MonoBehaviour
{
    [SerializeField]
    private Transform visualRoot;

    [SerializeField]
    private float moveSpeed = 2.5f;

    [SerializeField]
    private float dropDistance = 3f;

    [SerializeField]
    private float dropSpeed = 6f;

    private BoxPayloadType payloadType;
    private BoxState state;
    private ConveyorLane currentLane;
    private SorterController sorter;
    private BoxPool ownerPool;
    private Vector3 entryTargetPosition;
    private float dropTraveled;

    public BoxPayloadType PayloadType
    {
        get { return payloadType; }
        set { payloadType = value; }
    }

    public BoxState State
    {
        get { return state; }
    }

    public void SetPool(BoxPool pool)
    {
        ownerPool = pool;
    }

    public void SetupForEntry(Vector3 position, SorterController sorterRef, BoxPayloadType type)
    {
        payloadType = type;
        sorter = sorterRef;
        state = BoxState.EntryWaiting;
        transform.position = position;
        currentLane = null;
        dropTraveled = 0f;
    }

    public void BeginAdvanceToSorter(Vector3 targetPosition)
    {
        entryTargetPosition = targetPosition;
        state = BoxState.EntryAdvancing;
    }

    public void MoveToEntrySlot(Vector3 targetPosition)
    {
        entryTargetPosition = targetPosition;
        state = BoxState.EntrySliding;
    }

    public void SpawnOnLane(ConveyorLane lane, BoxPayloadType type, SorterController sorterRef)
    {
        payloadType = type;
        sorter = sorterRef;
        currentLane = lane;
        state = BoxState.MovingOnLane;
        transform.position = lane.StartPosition;
        transform.rotation = Quaternion.LookRotation((lane.EndPosition - lane.StartPosition).normalized, Vector3.up);
    }

    private void Update()
    {
        switch (state)
        {
            case BoxState.EntryAdvancing:
                TickEntryAdvancing();
                break;
            case BoxState.EntrySliding:
                TickEntrySliding();
                break;
            case BoxState.MovingOnLane:
                TickMovingOnLane();
                break;
            case BoxState.DroppingDown:
                TickDropping();
                break;
            case BoxState.Despawning:
                DespawnNow();
                break;
        }
    }

    private void TickEntryAdvancing()
    {
        Vector3 dir = entryTargetPosition - transform.position;
        float dist = dir.magnitude;
        if (dist <= 0.001f)
        {
            transform.position = entryTargetPosition;
            state = BoxState.AtSorter;
            if (sorter != null)
                sorter.OnBoxArrived(this);
            return;
        }

        dir.Normalize();
        float step = moveSpeed * Time.deltaTime;
        if (step >= dist)
        {
            transform.position = entryTargetPosition;
            state = BoxState.AtSorter;
            if (sorter != null)
                sorter.OnBoxArrived(this);
        }
        else
            transform.position += dir * step;
    }

    private void TickEntrySliding()
    {
        Vector3 dir = entryTargetPosition - transform.position;
        float dist = dir.magnitude;
        float step = moveSpeed * Time.deltaTime;

        if (dist <= step)
        {
            transform.position = entryTargetPosition;
            state = BoxState.EntryWaiting;
        }
        else
        {
            dir.Normalize();
            transform.position += dir * step;
        }
    }

    private void TickMovingOnLane()
    {
        if (currentLane == null)
        {
            state = BoxState.Despawning;
            return;
        }

        Vector3 target = currentLane.EndPosition;
        Vector3 dir = target - transform.position;
        float dist = dir.magnitude;
        float step = currentLane.MoveSpeed * Time.deltaTime;
        if (dist <= step)
        {
            transform.position = target;
            state = BoxState.Despawning;
        }
        else
        {
            dir.Normalize();
            transform.position += dir * step;
        }
    }

    private void TickDropping()
    {
        float step = dropSpeed * Time.deltaTime;
        dropTraveled += step;
        transform.position += Vector3.down * step;

        if (dropTraveled >= dropDistance)
            state = BoxState.Despawning;
    }

    public void RouteToLane(ConveyorLane lane)
    {
        if (payloadType == BoxPayloadType.Bomb && lane.Direction == Direction8.South)
        {
            BeginDrop();
            return;
        }

        currentLane = lane;
        state = BoxState.MovingOnLane;
        transform.position = lane.StartPosition;
        transform.rotation = Quaternion.LookRotation((lane.EndPosition - lane.StartPosition).normalized, Vector3.up);
    }

    public void BeginDrop()
    {
        dropTraveled = 0f;
        state = BoxState.DroppingDown;
    }

    private void DespawnNow()
    {
        state = BoxState.IdleInPool;
        sorter = null;
        currentLane = null;
        if (ownerPool != null)
            ownerPool.Release(this);
        else
            gameObject.SetActive(false);
    }
}