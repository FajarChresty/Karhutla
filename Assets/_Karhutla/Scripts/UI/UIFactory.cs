using UnityEngine;
using UnityEngine.UI;

namespace Karhutla
{
    /// <summary>
    /// Helper kecil untuk membuat elemen Unity UI (uGUI) lewat kode: Canvas, Panel, Text, dan Bar.
    /// Seluruh UI prototype dibangun dari kode ini saat Play, jadi tidak perlu prefab atau aset gambar.
    /// Font memakai font bawaan Unity (LegacyRuntime), tanpa TextMeshPro.
    /// </summary>
    public static class UIFactory
    {
        private static Font font;
        private static Canvas canvas;

        public static Font DefaultFont
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        /// <summary>Canvas utama (dibuat otomatis saat pertama kali diminta).</summary>
        public static Canvas Canvas
        {
            get
            {
                if (canvas == null) canvas = CreateCanvas();
                return canvas;
            }
        }

        private static Canvas CreateCanvas()
        {
            var go = new GameObject("UI Canvas (dibuat saat Play)");
            var c = go.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 10;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return c;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return go.GetComponent<RectTransform>();
        }

        public static Text CreateText(Transform parent, string name, int fontSize, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = DefaultFont;
            t.fontSize = fontSize;
            t.alignment = anchor;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        /// <summary>Tempatkan RectTransform pada satu titik anchor dengan ukuran tetap.</summary>
        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
        }

        /// <summary>Isi penuh parent dengan margin (kiri, bawah, kanan, atas).</summary>
        public static void Stretch(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Hijau (0) -> kuning -> merah (1).</summary>
        public static Color Gradient(float v01)
        {
            v01 = Mathf.Clamp01(v01);
            Color green = new Color(0.25f, 0.75f, 0.3f);
            Color yellow = new Color(0.95f, 0.8f, 0.2f);
            Color red = new Color(0.85f, 0.2f, 0.15f);
            return v01 < 0.5f ? Color.Lerp(green, yellow, v01 * 2f) : Color.Lerp(yellow, red, (v01 - 0.5f) * 2f);
        }

        /// <summary>Bar sederhana: latar gelap + isi berwarna + teks. Isi diatur lewat anchor (tanpa sprite).</summary>
        public class StatBar
        {
            private RectTransform fill;
            private Image fillImage;
            private Text label;
            public RectTransform Root { get; private set; }

            public static StatBar Create(Transform parent, string name, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
            {
                var bar = new StatBar();
                bar.Root = CreatePanel(parent, name, new Color(0f, 0f, 0f, 0.6f));
                Place(bar.Root, anchor, pivot, position, size);

                bar.fill = CreatePanel(bar.Root, "Fill", Color.green);
                bar.fillImage = bar.fill.GetComponent<Image>();

                bar.label = CreateText(bar.Root, "Label", 20, TextAnchor.MiddleLeft, Color.white);
                Stretch(bar.label.rectTransform, 10f, 0f, 10f, 0f);

                bar.Set(0f, Color.green, "");
                return bar;
            }

            public void Set(float value01, Color color, string text)
            {
                value01 = Mathf.Clamp01(value01);
                fill.anchorMin = new Vector2(0f, 0f);
                fill.anchorMax = new Vector2(value01, 1f);
                fill.offsetMin = new Vector2(2f, 2f);
                fill.offsetMax = new Vector2(-2f, -2f);
                fillImage.color = color;
                label.text = text;
            }
        }
    }
}
