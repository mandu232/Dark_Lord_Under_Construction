using DarkLord.UI;
using DarkLord.Actors;
using System.Collections.Generic;
using UnityEngine;

namespace DarkLord.Gameplay
{
    public sealed partial class StartingTutorial
    {
        private GameMenu pauseMenu;
        private sealed class DialogueIdleSnapshot
        {
            public Animator animator;
            public AnimatorUpdateMode updateMode;
            public float speed;
            public int stateHash;
            public float stateTime;
            public readonly List<(int id, AnimatorControllerParameterType type, float value)> movement = new();
            public readonly List<int> triggers = new();
        }
        private readonly Dictionary<Animator, DialogueIdleSnapshot> dialogueIdles = new();
        private float nextDialogueIdleScan;
        public bool OwnsDialoguePause { get; private set; }
        public float DialogueResumeScale { get; private set; } = 1;
        private bool MenuOwnsPause
        {
            get
            {
                if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded) return false;
                if (pauseMenu == null)
                    foreach (var root in gameObject.scene.GetRootGameObjects())
                    { pauseMenu = root.GetComponentInChildren<GameMenu>(true); if (pauseMenu != null) break; }
                return pauseMenu != null && pauseMenu.OwnsPause;
            }
        }
        private void SynchronizeDialoguePause()
        {
            SynchronizePlayerExpression();
            if (!isActiveAndEnabled || !Active || !DialogueOpen) { ReleaseDialoguePause(); return; }
            if (!OwnsDialoguePause)
            {
                DarkLord.Player.PlayerHitStop.CancelActive();
                DialogueResumeScale = Time.timeScale > 0 ? Time.timeScale : 1;
                OwnsDialoguePause = true;
            }
            SynchronizeDialogueIdles();
            // Physics, combat timers, movement and projectiles remain paused.
            Time.timeScale = 0;
        }
        private void ReleaseDialoguePause()
        {
            RestoreDialogueIdles();
            storyAnimation?.SetDialogueIdle(false);
            if (!OwnsDialoguePause) return;
            OwnsDialoguePause = false;
            if (!MenuOwnsPause && (waves == null || waves.State != WaveState.RewardSelection) && Time.timeScale == 0)
                Time.timeScale = DialogueResumeScale;
        }

        private void SynchronizeDialogueIdles()
        {
            foreach (var saved in dialogueIdles.Values)
                if (saved.animator != null) saved.animator.speed = MenuOwnsPause ? 0 : saved.speed;
            if (Time.unscaledTime < nextDialogueIdleScan) return;
            nextDialogueIdleScan = Time.unscaledTime + .25f;
            foreach (var actor in FindObjectsByType<ActorHealth>(FindObjectsSortMode.None))
            {
                if (actor == null || !actor.IsAlive || !actor.isActiveAndEnabled || actor == waves.Lord || actor.gameObject.scene != gameObject.scene ||
                    actor.GetComponent<ActorAnimation>() == null) continue;
                var animator = actor.GetComponentInChildren<Animator>();
                if (animator == null || !animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) continue;
                if (!dialogueIdles.TryGetValue(animator, out var saved))
                {
                    int idle = Animator.StringToHash("Base Layer.Idle");
                    if (!animator.HasState(0, idle)) continue;
                    var state = animator.GetCurrentAnimatorStateInfo(0);
                    saved = new DialogueIdleSnapshot { animator = animator, updateMode = animator.updateMode, speed = animator.speed,
                        stateHash = state.fullPathHash, stateTime = state.normalizedTime };
                    foreach (var parameter in animator.parameters)
                    {
                        if (parameter.type == AnimatorControllerParameterType.Trigger)
                        {
                            if (animator.GetBool(parameter.nameHash)) saved.triggers.Add(parameter.nameHash);
                            animator.ResetTrigger(parameter.nameHash);
                        }
                        else if (parameter.name == "Move_Speed" || parameter.name == "Speed" || parameter.name == "speed")
                        {
                            if (parameter.type == AnimatorControllerParameterType.Float)
                            { saved.movement.Add((parameter.nameHash, parameter.type, animator.GetFloat(parameter.nameHash))); animator.SetFloat(parameter.nameHash, 0); }
                            else if (parameter.type == AnimatorControllerParameterType.Int)
                            { saved.movement.Add((parameter.nameHash, parameter.type, animator.GetInteger(parameter.nameHash))); animator.SetInteger(parameter.nameHash, 0); }
                        }
                    }
                    dialogueIdles.Add(animator, saved);
                    animator.updateMode = AnimatorUpdateMode.UnscaledTime;
                    animator.Play(idle, 0, 0);
                }
                animator.speed = MenuOwnsPause ? 0 : saved.speed;
            }
        }

        private void RestoreDialogueIdles()
        {
            foreach (var saved in dialogueIdles.Values)
            {
                var animator = saved.animator;
                if (animator == null) continue;
                animator.speed = saved.speed;
                animator.updateMode = saved.updateMode;
                if (!animator.isActiveAndEnabled || animator.runtimeAnimatorController == null) continue;
                foreach (var parameter in saved.movement)
                    if (parameter.type == AnimatorControllerParameterType.Float) animator.SetFloat(parameter.id, parameter.value);
                    else animator.SetInteger(parameter.id, Mathf.RoundToInt(parameter.value));
                foreach (int trigger in saved.triggers) animator.SetTrigger(trigger);
                if (saved.stateHash != 0 && animator.HasState(0, saved.stateHash))
                    animator.Play(saved.stateHash, 0, saved.stateTime);
            }
            dialogueIdles.Clear();
            nextDialogueIdleScan = 0;
        }
    }
}
