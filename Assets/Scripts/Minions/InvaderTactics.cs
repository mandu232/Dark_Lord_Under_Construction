using System;
using DarkLord.Actors;
using DarkLord.Gameplay;
using UnityEngine;

namespace DarkLord.Minions
{
    public enum InvaderRole { Warrior = 0, Shieldbearer = 1, Healer = 2, Charger = 3, Archer = 4, HolyKnight = 5, Paladin = 6, Cleric = 7 }
    public enum InvaderAction { Ready, Healing, WindingUp, Charging, Recovering, HeroCleave, ShieldBash, Judging, Roaring, Leaping, Slamming, SwordWave, Aiming, Signature }

    [Serializable]
    public sealed class InvaderTacticsSaveData
    {
        public InvaderRole role;
        public InvaderAction action;
        public float remaining, cooldown;
        public Vector2 direction, healPosition;
        public bool hero, phaseTwo;
        public Vector2[] marks;
    }

    [DisallowMultipleComponent, RequireComponent(typeof(MinionController))]
    public sealed partial class InvaderTactics : MonoBehaviour
    {
        private MinionController controller;
        private ActorHealth health;
        private TopDownMotor motor;
        private float endsAt, nextAbility, actionLength;
        private Vector2 direction;
        private ActorHealth healTarget;
        private ActorAnimation presentation;
        private readonly System.Collections.Generic.HashSet<ActorHealth> chargeHits = new System.Collections.Generic.HashSet<ActorHealth>();
        private bool hero;
        private const float HeroCleaveRadius = 2.5f;
        private const float ChargeHitRadius = 1.15f;
        private const float HeroChargeDuration = .35f;
        private const float ChargeSpeedMultiplier = 2.6f;
        private SpriteRenderer heroWarning;
        private Texture2D warningTexture;
        private Sprite warningSprite;
        private InvaderAction warningShape = InvaderAction.Ready;
        private float warningTravel = -1;
        private readonly System.Collections.Generic.HashSet<ActorHealth> cleaveHits = new System.Collections.Generic.HashSet<ActorHealth>();
        public void SetHero(bool value)
        {
            if (hero != value) soldierArtDirty = true;
            hero = value;
            RefreshHeroWarning();
        }
        private Vector2 savedHealPosition;
        private readonly Collider2D[] nearby = new Collider2D[96];
        private Sprite iconSprite;
        private SpriteRenderer badge, cross, cue;
        public InvaderRole Role { get; private set; }
        public InvaderAction Action { get; private set; }
        public bool IsPerforming => Action != InvaderAction.Ready;
        // 0 at the start of the current action, 1 when it resolves; animations use it to land on the hit.
        public float ActionProgress => actionLength > 0 ? Mathf.Clamp01(1 - (endsAt - Time.time) / actionLength) : 1;
        // The first region's champion adds the leap and sword wave. His body art also covers the tutorial Rowen, who has no region.
        public bool IsRowen => hero && region != null && region.Index == 0;
        private bool HasRowenBody => hero && (region == null || region.Index == 0);
        private RowenAnimator rowenBody;
        public static string RoleName(InvaderRole role) => role switch
        {
            InvaderRole.Shieldbearer => "방패병",
            InvaderRole.Healer => "치유사",
            InvaderRole.Charger => "돌격병",
            InvaderRole.Archer => "궁수",
            InvaderRole.HolyKnight => "성기사",
            InvaderRole.Paladin => "팔라딘",
            InvaderRole.Cleric => "성직자",
            _ => "전사"
        };
        internal static bool IsHealingRole(InvaderRole role) => role == InvaderRole.Healer || role == InvaderRole.Cleric;
        internal static bool IsShieldRole(InvaderRole role) => role == InvaderRole.Shieldbearer || role == InvaderRole.Paladin;
        internal static bool IsChargeRole(InvaderRole role, bool champion) =>
            role == InvaderRole.Charger || champion && role == InvaderRole.Paladin;
        internal bool IsHealer => IsHealingRole(Role);
        internal bool HasShield => IsShieldRole(Role);

