using System;
using System.Threading.Tasks;
using DarkLord.Actors;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class ValidateGameplayEffectRendering
{
    public static async Task<string> Run()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Run this GPU validation in Play Mode.");
        var scene = SceneManager.CreateScene("Original-color VFX GPU validation");
        var root = new GameObject("Isolated rendering validation");
        SceneManager.MoveGameObjectToScene(root, scene);
        var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
        var whiteMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
        var rt = new RenderTexture(128, 128, 24, RenderTextureFormat.ARGB32); rt.Create();
        var pixels = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        Camera camera = null;
        Color Read(int x, int y)
        {
            var previous = RenderTexture.active;
            try { RenderTexture.active = rt; pixels.ReadPixels(new Rect(0, 0, 128, 128), 0, 0); pixels.Apply(); return pixels.GetPixel(x, y); }
            finally { RenderTexture.active = previous; }
        }
        SpriteRenderer Make(string name, float scale, Material material, int order)
        {
            var node = new GameObject(name, typeof(SpriteRenderer)); node.transform.SetParent(root.transform);
            node.transform.position = new Vector3(1000, 1000, 0); node.transform.localScale = Vector3.one * scale;
            var renderer = node.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = material; renderer.sortingOrder = order;
            return renderer;
        }
        try
        {
            Make("Graded background", 2, whiteMaterial, -30000);
            var actor = Make("Protected actor", .4f, whiteMaterial, 0);
            actor.gameObject.AddComponent<ActorDepthSorting>();
            var effect = Make("Green original-color effect", 1.6f, Resources.Load<Material>("GameplayEffectOriginalColor"), 20);
            effect.color = Color.green;
            var traceNode = new GameObject("Blue original-color trace", typeof(LineRenderer)); traceNode.transform.SetParent(root.transform);
            var trace = traceNode.GetComponent<LineRenderer>(); trace.sharedMaterial = Resources.Load<Material>("GameplayTraceOriginalColor");
            trace.positionCount = 2; trace.SetPosition(0, new Vector3(999.5f, 999.45f, 0)); trace.SetPosition(1, new Vector3(1000.7f, 999.45f, 0));
            trace.startWidth = trace.endWidth = .15f; trace.startColor = trace.endColor = Color.blue; trace.sortingOrder = 21;
            var cameraNode = new GameObject("Validation camera", typeof(Camera)); cameraNode.transform.SetParent(root.transform);
            camera = cameraNode.GetComponent<Camera>(); camera.transform.position = new Vector3(1000, 1000, -10);
            camera.orthographic = true; camera.orthographicSize = 1; camera.aspect = 1; camera.targetTexture = rt;
            camera.allowHDR = camera.allowMSAA = false; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var volumeNode = new GameObject("Strong red grade", typeof(Volume)); volumeNode.transform.SetParent(root.transform);
            var volume = volumeNode.GetComponent<Volume>(); volume.isGlobal = true; volume.priority = 10000; volume.sharedProfile = profile;
            var grade = profile.Add<ColorAdjustments>(true); grade.colorFilter.Override(Color.red); grade.postExposure.Override(-1);
            await Task.Delay(350);
            var gradedEffect = Read(96, 64); var gradedBackground = Read(5, 5); var protectedActor = Read(64, 64); var gradedTrace = Read(96, 29);
            volume.enabled = false;
            await Task.Delay(250);
            var clearEffect = Read(96, 64); var clearBackground = Read(5, 5);
            float delta = Mathf.Max(Mathf.Abs(gradedEffect.r - clearEffect.r), Mathf.Abs(gradedEffect.g - clearEffect.g), Mathf.Abs(gradedEffect.b - clearEffect.b));
            if (gradedEffect.g < .8f || delta > .03f) throw new Exception($"Effect was graded or missing: on={gradedEffect} off={clearEffect} delta={delta}");
            if (gradedBackground.g >= clearBackground.g - .2f) throw new Exception($"Background grading was bypassed: on={gradedBackground} off={clearBackground}");
            if (protectedActor.r < .3f || protectedActor.g > .35f) throw new Exception($"Actor silhouette was covered: {protectedActor}");
            if (gradedTrace.b < .8f || gradedTrace.g > .1f) throw new Exception($"Trace was graded or missing: {gradedTrace}");
            return $"PASS: effect RGB delta with grade on/off={delta:F4}; green={gradedEffect.g:F3}; trace blue={gradedTrace.b:F3}; background green {gradedBackground.g:F3}->{clearBackground.g:F3}; protected actor={protectedActor}";
        }
        finally
        {
            if (camera != null) camera.targetTexture = null;
            UnityEngine.Object.Destroy(root); UnityEngine.Object.Destroy(sprite); UnityEngine.Object.Destroy(whiteMaterial);
            foreach (var component in profile.components) UnityEngine.Object.Destroy(component);
            UnityEngine.Object.Destroy(profile); UnityEngine.Object.Destroy(pixels); rt.Release(); UnityEngine.Object.Destroy(rt);
            var unload = SceneManager.UnloadSceneAsync(scene);
            while (unload != null && !unload.isDone) await Task.Delay(10);
        }
    }
}
