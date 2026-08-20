using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class StartButton : BaseBehaviour
{
    public Camera targetCamera;

    public float mouseRotationSpeed = 0.1f;
    public float inertiaDamping = 3f;
    public Vector3 baseRotationSpeed = new(10f, 15f, 5f);
    public float clickDragPixelThreshold = 20f;

    public GameObject ExitButton;
    public GameObject closedBoxObject;
    public GameObject openBoxObject;
    public GameObject fadeImageObject;

    public float spinRandomAngleMin = 400f;
    public float spinRandomAngleMax = 800f;
    public float spinDuration = 0.7f;
    public float settleDuration = 0.6f;
    public float fadeDuration = 0.9f;

    private bool isDragging;
    private bool isAnimating;
    private Vector3 lastMousePosition;
    private Quaternion dragStartRotation;
    private Vector2 dragTotalDelta;
    private Vector3 inertiaVelocity;
    private Vector2 dragPixelDelta;
    private Sequence pressSequence;
    private Collider hitCollider;

    private void Awake()
    {
        targetCamera = Camera.main;
        hitCollider = GetComponent<Collider>();
    }

    private void BeginDrag(Vector2 pointerPosition)
    {
        if (isAnimating)
            return;

        isDragging = true;
        lastMousePosition = pointerPosition;
        dragStartRotation = transform.rotation;
        dragTotalDelta = Vector2.zero;
        inertiaVelocity = Vector3.zero;
        dragPixelDelta = Vector2.zero;
    }

    private void EndDrag()
    {
        if (isAnimating)
            return;

        bool wasDragging = isDragging;
        isDragging = false;

        if (!wasDragging)
            return;

        float threshold = clickDragPixelThreshold;
        float thresholdSqr = threshold * threshold;

        if (dragPixelDelta.sqrMagnitude <= thresholdSqr)
            HandleClick();
    }

    private void Update()
    {
        if (isAnimating)
            return;

        Mouse mouse = Mouse.current;
        Vector2 pointerPosition = mouse != null ? mouse.position.ReadValue() : Vector2.zero;

        if (mouse != null && mouse.leftButton.wasPressedThisFrame && IsPointerOverSelf(pointerPosition))
            BeginDrag(pointerPosition);

        if (mouse != null && mouse.leftButton.wasReleasedThisFrame && isDragging)
            EndDrag();

        if (isDragging && targetCamera != null && mouse != null)
        {
            Vector3 mousePosition = pointerPosition;
            Vector3 mouseDelta = mousePosition - lastMousePosition;
            lastMousePosition = mousePosition;

            Vector2 pixelDelta = (Vector2)mouseDelta;
            dragPixelDelta += pixelDelta;

            Vector2 scaledDelta = (Vector2)mouseDelta * mouseRotationSpeed;
            dragTotalDelta += scaledDelta;

            float rotX = dragTotalDelta.y;
            float rotY = -dragTotalDelta.x;

            Vector3 axisRight = targetCamera.transform.right;
            Vector3 axisUp = targetCamera.transform.up;

            Quaternion yaw = Quaternion.AngleAxis(rotY, axisUp);
            Quaternion pitch = Quaternion.AngleAxis(rotX, axisRight);
            Quaternion dragRotation = yaw * pitch;

            transform.rotation = dragRotation * dragStartRotation;

            if (Time.deltaTime > 0f)
                inertiaVelocity = new Vector3(scaledDelta.y, -scaledDelta.x, 0f) / Time.deltaTime;
        }
        else
        {
            Vector3 axisRight = targetCamera.transform.right;
            Vector3 axisUp = targetCamera.transform.up;

            float deltaPitch = inertiaVelocity.x * Time.deltaTime;
            float deltaYaw = inertiaVelocity.y * Time.deltaTime;

            if (deltaPitch != 0f || deltaYaw != 0f)
            {
                Quaternion yaw = Quaternion.AngleAxis(deltaYaw, axisUp);
                Quaternion pitch = Quaternion.AngleAxis(deltaPitch, axisRight);
                Quaternion inertiaRotation = yaw * pitch;
                transform.rotation = inertiaRotation * transform.rotation;
            }
        }

        transform.Rotate(baseRotationSpeed * Time.deltaTime, Space.Self);

        float t = inertiaDamping * Time.deltaTime;
        if (t > 1f)
            t = 1f;
        inertiaVelocity = Vector3.Lerp(inertiaVelocity, Vector3.zero, t);
    }

    private bool IsPointerOverSelf(Vector2 pointerPosition)
    {
        if (targetCamera == null || hitCollider == null)
            return false;

        Ray ray = targetCamera.ScreenPointToRay(pointerPosition);
        return hitCollider.Raycast(ray, out _, Mathf.Infinity);
    }

    private void HandleClick()
    {
        StartPressAnimation();
    }

    private void StartPressAnimation()
    {
        isAnimating = true;
        isDragging = false;
        inertiaVelocity = Vector3.zero;
        dragTotalDelta = Vector2.zero;

        baseRotationSpeed = Vector3.zero;

        if (pressSequence != null && pressSequence.IsActive())
            pressSequence.Kill();

        float angleX = Random.Range(spinRandomAngleMin, spinRandomAngleMax) * (Random.value > 0.5f ? 1f : -1f);
        float angleY = Random.Range(spinRandomAngleMin, spinRandomAngleMax) * (Random.value > 0.5f ? 1f : -1f);
        float angleZ = Random.Range(spinRandomAngleMin, spinRandomAngleMax) * (Random.value > 0.5f ? 1f : -1f);
        Vector3 spinEuler = new(angleX, angleY, angleZ);

        pressSequence = DOTween.Sequence();
        pressSequence.Append(
            transform.DORotate(spinEuler, spinDuration, RotateMode.LocalAxisAdd)
                .SetEase(Ease.OutQuad)
        );
        pressSequence.Append(
            transform.DORotate(Vector3.zero, settleDuration, RotateMode.Fast)
                .SetEase(Ease.OutCubic)
        );
        pressSequence.OnComplete(OnPressAnimationComplete);
    }

    private void OnPressAnimationComplete()
    {
        transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        ExitButton.SetActive(false);
        closedBoxObject.SetActive(false);
        openBoxObject.SetActive(true);
        fadeImageObject.SetActive(true);

        CanvasGroup canvasGroup = fadeImageObject.GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;

        canvasGroup
            .DOFade(1f, fadeDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() => SceneManager.LoadScene(1));
    }
}
