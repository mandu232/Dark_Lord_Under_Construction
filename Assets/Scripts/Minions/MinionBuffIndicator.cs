using System.Collections.Generic;
using DarkLord.Dungeon;
using UnityEngine;
namespace DarkLord.Minions
{
    [DisallowMultipleComponent]
    public sealed class MinionBuffIndicator : MonoBehaviour
    {
        private MinionController controller;
        private LineRenderer ring, chevron;
        private readonly SpriteRenderer[] icons = new SpriteRenderer[3];
        private readonly List<ResourceBuilding> buffs = new List<ResourceBuilding>();
        private float nextRefresh;
        public bool IsVisible => ring != null && ring.enabled;
        public int ActiveBuffCount { get; private set; }
        private void Awake() => controller = GetComponentInParent<MinionController>();
        private void LateUpdate()
        {
            if (controller == null || !controller.isActiveAndEnabled || !controller.IsAlive || controller.IsHeld)
            { Hide(); return; }
            if (Time.time >= nextRefresh) { Refresh(); nextRefresh = Time.time + .1f; }
        }
        public void Refresh()
        {
            if (controller == null || !controller.isActiveAndEnabled || !controller.IsAlive || controller.IsHeld)
            { Hide(); return; }
            controller.GetRoomBonuses(out _,out _,out _,buffs);
            ActiveBuffCount = buffs.Count;
            if (buffs.Count == 0) { Hide(); return; }
            if (ring == null)
            {
                ring = Line("Facility buff ground ring",24,true);
                chevron = Line("Facility buff up arrow",3,false);
                for (int i = 0; i < icons.Length; i++)
                {
                    var node = new GameObject("Facility buff icon " + i); node.transform.SetParent(transform,false);
                    icons[i] = node.AddComponent<SpriteRenderer>();
                }
            }
            int order = Mathf.Clamp(-Mathf.RoundToInt(transform.position.y * 16) + 35,-30000,30000);
            ring.enabled = chevron.enabled = true; ring.sortingOrder = chevron.sortingOrder = order;
            for (int i = 0; i < 24; i++)
            {
                float angle = i * Mathf.PI / 12;
                ring.SetPosition(i,new Vector3(Mathf.Cos(angle)*.46f,Mathf.Sin(angle)*.22f,0));
            }
            chevron.SetPosition(0,new Vector3(-.13f,.9f,0)); chevron.SetPosition(1,new Vector3(0,1.05f,0)); chevron.SetPosition(2,new Vector3(.13f,.9f,0));
            int shown = Mathf.Min(icons.Length,buffs.Count);
            for (int i = 0; i < icons.Length; i++)
            {
                var icon = icons[i]; icon.enabled = i < shown && buffs[i].BuffIcon != null;
                if (!icon.enabled) continue;
                icon.sprite = buffs[i].BuffIcon; icon.sortingOrder = order + 1;
                icon.transform.localPosition = new Vector3((i-(shown-1)*.5f)*.34f,1.3f,0);
                float scale = .28f / Mathf.Max(.001f,Mathf.Max(icon.sprite.bounds.size.x,icon.sprite.bounds.size.y));
                icon.transform.localScale = Vector3.one * scale;
            }
        }
        private LineRenderer Line(string name,int count,bool loop)
        {
            var node=new GameObject(name); node.transform.SetParent(transform,false);
            var line=node.AddComponent<LineRenderer>(); line.useWorldSpace=false; line.positionCount=count; line.loop=loop;
            line.sharedMaterial=Resources.Load<Material>("GameplayTraceOriginalColor"); line.startWidth=line.endWidth=.045f;
            line.startColor=line.endColor=new Color(.3f,.9f,1,.85f); return line;
        }
        private void Hide()
        {
            ActiveBuffCount=0;
            if (ring == null) return;
            ring.enabled=chevron.enabled=false;
            foreach(var icon in icons) if(icon != null)icon.enabled=false;
        }
        private void OnDisable() => Hide();
    }
}
