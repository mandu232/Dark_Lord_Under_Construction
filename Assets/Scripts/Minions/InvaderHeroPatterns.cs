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
        // Rowen's leap: crouch, fly to where the target stood, slam with knockback. His sword wave flies down a line.
        private const float LeapMin = 3, LeapRange = 9, SlamRadius = 2.2f, WaveRange = 10;
        private bool phaseTwo, cleaveReturned;
        private int patternStep, chainLeft, volleysLeft, rowenStep, signatureStep;
        private const float SignatureDelay = .9f;
        // Each race's boss kit: cleave, charge, floor mark, impact, signature impact. Indexed by InvaderRace; humans keep the holy kit.
        private static readonly GameplayEffectKind[][] BossKits =
        {
            new[] { GameplayEffectKind.HolySlash, GameplayEffectKind.HolyCharge, GameplayEffectKind.HolyMark, GameplayEffectKind.HolyPulse, GameplayEffectKind.HolyPulse },
            new[] { GameplayEffectKind.OrcCleave, GameplayEffectKind.OrcCharge, GameplayEffectKind.OrcMark, GameplayEffectKind.OrcPulse, GameplayEffectKind.OrcQuake },
            new[] { GameplayEffectKind.ElfCleave, GameplayEffectKind.ElfCharge, GameplayEffectKind.ElfMark, GameplayEffectKind.ElfPulse, GameplayEffectKind.ElfMoonfall },
            new[] { GameplayEffectKind.FrostCleave, GameplayEffectKind.FrostCharge, GameplayEffectKind.FrostMark, GameplayEffectKind.FrostPulse, GameplayEffectKind.FrostSpikes },
            new[] { GameplayEffectKind.EmberCleave, GameplayEffectKind.EmberCharge, GameplayEffectKind.EmberMark, GameplayEffectKind.EmberPulse, GameplayEffectKind.EmberMeteor },
        };
        private const int KitCleave = 0, KitCharge = 1, KitMark = 2, KitPulse = 3, KitSignature = 4;
        private GameplayEffectKind BossFx(int slot) => BossKits[(int)Race][slot];
        private Vector2[] marks = System.Array.Empty<Vector2>();
        public bool PhaseTwo => phaseTwo;
        public bool IsHero => hero;
        // Rowen cannot be interrupted mid-pattern, only punished in his recovery.
        public bool HeroArmored => hero && IsPerforming && Action != InvaderAction.Recovering;
        private float HeroPace => (hero && phaseTwo ? 1.25f : 1) * (enraged ? 1.25f : 1) * (region != null ? region.Pace : 1);

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
            // Rowen works two more answers into his rotation at range: a leap onto the target and a sword wave.
            if (IsRowen)
            {
                int step = rowenStep++ % 4;
                if (step == 1 && distance >= LeapMin && distance <= LeapRange) { StartLeap(target); return true; }
                if (step == 3 && sight && distance <= WaveRange) { StartWave(delta / distance); return true; }
            }
            // Every regional boss but Rowen works its own floor pattern into the ranged rotation.
            if (!HasRowenBody && signatureStep++ % 3 == 2 && distance <= JudgementRange)
            {
                volleysLeft = phaseTwo ? 1 : 0;
                StartSignature(target);
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
            foreach (var mark in marks) GameplayEffects.Show(transform, BossFx(KitMark), mark, scale: MarkRadius / 1.625f, duration: MarkDelay);
        }

        // The boss's own floor pattern, each with a different safe spot:
        // Urgad splits a line towards the target (step aside), Serilia brands an X on it (step straight out),
        // the holy captain consecrates a cross around himself (stand on a diagonal), Hrim rings himself with ice (hug him or back off),
        // Varkan rains meteors around the target (read the marks).
        private void StartSignature(ActorHealth target)
        {
            Vector2 self = transform.position, at = target.transform.position;
            Vector2 aim = (at - self).sqrMagnitude > .01f ? (at - self).normalized : motor.Facing, side = new Vector2(-aim.y, aim.x);
            direction = aim; motor.Face(aim); motor.Stop();
            var points = new System.Collections.Generic.List<Vector2>();
            if (region.IsHolyKingdom)
                foreach (var arm in new[] { aim, side, -aim, -side })
                    for (int i = 1; i <= 2; i++) points.Add(self + arm * MarkRadius * 1.25f * i);
            else if (Race == InvaderRace.Orc)
                for (int i = 1; i <= 5; i++) points.Add(self + aim * MarkRadius * 1.2f * i);
            else if (Race == InvaderRace.Elf)
            {
                points.Add(at);
                foreach (var corner in new[] { aim + side, aim - side, -aim + side, -aim - side }) points.Add(at + corner * MarkRadius * 1.1f);
            }
            else if (Race == InvaderRace.Frostkin)
                for (int i = 0; i < 8; i++) points.Add(self + (Vector2)(Quaternion.Euler(0, 0, i * 45) * aim) * 2.6f);
            else
            {
                points.Add(at);
                for (int i = 0; i < 4; i++) points.Add(at + Random.insideUnitCircle * 3.2f);
            }
            marks = points.ToArray();
            foreach (var mark in marks) GameplayEffects.Show(transform, BossFx(KitMark), mark, scale: MarkRadius / 1.625f, duration: SignatureDelay);
            nextAbility = Time.time + SignatureDelay + (phaseTwo ? 3.5f : 4.5f);
            Begin(InvaderAction.Signature, SignatureDelay);
        }

        private void StartLeap(ActorHealth target)
        {
            // Land where the target is heading, so standing still is the worst answer.
            Vector2 from = transform.position, land = target.transform.position;
            var targetMotor = target.GetComponent<TopDownMotor>();
            if (targetMotor != null) land += targetMotor.Velocity * .3f;
            float leap = phaseTwo ? .65f : .8f, distance = Vector2.Distance(from, land);
            direction = distance > .01f ? (land - from) / distance : motor.Facing;
            motor.Face(direction); motor.Stop();
            marks = new[] { land };
            GameplayEffects.Show(transform, GameplayEffectKind.HolyMark, land, scale: SlamRadius / 1.625f, duration: leap);
            nextAbility = Time.time + leap + (phaseTwo ? 3f : 4f);
            Begin(InvaderAction.Leaping, leap);
            // Crouch for the first fifth, then fly; the step still stops at walls like any other movement.
            motor.AttackStep(direction, distance, leap * .75f, leap * .2f);
        }

        private void StartWave(Vector2 aim)
        {
            direction = aim; motor.Face(direction); motor.Stop();
            float windup = phaseTwo ? .5f : .65f;
            nextAbility = Time.time + windup + (phaseTwo ? 3f : 3.5f);
            Begin(InvaderAction.SwordWave, windup);
        }

        // Finishes the actions only Rowen has. Returns false for anything the shared code should end itself.
        private bool FinishHeroAction(ActorHealth target)
        {
            if (Action == InvaderAction.Leaping)
            {
                GameplayEffects.Show(transform, GameplayEffectKind.HolyPulse, transform.position, scale: SlamRadius / MarkArt);
                Strike(transform.position, SlamRadius, phaseTwo ? 32 : 28);
                Begin(InvaderAction.Slamming, .35f);
                return true;
            }
            if (Action == InvaderAction.Slamming) { Begin(InvaderAction.Recovering, phaseTwo ? .8f : 1.1f); return true; }
            if (Action == InvaderAction.SwordWave)
            {
                rowenBody?.ShowImpact(Action, direction);
                // Phase two fans out three waves.
                Vector2 from = (Vector2)transform.position + direction * .6f;
                SwordWave.Launch(health, controller.ObstacleLayers, from, direction, 20);
                if (phaseTwo)
                    foreach (float turn in new[] { -22f, 22f })
                        SwordWave.Launch(health, controller.ObstacleLayers, from, Quaternion.Euler(0, 0, turn) * direction, 20);
                Begin(InvaderAction.Recovering, .7f);
                return true;
            }
            if (Action == InvaderAction.Roaring)
            {
                rowenBody?.ShowImpact(Action, motor.Facing);
                GameplayEffects.Show(transform, BossFx(KitPulse), transform.position, scale: HeroCleaveRadius / MarkArt);
                Strike(transform.position, HeroCleaveRadius, 15);
                Begin(InvaderAction.Recovering, .5f);
                return true;
            }
            if (Action == InvaderAction.Judging)
            {
                rowenBody?.ShowImpact(Action, motor.Facing);
                foreach (var mark in marks)
                {
                    GameplayEffects.Show(transform, BossFx(KitPulse), mark, scale: MarkRadius / MarkArt);
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
            if (Action == InvaderAction.Signature)
            {
                ShowSoldierImpact();
                // Overlapping marks hit each victim once per volley. Hrim's ice also slows.
                cleaveHits.Clear();
                foreach (var mark in marks)
                {
                    GameplayEffects.Show(transform, BossFx(KitSignature), mark, scale: MarkRadius / MarkArt);
                    Strike(mark, MarkRadius, phaseTwo ? 26 : 22, Race == InvaderRace.Frostkin ? .55f : 1, false);
                }
                if (volleysLeft-- > 0 && target != null && target.IsAlive) StartSignature(target);
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

        private void Strike(Vector2 point, float radius, int damage, float slow = 1, bool fresh = true)
        {
            if (fresh) cleaveHits.Clear();
            int count = Physics2D.OverlapCircle(point, radius, new ContactFilter2D { useTriggers = false }, nearby);
            for (int i = 0; i < count; i++)
            {
                var victim = nearby[i].GetComponentInParent<ActorHealth>();
                if (victim == null || !victim.IsAlive || victim.Faction == health.Faction || victim.gameObject.scene != gameObject.scene || !cleaveHits.Add(victim)) continue;
                if (!victim.TakeDamage(damage, health, DamageType.Physical)) continue;
                ApplyHeroKnockback(victim, (Vector2)victim.transform.position - point);
                if (slow < 1 && victim.IsAlive) victim.GetComponent<TopDownMotor>()?.ApplySlow(slow, 2f);
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
