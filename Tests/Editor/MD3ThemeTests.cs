using NUnit.Framework;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor.Tests
{
    public class MD3ThemeTests
    {
        [Test]
        public void StyleSheetLoadersReturnTheCorrectCachedAssets()
        {
            var themeSheet = MD3Theme.LoadThemeStyleSheet();
            var componentsSheet = MD3Theme.LoadComponentsStyleSheet();
            Assert.That(themeSheet, Is.Not.Null);
            Assert.That(componentsSheet, Is.Not.Null);
            Assert.That(themeSheet.name, Is.EqualTo("MD3Theme"));
            Assert.That(componentsSheet.name, Is.EqualTo("MD3Components"));
            Assert.That(MD3Theme.LoadThemeStyleSheet(), Is.SameAs(themeSheet));
            Assert.That(MD3Theme.LoadComponentsStyleSheet(), Is.SameAs(componentsSheet));
        }

        [Test]
        public void ResolveUsesTheNearestThemeIncludingTheElementItself()
        {
            var outer = new VisualElement();
            outer.AddToClassList("md3-dark");
            var inner = new VisualElement();
            inner.AddToClassList("md3-light");
            var child = new VisualElement();
            outer.Add(inner);
            inner.Add(child);
            Assert.That(MD3Theme.Resolve(child), Is.SameAs(MD3Theme.Light()));
            Assert.That(MD3Theme.Resolve(inner), Is.SameAs(MD3Theme.Light()));
            Assert.That(MD3Theme.Resolve(outer), Is.SameAs(MD3Theme.Dark()));
        }

        [Test]
        public void RepeatedWindowStyleSheetSetupDoesNotDuplicateStyleSheets()
        {
            var root = new VisualElement();
            MD3Theme.AddStyleSheetsTo(root);
            MD3Theme.AddStyleSheetsTo(root);
            Assert.That(root.styleSheets.count, Is.EqualTo(2));
            Assert.That(root.styleSheets[0], Is.SameAs(MD3Theme.LoadThemeStyleSheet()));
            Assert.That(root.styleSheets[1], Is.SameAs(MD3Theme.LoadComponentsStyleSheet()));
        }
    }
}
