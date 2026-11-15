using System;
using DarkLord.Gameplay;
using UnityEngine;

namespace DarkLord.Dungeon
{
    // Loops the pixelfx rift on the invaders' entrance cell: torn open during a wave, sealed shut between waves.
    [DisallowMultipleComponent]
    public sealed class EntranceRiftVisual : MonoBehaviour
    {
        private SpriteRenderer body;
        private GameplayEffectLibrary.Clip openClip, closedClip;
        private Func<bool> isOpen;
        private UnityEngine.Rendering.Universal.Light2D glow;

        // False when the library has no rift sheet, so the caller can fall back to the plain crack.
        public static bool TryAttach(GameObject marker, int sortingOrder, Func<bool> isOpen)
        {
            var library = Resources.Load<GameplayEffectLibrary>("GameplayEffects");
            var open = library != null ? library.Find(GameplayEffectKind.Rift) : null;
            if (!Playable(open)) return false;
            var closed = library.Find(GameplayEffectKind.RiftClosed);
            var renderer = marker.GetComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            if (library.material != null) renderer.sharedMaterial = library.material;
            var visual = marker.AddComponent<EntranceRiftVisual>();
            visual.body = renderer; visual.openClip = open; visual.isOpen = isOpen;
            visual.closedClip = Playable(closed) ? closed : open;
            visual.glow = DungeonLighting.AddPoint(marker.transform, "Rift light", Vector3.zero, DungeonLighting.RiftColor, 1f, 3.5f);
            visual.Update();
            return true;
        }

        private static bool Playable(GameplayEffectLibrary.Clip clip) => clip != null && clip.frames != null && clip.frames.Length > 0;

        private void Update()
        {
            bool open = isOpen == null || isOpen();
            var clip = open ? openClip : closedClip;
            if (glow != null) glow.intensity = Mathf.MoveTowards(glow.intensity, open ? 1f : .35f, Time.deltaTime * 2);
            int count = clip.frames.Length;
            body.sprite = clip.frames[Mathf.FloorToInt(Mathf.Repeat(Time.time / clip.duration, 1) * count) % count];
        }
    }
}
