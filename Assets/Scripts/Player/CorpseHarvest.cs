using System.Collections.Generic;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Minions;
using DarkLord.Persistence;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DarkLord.Player
{
    [DefaultExecutionOrder(-70), DisallowMultipleComponent]
    public sealed class CorpseHarvest : MonoBehaviour
    {
        public const float PickupDistance = 2.5f, AbsorptionDistance = 2.25f;
        private DungeonGrid dungeon;
        private WaveDirector waves;
        private SpriteRenderer corpseTemplate;
        private ActorHealth health;
        private Transform remains;
        private readonly List<InvaderCorpse> corpses = new List<InvaderCorpse>();
        private InputAction interact, cancel;
        private int consumedFrame = -1;
        public InvaderCorpse Carried { get; private set; }
        public event System.Action<InvaderCorpse> CorpsePickedUp;
        public event System.Action<InvaderCorpse, ResourceBuilding, bool> CorpseDelivered;
        internal InvaderCorpse LeaveCollapseVictim(Vector3 position) => Create(position, InvaderRole.Warrior, corpseTemplate);
        public IReadOnlyList<InvaderCorpse> Corpses => corpses;
        public DungeonGrid Dungeon => dungeon;
        public bool IsDragging => false;
        public bool BlocksGameplayInput => consumedFrame == Time.frameCount;
        public string Message { get; private set; } = "";
        public bool CanUse => (GetComponent<RunEconomy>() == null || !GetComponent<RunEconomy>().BlocksGameplayInput) && isActiveAndEnabled && dungeon != null && dungeon.isActiveAndEnabled && health != null && health.IsAlive && Time.timeScale > 0 &&
            waves != null && !waves.IsFinished && !waves.BlocksTutorialInput && (GetComponent<DungeonExpansion>() == null || !GetComponent<DungeonExpansion>().ExpansionMode) &&
            (GetComponent<MinionSummoning>() == null || !GetComponent<MinionSummoning>().IsOpen) &&
            (GetComponent<AlchemyExchange>() == null || !GetComponent<AlchemyExchange>().IsOpen);
        private void Awake()
        {
            interact = new InputAction("Carry corpse", InputActionType.Button, "<Keyboard>/f");
            cancel = new InputAction("Drop corpse", InputActionType.Button, "<Keyboard>/escape"); health = GetComponent<ActorHealth>();
        }
        private void OnEnable() { interact.Enable(); cancel.Enable(); }
        private void OnDisable() { interact.Disable(); cancel.Disable(); DropCarried(); }
        private void OnDestroy() { interact.Dispose(); cancel.Dispose(); Clear(); }
        public void Configure(DungeonGrid grid, WaveDirector director, SpriteRenderer template)
        { dungeon = grid; waves = director; corpseTemplate = template; EnsureRoot(); }
        private void EnsureRoot()
        {
            if (remains != null) return;
            var node = new GameObject("Invader remains"); node.transform.SetParent(waves.transform, false); remains = node.transform;
        }
        public InvaderCorpse LeaveCorpse(ActorHealth enemy)
        {
            if (enemy == null || enemy.IsAlive || enemy.Faction != Faction.Invader) return null;
            var source = enemy.GetComponentInChildren<SpriteRenderer>(); if (source == null) return null;
            var role = enemy.GetComponent<InvaderTactics>()?.Role ?? InvaderRole.Warrior;
            var corpse = Create(enemy.transform.position, role, source);
            foreach (var sprite in enemy.GetComponentsInChildren<SpriteRenderer>()) sprite.enabled = false;
            return corpse;
        }
        private InvaderCorpse Create(Vector3 position, InvaderRole role, SpriteRenderer source)
        {
            EnsureRoot(); var node = new GameObject("Fallen " + role, typeof(InvaderCorpse)); node.transform.SetParent(remains, true); node.transform.position = position;
            var corpse = node.GetComponent<InvaderCorpse>(); corpse.Configure(source, role); corpses.Add(corpse); return corpse;
        }
        public InvaderCorpse NearbyCorpse
        {
            get
            {
                InvaderCorpse best = null; float distance = PickupDistance;
                foreach (var corpse in corpses)
                {
                    if (corpse == null || corpse.Consumed || corpse == Carried) continue;
                    float next = Vector2.Distance(transform.position, corpse.transform.position);
                    if (next <= distance) { distance = next; best = corpse; }
                }
                return best;
            }
        }
        public bool TryPickup(InvaderCorpse corpse)
        {
            if (!CanUse || Carried != null || corpse == null || corpse.Consumed || !corpses.Contains(corpse) || Vector2.Distance(transform.position, corpse.transform.position) > PickupDistance) return false;
            Carried = corpse; corpse.SetHeld(true); consumedFrame = Time.frameCount;
            Message = "시체 운반 중 · 소환소 가까이 가져가면 흡수 · F: 내려놓기"; FollowCarried(); CorpsePickedUp?.Invoke(corpse); return true;
        }
        public void DropCarried()
        {
            if (Carried == null) return;
            Carried.SetHeld(false); Carried = null; consumedFrame = Time.frameCount;
        }
        public bool TryInteract()
        {
            if (!CanUse) return false;
            if (Carried != null) { DropCarried(); return true; }
            return TryPickup(NearbyCorpse);
        }
        public bool TryDeliver(InvaderCorpse corpse, ResourceBuilding facility)
        {
            if (!CanUse || corpse == null || corpse.Consumed || !corpses.Contains(corpse) || facility == null || !facility.CanReceiveCorpse ||
                Vector2.Distance(corpse.transform.position, facility.transform.position) > AbsorptionDistance || dungeon.BuildingAt(facility.Cell) != facility) return false;
            if (!facility.TryAddSummonStack()) return false;
            bool wasCarried = Carried == corpse;
            corpses.Remove(corpse); if (wasCarried) Carried = null;
            corpse.Absorb(facility); consumedFrame = Time.frameCount; Message = $"시체를 흡수했습니다. 소환 스택 {facility.SummonStacks}개";
            CorpseDelivered?.Invoke(corpse, facility, wasCarried); return true;
        }
        private void FollowCarried()
        {
            if (Carried == null) return;
            Carried.transform.position = transform.position + new Vector3(.55f, -.35f, 0);
        }
        private void Update()
        {
            if (health == null || !health.IsAlive) { DropCarried(); return; }
            if (Time.timeScale == 0) return;
            if (!CanUse) { return; }
            FollowCarried();
            if (Carried != null)
            {
                var facility = dungeon.NearestRecruitmentFacility(Carried.transform.position);
                if (facility != null && TryDeliver(Carried, facility)) return;
            }
            if (cancel.WasPressedThisFrame() && (Carried != null)) { DropCarried(); consumedFrame = Time.frameCount; return; }
            if (interact.WasPressedThisFrame() && TryInteract()) return;
        }
        private void LateUpdate() { if (Time.timeScale > 0) FollowCarried(); }
        public CorpseSaveData[] Capture() => corpses.Where(c => c != null && !c.Consumed).Select(c => new CorpseSaveData { position=c.transform.position, role=c.Role, carried=c==Carried }).ToArray();
        public void Restore(CorpseSaveData[] saved)
        {
            Clear(); EnsureRoot();
            if (saved == null || corpseTemplate == null) return;
            foreach (var data in saved) { var corpse = Create(data.position, data.role, corpseTemplate); if (data.carried) { Carried = corpse; corpse.SetHeld(true); } }
            FollowCarried();
        }
        private void Clear()
        {
            Carried = null; corpses.Clear();
            if (remains != null) { remains.gameObject.SetActive(false); Destroy(remains.gameObject); } remains = null;
        }
    }
}