        private DarkLord.Gameplay.RegionDefinition region;
        private bool enraged;
        public DarkLord.Gameplay.InvaderRace Race => region != null ? region.Race : DarkLord.Gameplay.InvaderRace.Human;
        public Color RaceTint => region != null ? region.BodyTint : Color.white;
        public bool IsEnraged => enraged;
        private Color RageTint => enraged ? new Color(1, .55f, .5f) : Color.white;
        // Call before Configure: the uniform colour and movement pace both read the race.
        public void SetRace(DarkLord.Gameplay.RegionDefinition value)
        {
            if (region == null && value != null) transform.localScale *= value.Scale;
            region = value;
            soldierArtDirty = true;
        }
        private Color RoleTint(InvaderRole role) => (role == InvaderRole.Shieldbearer ? new Color(.65f, .8f, 1) :
            role == InvaderRole.Healer ? new Color(.65f, 1, .7f) :
            role == InvaderRole.Charger ? new Color(1, .65f, .45f) : Color.white) * RaceTint * RageTint;
        private void UpdateRage()
        {
            if (enraged || region == null || !region.Enrages || !health.IsAlive || health.CurrentHealth * 2 > health.MaximumHealth) return;
            enraged = true;
            if (Action != InvaderAction.Charging) motor.SpeedMultiplier = HeroPace;
            var body = controller.GetComponentInChildren<SpriteRenderer>();
            if (body != null) body.color = RoleTint(Role);
        }

