var cam=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).First();
var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.isRootCanvas).ToArray();
var oldModes=canvases.Select(c=>c.renderMode).ToArray();
var oldCameras=canvases.Select(c=>c.worldCamera).ToArray();
var rt=new RenderTexture(1920,1080,24);
var oldTarget=cam.targetTexture;var oldActive=RenderTexture.active;
try{
 foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=cam;c.planeDistance=1;}
 Canvas.ForceUpdateCanvases();
 cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
 var texture=new Texture2D(1920,1080,TextureFormat.RGBA32,false);
 texture.ReadPixels(new Rect(0,0,1920,1080),0,0);texture.Apply();
 System.IO.File.WriteAllBytes("output/title-redesign/ingame-title-preview.png",texture.EncodeToPNG());
 UnityEngine.Object.DestroyImmediate(texture);
}finally{
 cam.targetTexture=oldTarget;RenderTexture.active=oldActive;
 for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=oldModes[i];canvases[i].worldCamera=oldCameras[i];}
 UnityEngine.Object.DestroyImmediate(rt);
}
return "Preview rendered without saving scene changes";
