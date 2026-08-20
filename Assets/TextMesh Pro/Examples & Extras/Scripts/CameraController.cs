using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;


namespace TMPro.Examples
{
    
    public class CameraController : MonoBehaviour
    {
        public enum CameraModes { Follow, Isometric, Free }

        private Transform cameraTransform;
        private Transform dummyTarget;

        public Transform CameraTarget;

        public float FollowDistance = 30.0f;
        public float MaxFollowDistance = 100.0f;
        public float MinFollowDistance = 2.0f;

        public float ElevationAngle = 30.0f;
        public float MaxElevationAngle = 85.0f;
        public float MinElevationAngle = 0f;

        public float OrbitalAngle = 0f;

        public CameraModes CameraMode = CameraModes.Follow;

        public bool MovementSmoothing = true;
        public bool RotationSmoothing = false;
        private bool previousSmoothing;

        public float MovementSmoothingValue = 25f;
        public float RotationSmoothingValue = 5.0f;

        public float MoveSensitivity = 2.0f;

        private Vector3 currentVelocity = Vector3.zero;
        private Vector3 desiredPosition;
        private float mouseX;
        private float mouseY;
        private Vector3 moveVector;
        private float mouseWheel;

        // Controls for Touches on Mobile devices
        //private float prev_ZoomDelta;


        private const string event_SmoothingValue = "Slider - Smoothing Value";
        private const string event_FollowDistance = "Slider - Camera Zoom";


        void Awake()
        {
            if (QualitySettings.vSyncCount > 0)
                Application.targetFrameRate = 60;
            else
                Application.targetFrameRate = -1;

            cameraTransform = transform;
            previousSmoothing = MovementSmoothing;
        }

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }


        // Use this for initialization
        void Start()
        {
            if (CameraTarget == null)
            {
                // If we don't have a target (assigned by the player, create a dummy in the center of the scene).
                dummyTarget = new GameObject("Camera Target").transform;
                CameraTarget = dummyTarget;
            }
        }

        // Update is called once per frame
        void LateUpdate()
        {
            GetPlayerInput();


            // Check if we still have a valid target
            if (CameraTarget != null)
            {
                if (CameraMode == CameraModes.Isometric)
                {
                    desiredPosition = CameraTarget.position + Quaternion.Euler(ElevationAngle, OrbitalAngle, 0f) * new Vector3(0, 0, -FollowDistance);
                }
                else if (CameraMode == CameraModes.Follow)
                {
                    desiredPosition = CameraTarget.position + CameraTarget.TransformDirection(Quaternion.Euler(ElevationAngle, OrbitalAngle, 0f) * (new Vector3(0, 0, -FollowDistance)));
                }
                else
                {
                    // Free Camera implementation
                }

                if (MovementSmoothing == true)
                {
                    // Using Smoothing
                    cameraTransform.position = Vector3.SmoothDamp(cameraTransform.position, desiredPosition, ref currentVelocity, MovementSmoothingValue * Time.fixedDeltaTime);
                    //cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, Time.deltaTime * 5.0f);
                }
                else
                {
                    // Not using Smoothing
                    cameraTransform.position = desiredPosition;
                }

                if (RotationSmoothing == true)
                    cameraTransform.rotation = Quaternion.Lerp(cameraTransform.rotation, Quaternion.LookRotation(CameraTarget.position - cameraTransform.position), RotationSmoothingValue * Time.deltaTime);
                else
                {
                    cameraTransform.LookAt(CameraTarget);
                }

            }

        }



        void GetPlayerInput()
        {
            moveVector = Vector3.zero;
            Mouse mouse = Mouse.current;
            Keyboard keyboard = Keyboard.current;
            var touches = Touch.activeTouches;

            // Check Mouse Wheel Input prior to Shift Key so we can apply multiplier on Shift for Scrolling
            mouseWheel = mouse != null ? mouse.scroll.ReadValue().y / 1200f : 0f;

            int touchCount = touches.Count;
            bool shiftPressed = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);

