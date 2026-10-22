using DarkLord.Player;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkLord.UI
{
    public sealed class TechNodePointer : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler
    {
        private CoreTechPanel panel;private TechId id;
        public void Configure(CoreTechPanel owner,TechId technology){panel=owner;id=technology;}
        public void OnPointerEnter(PointerEventData data)=>panel.ShowTooltip(id);
        public void OnPointerExit(PointerEventData data)=>panel.HideTooltip(id);
        public void OnSelect(BaseEventData data)=>panel.ShowTooltip(id);
        public void OnDeselect(BaseEventData data)=>panel.HideTooltip(id);
        private void OnDisable(){if(panel!=null)panel.HideTooltip(id);}
    }
}
