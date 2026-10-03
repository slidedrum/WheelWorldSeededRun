using UnityEngine;
using UnityEngine.UI;
using UnityObject = UnityEngine.Object;

namespace SeededRun
{
    internal static class SeedWatermark
    {
        private const float ScreenMargin = 20f;
        private const float TextAreaWidth = 600f;
        private const float TextAreaHeight = 100f;
        private const int FontSize = 24;
        private const int OverlaySortingOrder = 1000;

        private static GameObject _overlayObject;
        private static Canvas _overlayCanvas;
        private static Text _watermarkText;
        private static string _displayedText;
        private static bool _creationFailed;

        internal static void Update()
        {
            if (_overlayObject == null && !_creationFailed)
                Create();

            if (_overlayCanvas == null)
                return;

            bool isLoading = LoadingScreen.IsLoading;
            _overlayCanvas.enabled = !isLoading;
            if (!isLoading)
                UpdateText();
        }

        internal static void Destroy()
        {
            if (_overlayObject != null)
                UnityObject.Destroy(_overlayObject);

            _overlayObject = null;
            _overlayCanvas = null;
            _watermarkText = null;
            _displayedText = null;
            _creationFailed = false;
        }

        private static void Create()
        {
            Font font = LoadDefaultFont();
            if (font == null)
            {
                _creationFailed = true;
                Plugin.PluginLog.LogError("Cannot create the seed watermark because Unity's default font is unavailable.");
                return;
            }

            _overlayObject = new GameObject("SeedMod Watermark");
            UnityObject.DontDestroyOnLoad(_overlayObject);

            _overlayCanvas = _overlayObject.AddComponent<Canvas>();
            _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _overlayCanvas.sortingOrder = OverlaySortingOrder;

            CanvasScaler canvasScaler = _overlayObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;

            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(_overlayObject.transform, false);

            _watermarkText = textObject.AddComponent<Text>();
            _watermarkText.font = font;
            _watermarkText.fontSize = FontSize;
            _watermarkText.color = Color.white;
            _watermarkText.alignment = TextAnchor.UpperRight;
            _watermarkText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _watermarkText.verticalOverflow = VerticalWrapMode.Overflow;
            _watermarkText.raycastTarget = false;

            RectTransform textRect = _watermarkText.rectTransform;
            textRect.anchorMin = Vector2.one;
            textRect.anchorMax = Vector2.one;
            textRect.pivot = Vector2.one;
            textRect.anchoredPosition = new Vector2(-ScreenMargin, -ScreenMargin);
            textRect.sizeDelta = new Vector2(TextAreaWidth, TextAreaHeight);

            UpdateText();
            Plugin.PluginLog.LogInfo("Created the SeedMod watermark overlay.");
        }

        private static void UpdateText()
        {
            string text = $"{SeededSaveData.GetDisplaySeed()} - SeedMod {MyPluginInfo.PLUGIN_VERSION}";
            if (_displayedText == text)
                return;

            _displayedText = text;
            _watermarkText.text = text;
        }

        private static Font LoadDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
