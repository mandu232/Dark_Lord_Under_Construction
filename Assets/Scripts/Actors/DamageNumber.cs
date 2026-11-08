using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DarkLord.Actors
{
    // A pooled number that pops off a struck character. Bounded so a large fight cannot pile up objects.
    public sealed class DamageNumber : MonoBehaviour
    {
        private const int Limit = 48;
        private const float Life = .7f;
        private static readonly Stack<DamageNumber> pool = new Stack<DamageNumber>();
        private static int live;
        private TextMeshPro text;
        private Color color;
        private Vector3 drift;
        private float age, size;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetPool() { pool.Clear(); live = 0; }

        public static void Show(Component owner, int amount, Color color, float size)
        {
            if (!Application.isPlaying || amount <= 0 || live >= Limit) return;
            DamageNumber number = null;
            while (pool.Count > 0 && number == null) number = pool.Pop(); // Pooled numbers die with their scene.
            if (number == null)
            {
                var node = new GameObject("Damage number");
                number = node.AddComponent<DamageNumber>();
                number.text = node.AddComponent<TextMeshPro>();
                number.text.alignment = TextAlignmentOptions.Center;
                number.text.fontStyle = FontStyles.Bold;
                number.text.textWrappingMode = TextWrappingModes.NoWrap;
                number.text.outlineWidth = .25f;
                number.text.outlineColor = Color.black;
                number.text.sortingOrder = 30000;
            }
            if (number.gameObject.scene != owner.gameObject.scene) SceneManager.MoveGameObjectToScene(number.gameObject, owner.gameObject.scene);
            number.gameObject.SetActive(true);
            number.transform.position = owner.transform.position + new Vector3(Random.Range(-.25f, .25f), .9f, 0);
            number.text.text = amount.ToString();
            number.color = color; number.size = size; number.age = 0;
            number.drift = new Vector3(Random.Range(-.6f, .6f), 2.2f, 0);
            live++;
            number.Present();
        }

        private void Update()
        {
            age += Time.deltaTime;
            if (age >= Life) { gameObject.SetActive(false); live--; pool.Push(this); return; }
            transform.position += drift * Time.deltaTime;
            drift.y = Mathf.Max(.2f, drift.y - 5 * Time.deltaTime);
            Present();
        }

        private void Present()
        {
            // Pop in large, settle, then fade over the last third.
            text.fontSize = size * (1 + .5f * Mathf.Clamp01(1 - age / .1f));
            text.color = new Color(color.r, color.g, color.b, Mathf.Clamp01((Life - age) / (Life / 3)));
        }

        private void OnDestroy() { if (gameObject.activeSelf) live = Mathf.Max(0, live - 1); }
    }
}
