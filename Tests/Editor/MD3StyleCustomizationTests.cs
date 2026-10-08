using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using FontAsset = UnityEngine.TextCore.Text.FontAsset;

namespace AjisaiFlow.MD3SDK.Editor.Tests
{
    public class MD3StyleCustomizationTests
    {
        readonly string _temporaryAssetDirectory = "Assets/__MD3StyleTests_" + Guid.NewGuid().ToString("N");
        readonly List<UnityEngine.Object> _temporaryObjects = new();
        EditorWindow _window;
        bool _createdTemporaryAssetDirectory;

        [TearDown]
        public void TearDown()
        {
            if (_window != null) _window.Close();
            foreach (var asset in _temporaryObjects)
                if (asset != null) UnityEngine.Object.DestroyImmediate(asset);
            _temporaryObjects.Clear();
            if (_createdTemporaryAssetDirectory)
            {
                AssetDatabase.DeleteAsset(_temporaryAssetDirectory);
                _createdTemporaryAssetDirectory = false;
            }
        }

        [Test]
        public void CloningBuiltInThemesKeepsTheirDefaultsIndependent()
        {
            foreach (var original in new[] { MD3Theme.Dark(), MD3Theme.Light() })
            {
                var primary = original.Primary;
                var clone = original.Clone();
                Assert.That(clone, Is.Not.SameAs(original));
                Assert.That(clone.Primary, Is.EqualTo(primary));
                Assert.That(clone.IsDark, Is.EqualTo(original.IsDark));
                clone.Primary = Color.green;
                clone.IsDark = !original.IsDark;
                Assert.That(original.Primary, Is.EqualTo(primary));
                Assert.That(clone.IsDark, Is.Not.EqualTo(original.IsDark));
            }
        }

        [Test]
        public void ADetachedScopeResolvesItsOwnThemeAndNestedOverrides()
        {
            var parent = new VisualElement();
            var nested = new VisualElement();
            var leaf = new VisualElement();
            parent.Add(nested);
            nested.Add(leaf);
            var outerTheme = MD3Theme.Dark().Clone();
            var innerTheme = MD3Theme.Light().Clone();
            outerTheme.ApplyTo(parent);
            innerTheme.ApplyTo(nested);

            Assert.That(MD3Theme.Resolve(parent), Is.SameAs(outerTheme));
            Assert.That(MD3Theme.Resolve(nested), Is.SameAs(innerTheme));
            Assert.That(MD3Theme.Resolve(leaf), Is.SameAs(innerTheme));

            nested.RemoveFromHierarchy();
            Assert.That(MD3Theme.Resolve(nested), Is.SameAs(innerTheme));
            Assert.That(MD3Theme.Resolve(leaf), Is.SameAs(innerTheme));
        }

        [Test]
        public void ReapplyingABuiltInThemeReplacesACustomScope()
        {
            var root = new VisualElement();
            var child = new VisualElement();
            root.Add(child);
            var custom = MD3Theme.Light().Clone();
            custom.ApplyTo(root);
            MD3Theme.Dark().ApplyTo(root);

            Assert.That(MD3Theme.Resolve(root), Is.SameAs(MD3Theme.Dark()));
            Assert.That(MD3Theme.Resolve(child), Is.SameAs(MD3Theme.Dark()));
            Assert.That(root.ClassListContains("md3-dark"), Is.True);
            Assert.That(root.ClassListContains("md3-light"), Is.False);
        }

