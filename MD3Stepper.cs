using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public class MD3Stepper : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3Stepper, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        public event Action<int> stepChanged;

        readonly string[] _stepLabels;
        readonly VisualElement[] _stepNodes;
        readonly VisualElement[] _circles;
        readonly Label[] _numberLabels;
        readonly Label[] _textLabels;
        readonly VisualElement[] _connectors;
        MD3Theme _theme;

        public int CurrentStep
        {
            get => _currentStep;
            set
            {
                if (value < 0) value = 0;
                if (value >= _stepLabels.Length) value = _stepLabels.Length - 1;
                if (_currentStep == value) return;
                _currentStep = value;
                ApplyColors();
                stepChanged?.Invoke(_currentStep);
            }
        }
        int _currentStep;

        public MD3Stepper() : this(new[] { "Step 1", "Step 2", "Step 3" }) { }

        public MD3Stepper(string[] stepLabels, int currentStep = 0, Action<int> onStepChanged = null)
        {
            _stepLabels = stepLabels;
            _currentStep = currentStep;
            if (onStepChanged != null) stepChanged += onStepChanged;

            AddToClassList("md3-stepper");

            _stepNodes = new VisualElement[stepLabels.Length];
            _circles = new VisualElement[stepLabels.Length];
            _numberLabels = new Label[stepLabels.Length];
            _textLabels = new Label[stepLabels.Length];
            _connectors = stepLabels.Length > 1 ? new VisualElement[stepLabels.Length - 1] : Array.Empty<VisualElement>();

            for (int i = 0; i < stepLabels.Length; i++)
            {
                // Step node (column: circle + label)
                var node = new VisualElement();
                node.AddToClassList("md3-stepper__step");
                _stepNodes[i] = node;

                // Circle
                var circle = new VisualElement();
                circle.AddToClassList("md3-stepper__circle");
                var numberLabel = new Label((i + 1).ToString());
                numberLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                numberLabel.style.fontSize = 14;
                numberLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                numberLabel.pickingMode = PickingMode.Ignore;
                circle.style.alignItems = Align.Center;
                circle.style.justifyContent = Justify.Center;
                circle.Add(numberLabel);
                _circles[i] = circle;
                _numberLabels[i] = numberLabel;
                node.Add(circle);

                // Label
                var textLabel = new Label(stepLabels[i]);
                textLabel.AddToClassList("md3-stepper__label");
                textLabel.pickingMode = PickingMode.Ignore;
                _textLabels[i] = textLabel;
                node.Add(textLabel);

                Add(node);

                // Connector (except after last step)
                if (i < stepLabels.Length - 1)
                {
                    var connector = new VisualElement();
                    connector.AddToClassList("md3-stepper__connector");
                    _connectors[i] = connector;
                    Add(connector);
                }
            }

            RegisterCallback<AttachToPanelEvent>(OnAttach);
        }

        public void RefreshTheme()
        {
            _theme = ResolveTheme();
            ApplyColors();
        }

        void OnAttach(AttachToPanelEvent evt) => RefreshTheme();

        void ApplyColors()
        {
            if (_theme == null) _theme = ResolveTheme();
            if (_theme == null) return;

            for (int i = 0; i < _stepLabels.Length; i++)
            {
                bool completed = i < _currentStep;
                bool active = i == _currentStep;

                // Circle
                if (completed || active)
                {
                    _circles[i].style.backgroundColor = _theme.Primary;
                    _circles[i].style.borderTopWidth = 0;
                    _circles[i].style.borderBottomWidth = 0;
                    _circles[i].style.borderLeftWidth = 0;
                    _circles[i].style.borderRightWidth = 0;
                    _numberLabels[i].style.color = _theme.OnPrimary;
                }
                else
                {
                    _circles[i].style.backgroundColor = Color.clear;
                    _circles[i].style.borderTopWidth = 2;
                    _circles[i].style.borderBottomWidth = 2;
                    _circles[i].style.borderLeftWidth = 2;
                    _circles[i].style.borderRightWidth = 2;
                    _circles[i].style.borderTopColor = _theme.Outline;
                    _circles[i].style.borderBottomColor = _theme.Outline;
                    _circles[i].style.borderLeftColor = _theme.Outline;
                    _circles[i].style.borderRightColor = _theme.Outline;
                    _numberLabels[i].style.color = _theme.OnSurfaceVariant;
                }

                // Number or checkmark
                _numberLabels[i].text = completed ? MD3Icon.Check : (i + 1).ToString();
                if (completed)
                    MD3Icon.Apply(_numberLabels[i], 14f);
                else
                {
                    _numberLabels[i].style.unityFont = StyleKeyword.Null;
                    _numberLabels[i].style.unityFontDefinition = StyleKeyword.Null;
                }

                // Step label
                if (active)
                    _textLabels[i].style.color = _theme.Primary;
                else if (completed)
                    _textLabels[i].style.color = _theme.OnSurface;
                else
                    _textLabels[i].style.color = _theme.OnSurfaceVariant;
            }

            // Connectors
            for (int i = 0; i < _connectors.Length; i++)
            {
                bool completed = i < _currentStep;
                _connectors[i].style.backgroundColor = completed ? _theme.Primary : _theme.OutlineVariant;
            }
        }

        MD3Theme ResolveTheme() => MD3Theme.Resolve(this);
    }
}
