using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [SerializeField] private Transform focus;
    [SerializeField] private float height = 10f;
    [SerializeField] private float distance = 10f;
    [SerializeField] private float tiltAngle = 60f;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private bool invertRotationControls = false;

    private float currentYaw;
    private float targetYaw;
    private float shakeTimeRemaining;
    private float shakeDuration;
    private float shakeStrength;

    private void Awake()
    {
        Instance = this;

        if (focus == null)
        {
            PlayerController player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                focus = player.transform;
            }
        }

        currentYaw = transform.eulerAngles.y;
        targetYaw = currentYaw;
    }

    public static CameraManager GetOrCreateMainCameraManager()
    {
        if (Instance != null)
        {
            return Instance;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return null;
        }

        CameraManager manager = mainCamera.GetComponent<CameraManager>();
        if (manager == null)
        {
            manager = mainCamera.gameObject.AddComponent<CameraManager>();
        }

        Instance = manager;
        return manager;
    }

    private void LateUpdate()
    {
        currentYaw = Mathf.MoveTowardsAngle(currentYaw, targetYaw, rotationSpeed * Time.deltaTime);

        var rotation = Quaternion.Euler(tiltAngle, currentYaw, 0f);
        var offset = rotation * new Vector3(0f, height, -distance);
        var focusPosition = focus != null ? focus.position : Vector3.zero;

        Vector3 shakeOffset = Vector3.zero;
        if (shakeTimeRemaining > 0f)
        {
            shakeTimeRemaining = Mathf.Max(0f, shakeTimeRemaining - Time.unscaledDeltaTime);
            float fade = shakeDuration > 0f ? shakeTimeRemaining / shakeDuration : 0f;
            shakeOffset = Random.insideUnitSphere * (shakeStrength * fade);
        }

        transform.position = focusPosition + offset + shakeOffset;
        transform.rotation = rotation;
    }

    public void Shake(float duration = 0.18f, float strength = 0.09f)
    {
        shakeDuration = Mathf.Max(shakeDuration, duration);
        shakeTimeRemaining = Mathf.Max(shakeTimeRemaining, duration);
        shakeStrength = Mathf.Max(shakeStrength, strength);
    }

    public void RotateLeft()
    {
        targetYaw += invertRotationControls ? -90f : 90f;
    }

    public void RotateRight()
    {
        targetYaw += invertRotationControls ? 90f : -90f;
    }
}
