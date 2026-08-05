using UnityEngine;
using KingdomEnhanced.Core;

namespace KingdomEnhanced.Shared
{
    /// <summary>
    /// 提供 ModMenu 共用的 GUI 绘制辅助能力。
    /// </summary>
    public static class GuiHelper
    {
        /// <summary>
        /// Unity GL 三角形绘制常量。
        /// </summary>
        public const int GL_TRIANGLES = 4;

        /// <summary>
        /// 绘制分隔线时复用的材质实例。
        /// </summary>
        private static Material _lineMaterial;

        /// <summary>
        /// 滑条范围与重置按钮复用的样式实例。
        /// </summary>
        private static GUIStyle _sliderRangeStyle;

        /// <summary>
        /// 获取用于绘制线段的材质。
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
        /// 以指定颜色和粗细绘制一条屏幕空间直线。
        /// </summary>
        /// <param name="start">线段起点。</param>
        /// <param name="end">线段终点。</param>
        /// <param name="color">线段颜色。</param>
        /// <param name="thickness">线段粗细。</param>
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
        /// 绘制一个按资源键解析后的分组标题。
        /// </summary>
        /// <param name="titleKey">分组标题资源键。</param>
        /// <param name="style">标题样式。</param>
        public static void DrawSection(string titleKey, GUIStyle style)
        {
            GUILayout.Space(18f);
            GUILayout.Label(LocalizationService.Get(titleKey), style);
            GUILayout.Space(6f);
        }

        /// <summary>
        /// 绘制浮点滑条与对应的重置、范围、当前值文本。
        /// </summary>
        /// <param name="label">已完成本地化的标签文本。</param>
        /// <param name="value">当前滑条值。</param>
        /// <param name="min">最小值。</param>
        /// <param name="max">最大值。</param>
        /// <param name="defaultValue">重置时恢复的默认值。</param>
        /// <param name="labelStyle">标签样式。</param>
        /// <param name="dimStyle">弱化文本样式。</param>
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
        /// 绘制整数滑条与对应的重置、范围、当前值文本。
        /// </summary>
        /// <param name="label">已完成本地化的标签文本。</param>
        /// <param name="value">当前滑条值。</param>
        /// <param name="min">最小值。</param>
        /// <param name="max">最大值。</param>
        /// <param name="defaultValue">重置时恢复的默认值。</param>
        /// <param name="labelStyle">标签样式。</param>
        /// <param name="dimStyle">弱化文本样式。</param>
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
        /// 初始化滑条范围和重置按钮共用样式。
        /// </summary>
        /// <param name="baseStyle">作为基底复制的样式。</param>
        private static void EnsureRangeStyle(GUIStyle baseStyle)
        {
            if (_sliderRangeStyle != null) return;
            _sliderRangeStyle = new GUIStyle(baseStyle) { fontSize = 10 };
        }
    }
}
