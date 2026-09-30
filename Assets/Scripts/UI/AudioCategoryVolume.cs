using UnityEngine;

namespace DarkLord.UI
{
    public enum AudioCategory { Character, Music, Effects }

    // Attach beside an AudioSource. Its authored volume remains the base gain.
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class AudioCategoryVolume : MonoBehaviour
    {
        [SerializeField] private AudioCategory category = AudioCategory.Effects;
        private AudioSource source;
        private float baseVolume;

        private void Awake()
        {
            source = GetComponent<AudioSource>();
            baseVolume = source.volume;
        }

        private void OnEnable()
        {
            GameSettings.AudioVolumesApplied += Apply;
            Apply(GameSettings.Load());
        }

        private void OnDisable()
        {
            GameSettings.AudioVolumesApplied -= Apply;
            source.volume = baseVolume;
        }

        public void SetCategory(AudioCategory value)
        {
            category = value;
            if (isActiveAndEnabled) Apply(GameSettings.Load());
        }

        private void Apply(GameSettings settings)
        {
            float volume = category == AudioCategory.Character ? settings.CharacterVolume :
                category == AudioCategory.Music ? settings.MusicVolume : settings.EffectsVolume;
            source.volume = baseVolume * volume;
        }
    }
}
