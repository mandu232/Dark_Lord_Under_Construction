using UnityEngine;

namespace DarkLord.UI
{
    // A vector ring avoids additional textures and scales with the HUD canvas.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class InvasionRingGraphic : UnityEngine.UI.Graphic
    {
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            const int segments = 64;
            float outer = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * .5f;
            float inner = Mathf.Max(0, outer - 3);
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                mesh.AddVert(direction * outer, color, Vector2.zero);
                mesh.AddVert(direction * inner, color, Vector2.zero);
                if (i == segments) break;
                int index = i * 2;
                mesh.AddTriangle(index, index + 2, index + 1);
                mesh.AddTriangle(index + 1, index + 2, index + 3);
            }
        }
    }
}
