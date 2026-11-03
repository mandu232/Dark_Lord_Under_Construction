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
        [SerializeField] private Sprite buffIcon;
        public Sprite BuffIcon => buffIcon;
        [SerializeField] private bool recruitmentFacility;
        [SerializeField, Min(0)] private int roomCapacityBonus;
        public bool IsRecruitmentFacility => recruitmentFacility;
        public int SummonStacks { get; private set; }
        public bool CanReceiveCorpse => recruitmentFacility && isActiveAndEnabled && !IsRuined && SummonStacks < int.MaxValue;
        public bool TryAddSummonStack() { if (!CanReceiveCorpse) return false; SummonStacks++; return true; }
        public bool TrySpendSummonStack() { if (!recruitmentFacility || !isActiveAndEnabled || IsRuined || SummonStacks <= 0) return false; SummonStacks--; return true; }
        public void RestoreSummonStacks(int value) => SummonStacks = Mathf.Max(0, value);
        public bool TutorialCapacityUnlocked { get; private set; }
        public void RestoreTutorialCapacity(bool value) => TutorialCapacityUnlocked = recruitmentFacility && value;
        public int RoomCapacityBonus => roomCapacityBonus + (TutorialCapacityUnlocked ? 1 : 0);
        [SerializeField, Min(.5f)] private float buffRadius = 6f;
        public float BuffRadius => buffRadius > 0 ? buffRadius : 6f;
        [SerializeField] private MinionTrait requiredTrait;
        [SerializeField, Range(0, 200)] private int attackBonusPercent;
        [SerializeField, Range(0, 100)] private int physicalResistanceBonus, magicResistanceBonus;
        public bool IsBuffFacility => buffFacility;
        public MinionTrait RequiredTrait => requiredTrait;
        public int AttackBonusPercent => attackBonusPercent;
        public int ResistanceBonus(DamageType type) => type == DamageType.Magic ? magicResistanceBonus : physicalResistanceBonus;
        public bool Buffs(DarkLord.Minions.MinionController minion) => buffFacility && isActiveAndEnabled && !IsRuined &&
            minion != null && minion.IsAlive && minion.Health.Faction == Faction.Dungeon &&
            minion.gameObject.scene == gameObject.scene && ((Vector2)(minion.transform.position - transform.position)).sqrMagnitude <= BuffRadius * BuffRadius && (requiredTrait == MinionTrait.None || (minion.Traits & requiredTrait) == requiredTrait);
        public string EffectDescription => recruitmentFacility ? $"소환 스택 {SummonStacks}개 · 시체 1구당 +1\n시체 옆에서 F로 들어 소환소에 가져오세요.\nF: 소환 창 · 스택 1개로 무작위 하수인 즉시 소환" : roomCapacityBonus > 0 ? $"이 방의 하수인 배치 한도 +{roomCapacityBonus}\n여러 숙소의 효과는 합산됩니다. 파괴 시 효과 중단" : !buffFacility ? $"생산  {(resource == CurrencyType.Gold ? "금화" : resource == CurrencyType.Souls ? "혈액" : "혈석")} +{productionPerRound}\n라운드 클리어마다 지급" :
            $"반경 {BuffRadius / DungeonGrid.CellSize:0.#}칸 강화 · {(requiredTrait == MinionTrait.None ? "모든 하수인" : requiredTrait == MinionTrait.Magic ? "마법 특성" : requiredTrait == MinionTrait.Acid ? "산성 특성" : "근접 특성")}\n" +
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
        private FacilityCoverageDisplay coverage;
        public void SetCoverageVisible(bool visible)
        {
            if (!visible || IsRuined || !isActiveAndEnabled)
            { if (coverage != null) coverage.Hide(); return; }
            if (coverage == null)
            {
                var node = new GameObject("Facility effect coverage"); node.transform.SetParent(transform, false);
                coverage = node.AddComponent<FacilityCoverageDisplay>();
            }
            coverage.Show(this, dungeon, Cell);
        }
        public void PlayPlacementEffect()
        {
            if (intactVisual != null)
                (intactVisual.GetComponent<FacilityPlacementBounce>() ?? intactVisual.AddComponent<FacilityPlacementBounce>()).Play();
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
