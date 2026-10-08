using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Player;
using UnityEngine;

namespace DarkLord.Dungeon
{
    [DisallowMultipleComponent, RequireComponent(typeof(ActorHealth))]
    public sealed class ResourceBuilding : MonoBehaviour
    {
        [SerializeField] private string displayName = "생산 건물";
        [SerializeField] private CurrencyType resource;
        [SerializeField, Min(1)] private int productionPerRound = 35;
        [SerializeField, Min(0)] private int constructionCost = 80;
        [SerializeField, Min(0)] private int repairCost = 40;
        [SerializeField] private GameObject intactVisual, ruinsVisual;
        private ActorHealth health;
        private DungeonGrid dungeon;
        private readonly List<ActorHealth> defenders = new List<ActorHealth>();
        private SpriteRenderer[] intactRenderers;
        private Color[] originalColors;
        public ActorHealth Health => health;
        public string DisplayName => displayName;
        public CurrencyType Resource => resource;
        public int ProductionPerRound => productionPerRound;
        public int ConstructionCost => constructionCost;
        public int RepairCost => repairCost;
        public bool IsRuined => !health.IsAlive;
        public int LastPaidRound { get; private set; }
        public Vector2Int Cell { get; private set; }
        public GameObject Design { get; private set; }
        public bool IsProtected
        {
            get
            {
                if (dungeon == null) return false;
                var room = dungeon.Construction.RoomAt(Cell);
                dungeon.GetRoomDefenders(room, defenders);
                return defenders.Count > 0;
            }
        }
        private void Awake()
        {
            health = GetComponent<ActorHealth>();
            health.DamageFilter = CanReceiveDamage;
            health.Died += OnDestroyed;
            health.Damaged += OnDamaged;
            intactRenderers = intactVisual != null ? intactVisual.GetComponentsInChildren<SpriteRenderer>() : new SpriteRenderer[0];
            originalColors = new Color[intactRenderers.Length];
            for (int i = 0; i < intactRenderers.Length; i++) originalColors[i] = intactRenderers[i].color;
            RefreshVisuals();
        }
        public void Bind(DungeonGrid grid, Vector2Int cell, GameObject design)
        { dungeon = grid; Cell = cell; Design = design; }
        private bool CanReceiveDamage(ActorHealth source) => source == null || source.Faction != Faction.Invader || !IsProtected;
        private void OnDestroyed() => RefreshVisuals();
        private void OnDamaged(ActorHealth source) => RefreshVisuals();
        private void RefreshVisuals()
        {
            if (intactVisual != null) intactVisual.SetActive(!IsRuined);
            if (ruinsVisual != null) ruinsVisual.SetActive(IsRuined);
            for (int i = 0; i < intactRenderers.Length; i++)
                intactRenderers[i].color = Color.Lerp(new Color(.5f,.2f,.2f,1), originalColors[i], (float)health.CurrentHealth / health.MaximumHealth);
            // Remains keep their floor reservation but stop obstructing actors.
            foreach (var collider in GetComponents<Collider2D>()) collider.enabled = !IsRuined;
        }
        public bool CanRepair(CurrencyWallet wallet, out string reason)
        {
            reason = "";
            if (!isActiveAndEnabled || Time.timeScale <= 0) { reason = "지금은 수리할 수 없습니다."; return false; }
            if (!IsRuined) { reason = "파괴된 건물만 수리할 수 있습니다."; return false; }
            if (dungeon != null && dungeon.HasInvaderInRoom(Cell)) { reason = "방에 용사가 있어 수리할 수 없습니다."; return false; }
            if (wallet == null || !wallet.CanAfford(CurrencyType.Gold, repairCost)) { reason = $"금화가 부족합니다. 수리 비용: {repairCost}"; return false; }
            reason = "수리하면 체력과 생산이 복구됩니다.";
            return true;
        }
        public bool TryRepair(CurrencyWallet wallet)
        {
            if (!CanRepair(wallet, out _) || !wallet.TrySpend(CurrencyType.Gold, repairCost)) return false;
            health.RestoreHealth(health.MaximumHealth, health.MaximumHealth);
            RefreshVisuals();
            return true;
        }
        public int PayRound(CurrencyWallet wallet, int round)
        {
            if (wallet == null || IsRuined || round <= LastPaidRound || round <= 0) return 0;
            LastPaidRound = round;
            wallet.Add(resource, productionPerRound);
            return productionPerRound;
        }
        public void RestoreState(int maximumHealth, int currentHealth, int lastPaidRound)
        {
            health.RestoreHealth(maximumHealth, currentHealth);
            LastPaidRound = lastPaidRound;
            RefreshVisuals();
        }
        private void OnDestroy()
        {
            if (health == null) return;
            health.Died -= OnDestroyed;
            health.Damaged -= OnDamaged;
            health.DamageFilter = null;
        }
    }
}
