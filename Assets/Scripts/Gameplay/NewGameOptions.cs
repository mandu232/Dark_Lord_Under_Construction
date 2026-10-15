using System;
using UnityEngine;

namespace DarkLord.Gameplay
{
    public enum GameDifficulty { Normal, Easy, Hard, Nightmare }

    [Serializable]
    public sealed class NewGameOptions
    {
        public GameDifficulty difficulty;
        public string saveName = "";
        public bool skipOpening;

        public float EnemyCountMultiplier => difficulty == GameDifficulty.Easy ? .75f : difficulty == GameDifficulty.Hard ? 1.35f : difficulty == GameDifficulty.Nightmare ? 1.7f : 1f;
        public float EnemyHealthMultiplier => difficulty == GameDifficulty.Easy ? .75f : difficulty == GameDifficulty.Hard ? 1.4f : difficulty == GameDifficulty.Nightmare ? 1.8f : 1f;
        public float RewardMultiplier => difficulty == GameDifficulty.Easy ? 1.2f : difficulty == GameDifficulty.Hard ? 1.3f : difficulty == GameDifficulty.Nightmare ? 1.6f : 1f;
        public string DifficultyName => difficulty == GameDifficulty.Easy ? "쉬움" : difficulty == GameDifficulty.Hard ? "어려움" : difficulty == GameDifficulty.Nightmare ? "악몽" : "보통";
        public NewGameOptions Copy() => new NewGameOptions { difficulty = difficulty, saveName = NormalizeName(saveName), skipOpening = skipOpening };
        public bool IsValid => Enum.IsDefined(typeof(GameDifficulty), difficulty);
        public static string NormalizeName(string value)
        {
            value = (value ?? "").Trim();
            value = new string(Array.FindAll(value.ToCharArray(), c => !char.IsControl(c)));
            return value.Length > 24 ? value.Substring(0, 24) : value;
        }
        public int EnemyCount(int value) => Mathf.Max(1, Mathf.RoundToInt(value * EnemyCountMultiplier));
        public int EnemyHealth(int value) => Mathf.Max(1, Mathf.RoundToInt(value * EnemyHealthMultiplier));
        public int Reward(int value) => Mathf.Max(0, Mathf.RoundToInt(value * RewardMultiplier));
    }
}
