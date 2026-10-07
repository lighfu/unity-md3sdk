using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor.Tests
{
    public sealed class MD3TestWindow : EditorWindow { }

    // VisualElement.SendEvent requires a panel in Unity 2022.3. Keep the
    // temporary window alive only for tests that exercise UI event dispatch.
    internal sealed class MD3TestPanel : IDisposable
    {
        readonly MD3TestWindow _window;

        internal VisualElement Root => _window.rootVisualElement;

        internal MD3TestPanel()
        {
            _window = ScriptableObject.CreateInstance<MD3TestWindow>();
            _window.Show();
        }

        public void Dispose()
        {
            if (_window != null)
                _window.Close();
        }
    }
}
