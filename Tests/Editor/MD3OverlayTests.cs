using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor.Tests
{
    public class MD3OverlayTests
    {
        [TestCase("md3-light")]
        [TestCase("md3-dark")]
        public void FindThemedRootIncludesTheStartingElement(string themeClass)
        {
            var element = new VisualElement();
            element.AddToClassList(themeClass);

            Assert.That(MD3Overlay.FindThemedRoot(element), Is.SameAs(element));
        }

        [Test]
        public void FindThemedRootChoosesTheOutermostThemeAcrossUnthemedAncestors()
        {
            var outer = new VisualElement();
            outer.AddToClassList("md3-light");
            var container = new VisualElement();
            var inner = new VisualElement();
            inner.AddToClassList("md3-dark");
            var anchor = new VisualElement();
            outer.Add(container);
            container.Add(inner);
            inner.Add(anchor);

            Assert.That(MD3Overlay.FindThemedRoot(anchor), Is.SameAs(outer));
        }

        [Test]
        public void FindThemedRootLeavesUnthemedFallbackToTheCaller()
        {
            var parent = new VisualElement();
            var anchor = new VisualElement();
            parent.Add(anchor);

            Assert.That(MD3Overlay.FindThemedRoot(anchor), Is.Null);
            Assert.That(MD3Overlay.FindThemedRoot(null), Is.Null);
        }

        [Test]
        public void ScrimKeepsItsSharedStyleAndIsAddedAboveExistingChildren()
        {
            var root = new VisualElement();
            var content = new VisualElement();
            root.Add(content);
            var color = new Color(0f, 0f, 0f, 0.32f);

            var scrim = MD3Overlay.AddScrim(root, color, () => { });

            Assert.That(root[0], Is.SameAs(content));
            Assert.That(root[1], Is.SameAs(scrim));
            Assert.That(scrim.ClassListContains("md3-fab-speed-dial__scrim"), Is.True);
            Assert.That(scrim.style.backgroundColor.value, Is.EqualTo(color));
        }

        [Test]
        public void ScrimDismissesAndStopsTheClickFromBubbling()
        {
            using (var panel = new MD3TestPanel())
            {
                int dismissCount = 0;
                int parentClickCount = 0;
                panel.Root.RegisterCallback<ClickEvent>(evt => parentClickCount++);
                var scrim = MD3Overlay.AddScrim(panel.Root, Color.clear, () => dismissCount++);

                using (var evt = ClickEvent.GetPooled())
                {
                    evt.target = scrim;
                    scrim.SendEvent(evt);
                    Assert.That(evt.isPropagationStopped, Is.True);
                }

                Assert.That(dismissCount, Is.EqualTo(1));
                Assert.That(parentClickCount, Is.Zero);
            }
        }

        [Test]
        public void GeometryCallbackRunsOnlyOnce()
        {
            using (var panel = new MD3TestPanel())
            {
                var popup = new VisualElement();
                panel.Root.Add(popup);
                int callbackCount = 0;
                MD3Overlay.OnNextGeometry(popup, () => callbackCount++);

                SendGeometryChange(popup);
                SendGeometryChange(popup);

                Assert.That(callbackCount, Is.EqualTo(1));
            }
        }

        [Test]
        public void GeometryCallbackCanRegisterTheNextPlacement()
        {
            using (var panel = new MD3TestPanel())
            {
                var popup = new VisualElement();
                panel.Root.Add(popup);
                int callbackCount = 0;
                MD3Overlay.OnNextGeometry(popup, () =>
                {
                    callbackCount++;
                    MD3Overlay.OnNextGeometry(popup, () => callbackCount++);
                });

                SendGeometryChange(popup);
                Assert.That(callbackCount, Is.EqualTo(1));
                SendGeometryChange(popup);
                SendGeometryChange(popup);
                Assert.That(callbackCount, Is.EqualTo(2));
            }
        }

        static void SendGeometryChange(VisualElement element)
        {
            using (var evt = GeometryChangedEvent.GetPooled(Rect.zero, new Rect(0f, 0f, 100f, 40f)))
            {
                evt.target = element;
                element.SendEvent(evt);
            }
        }
    }
}
