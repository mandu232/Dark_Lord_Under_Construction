using DarkLord.Player;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DarkLord.UI
{
    public sealed class TechNodePointer : MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler,IInitializePotentialDragHandler,IBeginDragHandler,IDragHandler,IEndDragHandler
    {
        private CoreTechPanel panel;private TechId id;
        public void Configure(CoreTechPanel owner,TechId technology){panel=owner;id=technology;}
        public void OnPointerEnter(PointerEventData data)=>panel.ShowTooltip(id);
        public void OnPointerExit(PointerEventData data)=>panel.HideTooltip(id);
        public void OnSelect(BaseEventData data)=>panel.ShowTooltip(id);
        public void OnDeselect(BaseEventData data)=>panel.HideTooltip(id);
        public void OnInitializePotentialDrag(PointerEventData data)=>panel.TreeScroll.OnInitializePotentialDrag(data);
        public void OnBeginDrag(PointerEventData data){data.eligibleForClick=false;panel.BeginTreeDrag();panel.TreeScroll.OnBeginDrag(data);}
        public void OnDrag(PointerEventData data)=>panel.TreeScroll.OnDrag(data);
        public void OnEndDrag(PointerEventData data)=>panel.TreeScroll.OnEndDrag(data);
        private void OnDisable(){if(panel!=null)panel.HideTooltip(id);}
    }
}
