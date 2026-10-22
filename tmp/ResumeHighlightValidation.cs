var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Unity.Pipeline.Editor.Testing.PipelineTestRunner")).First(t=>t!=null);
var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static;
var api=(UnityEditor.TestTools.TestRunner.Api.TestRunnerApi)type.GetField("m_ActiveApi",flags).GetValue(null);
var collector=(UnityEditor.TestTools.TestRunner.Api.ICallbacks)type.GetField("m_ActiveCollector",flags).GetValue(null);
var fixture=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("DarkLord.Tests.CorpseTrainingTutorialTests")).First(t=>t!=null);
var tests=fixture.GetMethods().Where(m=>m.GetCustomAttributes(false).Any(a=>a.GetType().Name=="UnityTestAttribute")).Select(m=>fixture.FullName+"."+m.Name).ToArray();
api.RegisterCallbacks(collector);var id=api.Execute(new UnityEditor.TestTools.TestRunner.Api.ExecutionSettings(new UnityEditor.TestTools.TestRunner.Api.Filter{testMode=UnityEditor.TestTools.TestRunner.Api.TestMode.PlayMode,testNames=tests}));return new{id,tests=tests.Length};
