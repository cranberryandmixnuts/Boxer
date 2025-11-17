using UnityEngine;

public class ConveyorScroll : MonoBehaviour
{
    [SerializeField]
    private Renderer targetRenderer;

    [SerializeField]
    private Vector2 scrollSpeed = new(0f, 1f);

    private void Update()
    {
        Vector2 offset = scrollSpeed * Time.time;
        targetRenderer.material.mainTextureOffset = offset;
    }
}