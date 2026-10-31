using System;
using DarkLord.Actors;
using UnityEngine;

namespace DarkLord.Minions
{
    public enum InvaderRole { Warrior = 0, Shieldbearer = 1, Healer = 2, Charger = 3 }
    public enum InvaderAction { Ready, Healing, WindingUp, Charging, Recovering, HeroCleave }

    [Serializable]
    public sealed class InvaderTacticsSaveData
    {
        public InvaderRole role;
        public InvaderAction action;
        public float remaining, cooldown;
        public Vector2 direction, healPosition;
    }

    [DisallowMultipleComponent, RequireComponent(typeof(MinionController))]
    public sealed class InvaderTactics : MonoBehaviour
    {
        private MinionController controller;
        private ActorHealth health;
        private TopDownMotor motor;
        private float endsAt, nextAbility;
        private Vector2 direction;
        private ActorHealth healTarget;
        private ActorAnimation presentation;
        private SpriteRenderer heroEffect;
        private Texture2D heroEffectTexture;
        private Sprite heroEffectSprite;
        private float heroEffectUntil;
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
        public void SetHero(bool value) { hero = value; RefreshHeroWarning(); }
        private Vector2 savedHealPosition;
        private readonly Collider2D[] nearby = new Collider2D[96];
        private Sprite iconSprite;
        private SpriteRenderer badge, cross, cue;
        public InvaderRole Role { get; private set; }
        public InvaderAction Action { get; private set; }
        public bool IsPerforming => Action != InvaderAction.Ready;
        public static string RoleName(InvaderRole role) => role == InvaderRole.Shieldbearer ? "방패병" : role == InvaderRole.Healer ? "치유사" : role == InvaderRole.Charger ? "돌격병" : "전사";

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
            if (badge == null) CreateMarkers();
            badge.color = role == InvaderRole.Shieldbearer ? new Color(.35f, .7f, 1) : role == InvaderRole.Healer ? new Color(.3f, 1, .5f) : new Color(1, .3f, .2f);
            badge.transform.localScale = role == InvaderRole.Shieldbearer ? new Vector3(.34f, .42f, 1) : new Vector3(.12f, .4f, 1);
            badge.gameObject.SetActive(role != InvaderRole.Warrior);
            cross.color = badge.color;
            cross.gameObject.SetActive(role == InvaderRole.Healer);
        }
        private float Mitigate(ActorHealth source, DamageType type)
        {
            if (!isActiveAndEnabled || Role != InvaderRole.Shieldbearer || type != DamageType.Physical || source == null) return 1;
            Vector2 delta = source.transform.position - transform.position;
            return delta.sqrMagnitude > .001f && Vector2.Dot(motor.Facing, delta.normalized) >= .5f ? .3f : 1;
        }
        private void Interrupt(ActorHealth source)
        {
            if (controller.IsStaggerImmune) return;
            // Casting can be interrupted; a committed charge is countered by dodging or slowing.
            if (Action == InvaderAction.Healing) { Cancel(); nextAbility = Time.time + 2; }
        }
        private void Cancel()
        {
            Action = InvaderAction.Ready; healTarget = null;
            if (motor != null) motor.IgnoreMinionBlocking = false;
            if (heroEffect != null) heroEffect.enabled = false;
            if (heroWarning != null) heroWarning.enabled = false;
            if (motor != null) { motor.SpeedMultiplier = 1; motor.Stop(); }
            if (cue != null) cue.gameObject.SetActive(false);
        }
        public bool TryAct(ActorHealth target)
        {
            if (!isActiveAndEnabled || !controller.enabled || !health.IsAlive || health.Faction != Faction.Invader || Time.timeScale <= 0) return false;
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
                if (Action == InvaderAction.WindingUp) { Begin(InvaderAction.Charging, hero ? HeroChargeDuration : .65f); motor.SpeedMultiplier = ChargeSpeedMultiplier; return true; }
                if (Action == InvaderAction.HeroCleave)
                {
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
                    Begin(InvaderAction.Recovering, .5f);
                    return true;
                }
                if (Action == InvaderAction.Healing)
                {
                    if (healTarget == null) healTarget = FindHealTarget(true);
                    if (CanHeal(healTarget)) healTarget.Heal(18);
                }
                Cancel();
                return true;
            }
            if (Time.time < nextAbility) return false;
            nextAbility = Time.time + .2f;
            if (Role == InvaderRole.Healer)
            {
                healTarget = FindHealTarget(false);
                if (healTarget == null) return false;
                savedHealPosition = healTarget.transform.position;
                nextAbility = Time.time + 4;
                Begin(InvaderAction.Healing, .8f);
                motor.Stop();
                return true;
            }
            if (hero && target != null && target.IsAlive && controller.HasTerrainLineOfSight(target))
            {
                Vector2 delta = target.transform.position - transform.position;
                if (delta.sqrMagnitude > .001f && delta.magnitude <= HeroCleaveRadius)
                {
                    direction = delta.normalized; motor.Face(direction); motor.Stop();
                    nextAbility = Time.time + 3f;
                    Begin(InvaderAction.HeroCleave, .6f);
                    return true;
                }
            }
            if (Role == InvaderRole.Charger && target != null && target.IsAlive && controller.HasTerrainLineOfSight(target))
            {
                Vector2 delta = target.transform.position - transform.position;
                if (delta.magnitude < 1.3f || delta.magnitude > 6) return false;
                direction = delta.normalized; motor.Face(direction); motor.Stop();
                nextAbility = Time.time + 6;
                Begin(InvaderAction.WindingUp, .75f);
                return true;
            }
            return false;
        }
        private void Begin(InvaderAction action, float duration)
        {
            Action = action; endsAt = Time.time + duration;
            motor.SpeedMultiplier = 1;
            if (action != InvaderAction.Charging) motor.IgnoreMinionBlocking = false;
            if (action == InvaderAction.Charging)
            {
                chargeHits.Clear();
                if (hero) { presentation.Present(ActorState.Run, direction); ShowHeroEffect(true); }
            }
            cue.gameObject.SetActive(action == InvaderAction.WindingUp || action == InvaderAction.Healing || action == InvaderAction.HeroCleave);
            cue.color = action == InvaderAction.Healing ? new Color(.2f, 1, .4f, .75f) : new Color(1, .15f, .1f, .7f);
            cue.transform.localPosition = action == InvaderAction.Healing ? new Vector3(0, 1.3f) : (Vector3)(direction * 2.5f);
            cue.transform.localScale = action == InvaderAction.Healing ? new Vector3(.65f, .15f, 1) : new Vector3(5, .18f, 1);
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
                (Action == InvaderAction.HeroCleave || Action == InvaderAction.WindingUp);
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
                    if (Action == InvaderAction.HeroCleave)
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
            bool cleave = Action == InvaderAction.HeroCleave;
            float width = cleave ? HeroCleaveRadius * 2 : distance + ChargeHitRadius * 2;
            float height = cleave ? HeroCleaveRadius * 2 : ChargeHitRadius * 2;
            // Sprite bounds are 2 by 1 units. Compensate for Rowen's enlarged model.
            heroWarning.transform.localScale = new Vector3(width / (2 * xScale), height / yScale, 1);
            heroWarning.transform.localPosition = cleave ? Vector3.zero :
                new Vector3(direction.x * distance * .5f / xScale, direction.y * distance * .5f / yScale, 0);
        }
        private void ShowHeroEffect(bool charge)
        {
            if (heroEffect == null)
            {
                heroEffectTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                heroEffectTexture.filterMode = FilterMode.Bilinear;
                var pixels = new Color[64 * 64];
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                {
                    float dx = (x - 31.5f) / 31f, dy = (y - 31.5f) / 31f;
                    float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = dx >= 0 && radius < .96f && radius > .72f ? Mathf.Clamp01((.96f - radius) * 15f) : 0;
                    pixels[y * 64 + x] = new Color(1f, .88f, .45f, alpha * .8f);
                }
                heroEffectTexture.SetPixels(pixels); heroEffectTexture.Apply();
                heroEffectSprite = Sprite.Create(heroEffectTexture, new Rect(0, 0, 64, 64), Vector2.one * .5f, 32);
                heroEffect = Marker("Rowen attack effect", Vector3.zero, Vector3.one);
                heroEffect.sprite = heroEffectSprite;
            }
            heroEffect.enabled = true;
            heroEffect.color = charge ? new Color(.65f, .9f, 1f, .9f) : Color.white;
            heroEffect.transform.localPosition = (Vector3)(direction * (charge ? .7f : 1.1f));
            heroEffect.transform.localScale = charge ? new Vector3(1.5f, .65f, 1) : new Vector3(2f, 2f, 1);
            heroEffect.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            heroEffectUntil = Time.time + (charge ? .35f : .25f);
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
            if (heroEffect != null && Time.time >= heroEffectUntil) heroEffect.enabled = false;
            RefreshHeroWarning();
            if (Role != InvaderRole.Shieldbearer || cue == null) return;
            cue.gameObject.SetActive(health.IsAlive && controller.enabled);
            cue.color = new Color(.35f, .7f, 1, .85f);
            cue.transform.localPosition = (Vector3)(motor.Facing * .65f);
            cue.transform.localScale = new Vector3(.12f, .65f, 1);
            cue.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(motor.Facing.y, motor.Facing.x) * Mathf.Rad2Deg);
        }
        public InvaderTacticsSaveData CaptureSave() => new InvaderTacticsSaveData { role = Role, action = Action,
            remaining = IsPerforming ? Mathf.Max(0, endsAt - Time.time) : 0, cooldown = Mathf.Max(0, nextAbility - Time.time), direction = direction,
            healPosition = healTarget != null ? (Vector2)healTarget.transform.position : savedHealPosition };
        public void RestoreSave(InvaderTacticsSaveData data)
        {
            Configure(data != null ? data.role : InvaderRole.Warrior);
            if (data == null) return;
            direction = data.direction.normalized; savedHealPosition = data.healPosition;
            nextAbility = Time.time + data.cooldown;
            if (data.action != InvaderAction.Ready) Begin(data.action, data.remaining);
        }
        private void OnDestroy() { if (iconSprite != null) Destroy(iconSprite); if (heroEffectSprite != null) Destroy(heroEffectSprite); if (heroEffectTexture != null) Destroy(heroEffectTexture); if (warningSprite != null) Destroy(warningSprite); if (warningTexture != null) Destroy(warningTexture); }
    }
}
