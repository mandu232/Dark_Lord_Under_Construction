using System.Collections.Generic;
using DarkLord.Actors;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkLord.Minions
{
    [DisallowMultipleComponent]
    public sealed class InvaderArrow : MonoBehaviour
    {
        private const float Speed = 13f;
        private static Sprite arrowSprite;
        private readonly List<RaycastHit2D> hits = new List<RaycastHit2D>(8);
        private ActorHealth source;
        private LayerMask walls;
        private Vector2 heading;
        private float remaining;
        private int damage;
        private SpriteRenderer visual;

        internal static void Launch(ActorHealth source, LayerMask walls, Vector2 heading, float range, int damage)
        {
            if (source == null || heading.sqrMagnitude < .001f) return;
            var node = new GameObject("Invader arrow");
            SceneManager.MoveGameObjectToScene(node, source.gameObject.scene);
            node.transform.position = source.transform.position;
            var arrow = node.AddComponent<InvaderArrow>();
            arrow.source = source;
            arrow.walls = walls;
            arrow.heading = heading.normalized;
            arrow.remaining = range;
            arrow.damage = damage;
            arrow.visual = node.AddComponent<SpriteRenderer>();
            arrow.visual.sprite = ArrowSprite();
            var shooter = source.GetComponent<SpriteRenderer>();
            if (shooter != null) arrow.visual.sortingLayerID = shooter.sortingLayerID;
            node.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg);
            arrow.SortVisual();
        }

        private void Update()
        {
            if (source == null) { Destroy(gameObject); return; }
            float step = Mathf.Min(remaining, Speed * Time.deltaTime);
            if (step <= 0f) return;

            Vector2 position = transform.position;
            var filter = new ContactFilter2D { useTriggers = false };
            Physics2D.Raycast(position, heading, filter, hits, step);
            ActorHealth victim = null;
            bool blocked = false;
            float closest = float.PositiveInfinity;
            foreach (var hit in hits)
            {
                if (hit.collider.gameObject.scene != gameObject.scene) continue;
                bool wall = (walls.value & (1 << hit.collider.gameObject.layer)) != 0;
                if (hit.distance > closest || hit.distance == closest && !wall) continue;
                var actor = hit.collider.GetComponentInParent<ActorHealth>();
                if (actor == source) continue;
                if (!wall && (actor == null || !actor.isActiveAndEnabled || !actor.IsAlive || actor.Faction == source.Faction))
                    continue;
                blocked = true;
                closest = hit.distance;
                victim = wall ? null : actor;
            }
            if (blocked)
            {
                victim?.TakeDamage(damage, source, DamageType.Physical);
                Destroy(gameObject);
                return;
            }
            remaining -= step;
            transform.position = position + heading * step;
            SortVisual();
            if (remaining <= 0f) Destroy(gameObject);
        }

        private void SortVisual() =>
            visual.sortingOrder = Mathf.Clamp(-Mathf.RoundToInt(transform.position.y * 16) + 8, -30000, 30000);

        private static Sprite ArrowSprite()
        {
            if (arrowSprite != null) return arrowSprite;
            var texture = new Texture2D(12, 3, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var pixels = new Color32[36];
            var feather = new Color32(211, 164, 115, 255);
            var shaft = new Color32(163, 106, 67, 255);
            var tip = new Color32(224, 229, 240, 255);
            pixels[0] = pixels[1] = pixels[24] = pixels[25] = feather;
            pixels[9] = pixels[10] = pixels[33] = pixels[34] = tip;
            for (int i = 0; i < 12; i++) pixels[12 + i] = i < 9 ? shaft : tip;
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            arrowSprite = Sprite.Create(texture, new Rect(0, 0, 12, 3), new Vector2(.85f, .5f), 16);
            return arrowSprite;
        }
    }
}
