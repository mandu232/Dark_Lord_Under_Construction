using System.Collections;
using System.IO;
using System.Linq;
using DarkLord.Actors;
using DarkLord.Gameplay;
using DarkLord.Persistence;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    /// <summary>Renders lighting variants for visual review. Explicit: never part of a normal run.
    /// DARKLORD_CAPTURE_DIR picks the output folder (Unity clears Temp on exit).</summary>
    [Explicit]
    public sealed class LightingCapture
    {
        [UnityTest] public IEnumerator CaptureLightingVariants()
        {
            Time.timeScale = 1; GameSaveSession.BeginNew(0, new NewGameOptions { difficulty = GameDifficulty.Easy });
            yield return SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity", LoadSceneMode.Additive);
            var scene = SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity");
            SceneManager.SetActiveScene(scene);
            yield return null; yield return null;
            var waves = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WaveDirector>(true)).Single();
            while (waves.Tutorial.DialogueOpen) waves.Tutorial.NextDialogue();
            for (int i = 0; i < 90; i++) yield return null;
            var camera = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)).First(c => c.isActiveAndEnabled && c.orthographic);
            string folder = System.Environment.GetEnvironmentVariable("DARKLORD_CAPTURE_DIR") ?? "Temp/LightingCapture";
            Directory.CreateDirectory(folder);
            var lights = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var global = lights.First(l => l.lightType == Light2D.LightType.Global);
            var points = lights.Where(l => l.lightType != Light2D.LightType.Global).ToArray();
            var torch = Object.FindObjectsByType<TorchShadowLight>(FindObjectsSortMode.None).Where(t => t.transform.Find("Torch light") != null)
                .OrderBy(t => Vector2.Distance(t.transform.position, waves.Lord.transform.position)).FirstOrDefault();
            Vector3 home = camera.transform.position;
            Color ambient = global.color; float ambientIntensity = global.intensity;

            foreach (var r in waves.Lord.GetComponentsInChildren<SpriteRenderer>(true))
                Debug.Log($"LightingCapture lord renderer {r.name} enabled {r.enabled} active {r.gameObject.activeInHierarchy} color {r.color} order {r.sortingOrder} bounds {r.bounds} material {(r.sharedMaterial != null ? r.sharedMaterial.name : "null")}");
            // "before" approximates the old flat look: point lights off, plain white ambient.
            var shots = new (string name, bool lit, float intensity, Color color)[]
            {
                ("a-before", false, 1f, Color.white),
                ("b-after", true, ambientIntensity, ambient),
            };
            foreach (var shot in shots)
            {
                foreach (var point in points) point.enabled = shot.lit;
                global.intensity = shot.intensity; global.color = shot.color;
                yield return null;
                camera.transform.position = home;
                Save(camera, folder + "/" + shot.name + "-core.png");
                if (torch != null)
                {
                    camera.transform.position = new Vector3(torch.transform.position.x, torch.transform.position.y - 1.5f, home.z);
                    Save(camera, folder + "/" + shot.name + "-torch.png");
                }
            }
            camera.transform.position = home;
            Debug.Log("LightingCapture points " + points.Length + ", torch " + (torch != null));
        }

        private static void Save(Camera camera, string path)
        {
            var target = new RenderTexture(1600, 900, 24);
            var previous = camera.targetTexture;
            camera.targetTexture = target; camera.Render(); camera.targetTexture = previous;
            RenderTexture.active = target;
            var image = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); image.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.Destroy(image); target.Release(); Object.Destroy(target);
        }
    }
}
