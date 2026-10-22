var waves=UnityEngine.Object.FindFirstObjectByType<DarkLord.Gameplay.WaveDirector>();waves.Tutorial.Advance(50);int n=0;while(waves.Tutorial.DialogueOpen&&n++<20)waves.Tutorial.NextDialogue();
return "Training target active";
