using UnityEngine;

public class DoorNode : CircuitNode
{
    [Header("Door")]
    [SerializeField] private Transform doorBody;
    [SerializeField] private Collider blockingCollider;
    [SerializeField] private Vector3 hingeLocalOffset = new Vector3(-0.5f, 0f, 0f);
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 180f;
    [SerializeField] private bool disableColliderWhenOpen = false;

    [Header("Connection Marker")]
    [SerializeField] private bool showConnectionMarker = true;
    [SerializeField] private bool worldAlignedConnectionMarker = true;
    [SerializeField] private float worldAlignedConnectionMarkerYaw = 0f;
    [SerializeField] private Vector3 connectionMarkerLocalOffset = new Vector3(0f, -0.43f, -1f);
    [SerializeField] private Color connectionMarkerColor = new Color(1f, 0.82f, 0.12f, 1f);
    [SerializeField] private Color connectionMarkerPulseColor = new Color(0.1f, 1f, 1f, 1f);
    [SerializeField] private float connectionMarkerPulseSpeed = 2.2f;
    [SerializeField] private float connectionMarkerGroundOffset = 0.04f;
    [SerializeField] private float connectionMarkerGroundSearchHeight = 3f;

    private Vector3 closedPosition;
    private Quaternion closedRotation;
    private float currentAngle;
    private float targetAngle;
    private GameObject runtimeDoorBodyObject;
    private bool detachedDoorBody;
    private bool hasInitializedPowerState;
    private Transform connectionMarkerRoot;
    private Material connectionMarkerMaterial;

    private void Awake()
    {
        if (doorBody == null)
        {
            doorBody = CreateRuntimeDoorBody();
        }

        if (blockingCollider == null)
        {
            blockingCollider = doorBody.GetComponent<Collider>();
        }

        // A rotating child under a non-uniformly scaled transform is sheared by
        // the parent's scale, which makes the door appear to change size while
        // opening. Keep its current world transform, but move it beside the door
        // root so the opening rotation is applied without that scale distortion.
        if (doorBody.parent == transform)
        {
            doorBody.SetParent(transform.parent, true);
            detachedDoorBody = true;
        }

        closedPosition = doorBody.position;
        closedRotation = doorBody.rotation;
        CreateConnectionMarker();
    }

