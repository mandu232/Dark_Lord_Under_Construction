using DarkLord.Actors;
using DarkLord.Gameplay;
using UnityEngine;

namespace DarkLord.Minions
{
    // Rowen fights by range: cleave up close, charge at mid range, falling judgement from afar.
    // Below half health he roars once and every pattern gets a follow-up. Each ends in a recovery the player can punish.
    public sealed partial class InvaderTactics
    {
        private const float MarkRadius = 1.6f, MarkDelay = .7f, MarkArt = 2.5f;
        private const float RoarDuration = .9f, ChargeRange = 6, JudgementRange = 14;
        private bool phaseTwo, cleaveReturned;
        private int patternStep, chainLeft, volleysLeft;
        private Vector2[] marks = System.Array.Empty<Vector2>();
        public bool PhaseTwo => phaseTwo;
        public bool IsHero => hero;
        // Rowen cannot be interrupted mid-pattern, only punished in his recovery.
        public bool HeroArmored => hero && IsPerforming && Action != InvaderAction.Recovering;
        private float HeroPace => hero && phaseTwo ? 1.25f : 1;

        private bool TryHeroPattern(ActorHealth target)
        {
            if (!phaseTwo && health.CurrentHealth * 2 <= health.MaximumHealth)
            {
                phaseTwo = true; motor.Stop();
                nextAbility = Time.time + RoarDuration + .4f;
                Begin(InvaderAction.Roaring, RoarDuration);
                return true;
            }
            if (target == null || !target.IsAlive) return false;
            Vector2 delta = target.transform.position - transform.position;
            float distance = delta.magnitude;
            bool sight = controller.HasTerrainLineOfSight(target);
            if (sight && distance > .001f && distance <= HeroCleaveRadius)
            {
                direction = delta / distance; motor.Face(direction); motor.Stop();
                cleaveReturned = false;
                nextAbility = Time.time + (phaseTwo ? 1.8f : 3f);
                Begin(InvaderAction.HeroCleave, phaseTwo ? .45f : .6f);
                return true;
            }
            // At range he alternates, so a player who learned to sidestep the charge still has to watch the floor.
            bool chargeReady = sight && distance >= 1.3f && distance <= ChargeRange;
            if (distance <= JudgementRange && (!chargeReady || patternStep++ % 2 == 1))
            {
                volleysLeft = phaseTwo ? 2 : 0;
                nextAbility = Time.time + MarkDelay + (phaseTwo ? 3.5f : 3f);
                motor.Stop(); PlaceMarks(target);
                Begin(InvaderAction.Judging, MarkDelay);
                return true;
            }
            chainLeft = chargeReady && phaseTwo ? 2 : 0;
            return false; // The shared charger branch performs the first dash.
        }

        private void PlaceMarks(ActorHealth target)
        {
            // Aim where the target is heading; in phase two two more marks close off the escape to either side.
            Vector2 centre = target.transform.position;
            var targetMotor = target.GetComponent<TopDownMotor>();
            if (targetMotor != null) centre += targetMotor.Velocity * .35f;
            if (phaseTwo)
            {
                Vector2 away = (centre - (Vector2)transform.position).normalized, side = new Vector2(-away.y, away.x) * MarkRadius * 1.7f;
                marks = new[] { centre, centre + side, centre - side };
            }
            else marks = new[] { centre };
            foreach (var mark in marks) GameplayEffects.Show(transform, GameplayEffectKind.HolyMark, mark, scale: MarkRadius / 1.625f, duration: MarkDelay);
        }

        // Finishes the actions only Rowen has. Returns false for anything the shared code should end itself.
        private bool FinishHeroAction(ActorHealth target)
        {
            if (Action == InvaderAction.Roaring)
            {
                GameplayEffects.Show(transform, GameplayEffectKind.HolyPulse, transform.position, scale: HeroCleaveRadius / MarkArt);
                Strike(transform.position, HeroCleaveRadius, 15);
                Begin(InvaderAction.Recovering, .5f);
                return true;
            }
            if (Action == InvaderAction.Judging)
            {
                foreach (var mark in marks)
                {
                    GameplayEffects.Show(transform, GameplayEffectKind.HolyPulse, mark, scale: MarkRadius / MarkArt);
                    Strike(mark, MarkRadius, phaseTwo ? 25 : 20);
                }
                if (volleysLeft > 0 && target != null && target.IsAlive)
                {
                    volleysLeft--; PlaceMarks(target);
                    Begin(InvaderAction.Judging, MarkDelay);
                }
                else Begin(InvaderAction.Recovering, phaseTwo ? 1f : 1.2f);
                return true;
            }
            if (Action == InvaderAction.Charging && (chainLeft > 0 || phaseTwo))
            {
                if (chainLeft-- > 0 && target != null && target.IsAlive)
                {
                    Vector2 delta = target.transform.position - transform.position;
                    if (delta.sqrMagnitude > .01f) direction = delta.normalized;
                    motor.Face(direction); motor.Stop();
                    Begin(InvaderAction.WindingUp, .32f);
                }
                else Begin(InvaderAction.Recovering, 1.1f);
                return true;
            }
            return false;
        }

        private void Strike(Vector2 point, float radius, int damage)
        {
            cleaveHits.Clear();
            int count = Physics2D.OverlapCircle(point, radius, new ContactFilter2D { useTriggers = false }, nearby);
            for (int i = 0; i < count; i++)
            {
                var victim = nearby[i].GetComponentInParent<ActorHealth>();
                if (victim == null || !victim.IsAlive || victim.Faction == health.Faction || victim.gameObject.scene != gameObject.scene || !cleaveHits.Add(victim)) continue;
                if (victim.TakeDamage(damage, health, DamageType.Physical)) ApplyHeroKnockback(victim, (Vector2)victim.transform.position - point);
            }
        }

        private void ShieldBash(ActorHealth target)
        {
            if (target == null || !target.IsAlive || !controller.HasTerrainLineOfSight(target) ||
                Vector2.Distance(transform.position, target.transform.position) > 1.9f || !target.TakeDamage(10, health, DamageType.Physical) || !target.IsAlive) return;
            target.GetComponent<TopDownMotor>()?.ApplyKnockback(target.transform.position - transform.position, 7f, .25f);
            target.GetComponent<MinionController>()?.ApplyStun(.5f);
        }
    }
}
