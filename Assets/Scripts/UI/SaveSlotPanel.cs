using System;
using DarkLord.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace DarkLord.UI
{
    public enum SaveSlotMode { New, Continue, Save }
    public sealed class SaveSlotPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text title, message, overwriteDescription;
        [SerializeField] private Button[] slots;
        [SerializeField] private TMP_Text[] slotLabels;
        [SerializeField] private GameObject overwritePanel;
        private GameMenu owner;
        public void Bind(GameMenu menu) => owner=menu;
        public void Refresh(SaveSlotMode mode)
        {
            title.text=mode==SaveSlotMode.New?"새 게임 · 저장 공간 선택":mode==SaveSlotMode.Continue?"이어하기 · 저장 공간 선택":"저장 공간 선택";
            message.text="";overwritePanel.SetActive(false);
            for(int i=0;i<3;i++)
            {
                bool valid=SaveSlotStore.Default.TryRead(i,out var data,out var error);
                slots[i].interactable=mode!=SaveSlotMode.Continue || valid;
                string name = valid && !string.IsNullOrWhiteSpace(data.options?.saveName) ? data.options.saveName : $"저장 공간 {i+1}";
                if (valid)
                {
                    string difficulty = (data.options ?? new DarkLord.Gameplay.NewGameOptions()).DifficultyName;
                    string playedDate = DateTimeOffset.TryParse(data.savedAtUtc, out var date)
                        ? date.ToLocalTime().ToString("yyyy-MM-dd") : "날짜 정보 없음";
                    var duration = TimeSpan.FromSeconds(data.playedSeconds);
                    string playedTime = $"{(long)duration.TotalHours:00}:{duration.Minutes:00}:{duration.Seconds:00}";
                    slotLabels[i].text = $"{name}\n\n난이도: {difficulty}\n\n{playedDate}\n웨이브: {data.wave.number}\n{playedTime}";
                }
                else
                {
                    string status = SaveSlotStore.Default.Exists(i) ? "읽을 수 없는 저장 기록" : "빈 저장 공간";
                    slotLabels[i].text = $"{name}\n{status}";
                }
                slotLabels[i].richText = false;
            }
        }
        public void Error(string text) { overwritePanel.SetActive(false); message.text=text; }
        public Button Focus
        {
            get
            {
                if(overwritePanel.activeSelf)return overwritePanel.GetComponentInChildren<Button>();
                foreach(var slot in slots)if(slot.interactable)return slot;
                return null;
            }
        }
        private void FocusButton()
        { if(UnityEngine.EventSystems.EventSystem.current!=null && Focus!=null)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(Focus.gameObject); }
        public bool CancelOverwriteIfOpen()
        { if(!overwritePanel.activeSelf)return false;CancelOverwrite();return true; }
        public void Overwrite(int slot, bool newGame)
        { overwriteDescription.text=$"저장 공간 {slot+1}에 기록이 있습니다.\n"+(newGame?"이 공간에 새 게임을 저장하면 기존 기록이 덮어써집니다.":"현재 진행 상황으로 덮어쓸까요?");overwritePanel.SetActive(true); FocusButton(); }
        public void SelectFirst()=>owner?.SelectSlot(0);
        public void SelectSecond()=>owner?.SelectSlot(1);
        public void SelectThird()=>owner?.SelectSlot(2);
        public void Cancel()=>owner?.CancelSlots();
        public void ConfirmOverwrite()=>owner?.ConfirmSlot();
        public void CancelOverwrite(){overwritePanel.SetActive(false);FocusButton();}
    }
}

