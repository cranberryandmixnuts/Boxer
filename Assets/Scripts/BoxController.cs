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
    private Vector3 targetPosition;
    private bool moveArrivesAtSorter;
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
        currentLane = null;
        transform.SetPositionAndRotation(position, Quaternion.identity);
        if (visualRoot != null)
            visualRoot.localRotation = Quaternion.identity;
        ResetScale();
        state = BoxState.InSlot;
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
        if (visualRoot != null)
            visualRoot.localRotation = Quaternion.identity;
        PlayEntryHop(CalcSlotDuration(pos));
        state = BoxState.MovingToTarget;
    }

    public void SpawnOnLane(ConveyorLane lane, BoxPayloadType type, SorterController sorterRef)
    {
        KillTweens();
        payloadType = type;
        sorter = sorterRef;
        currentLane = lane;
        transform.SetPositionAndRotation(lane.StartPosition, Quaternion.LookRotation((lane.EndPosition - lane.StartPosition).normalized, Vector3.up));
        if (visualRoot != null)
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
            if (sorter != null)
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
        transform.SetPositionAndRotation(lane.StartPosition, Quaternion.LookRotation((lane.EndPosition - lane.StartPosition).normalized, Vector3.up));
        if (visualRoot != null)
            visualRoot.localRotation = Quaternion.identity;
        state = BoxState.MovingOnLane;
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
            ReturnToPool();
        });
        state = BoxState.Dropping;
    }

    private void ReturnToPool()
    {
        KillTweens();
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