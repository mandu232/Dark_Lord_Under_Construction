using System;
using DarkLord.Actors;
using UnityEngine;

namespace DarkLord.Minions
{
    public enum InvaderRole { Warrior = 0, Shieldbearer = 1, Healer = 2, Charger = 3 }
    public enum InvaderAction { Ready, Healing, WindingUp, Charging, Recovering }

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
                    motor.SpeedMultiplier = 2.6f;
                    motor.Move(direction); // Motor retains terrain, body-blocking and timed slow rules.
                    if (Time.time < endsAt && HitCharge(target)) Begin(InvaderAction.Recovering, .65f);
                }
                if (Time.time < endsAt) return true;
                if (Action == InvaderAction.WindingUp) { Begin(InvaderAction.Charging, .65f); motor.SpeedMultiplier = 2.6f; return true; }
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
            cue.gameObject.SetActive(action == InvaderAction.WindingUp || action == InvaderAction.Healing);
            cue.color = action == InvaderAction.Healing ? new Color(.2f, 1, .4f, .75f) : new Color(1, .15f, .1f, .7f);
            cue.transform.localPosition = action == InvaderAction.Healing ? new Vector3(0, 1.3f) : (Vector3)(direction * 2.5f);
            cue.transform.localScale = action == InvaderAction.Healing ? new Vector3(.65f, .15f, 1) : new Vector3(5, .18f, 1);
            cue.transform.localRotation = action == InvaderAction.Healing ? Quaternion.identity : Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
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
            int count = Physics2D.OverlapCircle(transform.position, 1.15f, new ContactFilter2D { useTriggers = false }, nearby);
            for (int i = 0; i < count; i++)
            {
                var actor = nearby[i].GetComponentInParent<ActorHealth>();
                if (TryHitCharge(actor)) return true;
            }
            // The dungeon core is a route objective without a physics collider.
            return TryHitCharge(target);
        }
        private bool TryHitCharge(ActorHealth actor)
        {
            if (actor == null || !actor.IsAlive || actor.Faction == health.Faction || actor.gameObject.scene != gameObject.scene || !controller.HasTerrainLineOfSight(actor)) return false;
            Vector2 delta = actor.transform.position - transform.position;
            if (delta.magnitude > 1.15f || Vector2.Dot(direction, delta.normalized) < .2f) return false;
            actor.TakeDamage(22, health, DamageType.Physical);
            motor.Stop(); return true;
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
        private void OnDestroy() { if (iconSprite != null) Destroy(iconSprite); }
    }
}
