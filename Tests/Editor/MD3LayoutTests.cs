using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor.Tests
{
    public sealed class MD3LayoutTestWindow : EditorWindow { }

    public class MD3LayoutTests
    {
        [TestCase("Row")]
        [TestCase("Column")]
        [TestCase("ScrollColumn")]
        public void GapKeepsOuterMarginsClearAndUpdatesAfterChildrenChange(string kind)
        {
            var window = ScriptableObject.CreateInstance<MD3LayoutTestWindow>();
            try
            {
                window.Show();
                VisualElement layout = kind == "Row" ? new MD3Row(12f)
                    : kind == "Column" ? new MD3Column(12f)
                    : (VisualElement)new MD3ScrollColumn(12f, 8f);
                var first = new VisualElement();
                var last = new VisualElement();
                layout.Add(first);
                layout.Add(last);
                window.rootVisualElement.Add(layout);
                SendGeometryChange(layout.contentContainer);

                AssertMargins(first, kind, 0f, 6f);
                AssertMargins(last, kind, 6f, 0f);

                var added = new VisualElement();
                layout.Add(added);
                SendGeometryChange(layout.contentContainer);
                AssertMargins(last, kind, 6f, 6f);
                AssertMargins(added, kind, 6f, 0f);

                layout.Remove(first);
                SendGeometryChange(layout.contentContainer);
                AssertMargins(last, kind, 0f, 6f);
                if (kind == "ScrollColumn")
                    Assert.That(layout.contentContainer.style.paddingTop.value.value, Is.EqualTo(8f));
            }
            finally
            {
                window.Close();
            }
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        public void NonpositiveOrNaNGapDoesNotOverwriteChildMargins(float gap)
        {
            var window = ScriptableObject.CreateInstance<MD3LayoutTestWindow>();
            try
            {
                window.Show();
                var column = new MD3Column(gap);
                var child = new VisualElement().Margin(3f);
                column.Add(child);
                window.rootVisualElement.Add(column);
                SendGeometryChange(column);
                Assert.That(child.style.marginTop.value.value, Is.EqualTo(3f));
                Assert.That(child.style.marginBottom.value.value, Is.EqualTo(3f));
            }
            finally
            {
                window.Close();
            }
        }

        static void AssertMargins(VisualElement element, string kind, float before, float after)
        {
            Assert.That((kind == "Row" ? element.style.marginLeft : element.style.marginTop).value.value,
                Is.EqualTo(before));
            Assert.That((kind == "Row" ? element.style.marginRight : element.style.marginBottom).value.value,
                Is.EqualTo(after));
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
