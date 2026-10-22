var summoning=UnityEngine.Object.FindFirstObjectByType<DarkLord.Player.MinionSummoning>();
var opened=summoning.TryRecruit();
UnityEngine.ScreenCapture.CaptureScreenshot("tmp/CorpseRecruitmentReview.png");
return opened;
