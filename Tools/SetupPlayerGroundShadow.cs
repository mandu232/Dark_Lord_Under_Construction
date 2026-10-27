using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DarkLord.Actors;

public static class SetupPlayerGroundShadow
{
    public static string Run()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        var shader=Shader.Find("DarkLord/Sprites/Projected Shadow");
        if(shader==null || !shader.isSupported || ShaderUtil.ShaderHasError(shader)) throw new InvalidOperationException("Projection shader is not ready.");
        const string path="Assets/Sprtie/Characters/Player/PlayerGroundShadow.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(material==null) {material=new Material(shader); AssetDatabase.CreateAsset(material,path);}
        material.shader=shader;
        material.SetColor("_ShadowColor",new Color(.025f,.018f,.045f,.46f));
        material.SetFloat("_GroundY",-.84375f); material.SetFloat("_Shear",.55f); material.SetFloat("_Flatten",.27f);
        EditorUtility.SetDirty(material);
        var scene=SceneManager.GetSceneByPath("Assets/Scenes/InGame.unity"); bool opened=!scene.isLoaded;
        if(opened) scene=EditorSceneManager.OpenScene("Assets/Scenes/InGame.unity",OpenSceneMode.Additive);
        try
        {
            var player=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<DarkLord.Player.PlayerMovement>(true)).Single();
            var child=player.transform.Find("Ground Shadow");
            if(child==null) {child=new GameObject("Ground Shadow",typeof(SpriteRenderer)).transform; child.SetParent(player.transform,false);}
            child.localPosition=Vector3.zero; child.localRotation=Quaternion.identity; child.localScale=Vector3.one;
            var renderer=child.GetComponent<SpriteRenderer>(); renderer.sharedMaterial=material;
            var projection=child.GetComponent<SpriteProjectedShadow>(); if(projection==null) projection=child.gameObject.AddComponent<SpriteProjectedShadow>();
            projection.Refresh();
            EditorUtility.SetDirty(child); EditorUtility.SetDirty(renderer); EditorUtility.SetDirty(projection);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            return "Saved silhouette projection at feet, follows sprite/flip/visibility; shader has no errors.";
        }
        finally {if(opened) EditorSceneManager.CloseScene(scene,true);}
    }
}