        [Test]
        public void ApplyingThemeDirectlyToComponentsPreservesTheirVisualRole()
        {
            var theme = MD3Theme.Light().Clone();
            theme.Primary = Color.green;
            theme.Surface = Color.red;
            var button = new MD3Button("Themed component");
            var text = new MD3Text("Themed text");

            theme.ApplyTo(button);
            theme.ApplyTo(text);

            Assert.That(MD3Theme.Resolve(button), Is.SameAs(theme));
            Assert.That(button.style.backgroundColor.value, Is.EqualTo(Color.green));
            Assert.That(text.style.backgroundColor.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(text.Q<Label>().style.color.value, Is.EqualTo(theme.OnSurface));
        }

        [Test]
        public void ClearFromRestoresParentThemeAndRefreshesComponents()
        {
            var parent = new VisualElement();
            var nested = new VisualElement();
            var button = new MD3Button("Nested");
            parent.Add(nested);
            nested.Add(button);
            var outerTheme = MD3Theme.Light().Clone();
            outerTheme.Primary = Color.green;
            var innerTheme = MD3Theme.Dark().Clone();
            innerTheme.Primary = Color.red;
            outerTheme.ApplyTo(parent);
            innerTheme.ApplyTo(nested);
            Assert.That(button.style.backgroundColor.value, Is.EqualTo(Color.red));

            MD3Theme.ClearFrom(nested);

            Assert.That(MD3Theme.Resolve(nested), Is.SameAs(outerTheme));
            Assert.That(MD3Theme.Resolve(button), Is.SameAs(outerTheme));
            Assert.That(button.style.backgroundColor.value, Is.EqualTo(Color.green));
            Assert.That(nested.ClassListContains("md3-dark"), Is.False);
            Assert.That(nested.ClassListContains("md3-light"), Is.False);
            Assert.That(nested.style.backgroundColor.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(nested.style.color.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(nested.style.unityFontDefinition.keyword, Is.EqualTo(StyleKeyword.Null));
        }

        [Test]
        public void ApplyToAddsBaseSheetsOnceAndCustomSheetsAfterThem()
        {
            var root = new VisualElement();
            var first = CreateObject<StyleSheet>();
            var second = CreateObject<StyleSheet>();
            var theme = MD3Theme.Light().Clone();
            theme.ApplyTo(root, first, null, second, first);
            theme.ApplyTo(root, first, second);

            var themeSheet = MD3Theme.LoadThemeStyleSheet();
            var componentSheet = MD3Theme.LoadComponentsStyleSheet();
            Assert.That(themeSheet, Is.Not.Null);
            Assert.That(componentSheet, Is.Not.Null);
            Assert.That(root.styleSheets.count, Is.EqualTo(4));
            Assert.That(root.styleSheets[0], Is.SameAs(themeSheet));
            Assert.That(root.styleSheets[1], Is.SameAs(componentSheet));
            Assert.That(root.styleSheets[2], Is.SameAs(first));
            Assert.That(root.styleSheets[3], Is.SameAs(second));
        }

        [Test]
        public void CustomFontAndFontAssetAreAppliedWithFontAssetTakingPriority()
        {
            var root = new VisualElement();
            var font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            _temporaryObjects.Add(font);
            var fontAsset = FontAsset.CreateFontAsset(Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));
            _temporaryObjects.Add(fontAsset);
            Assert.That(fontAsset, Is.Not.Null);
            var theme = MD3Theme.Light().Clone();
            theme.TextFont = font;
            theme.ApplyTo(root);
            Assert.That(root.style.unityFontDefinition.value.font, Is.SameAs(font));

            theme.TextFontAsset = fontAsset;
            theme.ApplyTo(root);
            Assert.That(root.style.unityFontDefinition.value.fontAsset, Is.SameAs(fontAsset));

            var clone = theme.Clone();
            Assert.That(clone.TextFont, Is.SameAs(font));
            Assert.That(clone.TextFontAsset, Is.SameAs(fontAsset));
        }

        [UnityTest]
        public IEnumerator ScopeSurvivesPanelDetachAndComponentsFollowReparenting()
        {
            var windowRoot = ShowWindow();
            var left = new VisualElement();
            var right = new VisualElement();
            var nested = new VisualElement();
            var scopedButton = new MD3Button("Scoped");
            var movingButton = new MD3Button("Moving");
            windowRoot.Add(left);
            windowRoot.Add(right);
            left.Add(nested);
            nested.Add(scopedButton);
            left.Add(movingButton);
            var leftTheme = MD3Theme.Light().Clone();
            leftTheme.Primary = Color.green;
            var rightTheme = MD3Theme.Light().Clone();
            rightTheme.Primary = Color.blue;
            var nestedTheme = MD3Theme.Dark().Clone();
            nestedTheme.Primary = Color.red;
            leftTheme.ApplyTo(left);
            rightTheme.ApplyTo(right);
            nestedTheme.ApplyTo(nested);
            yield return WaitForStyles();

            nested.RemoveFromHierarchy();
            Assert.That(MD3Theme.Resolve(nested), Is.SameAs(nestedTheme));
            Assert.That(MD3Theme.Resolve(scopedButton), Is.SameAs(nestedTheme));
            right.Add(nested);
            right.Add(movingButton);
            yield return WaitForStyles();

            Assert.That(MD3Theme.Resolve(scopedButton), Is.SameAs(nestedTheme));
            Assert.That(scopedButton.style.backgroundColor.value, Is.EqualTo(Color.red));
            Assert.That(MD3Theme.Resolve(movingButton), Is.SameAs(rightTheme));
            Assert.That(movingButton.style.backgroundColor.value, Is.EqualTo(Color.blue));

            MD3Theme.ClearFrom(nested);
            yield return WaitForStyles();
            Assert.That(scopedButton.style.backgroundColor.value, Is.EqualTo(Color.blue));
        }

        [UnityTest]
        public IEnumerator DirectComponentScopeKeepsItsThemeDuringInteractionAndClear()
        {
            var root = ShowWindow();
            var parentTheme = MD3Theme.Light().Clone();
            parentTheme.Primary = Color.blue;
            parentTheme.ApplyTo(root, Array.Empty<StyleSheet>());
            var button = new MD3Button("Scoped button");
            root.Add(button);
            var componentTheme = MD3Theme.Dark().Clone();
            componentTheme.Primary = Color.green;
            componentTheme.OnPrimary = Color.white;
            componentTheme.ApplyTo(button);
            yield return WaitForStyles();

            Assert.That(button.resolvedStyle.flexGrow, Is.EqualTo(0));
            using (var enter = MouseEnterEvent.GetPooled())
            {
                enter.target = button;
                button.SendEvent(enter);
            }
            yield return null;
            Assert.That(button.style.backgroundColor.value,
                Is.EqualTo(componentTheme.HoverOverlay(Color.green, Color.white)));
            using (var down = MouseDownEvent.GetPooled())
            {
                down.target = button;
                button.SendEvent(down);
            }
            yield return null;
            Assert.That(button.style.backgroundColor.value,
                Is.EqualTo(componentTheme.PressOverlay(Color.green, Color.white)));
            using (var leave = MouseLeaveEvent.GetPooled())
            {
                leave.target = button;
                button.SendEvent(leave);
            }
            yield return null;

            MD3Theme.ClearFrom(button);
            Assert.That(MD3Theme.Resolve(button), Is.SameAs(parentTheme));
            Assert.That(button.style.backgroundColor.value, Is.EqualTo(Color.blue));
        }

        [UnityTest]
        public IEnumerator FontRefreshPreservesRootAndNestedExplicitFonts()
        {
            var root = ShowWindow();
            var nested = new VisualElement();
            root.Add(nested);
            var outerFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
            var innerFont = Font.CreateDynamicFontFromOSFont("Times New Roman", 14);
            _temporaryObjects.Add(outerFont);
            _temporaryObjects.Add(innerFont);
            var outerTheme = MD3Theme.Light().Clone();
            var innerTheme = MD3Theme.Dark().Clone();
            outerTheme.TextFont = outerFont;
            innerTheme.TextFont = innerFont;
            outerTheme.ApplyTo(root);
            innerTheme.ApplyTo(nested);
            yield return WaitForStyles();

            MD3FontManager.RefreshAllWindows();
            yield return WaitForStyles();

            Assert.That(root.style.unityFontDefinition.value.font, Is.SameAs(outerFont));
            Assert.That(nested.style.unityFontDefinition.value.font, Is.SameAs(innerFont));
            MD3Theme.ClearFrom(nested);
            Assert.That(nested.style.unityFontDefinition.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(MD3Theme.Resolve(nested), Is.SameAs(outerTheme));
        }

        [Test]
        public void ApplyingAnOuterThemeLaterLetsAnExistingScopeInheritItsFont()
        {
            var root = new VisualElement();
            var section = new VisualElement();
            root.Add(section);
            MD3Theme.FromSeedColor(Color.cyan, false).ApplyTo(section);

            var outerFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
            _temporaryObjects.Add(outerFont);
            var outerTheme = MD3Theme.Light().Clone();
            outerTheme.TextFont = outerFont;
            outerTheme.ApplyTo(root);

            Assert.That(root.style.unityFontDefinition.value.font, Is.SameAs(outerFont));
            Assert.That(section.style.unityFontDefinition.keyword, Is.EqualTo(StyleKeyword.Null));
        }

        [UnityTest]
        public IEnumerator NestedScopesWithoutAFontInheritTheOuterCustomFont()
        {
            var root = ShowWindow();
            var section = new VisualElement();
            root.Add(section);
            var outerFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
            _temporaryObjects.Add(outerFont);
            var outerTheme = MD3Theme.Light().Clone();
            outerTheme.TextFont = outerFont;
            outerTheme.ApplyTo(root);
            MD3Theme.FromSeedColor(Color.cyan, false).ApplyTo(section);

            // Applied while detached, so nesting is only known once it is attached.
            var button = new MD3Button("Button");
            MD3Theme.Dark().Clone().ApplyTo(button);
            section.Add(button);
            yield return WaitForStyles();

            Assert.That(section.style.unityFontDefinition.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(button.style.unityFontDefinition.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(button.resolvedStyle.unityFontDefinition.font, Is.SameAs(outerFont));

            MD3FontManager.RefreshAllWindows();
            yield return WaitForStyles();

            Assert.That(root.style.unityFontDefinition.value.font, Is.SameAs(outerFont));
            Assert.That(section.style.unityFontDefinition.keyword, Is.EqualTo(StyleKeyword.Null));
            Assert.That(button.style.unityFontDefinition.keyword, Is.EqualTo(StyleKeyword.Null));
        }

        [UnityTest]
        public IEnumerator ClearingTextFieldRadiusOverrideRestoresUSSAndFilledCorners()
        {
            var root = ShowWindow();
            root.AddToClassList("brand");
            var outlined = new MD3TextField("Outlined");
            var filled = new MD3TextField("Filled", MD3TextFieldStyle.Filled);
            root.Add(outlined);
            root.Add(filled);
            var sheet = ImportStyleSheet("Fields", @"
.brand { --brand-field-radius: 11px; }
.brand .md3-textfield__container--outlined { border-radius: var(--brand-field-radius); }
.brand .md3-textfield__container--filled {
    border-top-left-radius: var(--brand-field-radius);
    border-top-right-radius: var(--brand-field-radius);
    border-bottom-left-radius: 0;
    border-bottom-right-radius: 0;
}
");
            MD3Theme.Light().ApplyTo(root, sheet);
            outlined.BorderRadius = 17;
            filled.BorderRadius = 17;
            yield return WaitForStyles();
            var outlinedContainer = outlined.Q(className: "md3-textfield__container");
            var filledContainer = filled.Q(className: "md3-textfield__container");
            Assert.That(outlinedContainer.resolvedStyle.borderTopLeftRadius, Is.EqualTo(17).Within(0.01));
            Assert.That(filledContainer.resolvedStyle.borderBottomLeftRadius, Is.EqualTo(17).Within(0.01));

            outlined.BorderRadius = null;
            filled.BorderRadius = null;
            yield return WaitForStyles();

            Assert.That(outlinedContainer.resolvedStyle.borderTopLeftRadius, Is.EqualTo(11).Within(0.01));
            Assert.That(outlinedContainer.resolvedStyle.borderBottomLeftRadius, Is.EqualTo(11).Within(0.01));
            Assert.That(filledContainer.resolvedStyle.borderTopLeftRadius, Is.EqualTo(11).Within(0.01));
            Assert.That(filledContainer.resolvedStyle.borderBottomLeftRadius, Is.EqualTo(0).Within(0.01));
            Assert.That(filledContainer.resolvedStyle.borderBottomRightRadius, Is.EqualTo(0).Within(0.01));
        }

        [UnityTest]
        public IEnumerator USSCanOverrideComponentShapeSpacingAndTextTypography()
        {
            var root = ShowWindow();
            root.AddToClassList("brand");
            var button = new MD3Button("Branded");
            var regularButton = new MD3Button("Regular");
            var text = new MD3Text("Body", MD3TextStyle.Body);
            button.AddToClassList("prominent");
            root.Add(button);
            root.Add(regularButton);
            root.Add(text);
            var sheet = ImportStyleSheet("Brand", @"
.brand .md3-button { border-radius: 7px; padding-left: 31px; padding-right: 31px; }
.brand .md3-button.prominent { border-radius: 3px; padding-left: 41px; }
.brand .md3-text--body .md3-text__label { font-size: 23px; -unity-font-style: bold; }
");
            MD3Theme.Light().Clone().ApplyTo(root, sheet);
            yield return WaitForStyles();

            Assert.That(button.resolvedStyle.borderTopLeftRadius, Is.EqualTo(3).Within(0.01));
            Assert.That(button.resolvedStyle.paddingLeft, Is.EqualTo(41).Within(0.5));
            Assert.That(regularButton.resolvedStyle.borderTopLeftRadius, Is.EqualTo(7).Within(0.01));
            Assert.That(regularButton.resolvedStyle.paddingLeft, Is.EqualTo(31).Within(0.5));
            var label = text.Q<Label>(className: "md3-text__label");
            Assert.That(label.resolvedStyle.fontSize, Is.EqualTo(23).Within(0.01));
            Assert.That(label.resolvedStyle.unityFontStyleAndWeight, Is.EqualTo(FontStyle.Bold));
        }

        [UnityTest]
        public IEnumerator SharedUSSVariablesCustomizeShapeSpacingAndTypographyAcrossTheScope()
        {
            var root = ShowWindow();
            root.AddToClassList("brand");
            var button = new MD3Button("Token defaults");
            var text = new MD3Text("Body", MD3TextStyle.Body);
            root.Add(button);
            root.Add(text);
            var sheet = ImportStyleSheet("Tokens", @"
.brand {
    --brand-radius: 9px;
    --brand-spacing: 35px;
    --brand-body-font-size: 21px;
}
.brand .md3-button {
    border-radius: var(--brand-radius);
    padding-left: var(--brand-spacing);
    padding-right: var(--brand-spacing);
}
.brand .md3-text--body .md3-text__label { font-size: var(--brand-body-font-size); }
");
            MD3Theme.Light().Clone().ApplyTo(root, sheet);
            yield return WaitForStyles();

            Assert.That(button.resolvedStyle.borderTopLeftRadius, Is.EqualTo(9).Within(0.01));
            Assert.That(button.resolvedStyle.paddingLeft, Is.EqualTo(35).Within(0.5));
            Assert.That(button.resolvedStyle.paddingRight, Is.EqualTo(35).Within(0.5));
            Assert.That(text.Q<Label>().resolvedStyle.fontSize, Is.EqualTo(21).Within(0.01));
        }

        [UnityTest]
        public IEnumerator DefaultButtonAndTypographySizesArePreserved()
        {
            var root = ShowWindow();
            var small = new MD3Button("Small", size: MD3ButtonSize.Small);
            var medium = new MD3Button("Medium");
            var large = new MD3Button("Large", size: MD3ButtonSize.Large);
            var body = new MD3Text("Body", MD3TextStyle.Body);
            var headline = new MD3Text("Headline", MD3TextStyle.HeadlineMedium);
            var unknown = new MD3Text("Unknown style", (MD3TextStyle)int.MaxValue);
            root.Add(small);
            root.Add(medium);
            root.Add(large);
            root.Add(body);
            root.Add(headline);
            root.Add(unknown);
            MD3Theme.Light().ApplyTo(root, Array.Empty<StyleSheet>());
            yield return WaitForStyles();

            Assert.That(small.resolvedStyle.height, Is.EqualTo(32).Within(0.01));
            Assert.That(medium.resolvedStyle.height, Is.EqualTo(40).Within(0.01));
            Assert.That(large.resolvedStyle.height, Is.EqualTo(48).Within(0.01));
            Assert.That(small.resolvedStyle.borderTopLeftRadius, Is.EqualTo(16).Within(0.01));
            Assert.That(medium.resolvedStyle.borderTopLeftRadius, Is.EqualTo(20).Within(0.01));
            Assert.That(large.resolvedStyle.borderTopLeftRadius, Is.EqualTo(24).Within(0.01));
            Assert.That(medium.resolvedStyle.paddingLeft, Is.EqualTo(24).Within(0.01));
            Assert.That(body.Q<Label>().resolvedStyle.fontSize, Is.EqualTo(14).Within(0.01));
            Assert.That(headline.Q<Label>().resolvedStyle.fontSize, Is.EqualTo(20).Within(0.01));
            Assert.That(headline.Q<Label>().resolvedStyle.unityFontStyleAndWeight, Is.EqualTo(FontStyle.Bold));
            Assert.That(unknown.Q<Label>().resolvedStyle.fontSize, Is.EqualTo(14).Within(0.01));
        }

        T CreateObject<T>() where T : ScriptableObject
        {
            var value = ScriptableObject.CreateInstance<T>();
            _temporaryObjects.Add(value);
            return value;
        }

        VisualElement ShowWindow()
        {
            _window = ScriptableObject.CreateInstance<MD3StyleTestWindow>();
            _window.position = new Rect(0, 0, 640, 480);
            _window.Show();
            return _window.rootVisualElement;
        }

        static IEnumerator WaitForStyles()
        {
            for (var frame = 0; frame < 10; frame++) yield return null;
        }

        StyleSheet ImportStyleSheet(string name, string source)
        {
            Directory.CreateDirectory(_temporaryAssetDirectory);
            _createdTemporaryAssetDirectory = true;
            var path = _temporaryAssetDirectory + "/" + name + ".uss";
            File.WriteAllText(path, source);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
            Assert.That(sheet, Is.Not.Null);
            return sheet;
        }
    }

    internal class MD3StyleTestWindow : EditorWindow { }
}
