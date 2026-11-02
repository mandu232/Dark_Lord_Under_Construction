var node = new UnityEngine.GameObject("FGT movement regression probe");
UnityEngine.Object.DontDestroyOnLoad(node);
var pending = node.AddComponent<DarkLord.Tests.TutorialRegressionProbe>().RunFixture("CorpseRecruitmentTests", "CarryingCorpseSlowsWalkingAndRestoresSpeedAfterDropAndReload", "DodgeUsesSameFrameMovementInputAndReturnsToWalking", "WalkingBoostDoesNotIncreaseDodgeSpeed", "MeleeUsesDirectionalSlashAndReturnsToLocomotionWithoutShadowingTheEffect", "FKeyCarriesBodyBeforeOpeningRecruitmentAndAllowsWalking", "ClickingCorpseAttacksWithoutDraggingOrConsumingIt");
var output = System.IO.Path.GetFullPath("Logs/FGTMovementResults.txt");
pending.ContinueWith(t => System.IO.File.WriteAllText(output, t.IsFaulted ? t.Exception.ToString() : t.Result));
return "Movement checks started; results will be written to Logs/FGTMovementResults.txt";
