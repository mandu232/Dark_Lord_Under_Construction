using System.Collections.Generic;
using DarkLord.Actors;
using DarkLord.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkLord.Minions
{
    // Rowen's sword wave: flies straight, hits each enemy once with a shove, stops at walls or after its range.
    [DisallowMultipleComponent]
    public sealed class SwordWave : MonoBehaviour
    {
        private const float Speed = 9, Range = 10, Radius = .75f;
        private static readonly Collider2D[] Nearby = new Collider2D[32];
        private readonly HashSet<ActorHealth> hit = new HashSet<ActorHealth>();
        private ActorHealth source;
        private LayerMask walls;
        private Vector2 heading;
        private float travelled;
        private int damage;
        private SpriteRenderer body;
        private GameplayEffectLibrary.Clip clip;

        public static void Launch(ActorHealth source, LayerMask walls, Vector2 from, Vector2 heading, int damage)
        {
            if (source == null || heading.sqrMagnitude < .0001f) return;
            var node = new GameObject("Holy sword wave");
            SceneManager.MoveGameObjectToScene(node, source.gameObject.scene);
            node.transform.position = from;
            node.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(heading.y, heading.x) * Mathf.Rad2Deg);
            var wave = node.AddComponent<SwordWave>();
            wave.source = source; wave.walls = walls; wave.heading = heading.normalized; wave.damage = damage;
            wave.body = node.AddComponent<SpriteRenderer>();
            var library = Resources.Load<GameplayEffectLibrary>("GameplayEffects");
            wave.clip = library != null ? library.Find(GameplayEffectKind.HolyWave) : null;
            if (library != null && library.material != null) wave.body.sharedMaterial = library.material;
            if (wave.clip != null && wave.clip.frames != null && wave.clip.frames.Length > 0)
                node.transform.localScale = Vector3.one * (wave.clip.width / wave.clip.frames[0].bounds.size.x);
        }

        private void Update()
        {
            if (source == null) { Destroy(gameObject); return; }
            float step = Speed * Time.deltaTime;
            Vector2 position = transform.position;
            if (step > 0 && Physics2D.Linecast(position, position + heading * step, walls)) { Destroy(gameObject); return; }
            position += heading * step; travelled += step;
            transform.position = position;
            int count = Physics2D.OverlapCircle(position, Radius, new ContactFilter2D { useTriggers = false }, Nearby);
            for (int i = 0; i < count; i++)
            {
                var victim = Nearby[i].GetComponentInParent<ActorHealth>();
                if (victim == null || !victim.IsAlive || victim.Faction == source.Faction || victim.gameObject.scene != gameObject.scene || !hit.Add(victim)) continue;
                if (victim.TakeDamage(damage, source, DamageType.Physical) && victim.IsAlive && victim.GetComponent<MinionController>()?.IsStaggerImmune != true)
                    victim.GetComponent<TopDownMotor>()?.ApplyKnockback(heading, 5f, .2f);
            }
            if (clip != null && clip.frames != null && clip.frames.Length > 0)
            {
                body.sprite = clip.frames[Mathf.FloorToInt(Mathf.Repeat(Time.time / clip.duration, 1) * clip.frames.Length) % clip.frames.Length];
                body.sortingOrder = Mathf.Clamp(-Mathf.RoundToInt(position.y * 16) + 8, -30000, 30000);
                body.color = new Color(1, 1, 1, Mathf.Clamp01((Range - travelled) / 2f));
            }
            if (travelled >= Range) Destroy(gameObject);
        }
    }
}
