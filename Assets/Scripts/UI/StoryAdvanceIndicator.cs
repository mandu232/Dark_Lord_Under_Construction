using UnityEngine;

namespace DarkLord.UI
{
    /// <summary>A downward triangle drawn without a texture or font glyph dependency.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class StoryAdvanceIndicator : UnityEngine.UI.MaskableGraphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vertices)
        {
            vertices.Clear();
            var rect = GetPixelAdjustedRect();
            vertices.AddVert(new Vector3(rect.xMin, rect.yMax), color, Vector2.zero);
            vertices.AddVert(new Vector3(rect.xMax, rect.yMax), color, Vector2.zero);
            vertices.AddVert(new Vector3(rect.center.x, rect.yMin), color, Vector2.zero);
            vertices.AddTriangle(0, 1, 2);
        }
    }
}
