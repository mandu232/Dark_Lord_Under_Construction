using DarkLord.Actors;
using DarkLord.Gameplay;
using UnityEngine;

namespace DarkLord.Minions
{
    // Each regional boss's own pattern, so the five bosses stop playing alike.
    public enum BossSkill { None, LeapAndWave, Sanctuary, Warstomp, MoonVolley, GlacierLances, EruptionChase }

    // Every pattern resolves in short steps (each under a second, so saves stay valid) and names its safe answer:
    // Sanctuary: leave the ring before the burst (he shrugs off damage while channelling and mends his allies).
    // Warstomp: three expanding shock rings, step back in behind the wave.
    // Moon volley: Serilia backsteps out of reach and looses a fan of arrows, close the gap or sidestep.
    // Glacier lances: rows of ice burst one after another towards the target, step sideways.
    // Eruption chase: eruptions keep landing where the target stands, keep moving.
    public sealed partial class InvaderTactics
    {
        private const float StompStep = .55f, StompRingWidth = 1.2f, LanceRowStep = .35f, ChaseStep = .5f, SanctuaryRadius = 3.2f;
        private int skillStep, bossTurn;
        public int SkillStep => skillStep;
        private static float SkillReach(BossSkill skill) => skill switch
        {
            BossSkill.Sanctuary or BossSkill.Warstomp => 5,
            BossSkill.MoonVolley => 9,
            BossSkill.GlacierLances => 7,
            BossSkill.EruptionChase => 12,
            _ => 0
        };

        public static BossSkill SkillFor(RegionDefinition region)
        {
            if (region == null || region.Index == 0) return BossSkill.LeapAndWave;
            if (region.IsHolyKingdom) return BossSkill.Sanctuary;
            return region.Race switch
            {
                InvaderRace.Orc => BossSkill.Warstomp,
                InvaderRace.Elf => BossSkill.MoonVolley,
                InvaderRace.Frostkin => BossSkill.GlacierLances,
                InvaderRace.Emberborn => BossSkill.EruptionChase,
                _ => BossSkill.Sanctuary
            };
        }
        public BossSkill Skill => hero ? SkillFor(region) : BossSkill.None;
        public static string SkillName(BossSkill skill) => skill switch
        {
            BossSkill.LeapAndWave => "도약 강타 · 검기",
            BossSkill.Sanctuary => "성역 선포",
            BossSkill.Warstomp => "대지 진동",
            BossSkill.MoonVolley => "달빛 화살비",
            BossSkill.GlacierLances => "빙하 창",
            BossSkill.EruptionChase => "용암 추격",
            _ => ""
        };
        // Sanctuary halves the damage he takes while he channels.
        private bool Sanctified => hero && Action == InvaderAction.BossSkill && Skill == BossSkill.Sanctuary && skillStep == 0;

        private bool StartBossSkill(ActorHealth target)
        {
            Vector2 self = transform.position, at = target.transform.position;
            Vector2 aim = (at - self).sqrMagnitude > .01f ? (at - self).normalized : motor.Facing;
            direction = aim; motor.Face(aim); motor.Stop();
            skillStep = 0; cleaveHits.Clear();
            float step;
            switch (Skill)
            {
                case BossSkill.Sanctuary:
                    marks = new[] { self };
                    GameplayEffects.Show(transform, GameplayEffectKind.Aegis, self, follow: true, duration: .9f, scale: 1.4f);
                    GameplayEffects.Show(transform, GameplayEffectKind.HolyMark, self, scale: SanctuaryRadius / 1.625f, duration: .9f);
                    step = .9f; break;
                case BossSkill.Warstomp:
                    marks = new[] { self };
                    GameplayEffects.Show(transform, GameplayEffectKind.OrcMark, self, scale: StompRingWidth / 1.625f, duration: StompStep);
                    step = phaseTwo ? .45f : StompStep; break;
                case BossSkill.MoonVolley:
                {
                    // Backstep away from the target; the step still stops at walls.
                    marks = new[] { self - aim * 2.6f };
                    GameplayEffects.Show(transform, GameplayEffectKind.ElfCharge, self, -aim, true);
                    GameplayEffects.Show(transform, GameplayEffectKind.ElfMark, marks[0], scale: .6f, duration: .3f);
                    motor.AttackStep(-aim, 2.6f, .25f);
                    step = .3f; break;
                }
                case BossSkill.GlacierLances:
                {
                    // Four rows of three spikes, all shown up front, erupting one row at a time.
                    Vector2 side = new Vector2(-aim.y, aim.x) * MarkRadius * .9f;
                    marks = new Vector2[12];
                    for (int row = 0; row < 4; row++)
                    {
                        Vector2 centre = self + aim * (1.6f + row * MarkRadius * 1.15f);
                        marks[row * 3] = centre; marks[row * 3 + 1] = centre + side; marks[row * 3 + 2] = centre - side;
                    }
                    foreach (var mark in marks) GameplayEffects.Show(transform, GameplayEffectKind.FrostMark, mark, scale: MarkRadius / 1.625f, duration: .7f + LanceRowStep * 3);
                    step = .7f; break;
                }
                case BossSkill.EruptionChase:
                    marks = new[] { at };
                    GameplayEffects.Show(transform, GameplayEffectKind.EmberMark, at, scale: MarkRadius / 1.625f, duration: ChaseStep);
                    step = ChaseStep; break;
                default: return false;
            }
            nextAbility = Time.time + 5.5f;
            Begin(InvaderAction.BossSkill, step);
            return true;
        }

