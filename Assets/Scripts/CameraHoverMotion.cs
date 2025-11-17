using UnityEngine;

public sealed class CameraHoverMotion : MonoBehaviour
{
    [Header("Hover 설정")]
    [SerializeField]
    private float forwardAmplitude = 0.2f;

    [SerializeField]
    private float forwardSpeed = 0.3f;

    [SerializeField]
    private float heightAmplitude = 0.1f;

    [SerializeField]
    private float heightSpeed = 0.2f;

    [SerializeField]
    private float tiltAngle = 0.6f;

    [SerializeField]
    private float tiltSpeed = 0.1f;

    [Header("정답 효과")]
    [SerializeField]
    private float successKickDistance = 0.15f;

    [SerializeField]
    private float successKickDuration = 0.15f;

    [Header("오답 효과")]
    [SerializeField]
    private float failShakeAmplitude = 0.4f;

    [SerializeField]
    private float failShakeDuration = 0.4f;

    [SerializeField]
    private float failShakeFrequency = 20f;

    private Vector3 baseLocalPosition;
    private Vector3 baseLocalEulerAngles;
    private float time;
    private float successKickTimer;
    private float failShakeTimer;

    private void Awake()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalEulerAngles = transform.localEulerAngles;
    }

    private void OnEnable()
    {
        time = 0f;
        successKickTimer = 0f;
        failShakeTimer = 0f;
    }

    private void OnDisable()
    {
        transform.localPosition = baseLocalPosition;
        transform.localEulerAngles = baseLocalEulerAngles;
    }

    private void Update()
    {
        time += Time.deltaTime;

        float forwardOffset = Mathf.Sin(time * forwardSpeed) * forwardAmplitude;
        float heightOffset = Mathf.Sin(time * heightSpeed) * heightAmplitude;
        float tiltOffset = Mathf.Sin(time * tiltSpeed) * tiltAngle;

        Vector3 hoverPositionOffset = Vector3.forward * forwardOffset + Vector3.up * heightOffset;
        float hoverTiltOffset = tiltOffset;

        Vector3 effectPositionOffset = Vector3.zero;
        float effectTiltOffset = 0f;

        if (successKickTimer > 0f && successKickDuration > 0f)
        {
            successKickTimer -= Time.deltaTime;
            if (successKickTimer < 0f)
                successKickTimer = 0f;

            float t = 1f - (successKickTimer / successKickDuration);
            t = Mathf.Clamp01(t);
            float kick = Mathf.Sin(t * Mathf.PI);
            effectPositionOffset += Vector3.back * kick * successKickDistance;
        }

        if (failShakeTimer > 0f && failShakeDuration > 0f)
        {
            failShakeTimer -= Time.deltaTime;
            if (failShakeTimer < 0f)
                failShakeTimer = 0f;

            float progress = 1f - (failShakeTimer / failShakeDuration);
            progress = Mathf.Clamp01(progress);
            float damper = 1f - progress;
            float shake = Mathf.Sin(time * failShakeFrequency) * failShakeAmplitude * damper;

            effectPositionOffset += Vector3.right * shake;
            effectTiltOffset += shake * 1.5f;
        }

        Vector3 localPosition = baseLocalPosition + hoverPositionOffset + effectPositionOffset;
        transform.localPosition = localPosition;

        Vector3 localEulerAngles = baseLocalEulerAngles;
        localEulerAngles.z += hoverTiltOffset + effectTiltOffset;
        transform.localEulerAngles = localEulerAngles;
    }

    public void PlaySuccessKick()
    {
        successKickTimer = successKickDuration;
        failShakeTimer = 0f;
    }

    public void PlayFailShake()
    {
        failShakeTimer = failShakeDuration;
        successKickTimer = 0f;
    }
}