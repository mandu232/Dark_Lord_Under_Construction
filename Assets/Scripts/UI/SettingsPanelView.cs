using System.Collections;
using UnityEngine;

namespace DarkLord.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class SettingsPanelView : MonoBehaviour
    {
        [SerializeField] private GameObject[] pages;
        [SerializeField] private UnityEngine.UI.Button[] categoryButtons;
        [SerializeField] private bool fullScreenPresentation;
        [SerializeField] private bool externalPresentation;
        [SerializeField] private CanvasGroup[] openingContent = new CanvasGroup[0];
        private CanvasGroup group;
        private RectTransform rect;
        private Vector2 restingPosition;
        private Vector3 restingScale;
        private Coroutine opening;
        public int SelectedCategory { get; private set; }

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            rect = (RectTransform)transform;
            restingPosition = rect.anchoredPosition;
            restingScale = rect.localScale;
        }

        private void OnEnable()
        {
            SelectCategory(0);
            if (externalPresentation) { ResetPresentation(); return; }
            if (fullScreenPresentation)
            {
                ResetPresentation();
                opening = StartCoroutine(AnimateContent());
                return;
            }
            opening = StartCoroutine(AnimateOpen());
        }

        public void SelectCategory(int index)
        {
            if (pages == null || index < 0 || index >= pages.Length) return;
            SelectedCategory = index;
            for (int i = 0; i < pages.Length; i++)
            {
                pages[i].SetActive(i == index);
                var colors = categoryButtons[i].colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1);
                colors.selectedColor = colors.highlightedColor;
                colors.pressedColor = new Color(.85f, .85f, .85f, 1);
                categoryButtons[i].colors = colors;
                categoryButtons[i].image.color = i == index
                    ? Color.white : new Color(.62f, .58f, .65f, 1);
            }
        }

        private IEnumerator AnimateContent()
        {
            // Fade the artwork and controls together, using real time while paused.
            const float duration = .3f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.SmoothStep(0, 1, elapsed / duration);
                group.alpha = t;
                yield return null;
            }
            ResetPresentation();
            opening = null;
        }

        private IEnumerator AnimateOpen()
        {
            const float duration = 0.24f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = 1f - Mathf.Pow(1f - elapsed / duration, 3f);
                group.alpha = t;
                rect.localScale = restingScale;
                rect.anchoredPosition = restingPosition + Vector2.down * (Mathf.Round(24f * (1f - t) / 4f) * 4f);
                yield return null;
            }
            ResetPresentation();
            opening = null;
        }

        private void OnDisable()
        {
            if (opening != null) StopCoroutine(opening);
            opening = null;
            if (rect != null) ResetPresentation();
        }

        private void ResetPresentation()
        {
            group.alpha = 1;
            rect.localScale = restingScale;
            rect.anchoredPosition = restingPosition;
            foreach (var content in openingContent)
                if (content != null) content.alpha = 1;
        }
    }
}
