using DarkLord.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DarkLord.UI
{
    // Lives on the HUD so hiding the button does not stop its visibility updates.
    public sealed class WaveStartButtonPresentation : MonoBehaviour
    {
        private WaveDirector director;
        private Button button;
        private TMP_Text label;
        private DarkLord.Dungeon.DungeonExpansion construction;

        public void Configure(WaveDirector source, Button target, TMP_FontAsset font)
        {
            director = source; button = target;
            foreach (var candidate in FindObjectsByType<DarkLord.Dungeon.DungeonExpansion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (candidate.gameObject.scene == gameObject.scene) { construction = candidate; break; }
            label = button.GetComponentInChildren<TMP_Text>(true);
            var oldFrame = button.transform.Find("Castle Frame");
            if (oldFrame != null && oldFrame.TryGetComponent<Image>(out var oldImage)) oldImage.enabled = false;
            var skin = Resources.Load<RewardUiSkin>("WaveRewardSkin");
            var image = button.GetComponent<Image>();
            if (image != null && skin != null && skin.waveStartButton != null)
            {
                image.sprite = skin.waveStartButton; image.type = Image.Type.Sliced; image.color = Color.white;
                var canvas = button.GetComponentInParent<Canvas>();
                image.pixelsPerUnitMultiplier = (canvas != null ? canvas.referencePixelsPerUnit : 100) / (image.sprite.pixelsPerUnit * 2);
            }
            button.GetComponent<RectTransform>().sizeDelta = new Vector2(320,64);
            if (label != null) { label.font = font; label.fontSize = 20; label.color = new Color32(250,226,207,255); }
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white; colors.highlightedColor = new Color(1,.85f,.85f);
            colors.pressedColor = new Color(.7f,.5f,.55f); colors.disabledColor = new Color(.5f,.5f,.5f,.65f);
            button.colors = colors;
            button.onClick.AddListener(StartWave);
            Refresh();
        }

        private void LateUpdate() => Refresh();
        private void Refresh()
        {
            if (button == null || director == null) return;
            bool tutorial = director.Tutorial != null && director.Tutorial.Active;
            button.gameObject.SetActive(!director.AutomaticWaves && director.State == WaveState.Preparing && director.HasNextWave && !tutorial);
            var summoning = director.Lord != null ? director.Lord.GetComponent<DarkLord.Player.MinionSummoning>() : null;
            button.interactable = director.CanStartWave && !(summoning != null && summoning.BlocksGameplayInput) &&
                !(director.Alchemy != null && director.Alchemy.BlocksGameplayInput) &&
                !(director.Economy != null && director.Economy.BlocksGameplayInput) &&
                !(construction != null && construction.ExpansionMode);
            if (label != null) label.text = $"웨이브 {director.CurrentWave + 1} 시작";
        }
        private void StartWave() { if (button != null && button.interactable) director.BeginNextWave(); }
        private void OnDestroy() { if (button != null) button.onClick.RemoveListener(StartWave); }
    }
}
