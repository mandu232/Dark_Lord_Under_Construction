using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace DarkLord.Editor
{
    public static class GameplayValidation
    {
        private static TestRunnerApi api;

        [InitializeOnLoadMethod]
        private static void Register()
        {
            api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Results());
        }

        [MenuItem("Tools/Dark Lord/Run Gameplay Tests")]
        public static void Run()
        {
            if (api == null) Register();
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.PlayMode,
                assemblyNames = new[] { "DarkLord.PlayModeTests" }
            }));
        }

        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                Directory.CreateDirectory("Logs");
                TestRunnerApi.SaveResultToFile(result, "Logs/GameplayTests.xml");
                Debug.Log($"Gameplay tests: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped.");
            }
        }
    }
}
