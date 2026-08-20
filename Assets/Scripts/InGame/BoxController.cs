using UnityEngine;
using DG.Tweening;

public enum BoxState
{
    InSlot,
    MovingToTarget,
    AtSorter,
    MovingOnLane,
    Dropping
}

public class BoxController : BaseBehaviour, IPoolable
{
    [SerializeField]
    private Transform visualRoot;

    [SerializeField]
    private GameObject arrowObject;

    [SerializeField]
    private GameObject xObject;

    [SerializeField]
    private float slotMoveSpeed = 7f;

    [SerializeField]
    private float dropShrinkDuration = 0.25f;

    [SerializeField]
    private float dropZOffset = 0.1f;

    [SerializeField]
    private float entryScaleFactor = 1.13f;

    private BoxPayloadType payloadType;
    private BoxState state;
    private ConveyorLane currentLane;
    private SorterController sorter;
    private Vector3 targetPosition;
    private bool moveArrivesAtSorter;
    private Vector3 baseScale;
    private bool initialized;
    private Tween entryTween;
    private Tween dropTween;

    private void Awake()
    {
        EnsureInitialized();
    }

    public BoxPayloadType PayloadType
    {
        get { return payloadType; }
        set { payloadType = value; }
    }

    public BoxState State
    {
        get { return state; }
    }

    public void OnSpawn()
    {
        ResetRuntimeState();
    }

    public void OnDespawn()
    {
        ResetRuntimeState();
    }

    public void SetupForEntry(Vector3 position, SorterController sorterRef, BoxPayloadType type)
    {
        KillTweens();
        payloadType = type;
        sorter = sorterRef;
        currentLane = null;
        transform.SetPositionAndRotation(position, Quaternion.identity);
        visualRoot.localRotation = Quaternion.identity;
        ResetScale();
        state = BoxState.InSlot;
        UpdateDirectionIndicator();
    }

    public void BeginAdvanceToSorter(Vector3 sorterPos)
    {
        StartMoveTo(sorterPos, true);
    }

    public void MoveToEntrySlot(Vector3 slotPos)
    {
        StartMoveTo(slotPos, false);
    }

    private void StartMoveTo(Vector3 pos, bool arrivesAtSorter)
    {
        KillTweens();
        targetPosition = pos;
        moveArrivesAtSorter = arrivesAtSorter;
        transform.rotation = Quaternion.identity;
        visualRoot.localRotation = Quaternion.identity;
        PlayEntryHop(CalcSlotDuration(pos));
        state = BoxState.MovingToTarget;
    }

    public void SpawnOnLane(ConveyorLane lane, BoxPayloadType type, SorterController sorterRef)
    {
        KillTweens();
        ResetDirectionIndicator();
        payloadType = type;
        sorter = sorterRef;
        currentLane = lane;
        transform.SetPositionAndRotation(lane.StartPosition, Quaternion.LookRotation((lane.EndPosition - lane.StartPosition).normalized, Vector3.up));
        visualRoot.localRotation = Quaternion.identity;
        state = BoxState.MovingOnLane;
    }

    private void Update()
    {
        switch (state)
        {
            case BoxState.MovingToTarget:
                TickMoveToTarget();
                break;
            case BoxState.MovingOnLane:
                TickMovingOnLane();
                break;
            case BoxState.Dropping:
                break;
        }
    }

    private void TickMoveToTarget()
    {
        float duration = CalcSlotDuration(targetPosition);
        if (duration <= 0f)
        {
            FinishMoveToTarget();
            return;
        }

        Vector3 dir = targetPosition - transform.position;
        float dist = dir.magnitude;
        float step = (dist / duration) * Time.deltaTime;

        if (step >= dist)
        {
            transform.position = targetPosition;
            FinishMoveToTarget();
        }
        else
        {
            dir.Normalize();
            transform.position += dir * step;
        }
    }

    private void FinishMoveToTarget()
    {
        if (moveArrivesAtSorter)
        {
            state = BoxState.AtSorter;
            sorter.OnBoxArrived(this);
        }
        else
            state = BoxState.InSlot;
    }

