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
        [SerializeField] private RectTransform window;
        private RectTransform canvasRect;
        private Canvas canvas;
        private Camera camera;
        [SerializeField] private TMP_Text title, summary, message, upkeep, coreHint, merchantHint;
        [SerializeField] private UnityEngine.UI.Button[] goods = new UnityEngine.UI.Button[4];
        [SerializeField] private TMP_Text[] goodLabels = new TMP_Text[4];
        [SerializeField] private UnityEngine.UI.Button closeButton;
        private CoreTechPanel technology;
        public bool IsVisible => window != null && window.gameObject.activeInHierarchy || technology != null && technology.IsVisible;
        public void Configure(WaveDirector director, TMP_FontAsset labelFont)
        {
            if (waves != null) return;
            waves = director; font = labelFont; canvas = GetComponentInParent<Canvas>(); canvasRect = GetComponent<RectTransform>();
            camera = gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>()).FirstOrDefault(c => c.orthographic);
            for (int i = 0; i < goods.Length; i++)
            {
                int index = i; goods[i].onClick.AddListener(() => economy.TryBuy(index));
            }
            closeButton.onClick.AddListener(() => economy.Close());
            window.gameObject.SetActive(false);
            if (upkeep != null) upkeep.gameObject.SetActive(false);
            technology = gameObject.AddComponent<CoreTechPanel>(); technology.Configure(director, font);
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
            if (upkeep != null) upkeep.gameObject.SetActive(false);
            bool movingCore=waves.Lord.GetComponent<DarkLord.Dungeon.DungeonExpansion>()?.IsMovingCore==true;
            coreHint.text=movingCore?"코어 이동 · 좌클릭 배치\n우클릭·Esc: 취소":"[F] 코어 · 능력 강화";
            WorldHint(coreHint,economy.CorePosition+Vector3.up*1.8f,active&&(movingCore||!economy.IsOpen&&economy.HasNearbyInteraction&&economy.NearCore&&!economy.NearMerchant));
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
