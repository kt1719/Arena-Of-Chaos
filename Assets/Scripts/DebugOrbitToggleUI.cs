using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DebugOrbitToggleUI : MonoBehaviour
{
    public static DebugOrbitToggleUI Instance => EnsureExists();

    public event Action<bool> OnToggleChanged;

    public bool IsEnabled => _toggle != null && _toggle.isOn;

    private static DebugOrbitToggleUI _instance;
    private Toggle _toggle;

    public static DebugOrbitToggleUI EnsureExists()
    {
        if (_instance != null)
        {
            return _instance;
        }

        GameObject root = new GameObject("DebugOrbitToggleUI");
        _instance = root.AddComponent<DebugOrbitToggleUI>();
        DontDestroyOnLoad(root);
        _instance.BuildUI(root.transform);
        return _instance;
    }

    public static bool TryGetInstance(out DebugOrbitToggleUI instance)
    {
        instance = _instance;
        return instance != null;
    }

    private void BuildUI(Transform parent)
    {
        EnsureEventSystem();
        Canvas canvas = CreateCanvas(parent);
        CreateToggle(canvas.transform);
    }

    private static void EnsureEventSystem()
    {
        EventSystem existingEventSystem = FindFirstObjectByType<EventSystem>();
        if (existingEventSystem != null)
        {
            EnsureInputModule(existingEventSystem.gameObject);
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        EnsureInputModule(eventSystemObject);
        DontDestroyOnLoad(eventSystemObject);
    }

    private static void EnsureInputModule(GameObject eventSystemObject)
    {
        Type inputSystemModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModuleType != null)
        {
            BaseInputModule existingInputModule = eventSystemObject.GetComponent<BaseInputModule>();
            if (existingInputModule == null || existingInputModule.GetType() != inputSystemModuleType)
            {
                if (existingInputModule != null)
                {
                    Destroy(existingInputModule);
                }

                eventSystemObject.AddComponent(inputSystemModuleType);
            }

            return;
        }

        if (eventSystemObject.GetComponent<StandaloneInputModule>() == null)
        {
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }

    private Canvas CreateCanvas(Transform parent)
    {
        GameObject canvasObj = new GameObject("Canvas");
        canvasObj.transform.SetParent(parent, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4000;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasObj.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private void CreateToggle(Transform canvasTransform)
    {
        GameObject toggleRoot = new GameObject("DebugOrbitToggle");
        toggleRoot.transform.SetParent(canvasTransform, false);

        RectTransform rootRect = toggleRoot.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = Vector2.zero;
        rootRect.sizeDelta = new Vector2(360f, 62f);

        Image panelBackground = toggleRoot.AddComponent<Image>();
        panelBackground.color = new Color(0f, 0f, 0f, 0.7f);

        HorizontalLayoutGroup layout = toggleRoot.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childControlHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
        layout.childAlignment = TextAnchor.MiddleLeft;

        CreateToggleControl(toggleRoot.transform);
        CreateLabel(toggleRoot.transform);
    }

    private void CreateToggleControl(Transform parent)
    {
        GameObject toggleObj = new GameObject("Toggle");
        toggleObj.transform.SetParent(parent, false);

        RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
        toggleRect.sizeDelta = new Vector2(32f, 32f);

        LayoutElement toggleLayout = toggleObj.AddComponent<LayoutElement>();
        toggleLayout.preferredWidth = 32f;
        toggleLayout.preferredHeight = 32f;

        Image background = toggleObj.AddComponent<Image>();
        background.color = new Color(1f, 1f, 1f, 0.95f);

        _toggle = toggleObj.AddComponent<Toggle>();

        GameObject checkmarkObj = new GameObject("Checkmark");
        checkmarkObj.transform.SetParent(toggleObj.transform, false);

        RectTransform checkmarkRect = checkmarkObj.AddComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0.2f, 0.2f);
        checkmarkRect.anchorMax = new Vector2(0.8f, 0.8f);
        checkmarkRect.offsetMin = Vector2.zero;
        checkmarkRect.offsetMax = Vector2.zero;

        Image checkmarkImage = checkmarkObj.AddComponent<Image>();
        checkmarkImage.color = new Color(0.15f, 0.85f, 0.25f, 1f);

        _toggle.targetGraphic = background;
        _toggle.graphic = checkmarkImage;
        _toggle.isOn = false;
        _toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    private static void CreateLabel(Transform parent)
    {
        GameObject labelObj = new GameObject("Label");
        labelObj.transform.SetParent(parent, false);

        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(280f, 40f);

        LayoutElement labelLayout = labelObj.AddComponent<LayoutElement>();
        labelLayout.preferredWidth = 280f;
        labelLayout.preferredHeight = 40f;

        Text label = labelObj.AddComponent<Text>();
        label.text = "Debug Orbit (Local)";
        label.alignment = TextAnchor.MiddleLeft;
        label.font = GetSafeBuiltinFont();
        label.fontSize = 20;
        label.fontStyle = FontStyle.Bold;
        label.color = new Color(1f, 0.95f, 0.4f, 1f);
    }

    private static Font GetSafeBuiltinFont()
    {
        try
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }
        catch
        {
            return null;
        }
    }

    private void OnToggleValueChanged(bool isEnabled)
    {
        OnToggleChanged?.Invoke(isEnabled);
    }

    private void OnDestroy()
    {
        if (_toggle != null)
        {
            _toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }

        if (_instance == this)
        {
            _instance = null;
        }
    }
}
