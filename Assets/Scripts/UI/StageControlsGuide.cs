using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class StageControlsGuide
{
    private const string GuideObjectName = "StageControlsGuide";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        CreateForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CreateForScene(scene);
    }

    private static void CreateForScene(Scene scene)
    {
        if (!IsStageScene(scene.name) || GameObject.Find(GuideObjectName) != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject(GuideObjectName);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panelObject = new GameObject("Panel");
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panelObject.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.one;
        panelRect.anchorMax = Vector2.one;
        panelRect.pivot = Vector2.one;
        panelRect.anchoredPosition = new Vector2(-28f, -28f);
        panelRect.sizeDelta = new Vector2(360f, 218f);

        Image panel = panelObject.AddComponent<Image>();
        panel.color = new Color(0.035f, 0.075f, 0.1f, 0.82f);
        panel.raycastTarget = false;

        GameObject textObject = new GameObject("ControlsText");
        textObject.transform.SetParent(panelObject.transform, false);
        RectTransform textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(22f, 16f);
        textRect.offsetMax = new Vector2(-22f, -16f);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset controlsFont = Resources.Load<TMP_FontAsset>(
            "Fonts/x10y12pxDonguriDuel SDF"
        );
        if (controlsFont != null)
        {
            text.font = controlsFont;
        }

        text.text =
            "<b>CONTROLS</b>\n" +
            "W A S D     MOVE\n" +
            "SPACE       ACTION / CARRY\n" +
            "RIGHT CLICK CIRCUIT\n" +
            "Q / E       CAMERA\n" +
            "ESC         MENU";
        text.fontSize = 24f;
        text.color = new Color(0.88f, 0.96f, 1f, 1f);
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
    }

    private static bool IsStageScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || !sceneName.StartsWith("Stage"))
        {
            return false;
        }

        string stageNumber = sceneName.Substring("Stage".Length);
        return int.TryParse(stageNumber, out _);
    }
}
