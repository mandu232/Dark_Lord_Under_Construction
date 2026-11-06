using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Dungeon;
using UnityEngine;

namespace DarkLord.Minions
{
    public enum WorkerState { Idle, Moving, Mining, WaitingForPath, Interrupted, Dead }

    [DisallowMultipleComponent, RequireComponent(typeof(TopDownMotor), typeof(ActorHealth), typeof(CircleCollider2D))]
    public sealed class WorkerMiner : MonoBehaviour
    {
        [SerializeField] private DungeonGrid dungeon;
        [SerializeField, Min(0.1f)] private float miningSeconds = 2f;
        private readonly List<Vector2Int> jobs = new List<Vector2Int>();
        private readonly Dictionary<Vector2Int, float> progress = new Dictionary<Vector2Int, float>();
        private readonly List<Vector2Int> path = new List<Vector2Int>();
        private static readonly Vector2Int[] Directions = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
        private TopDownMotor motor;
        private ActorHealth health;
        private CircleCollider2D bodyCollider;
        private Vector2Int? activeJob;
        private int waypoint;
        private float nextSearch, interruptedUntil, stuckTime;
        private Vector2 lastPosition;
        public WorkerState State { get; private set; }
        public int PendingCount => jobs.Count;
        public Vector2Int? ActiveJob => activeJob;
        public float WorkProgress => activeJob.HasValue && progress.TryGetValue(activeJob.Value, out float value) ? Mathf.Clamp01(value / miningSeconds) : 0;
        public bool Available => isActiveAndEnabled && health != null && health.IsAlive && dungeon != null && dungeon.isActiveAndEnabled;
        public DungeonGrid Dungeon => dungeon;

        private void Awake()
        {
            motor = GetComponent<TopDownMotor>();
            health = GetComponent<ActorHealth>();
            bodyCollider = GetComponent<CircleCollider2D>();
            bodyCollider.radius = 0.22f;
        }

        private void Start()
        {
            // Only this worker passes allied actors, preventing queue stalls in narrow tunnels.
            foreach (var ally in FindObjectsByType<ActorHealth>(FindObjectsSortMode.None))
                if (ally != health && ally.Faction == health.Faction && ally.gameObject.scene == gameObject.scene)
                    foreach (var collider in ally.GetComponentsInChildren<Collider2D>())
                        Physics2D.IgnoreCollision(bodyCollider, collider);
        }

        private void OnEnable() { health.Damaged += OnDamaged; health.Died += OnDied; }
        private void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
            motor.Stop();
            ReleasePath();
        }

        public void Configure(DungeonGrid grid, float seconds = 2f)
        {
            if (jobs.Count != 0) throw new System.InvalidOperationException("Cancel jobs before changing worker configuration.");
            dungeon = grid;
            miningSeconds = Mathf.Max(0.1f, seconds);
        }

        public bool IsQueued(Vector2Int cell) => progress.ContainsKey(cell);
        public void CancelAllMining()
        {
            while (jobs.Count > 0) CancelMining(jobs[jobs.Count - 1]);
        }

        public ExpansionResult CanQueue(Vector2Int cell)
        {
            if (!Available) return ExpansionResult.Unavailable;
            dungeon.Initialize();
            if (!dungeon.Layout.Contains(cell)) return ExpansionResult.OutsideMap;
            return dungeon.Layout.GetCell(cell) == DungeonCell.Rock ? ExpansionResult.Success : ExpansionResult.NotRock;
        }

        public ExpansionResult QueueMining(Vector2Int cell)
        {
            var result = CanQueue(cell);
            if (result != ExpansionResult.Success || IsQueued(cell)) return result;
            jobs.Add(cell);
            progress.Add(cell, 0);
            dungeon.SetMiningMark(cell, true);
            nextSearch = 0;
            return ExpansionResult.Success;
        }

        public bool CancelMining(Vector2Int cell)
        {
            if (!progress.Remove(cell)) return false;
            jobs.Remove(cell);
            if (activeJob == cell) ReleasePath();
            if (dungeon != null) dungeon.SetMiningMark(cell, false);
            return true;
        }

        private void ReleasePath()
        {
            if (activeJob.HasValue && dungeon != null && IsQueued(activeJob.Value)) dungeon.SetMiningMark(activeJob.Value, true);
            activeJob = null;
            path.Clear();
            waypoint = 0;
            stuckTime = 0;
            nextSearch = Time.time + 0.5f;
            motor.Stop();
        }

