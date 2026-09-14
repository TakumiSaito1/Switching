using UnityEngine;

public class PoweredBridgeNode : MonoBehaviour
{
    [Header("Bridge")]
    [SerializeField] private Transform bridgeBody;
    [SerializeField] private CircuitNode powerSource;
    [SerializeField] private Vector3 poweredLocalPosition = Vector3.zero;
    [SerializeField] private Vector3 unpoweredLocalPosition = new Vector3(0f, -3f, 0f);
    [SerializeField] private float moveSpeed = 3f;

    [Header("Visual")]
    [SerializeField] private Color poweredColor = new Color(0.25f, 0.85f, 1f);
    [SerializeField] private Color unpoweredColor = new Color(0.18f, 0.24f, 0.28f);

    private Vector3 targetLocalPosition;
    private Renderer[] bridgeRenderers;
    private bool isPowered;
    private bool hasInitializedPowerState;

    private void Start()
    {
        if (bridgeBody == null)
        {
            bridgeBody = transform;
        }

        bridgeRenderers = bridgeBody.GetComponentsInChildren<Renderer>();
        bridgeBody.localPosition = unpoweredLocalPosition;
        targetLocalPosition = unpoweredLocalPosition;

        if (powerSource != null)
        {
            OnPowerChanged(powerSource.isPowered);
        }
    }

    private void Update()
    {
        if (bridgeBody == null)
        {
            return;
        }

        if (powerSource != null && powerSource.isPowered != isPowered)
        {
            OnPowerChanged(powerSource.isPowered);
        }

        bridgeBody.localPosition = Vector3.MoveTowards(
            bridgeBody.localPosition,
            targetLocalPosition,
            moveSpeed * Time.deltaTime
        );
    }

    private void OnPowerChanged(bool powered)
    {
        isPowered = powered;
        targetLocalPosition = powered ? poweredLocalPosition : unpoweredLocalPosition;
        ApplyColor(powered);

        if (hasInitializedPowerState && powered)
        {
            Vector3 effectPosition = bridgeBody != null ? bridgeBody.position : transform.position;
            GameSfx.PlayAt("sfx_door_open_lowpoly", effectPosition, 0.65f);
            PowerActivationEffect.Play(effectPosition);
            CameraManager.GetOrCreateMainCameraManager()?.Shake(0.16f, 0.07f);
        }

        hasInitializedPowerState = true;
    }

    private void ApplyColor(bool powered)
    {
        if (bridgeRenderers == null)
        {
            return;
        }

        Color color = powered ? poweredColor : unpoweredColor;
        foreach (Renderer bridgeRenderer in bridgeRenderers)
        {
            if (bridgeRenderer != null)
            {
                bridgeRenderer.material.color = color;
            }
        }
    }
}
