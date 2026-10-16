using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

namespace DarkLord.UI
{
    // One continuous castle with UI stations at left, center and right.
    [DefaultExecutionOrder(100), DisallowMultipleComponent]
    public sealed class CastleMenuPanorama : MonoBehaviour
    {
        [SerializeField] private RectTransform castle, character, title, actions, settings, slots;
        [SerializeField] private GameObject dimmer;
        [UnityEngine.Serialization.FormerlySerializedAs("rooftop")]
        [SerializeField] private RectTransform newGameSetup;
        [SerializeField] private Sprite newGameCharacterSprite;
        private UnityEngine.UI.Image characterImage;
        private Sprite originalCharacterSprite;
        [SerializeField, Min(.1f)] private float duration = .85f;
        private Vector2 characterOrigin, titleOrigin, actionsOrigin;
        private Vector3 castleScale, characterScale;
        private float setupZoom, zoomFrom, zoomTarget;
        private float view, from, target, elapsed;
        private bool leavingSetup;
        private Vector2 pointer;
        private GameMenu.ScreenState state;
        private CanvasGroup settingsGroup, slotsGroup, actionsGroup;
        private CanvasGroup setupGroup;
        private readonly Dictionary<UnityEngine.UI.Selectable, Color> disabledColors = new Dictionary<UnityEngine.UI.Selectable, Color>();

        private void Awake()
        {
            characterOrigin=character.anchoredPosition;
            characterImage=character.GetComponent<UnityEngine.UI.Image>();
            if(characterImage!=null)originalCharacterSprite=characterImage.sprite;
            titleOrigin=title.anchoredPosition;
            actionsOrigin=actions.anchoredPosition;
            castleScale=castle.localScale;
            characterScale=character.localScale;
            settingsGroup=settings.GetComponent<CanvasGroup>();
            slotsGroup=slots.GetComponent<CanvasGroup>();
            actionsGroup=actions.GetComponent<CanvasGroup>();
            if(newGameSetup!=null)setupGroup=newGameSetup.GetComponent<CanvasGroup>();
        }

        public void Navigate(GameMenu.ScreenState next)
        {
            leavingSetup=state==GameMenu.ScreenState.NewGameSetup || (leavingSetup && elapsed<duration);
            state=next;
            from=view;
            target=next==GameMenu.ScreenState.Settings?1:next==GameMenu.ScreenState.Slots?-1:0;
            zoomFrom=setupZoom;
            zoomTarget=next==GameMenu.ScreenState.NewGameSetup?1:0;
            elapsed=0;
            Draw();
        }

        private void LateUpdate()
        {
            elapsed+=Time.unscaledDeltaTime;
            float t=Mathf.Clamp01(elapsed/duration);
            view=Mathf.Lerp(from,target,t*t*t*(t*(t*6-15)+10));
            setupZoom=Mathf.Lerp(zoomFrom,zoomTarget,t*t*t*(t*(t*6-15)+10));
            Vector2 desired=Vector2.zero;
            if(Application.isFocused && Mouse.current!=null && Screen.width>0 && Screen.height>0)
            {
                var p=Mouse.current.position.ReadValue();
                if(p.x>=0 && p.x<=Screen.width && p.y>=0 && p.y<=Screen.height)
                    desired=new Vector2(p.x/Screen.width*2-1,p.y/Screen.height*2-1);
            }
            pointer=Vector2.Lerp(pointer,desired,1-Mathf.Exp(-4*Time.unscaledDeltaTime));
            Draw();
        }

        private void Draw()
        {
            float width=((RectTransform)transform).rect.width;
            float height=((RectTransform)transform).rect.height;
            // Cover ultrawide windows too; the far wall moves less than the foreground.
            float artWidth=Mathf.Max(height*3, width+1400)+80;
            castle.sizeDelta=new Vector2(artWidth,Mathf.Max(height+60,artWidth/3));
            castle.localScale=castleScale*(1+.08f*setupZoom);
            character.localScale=characterScale*(1+.12f*setupZoom);
            castle.anchoredPosition=new Vector2(-view*(artWidth-width)*.46f,0)-pointer*5;
            character.anchoredPosition=characterOrigin+Vector2.left*(view*(width+100))-pointer*14;
            title.anchoredPosition=titleOrigin+Vector2.left*(view*width);
            actions.anchoredPosition=actionsOrigin+Vector2.left*(view*width);
            bool setupVisible=state==GameMenu.ScreenState.NewGameSetup || (leavingSetup && elapsed<duration);
            // Keep the setup pose until its return transition has finished.
            if(characterImage!=null)
                characterImage.sprite=setupVisible && newGameCharacterSprite!=null
                    ?newGameCharacterSprite:originalCharacterSprite;
            bool mainVisible=(Mathf.Abs(view)<.999f || target==0) && !setupVisible;
            title.gameObject.SetActive(mainVisible);
            actions.gameObject.SetActive(mainVisible);
            bool ready=Mathf.Abs(view-target)<.001f;
            SetPanel(settings,settingsGroup,view>.001f || target==1,ready && state==GameMenu.ScreenState.Settings);
            SetPanel(slots,slotsGroup,view<-.001f || target==-1,ready && state==GameMenu.ScreenState.Slots);
            settings.anchoredPosition=new Vector2((1-view)*width,0);
            slots.anchoredPosition=new Vector2((-1-view)*width,0);
            if(newGameSetup!=null)
            {
                SetPanel(newGameSetup,setupGroup,setupVisible,ready && state==GameMenu.ScreenState.NewGameSetup);
                newGameSetup.anchoredPosition=new Vector2(-view*width,0);
            }
            SetInput(actionsGroup,ready&&state==GameMenu.ScreenState.Main);
            if(dimmer!=null)dimmer.SetActive(state==GameMenu.ScreenState.Quit);
        }

        private void SetPanel(RectTransform panel,CanvasGroup group,bool visible,bool input)
        {
            panel.gameObject.SetActive(visible);
            if(group==null)return;
            group.alpha=1; SetInput(group,input);
        }

        private void SetInput(CanvasGroup group,bool input)
        {
            if(group==null)return;
            foreach(var control in group.GetComponentsInChildren<UnityEngine.UI.Selectable>(true))
            {
                var colors=control.colors;
                // Only the temporary parent lock uses the normal tint. Truly disabled
                // controls (for example empty Continue slots) keep their disabled tint.
                if(!input && control.interactable)
                {
                    if(!disabledColors.ContainsKey(control))disabledColors.Add(control,colors.disabledColor);
                    colors.disabledColor=colors.normalColor;
                    control.colors=colors;
                }
                else if(disabledColors.TryGetValue(control,out var original))
                {
                    colors.disabledColor=original;control.colors=colors;
                    disabledColors.Remove(control);
                }
            }
            group.interactable=input;group.blocksRaycasts=input;
        }
    }
}