        private void Awake()
        {
            controller = GetComponent<MinionController>();
            health = GetComponent<ActorHealth>();
            motor = GetComponent<TopDownMotor>();
            presentation = GetComponent<ActorAnimation>();
        }
        private void OnEnable() { health.DamageMultiplier += Mitigate; health.Damaged += Interrupt; health.Died += Cancel; }
        private void OnDisable()
        {
            health.DamageMultiplier -= Mitigate; health.Damaged -= Interrupt; health.Died -= Cancel;
            Cancel();
        }
        public void Configure(InvaderRole role)
        {
            Cancel(); Role = role; nextAbility = Time.time + 1;
            soldierArtDirty = true;
            // Regional tint also serves as the fallback before role art loads.
            var body = controller.GetComponentInChildren<SpriteRenderer>();
            if (body != null) body.color = RoleTint(role);
            if (badge == null) CreateMarkers();
            badge.color = HasShield ? new Color(.35f, .7f, 1) : IsHealer ? new Color(.3f, 1, .5f) : new Color(1, .3f, .2f);
            badge.transform.localScale = HasShield ? new Vector3(.34f, .42f, 1) : new Vector3(.12f, .4f, 1);
            badge.gameObject.SetActive(HasShield || IsHealer || role == InvaderRole.Charger);
            cross.color = badge.color;
            cross.gameObject.SetActive(IsHealer);
        }
        private float Mitigate(ActorHealth source, DamageType type)
        {
            if (!isActiveAndEnabled) return 1;
            float race = region == null ? 1 : type == DamageType.Magic ? region.MagicTaken : region.PhysicalTaken;
            if (!HasShield || type != DamageType.Physical || source == null) return race;
            Vector2 delta = source.transform.position - transform.position;
            return race * (delta.sqrMagnitude > .001f && Vector2.Dot(motor.Facing, delta.normalized) >= .5f ? .3f : 1);
        }
        private void Interrupt(ActorHealth source)
        {
            if (controller.IsStaggerImmune) return;
            // Casting can be interrupted; a committed charge is countered by dodging or slowing.
            if (Action == InvaderAction.Healing) { Cancel(); nextAbility = Time.time + 2; }
            else if (Action == InvaderAction.Aiming) { Cancel(); nextAbility = Time.time + .7f; }
        }
        internal void CancelForStun() { Cancel(); nextAbility = Mathf.Max(nextAbility, Time.time + 2); }
        private void Cancel()
        {
            Action = InvaderAction.Ready; healTarget = null;
            if (motor != null) motor.IgnoreMinionBlocking = false;
            if (heroWarning != null) heroWarning.enabled = false;
            if (motor != null) { motor.SpeedMultiplier = HeroPace; motor.Stop(); }
            if (cue != null) cue.gameObject.SetActive(false);
        }
        public bool TryAct(ActorHealth target)
        {
            if (!isActiveAndEnabled || !controller.enabled || controller.IsStunned || !health.IsAlive || health.Faction != Faction.Invader || Time.timeScale <= 0) return false;
            if (IsPerforming)
            {
                motor.Stop();
                if (Action == InvaderAction.Charging)
                {
                    motor.SpeedMultiplier = ChargeSpeedMultiplier;
                    motor.IgnoreMinionBlocking = hero;
                    motor.Move(direction); // Motor retains terrain, body-blocking and timed slow rules.
                    if (Time.time < endsAt && HitCharge(target) && !hero) Begin(InvaderAction.Recovering, .65f);
                }
                if (Time.time < endsAt) return true;
                if (Action == InvaderAction.Aiming)
                {
                    ShowSoldierImpact();
                    InvaderArrow.Launch(health, controller.ObstacleLayers, direction, BowRange, BowDamage);
                    Begin(InvaderAction.Recovering, .32f);
                    return true;
                }
                if (Action == InvaderAction.WindingUp) { Begin(InvaderAction.Charging, hero ? HeroChargeDuration : .65f); motor.SpeedMultiplier = ChargeSpeedMultiplier; return true; }
                if (Action == InvaderAction.HeroCleave)
                {
                    rowenBody?.ShowImpact(Action, direction);
                    if (UsesRegionalChampionArt) ShowSoldierImpact();
                    if (hero) { motor.AttackStep(direction, .55f, .18f); presentation.Present(ActorState.Attack, direction); ShowHeroEffect(false); }
                    cleaveHits.Clear();
                    int count = Physics2D.OverlapCircle(transform.position, HeroCleaveRadius, new ContactFilter2D { useTriggers = false }, nearby);
                    for (int i = 0; i < count; i++)
                    {
                        var victim = nearby[i].GetComponentInParent<ActorHealth>();
                        if (victim == null || !victim.IsAlive || victim.Faction == health.Faction || victim.gameObject.scene != gameObject.scene || !cleaveHits.Add(victim) || !controller.HasTerrainLineOfSight(victim)) continue;
                        Vector2 delta = victim.transform.position - transform.position;
                        if (delta.sqrMagnitude > .0001f && Vector2.Dot(direction, delta.normalized) >= 0f)
                            if (victim.TakeDamage(25, health, DamageType.Physical)) ApplyHeroKnockback(victim, delta);
                    }
                    // In phase two he carries the swing through to the half circle behind him.
                    if (hero && phaseTwo && !cleaveReturned) { cleaveReturned = true; direction = -direction; motor.Face(direction); Begin(InvaderAction.HeroCleave, .3f); }
                    else Begin(InvaderAction.Recovering, phaseTwo ? .8f : .5f);
                    return true;
                }
                if (hero && FinishHeroAction(target)) return true;
                if (Action == InvaderAction.ShieldBash)
                {
                    ShowSoldierImpact();
                    ShieldBash(target);
                    Begin(InvaderAction.Recovering, .4f);
                    return true;
                }
                if (Action == InvaderAction.Healing)
                {
                    if (healTarget == null) healTarget = FindHealTarget(true);
                    if (CanHeal(healTarget))
                    {
                        ShowSoldierImpact();
                        int amount = Role == InvaderRole.Cleric ? Mathf.Max(18, Mathf.RoundToInt(healTarget.MaximumHealth * .12f)) : 18;
                        healTarget.Heal(amount);
                        ShowHealEffects(healTarget);
                    }
                }
                Cancel();
                return true;
            }
            if (IsArcher) return TryAim(target);
            if (Time.time < nextAbility) return false;
            nextAbility = Time.time + .2f;
            if (IsHealer)
            {
                healTarget = FindHealTarget(false);
                if (healTarget == null) return false;
                savedHealPosition = healTarget.transform.position;
                nextAbility = Time.time + 4;
                Begin(InvaderAction.Healing, .8f);
                motor.Stop();
                return true;
            }
            if (hero && TryHeroPattern(target)) return true;
            if (HasShield && !IsChargeRole(Role, hero) && target != null && target.IsAlive && controller.HasTerrainLineOfSight(target))
            {
                Vector2 delta = target.transform.position - transform.position;
                if (delta.sqrMagnitude < .001f || delta.magnitude > 1.5f) return false;
                direction = delta.normalized; motor.Face(direction); motor.Stop();
                nextAbility = Time.time + 5;
                Begin(InvaderAction.ShieldBash, .45f);
                return true;
            }
            if (IsChargeRole(Role, hero) && target != null && target.IsAlive && controller.HasTerrainLineOfSight(target))
            {
                Vector2 delta = target.transform.position - transform.position;
                if (delta.magnitude < 1.3f || delta.magnitude > 6) return false;
                direction = delta.normalized; motor.Face(direction); motor.Stop();
                nextAbility = Time.time + (hero ? (phaseTwo ? 3.5f : 4.5f) : 6);
                Begin(InvaderAction.WindingUp, hero && phaseTwo ? .55f : .75f);
                return true;
            }
            return false;
        }
        private void Begin(InvaderAction action, float duration)
        {
            Action = action; endsAt = Time.time + duration; actionLength = duration;
            motor.SpeedMultiplier = HeroPace;
            // He clears minions in a charge and sails over them in a leap.
            motor.IgnoreMinionBlocking = action == InvaderAction.Leaping;
            if (action == InvaderAction.Charging)
            {
                chargeHits.Clear();
                if (hero) { presentation.Present(ActorState.Run, direction); ShowHeroEffect(true); }
            }
            bool bash = action == InvaderAction.ShieldBash;
            cue.gameObject.SetActive(action == InvaderAction.WindingUp || action == InvaderAction.Healing || action == InvaderAction.HeroCleave || action == InvaderAction.SwordWave || bash);
            cue.color = action == InvaderAction.Healing ? new Color(.2f, 1, .4f, .75f) : new Color(1, .15f, .1f, .7f);
            cue.transform.localPosition = action == InvaderAction.Healing ? new Vector3(0, 1.3f) : (Vector3)(direction * (bash ? .9f : 2.5f));
            cue.transform.localScale = action == InvaderAction.Healing ? new Vector3(.65f, .15f, 1) : bash ? new Vector3(1.8f, .18f, 1) : new Vector3(5, .18f, 1);
            cue.transform.localRotation = action == InvaderAction.Healing ? Quaternion.identity : Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            RefreshHeroWarning();
        }
        private bool CanHeal(ActorHealth actor) => actor != null && actor != health && actor.IsAlive && actor.isActiveAndEnabled &&
            actor.Faction == health.Faction && actor.gameObject.scene == gameObject.scene && actor.CurrentHealth < actor.MaximumHealth &&
            Vector2.Distance(transform.position, actor.transform.position) <= 5.5f && controller.HasTerrainLineOfSight(actor);
        private ActorHealth FindHealTarget(bool restore)
        {
            int count = Physics2D.OverlapCircle(transform.position, 5.5f, new ContactFilter2D { useTriggers = false }, nearby);
            ActorHealth best = null; float score = float.NegativeInfinity;
            for (int i = 0; i < count; i++)
            {
                var actor = nearby[i].GetComponentInParent<ActorHealth>();
                if (!CanHeal(actor)) continue;
                float value = restore ? -Vector2.Distance(savedHealPosition, actor.transform.position) : actor.MaximumHealth - actor.CurrentHealth;
                if (value > score) { best = actor; score = value; }
            }
            return best;
        }
        private bool HitCharge(ActorHealth target)
        {
            int count = Physics2D.OverlapCircle(transform.position, ChargeHitRadius, new ContactFilter2D { useTriggers = false }, nearby);
            for (int i = 0; i < count; i++)
            {
                var actor = nearby[i].GetComponentInParent<ActorHealth>();
                if (TryHitCharge(actor) && !hero) return true;
            }
            // The dungeon core is a route objective without a physics collider.
            return TryHitCharge(target) && !hero;
        }
        private bool TryHitCharge(ActorHealth actor)
        {
            if (actor == null || !actor.IsAlive || actor.Faction == health.Faction || actor.gameObject.scene != gameObject.scene || !controller.HasTerrainLineOfSight(actor)) return false;
            Vector2 delta = actor.transform.position - transform.position;
            if (delta.magnitude > ChargeHitRadius || Vector2.Dot(direction, delta.normalized) < .2f) return false;
            if (hero && !chargeHits.Add(actor)) return false;
            bool hit = actor.TakeDamage(22, health, DamageType.Physical);
            if (hero && hit) ApplyHeroKnockback(actor, delta);
            if (!hero) motor.Stop(); return true;
        }
        private static void ApplyHeroKnockback(ActorHealth victim, Vector2 direction)
        {
            if (!victim.IsAlive || victim.GetComponent<MinionController>()?.IsStaggerImmune == true) return;
            victim.GetComponent<TopDownMotor>()?.ApplyKnockback(direction, 7f, .3f);
        }
        private void RefreshHeroWarning()
        {
            bool active = hero && isActiveAndEnabled && health != null && health.IsAlive &&
                (Action == InvaderAction.HeroCleave || Action == InvaderAction.WindingUp || Action == InvaderAction.Roaring);
            if (!active) { if (heroWarning != null) heroWarning.enabled = false; return; }
            if (cue != null) cue.gameObject.SetActive(false);
            if (heroWarning == null)
            {
                heroWarning = Marker("Rowen danger area", Vector3.zero, Vector3.one);
                heroWarning.sortingOrder = -10;
            }
            float distance = motor.MoveSpeed * ChargeSpeedMultiplier * motor.MovementMultiplier * motor.SlowMultiplier * HeroChargeDuration;
            // The sprite is regenerated only when switching between the two shapes.
            if (warningShape != Action || warningSprite == null || Action == InvaderAction.WindingUp && Mathf.Abs(warningTravel - distance) > .01f)
            {
                if (warningSprite != null) Destroy(warningSprite);
                if (warningTexture != null) Destroy(warningTexture);
                warningTexture = new Texture2D(128, 64, TextureFormat.RGBA32, false);
                warningTexture.filterMode = FilterMode.Bilinear;
                var pixels = new Color[128 * 64];
                float travel = motor.MoveSpeed * ChargeSpeedMultiplier * motor.MovementMultiplier * motor.SlowMultiplier * HeroChargeDuration;
                float halfLength = travel * .5f + ChargeHitRadius;
                for (int y = 0; y < 64; y++) for (int x = 0; x < 128; x++)
                {
                    float nx = (x + .5f) / 128f * 2f - 1f, ny = (y + .5f) / 64f * 2f - 1f;
                    bool inside;
                    float edge;
                    if (Action == InvaderAction.Roaring)
                    {
                        float radius = Mathf.Sqrt(nx * nx + ny * ny);
                        inside = radius <= 1;
                        edge = 1 - radius;
                    }
                    else if (Action == InvaderAction.HeroCleave)
                    {
                        float radius = Mathf.Sqrt(nx * nx + ny * ny);
                        inside = nx >= 0 && radius <= 1;
                        edge = Mathf.Min(1 - radius, nx);
                    }
                    else
                    {
                        float px = nx * halfLength;
                        float py = ny * ChargeHitRadius;
                        float q = Mathf.Max(0, Mathf.Abs(px) - travel * .5f);
                        float radius = Mathf.Sqrt(q * q + py * py);
                        inside = radius <= ChargeHitRadius;
                        edge = (ChargeHitRadius - radius) / ChargeHitRadius;
                    }
                    float alpha = inside ? (edge < .045f ? .8f : .3f) : 0;
                    pixels[y * 128 + x] = new Color(1, 1, 1, alpha);
                }
                warningTexture.SetPixels(pixels); warningTexture.Apply();
                warningSprite = Sprite.Create(warningTexture, new Rect(0, 0, 128, 64), Vector2.one * .5f, 64);
                heroWarning.sprite = warningSprite;
                warningShape = Action;
                warningTravel = distance;
            }
            heroWarning.enabled = true;
            heroWarning.color = new Color(1f, .08f, .08f, 1f);
            heroWarning.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            Vector3 scale = transform.lossyScale;
            float xScale = Mathf.Max(.001f, Mathf.Abs(scale.x)), yScale = Mathf.Max(.001f, Mathf.Abs(scale.y));
            bool cleave = Action == InvaderAction.HeroCleave || Action == InvaderAction.Roaring;
            float width = cleave ? HeroCleaveRadius * 2 : distance + ChargeHitRadius * 2;
            float height = cleave ? HeroCleaveRadius * 2 : ChargeHitRadius * 2;
            // Sprite bounds are 2 by 1 units. Compensate for Rowen's enlarged model.
            heroWarning.transform.localScale = new Vector3(width / (2 * xScale), height / yScale, 1);
            heroWarning.transform.localPosition = cleave ? Vector3.zero :
                new Vector3(direction.x * distance * .5f / xScale, direction.y * distance * .5f / yScale, 0);
        }
        private void ShowHeroEffect(bool charge)
        {
            GameplayEffects.Show(transform, BossFx(charge ? KitCharge : KitCleave), transform.position, direction, true);
        }
        // Release flare on the caster, mend on the ally: orc shaman spirit fire, elf mage moonlight, cleric holy light.
        private void ShowHealEffects(ActorHealth ally)
        {
            GameplayEffectKind cast, mend;
            if (Role == InvaderRole.Cleric) { cast = GameplayEffectKind.ClericCast; mend = GameplayEffectKind.HolyHeal; }
            else if (Race == InvaderRace.Orc) { cast = GameplayEffectKind.OrcShamanCast; mend = GameplayEffectKind.OrcShamanMend; }
            else if (Race == InvaderRace.Elf) { cast = GameplayEffectKind.ElfMageCast; mend = GameplayEffectKind.ElfMageMend; }
            else return; // The human healer keeps its plain cue.
            GameplayEffects.Show(transform, cast, transform.position, follow: true);
            GameplayEffects.Show(ally.transform, mend, ally.transform.position, follow: true);
        }
        private SpriteRenderer Marker(string title, Vector3 position, Vector3 size)
        {
            var node = new GameObject(title); node.transform.SetParent(transform, false);
            node.transform.localPosition = position; node.transform.localScale = size;
            var renderer = node.AddComponent<SpriteRenderer>(); renderer.sprite = iconSprite; renderer.sortingOrder = 30;
            return renderer;
        }
        private void CreateMarkers()
        {
            iconSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
            badge = Marker("Role badge", new Vector3(0, .9f), Vector3.one);
            cross = Marker("Healer cross", new Vector3(0, .9f), new Vector3(.4f, .12f, 1));
            cue = Marker("Ability warning", Vector3.zero, Vector3.one); cue.gameObject.SetActive(false);
        }
        private void LateUpdate()
        {
            // Lazily, so every spawn and save-restore path gets the body art once hero and region are both known.
            if (rowenBody == null && HasRowenBody) rowenBody = gameObject.AddComponent<RowenAnimator>();
            UpdateRage();
            DrawSoldierArt();
            RefreshHeroWarning();
            if (!HasShield || cue == null || hero) return;
            cue.gameObject.SetActive(health.IsAlive && controller.enabled);
            cue.color = new Color(.35f, .7f, 1, .85f);
            cue.transform.localPosition = (Vector3)(motor.Facing * .65f);
            cue.transform.localScale = new Vector3(.12f, .65f, 1);
            cue.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(motor.Facing.y, motor.Facing.x) * Mathf.Rad2Deg);
        }
        public InvaderTacticsSaveData CaptureSave() => new InvaderTacticsSaveData { role = Role, action = Action,
            remaining = IsPerforming ? Mathf.Max(0, endsAt - Time.time) : 0, cooldown = Mathf.Max(0, nextAbility - Time.time), direction = direction,
            healPosition = healTarget != null ? (Vector2)healTarget.transform.position : savedHealPosition,
            hero = hero, phaseTwo = phaseTwo, marks = marks };
        public void RestoreSave(InvaderTacticsSaveData data)
        {
            Configure(data != null ? data.role : InvaderRole.Warrior);
            if (data == null) return;
            direction = data.direction.normalized; savedHealPosition = data.healPosition;
            nextAbility = Time.time + data.cooldown;
            if (data.hero) hero = true;
            phaseTwo = data.phaseTwo; marks = data.marks ?? System.Array.Empty<Vector2>();
            if (data.action != InvaderAction.Ready) Begin(data.action, data.remaining);
            if (data.action == InvaderAction.Aiming)
            {
                motor.Face(direction);
                actionLength = BowAimDuration;
            }
        }
        private void OnDestroy() { if (iconSprite != null) Destroy(iconSprite); if (warningSprite != null) Destroy(warningSprite); if (warningTexture != null) Destroy(warningTexture); }
    }
}
