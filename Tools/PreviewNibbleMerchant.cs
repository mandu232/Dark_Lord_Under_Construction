using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using DarkLord.Gameplay;
using DarkLord.Persistence;
using DarkLord.Actors;
public static class PreviewNibbleMerchant
{
    private static async Task Pump(IEnumerator routine)
    {
        while (routine.MoveNext())
        {
            if (routine.Current is AsyncOperation operation) while (!operation.isDone) await Task.Yield();
            else await Task.Yield();
        }
    }
    public static async Task<string> Run()
    {
        var fixture = new DarkLord.Tests.ResourceEconomyTests();
        try { await Pump(fixture.Setup()); fixture.MerchantUsesLimitedPalettePixelSpriteAndActorDepth(); }
        finally { await Pump(fixture.Cleanup()); }
        GameSaveSession.BeginNew(0, new NewGameOptions { difficulty = GameDifficulty.Easy, skipOpening = true });
        var loading = SceneManager.LoadSceneAsync("Assets/Scenes/InGame.unity");
        while (!loading.isDone) await Task.Yield();
        await Task.Yield(); await Task.Yield();
        var waves = Object.FindFirstObjectByType<WaveDirector>();
        var node = new GameObject("Nibble visual review");
        node.transform.position = waves.Lord.transform.position + Vector3.right * 1.5f;
        node.AddComponent<GoblinMerchantVisual>();
        var camera = Camera.main; var follow = camera.GetComponent<DarkLord.Player.CameraFollow>();
        if (follow != null) follow.enabled = false;
        camera.transform.position = new Vector3(waves.Lord.transform.position.x + .5f, waves.Lord.transform.position.y + .4f, camera.transform.position.z);
        camera.orthographicSize = 4;
        Time.timeScale = 0;
        return "Merchant asset/runtime/depth test passed; actual InGame preview ready beside player, no save or scene writes.";
    }
}
