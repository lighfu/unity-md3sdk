using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor.Tests
{
    public sealed class MD3AnimationTestWindow : EditorWindow { }

    public class MD3AnimationTests
    {
        MD3AnimationTestWindow _window;
        VisualElement _target;
        readonly List<MD3AnimationHandle> _handles = new List<MD3AnimationHandle>();

        [SetUp]
        public void SetUp()
        {
            _window = ScriptableObject.CreateInstance<MD3AnimationTestWindow>();
            _window.Show();
            _target = new VisualElement();
            _window.rootVisualElement.Add(_target);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var handle in _handles)
                handle.Cancel();
            _handles.Clear();
            if (_window != null)
                _window.Close();
        }

        [UnityTest]
        public IEnumerator FloatWithEnumEasingUpdatesEndpointBeforeCompleting()
        {
            var events = new List<string>();
            float value = 0f;
            bool complete = false;
            _handles.Add(MD3Animate.Float(_target, 2f, 6f, 48f, MD3Easing.EaseOutCubic, v =>
            {
                value = v;
                events.Add("update");
            }, () =>
            {
                events.Add("complete");
                complete = true;
            }));

            yield return WaitFor(() => complete);
            Assert.That(value, Is.EqualTo(6f).Within(0.0001f));
            Assert.That(events.Count, Is.GreaterThan(1));
            Assert.That(events[events.Count - 2], Is.EqualTo("update"));
            Assert.That(events[events.Count - 1], Is.EqualTo("complete"));
            int eventCount = events.Count;
            yield return WaitForDuration(100f);
            Assert.That(events.Count, Is.EqualTo(eventCount));
        }

        [UnityTest]
        public IEnumerator FloatWithCustomEasingKeepsUnclampedValues()
        {
            var values = new List<float>();
            bool complete = false;
            _handles.Add(MD3Animate.Float(_target, 2f, 6f, 48f, t => 1.25f,
                values.Add, () => complete = true));

            yield return WaitFor(() => complete);
            Assert.That(values, Is.Not.Empty);
            foreach (float value in values)
                Assert.That(value, Is.EqualTo(7f).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator CancelledFloatOverloadsDoNotUpdateOrComplete()
        {
            int updates = 0;
            int completions = 0;
            var enumHandle = MD3Animate.Float(_target, 0f, 1f, 48f, MD3Easing.Linear,
                v => updates++, () => completions++);
            var customHandle = MD3Animate.Float(_target, 0f, 1f, 48f, t => t,
                v => updates++, () => completions++);
            _handles.Add(enumHandle);
            _handles.Add(customHandle);
            enumHandle.Cancel();
            customHandle.Cancel();

            yield return WaitForDuration(150f);
            Assert.That(updates, Is.Zero);
            Assert.That(completions, Is.Zero);
        }

        [UnityTest]
        public IEnumerator TweenYoyoRepeatFinishesBeforeStartingChainedTween()
        {
            var events = new List<string>();
            float firstValue = -1f;
            float secondValue = -1f;
            bool complete = false;
            var tween = MD3Animate.Tween(_target).Duration(48f).Repeat(2).Yoyo()
                .Animate(0f, 10f, v => firstValue = v)
                .OnComplete(() => events.Add("first complete"));
            var next = tween.Then().Duration(48f)
                .Animate(0f, 20f, v =>
                {
                    secondValue = v;
                    events.Add("second update");
                })
                .OnComplete(() =>
                {
                    events.Add("second complete");
                    complete = true;
                });
            _handles.Add(next.Start());

            yield return WaitFor(() => complete);
            Assert.That(firstValue, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(secondValue, Is.EqualTo(20f).Within(0.0001f));
            Assert.That(events[0], Is.EqualTo("first complete"));
            Assert.That(events[events.Count - 1], Is.EqualTo("second complete"));
            Assert.That(events.FindAll(e => e == "first complete").Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SpringSettlesAtTargetAndCompletesAfterFinalUpdate()
        {
            var values = new List<float>();
            bool complete = false;
            float valueAtCompletion = 0f;
            _handles.Add(MD3Animate.Spring(_target, 0f, 1f, values.Add,
                stiffness: 400f, damping: 40f, onComplete: () =>
                {
                    valueAtCompletion = values[values.Count - 1];
                    complete = true;
                }));

            yield return WaitFor(() => complete);
            Assert.That(values.Count, Is.GreaterThan(1));
            Assert.That(valueAtCompletion, Is.EqualTo(1f));
            int updateCount = values.Count;
            yield return WaitForDuration(100f);
            Assert.That(values.Count, Is.EqualTo(updateCount));
        }

        [UnityTest]
        public IEnumerator TweenSpringCompletesBeforeStartingChainedTween()
        {
            float springValue = -1f;
            bool springComplete = false;
            bool complete = false;
            bool chainObservedSettledSpring = true;
            var tween = MD3Animate.Tween(_target)
                .Spring(0f, 1f, v => springValue = v, stiffness: 400f, damping: 40f)
                .OnComplete(() => springComplete = true);
            tween.Then().Duration(48f).Animate(0f, 2f, v =>
            {
                chainObservedSettledSpring &= springComplete && springValue == 1f;
            }).OnComplete(() => complete = true);
            _handles.Add(tween.Start());

            yield return WaitFor(() => complete);
            Assert.That(springValue, Is.EqualTo(1f));
            Assert.That(chainObservedSettledSpring, Is.True);
        }

        static IEnumerator WaitFor(Func<bool> condition)
        {
            double deadline = EditorApplication.timeSinceStartup + 5.0;
            while (!condition())
            {
                Assert.That(EditorApplication.timeSinceStartup, Is.LessThan(deadline),
                    "Animation did not complete within five seconds.");
                yield return null;
            }
        }

        static IEnumerator WaitForDuration(float durationMs)
        {
            double deadline = EditorApplication.timeSinceStartup + durationMs / 1000.0;
            while (EditorApplication.timeSinceStartup < deadline)
                yield return null;
        }
    }
}
