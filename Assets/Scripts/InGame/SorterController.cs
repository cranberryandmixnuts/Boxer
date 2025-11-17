using System.Collections.Generic;
using UnityEngine;

public class SorterController : MonoBehaviour
{
    [SerializeField]
    private ConveyorLane southLane;

    [SerializeField]
    private ConveyorLane southWestLane;

    [SerializeField]
    private ConveyorLane westLane;

    [SerializeField]
    private ConveyorLane northWestLane;

    [SerializeField]
    private ConveyorLane northLane;

    [SerializeField]
    private ConveyorLane northEastLane;

    [SerializeField]
    private ConveyorLane eastLane;

    [SerializeField]
    private ConveyorLane southEastLane;

    [SerializeField]
    private SouthEntryController southEntry;

    private BoxController currentBox;
    private GameController gameController;
    private Dictionary<BoxPayloadType, Direction8> routeTable;

    private void Awake()
    {
        gameController = FindFirstObjectByType<GameController>();
        BuildRouteTable();
    }

    private void BuildRouteTable()
    {
        routeTable = new Dictionary<BoxPayloadType, Direction8>
        {
            [BoxPayloadType.Shape1] = Direction8.SouthWest,
            [BoxPayloadType.Shape2] = Direction8.West,
            [BoxPayloadType.Shape3] = Direction8.NorthWest,
            [BoxPayloadType.Shape4] = Direction8.North,
            [BoxPayloadType.Shape5] = Direction8.NorthEast,
            [BoxPayloadType.Shape6] = Direction8.East,
            [BoxPayloadType.Shape7] = Direction8.SouthEast,
            [BoxPayloadType.Bomb] = Direction8.South
        };
    }

    public void OnBoxArrived(BoxController box)
    {
        currentBox = box;
    }

    public void RouteCurrentBox(Direction8 direction)
    {
        if (currentBox == null)
            return;

        bool isCorrect = IsDirectionCorrect(currentBox, direction);
        ConveyorLane lane = GetLaneByDirection(direction);

        if (gameController != null)
            gameController.ResolveRouting(isCorrect);

        currentBox.RouteToLane(lane);
        currentBox = null;

        if (southEntry != null)
            southEntry.OnSorterFreed();
    }

    private bool IsDirectionCorrect(BoxController box, Direction8 direction)
    {
        if (!routeTable.TryGetValue(box.PayloadType, out Direction8 expected))
            return false;

        return expected == direction;
    }

    private ConveyorLane GetLaneByDirection(Direction8 direction)
    {
        switch (direction)
        {
            case Direction8.South:
                return southLane;
            case Direction8.SouthWest:
                return southWestLane;
            case Direction8.West:
                return westLane;
            case Direction8.NorthWest:
                return northWestLane;
            case Direction8.North:
                return northLane;
            case Direction8.NorthEast:
                return northEastLane;
            case Direction8.East:
                return eastLane;
            case Direction8.SouthEast:
                return southEastLane;
            default:
                break;
        }

        return southLane;
    }

    public Direction8 GetExpectedDirection(BoxPayloadType type)
    {
        if (routeTable == null)
            BuildRouteTable();

        if (routeTable.TryGetValue(type, out Direction8 direction))
            return direction;

        return Direction8.South;
    }
}