            if (shiftPressed || touchCount > 0)
            {
                mouseWheel *= 10;

                if (keyboard != null && keyboard.iKey.wasPressedThisFrame)
                    CameraMode = CameraModes.Isometric;

                if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
                    CameraMode = CameraModes.Follow;

                if (keyboard != null && keyboard.sKey.wasPressedThisFrame)
                    MovementSmoothing = !MovementSmoothing;


                // Check for right mouse button to change camera follow and elevation angle
                if (mouse != null && mouse.rightButton.isPressed)
                {
                    Vector2 mouseDelta = mouse.delta.ReadValue() * 0.1f;
                    mouseY = mouseDelta.y;
                    mouseX = mouseDelta.x;

                    if (mouseY > 0.01f || mouseY < -0.01f)
                    {
                        ElevationAngle -= mouseY * MoveSensitivity;
                        // Limit Elevation angle between min & max values.
                        ElevationAngle = Mathf.Clamp(ElevationAngle, MinElevationAngle, MaxElevationAngle);
                    }

                    if (mouseX > 0.01f || mouseX < -0.01f)
                    {
                        OrbitalAngle += mouseX * MoveSensitivity;
                        if (OrbitalAngle > 360)
                            OrbitalAngle -= 360;
                        if (OrbitalAngle < 0)
                            OrbitalAngle += 360;
                    }
                }

                // Get Input from Mobile Device
                if (touchCount == 1 && touches[0].phase == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    Vector2 deltaPosition = touches[0].delta;

                    // Handle elevation changes
                    if (deltaPosition.y > 0.01f || deltaPosition.y < -0.01f)
                    {
                        ElevationAngle -= deltaPosition.y * 0.1f;
                        // Limit Elevation angle between min & max values.
                        ElevationAngle = Mathf.Clamp(ElevationAngle, MinElevationAngle, MaxElevationAngle);
                    }


                    // Handle left & right 
                    if (deltaPosition.x > 0.01f || deltaPosition.x < -0.01f)
                    {
                        OrbitalAngle += deltaPosition.x * 0.1f;
                        if (OrbitalAngle > 360)
                            OrbitalAngle -= 360;
                        if (OrbitalAngle < 0)
                            OrbitalAngle += 360;
                    }

                }

                // Check for left mouse button to select a new CameraTarget or to reset Follow position
                if (mouse != null && mouse.leftButton.isPressed)
                {
                    Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
                    RaycastHit hit;

                    if (Physics.Raycast(ray, out hit, 300, 1 << 10 | 1 << 11 | 1 << 12 | 1 << 14))
                    {
                        if (hit.transform == CameraTarget)
                        {
                            // Reset Follow Position
                            OrbitalAngle = 0;
                        }
                        else
                        {
                            CameraTarget = hit.transform;
                            OrbitalAngle = 0;
                            MovementSmoothing = previousSmoothing;
                        }

                    }
                }


                if (mouse != null && mouse.middleButton.isPressed)
                {
                    if (dummyTarget == null)
                    {
                        // We need a Dummy Target to anchor the Camera
                        dummyTarget = new GameObject("Camera Target").transform;
                        dummyTarget.position = CameraTarget.position;
                        dummyTarget.rotation = CameraTarget.rotation;
                        CameraTarget = dummyTarget;
                        previousSmoothing = MovementSmoothing;
                        MovementSmoothing = false;
                    }
                    else if (dummyTarget != CameraTarget)
                    {
                        // Move DummyTarget to CameraTarget
                        dummyTarget.position = CameraTarget.position;
                        dummyTarget.rotation = CameraTarget.rotation;
                        CameraTarget = dummyTarget;
                        previousSmoothing = MovementSmoothing;
                        MovementSmoothing = false;
                    }


                    Vector2 mouseDelta = mouse.delta.ReadValue() * 0.1f;
                    mouseY = mouseDelta.y;
                    mouseX = mouseDelta.x;

                    moveVector = cameraTransform.TransformDirection(mouseX, mouseY, 0);

                    dummyTarget.Translate(-moveVector, Space.World);

                }

            }

            // Check Pinching to Zoom in - out on Mobile device
            if (touchCount == 2)
            {
                Touch touch0 = touches[0];
                Touch touch1 = touches[1];

                Vector2 touch0PrevPos = touch0.screenPosition - touch0.delta;
                Vector2 touch1PrevPos = touch1.screenPosition - touch1.delta;

                float prevTouchDelta = (touch0PrevPos - touch1PrevPos).magnitude;
                float touchDelta = (touch0.screenPosition - touch1.screenPosition).magnitude;

                float zoomDelta = prevTouchDelta - touchDelta;

                if (zoomDelta > 0.01f || zoomDelta < -0.01f)
                {
                    FollowDistance += zoomDelta * 0.25f;
                    // Limit FollowDistance between min & max values.
                    FollowDistance = Mathf.Clamp(FollowDistance, MinFollowDistance, MaxFollowDistance);
                }


            }

            // Check MouseWheel to Zoom in-out
            if (mouseWheel < -0.01f || mouseWheel > 0.01f)
            {

                FollowDistance -= mouseWheel * 5.0f;
                // Limit FollowDistance between min & max values.
                FollowDistance = Mathf.Clamp(FollowDistance, MinFollowDistance, MaxFollowDistance);
            }


        }
    }
}
