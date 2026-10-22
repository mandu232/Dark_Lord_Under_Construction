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
        private readonly List<Vector2Int> path = new List<Vector2Int>();
        public bool ReachedExit { get; private set; }
        public void Begin(DungeonGrid grid)
        {
            dungeon = grid; motor = GetComponent<TopDownMotor>(); animation = GetComponent<ActorAnimation>();
            GetComponent<MinionController>().enabled = false;
            var tactics = GetComponent<InvaderTactics>(); if (tactics != null) tactics.enabled = false;
            dungeon.Construction.FindPath(dungeon.WorldToCell(transform.position), dungeon.Layout.Entrance, path);
            motor.SpeedMultiplier = 4;
            motor.IgnoreActorBlocking = true;
        }
        private void Update()
        {
            if (dungeon == null || ReachedExit || Time.timeScale <= 0) return;
            motor.RestoreSlows(null);
            while (path.Count > 0 && Vector2.Distance(transform.position, dungeon.CellCenter(path[0])) < .35f) path.RemoveAt(0);
            var target = dungeon.CellCenter(path.Count > 0 ? path[0] : dungeon.Layout.Entrance);
            Vector2 direction = target - transform.position;
            if (path.Count == 0 && direction.magnitude < .4f)
            {
                ReachedExit = true; motor.Stop();
                foreach (var renderer in GetComponentsInChildren<SpriteRenderer>()) renderer.enabled = false;
                return;
            }
            motor.Move(direction); animation.Present(ActorState.Run, direction);
        }
    }
}