    protected override void Start()
    {
        nodeType = NodeType.Door;
        base.Start();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (runtimeDoorBodyObject == null && detachedDoorBody && doorBody != null)
        {
            if (Application.isPlaying)
            {
                Destroy(doorBody.gameObject);
            }
            else
            {
                DestroyImmediate(doorBody.gameObject);
            }
        }

        if (runtimeDoorBodyObject != null)
        {
            if (Application.isPlaying)
            {
                Destroy(runtimeDoorBodyObject);
            }
            else
            {
                DestroyImmediate(runtimeDoorBodyObject);
            }
        }

        if (connectionMarkerRoot != null)
        {
            if (Application.isPlaying)
            {
                Destroy(connectionMarkerRoot.gameObject);
            }
            else
            {
                DestroyImmediate(connectionMarkerRoot.gameObject);
            }
        }

        if (connectionMarkerMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(connectionMarkerMaterial);
            }
            else
            {
                DestroyImmediate(connectionMarkerMaterial);
            }
        }
    }

    private void Update()
    {
        float nextAngle = Mathf.MoveTowards(currentAngle, targetAngle, openSpeed * Time.deltaTime);
        ApplyAngle(nextAngle);
        UpdateConnectionMarker();
    }

    public override void OnPowerChanged(bool powered)
    {
        base.OnPowerChanged(powered);

        targetAngle = powered ? openAngle : 0f;

        if (disableColliderWhenOpen && blockingCollider != null)
        {
            blockingCollider.enabled = !powered;
        }

        if (hasInitializedPowerState)
        {
            GameSfx.PlayAt("sfx_door_open_lowpoly", transform.position, 0.8f);

            if (powered)
            {
                PowerActivationEffect.Play(transform.position + Vector3.up * 0.8f);
                CameraManager.GetOrCreateMainCameraManager()?.Shake();
            }
        }

        hasInitializedPowerState = true;
    }

    private void ApplyAngle(float angle)
    {
        currentAngle = angle;

        Vector3 hingeWorldPosition = closedPosition + closedRotation * hingeLocalOffset;
        Quaternion rotationOffset = Quaternion.AngleAxis(currentAngle, Vector3.up);

        doorBody.rotation = rotationOffset * closedRotation;
        doorBody.position = hingeWorldPosition + rotationOffset * (closedPosition - hingeWorldPosition);
    }

    private Transform CreateRuntimeDoorBody()
    {
        runtimeDoorBodyObject = new GameObject("DoorBody");
        runtimeDoorBodyObject.layer = gameObject.layer;
        runtimeDoorBodyObject.transform.SetParent(transform.parent, false);
        runtimeDoorBodyObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
        runtimeDoorBodyObject.transform.localScale = transform.localScale;

        MeshFilter rootMeshFilter = GetComponent<MeshFilter>();
        if (rootMeshFilter != null)
        {
            MeshFilter bodyMeshFilter = runtimeDoorBodyObject.AddComponent<MeshFilter>();
            bodyMeshFilter.sharedMesh = rootMeshFilter.sharedMesh;
        }

        Renderer rootRenderer = GetComponent<Renderer>();
        if (rootRenderer != null)
        {
            MeshRenderer bodyRenderer = runtimeDoorBodyObject.AddComponent<MeshRenderer>();
            bodyRenderer.sharedMaterials = rootRenderer.sharedMaterials;
            rootRenderer.enabled = false;
        }

        Collider rootCollider = GetComponent<Collider>();
        if (rootCollider != null)
        {
            blockingCollider = CopyCollider(rootCollider, runtimeDoorBodyObject);
            rootCollider.enabled = false;
        }

        return runtimeDoorBodyObject.transform;
    }

    private void CreateConnectionMarker()
    {
        if (!showConnectionMarker)
        {
            return;
        }

        GameObject root = new GameObject("CircuitPlacementMarker");
        root.layer = gameObject.layer;
        root.transform.SetParent(transform.parent, true);
        root.transform.SetPositionAndRotation(GetConnectionMarkerPosition(), GetConnectionMarkerRotation());
        root.transform.localScale = Vector3.one;
        connectionMarkerRoot = root.transform;

        connectionMarkerMaterial = new Material(FindMarkerShader());
        SetMarkerMaterialColor(connectionMarkerColor);

        CreateMarkerBar("Marker_Edge_Front", new Vector3(0f, 0f, 0.45f), new Vector3(0.78f, 0.035f, 0.08f));
        CreateMarkerBar("Marker_Edge_Back", new Vector3(0f, 0f, -0.45f), new Vector3(0.78f, 0.035f, 0.08f));
        CreateMarkerBar("Marker_Edge_Left", new Vector3(-0.45f, 0f, 0f), new Vector3(0.08f, 0.035f, 0.78f));
        CreateMarkerBar("Marker_Edge_Right", new Vector3(0.45f, 0f, 0f), new Vector3(0.08f, 0.035f, 0.78f));
        CreateMarkerBar("Marker_Arrow_Stem", new Vector3(0f, 0f, 0.88f), new Vector3(0.11f, 0.035f, 0.42f));

        Transform arrowLeft = CreateMarkerBar("Marker_Arrow_Left", new Vector3(-0.12f, 0f, 0.62f), new Vector3(0.09f, 0.035f, 0.35f));
        arrowLeft.localRotation = Quaternion.Euler(0f, 35f, 0f);

        Transform arrowRight = CreateMarkerBar("Marker_Arrow_Right", new Vector3(0.12f, 0f, 0.62f), new Vector3(0.09f, 0.035f, 0.35f));
        arrowRight.localRotation = Quaternion.Euler(0f, -35f, 0f);
    }

    private Transform CreateMarkerBar(string objectName, Vector3 localPosition, Vector3 localScale)
    {
        GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bar.name = objectName;
        bar.layer = gameObject.layer;
        bar.transform.SetParent(connectionMarkerRoot, false);
        bar.transform.localPosition = localPosition;
        bar.transform.localScale = localScale;

        Collider markerCollider = bar.GetComponent<Collider>();
        if (markerCollider != null)
        {
            Destroy(markerCollider);
        }

        Renderer markerRenderer = bar.GetComponent<Renderer>();
        if (markerRenderer != null)
        {
            markerRenderer.sharedMaterial = connectionMarkerMaterial;
        }

        return bar.transform;
    }

    private void UpdateConnectionMarker()
    {
        if (connectionMarkerRoot == null || connectionMarkerMaterial == null)
        {
            return;
        }

        float pulse = (Mathf.Sin(Time.time * connectionMarkerPulseSpeed) + 1f) * 0.5f;
        Color markerColor = Color.Lerp(connectionMarkerColor, connectionMarkerPulseColor, pulse);
        SetMarkerMaterialColor(markerColor);
        float markerScale = Mathf.Lerp(0.92f, 1.08f, pulse);
        connectionMarkerRoot.SetPositionAndRotation(GetConnectionMarkerPosition(), GetConnectionMarkerRotation());
        connectionMarkerRoot.localScale = new Vector3(markerScale, 1f, markerScale);
    }

    private Vector3 GetConnectionMarkerPosition()
    {
        Vector3 markerPosition = transform.position + GetConnectionMarkerRotation() * connectionMarkerLocalOffset;
        Ray groundRay = new Ray(markerPosition + Vector3.up * connectionMarkerGroundSearchHeight, Vector3.down);
        RaycastHit[] hits = Physics.RaycastAll(
            groundRay,
            connectionMarkerGroundSearchHeight * 2f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = float.MaxValue;
        bool foundGround = false;
        foreach (RaycastHit hit in hits)
        {
            if (!hit.collider.CompareTag("Ground"))
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                markerPosition.y = hit.point.y + connectionMarkerGroundOffset;
                foundGround = true;
            }
        }

        if (!foundGround)
        {
            markerPosition.y += connectionMarkerGroundOffset;
        }

        return markerPosition;
    }

    private Quaternion GetConnectionMarkerRotation()
    {
        return worldAlignedConnectionMarker
            ? Quaternion.Euler(0f, worldAlignedConnectionMarkerYaw, 0f)
            : transform.rotation;
    }

    public override Vector3 GetConnectionPosition()
    {
        return transform.position + GetConnectionMarkerRotation() * connectionMarkerLocalOffset;
    }

    private void SetMarkerMaterialColor(Color color)
    {
        if (connectionMarkerMaterial == null)
        {
            return;
        }

        connectionMarkerMaterial.color = color;

        if (connectionMarkerMaterial.HasProperty("_BaseColor"))
        {
            connectionMarkerMaterial.SetColor("_BaseColor", color);
        }

        if (connectionMarkerMaterial.HasProperty("_EmissionColor"))
        {
            connectionMarkerMaterial.EnableKeyword("_EMISSION");
            connectionMarkerMaterial.SetColor("_EmissionColor", color * 1.4f);
        }
    }

    private Shader FindMarkerShader()
    {
        return Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard");
    }

    private Collider CopyCollider(Collider source, GameObject target)
    {
        if (source is BoxCollider sourceBox)
        {
            BoxCollider targetBox = target.AddComponent<BoxCollider>();
            targetBox.center = sourceBox.center;
            targetBox.size = sourceBox.size;
            CopyColliderSettings(sourceBox, targetBox);
            return targetBox;
        }

        if (source is MeshCollider sourceMesh)
        {
            MeshCollider targetMesh = target.AddComponent<MeshCollider>();
            targetMesh.sharedMesh = sourceMesh.sharedMesh;
            targetMesh.convex = sourceMesh.convex;
            CopyColliderSettings(sourceMesh, targetMesh);
            return targetMesh;
        }

        if (source is CapsuleCollider sourceCapsule)
        {
            CapsuleCollider targetCapsule = target.AddComponent<CapsuleCollider>();
            targetCapsule.center = sourceCapsule.center;
            targetCapsule.radius = sourceCapsule.radius;
            targetCapsule.height = sourceCapsule.height;
            targetCapsule.direction = sourceCapsule.direction;
            CopyColliderSettings(sourceCapsule, targetCapsule);
            return targetCapsule;
        }

        return null;
    }

    private void CopyColliderSettings(Collider source, Collider target)
    {
        target.sharedMaterial = source.sharedMaterial;
        target.isTrigger = source.isTrigger;
    }
}
