using System.Linq;
using DarkLord.Gameplay;
using DarkLord.Player;
using TMPro;
using UnityEngine;

namespace DarkLord.UI
{
    public sealed class EconomyPanel : MonoBehaviour
    {
        private WaveDirector waves;
        private RunEconomy economy;
        private TMP_FontAsset font;
        private RectTransform window, canvasRect;
        private Canvas canvas;
        private Camera camera;
        private TMP_Text title, summary, message, upkeep, coreHint, merchantHint;
        private readonly UnityEngine.UI.Button[] goods = new UnityEngine.UI.Button[4];
        private readonly TMP_Text[] goodLabels = new TMP_Text[4];
        private CoreTechPanel technology;
        public bool IsVisible => window != null && window.gameObject.activeInHierarchy || technology != null && technology.IsVisible;
        public void Configure(WaveDirector director, TMP_FontAsset labelFont)
        {
            waves = director; font = labelFont; canvas = GetComponentInParent<Canvas>(); canvasRect = GetComponent<RectTransform>();
            camera = gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).FirstOrDefault(c => c.orthographic);
            window = Rect("Core and merchant facility", transform, new Vector2(-24, 0), new Vector2(520, 790));
            window.anchorMin = window.anchorMax = window.pivot = new Vector2(1, .5f);
            var skin = Resources.Load<RewardUiSkin>("WaveRewardSkin");
            var plate = window.gameObject.AddComponent<UnityEngine.UI.Image>(); plate.sprite = skin != null ? skin.cardFrame : null; plate.color = Color.white;
            var portraitRect = Rect("Nibble merchant portrait", window, new Vector2(-24, 0), new Vector2(360, 440));
            portraitRect.anchorMin = portraitRect.anchorMax = Vector2.zero; portraitRect.pivot = new Vector2(1, 0);
            var portrait = portraitRect.gameObject.AddComponent<UnityEngine.UI.RawImage>(); portrait.raycastTarget = false;
            portrait.texture = Resources.Load<Texture2D>("Story/Portraits/NibbleDungeon");
            portrait.enabled = portrait.texture != null;
            if (portrait.texture != null) portraitRect.sizeDelta = new Vector2(440f * portrait.texture.width / portrait.texture.height, 440);
            title = Label("Facility title", window, "", 28, new Vector2(0, -42), new Vector2(440, 42));
            summary = Label("Facility summary", window, "", 18, new Vector2(0, -104), new Vector2(430, 110));
            for (int i = 0; i < 4; i++)
            {
                int index = i; goods[i] = Button("Merchant offer " + i, -238-i*94, 80, () => economy.TryBuy(index), out goodLabels[i]);
            }
            message = Label("Economy transaction status", window, "", 17, new Vector2(0,-654), new Vector2(438,76));
            Button("Close economy facility", -740, 42, () => economy.Close(), out var closeLabel); closeLabel.text = "닫기 · Esc";
            upkeep = Label("Blood upkeep and merchant status", transform, "", 17, new Vector2(36, -690), new Vector2(440, 120));
            upkeep.rectTransform.anchorMin = upkeep.rectTransform.anchorMax = upkeep.rectTransform.pivot = new Vector2(0,1);
            upkeep.alignment = TextAlignmentOptions.TopLeft;
            coreHint = Label("Core facility interaction", transform, "", 18, Vector2.zero, new Vector2(320,36));
            merchantHint = Label("Goblin merchant interaction", transform, "", 18, Vector2.zero, new Vector2(360,54));
            coreHint.rectTransform.anchorMin = coreHint.rectTransform.anchorMax = coreHint.rectTransform.pivot = Vector2.one*.5f;
            merchantHint.rectTransform.anchorMin = merchantHint.rectTransform.anchorMax = merchantHint.rectTransform.pivot = Vector2.one*.5f;
            window.gameObject.SetActive(false);
            technology=gameObject.AddComponent<CoreTechPanel>();technology.Configure(director,font);
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var node = new GameObject(name, typeof(RectTransform)); node.transform.SetParent(parent, false);
            var r = node.GetComponent<RectTransform>(); r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f,1); r.anchoredPosition = position; r.sizeDelta = size; return r;
        }
        private TMP_Text Label(string name, Transform parent, string value, int size, Vector2 position, Vector2 bounds)
        {
            var text = Rect(name,parent,position,bounds).gameObject.AddComponent<TextMeshProUGUI>();
            text.font=font; text.text=value; text.fontSize=size; text.color=new Color(1,.89f,.7f); text.raycastTarget=false; text.alignment=TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.Normal; return text;
        }
        private UnityEngine.UI.Button Button(string name, float y, float height, UnityEngine.Events.UnityAction action, out TMP_Text label)
        {
            var rect=Rect(name,window,new Vector2(0,y),new Vector2(444,height)); rect.pivot=Vector2.one*.5f;
            var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>(); var skin=Resources.Load<RewardUiSkin>("WaveRewardSkin"); image.sprite=skin!=null?skin.button:null; image.color=Color.white;
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.onClick.AddListener(action);
            label=Label(name+" label",rect,"",18,Vector2.zero,new Vector2(416,height-4)); label.rectTransform.anchorMin=label.rectTransform.anchorMax=label.rectTransform.pivot=Vector2.one*.5f;
            return button;
        }
        private void WorldHint(TMP_Text label, Vector3 position, bool visible)
        {
            if(camera==null||canvas==null){label.gameObject.SetActive(false);return;}
            var screen=camera.WorldToScreenPoint(position); label.gameObject.SetActive(visible&&screen.z>0&&camera.pixelRect.Contains(screen));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,screen,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var local); label.rectTransform.anchoredPosition=local;
        }
        private void LateUpdate()
        {
            if(waves==null||window==null)return; if(economy==null)economy=waves.Economy; if(economy==null)return;
            bool active=Time.timeScale>0&&!waves.IsFinished;
            window.gameObject.SetActive(active&&economy.IsMerchantOpen);
            upkeep.gameObject.SetActive(active);
            upkeep.text=$"하수인 유지비 · 다음 라운드 혈액 {economy.NextUpkeep}\n1마리당 {RunEconomy.BloodPerMinion} · 이번 납부 {economy.LastBloodPaid}"+
                (economy.BloodDebt>0?$" · <color=#FF8080>미납 {economy.BloodDebt}</color>":"")+"\n"+
                (economy.MerchantPresent?$"고블린 상인 방문 중 · {Mathf.CeilToInt(economy.MerchantRemaining)}초" + (waves.CanUseFacilities ? " · 시작방에서 F" : ""):$"고블린 상인 · {economy.NextMerchantWave}라운드에 방문 예정");
            coreHint.text="[F] 코어 · 능력 강화";
            WorldHint(coreHint,economy.CorePosition+Vector3.up*1.8f,active&&!economy.IsOpen&&economy.HasNearbyInteraction&&economy.NearCore&&!economy.NearMerchant);
            merchantHint.text=$"고블린 상인 · {Mathf.CeilToInt(economy.MerchantRemaining)}초"+(economy.NearMerchant&&waves.CanUseFacilities?"\n[F] 거래하기":"");
            WorldHint(merchantHint,economy.MerchantPosition+Vector3.up*2.1f,active&&waves.CanUseFacilities&&economy.MerchantPresent&&!economy.IsOpen);
            if(!window.gameObject.activeInHierarchy)return;
            title.text="니블 틱 · 던전 컴퍼니";
            summary.text=$"보유 금화 {economy.Wallet.Gold} · 남은 시간 {Mathf.CeilToInt(economy.MerchantRemaining)}초\n상품별 1회 구매 · 방문할 때마다 새 재고\n구매한 건설 아이템은 건설창에서 사용";
            for(int i=0;i<4;i++)
            {
                goods[i].interactable=economy.CanBuy(i);
                goodLabels[i].text=economy.OfferName(i)+"\n"+(economy.IsSold(i)?"매진":$"금화 {RunEconomy.Price(i)}");
            }
            message.text=economy.Message;
        }
        private void OnDisable()
        {
            if(window!=null)window.gameObject.SetActive(false); if(upkeep!=null)upkeep.gameObject.SetActive(false);
            if(coreHint!=null)coreHint.gameObject.SetActive(false); if(merchantHint!=null)merchantHint.gameObject.SetActive(false);
        }
    }
}
