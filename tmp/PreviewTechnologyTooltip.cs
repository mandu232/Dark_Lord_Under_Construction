var panel=UnityEngine.Object.FindFirstObjectByType<DarkLord.UI.CoreTechPanel>();
panel.ShowTooltip(DarkLord.Player.TechId.Aegis);
UnityEngine.ScreenCapture.CaptureScreenshot("tmp/TechnologyTooltipReview.png");
return panel.TooltipText;
