using UnityEngine;
using KingdomEnhanced.Core;

namespace KingdomEnhanced.Shared
{
    /// <summary>
    /// Provides shared GUI drawing helpers used by the ModMenu.
    /// </summary>
    public static class GuiHelper
    {
        /// <summary>
        /// Unity GL triangle drawing constant.
        /// </summary>
        public const int GL_TRIANGLES = 4;

        /// <summary>
        /// Reusable material instance when drawing separator lines.
        /// </summary>
        private static Material _lineMaterial;

        /// <summary>
        /// Reusable style instance for the slider range text and reset button.
        /// </summary>
        private static GUIStyle _sliderRangeStyle;

        /// <summary>
        /// Gets the material used to draw line segments.
        /// </summary>
        private static Material LineMaterial
        {
            get
            {
                if (_lineMaterial != null) return _lineMaterial;
                var shader = Shader.Find("UI/Default");
                if (shader == null) return null;
                _lineMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                _lineMaterial.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                _lineMaterial.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                _lineMaterial.SetInt("_ZWrite", 0);
                _lineMaterial.SetInt("_Cull",   (int)UnityEngine.Rendering.CullMode.Off);
                _lineMaterial.enableInstancing = false;
                return _lineMaterial;
            }
        }

        /// <summary>
        /// Draws a screen-space straight line with the given color and thickness.
        /// </summary>
        /// <param name="start">Start point of the segment.</param>
        /// <param name="end">End point of the segment.</param>
        /// <param name="color">Color of the segment.</param>
        /// <param name="thickness">Thickness of the segment.</param>
        public static void DrawLine(Vector2 start, Vector2 end, Color color, float thickness)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (LineMaterial == null) return;

            LineMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadPixelMatrix();
            GL.Begin(GL_TRIANGLES);
            GL.Color(color);

            Vector2 dir    = (end - start).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x);
            Vector2 offset = normal * (thickness * 0.5f);

            GL.Vertex3(start.x + offset.x, start.y + offset.y, 0);
            GL.Vertex3(start.x - offset.x, start.y - offset.y, 0);
            GL.Vertex3(end.x   - offset.x, end.y   - offset.y, 0);
            GL.Vertex3(start.x + offset.x, start.y + offset.y, 0);
            GL.Vertex3(end.x   - offset.x, end.y   - offset.y, 0);
            GL.Vertex3(end.x   + offset.x, end.y   + offset.y, 0);

            GL.End();
            GL.PopMatrix();
        }

        /// <summary>
        /// Draws a section title resolved from a localization key.
        /// </summary>
        /// <param name="titleKey">Localization key of the section title.</param>
        /// <param name="style">Style of the title.</param>
        public static void DrawSection(string titleKey, GUIStyle style)
        {
            GUILayout.Space(18f);
            GUILayout.Label(LocalizationService.Get(titleKey), style);
            GUILayout.Space(6f);
        }

        /// <summary>
        /// Draws a float slider with reset, range, and current value text.
        /// </summary>
        /// <param name="label">Label text, already localized.</param>
        /// <param name="value">Current slider value.</param>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        /// <param name="defaultValue">Default value restored on reset.</param>
        /// <param name="labelStyle">Style of the label.</param>
        /// <param name="dimStyle">Dimmed text style.</param>
        public static void DrawSlider(string label, ref float value, float min, float max, float defaultValue,
            GUIStyle labelStyle, GUIStyle dimStyle)
        {
            EnsureRangeStyle(dimStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(LocalizationService.Get("common.button.reset"), _sliderRangeStyle, GUILayout.Width(52))) value = defaultValue;
            GUILayout.Label(LocalizationService.Format("common.value.multiplier", value.ToString("F1")), dimStyle, GUILayout.Width(42));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(min.ToString("F1"), _sliderRangeStyle, GUILayout.Width(30));
            value = GUILayout.HorizontalSlider(value, min, max);
            GUILayout.Label(max.ToString("F1"), _sliderRangeStyle, GUILayout.Width(30));
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
        }

        /// <summary>
        /// Draws an integer slider with reset, range, and current value text.
        /// </summary>
        /// <param name="label">Label text, already localized.</param>
        /// <param name="value">Current slider value.</param>
        /// <param name="min">Minimum value.</param>
        /// <param name="max">Maximum value.</param>
        /// <param name="defaultValue">Default value restored on reset.</param>
        /// <param name="labelStyle">Style of the label.</param>
        /// <param name="dimStyle">Dimmed text style.</param>
        public static void DrawIntSlider(string label, ref int value, int min, int max, int defaultValue,
            GUIStyle labelStyle, GUIStyle dimStyle)
        {
            EnsureRangeStyle(dimStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(LocalizationService.Get("common.button.reset"), _sliderRangeStyle, GUILayout.Width(52))) value = defaultValue;
            GUILayout.Label(value <= 0 ? LocalizationService.Get("common.value.default") : value.ToString(), dimStyle, GUILayout.Width(52));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label(min.ToString(), _sliderRangeStyle, GUILayout.Width(30));
            float f = GUILayout.HorizontalSlider(value, min, max);
            value = Mathf.RoundToInt(f);
            GUILayout.Label(max.ToString(), _sliderRangeStyle, GUILayout.Width(30));
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);
        }

        /// <summary>
        /// Initializes the shared slider range and reset button style.
        /// </summary>
        /// <param name="baseStyle">Base style copied as the source.</param>
        private static void EnsureRangeStyle(GUIStyle baseStyle)
        {
            if (_sliderRangeStyle != null) return;
            _sliderRangeStyle = new GUIStyle(baseStyle) { fontSize = 10 };
        }
    }
}
