using UnityEngine;

public class StartButton : MonoBehaviour
{
    public Camera targetCamera;

    public float mouseRotationSpeed = 0.1f;
    public float inertiaDamping = 3f;
    public Vector3 baseRotationSpeed = new(10f, 15f, 5f);

    private bool isDragging;
    private Vector3 lastMousePosition;
    private Quaternion dragStartRotation;
    private Vector2 dragTotalDelta;
    private Vector3 inertiaVelocity;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void OnMouseDown()
    {
        isDragging = true;
        lastMousePosition = Input.mousePosition;
        dragStartRotation = transform.rotation;
        dragTotalDelta = Vector2.zero;
        inertiaVelocity = Vector3.zero;
    }

    private void OnMouseUp()
    {
        isDragging = false;
    }

    private void Update()
    {
        if (isDragging && targetCamera != null)
        {
            Vector3 mousePosition = Input.mousePosition;
            Vector3 mouseDelta = mousePosition - lastMousePosition;
            lastMousePosition = mousePosition;

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
            if (targetCamera != null)
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
    }
}