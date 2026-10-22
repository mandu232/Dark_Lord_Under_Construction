using UnityEngine;
namespace DarkLord.Gameplay
{
    public sealed class CorpseAbsorptionEffect : MonoBehaviour
    {
        private SpriteRenderer body;
        private readonly SpriteRenderer[] dust = new SpriteRenderer[14];
        private readonly Vector3[] starts = new Vector3[14];
        private Vector3 origin, target, bodyScale;
        private float elapsed;
        private Sprite pixel;
        public void Begin(SpriteRenderer source, Vector3 destination)
        {
            body = source; origin = source.transform.position; target = destination; bodyScale = transform.localScale;
            pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
            for (int i = 0; i < dust.Length; i++)
            {
                var node = new GameObject("Corpse dust", typeof(SpriteRenderer)); node.transform.SetParent(transform.parent, true);
                starts[i] = origin + (Vector3)(Random.insideUnitCircle * .55f);
                node.transform.position = starts[i]; node.transform.localScale = Vector3.one * Random.Range(.06f, .13f);
                dust[i] = node.GetComponent<SpriteRenderer>(); dust[i].sprite = pixel;
                dust[i].sharedMaterial = source.sharedMaterial; dust[i].sortingLayerID = source.sortingLayerID; dust[i].sortingOrder = 32000;
            }
        }
        private void Update()
        {
            if (body == null) { Destroy(gameObject); return; }
            elapsed += Time.deltaTime; float t = Mathf.Clamp01(elapsed / .85f);
            body.color = new Color(.7f, .3f, .35f, Mathf.Clamp01(1 - t * 3));
            transform.position = Vector3.Lerp(origin, target, t * t); transform.localScale = bodyScale * Mathf.Lerp(1, .05f, t);
            for (int i = 0; i < dust.Length; i++)
            {
                float progress = Mathf.Clamp01((t - i * .012f) / .8f);
                dust[i].transform.position = Vector3.Lerp(starts[i], target, progress * progress) + Vector3.up * Mathf.Sin(progress * Mathf.PI) * .45f;
                dust[i].color = Color.Lerp(new Color(.7f, .15f, .24f, 1), new Color(1, .76f, .35f, 0), progress);
            }
            if (t >= 1) Destroy(gameObject);
        }
        private void OnDestroy()
        {
            foreach (var part in dust) if (part != null) Destroy(part.gameObject);
            if (pixel != null) Destroy(pixel);
        }
    }
}
