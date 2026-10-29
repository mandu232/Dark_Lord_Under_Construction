var root = new UnityEngine.GameObject("Shadow rendering validation");
var texture = new UnityEngine.Texture2D(3, 1, UnityEngine.TextureFormat.RGBA32, false);
var sprite = UnityEngine.Sprite.Create(texture, new UnityEngine.Rect(0,0,3,1), new UnityEngine.Vector2(.5f,.5f), 1);
var material = new UnityEngine.Material(UnityEngine.Shader.Find("DarkLord/Sprites/Projected Shadow"));
var target = new UnityEngine.RenderTexture(192,64,24,UnityEngine.RenderTextureFormat.ARGB32);
var readback = new UnityEngine.Texture2D(192,64,UnityEngine.TextureFormat.RGBA32,false);
var previous = UnityEngine.RenderTexture.active;
try {
    if (UnityEditor.ShaderUtil.ShaderHasError(material.shader)) throw new System.Exception("Shadow shader compilation failed");
    texture.filterMode = UnityEngine.FilterMode.Point;
    texture.SetPixels(new[] {UnityEngine.Color.white,new UnityEngine.Color(1,74f/255f,80f/255f,1),new UnityEngine.Color(148f/255f,38f/255f,56f/255f,1)}); texture.Apply();
    var source = root.AddComponent<UnityEngine.SpriteRenderer>(); source.sprite=sprite;
    var node = new UnityEngine.GameObject("Shadow"); node.transform.SetParent(root.transform,false); node.layer=30;
    var shadow = node.AddComponent<UnityEngine.SpriteRenderer>(); shadow.sharedMaterial=material;
    var projection=node.AddComponent<DarkLord.Actors.SpriteProjectedShadow>();
    material.SetFloat("_Shear",0);material.SetFloat("_Flatten",1);
    var cameraNode = new UnityEngine.GameObject("Validation camera");cameraNode.transform.SetParent(root.transform,false);
    var camera=cameraNode.AddComponent<UnityEngine.Camera>();camera.orthographic=true;camera.orthographicSize=.5f;camera.aspect=3;
    camera.transform.position=new UnityEngine.Vector3(0,0,-10);camera.cullingMask=1<<30;camera.clearFlags=UnityEngine.CameraClearFlags.SolidColor;camera.backgroundColor=UnityEngine.Color.clear;camera.targetTexture=target;camera.allowHDR=false;
    var results=new System.Collections.Generic.List<string>();
    foreach (var sheet in new[]{"Slash_1","Slash_2","Walk"}) {
        texture.name=sheet; projection.Refresh();camera.Render();UnityEngine.RenderTexture.active=target;
        readback.ReadPixels(new UnityEngine.Rect(0,0,192,64),0,0);readback.Apply();
        float body=readback.GetPixel(32,32).a,effect=readback.GetPixel(96,32).a,clothing=readback.GetPixel(160,32).a;
        results.Add(sheet+": body="+body+", effect="+effect+", red clothing="+clothing);
        if(body<.05f || clothing<.05f || (sheet!="Walk" ? effect>.01f : effect<.05f)) throw new System.Exception(string.Join("; ",results));
    }
    return string.Join("; ",results);
} finally {
    UnityEngine.RenderTexture.active=previous;
    UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(sprite);UnityEngine.Object.DestroyImmediate(texture);
    UnityEngine.Object.DestroyImmediate(material);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(readback);
}
