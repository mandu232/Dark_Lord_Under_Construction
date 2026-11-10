using System;
using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Gameplay;
using DarkLord.Player;
using UnityEngine;

namespace DarkLord.Minions
{
    [Serializable]
    public sealed class CorpseWorkerSaveData
    {
        public Vector3 position;
        public int health;
    }

    [DisallowMultipleComponent]
    public sealed class CorpseWorker : MonoBehaviour
    {
        public const int MaximumHealth = 35;
        private const float SearchInterval = .45f;
        private const float WorkerSpeedMultiplier = .65f;
        private const float CarrySpeedMultiplier = .85f;
        private readonly List<Vector2Int> path = new List<Vector2Int>();
        private readonly List<Vector2Int> candidatePath = new List<Vector2Int>();
        private readonly HashSet<Collider2D> overflownFacilities = new HashSet<Collider2D>();
        private MinionSummoning summoning;
        private CorpseHarvest harvest;
        private DungeonGrid dungeon;
        private TopDownMotor motor;
        private CircleCollider2D bodyCollider;
        private ActorAnimation animation;
        private InvaderCorpse corpse;
        private ResourceBuilding station;
        private bool carrying, fleeing;
        private int waypoint, revision;
        private float nextSearch;
        private Vector2Int? goal;
        public ActorHealth Health { get; private set; }
        public bool IsAlive => Health != null && Health.IsAlive;

        internal void Configure(MinionSummoning owner, CorpseHarvest source, int health)
        {
            summoning = owner;
            harvest = source;
            dungeon = owner.Dungeon;
            // The core can overlap a worker's starting tile before it receives a contact event.
            if (dungeon.CoreFootprintCollider != null) IgnoreFacilityFootprint(dungeon.CoreFootprintCollider);
            motor.SpeedMultiplier = WorkerSpeedMultiplier;
            Health.Configure(Faction.Dungeon, MaximumHealth);
            Health.RestoreHealth(MaximumHealth, health);
        }

        private void Awake()
        {
            Health = GetComponent<ActorHealth>();
            motor = GetComponent<TopDownMotor>();
            bodyCollider = GetComponent<CircleCollider2D>();
            animation = GetComponent<ActorAnimation>();
        }

        private void OnEnable() => Health.Died += OnDied;
        private void OnDisable()
        {
            Health.Died -= OnDied;
            if (bodyCollider != null && bodyCollider.isActiveAndEnabled)
                foreach (var facility in overflownFacilities)
                    if (facility != null && facility.isActiveAndEnabled) Physics2D.IgnoreCollision(bodyCollider, facility, false);
            overflownFacilities.Clear();
            ReleaseCorpse();
            motor.Stop();
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var footprint = collision.collider;
            if (!isActiveAndEnabled || !IsAlive || collision.otherCollider != bodyCollider ||
                dungeon == null || !dungeon.IsFacilityFootprint(footprint)) return;

            // The bird flies over protruding bases while its route still avoids reserved facility cells.
            IgnoreFacilityFootprint(footprint);
        }

        private void IgnoreFacilityFootprint(Collider2D footprint)
        {
            Physics2D.IgnoreCollision(bodyCollider, footprint);
            overflownFacilities.Add(footprint);
        }

