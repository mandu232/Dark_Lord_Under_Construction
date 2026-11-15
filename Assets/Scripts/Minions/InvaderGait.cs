using DarkLord.Actors;
using UnityEngine;

namespace DarkLord.Minions
{
    // How fast an invader on a dungeon route is walking right now. Nobody marches like clockwork:
    //  - each invader has a stride of its own and a gentle surge and lag to its step;
    //  - a column marches warily, closes up at a run when it has fallen behind, and runs in a fight;
    //  - the badly wounded drag their feet;
    //  - a standing invader takes a moment to get up to speed instead of starting at full pace.
    // Retreating invaders sprint out of panic and tire as they go (see InvaderRetreat).
    // Champions keep their own deliberate pace. Any committed action (a charge, an aimed shot) runs at
    // the plain pace, so its warning area and its travel stay exact.
    public sealed partial class MinionController
    {
        public const float MarchPace = .88f, BattlePace = 1.05f, CatchUpPace = 1.2f, FallBackPace = 1.12f;
        public const float WoundedShare = .4f, LimpPace = .75f;
        private const float StartPace = .45f, Acceleration = 3.5f, Deceleration = 2.5f, CadenceSwing = .06f;
        private float stride = -1, cadencePhase, cadenceRate, gaitPace = StartPace;

        public float GaitPace => gaitPace;

        private void UpdateGait()
        {
            if (health.Faction != Faction.Invader || dungeonRoute == null) return;
            if (IsHero || tactics != null && tactics.IsPerforming)
            {
                gaitPace = 1;
                motor.MovementMultiplier = 1;
                return;
            }
            if (stride < 0)
            {
                stride = .93f + .14f * Spread(.6180339f);
                cadencePhase = Mathf.PI * 2 * Spread(.7548777f);
                cadenceRate = 1.1f + .8f * Spread(.5698403f);
            }
            float wanted = DesiredPace();
            // Standing still: the next step starts from a walk and builds up.
            if (State != ActorState.Run) gaitPace = Mathf.Min(gaitPace, wanted * StartPace);
            else gaitPace = Mathf.MoveTowards(gaitPace, wanted, (gaitPace < wanted ? Acceleration : Deceleration) * Time.deltaTime);
            motor.MovementMultiplier = gaitPace;
        }

        // A fixed value in [0, 1) per invader, so each keeps its own quirks without drawing on the shared random state.
        private float Spread(float salt) => Mathf.Repeat((encirclementId + 1) * salt + salt * 7.31f, 1);

        private float DesiredPace()
        {
            float pace;
            if (IsRetreating) pace = RetreatPace();
            else if (IsFallingBack) pace = FallBackPace;
            else if (target != null && target != assaultObjective || party != null && party.InCombat) pace = BattlePace;
            else if (party != null && party.IsLagging(this)) pace = CatchUpPace;
            else pace = MarchPace;
            float share = health.CurrentHealth / (float)Mathf.Max(1, health.MaximumHealth);
            if (share < WoundedShare) pace *= Mathf.Lerp(LimpPace, 1, share / WoundedShare);
            return pace * stride * (1 + CadenceSwing * Mathf.Sin(Time.time * cadenceRate + cadencePhase));
        }
    }
}
