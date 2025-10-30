using UnityEngine;
using DG.Tweening;

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
    private BoxPool ownerPool;
    private Vector3 entryTargetPosition;
    private Vector3 baseScale;
    private Tween entryTween;
    private Tween dropTween;

    private void Awake()
    {
        if (visualRoot != null)
            baseScale = visualRoot.localScale;
        else
            baseScale = transform.localScale;
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

    public void SetPool(BoxPool pool)
    {
        ownerPool = pool;
    }

    public void SetupForEntry(Vector3 position, SorterController sorterRef, BoxPayloadType type)
    {
        KillTweens();
        payloadType = type;
        sorter = sorterRef;
        state = BoxState.EntryWaiting;
        transform.SetPositionAndRotation(position, Quaternion.identity);
        if (visualRoot != null)
            visualRoot.localRotation = Quaternion.identity;
        ResetScale();
        currentLane = null;
    }

    public void BeginAdvanceToSorter(Vector3 targetPosition)
    {
        KillTweens();
        entryTargetPosition = targetPosition;
        state = BoxState.EntryAdvancing;
        transform.rotation = Quaternion.identity;
        if (visualRoot != null)
            visualRoot.localRotation = Quaternion.identity;
        PlayEntryHop(CalcSlotDuration(entryTargetPosition));
    }

    public void MoveToEntrySlot(Vector3 targetPosition)
    {
        KillTweens();
        entryTargetPosition = targetPosition;
        state = BoxState.EntrySliding;
        transform.rotation = Quaternion.identity;
        if (visualRoot != null)
            visualRoot.localRotation = Quaternion.identity;
        PlayEntryHop(CalcSlotDuration(entryTargetPosition));
    }

    public void SpawnOnLane(ConveyorLane lane, BoxPayloadType type, SorterController sorterRef)
    {
        KillTweens();
        payloadType = type;
        sorter = sorterRef;
        currentLane = lane;
        state = BoxState.MovingOnLane;
        transform.SetPositionAndRotation(lane.StartPosition, Quaternion.LookRotation((lane.EndPosition - lane.StartPosition).normalized, Vector3.up));
        if (visualRoot != null)
            visualRoot.localRotation = Quaternion.identity;
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
                break;
            case BoxState.Despawning:
                DespawnNow();
                break;
        }
    }

    private void TickEntryAdvancing()
    {
        float duration = CalcSlotDuration(entryTargetPosition);
        StepSlotMove(duration, true);
    }

    private void TickEntrySliding()
    {
        float duration = CalcSlotDuration(entryTargetPosition);
        StepSlotMove(duration, false);
    }

    private void StepSlotMove(float duration, bool arriveSorter)
    {
        if (duration <= 0f)
        {
            transform.position = entryTargetPosition;
            if (arriveSorter)
            {
                state = BoxState.AtSorter;
                if (sorter != null)
                    sorter.OnBoxArrived(this);
            }
            else
                state = BoxState.EntryWaiting;
            return;
        }

        Vector3 dir = entryTargetPosition - transform.position;
        float dist = dir.magnitude;
        float step = (dist / duration) * Time.deltaTime;

        if (step >= dist)
        {
            transform.position = entryTargetPosition;
            if (arriveSorter)
            {
                state = BoxState.AtSorter;
                if (sorter != null)
                    sorter.OnBoxArrived(this);
            }
            else
                state = BoxState.EntryWaiting;
        }
        else
        {
            dir.Normalize();
            transform.position += dir * step;
        }
    }

    private float CalcSlotDuration(Vector3 target)
    {
        float dist = Vector3.Distance(transform.position, target);
        if (slotMoveSpeed <= 0.0001f)
            return 0.15f;
        return dist / slotMoveSpeed;
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

    public void RouteToLane(ConveyorLane lane)
    {
        KillTweens();

        bool dropHere = false;

        if (lane == null)
            dropHere = true;
        else if (lane.Direction == Direction8.South)
            dropHere = true;

        if (dropHere)
        {
            BeginDrop();
            return;
        }

        currentLane = lane;
        state = BoxState.MovingOnLane;
        transform.SetPositionAndRotation(lane.StartPosition, Quaternion.LookRotation((lane.EndPosition - lane.StartPosition).normalized, Vector3.up));
        if (visualRoot != null)
            visualRoot.localRotation = Quaternion.identity;
    }

    public void BeginDrop()
    {
        KillTweens();
        Vector3 p = transform.position;
        p.z += dropZOffset;
        transform.position = p;
        Transform t = visualRoot != null ? visualRoot : transform;
        t.localScale = baseScale;
        dropTween = t.DOScale(Vector3.zero, dropShrinkDuration).SetEase(Ease.InQuad).OnComplete(() =>
        {
            state = BoxState.Despawning;
        });
        state = BoxState.DroppingDown;
    }

    private void DespawnNow()
    {
        KillTweens();
        state = BoxState.IdleInPool;
        sorter = null;
        currentLane = null;
        transform.rotation = Quaternion.identity;
        ResetScale();
        if (visualRoot != null)
            visualRoot.localRotation = Quaternion.identity;
        if (ownerPool != null)
            ownerPool.Release(this);
        else
            gameObject.SetActive(false);
    }

    private void PlayEntryHop(float duration)
    {
        Transform t = visualRoot != null ? visualRoot : transform;
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
        if (visualRoot != null)
            visualRoot.localScale = baseScale;
        else
            transform.localScale = baseScale;
    }
}