        // Resolves one step and either begins the next one or recovers.
        private void ContinueBossSkill(ActorHealth target)
        {
            bool targetAlive = target != null && target.IsAlive;
            switch (Skill)
            {
                case BossSkill.Sanctuary:
                {
                    Vector2 self = transform.position;
                    GameplayEffects.Show(transform, GameplayEffectKind.HolyWave, self, scale: SanctuaryRadius / MarkArt);
                    Strike(self, SanctuaryRadius, phaseTwo ? 22 : 18);
                    MendAllies(phaseTwo ? .18f : .12f);
                    // Phase two follows with an outer ring: whoever fled now has to step back in.
                    if (phaseTwo && skillStep == 0)
                    {
                        skillStep = 1;
                        marks = new Vector2[8];
                        for (int i = 0; i < 8; i++) marks[i] = self + (Vector2)(Quaternion.Euler(0, 0, i * 45 + 22.5f) * Vector2.right) * (SanctuaryRadius + 1.2f);
                        foreach (var mark in marks) GameplayEffects.Show(transform, GameplayEffectKind.HolyMark, mark, scale: MarkRadius / 1.625f, duration: .7f);
                        Begin(InvaderAction.BossSkill, .7f); return;
                    }
                    if (skillStep == 1)
                    {
                        cleaveHits.Clear();
                        foreach (var mark in marks)
                        {
                            GameplayEffects.Show(transform, GameplayEffectKind.HolyPulse, mark, scale: MarkRadius / MarkArt);
                            Strike(mark, MarkRadius, 20, 1, false);
                        }
                    }
                    break;
                }
                case BossSkill.Warstomp:
                {
                    // Each ring only hits between its inner and outer edge, so the space behind the wave is safe.
                    float outer = StompRingWidth * (skillStep + 1), inner = outer - StompRingWidth;
                    Vector2 centre = marks.Length > 0 ? marks[0] : (Vector2)transform.position;
                    for (int i = 0; i < 8; i++)
                        GameplayEffects.Show(transform, GameplayEffectKind.OrcQuake, centre + (Vector2)(Quaternion.Euler(0, 0, i * 45) * Vector2.right) * (outer - StompRingWidth * .5f), scale: .55f);
                    StrikeRing(centre, inner, outer, phaseTwo ? 22 : 18);
                    int rings = phaseTwo ? 4 : 3;
                    if (++skillStep < rings)
                    {
                        GameplayEffects.Show(transform, GameplayEffectKind.OrcMark, centre, scale: (outer + StompRingWidth) / 1.625f, duration: StompStep);
                        Begin(InvaderAction.BossSkill, phaseTwo ? .45f : StompStep); return;
                    }
                    break;
                }
                case BossSkill.MoonVolley:
                {
                    if (skillStep == 0)
                    {
                        // Aim after the backstep, then draw.
                        if (targetAlive)
                        {
                            Vector2 delta = target.transform.position - transform.position;
                            if (delta.sqrMagnitude > .01f) direction = delta.normalized;
                        }
                        motor.Face(direction); motor.Stop();
                        GameplayEffects.Show(transform, GameplayEffectKind.ElfMark, (Vector2)transform.position + direction * 1.2f, scale: .5f, duration: .5f);
                        skillStep = 1; Begin(InvaderAction.BossSkill, phaseTwo ? .4f : .5f); return;
                    }
                    ShowSoldierImpact();
                    GameplayEffects.Show(transform, GameplayEffectKind.ElfPulse, transform.position, direction, scale: .6f);
                    int arrows = phaseTwo ? 5 : 3;
                    for (int i = 0; i < arrows; i++)
                    {
                        float turn = (i - (arrows - 1) * .5f) * 14f;
                        InvaderArrow.Launch(health, controller.ObstacleLayers, Quaternion.Euler(0, 0, turn) * direction, 9, 14);
                    }
                    break;
                }
                case BossSkill.GlacierLances:
                {
                    int row = skillStep;
                    if (row * 3 + 2 < marks.Length)
                    {
                        cleaveHits.Clear();
                        for (int i = row * 3; i < row * 3 + 3; i++)
                        {
                            GameplayEffects.Show(transform, GameplayEffectKind.FrostSpikes, marks[i], scale: MarkRadius / MarkArt);
                            Strike(marks[i], MarkRadius, phaseTwo ? 20 : 16, .45f, false);
                        }
                        if (phaseTwo) FreezeHits(.4f);
                    }
                    if (++skillStep < 4) { Begin(InvaderAction.BossSkill, LanceRowStep); return; }
                    break;
                }
                case BossSkill.EruptionChase:
                {
                    Vector2 at = marks.Length > 0 ? marks[0] : (Vector2)transform.position;
                    GameplayEffects.Show(transform, GameplayEffectKind.EmberMeteor, at, scale: MarkRadius / MarkArt);
                    Strike(at, MarkRadius, phaseTwo ? 20 : 16);
                    int eruptions = phaseTwo ? 5 : 4;
                    if (++skillStep < eruptions && targetAlive)
                    {
                        // Lead the target a little, so standing still is the worst answer.
                        Vector2 next = target.transform.position;
                        var targetMotor = target.GetComponent<TopDownMotor>();
                        if (targetMotor != null) next += targetMotor.Velocity * .25f;
                        marks = new[] { next };
                        GameplayEffects.Show(transform, GameplayEffectKind.EmberMark, next, scale: MarkRadius / 1.625f, duration: ChaseStep);
                        Begin(InvaderAction.BossSkill, phaseTwo ? .42f : ChaseStep); return;
                    }
                    break;
                }
            }
            Begin(InvaderAction.Recovering, phaseTwo ? 1f : 1.2f);
        }

