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
        private TopDownMotor motor;
        private CorpseDragDust dust;
        private Vector3 lastCarrierPosition, dragVelocity;
        private Vector2 dragDirection;
        private const float DragDistance = .85f;
        private const float MaxDragDistance = 1.3f;
        private const float DragSmoothTime = .10f;
        private const float TeleportDistance = 2f;
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
        internal bool WorkersFlee => waves != null && waves.InCombat;
        internal bool WorkersCanCollect => isActiveAndEnabled && waves != null && !waves.IsFinished &&
            !waves.InCombat && (waves.Tutorial == null || !waves.Tutorial.Active);
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
            motor = GetComponent<TopDownMotor>();
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
            DeathLoot.Scatter(enemy.transform.position, transform, remains != null ? remains : waves.transform, waves);
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
                    if (corpse == null || corpse.Consumed || corpse == Carried || corpse.AssignedWorker != null) continue;
                    float next = Vector2.Distance(transform.position, corpse.transform.position);
                    if (next <= distance) { distance = next; best = corpse; }
                }
                return best;
            }
        }
        public bool TryPickup(InvaderCorpse corpse)
        {
            if (!CanUse || Carried != null || corpse == null || corpse.Consumed || corpse.AssignedWorker != null || !corpses.Contains(corpse) || Vector2.Distance(transform.position, corpse.transform.position) > PickupDistance) return false;
            Carried = corpse; corpse.SetHeld(true); consumedFrame = Time.frameCount;
            BeginDrag();
            Message = "시체 운반 중 · 소환소 가까이 가져가면 흡수 · F: 내려놓기"; CorpsePickedUp?.Invoke(corpse); return true;
        }
        public void DropCarried()
        {
            if (Carried == null) return;
            Carried.SetHeld(false); Carried = null; dragVelocity = Vector3.zero; consumedFrame = Time.frameCount;
        }
        public bool TryInteract()
        {
            if (!CanUse) return false;
            if (Carried != null) { DropCarried(); return true; }
            return TryPickup(NearbyCorpse);
        }
        public bool TryDeliver(InvaderCorpse corpse, ResourceBuilding facility)
        {
            return CanUse && corpse != null && corpse.AssignedWorker == null && Deliver(corpse, facility, corpse.transform.position);
        }
        internal bool TryDeliverFromWorker(InvaderCorpse corpse, ResourceBuilding facility, CorpseWorker worker)
        {
            return WorkersCanCollect && corpse != null && corpse.AssignedWorker == worker && Deliver(corpse, facility, worker.transform.position);
        }
        private bool Deliver(InvaderCorpse corpse, ResourceBuilding facility, Vector3 deliveryPosition)
        {
            if (corpse.Consumed || !corpses.Contains(corpse) || facility == null || !facility.CanReceiveCorpse ||
                Vector2.Distance(deliveryPosition, facility.transform.position) > AbsorptionDistance || dungeon.BuildingAt(facility.Cell) != facility) return false;
            if (!facility.TryAddSummonStack()) return false;
            bool wasCarried = Carried == corpse;
            corpses.Remove(corpse); if (wasCarried) { Carried = null; dragVelocity = Vector3.zero; }
            if (corpse.AssignedWorker == null) consumedFrame = Time.frameCount;
            corpse.AssignedWorker = null;
            corpse.Absorb(facility);
            Message = $"시체를 흡수했습니다. 소환 스택 {facility.SummonStacks}개";
            CorpseDelivered?.Invoke(corpse, facility, wasCarried); return true;
        }
        private void BeginDrag()
        {
            if (Carried == null) return;
            if (dust == null) dust = GetComponent<CorpseDragDust>() ?? gameObject.AddComponent<CorpseDragDust>();
            dust.Begin(Carried.Visual);
            lastCarrierPosition = transform.position;
            dragVelocity = Vector3.zero;
            Vector2 offset = (Vector2)(transform.position - Carried.transform.position);
            dragDirection = offset.sqrMagnitude > .0225f ? offset.normalized : motor != null ? motor.Facing : Vector2.down;
            if (dragDirection.sqrMagnitude < .001f) dragDirection = Vector2.down;
            if (offset.sqrMagnitude < .0225f)
                Carried.transform.position = transform.position - (Vector3)(dragDirection * DragDistance);
            else if (offset.sqrMagnitude > MaxDragDistance * MaxDragDistance)
                Carried.transform.position = transform.position - (Vector3)(dragDirection * MaxDragDistance);
        }
        private bool ReconcileTeleport()
        {
            if (Carried == null) return false;
            Vector2 displacement = transform.position - lastCarrierPosition;
            if (displacement.sqrMagnitude <= TeleportDistance * TeleportDistance) return false;
            dragDirection = displacement.normalized;
            Carried.transform.position = transform.position - (Vector3)(dragDirection * DragDistance);
            lastCarrierPosition = transform.position;
            dragVelocity = Vector3.zero;
            return true;
        }
        private void FollowCarried()
        {
            if (Carried == null || ReconcileTeleport()) return;
            Vector2 movement = transform.position - lastCarrierPosition;
            lastCarrierPosition = transform.position;
            if (movement.sqrMagnitude > .000001f) dragDirection = movement.normalized;
            Vector3 target = transform.position - (Vector3)(dragDirection * DragDistance);
            Vector3 previous = Carried.transform.position;
            Vector3 position = Vector3.SmoothDamp(previous, target, ref dragVelocity, DragSmoothTime, 8f, Time.deltaTime);
            Vector2 offset = position - transform.position;
            if (offset.sqrMagnitude > MaxDragDistance * MaxDragDistance)
            {
                position = transform.position + (Vector3)(offset.normalized * MaxDragDistance);
                dragVelocity = Vector3.zero;
            }
            Carried.transform.position = position;
            dust?.Record(previous, position, motor != null && motor.Velocity.sqrMagnitude > .04f);
        }
        private void Update()
        {
            if (health == null || !health.IsAlive) { DropCarried(); return; }
            if (Time.timeScale == 0) return;
            if (!CanUse) { return; }
            ReconcileTeleport();
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
            BeginDrag();
        }
        private void Clear()
        {
            Carried = null; dragVelocity = Vector3.zero; corpses.Clear();
            if (remains != null) { remains.gameObject.SetActive(false); Destroy(remains.gameObject); } remains = null;
        }
    }
}
