using UnityEngine;

public class ConveyorLane : MonoBehaviour
{
    [SerializeField]
    private Direction8 direction;

    [SerializeField]
    private Transform startPoint;

    [SerializeField]
    private Transform endPoint;

    [SerializeField]
    private bool isToSorter;

    [SerializeField]
    private float moveSpeed = 5f;

    public Direction8 Direction
    {
        get { return direction; }
    }

    public bool IsToSorter
    {
        get { return isToSorter; }
    }

    public float MoveSpeed
    {
        get { return moveSpeed; }
    }

    public Vector3 StartPosition
    {
        get { return startPoint != null ? startPoint.position : transform.position; }
    }

    public Vector3 EndPosition
    {
        get { return endPoint != null ? endPoint.position : transform.position; }
    }
}
