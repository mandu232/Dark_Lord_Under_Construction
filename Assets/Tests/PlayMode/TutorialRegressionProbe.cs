using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DarkLord.Tests
{
    // Fallback for Editor test-runner filters returning zero results despite discovering these fixtures.
    // Runs the existing fixture setup/assertions/cleanup as real Play Mode coroutines; not a shipping component.
    public sealed class TutorialRegressionProbe : MonoBehaviour
    {
        private Exception failure;
        public Task<string> RunFixture(string fixtureName, params string[] selectedTests)
        {
            var type = typeof(TutorialRegressionProbe).Assembly.GetType("DarkLord.Tests." + fixtureName, true);
            var result = new TaskCompletionSource<string>();
            StartCoroutine(Run(type, result, selectedTests)); return result.Task;
        }

        private IEnumerator Traverse(IEnumerator routine)
        {
            while (failure == null)
            {
                bool next = false; object current = null;
                try { next = routine.MoveNext(); if (next) current = routine.Current; }
                catch (Exception e) { failure = e; }
                if (!next || failure != null) yield break;
                if (current is IEnumerator nested) yield return Traverse(nested);
                else yield return current;
            }
        }

        private IEnumerator Invoke(object fixture, MethodInfo method, object[] args = null)
        {
            object value = null;
            try { value = method.Invoke(fixture, args); }
            catch (Exception e) { failure = e.InnerException ?? e; }
            if (failure == null && value is IEnumerator routine) yield return Traverse(routine);
        }

        private IEnumerator Run(Type type, TaskCompletionSource<string> completion, string[] selectedTests)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            var setup = methods.Where(m => m.IsDefined(typeof(SetUpAttribute), true) || m.IsDefined(typeof(UnitySetUpAttribute), true)).ToArray();
            var cleanup = methods.Where(m => m.IsDefined(typeof(TearDownAttribute), true) || m.IsDefined(typeof(UnityTearDownAttribute), true)).ToArray();
            int passed = 0, total = 0; var failures = new List<string>();
            foreach (var test in methods.Where(m => (selectedTests == null || selectedTests.Length == 0 || selectedTests.Contains(m.Name)) && (m.IsDefined(typeof(TestAttribute), true) || m.IsDefined(typeof(UnityTestAttribute), true) || m.IsDefined(typeof(TestCaseAttribute), true))))
            {
                var cases = test.GetCustomAttributes<TestCaseAttribute>().Select(a => a.Arguments).ToArray();
                if (cases.Length == 0) cases = new[] { Array.Empty<object>() };
                foreach (var args in cases)
                {
                    total++; failure = null; var fixture = Activator.CreateInstance(type);
                    foreach (var method in setup) { yield return Invoke(fixture, method); if (failure != null) break; }
                    if (failure == null) yield return Invoke(fixture, test, args);
                    var error = failure;
                    failure = null; foreach (var method in cleanup) yield return Invoke(fixture, method);
                    error = error ?? failure;
                    if (error == null) passed++; else failures.Add(test.Name + ": " + error);
                    yield return null;
                }
            }
            completion.SetResult($"{type.Name}: {passed}/{total} passed\n" + string.Join("\n", failures));
        }
    }
}
