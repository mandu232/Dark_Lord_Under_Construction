using UnityEngine;

namespace DarkLord.UI
{
    [DisallowMultipleComponent, RequireComponent(typeof(UnityEngine.UI.Image))]
    public sealed class MenuLightAnimation : MonoBehaviour
    {
        private static readonly int Clock = Shader.PropertyToID("_UnscaledTime");
        private UnityEngine.UI.Image image;
        private Material source, instance;
        private void Awake()
        {
            image = GetComponent<UnityEngine.UI.Image>();
            source = image.material;
            instance = new Material(source);
            image.material = instance;
        }
        private void Update()
        {
            if (instance != null) instance.SetFloat(Clock, Time.unscaledTime);
        }
        private void OnDestroy()
        {
            if (image != null) image.material = source;
            if (instance != null) Destroy(instance);
        }
    }
}