    private float CalcSlotDuration(Vector3 target)
    {
        float dist = Vector3.Distance(transform.position, target);
        if (slotMoveSpeed <= 0.0001f)
            return 0.12f;
        return dist / slotMoveSpeed;
    }

    private void TickMovingOnLane()
    {
        if (currentLane == null)
        {
            ReturnToPool();
            return;
        }

        Vector3 target = currentLane.EndPosition;
        Vector3 dir = target - transform.position;
        float dist = dir.magnitude;
        float step = currentLane.MoveSpeed * Time.deltaTime;
        if (step >= dist)
        {
            transform.position = target;
            ReturnToPool();
        }
        else
        {
            dir.Normalize();
            transform.position += dir * step;
        }
    }

    private void UpdateDirectionIndicator()
    {
        Direction8 direction = sorter.GetExpectedDirection(payloadType);
        bool isSouth = direction == Direction8.South;

        arrowObject.SetActive(!isSouth);

        if (!isSouth)
        {
            float yAngle = GetYRotationForDirection(direction);
            Transform t = arrowObject.transform;
            Vector3 euler = t.localEulerAngles;
            euler.y = yAngle;
            t.localEulerAngles = euler;
        }

        xObject.SetActive(isSouth);
    }

    private float GetYRotationForDirection(Direction8 direction)
    {
        return direction switch
        {
            Direction8.SouthEast => 45f,
            Direction8.East => 90f,
            Direction8.NorthEast => 135f,
            Direction8.North => 180f,
            Direction8.NorthWest => 225f,
            Direction8.West => 270f,
            Direction8.SouthWest => 315f,
            _ => 0f,
        };
    }

    public void RouteToLane(ConveyorLane lane)
    {
        KillTweens();
        ResetDirectionIndicator();

        bool dropHere = false;
        if (lane.Direction == Direction8.South)
            dropHere = true;

        if (dropHere)
        {
            BeginDrop();
            return;
        }

        currentLane = lane;
        transform.SetPositionAndRotation(lane.StartPosition, Quaternion.LookRotation((lane.EndPosition - lane.StartPosition).normalized, Vector3.up));
        visualRoot.localRotation = Quaternion.identity;
        state = BoxState.MovingOnLane;
    }

    public void BeginDrop()
    {
        KillTweens();
        ResetDirectionIndicator();

        Vector3 p = transform.position;
        p.z += dropZOffset;
        transform.position = p;
        Transform t = visualRoot;
        t.localScale = baseScale;
        dropTween = t.DOScale(Vector3.zero, dropShrinkDuration).SetEase(Ease.InQuad).OnComplete(() =>
        {
            ReturnToPool();
        });
        state = BoxState.Dropping;
    }

    private void ReturnToPool()
    {
        Pool.Destroy(gameObject);
    }

    private void ResetDirectionIndicator()
    {
        arrowObject.SetActive(false);
        Transform t = arrowObject.transform;
        Vector3 euler = t.localEulerAngles;
        euler.y = 0f;
        t.localEulerAngles = euler;

        xObject.SetActive(false);
    }

    private void PlayEntryHop(float duration)
    {
        Transform t = visualRoot;
        t.localScale = baseScale;
        if (duration <= 0f)
            duration = 0.1f;
        entryTween = t.DOScale(baseScale * entryScaleFactor, duration * 0.5f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.OutQuad);
    }

    private void KillTweens()
    {
        if (entryTween != null && entryTween.IsActive())
            entryTween.Kill();
        if (dropTween != null && dropTween.IsActive())
            dropTween.Kill();
        entryTween = null;
        dropTween = null;
    }

    private void ResetScale()
    {
        visualRoot.localScale = baseScale;
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        baseScale = visualRoot.localScale;
        initialized = true;
    }

    private void ResetRuntimeState()
    {
        EnsureInitialized();
        KillTweens();
        payloadType = default;
        state = BoxState.InSlot;
        currentLane = null;
        sorter = null;
        targetPosition = default;
        moveArrivesAtSorter = false;
        visualRoot.localRotation = Quaternion.identity;
        ResetScale();
        ResetDirectionIndicator();
    }
}
