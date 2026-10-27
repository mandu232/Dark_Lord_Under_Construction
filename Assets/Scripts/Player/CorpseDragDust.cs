using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkLord.Player
{
    // A small fixed pool keeps the trail transient without spawning objects every step.
    [DisallowMultipleComponent]
    public sealed class CorpseDragDust : MonoBehaviour
    {
        private const int PoolSize = 8;
        private const float Lifetime = .38f;
        private const float StepDistance = .28f;
        private readonly SpriteRenderer[] particles = new SpriteRenderer[PoolSize];
        private readonly Vector3[] origins = new Vector3[PoolSize];
        private readonly float[] remaining = new float[PoolSize];
        private Sprite sprite;
        private GameObject root;
        private float distance;
        private int next;

        public void Begin(SpriteRenderer body)
        {
            distance = 0;
            if (body == null) return;
            if (root == null)
            {
                root = new GameObject("Corpse drag dust");
                SceneManager.MoveGameObjectToScene(root, gameObject.scene);
                sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
                for (int i = 0; i < PoolSize; i++)
                {
                    var node = new GameObject("Dust particle", typeof(SpriteRenderer));
                    node.transform.SetParent(root.transform, false);
                    particles[i] = node.GetComponent<SpriteRenderer>();
                    particles[i].sprite = sprite;
                    particles[i].enabled = false;
                }
            }
            foreach (var particle in particles)
            {
                particle.sharedMaterial = body.sharedMaterial;
                particle.sortingLayerID = body.sortingLayerID;
                particle.sortingOrder = body.sortingOrder - 1;
            }
        }

        public void Record(Vector3 from, Vector3 to, bool moving)
        {
            if (!moving || root == null) { distance = 0; return; }
            float travelled = Vector2.Distance(from, to);
            distance += travelled;
            if (distance < StepDistance || travelled < .0001f) return;
            distance %= StepDistance;
            Vector2 direction = ((Vector2)(to - from)).normalized;
            Vector2 side = new Vector2(-direction.y, direction.x);
            Spawn(to + (Vector3)(side * .14f));
            Spawn(to - (Vector3)(side * .14f));
        }

        private void Spawn(Vector3 origin)
        {
            int index = next++ % PoolSize;
            origins[index] = origin;
            remaining[index] = Lifetime;
            particles[index].enabled = true;
            particles[index].transform.position = origin;
            particles[index].transform.localScale = new Vector3(.11f, .07f, 1);
            particles[index].color = new Color(.72f, .63f, .51f, .55f);
        }

        private void LateUpdate()
        {
            if (root == null || Time.deltaTime <= 0) return;
            for (int i = 0; i < PoolSize; i++)
            {
                if (remaining[i] <= 0) continue;
                remaining[i] = Mathf.Max(0, remaining[i] - Time.deltaTime);
                float t = 1 - remaining[i] / Lifetime;
                particles[i].enabled = remaining[i] > 0;
                particles[i].transform.position = origins[i] + Vector3.up * (.18f * t);
                particles[i].transform.localScale = new Vector3(.11f + .06f * t, .07f + .04f * t, 1);
                particles[i].color = new Color(.72f, .63f, .51f, .55f * (1 - t));
            }
        }

        private void OnDisable()
        {
            distance = 0;
            for (int i = 0; i < PoolSize; i++)
            { remaining[i] = 0; if (particles[i] != null) particles[i].enabled = false; }
        }
        private void OnDestroy()
        {
            if (root != null) Destroy(root);
            if (sprite != null) Destroy(sprite);
        }
    }
}
