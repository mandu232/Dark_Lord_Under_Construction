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
        [Header("Room aura (None keeps resource production)")]
        [SerializeField] private bool buffFacility;
        [SerializeField] private bool recruitmentFacility;
        [SerializeField, Min(0)] private int roomCapacityBonus;
        public bool IsRecruitmentFacility => recruitmentFacility;
        public int RoomCapacityBonus => roomCapacityBonus;
        [SerializeField] private MinionTrait requiredTrait;
        [SerializeField, Range(0, 200)] private int attackBonusPercent;
        [SerializeField, Range(0, 100)] private int physicalResistanceBonus, magicResistanceBonus;
        public bool IsBuffFacility => buffFacility;
        public MinionTrait RequiredTrait => requiredTrait;
        public int AttackBonusPercent => attackBonusPercent;
        public int ResistanceBonus(DamageType type) => type == DamageType.Magic ? magicResistanceBonus : physicalResistanceBonus;
        public bool Buffs(DarkLord.Minions.MinionController minion) => buffFacility && isActiveAndEnabled && !IsRuined &&
            minion != null && (requiredTrait == MinionTrait.None || (minion.Traits & requiredTrait) == requiredTrait);
        public string EffectDescription => recruitmentFacility ? "권속 소환소 · 준비 단계에 시설 3유닛 이내에서 마나 20으로 소환\n시설 근처에서 F: 모집 창 · 하수인 선택 후 빈 바닥 클릭: 배치 · 우클릭/Esc: 취소" : roomCapacityBonus > 0 ? $"이 방의 하수인 배치 한도 +{roomCapacityBonus}\n여러 숙소의 효과는 합산됩니다. 파괴 시 효과 중단" : !buffFacility ? $"생산  {(resource == CurrencyType.Gold ? "금화" : resource == CurrencyType.Souls ? "혈액" : "혈석")} +{productionPerRound}\n라운드 클리어마다 지급" :
            $"방 전체 강화 · {(requiredTrait == MinionTrait.None ? "모든 하수인" : requiredTrait == MinionTrait.Magic ? "마법 특성" : requiredTrait == MinionTrait.Acid ? "산성 특성" : "근접 특성")}\n" +
            (attackBonusPercent > 0 ? $"공격력 +{attackBonusPercent}%\n" : "") +
            (physicalResistanceBonus > 0 ? $"물리 내성 +{physicalResistanceBonus}%p\n" : "") +
            (magicResistanceBonus > 0 ? $"마법 내성 +{magicResistanceBonus}%p\n" : "");
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
            reason = "수리하면 체력과 시설 효과가 복구됩니다.";
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
            if (buffFacility || recruitmentFacility || roomCapacityBonus > 0 || wallet == null || IsRuined || round <= LastPaidRound || round <= 0) return 0;
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