        private void OnDied()
        {
            ReleaseCorpse();
            motor.Stop();
            foreach (var collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
            animation.Present(ActorState.Die, motor.Facing);
            Destroy(gameObject, 1.5f);
        }

        private void Update()
        {
            if (Time.timeScale <= 0 || !IsAlive || harvest == null || dungeon == null) return;
            if (harvest.WorkersFlee)
            {
                if (!fleeing)
                {
                    fleeing = true;
                    ReleaseCorpse();
                    goal = null;
                }
                if (summoning.TryWorkerShelter(this, out var shelter))
                    MoveAlongPath(shelter, dungeon.CellCenter(shelter), .2f);
                else Stop();
                return;
            }
            if (fleeing) { fleeing = false; goal = null; nextSearch = 0; }
            if (!harvest.WorkersCanCollect)
            {
                ReleaseCorpse();
                Stop();
                return;
            }
            if (corpse != null && (corpse.Consumed || corpse.AssignedWorker != this || station == null || !station.CanReceiveCorpse))
                ReleaseCorpse();
            if (corpse == null)
            {
                if (Time.time >= nextSearch) FindWork();
                if (corpse == null) { Stop(); return; }
            }
            if (!carrying)
            {
                var corpseCell = dungeon.WorldToCell(corpse.transform.position);
                bool insideFacilityCell = dungeon.BlocksNpcMovement(corpseCell);
                float pickupRange = insideFacilityCell ? CorpseHarvest.PickupDistance : .48f;
                if (Vector2.Distance(transform.position, corpse.transform.position) > pickupRange)
                {
                    // Routes end beside a facility cell, which can still be out of pickup range of a corpse on its far side.
                    MoveAlongPath(corpseCell, corpse.transform.position, .35f, insideFacilityCell);
                    return;
                }
                carrying = true;
                motor.SpeedMultiplier = WorkerSpeedMultiplier * CarrySpeedMultiplier;
                corpse.SetHeld(true);
                goal = null;
            }
            if (harvest.TryDeliverFromWorker(corpse, station, this))
            {
                ReleaseCorpse();
                Stop();
                return;
            }
            // Keep steering to the approach center after entering its cell.
            // A route to the blocked station itself can end at the far edge of this cell.
            if (goal != null && !dungeon.BlocksNpcMovement(goal.Value) &&
                MoveToStationApproach(goal.Value)) return;
            var start = dungeon.WorldToCell(transform.position);
            if (!dungeon.FindNpcPath(start, station.Cell, candidatePath))
            {
                ReleaseCorpse();
                Stop();
                return;
            }
            var approach = candidatePath.Count > 0 ? candidatePath[candidatePath.Count - 1] : start;
            if (!MoveToStationApproach(approach))
            {
                ReleaseCorpse();
                Stop();
            }
        }

        private bool MoveToStationApproach(Vector2Int cell)
        {
            var offset = cell - station.Cell;
            if (Mathf.Abs(offset.x) + Mathf.Abs(offset.y) != 1) return false;
            MoveAlongPath(cell, dungeon.CellCenter(cell), .08f);
            return true;
        }

        private void LateUpdate()
        {
            if (carrying && corpse != null && !corpse.Consumed)
            {
                Vector3 behind = transform.position - (Vector3)motor.Facing * .55f;
                corpse.transform.position = dungeon.Construction.IsWalkable(dungeon.WorldToCell(behind)) ? behind : transform.position;
            }
        }

        private void FindWork()
        {
            nextSearch = Time.time + SearchInterval;
            station = null;
            if (harvest.Corpses.Count == 0) return;
            Vector2Int start = dungeon.WorldToCell(transform.position);
            foreach (var facility in dungeon.RecruitmentFacilities)
            {
                if (!facility.CanReceiveCorpse || !dungeon.FindNpcPath(start, facility.Cell, candidatePath)) continue;
                station = facility;
                break;
            }
            if (station == null) return;
            float nearest = float.PositiveInfinity;
            foreach (var available in harvest.Corpses)
            {
                if (available == null || available.Consumed || available.AssignedWorker != null || available == harvest.Carried) continue;
                float distance = ((Vector2)(available.transform.position - transform.position)).sqrMagnitude;
                if (distance >= nearest || !dungeon.FindNpcPath(start, dungeon.WorldToCell(available.transform.position), candidatePath)) continue;
                corpse = available;
                nearest = distance;
            }
            if (corpse != null)
            {
                corpse.AssignedWorker = this;
                goal = null;
            }
        }

        private void ReleaseCorpse()
        {
            if (corpse != null && corpse.AssignedWorker == this)
            {
                corpse.AssignedWorker = null;
                if (carrying && !corpse.Consumed)
                {
                    if (!dungeon.Construction.IsWalkable(dungeon.WorldToCell(corpse.transform.position)))
                        corpse.transform.position = transform.position;
                    corpse.SetHeld(false);
                }
            }
            corpse = null;
            carrying = false;
            motor.SpeedMultiplier = WorkerSpeedMultiplier;
            goal = null;
        }

        private void MoveAlongPath(Vector2Int cell, Vector3 destination, float arrival, bool closeInOnBlockedCell = false)
        {
            Vector2Int current = dungeon.WorldToCell(transform.position);
            if (goal != cell || revision != dungeon.Construction.Revision || Time.time >= nextSearch ||
                (waypoint < path.Count && dungeon.BlocksNpcMovement(path[waypoint])))
            {
                goal = cell;
                revision = dungeon.Construction.Revision;
                nextSearch = Time.time + SearchInterval;
                waypoint = 0;
                if (!dungeon.FindNpcPath(current, cell, path))
                {
                    if (!fleeing) ReleaseCorpse();
                    Stop();
                    return;
                }
            }
            while (waypoint < path.Count && Vector2.Distance(transform.position, dungeon.CellCenter(path[waypoint])) < .18f) waypoint++;
            if (!closeInOnBlockedCell && waypoint >= path.Count && (current != cell || dungeon.BlocksNpcMovement(cell))) { Stop(); return; }
            Vector2 point = waypoint < path.Count ? dungeon.CellCenter(path[waypoint]) : destination;
            Vector2 delta = point - (Vector2)transform.position;
            if (waypoint >= path.Count && delta.magnitude <= arrival) { Stop(); return; }
            motor.Move(delta.normalized * Mathf.Min(1f, delta.magnitude / Mathf.Max(.01f, motor.EffectiveMoveSpeed * Time.fixedDeltaTime)));
            animation.Present(ActorState.Run, motor.Facing);
        }

        private void Stop()
        {
            motor.Stop();
            animation.Present(ActorState.Idle, motor.Facing);
        }
    }
}
