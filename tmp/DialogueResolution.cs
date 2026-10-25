var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
var t = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
var window = UnityEditor.EditorWindow.GetWindow(t);
System.IO.File.WriteAllText("tmp/dialogue-gameview-index.txt", t.GetProperty("selectedSizeIndex", flags).GetValue(window).ToString());
var method = t.GetMethod("SetCustomResolution", flags);
method.Invoke(method.IsStatic ? null : window, new object[] { new UnityEngine.Vector2(1024,768), "Dialogue review 4 by 3" });
return "Game view set to 1024x768";
