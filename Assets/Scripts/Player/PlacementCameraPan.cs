using DarkLord.Dungeon;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DarkLord.Player
{
    public sealed class PlacementCameraPan : MonoBehaviour
    {
        private Camera view;
        private CameraFollow follow;
        private PixelPerfectCamera pixels;
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
            pan.pixels=camera.GetComponent<PixelPerfectCamera>();
            pan.follow=camera.GetComponent<CameraFollow>();pan.followWasEnabled=pan.follow!=null&&pan.follow.enabled;
            if(pan.follow!=null)pan.follow.enabled=false;
            return pan;
        }
        public void Pan(Vector2 pointer,float deltaTime)
        { Pan(pointer, Vector2.zero, deltaTime); }
        public void Pan(Vector2? pointer, Vector2 movement, float deltaTime)
        {
            if(!IsActive||view==null||dungeon==null||deltaTime<=0||Time.timeScale<=0)return;
            Rect viewport=view.pixelRect;
            Vector2 edge = Vector2.zero;
            float margin=Mathf.Min(80,Mathf.Min(viewport.width,viewport.height)*.12f);
            if(pointer.HasValue && margin>0 && viewport.Contains(pointer.Value))
                edge = new Vector2(Edge(pointer.Value.x,viewport.xMin,viewport.xMax,margin), Edge(pointer.Value.y,viewport.yMin,viewport.yMax,margin));
            var direction=Vector2.ClampMagnitude(edge + movement,1);if(direction==Vector2.zero)return;
            var position=view.transform.position+(view.transform.right*direction.x+view.transform.up*direction.y)*view.orthographicSize*.9f*Mathf.Min(deltaTime,.05f);
            var min=dungeon.CellCenter(Vector2Int.zero);var max=dungeon.CellCenter(new Vector2Int(dungeon.Layout.Width-1,dungeon.Layout.Height-1));
            position.x=Mathf.Clamp(position.x,Mathf.Min(min.x,max.x),Mathf.Max(min.x,max.x));
            position.y=Mathf.Clamp(position.y,Mathf.Min(min.y,max.y),Mathf.Max(min.y,max.y));position.z=originalPosition.z;
            view.transform.position=position;
            SynchronizePixelCamera();
        }
        private void SynchronizePixelCamera()
        {
            if (pixels == null || !pixels.isActiveAndEnabled) return;
            // PixelPerfectCamera normally updates this matrix at render time, after placement input.
            // Match its snapped view now so raycasts and previews use this frame's camera position.
            Vector3 cameraPosition = view.transform.position;
            Vector3 offset = pixels.RoundToPixel(cameraPosition) - cameraPosition;
            offset.z = -offset.z;
            var inversePosition = Matrix4x4.TRS(cameraPosition + offset, Quaternion.identity, Vector3.one).inverse;
            var inverseRotation = Matrix4x4.Rotate(view.transform.rotation).inverse;
            view.worldToCameraMatrix = Matrix4x4.Scale(new Vector3(1, 1, -1)) * inverseRotation * inversePosition;
        }
        private static float Edge(float value,float min,float max,float margin)
        {return value<min+margin?-Mathf.Clamp01((min+margin-value)/margin):value>max-margin?Mathf.Clamp01((value-(max-margin))/margin):0;}
        public void Release(Object requester){if(owner==requester)Restore();}
        private void Restore()
        {
            if(owner==null)return;
            if(view!=null){view.transform.position=originalPosition;SynchronizePixelCamera();}
            if(follow!=null)follow.enabled=followWasEnabled;
            owner=null;dungeon=null;pixels=null;
        }
        private void OnDisable()=>Restore();
    }
}