        private void StrikeRing(Vector2 centre, float inner, float outer, int damage)
        {
            cleaveHits.Clear();
            int count = Physics2D.OverlapCircle(centre, outer, new ContactFilter2D { useTriggers = false }, nearby);
            for (int i = 0; i < count; i++)
            {
                var victim = nearby[i].GetComponentInParent<ActorHealth>();
                if (victim == null || !victim.IsAlive || victim.Faction == health.Faction || victim.gameObject.scene != gameObject.scene) continue;
                Vector2 delta = (Vector2)victim.transform.position - centre;
                if (delta.magnitude < inner || !cleaveHits.Add(victim) || !victim.TakeDamage(damage, health, DamageType.Physical)) continue;
                ApplyHeroKnockback(victim, delta);
            }
        }

        private void FreezeHits(float stun)
        {
            foreach (var victim in cleaveHits)
                if (victim != null && victim.IsAlive) victim.GetComponent<MinionController>()?.ApplyStun(stun);
        }

        private void MendAllies(float share)
        {
            int count = Physics2D.OverlapCircle(transform.position, SanctuaryRadius + 1, new ContactFilter2D { useTriggers = false }, nearby);
            for (int i = 0; i < count; i++)
            {
                var ally = nearby[i].GetComponentInParent<ActorHealth>();
                if (ally == null || ally == health || !ally.IsAlive || ally.Faction != health.Faction || ally.gameObject.scene != gameObject.scene ||
                    ally.CurrentHealth >= ally.MaximumHealth) continue;
                ally.Heal(Mathf.Max(10, Mathf.RoundToInt(ally.MaximumHealth * share)));
                GameplayEffects.Show(ally.transform, GameplayEffectKind.HolyHeal, ally.transform.position, follow: true);
            }
        }
    }
}
