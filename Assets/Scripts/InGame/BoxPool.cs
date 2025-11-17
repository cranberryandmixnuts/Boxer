using System.Collections.Generic;
using UnityEngine;

public class BoxPool : MonoBehaviour
{
    public static BoxPool Instance { get; private set; }

    [SerializeField]
    private BoxController boxPrefab;

    [SerializeField]
    private int initialCount = 16;

    private readonly Stack<BoxController> pool = new Stack<BoxController>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        for (int i = 0; i < initialCount; i++)
            CreateNew();
    }

    private BoxController CreateNew()
    {
        BoxController box = Instantiate(boxPrefab, transform);
        box.gameObject.SetActive(false);
        pool.Push(box);
        return box;
    }

    public BoxController Get()
    {
        if (pool.Count == 0)
            CreateNew();

        BoxController box = pool.Pop();
        box.gameObject.SetActive(true);
        box.SetPool(this);
        return box;
    }

    public void Release(BoxController box)
    {
        box.gameObject.SetActive(false);
        pool.Push(box);
    }
}
