using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Dungeon;
using DarkLord.Minions;
using UnityEngine;

namespace DarkLord.Gameplay
{
    public sealed class OpeningRetreat : MonoBehaviour
    {
        private DungeonGrid dungeon;
        private TopDownMotor motor;
        private ActorAnimation animation;
        private ActorHealth health;
        private readonly List<Vector2Int> path = new List<Vector2Int>();
        private readonly Collider2D[] nearby = new Collider2D[64];
        private readonly Dictionary<TopDownMotor, float> pushedUntil = new Dictionary<TopDownMotor, float>();
        private Vector2 travelDirection;
        public bool ReachedExit { get; private set; }
        public void Begin(DungeonGrid grid)
        {
            dungeon = grid; motor = GetComponent<TopDownMotor>(); animation = GetComponent<ActorAnimation>(); health = GetComponent<ActorHealth>();
            GetComponent<MinionController>().enabled = false;
            var tactics = GetComponent<InvaderTactics>(); if (tactics != null) tactics.enabled = false;
            health.Died += OnDied;
            dungeon.FindNpcPath(dungeon.WorldToCell(transform.position), dungeon.Layout.Entrance, path);
            motor.SpeedMultiplier = 4;
            motor.IgnoreActorBlocking = true;
        }
        private void Update()
        {
            if (dungeon == null || ReachedExit || Time.timeScale <= 0 || !health.IsAlive) return;
            motor.RestoreSlows(null);
            while (path.Count > 0 && Vector2.Distance(transform.position, dungeon.CellCenter(path[0])) < .35f) path.RemoveAt(0);
            var target = dungeon.CellCenter(path.Count > 0 ? path[0] : dungeon.Layout.Entrance);
            Vector2 direction = target - transform.position;
            if (path.Count == 0 && direction.magnitude < .4f)
            {
                ReachedExit = true; motor.Stop();
                gameObject.SetActive(false);
                return;
            }
            travelDirection = direction.normalized;
            motor.Move(direction); animation.Present(ActorState.Run, direction);
        }

        private void FixedUpdate()
        {
            if (dungeon == null || ReachedExit || !health.IsAlive || Time.timeScale <= 0 || travelDirection.sqrMagnitude < .001f) return;
            int count = Physics2D.OverlapCircle((Vector2)transform.position + travelDirection * .25f, .65f,
                new ContactFilter2D { useTriggers = false }, nearby);
            Vector2 sideways = new Vector2(-travelDirection.y, travelDirection.x);
            for (int i = 0; i < count; i++)
            {
                var other = nearby[i].GetComponentInParent<ActorHealth>();
                if (other == null || other.gameObject.scene != gameObject.scene || other.Faction != Faction.Dungeon || !other.IsAlive) continue;
                var otherMotor = other.GetComponent<TopDownMotor>();
                if (otherMotor == null || !otherMotor.isActiveAndEnabled || pushedUntil.TryGetValue(otherMotor, out float until) && Time.time < until) continue;
                Vector2 delta = (Vector2)(other.transform.position - transform.position);
                float side = Vector2.Dot(delta, sideways);
                float sign = Mathf.Abs(side) > .01f ? Mathf.Sign(side) : 1;
                otherMotor.ApplyKnockback((sideways * sign + travelDirection * .25f).normalized, 2.4f, .18f);
                pushedUntil[otherMotor] = Time.time + .3f;
            }
        }

        private void OnDied()
        {
            motor.Stop();
            foreach (var collider in GetComponentsInChildren<Collider2D>()) collider.enabled = false;
            animation.Present(ActorState.Die, motor.Facing);
            Destroy(gameObject, 1.5f);
        }

        private void OnDisable()
        {
            if (health != null) health.Died -= OnDied;
            if (motor != null) motor.Stop();
        }
    }
}
