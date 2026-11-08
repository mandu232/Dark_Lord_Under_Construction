using System.Collections.Generic;
using DarkLord.Dungeon;
using UnityEngine;
namespace DarkLord.Minions
{
    [DisallowMultipleComponent]
    public sealed class MinionBuffIndicator : MonoBehaviour
    {
        private MinionController controller;
        private SpriteRenderer aura;
        private DarkLord.Gameplay.GameplayEffectLibrary.Clip auraClip;
        private readonly SpriteRenderer[] icons = new SpriteRenderer[3];
        private readonly List<ResourceBuilding> buffs = new List<ResourceBuilding>();
        private float nextRefresh;
        public bool IsVisible => aura != null && aura.enabled;
        public int ActiveBuffCount { get; private set; }
        private void Awake() => controller = GetComponentInParent<MinionController>();
        private void LateUpdate()
        {
            if (controller == null || !controller.isActiveAndEnabled || !controller.IsAlive || controller.IsHeld)
            { Hide(); return; }
            if (Time.time >= nextRefresh) { Refresh(); nextRefresh = Time.time + .1f; }
            if (IsVisible && auraClip != null)
                aura.sprite = auraClip.frames[Mathf.Min(auraClip.frames.Length - 1, (int)(Mathf.Repeat(Time.time / auraClip.duration, 1) * auraClip.frames.Length))];
        }
        public void Refresh()
        {
            if (controller == null || !controller.isActiveAndEnabled || !controller.IsAlive || controller.IsHeld)
            { Hide(); return; }
            controller.GetRoomBonuses(out _,out _,out _,buffs);
            ActiveBuffCount = buffs.Count;
            if (buffs.Count == 0) { Hide(); return; }
            if (aura == null)
            {
                var library = Resources.Load<DarkLord.Gameplay.GameplayEffectLibrary>("GameplayEffects");
                auraClip = library != null ? library.Find(DarkLord.Gameplay.GameplayEffectKind.BloodAura) : null;
                var auraNode = new GameObject("Facility buff aura"); auraNode.transform.SetParent(transform,false);
                aura = auraNode.AddComponent<SpriteRenderer>();
                if (library != null && library.material != null) aura.sharedMaterial = library.material;
                if (auraClip != null && auraClip.frames.Length > 0) aura.sprite = auraClip.frames[0];
                for (int i = 0; i < icons.Length; i++)
                {
                    var node = new GameObject("Facility buff icon " + i); node.transform.SetParent(transform,false);
                    icons[i] = node.AddComponent<SpriteRenderer>();
                }
            }
            int order = Mathf.Clamp(-Mathf.RoundToInt(transform.position.y * 16) + 35,-30000,30000);
            aura.enabled = true; aura.sortingOrder = order;
            int shown = Mathf.Min(icons.Length,buffs.Count);
            for (int i = 0; i < icons.Length; i++)
            {
                var icon = icons[i]; icon.enabled = i < shown && buffs[i].BuffIcon != null;
                if (!icon.enabled) continue;
                icon.sprite = buffs[i].BuffIcon; icon.sortingOrder = order + 1;
                icon.transform.localPosition = new Vector3((i-(shown-1)*.5f)*.34f,1.3f,0);
                float scale = .28f / Mathf.Max(.001f,Mathf.Max(icon.sprite.bounds.size.x,icon.sprite.bounds.size.y));
                icon.transform.localScale = Vector3.one * scale;
            }
        }
        private void Hide()
        {
            ActiveBuffCount=0;
            if (aura == null) return;
            aura.enabled=false;
            foreach(var icon in icons) if(icon != null)icon.enabled=false;
        }
        private void OnDisable() => Hide();
    }
}
