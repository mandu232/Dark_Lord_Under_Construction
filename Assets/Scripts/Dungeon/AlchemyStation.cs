using DarkLord.Gameplay;
using DarkLord.Persistence;
using DarkLord.Player;
using UnityEngine;

namespace DarkLord.Dungeon
{
    [DisallowMultipleComponent]
    public sealed class AlchemyStation : MonoBehaviour
    {
        private CurrencyWallet wallet;
        private WaveDirector waves;
        private CurrencyType source;
        private bool hasJob;
        private float remaining;
        public Vector2Int Cell { get; private set; }
        public bool HasJob => hasJob;
        public bool IsReady => hasJob && remaining <= 0;
        public float Remaining => remaining;
        public float Duration => AlchemyExchange.DurationFor(source);
        public int GoldOutput => hasJob ? AlchemyExchange.GoldFor(source) : 0;
        public string Message { get; private set; } = "연성할 재료를 선택하세요.";
        public void Bind(Vector2Int cell, CurrencyWallet resources, WaveDirector director)
        { Cell = cell; wallet = resources; waves = director; }
        public bool CanStart(CurrencyType material, out string reason)
        {
            reason = "준비 단계에 연성을 시작할 수 있습니다.";
            if (!isActiveAndEnabled || wallet == null || waves == null || waves.State != WaveState.Preparing || waves.IsFinished || Time.timeScale <= 0) return false;
            if (hasJob) { reason = IsReady ? "완료된 결과물을 먼저 수령하세요." : "이미 연성 중입니다."; return false; }
            if (material != CurrencyType.Souls && material != CurrencyType.DarkCrystals) { reason = "혈액이나 혈석을 선택하세요."; return false; }
            if (!wallet.CanAfford(material, AlchemyExchange.CostFor(material))) { reason = "연성 재료가 부족합니다."; return false; }
            if ((long)wallet.Gold + AlchemyExchange.GoldFor(material) > wallet.GetCapacity(CurrencyType.Gold))
            { reason = "금화 저장 공간이 부족합니다. 금화를 사용하거나 창고를 건설하세요."; return false; }
            reason = "연성을 시작할 수 있습니다."; return true;
        }
        public bool TryStart(CurrencyType material, out string reason)
        {
            if (!CanStart(material, out reason)) return false;
            source = material; remaining = AlchemyExchange.DurationFor(material); hasJob = true;
            if (!wallet.TrySpend(material, AlchemyExchange.CostFor(material))) { hasJob = false; remaining = 0; return false; }
            Message = "재료를 투입했습니다. 자리를 떠나도 연성은 계속됩니다."; return true;
        }
        private void Update() => AdvanceWork(Time.deltaTime);
        public void AdvanceWork(float seconds)
        {
            if (!hasJob || IsReady || !isActiveAndEnabled || wallet == null || waves == null || waves.IsFinished || Time.timeScale <= 0 ||
                seconds < 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            remaining = Mathf.Max(0, remaining - seconds);
            if (remaining <= 0) Message = "연성 완료 · 장치와 상호작용하여 금화를 수령하세요.";
        }
        public bool CanCollect(out string reason)
        {
            reason = "지금은 수령할 수 없습니다.";
            if (!isActiveAndEnabled || wallet == null || waves == null || waves.IsFinished || Time.timeScale <= 0) return false;
            if (!IsReady) { reason = hasJob ? "아직 연성 중입니다." : "수령할 결과물이 없습니다."; return false; }
            int reward = AlchemyExchange.GoldFor(source);
            if ((long)wallet.Gold + reward > wallet.GetCapacity(CurrencyType.Gold))
            { reason = "금화 저장 공간이 부족합니다. 결과물은 장치에 보관됩니다."; return false; }
            reason = $"금화 {reward}을 수령할 수 있습니다."; return true;
        }
        public bool TryCollect(out string reason)
        {
            if (!CanCollect(out reason)) return false;
            int reward = AlchemyExchange.GoldFor(source);
            // Observers must never see both credited gold and an unpaid job in the same save.
            hasJob = false;
            if (!wallet.TryReceiveGoldExact(reward)) { hasJob = true; reason = "금화 저장 공간을 확인하세요."; return false; }
            Message = $"금화 {reward} 수령 완료"; return true;
        }
        public AlchemyJobSaveData CaptureJob() => hasJob ? new AlchemyJobSaveData { cell = Cell, source = source, remaining = remaining } : null;
        public void RestoreJob(AlchemyJobSaveData saved)
        {
            hasJob = saved != null; source = saved != null ? saved.source : CurrencyType.Souls; remaining = saved != null ? saved.remaining : 0;
            Message = !hasJob ? "연성할 재료를 선택하세요." : remaining > 0 ? "연성 중입니다. 자리를 떠나도 작업은 계속됩니다." : "연성 완료 · 장치와 상호작용하여 금화를 수령하세요.";
        }
    }
}
