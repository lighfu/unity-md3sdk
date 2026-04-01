using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public enum MD3Easing
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut,
        /// <summary>オーバーシュート付き EaseOut (反動アニメーション)。</summary>
        EaseOutBack,
        EaseInBack,
        EaseInOutBack,
        EaseInCubic,
        EaseOutCubic,
        EaseInOutCubic,
        EaseInQuart,
        EaseOutQuart,
        EaseInOutQuart,
        EaseOutElastic,
        EaseOutBounce,
    }

    public class MD3AnimationHandle
    {
        internal bool Cancelled;
        public void Cancel() => Cancelled = true;
    }

    // ── Keyframe ─────────────────────────────────────────

    /// <summary>
    /// A keyframe for multi-segment animation.
    /// Time is normalized 0..1 within the total duration.
    /// </summary>
    public struct MD3Keyframe
    {
        public float Time;
        public float Value;
        public MD3Easing Easing;

        public MD3Keyframe(float time, float value, MD3Easing easing = MD3Easing.Linear)
        {
            Time = time;
            Value = value;
            Easing = easing;
        }
    }

    // ── Tween (fluent builder) ───────────────────────────

    /// <summary>
    /// Fluent animation builder. Supports multi-property, keyframes,
    /// loop, delay, sequence (.Then), parallel (.With), and spring.
    /// </summary>
    public class MD3Tween
    {
        readonly VisualElement _target;
        float _durationMs = 300f;
        float _delayMs = 0f;
        MD3Easing _easing = MD3Easing.EaseInOut;
        Func<float, float> _customEasing;
        int _repeatCount = 1; // -1 = infinite
        bool _yoyo;
        Action _onComplete;
        Action<float> _onUpdate;

        // Multi-property channels
        readonly List<TweenChannel> _channels = new List<TweenChannel>();

        // Keyframe mode
        MD3Keyframe[] _keyframes;

        // Spring
        bool _isSpring;
        float _springTarget;
        float _springStiffness = 180f;
        float _springDamping = 12f;
        float _springMass = 1f;
        float _springInitial;
        Action<float> _springUpdate;

        // Chaining
        MD3Tween _then;  // sequential next
        MD3Tween _root;  // root of the chain (for .Start() from any node)
        readonly List<MD3Tween> _with = new List<MD3Tween>(); // parallel

        struct TweenChannel
        {
            public float From;
            public float To;
            public Action<float> OnUpdate;
        }

        public MD3Tween(VisualElement target)
        {
            _target = target;
        }

        // ── Configuration ──

        public MD3Tween Duration(float ms) { _durationMs = ms; return this; }
        public MD3Tween Delay(float ms) { _delayMs = ms; return this; }
        public MD3Tween Ease(MD3Easing easing) { _easing = easing; _customEasing = null; return this; }
        public MD3Tween Ease(Func<float, float> customEasing) { _customEasing = customEasing; return this; }
        public MD3Tween Repeat(int count = -1) { _repeatCount = count; return this; }
        public MD3Tween Yoyo(bool yoyo = true) { _yoyo = yoyo; return this; }
        public MD3Tween OnComplete(Action callback) { _onComplete = callback; return this; }
        public MD3Tween OnUpdate(Action<float> callback) { _onUpdate = callback; return this; }

        // ── Property channels ──

        /// <summary>Animate a float value.</summary>
        public MD3Tween Animate(float from, float to, Action<float> onUpdate)
        {
            _channels.Add(new TweenChannel { From = from, To = to, OnUpdate = onUpdate });
            return this;
        }

        /// <summary>Animate opacity.</summary>
        public MD3Tween Opacity(float from, float to)
        {
            return Animate(from, to, v => _target.style.opacity = v);
        }

        /// <summary>Animate uniform scale.</summary>
        public MD3Tween Scale(float from, float to)
        {
            return Animate(from, to, v => _target.style.scale = new Scale(new Vector3(v, v, 1f)));
        }

        /// <summary>Animate rotation in degrees.</summary>
        public MD3Tween Rotate(float fromDeg, float toDeg)
        {
            return Animate(fromDeg, toDeg, v => _target.style.rotate = new Rotate(v));
        }

        /// <summary>Animate translateX.</summary>
        public MD3Tween TranslateX(float from, float to)
        {
            return Animate(from, to, v => _target.style.translate = new Translate(v, _target.resolvedStyle.translate.y));
        }

        /// <summary>Animate translateY.</summary>
        public MD3Tween TranslateY(float from, float to)
        {
            return Animate(from, to, v => _target.style.translate = new Translate(_target.resolvedStyle.translate.x, v));
        }

        /// <summary>Animate width.</summary>
        public MD3Tween Width(float from, float to)
        {
            return Animate(from, to, v => _target.style.width = v);
        }

        /// <summary>Animate height.</summary>
        public MD3Tween Height(float from, float to)
        {
            return Animate(from, to, v => _target.style.height = v);
        }

        /// <summary>Animate background color.</summary>
        public MD3Tween BackgroundColor(Color from, Color to)
        {
            return Animate(0f, 1f, t => _target.style.backgroundColor = UnityEngine.Color.LerpUnclamped(from, to, t));
        }

        /// <summary>Animate text color.</summary>
        public MD3Tween TextColor(Color from, Color to)
        {
            return Animate(0f, 1f, t => _target.style.color = UnityEngine.Color.LerpUnclamped(from, to, t));
        }

        // ── Keyframes ──

        /// <summary>
        /// Set keyframes for piecewise animation.
        /// Each keyframe's Time is 0..1 (normalized). The easing in each
        /// keyframe applies to the segment FROM the previous keyframe TO this one.
        /// </summary>
        public MD3Tween Keyframes(params MD3Keyframe[] keyframes)
        {
            _keyframes = keyframes;
            return this;
        }

        // ── Spring ──

        /// <summary>
        /// Use spring physics instead of duration-based easing.
        /// The animation runs until the spring settles.
        /// </summary>
        public MD3Tween Spring(float from, float to, Action<float> onUpdate,
            float stiffness = 180f, float damping = 12f, float mass = 1f)
        {
            _isSpring = true;
            _springInitial = from;
            _springTarget = to;
            _springStiffness = stiffness;
            _springDamping = damping;
            _springMass = mass;
            _springUpdate = onUpdate;
            return this;
        }

        // ── Chaining ──

        /// <summary>Run another tween after this one completes.</summary>
        public MD3Tween Then(MD3Tween next) { next._root = _root ?? this; _then = next; return this; }

        /// <summary>Create and chain a new tween on the same target.</summary>
        public MD3Tween Then()
        {
            var next = new MD3Tween(_target);
            next._root = _root ?? this; // propagate root
            _then = next;
            return next;
        }

        /// <summary>Run another tween in parallel with this one.</summary>
        public MD3Tween With(MD3Tween parallel) { _with.Add(parallel); return this; }

        /// <summary>Create and run a new tween in parallel on the same target.</summary>
        public MD3Tween With()
        {
            var parallel = new MD3Tween(_target);
            _with.Add(parallel);
            return parallel;
        }

        // ── Execution ──

        /// <summary>Start the tween. If called from a chained node, starts from the root.</summary>
        public MD3AnimationHandle Start()
        {
            // If this is a chained child, delegate to root
            if (_root != null)
                return _root.Start();

            return StartSelf();
        }

        MD3AnimationHandle StartSelf()
        {
            var handle = new MD3AnimationHandle();

            // Start parallel tweens
            foreach (var w in _with)
                w.StartSelf();

            if (_delayMs > 0f)
            {
                MD3Animate.Delayed(_target, _delayMs, () =>
                {
                    if (!handle.Cancelled)
                        RunCore(handle);
                });
            }
            else
            {
                RunCore(handle);
            }

            return handle;
        }

        void RunCore(MD3AnimationHandle handle)
        {
            if (_isSpring)
            {
                RunSpring(handle);
                return;
            }

            int iteration = 0;
            RunIteration(handle, iteration);
        }

        void RunIteration(MD3AnimationHandle handle, int iteration)
        {
            bool reverse = _yoyo && (iteration % 2 == 1);

            MD3Animate.Float(_target, 0f, 1f, _durationMs, MD3Easing.Linear, rawT =>
            {
                if (handle.Cancelled) return;

                float t = reverse ? 1f - rawT : rawT;

                float value;
                if (_keyframes != null && _keyframes.Length > 0)
                    value = EvaluateKeyframes(t);
                else
                    value = ApplyEasingFunc(t);

                // Update all channels
                foreach (var ch in _channels)
                    ch.OnUpdate(Mathf.LerpUnclamped(ch.From, ch.To, value));

                _onUpdate?.Invoke(value);
            }, () =>
            {
                if (handle.Cancelled) return;

                iteration++;
                bool shouldRepeat = _repeatCount < 0 || iteration < _repeatCount;

                if (shouldRepeat)
                {
                    RunIteration(handle, iteration);
                }
                else
                {
                    _onComplete?.Invoke();
                    _then?.StartSelf();
                }
            });
        }

        void RunSpring(MD3AnimationHandle handle)
        {
            float pos = _springInitial;
            float vel = 0f;
            bool settled = false;
            double lastTime = EditorApplication.timeSinceStartup;

            MD3AnimLoop.Register(_target);

            _target.schedule.Execute(() =>
            {
                if (handle.Cancelled || settled) return;

                double now = EditorApplication.timeSinceStartup;
                float dt = Mathf.Min((float)(now - lastTime), 0.033f);
                lastTime = now;

                float displacement = pos - _springTarget;
                float springForce = -_springStiffness * displacement;
                float dampingForce = -_springDamping * vel;
                float acceleration = (springForce + dampingForce) / _springMass;

                vel += acceleration * dt;
                pos += vel * dt;

                _springUpdate(pos);

                if (Mathf.Abs(vel) < 0.5f && Mathf.Abs(pos - _springTarget) < 0.005f)
                {
                    settled = true;
                    pos = _springTarget;
                    _springUpdate(pos);
                    MD3AnimLoop.Unregister(_target);
                    _onComplete?.Invoke();
                    _then?.StartSelf();
                }
            }).Every(16).Until(() => handle.Cancelled || settled);
        }

        float ApplyEasingFunc(float t)
        {
            if (_customEasing != null)
                return _customEasing(t);
            return MD3Animate.ApplyEasing(t, _easing);
        }

        float EvaluateKeyframes(float t)
        {
            if (_keyframes.Length == 0) return t;
            if (t <= _keyframes[0].Time) return _keyframes[0].Value;
            if (t >= _keyframes[_keyframes.Length - 1].Time) return _keyframes[_keyframes.Length - 1].Value;

            for (int i = 1; i < _keyframes.Length; i++)
            {
                if (t <= _keyframes[i].Time)
                {
                    var prev = _keyframes[i - 1];
                    var curr = _keyframes[i];
                    float segT = (t - prev.Time) / (curr.Time - prev.Time);
                    float eased = MD3Animate.ApplyEasing(segT, curr.Easing);
                    return Mathf.LerpUnclamped(prev.Value, curr.Value, eased);
                }
            }

            return _keyframes[_keyframes.Length - 1].Value;
        }
    }

    // ── Static API (backward compatible + new) ───────────

    public static class MD3Animate
    {
        // ── Existing API (backward compatible) ──

        public static MD3AnimationHandle Float(
            VisualElement target,
            float from, float to,
            float durationMs,
            MD3Easing easing,
            Action<float> onUpdate,
            Action onComplete = null)
        {
            var handle = new MD3AnimationHandle();
            double startTime = EditorApplication.timeSinceStartup;

            MD3AnimLoop.Register(target);

            target.schedule.Execute(() =>
            {
                if (handle.Cancelled)
                {
                    MD3AnimLoop.Unregister(target);
                    return;
                }

                float elapsed = (float)((EditorApplication.timeSinceStartup - startTime) * 1000.0);
                float t = Mathf.Clamp01(elapsed / durationMs);
                float eased = ApplyEasing(t, easing);
                float value = Mathf.LerpUnclamped(from, to, eased);
                onUpdate(value);

                if (t >= 1f)
                {
                    MD3AnimLoop.Unregister(target);
                    onComplete?.Invoke();
                }
            }).Every(16).Until(() =>
            {
                bool done = handle.Cancelled || (float)((EditorApplication.timeSinceStartup - startTime) * 1000.0) >= durationMs;
                if (done) MD3AnimLoop.Unregister(target);
                return done;
            });

            return handle;
        }

        /// <summary>Float with custom easing function.</summary>
        public static MD3AnimationHandle Float(
            VisualElement target,
            float from, float to,
            float durationMs,
            Func<float, float> easingFunc,
            Action<float> onUpdate,
            Action onComplete = null)
        {
            var handle = new MD3AnimationHandle();
            double startTime = EditorApplication.timeSinceStartup;

            MD3AnimLoop.Register(target);

            target.schedule.Execute(() =>
            {
                if (handle.Cancelled)
                {
                    MD3AnimLoop.Unregister(target);
                    return;
                }

                float elapsed = (float)((EditorApplication.timeSinceStartup - startTime) * 1000.0);
                float t = Mathf.Clamp01(elapsed / durationMs);
                float eased = easingFunc(t);
                float value = Mathf.LerpUnclamped(from, to, eased);
                onUpdate(value);

                if (t >= 1f)
                {
                    MD3AnimLoop.Unregister(target);
                    onComplete?.Invoke();
                }
            }).Every(16).Until(() =>
            {
                bool done = handle.Cancelled || (float)((EditorApplication.timeSinceStartup - startTime) * 1000.0) >= durationMs;
                if (done) MD3AnimLoop.Unregister(target);
                return done;
            });

            return handle;
        }

        public static MD3AnimationHandle FadeScale(
            VisualElement target,
            float fromOpacity, float toOpacity,
            float fromScale, float toScale,
            float durationMs,
            MD3Easing easing,
            Action onComplete = null)
        {
            return Float(target, 0f, 1f, durationMs, easing, t =>
            {
                float opacity = Mathf.LerpUnclamped(fromOpacity, toOpacity, t);
                float scale = Mathf.LerpUnclamped(fromScale, toScale, t);
                target.style.opacity = opacity;
                target.style.scale = new Scale(new Vector3(scale, scale, 1f));
            }, onComplete);
        }

        public static MD3AnimationHandle Delayed(VisualElement target, float delayMs, Action callback)
        {
            var handle = new MD3AnimationHandle();
            target.schedule.Execute(() =>
            {
                if (!handle.Cancelled)
                    callback?.Invoke();
            }).ExecuteLater((long)delayMs);
            return handle;
        }

        // ── New: Fluent tween builder ──

        /// <summary>Create a fluent tween builder for the target element.</summary>
        public static MD3Tween Tween(VisualElement target) => new MD3Tween(target);

        // ── New: Keyframe animation ──

        /// <summary>
        /// Animate through keyframes. Each keyframe specifies a normalized time (0..1)
        /// and a value. The easing in each keyframe applies to the segment leading to it.
        /// </summary>
        public static MD3AnimationHandle Keyframes(
            VisualElement target,
            float durationMs,
            MD3Keyframe[] keyframes,
            Action<float> onUpdate,
            Action onComplete = null)
        {
            return Float(target, 0f, 1f, durationMs, MD3Easing.Linear, rawT =>
            {
                float value = EvaluateKeyframes(keyframes, rawT);
                onUpdate(value);
            }, onComplete);
        }

        // ── New: Spring animation ──

        /// <summary>
        /// Animate with spring physics. No fixed duration — runs until settled.
        /// </summary>
        public static MD3AnimationHandle Spring(
            VisualElement target,
            float from, float to,
            Action<float> onUpdate,
            float stiffness = 180f,
            float damping = 12f,
            float mass = 1f,
            Action onComplete = null)
        {
            var handle = new MD3AnimationHandle();
            float pos = from;
            float vel = 0f;
            double lastTime = EditorApplication.timeSinceStartup;

            bool settled = false;
            MD3AnimLoop.Register(target);

            target.schedule.Execute(() =>
            {
                if (handle.Cancelled || settled) return;

                double now = EditorApplication.timeSinceStartup;
                float dt = Mathf.Min((float)(now - lastTime), 0.033f);
                lastTime = now;

                float displacement = pos - to;
                float springForce = -stiffness * displacement;
                float dampingForce = -damping * vel;
                float acceleration = (springForce + dampingForce) / mass;

                vel += acceleration * dt;
                pos += vel * dt;

                onUpdate(pos);

                if (Mathf.Abs(vel) < 0.5f && Mathf.Abs(pos - to) < 0.005f)
                {
                    settled = true;
                    pos = to;
                    onUpdate(pos);
                    MD3AnimLoop.Unregister(target);
                    onComplete?.Invoke();
                }
            }).Every(16).Until(() => handle.Cancelled || settled);

            return handle;
        }

        // ── New: Sequence helper ──

        /// <summary>
        /// Run multiple animations sequentially. Each Action receives an onComplete
        /// callback that must be invoked to proceed to the next step.
        /// </summary>
        public static void Sequence(params Action<Action>[] steps)
        {
            RunSequenceStep(steps, 0);
        }

        static void RunSequenceStep(Action<Action>[] steps, int index)
        {
            if (index >= steps.Length) return;
            steps[index](() => RunSequenceStep(steps, index + 1));
        }

        // ── New: Parallel helper ──

        /// <summary>
        /// Run multiple animation handles in parallel and invoke onAllComplete when all finish.
        /// </summary>
        public static void Parallel(Action onAllComplete, params Func<Action, MD3AnimationHandle>[] animations)
        {
            int remaining = animations.Length;
            if (remaining == 0) { onAllComplete?.Invoke(); return; }

            foreach (var anim in animations)
            {
                anim(() =>
                {
                    remaining--;
                    if (remaining <= 0) onAllComplete?.Invoke();
                });
            }
        }

        // ── Easing functions ──

        internal static float ApplyEasing(float t, MD3Easing easing)
        {
            switch (easing)
            {
                case MD3Easing.EaseIn:         return t * t;
                case MD3Easing.EaseOut:        return 1f - (1f - t) * (1f - t);
                case MD3Easing.EaseInOut:       return t * t * (3f - 2f * t);
                case MD3Easing.EaseInCubic:    return t * t * t;
                case MD3Easing.EaseOutCubic:
                {
                    float u = 1f - t;
                    return 1f - u * u * u;
                }
                case MD3Easing.EaseInOutCubic:
                    return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) * 0.5f;
                case MD3Easing.EaseInQuart:    return t * t * t * t;
                case MD3Easing.EaseOutQuart:
                {
                    float u = 1f - t;
                    return 1f - u * u * u * u;
                }
                case MD3Easing.EaseInOutQuart:
                    return t < 0.5f ? 8f * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 4f) * 0.5f;
                case MD3Easing.EaseOutBack:
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    float t1 = t - 1f;
                    return 1f + c3 * t1 * t1 * t1 + c1 * t1 * t1;
                }
                case MD3Easing.EaseInBack:
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    return c3 * t * t * t - c1 * t * t;
                }
                case MD3Easing.EaseInOutBack:
                {
                    const float c1 = 1.70158f;
                    const float c2 = c1 * 1.525f;
                    return t < 0.5f
                        ? (Mathf.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2)) * 0.5f
                        : (Mathf.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) * 0.5f;
                }
                case MD3Easing.EaseOutElastic:
                {
                    if (t <= 0f) return 0f;
                    if (t >= 1f) return 1f;
                    const float c4 = 2f * Mathf.PI / 3f;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
                }
                case MD3Easing.EaseOutBounce:
                    return BounceOut(t);
                default: return t;
            }
        }

        static float BounceOut(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (t < 1f / d1) return n1 * t * t;
            if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
            if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }

        static float EvaluateKeyframes(MD3Keyframe[] keyframes, float t)
        {
            if (keyframes.Length == 0) return t;
            if (t <= keyframes[0].Time) return keyframes[0].Value;
            if (t >= keyframes[keyframes.Length - 1].Time) return keyframes[keyframes.Length - 1].Value;

            for (int i = 1; i < keyframes.Length; i++)
            {
                if (t <= keyframes[i].Time)
                {
                    var prev = keyframes[i - 1];
                    var curr = keyframes[i];
                    float segT = (curr.Time - prev.Time) > 0.0001f
                        ? (t - prev.Time) / (curr.Time - prev.Time)
                        : 1f;
                    float eased = ApplyEasing(segT, curr.Easing);
                    return Mathf.LerpUnclamped(prev.Value, curr.Value, eased);
                }
            }
            return keyframes[keyframes.Length - 1].Value;
        }

        internal static EditorWindow FindHostWindow(VisualElement element)
        {
            if (element?.panel == null) return null;
            var targetPanel = element.panel;
            foreach (var w in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (w.rootVisualElement?.panel == targetPanel)
                    return w;
            }
            return null;
        }
    }

    /// <summary>
    /// Centralized animation loop. Animated components register here instead of
    /// each adding their own EditorApplication.update callback + Repaint().
    /// Result: 1 update callback + 1 Repaint per window per frame, regardless
    /// of how many animated elements exist.
    /// </summary>
    internal static class MD3AnimLoop
    {
        struct Entry
        {
            public VisualElement Element;
            public EditorWindow Window;
        }

        static readonly List<Entry> s_entries = new List<Entry>();
        static readonly List<EditorWindow> s_windowBuf = new List<EditorWindow>();
        static double s_lastTick;

        internal static void Register(VisualElement el)
        {
            // Deduplicate — prevent double-registration
            for (int i = 0; i < s_entries.Count; i++)
                if (s_entries[i].Element == el) return;

            s_entries.Add(new Entry { Element = el, Window = null });
            if (s_entries.Count == 1)
                EditorApplication.update += Tick;
        }

        internal static void Unregister(VisualElement el)
        {
            for (int i = s_entries.Count - 1; i >= 0; i--)
            {
                if (s_entries[i].Element == el)
                {
                    s_entries.RemoveAt(i);
                    break;
                }
            }
            if (s_entries.Count == 0)
                EditorApplication.update -= Tick;
        }

        static void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - s_lastTick < 0.016) return; // ~60fps — smooth ripple/transition animations
            s_lastTick = now;

            // Mark all animated elements dirty
            for (int i = s_entries.Count - 1; i >= 0; i--)
            {
                var e = s_entries[i];
                if (e.Element?.panel == null)
                {
                    s_entries.RemoveAt(i);
                    continue;
                }
                e.Element.MarkDirtyRepaint();
            }

            if (s_entries.Count == 0)
            {
                EditorApplication.update -= Tick;
                return;
            }

            // Resolve host windows lazily — one lookup per tick, not per entry
            bool needsWindowLookup = false;
            for (int i = 0; i < s_entries.Count; i++)
            {
                if (s_entries[i].Window == null)
                {
                    needsWindowLookup = true;
                    break;
                }
            }

            if (needsWindowLookup)
            {
                var allWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();
                for (int i = 0; i < s_entries.Count; i++)
                {
                    var e = s_entries[i];
                    if (e.Window != null) continue;
                    if (e.Element?.panel == null) continue;
                    var panel = e.Element.panel;
                    foreach (var w in allWindows)
                    {
                        if (w.rootVisualElement?.panel == panel)
                        {
                            e.Window = w;
                            s_entries[i] = e;
                            break;
                        }
                    }
                }
            }

            // Repaint unique windows
            s_windowBuf.Clear();
            for (int i = 0; i < s_entries.Count; i++)
            {
                var w = s_entries[i].Window;
                if (w == null) continue;
                if (!s_windowBuf.Contains(w))
                    s_windowBuf.Add(w);
            }
            for (int i = 0; i < s_windowBuf.Count; i++)
                s_windowBuf[i].Repaint();

            EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}
