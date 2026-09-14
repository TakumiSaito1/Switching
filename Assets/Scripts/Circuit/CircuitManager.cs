using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class CircuitManager : MonoBehaviour
{
    public static CircuitManager Instance;

    public List<CircuitNode> allNodes = new List<CircuitNode>();

    [Header("Power Wave")]
    [SerializeField, Min(0f)] private float stepDelay = 0.055f;

    private Coroutine powerWaveCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Multiple CircuitManager instances found. Keeping the first one.", this);
            enabled = false;
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void RecalculatePower()
    {
        foreach (var node in allNodes)
        {
            if (node != null)
            {
                node.ConnectToNeighbors();
            }
        }

        Dictionary<CircuitNode, bool> previousPowerStates = new Dictionary<CircuitNode, bool>();
        foreach (var node in allNodes)
        {
            if (node != null)
            {
                previousPowerStates[node] = node.isPowered;
            }
        }

        Queue<CircuitNode> checkQueue = new Queue<CircuitNode>();
        HashSet<CircuitNode> poweredNodes = new HashSet<CircuitNode>();
        Dictionary<CircuitNode, int> powerSteps = new Dictionary<CircuitNode, int>();

        foreach (var node in allNodes)
        {
            if (node != null && IsPowerSource(node))
            {
                checkQueue.Enqueue(node);
                poweredNodes.Add(node);
                powerSteps[node] = 0;
            }
        }

        while (checkQueue.Count > 0)
        {
            CircuitNode current = checkQueue.Dequeue();

            foreach (var neighbor in current.connectedNodes)
            {
                if (neighbor == null || !CanReceivePower(neighbor) || poweredNodes.Contains(neighbor))
                {
                    continue;
                }

                poweredNodes.Add(neighbor);
                powerSteps[neighbor] = powerSteps[current] + 1;
                checkQueue.Enqueue(neighbor);
            }
        }

        if (powerWaveCoroutine != null)
        {
            StopCoroutine(powerWaveCoroutine);
            powerWaveCoroutine = null;
        }

        SortedDictionary<int, List<CircuitNode>> nodesTurningOn = new SortedDictionary<int, List<CircuitNode>>();
        foreach (var node in allNodes)
        {
            if (node == null)
            {
                continue;
            }

            bool wasPowered = previousPowerStates.TryGetValue(node, out bool previousPower) && previousPower;
            bool shouldBePowered = poweredNodes.Contains(node);

            if (wasPowered && !shouldBePowered)
            {
                node.OnPowerChanged(false);
            }
            else if (!wasPowered && shouldBePowered)
            {
                int step = powerSteps.TryGetValue(node, out int value) ? value : 0;
                if (!nodesTurningOn.TryGetValue(step, out List<CircuitNode> nodesAtStep))
                {
                    nodesAtStep = new List<CircuitNode>();
                    nodesTurningOn.Add(step, nodesAtStep);
                }

                nodesAtStep.Add(node);
            }
        }

        if (nodesTurningOn.Count > 0)
        {
            GameSfx.Play("sfx_circuit_power_lowpoly", 0.7f);
            powerWaveCoroutine = StartCoroutine(PlayPowerWave(nodesTurningOn));
        }
    }

    private IEnumerator PlayPowerWave(SortedDictionary<int, List<CircuitNode>> nodesByStep)
    {
        int previousStep = 0;
        foreach (KeyValuePair<int, List<CircuitNode>> entry in nodesByStep)
        {
            int stepDifference = entry.Key - previousStep;
            if (stepDifference > 0 && stepDelay > 0f)
            {
                yield return new WaitForSeconds(stepDelay * stepDifference);
            }

            foreach (CircuitNode node in entry.Value)
            {
                if (node != null)
                {
                    node.OnPowerChanged(true);
                }
            }

            previousStep = entry.Key;
        }

        powerWaveCoroutine = null;
    }

    private bool IsPowerSource(CircuitNode node)
    {
        return node.nodeType == CircuitNode.NodeType.Switch && node.isSwitchOn;
    }

    private bool CanReceivePower(CircuitNode node)
    {
        return node.nodeType != CircuitNode.NodeType.Switch || node.isSwitchOn;
    }
}

public sealed class PowerActivationEffect : MonoBehaviour
{
    private const float Lifetime = 0.45f;
    private static readonly Color EffectColor = new Color(0.25f, 0.9f, 1f, 1f);

    private float age;
    private Material effectMaterial;
    private Light effectLight;

    public static void Play(Vector3 position)
    {
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = "PowerActivationFlash";
        effect.transform.position = position;
        effect.transform.localScale = Vector3.one * 0.15f;

        Collider effectCollider = effect.GetComponent<Collider>();
        if (effectCollider != null)
        {
            Destroy(effectCollider);
        }

        PowerActivationEffect controller = effect.AddComponent<PowerActivationEffect>();
        controller.Setup(effect.GetComponent<Renderer>());
    }

    private void Setup(Renderer effectRenderer)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard");

        effectMaterial = new Material(shader);
        SetMaterialColor(EffectColor);
        effectRenderer.material = effectMaterial;

        effectLight = gameObject.AddComponent<Light>();
        effectLight.type = LightType.Point;
        effectLight.color = EffectColor;
        effectLight.range = 4f;
        effectLight.intensity = 2.2f;
        effectLight.shadows = LightShadows.None;
    }

    private void Update()
    {
        age += Time.deltaTime;
        float progress = Mathf.Clamp01(age / Lifetime);
        float visibility = 1f - progress;

        transform.localScale = Vector3.one * Mathf.Lerp(0.15f, 1.25f, progress);
        SetMaterialColor(new Color(EffectColor.r, EffectColor.g, EffectColor.b, visibility));

        if (effectLight != null)
        {
            effectLight.intensity = 2.2f * visibility;
        }

        if (progress >= 1f)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (effectMaterial != null)
        {
            Destroy(effectMaterial);
        }
    }

    private void SetMaterialColor(Color color)
    {
        if (effectMaterial == null)
        {
            return;
        }

        effectMaterial.color = color;
        if (effectMaterial.HasProperty("_BaseColor"))
        {
            effectMaterial.SetColor("_BaseColor", color);
        }

        if (effectMaterial.HasProperty("_EmissionColor"))
        {
            effectMaterial.EnableKeyword("_EMISSION");
            effectMaterial.SetColor("_EmissionColor", EffectColor * (2f * color.a));
        }
    }
}
