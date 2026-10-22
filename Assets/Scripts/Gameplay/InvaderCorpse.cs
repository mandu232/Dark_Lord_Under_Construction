using DarkLord.Dungeon;
using DarkLord.Minions;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace DarkLord.Gameplay
{
    public sealed class InvaderCorpse : MonoBehaviour
    {
        public InvaderRole Role { get; private set; }
        public bool Consumed { get; private set; }
        public SpriteRenderer Visual { get; private set; }
        private Color baseColor;
        public void Configure(SpriteRenderer source, InvaderRole role)
        {
            Role = role;
            Visual = gameObject.AddComponent<SpriteRenderer>();
            Visual.sprite = source.sprite; Visual.sharedMaterial = source.sharedMaterial;
            Visual.sprite = DeathPose(source) ?? source.sprite;
            Visual.sortingLayerID = source.sortingLayerID; Visual.sortingOrder = source.sortingOrder - 2;
            Visual.flipX = source.flipX; baseColor = new Color(.65f, .42f, .43f, 1); Visual.color = baseColor;
            transform.localScale = source.transform.lossyScale;
            transform.rotation = source.transform.rotation;
        }
        private static Sprite DeathPose(SpriteRenderer source)
        {
            var animator = source.GetComponentInParent<Animator>();
            if (animator == null) animator = source.transform.root.GetComponentInChildren<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null) return null;
            var clip = animator.runtimeAnimatorController.animationClips.FirstOrDefault(c => {
                string name = c.name.ToLowerInvariant(); return name.EndsWith("die") || name.Contains("death") || name.EndsWith("dead"); });
            if (clip == null) return null;
            var root = new GameObject("Sample final death pose");
            var names = new List<string>();
            for (var node = source.transform; node != null && node != animator.transform; node = node.parent) names.Add(node.name);
            var target = root.transform;
            for (int i = names.Count - 1; i >= 0; i--) { var child = new GameObject(names[i]); child.transform.SetParent(target, false); target = child.transform; }
            var renderer = target.gameObject.AddComponent<SpriteRenderer>();
            renderer.enabled = false;
            var evaluator = root.AddComponent<Animator>(); evaluator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var graph = PlayableGraph.Create("Final corpse pose");
            var playable = AnimationClipPlayable.Create(graph, clip);
            var output = AnimationPlayableOutput.Create(graph, "Death pose", evaluator);
            output.SetSourcePlayable(playable); playable.SetTime(Mathf.Max(0, clip.length - .0001f)); graph.Evaluate(0);
            var sprite = renderer.sprite;
            graph.Destroy();
            Destroy(root); return sprite;
        }
        public void SetHeld(bool held)
        { if (Visual != null) { Visual.sortingOrder = held ? 32000 : Mathf.RoundToInt(-transform.position.y * 100); Visual.color = baseColor; } }
        public void Absorb(ResourceBuilding facility)
        {
            if (Consumed) return;
            Consumed = true;
            gameObject.AddComponent<CorpseAbsorptionEffect>().Begin(Visual, facility.transform.position);
        }
    }
}
