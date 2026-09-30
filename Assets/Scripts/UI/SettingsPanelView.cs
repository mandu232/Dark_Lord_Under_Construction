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
                categoryButtons[i].colors = colors;
                categoryButtons[i].image.color = i == index
                    ? new Color32(155, 62, 51, 255) : new Color32(70, 54, 84, 255);
            }
        }

        private IEnumerator AnimateOpen()
        {
            const float duration = 0.24f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = 1f - Mathf.Pow(1f - elapsed / duration, 3f);
                group.alpha = t;
                rect.localScale = restingScale * Mathf.Lerp(0.96f, 1f, t);
                rect.anchoredPosition = restingPosition + Vector2.down * (24f * (1f - t));
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
        }
    }
}
