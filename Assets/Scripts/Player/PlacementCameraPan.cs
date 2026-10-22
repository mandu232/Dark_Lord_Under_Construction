using DarkLord.Dungeon;
using UnityEngine;

namespace DarkLord.Player
{
    public sealed class PlacementCameraPan : MonoBehaviour
    {
        private Camera view;
        private CameraFollow follow;
        private DungeonGrid dungeon;
        private Object owner;
        private bool followWasEnabled;
        private Vector3 originalPosition;
        public bool IsActive => owner != null;
        public static PlacementCameraPan Acquire(Camera camera,Object requester,DungeonGrid grid)
        {
            if(camera==null||!camera.orthographic||requester==null||grid==null)return null;
            var pan=camera.GetComponent<PlacementCameraPan>()??camera.gameObject.AddComponent<PlacementCameraPan>();
            if(pan.owner!=null)return pan.owner==requester?pan:null;
            pan.view=camera;pan.dungeon=grid;pan.owner=requester;pan.originalPosition=camera.transform.position;
            pan.follow=camera.GetComponent<CameraFollow>();pan.followWasEnabled=pan.follow!=null&&pan.follow.enabled;
            if(pan.follow!=null)pan.follow.enabled=false;
            return pan;
        }
        public void Pan(Vector2 pointer,float deltaTime)
        {
            if(!IsActive||view==null||dungeon==null||deltaTime<=0||Time.timeScale<=0)return;
            Rect viewport=view.pixelRect;if(!viewport.Contains(pointer))return;
            float margin=Mathf.Min(80,Mathf.Min(viewport.width,viewport.height)*.12f);if(margin<=0)return;
            float x=Edge(pointer.x,viewport.xMin,viewport.xMax,margin),y=Edge(pointer.y,viewport.yMin,viewport.yMax,margin);
            var direction=Vector2.ClampMagnitude(new Vector2(x,y),1);if(direction==Vector2.zero)return;
            var position=view.transform.position+(view.transform.right*direction.x+view.transform.up*direction.y)*view.orthographicSize*.9f*Mathf.Min(deltaTime,.05f);
            var min=dungeon.CellCenter(Vector2Int.zero);var max=dungeon.CellCenter(new Vector2Int(dungeon.Layout.Width-1,dungeon.Layout.Height-1));
            position.x=Mathf.Clamp(position.x,Mathf.Min(min.x,max.x),Mathf.Max(min.x,max.x));
            position.y=Mathf.Clamp(position.y,Mathf.Min(min.y,max.y),Mathf.Max(min.y,max.y));position.z=originalPosition.z;
            view.transform.position=position;
        }
        private static float Edge(float value,float min,float max,float margin)
        {return value<min+margin?-Mathf.Clamp01((min+margin-value)/margin):value>max-margin?Mathf.Clamp01((value-(max-margin))/margin):0;}
        public void Release(Object requester){if(owner==requester)Restore();}
        private void Restore()
        {
            if(owner==null)return;
            if(view!=null)view.transform.position=originalPosition;
            if(follow!=null)follow.enabled=followWasEnabled;
            owner=null;dungeon=null;
        }
        private void OnDisable()=>Restore();
    }
}