        private void OnDamaged(ActorHealth source)
        {
            interruptedUntil = Time.time + 0.5f;
            motor.Stop();
            State = WorkerState.Interrupted;
        }

        private void OnDied()
        {
            motor.Stop();
            ReleasePath();
            bodyCollider.enabled = false;
            State = WorkerState.Dead;
        }

        private void Update()
        {
            if (!Available || Time.timeScale == 0) { motor.Stop(); return; }
            if (Time.time < interruptedUntil) { motor.Stop(); return; }
            if (activeJob.HasValue && dungeon.Layout.GetCell(activeJob.Value) != DungeonCell.Rock) CancelMining(activeJob.Value);
            if (!activeJob.HasValue)
            {
                State = jobs.Count == 0 ? WorkerState.Idle : WorkerState.WaitingForPath;
                if (Time.time < nextSearch) return;
                nextSearch = Time.time + 0.5f;
                // Skip inaccessible jobs; finishing another may open their approach later.
                for (int i = 0; i < jobs.Count; i++)
                {
                    if (dungeon.Layout.GetCell(jobs[i]) != DungeonCell.Rock) { CancelMining(jobs[i--]); continue; }
                    if (!FindApproach(dungeon.Layout, dungeon.WorldToCell(transform.position), jobs[i], path, dungeon.BlocksNpcMovement)) continue;
                    activeJob = jobs[i];
                    waypoint = 0;
                    lastPosition = transform.position;
                    break;
                }
                if (!activeJob.HasValue) return;
            }

            if (waypoint < path.Count && dungeon.BlocksNpcMovement(path[waypoint]) && path[waypoint] != dungeon.WorldToCell(transform.position))
            { ReleasePath(); return; }
            if (waypoint < path.Count)
            {
                Vector2 delta = dungeon.CellCenter(path[waypoint]) - transform.position;
                if (delta.magnitude > 0.045f)
                {
                    State = WorkerState.Moving;
                    motor.Move(delta.normalized * Mathf.Min(1, delta.magnitude / Mathf.Max(0.01f, motor.MoveSpeed * Time.fixedDeltaTime)));
                    stuckTime = Vector2.Distance(lastPosition, transform.position) < 0.001f ? stuckTime + Time.deltaTime : 0;
                    lastPosition = transform.position;
                    if (stuckTime > 1.5f) ReleasePath();
                    return;
                }
                waypoint++;
                motor.Stop();
                return;
            }

            var cell = activeJob.Value;
            motor.Stop();
            if (dungeon.CanExpand(cell, transform.position) != ExpansionResult.Success) { ReleasePath(); return; }
            State = WorkerState.Mining;
            motor.Face(dungeon.CellCenter(cell) - transform.position);
            progress[cell] += Time.deltaTime;
            dungeon.SetMiningMark(cell, true, WorkProgress);
            if (progress[cell] < miningSeconds) return;
            if (dungeon.TryExpand(cell, transform.position) == ExpansionResult.Success) CancelMining(cell);
            else ReleasePath();
        }

        // Cardinal BFS follows floor centers: no corner cutting or walking through rock.
        public static bool FindApproach(DungeonLayout layout, Vector2Int start, Vector2Int target, List<Vector2Int> result, System.Predicate<Vector2Int> blocked = null)
        {
            result.Clear();
            if (!layout.IsFloor(start) || layout.GetCell(target) != DungeonCell.Rock || !layout.Contains(target)) return false;
            var frontier = new Queue<Vector2Int>();
            var parents = new Dictionary<Vector2Int, Vector2Int> { [start] = start };
            frontier.Enqueue(start);
            while (frontier.Count > 0)
            {
                var cell = frontier.Dequeue();
                if (Mathf.Abs(cell.x - target.x) + Mathf.Abs(cell.y - target.y) == 1 && blocked?.Invoke(cell) != true)
                {
                    result.Add(cell);
                    while (cell != start) { cell = parents[cell]; result.Add(cell); }
                    result.Reverse();
                    return true;
                }
                foreach (var direction in Directions)
                {
                    var next = cell + direction;
                    if (!layout.IsFloor(next) || blocked?.Invoke(next) == true || parents.ContainsKey(next)) continue;
                    parents.Add(next, cell);
                    frontier.Enqueue(next);
                }
            }
            return false;
        }

        private void OnDestroy()
        {
            if (dungeon != null) foreach (var cell in jobs) dungeon.SetMiningMark(cell, false);
        }
    }
}
