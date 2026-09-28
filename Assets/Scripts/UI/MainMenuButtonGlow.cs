using System.Collections.Generic;
using UnityEngine;

namespace DarkLord.UI
{
    // Soft, local UI halo. The original sprite is drawn last, preserving its pixel edges.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.UI.Image))]
    public sealed class MainMenuButtonGlow : UnityEngine.UI.BaseMeshEffect
    {
        [SerializeField] private Color glowColor = new Color(1f, 0.3f, 0.25f, 1f);
        [SerializeField, Range(0, 0.3f)] private float idleStrength = 0.004f;
        [SerializeField, Range(0, 0.3f)] private float selectedStrength = 0.012f;
        [SerializeField, Min(0)] private float radius = 6f;
        private bool selected;
        private readonly List<UIVertex> source = new List<UIVertex>(6);
        private readonly List<UIVertex> output = new List<UIVertex>(390);
        private static readonly Vector2[] Directions = CreateDirections();

        public void SetSelected(bool value)
        {
            if (selected == value) return;
            selected = value;
            if (graphic != null) graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(UnityEngine.UI.VertexHelper vertices)
        {
            if (!IsActive() || radius <= 0) return;
            float strength = selected ? selectedStrength : idleStrength;
            if (strength <= 0) return;
            source.Clear();
            output.Clear();
            vertices.GetUIVertexStream(source);
            // Four overlapping rings give a soft falloff without a full-screen post effect.
            // Rebuilt only when the sprite or selection changes; no extra Update loop.
            for (int ring = 4; ring >= 1; ring--)
            {
                float distance = radius * ring / 4f;
                float opacity = strength * (5 - ring) / 4f;
                foreach (var direction in Directions)
                {
                    foreach (var original in source)
                    {
                        var vertex = original;
                        vertex.position += new Vector3(direction.x * distance, direction.y * distance);
                        var color = glowColor;
                        color.a *= opacity * original.color.a / 255f;
                        vertex.color = color;
                        output.Add(vertex);
                    }
                }
            }
            output.AddRange(source);
            vertices.Clear();
            vertices.AddUIVertexTriangleStream(output);
        }

        private static Vector2[] CreateDirections()
        {
            var directions = new Vector2[16];
            for (int i = 0; i < directions.Length; i++)
            {
                float angle = i * Mathf.PI * 2 / directions.Length;
                directions[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
            return directions;
        }
    }
}
