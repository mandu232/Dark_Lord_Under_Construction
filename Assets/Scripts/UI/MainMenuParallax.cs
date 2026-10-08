using UnityEngine;
using UnityEngine.InputSystem;

namespace DarkLord.UI
{
    [DisallowMultipleComponent]
    public sealed class MainMenuParallax : MonoBehaviour
    {
        [SerializeField] private RectTransform background;
        [SerializeField] private RectTransform foreground;
        [SerializeField, Range(0, 30)] private float backgroundTravel = 5;
        [SerializeField, Range(0, 30)] private float foregroundTravel = 14;
        [SerializeField, Min(0.01f)] private float response = 4;
        private Vector2 offset;
        private Vector2 backgroundOrigin, foregroundOrigin;

        private void Awake()
        {
            if (background != null) backgroundOrigin = background.anchoredPosition;
            if (foreground != null) foregroundOrigin = foreground.anchoredPosition;
        }

        private void LateUpdate()
        {
            var target = Vector2.zero;
            if (Application.isFocused && Mouse.current != null && Screen.width > 0 && Screen.height > 0)
            {
                var pointer = Mouse.current.position.ReadValue();
                if (pointer.x >= 0 && pointer.x <= Screen.width && pointer.y >= 0 && pointer.y <= Screen.height)
                    target = new Vector2(pointer.x / Screen.width * 2 - 1, pointer.y / Screen.height * 2 - 1);
            }
            offset = Vector2.Lerp(offset, target, 1 - Mathf.Exp(-response * Time.unscaledDeltaTime));
            if (background != null) background.anchoredPosition = backgroundOrigin - offset * backgroundTravel;
            if (foreground != null) foreground.anchoredPosition = foregroundOrigin - offset * foregroundTravel;
        }

        private void OnDisable()
        {
            offset = Vector2.zero;
            if (background != null) background.anchoredPosition = backgroundOrigin;
            if (foreground != null) foreground.anchoredPosition = foregroundOrigin;
        }
    }
}
