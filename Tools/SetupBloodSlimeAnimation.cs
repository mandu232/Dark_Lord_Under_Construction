using System;
using System.IO;
using System.Linq;
using DarkLord.Actors;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class SetupBloodSlimeAnimation
{
    public static string Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode first.");
        const string sheet = "Assets/Sprite/Minion/Blood_Slime.png";
        const string root = "Assets/Gameplay/Animations/Minions/BloodSlime";
        var importer = (TextureImporter)AssetImporter.GetAtPath(sheet);
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(sheet).OfType<Sprite>().ToArray();
        Directory.CreateDirectory(root);
        AssetDatabase.Refresh();
        string[] names = { "Idle", "Walk", "Block", "Attack", "Attack2", "Hit", "Die" };
        int[] rows = { 0, 1, 3, 4, 5, 6, 7 };
        int[] counts = { 6, 8, 6, 8, 8, 4, 10 };
        float[] rates = { 8, 12, 12, 20, 20, 20, 10 };
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(root + "/BloodSlime.controller");
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(root + "/BloodSlime.controller");
        var machine = controller.layers[0].stateMachine;
        for (int i = 0; i < names.Length; i++)
        {
            string path = root + "/BloodSlime_" + names[i] + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = "BloodSlime_" + names[i], frameRate = rates[i] };
                AssetDatabase.CreateAsset(clip, path);
                var keys = new ObjectReferenceKeyframe[counts[i] + 1];
                for (int f = 0; f < counts[i]; f++)
                    keys[f] = new ObjectReferenceKeyframe { time = f / rates[i], value = sprites.Single(s => s.rect.x == f * 96 && s.rect.y == (7 - rows[i]) * 96) };
                keys[counts[i]] = new ObjectReferenceKeyframe { time = names[i].StartsWith("Attack") ? 0.5f : counts[i] / rates[i], value = keys[counts[i] - 1].value };
                AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = i < 2;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
            }
            var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == names[i]);
            if (state == null) state = machine.AddState(names[i], new Vector3(250, i * 70, 0));
            state.motion = clip;
            state.writeDefaultValues = false;
        }
        if (!controller.parameters.Any(p => p.name == "Move_Speed")) controller.AddParameter("Move_Speed", AnimatorControllerParameterType.Float);
        string[] triggers = { "Attack_1", "Attack_2", "Block", "Hit", "Die" };
        string[] destinations = { "Attack", "Attack2", "Block", "Hit", "Die" };
        var idle = machine.states.Single(s => s.state.name == "Idle").state;
        var walk = machine.states.Single(s => s.state.name == "Walk").state;
        machine.defaultState = idle;
        Connect(idle, walk, false, AnimatorConditionMode.Greater);
        Connect(walk, idle, false, AnimatorConditionMode.Less);
        for (int i = 0; i < triggers.Length; i++)
        {
            if (!controller.parameters.Any(p => p.name == triggers[i])) controller.AddParameter(triggers[i], AnimatorControllerParameterType.Trigger);
            var state = machine.states.Single(s => s.state.name == destinations[i]).state;
            if (!machine.anyStateTransitions.Any(t => t.destinationState == state))
            {
                var transition = machine.AddAnyStateTransition(state);
                transition.duration = 0;
                transition.hasExitTime = false;
                transition.canTransitionToSelf = false;
                transition.AddCondition(AnimatorConditionMode.If, 0, triggers[i]);
            }
            if (destinations[i] == "Die") continue;
            Connect(state, idle, true, AnimatorConditionMode.Less);
            Connect(state, walk, true, AnimatorConditionMode.Greater);
        }
        EditorUtility.SetDirty(controller);
        foreach (string name in new[] { "GuardMinion", "SummonGuard" })
        {
            string path = "Assets/Gameplay/Prefabs/Minions/" + name + ".prefab";
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var renderer = prefab.GetComponent<SpriteRenderer>();
                renderer.sprite = sprites.Single(s => s.rect.x == 0 && s.rect.y == 672);
                renderer.color = Color.white;
                var animator = prefab.GetComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                var data = new SerializedObject(prefab.GetComponent<ActorAnimation>());
                data.FindProperty("animator").objectReferenceValue = animator;
                data.FindProperty("spriteRenderer").objectReferenceValue = renderer;
                data.FindProperty("artworkFacesRight").boolValue = true;
                data.FindProperty("useActionParameters").boolValue = true;
                data.FindProperty("useExistingSpeedParameter").boolValue = false;
                data.FindProperty("useDirectionalParameters").boolValue = false;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        AssetDatabase.SaveAssets();
        return "Blood slime: seven clips and parameter controller applied to GuardMinion and SummonGuard.";
    }

    private static void Connect(AnimatorState from, AnimatorState to, bool exit, AnimatorConditionMode mode)
    {
        if (from.transitions.Any(t => t.destinationState == to)) return;
        var transition = from.AddTransition(to);
        transition.hasExitTime = exit;
        transition.exitTime = 1;
        transition.duration = 0;
        transition.AddCondition(mode, 0.001f, "Move_Speed");
    }
}
