using DarkLord.Actors;
using UnityEngine;

namespace DarkLord.Minions
{
    // Dropping a minion on an identical one merges them into a stronger tier that still takes a single room slot.
    public sealed partial class MinionController
    {
        public const int MaxTier = 3;
        private static readonly float[] TierHealth = { 1, 1.7f, 2.6f }, TierAttack = { 1, 1.6f, 2.3f }, TierSize = { 1, 1.15f, 1.3f };
        public int Tier { get; private set; } = 1;
        public float TierAttackScale => TierAttack[Tier - 1];
        public string TierName => Tier + "성";

        public bool CanMergeWith(MinionController other) => other != null && other != this && IsAlive && other.IsAlive &&
            health.Faction == Faction.Dungeon && other.health.Faction == Faction.Dungeon &&
            other.role == role && other.Tier == Tier && Tier < MaxTier;

        public void Promote(MinionController consumed)
        {
            int maximum = Mathf.RoundToInt(health.MaximumHealth * TierHealth[Tier] / TierHealth[Tier - 1]);
            // The stronger veteran's experience survives the merge.
            if (consumed.health.Level > health.Level) health.RestoreExperience(consumed.health.Level, consumed.health.Experience);
            SetTier(Tier + 1);
            health.RestoreHealth(maximum, maximum);
        }

        // Applies the visuals only: saved health already includes the tier.
        public void SetTier(int tier)
        {
            tier = Mathf.Clamp(tier, 1, MaxTier);
            transform.localScale *= TierSize[tier - 1] / TierSize[Tier - 1];
            Tier = tier;
            (GetComponent<MinionTierMarker>() ?? gameObject.AddComponent<MinionTierMarker>()).Show(tier);
        }
    }

    // Gold diamonds over the head, one per tier above the first.
    public sealed class MinionTierMarker : MonoBehaviour
    {
        private static Sprite sprite;
        private Transform root;
        public void Show(int tier)
        {
            if (root != null) { root.gameObject.SetActive(false); Destroy(root.gameObject); }
            if (tier <= 1) return;
            if (sprite == null) sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
            root = new GameObject("Tier").transform;
            root.SetParent(transform, false); root.localPosition = new Vector3(0, 1.3f);
            for (int i = 0; i < tier; i++)
            {
                var pip = new GameObject("Pip").AddComponent<SpriteRenderer>();
                pip.sprite = sprite; pip.color = tier == MinionController.MaxTier ? new Color(1, .82f, .3f) : new Color(.95f, .9f, .75f); pip.sortingOrder = 32;
                pip.transform.SetParent(root, false);
                pip.transform.localPosition = new Vector3((i - (tier - 1) / 2f) * .32f, 0);
                pip.transform.localRotation = Quaternion.Euler(0, 0, 45);
                pip.transform.localScale = new Vector3(.16f, .16f, 1);
            }
        }
    }
}
