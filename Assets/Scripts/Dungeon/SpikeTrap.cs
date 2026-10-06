using System.Collections.Generic;
using DarkLord.Actors;
using UnityEngine;

namespace DarkLord.Dungeon
{
    public sealed class SpikeTrap : MonoBehaviour
    {
        [SerializeField, Min(1)] private int damage = 12;
        [SerializeField, Min(0.1f)] private float cooldown = 1f;
        private readonly Dictionary<ActorHealth, float> hits = new Dictionary<ActorHealth, float>();
        private void OnTriggerExit2D(Collider2D other)
        {
            var target = other.GetComponentInParent<ActorHealth>();
            if (target != null) hits.Remove(target);
        }
        private void OnTriggerStay2D(Collider2D other)
        {
            if (Time.timeScale == 0) return;
            var target = other.GetComponentInParent<ActorHealth>();
            if (target == null || !target.IsAlive || target.Faction != Faction.Invader) return;
            if (hits.TryGetValue(target, out float next) && Time.time < next) return;
            hits[target] = Time.time + cooldown;
            target.TakeDamage(damage);
        }
    }
}
