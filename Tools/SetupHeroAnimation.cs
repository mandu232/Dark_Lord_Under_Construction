using System;
using System.Linq;
using DarkLord.Actors;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Apply wiring without rebuilding the user's clips, state layout or trigger transitions.
public static class SetupHeroAnimation
{
    public static string Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        const string root = "Assets/Gameplay/Animations/Hero/";
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(root + "Hero.controller");
        var machine = controller.layers[0].stateMachine;
        var states = AssetDatabase.LoadAllAssetsAtPath(root + "Hero.controller").OfType<AnimatorState>().ToArray();
        var run = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == "Run");
        if (run != null) machine.RemoveState(run);
        for (int i = controller.parameters.Length - 1; i >= 0; i--)
            if (controller.parameters[i].name == "State") controller.RemoveParameter(i);
        var idle = machine.states.Single(s => s.state.name == "Idle").state;
        var walk = machine.states.Single(s => s.state.name == "Walk").state;
        foreach (string name in new[] { "Attack", "Attack2", "Block", "Hit" })
        {
            var action = states.Single(s => s.name == name);
            AddReturn(action, idle, AnimatorConditionMode.Less);
            AddReturn(action, walk, AnimatorConditionMode.Greater);
        }
        EditorUtility.SetDirty(controller);
        const string prefabPath = "Assets/Gameplay/Prefabs/Enemies/WaveInvader.prefab";
        var prefab = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var animator = prefab.GetComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            var manager = new SerializedObject(prefab.GetComponent<ActorAnimation>());
            manager.FindProperty("animator").objectReferenceValue = animator;
            manager.FindProperty("spriteRenderer").objectReferenceValue = prefab.GetComponent<SpriteRenderer>();
            manager.FindProperty("artworkFacesRight").boolValue = true;
            manager.FindProperty("useActionParameters").boolValue = true;
            manager.FindProperty("useExistingSpeedParameter").boolValue = false;
            manager.FindProperty("useDirectionalParameters").boolValue = false;
            manager.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        AssetDatabase.SaveAssets();
        // Run has been explicitly excluded from the hero animation assets.
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(root + "Hero_Run.anim") != null)
            AssetDatabase.DeleteAsset(root + "Hero_Run.anim");
        return "Hero uses Move_Speed and separate action triggers; Run and State removed.";
    }

    private static void AddReturn(AnimatorState action, AnimatorState destination, AnimatorConditionMode mode)
    {
        var transition = action.transitions.FirstOrDefault(t => t.destinationState == destination);
        // Preserve authored valid transitions, but repair existing empty transitions too.
        if (transition != null && (transition.hasExitTime || transition.conditions.Length > 0)) return;
        if (transition == null) transition = action.AddTransition(destination);
        transition.hasExitTime = true;
        transition.exitTime = 1;
        transition.duration = 0;
        transition.AddCondition(mode, 0.001f, "Move_Speed");
        EditorUtility.SetDirty(transition);
    }
}
