using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class WireNode : CircuitNode
{
    private enum WireDirection
    {
        Forward,
        Back,
        Right,
        Left
    }

    [Header("Visual")]
    [SerializeField, Range(0.05f, 0.6f)] private float isolatedSize = 0.28f;
    [SerializeField] private float centerSize = 0.35f;
    [SerializeField] private float armLength = 0.5f;
    [SerializeField] private float armThickness = 0.18f;
    [SerializeField] private float visualHeight = 0.06f;
    [SerializeField] private Color poweredColor = new Color(0.25f, 0.85f, 1f);
    [SerializeField] private Color unpoweredColor = new Color(0.18f, 0.24f, 0.28f);
    [SerializeField, Min(0f)] private float poweredEmissionIntensity = 2.5f;

    private readonly List<Renderer> visualRenderers = new List<Renderer>();
    private Transform visualRoot;
    private Renderer nodeRenderer;

    protected override void Start()
    {
        nodeType = NodeType.Wire;
        nodeRenderer = GetComponent<Renderer>();

        if (Application.isPlaying)
        {
            base.Start();
            return;
        }

        ConnectToNeighbors();
        OnPowerChanged(isPowered);
    }

    private void OnEnable()
    {
        if (Application.isPlaying)
        {
            return;
        }

        nodeType = NodeType.Wire;
        nodeRenderer = GetComponent<Renderer>();
        ConnectToNeighbors();
        OnPowerChanged(isPowered);
    }

    protected override void OnConnectionsChanged()
    {
        RebuildVisuals();
        ApplyColor(isPowered);
    }

    public override void OnPowerChanged(bool powered)
    {
        base.OnPowerChanged(powered);
        ApplyColor(powered);
    }

    public void RebuildPreviewVisuals()
    {
        nodeType = NodeType.Wire;
        nodeRenderer = GetComponent<Renderer>();
        RebuildVisuals();
        ApplyColor(false);
    }

    private void RebuildVisuals()
    {
        Transform oldVisualRoot = visualRoot != null ? visualRoot : transform.Find("WireVisual");
        if (oldVisualRoot != null)
        {
            if (Application.isPlaying)
            {
                Destroy(oldVisualRoot.gameObject);
            }
            else
            {
                DestroyImmediate(oldVisualRoot.gameObject);
            }

            visualRenderers.Clear();
        }

        visualRoot = new GameObject("WireVisual").transform;
        visualRoot.SetParent(transform, false);
        visualRoot.localScale = new Vector3(
            transform.localScale.x != 0f ? 1f / transform.localScale.x : 1f,
            transform.localScale.y != 0f ? 1f / transform.localScale.y : 1f,
            transform.localScale.z != 0f ? 1f / transform.localScale.z : 1f
        );

        HashSet<WireDirection> directions = GetConnectedDirections();

        if (directions.Count == 0)
        {
            CreateBox("Isolated", Vector3.up * (visualHeight * 0.5f), new Vector3(isolatedSize, visualHeight, isolatedSize));
            return;
        }

        CreateBox("Center", Vector3.up * (visualHeight * 0.5f), new Vector3(centerSize, visualHeight, centerSize));

        foreach (WireDirection direction in directions)
        {
            CreateArm(direction);
        }
    }

    private HashSet<WireDirection> GetConnectedDirections()
    {
        HashSet<WireDirection> directions = new HashSet<WireDirection>();

        foreach (CircuitNode connectedNode in connectedNodes)
        {
            if (connectedNode == null)
            {
                continue;
            }

            Vector3 localTarget = transform.InverseTransformPoint(connectedNode.transform.position);
            localTarget.y = 0f;

            if (localTarget.sqrMagnitude <= 0.01f)
            {
                continue;
            }

            if (Mathf.Abs(localTarget.x) > Mathf.Abs(localTarget.z))
            {
                directions.Add(localTarget.x > 0f ? WireDirection.Right : WireDirection.Left);
            }
            else
            {
                directions.Add(localTarget.z > 0f ? WireDirection.Forward : WireDirection.Back);
            }
        }

        return directions;
    }

    private void CreateArm(WireDirection direction)
    {
        Vector3 position = Vector3.up * (visualHeight * 0.5f);
        Vector3 scale;

        switch (direction)
        {
            case WireDirection.Forward:
                position += Vector3.forward * (centerSize * 0.5f + armLength * 0.5f);
                scale = new Vector3(armThickness, visualHeight, armLength);
                break;
            case WireDirection.Back:
                position += Vector3.back * (centerSize * 0.5f + armLength * 0.5f);
                scale = new Vector3(armThickness, visualHeight, armLength);
                break;
            case WireDirection.Right:
                position += Vector3.right * (centerSize * 0.5f + armLength * 0.5f);
                scale = new Vector3(armLength, visualHeight, armThickness);
                break;
            default:
                position += Vector3.left * (centerSize * 0.5f + armLength * 0.5f);
                scale = new Vector3(armLength, visualHeight, armThickness);
                break;
        }

        CreateBox("Arm_" + direction, position, scale);
    }

    private void CreateBox(string objectName, Vector3 localPosition, Vector3 localScale)
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = objectName;
        visual.transform.SetParent(visualRoot, false);
        visual.transform.localPosition = localPosition;
        visual.transform.localScale = localScale;

        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
        {
            if (Application.isPlaying)
            {
                Destroy(visualCollider);
            }
            else
            {
                DestroyImmediate(visualCollider);
            }
        }

        Renderer visualRenderer = visual.GetComponent<Renderer>();
        if (visualRenderer != null)
        {
            visualRenderers.Add(visualRenderer);
        }
    }

    private void ApplyColor(bool powered)
    {
        Color color = powered ? poweredColor : unpoweredColor;

        if (nodeRenderer != null)
        {
            nodeRenderer.enabled = false;
        }

        foreach (Renderer visualRenderer in visualRenderers)
        {
            if (visualRenderer != null)
            {
                Material material = visualRenderer.material;
                material.color = color;

                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", color);
                }

                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor(
                        "_EmissionColor",
                        powered ? color * poweredEmissionIntensity : Color.black
                    );
                }
            }
        }
    }
}
