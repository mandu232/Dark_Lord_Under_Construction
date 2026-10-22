using DarkLord.Actors;
using UnityEngine;

namespace DarkLord.Minions
{
    public sealed class MinionRoleMarker : MonoBehaviour
    {
        private Sprite sprite;
        private void Start()
        {
            var actor = GetComponent<MinionController>();
            if (actor == null || actor.Role == MinionRole.BloodSlime) return;
            sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
            bool support = actor.Role == MinionRole.BloodSupport;
            Color color = support ? new Color(.4f, 1, .7f) : new Color(.4f, .8f, 1);
            Mark("Role vertical", new Vector3(0, 1.05f), new Vector3(.1f, .4f, 1), color);
            Mark("Role horizontal", new Vector3(0, 1.05f), new Vector3(.4f, .1f, 1), color);
            if (!support) Mark("Archer sight", new Vector3(0, 1.05f), new Vector3(.16f, .16f, 1), new Color(.08f, .12f, .3f));
        }
        private void Mark(string title, Vector3 position, Vector3 scale, Color color)
        {
            var node = new GameObject(title); node.transform.SetParent(transform, false); node.transform.localPosition = position; node.transform.localScale = scale;
            var renderer = node.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = 32;
        }
        private void OnDestroy() { if (sprite != null) Destroy(sprite); }
    }
}
