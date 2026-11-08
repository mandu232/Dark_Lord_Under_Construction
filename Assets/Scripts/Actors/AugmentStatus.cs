using System;
using UnityEngine;

namespace DarkLord.Actors
{
    [Serializable]
    public sealed class AugmentStatusSaveData
    {
        public float lordMark, trapBrand, empowered;
        public float broodRemaining;
        public int broodHits;
    }

    // Per-actor timers use scaled time, including pause and save restoration.
    [DisallowMultipleComponent]
    public sealed class AugmentStatus : MonoBehaviour
    {
        private float lordUntil, trapUntil, empoweredUntil, broodUntil;
        private int broodHits;
        private SpriteRenderer marker;
        private ActorHealth health;
        private DarkLord.UI.RewardUiSkin skin;
        private void Awake() => health = GetComponent<ActorHealth>();
        private void LateUpdate()
        {
            bool visible = health != null && health.IsAlive && (LordMarked || TrapBranded || Empowered);
            if (marker == null && visible)
            {
                skin = Resources.Load<DarkLord.UI.RewardUiSkin>("WaveRewardSkin");
                var node = new GameObject("Augment status marker"); node.transform.SetParent(transform, false);
                node.transform.localPosition = new Vector3(0, 1.45f, 0); node.transform.localScale = Vector3.one * .25f;
                marker = node.AddComponent<SpriteRenderer>(); marker.sortingOrder = 35;
            }
            if (marker == null) return;
            marker.gameObject.SetActive(visible);
            if (visible && skin != null)
            {
                marker.sprite = skin.Icon(Empowered ? 9 : LordMarked ? 6 : 8);
                if (marker.sprite != null) marker.transform.localScale = Vector3.one * (.45f / Mathf.Max(marker.sprite.bounds.size.x, marker.sprite.bounds.size.y));
            }
        }
        private void OnDisable() { if (marker != null) marker.gameObject.SetActive(false); }
        public bool LordMarked => Time.time < lordUntil;
        public bool TrapBranded => Time.time < trapUntil;
        public bool Empowered => Time.time < empoweredUntil;
        public void MarkLord()
        {
            if (!LordMarked) DarkLord.Gameplay.GameplayEffects.Show(transform, DarkLord.Gameplay.GameplayEffectKind.Curse, transform.position, follow: true);
            lordUntil = Time.time + 6;
        }
        public void BrandTrap() => trapUntil = Time.time + 8;
        public void Empower()
        {
            if (!Empowered) DarkLord.Gameplay.GameplayEffects.Show(transform,DarkLord.Gameplay.GameplayEffectKind.BloodEmpower,transform.position,follow:true,duration:5,scale:.7f);
            empoweredUntil = Time.time + 5;
        }
        public bool RegisterBroodHit()
        {
            if (Time.time >= broodUntil) broodHits = 0;
            broodUntil = Time.time + 4;
            if (++broodHits < 3) return false;
            broodHits = 0; return true;
        }
        public void ConsumeLord() => lordUntil = 0;
        public void ConsumeTrap() => trapUntil = 0;
        public void ConsumeEmpower() => empoweredUntil = 0;
        public AugmentStatusSaveData CaptureSave() => new AugmentStatusSaveData {
            broodHits = Time.time < broodUntil ? broodHits : 0, broodRemaining = Mathf.Max(0, broodUntil - Time.time),
            lordMark = Mathf.Max(0, lordUntil - Time.time), trapBrand = Mathf.Max(0, trapUntil - Time.time), empowered = Mathf.Max(0, empoweredUntil - Time.time) };
        public void RestoreSave(AugmentStatusSaveData data)
        {
            broodHits = Mathf.Clamp(data?.broodHits ?? 0,0,2); broodUntil = Time.time + Mathf.Clamp(data?.broodRemaining ?? 0,0,4);
            lordUntil = Time.time + (data?.lordMark ?? 0);
            trapUntil = Time.time + (data?.trapBrand ?? 0);
            empoweredUntil = Time.time + (data?.empowered ?? 0);
        }
        public static AugmentStatus Get(ActorHealth actor) => actor.GetComponent<AugmentStatus>() ?? actor.gameObject.AddComponent<AugmentStatus>();
    }
}
