using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public enum MD3Shape
    {
        Circle,
        Star,
        Sakura,
        Heart,
        Hexagon,
        Diamond,
        Triangle,
        Pentagon,
        Octagon,
        Cross,
        Clover,
        Flower,
        Gear,
        Shield,
        Drop,
        Custom,
    }

    public struct MD3ShapeParams
    {
        public int VertexCount;
        public float InnerRatio;
        public float Roundness;
    }

    public class MD3ShapedAvatar : VisualElement, IMD3Themeable
    {
        public new class UxmlFactory : UxmlFactory<MD3ShapedAvatar, UxmlTraits> { }
        public new class UxmlTraits : VisualElement.UxmlTraits { }

        const int MorphSampleCount = 128;

        MD3Theme _theme;
        double _startTime;

        readonly float _size;
        readonly float _rotationSpeed;
        readonly float _rotationOffset;

        Texture2D _texture;
        Vector2[] _shapeVertices;

        // Morph state
        Vector2[] _morphFrom;
        Vector2[] _morphTo;
        float _morphT = 1f; // 1 = not morphing
        MD3AnimationHandle _morphHandle;

        public MD3ShapedAvatar() : this(MD3Shape.Circle) { }

        public MD3ShapedAvatar(
            MD3Shape shape = MD3Shape.Circle,
            float size = 80f,
            float rotationSpeed = 0f,
            float rotationOffset = 0f,
            MD3ShapeParams? customParams = null)
        {
            _size = size;
            _rotationSpeed = rotationSpeed;
            _rotationOffset = rotationOffset;
            var shapeParams = customParams ?? GetDefaultParams(shape);

            AddToClassList("md3-shaped-avatar");
            style.width = size;
            style.height = size;
            style.flexShrink = 0;

            _shapeVertices = Resample(GenerateShapeVertices(shape, shapeParams), MorphSampleCount);

            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        public void SetTexture(Texture2D tex)
        {
            _texture = tex;
            MarkDirtyRepaint();
        }

        public Texture2D GetTexture() => _texture;

        /// <summary>
        /// Smoothly morph from the current shape to the target shape.
        /// </summary>
        public MD3AnimationHandle MorphTo(MD3Shape target, float durationMs = 500f,
            MD3Easing easing = MD3Easing.EaseInOut, MD3ShapeParams? customParams = null)
        {
            var targetParams = customParams ?? GetDefaultParams(target);
            var targetVerts = Resample(GenerateShapeVertices(target, targetParams), MorphSampleCount);
            return MorphToVertices(targetVerts, durationMs, easing);
        }

        /// <summary>
        /// Smoothly morph from the current shape to arbitrary target vertices.
        /// </summary>
        public MD3AnimationHandle MorphToVertices(Vector2[] targetVerts, float durationMs = 500f,
            MD3Easing easing = MD3Easing.EaseInOut)
        {
            _morphHandle?.Cancel();

            // Current displayed vertices become the morph source
            _morphFrom = GetCurrentVertices();
            _morphTo = Resample(targetVerts, MorphSampleCount);
            _morphT = 0f;

            // Ensure anim loop is running during morph
            MD3AnimLoop.Register(this);

            _morphHandle = MD3Animate.Float(this, 0f, 1f, durationMs, easing, t =>
            {
                _morphT = t;
                // Interpolate vertices
                for (int i = 0; i < MorphSampleCount; i++)
                    _shapeVertices[i] = Vector2.LerpUnclamped(_morphFrom[i], _morphTo[i], t);
                MarkDirtyRepaint();
            }, () =>
            {
                _morphT = 1f;
                _morphFrom = null;
                _morphTo = null;
                _morphHandle = null;
                // Unregister only if no continuous rotation
                if (_rotationSpeed == 0f)
                    MD3AnimLoop.Unregister(this);
            });

            return _morphHandle;
        }

        Vector2[] GetCurrentVertices()
        {
            var copy = new Vector2[_shapeVertices.Length];
            Array.Copy(_shapeVertices, copy, _shapeVertices.Length);
            return copy;
        }

        public void RefreshTheme()
        {
            _theme = MD3Theme.Resolve(this);
            MarkDirtyRepaint();
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            RefreshTheme();
            _startTime = EditorApplication.timeSinceStartup;
            if (_rotationSpeed != 0f)
                MD3AnimLoop.Register(this);
        }

        void OnDetach(DetachFromPanelEvent evt)
        {
            if (_rotationSpeed != 0f)
                MD3AnimLoop.Unregister(this);
        }

        // ── Drawing ──────────────────────────────────────────

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (_theme == null) _theme = MD3Theme.Resolve(this);
            if (_theme == null) return;

            float cx = _size * 0.5f;
            float cy = _size * 0.5f;
            float radius = _size * 0.5f;

            float angleDeg = _rotationOffset;
            if (_rotationSpeed != 0f)
            {
                double elapsed = EditorApplication.timeSinceStartup - _startTime;
                angleDeg = (float)(_rotationOffset + elapsed * _rotationSpeed % 360.0);
            }
            float angleRad = angleDeg * Mathf.Deg2Rad;
            float cosA = Mathf.Cos(angleRad);
            float sinA = Mathf.Sin(angleRad);

            int vertCount = _shapeVertices.Length;

            if (_texture != null)
                DrawTextured(mgc, cx, cy, radius, cosA, sinA, vertCount);
            else
                DrawSolid(mgc, cx, cy, radius, cosA, sinA, vertCount);
        }

        void DrawTextured(MeshGenerationContext mgc, float cx, float cy, float radius,
            float cosA, float sinA, int vertCount)
        {
            int totalVerts = vertCount + 1;
            int totalIndices = vertCount * 3;

            var md = mgc.Allocate(totalVerts, totalIndices, _texture);

            // Center vertex
            md.SetNextVertex(new Vertex
            {
                position = new Vector3(cx, cy, Vertex.nearZ),
                tint = Color.white,
                uv = new Vector2(0.5f, 0.5f)
            });

            // Outline vertices
            for (int i = 0; i < vertCount; i++)
            {
                var sv = _shapeVertices[i];

                // Rotate position
                float rx = sv.x * cosA - sv.y * sinA;
                float ry = sv.x * sinA + sv.y * cosA;
                float px = cx + rx * radius;
                float py = cy + ry * radius;

                // UV from rotated coords (image stays horizontal while shape rotates)
                float u = rx * 0.5f + 0.5f;
                float v = 1f - (ry * 0.5f + 0.5f); // Y-flip for UI Toolkit

                md.SetNextVertex(new Vertex
                {
                    position = new Vector3(px, py, Vertex.nearZ),
                    tint = Color.white,
                    uv = new Vector2(u, v)
                });
            }

            // Fan triangles
            for (int i = 0; i < vertCount; i++)
            {
                md.SetNextIndex(0);
                md.SetNextIndex((ushort)(1 + i));
                md.SetNextIndex((ushort)(1 + (i + 1) % vertCount));
            }
        }

        void DrawSolid(MeshGenerationContext mgc, float cx, float cy, float radius,
            float cosA, float sinA, int vertCount)
        {
            var painter = mgc.painter2D;
            painter.fillColor = _theme.PrimaryContainer;
            painter.BeginPath();

            for (int i = 0; i < vertCount; i++)
            {
                var sv = _shapeVertices[i];
                float rx = sv.x * cosA - sv.y * sinA;
                float ry = sv.x * sinA + sv.y * cosA;
                float px = cx + rx * radius;
                float py = cy + ry * radius;

                if (i == 0) painter.MoveTo(new Vector2(px, py));
                else painter.LineTo(new Vector2(px, py));
            }

            painter.ClosePath();
            painter.Fill();
        }

        // ── Shape generation ─────────────────────────────────

        static MD3ShapeParams GetDefaultParams(MD3Shape shape)
        {
            switch (shape)
            {
                case MD3Shape.Circle:   return new MD3ShapeParams { VertexCount = 64, InnerRatio = 1f,    Roundness = 0f };
                case MD3Shape.Star:     return new MD3ShapeParams { VertexCount = 5,  InnerRatio = 0.38f, Roundness = 0.3f };
                case MD3Shape.Sakura:   return new MD3ShapeParams { VertexCount = 5,  InnerRatio = 0.35f, Roundness = 0f };
                case MD3Shape.Heart:    return new MD3ShapeParams { VertexCount = 80, InnerRatio = 1f,    Roundness = 0f };
                case MD3Shape.Hexagon:  return new MD3ShapeParams { VertexCount = 6,  InnerRatio = 1f,    Roundness = 0.3f };
                case MD3Shape.Diamond:  return new MD3ShapeParams { VertexCount = 4,  InnerRatio = 1f,    Roundness = 0.2f };
                case MD3Shape.Triangle: return new MD3ShapeParams { VertexCount = 3,  InnerRatio = 1f,    Roundness = 0.3f };
                case MD3Shape.Pentagon: return new MD3ShapeParams { VertexCount = 5,  InnerRatio = 1f,    Roundness = 0.3f };
                case MD3Shape.Octagon:  return new MD3ShapeParams { VertexCount = 8,  InnerRatio = 1f,    Roundness = 0.15f };
                case MD3Shape.Cross:    return new MD3ShapeParams { VertexCount = 4,  InnerRatio = 0.38f, Roundness = 0.4f };
                case MD3Shape.Clover:   return new MD3ShapeParams { VertexCount = 4,  InnerRatio = 0.35f, Roundness = 0f };
                case MD3Shape.Flower:   return new MD3ShapeParams { VertexCount = 6,  InnerRatio = 0.35f, Roundness = 0f };
                case MD3Shape.Gear:     return new MD3ShapeParams { VertexCount = 8,  InnerRatio = 0.82f, Roundness = 0.15f };
                case MD3Shape.Shield:   return new MD3ShapeParams { VertexCount = 64, InnerRatio = 1f,    Roundness = 0f };
                case MD3Shape.Drop:     return new MD3ShapeParams { VertexCount = 64, InnerRatio = 1f,    Roundness = 0f };
                default:                return new MD3ShapeParams { VertexCount = 6,  InnerRatio = 1f,    Roundness = 0f };
            }
        }

        static Vector2[] GenerateShapeVertices(MD3Shape shape, MD3ShapeParams p)
        {
            Vector2[] verts;
            switch (shape)
            {
                case MD3Shape.Circle:   verts = GenerateCircle(p.VertexCount); break;
                case MD3Shape.Star:     verts = GenerateStar(p.VertexCount, p.InnerRatio); break;
                case MD3Shape.Sakura:   verts = GenerateSakura(p.VertexCount, p.InnerRatio); break;
                case MD3Shape.Heart:    verts = GenerateHeart(p.VertexCount); break;
                case MD3Shape.Hexagon:  verts = GeneratePolygon(p.VertexCount, -Mathf.PI / 6f); break;
                case MD3Shape.Diamond:  verts = GeneratePolygon(p.VertexCount, 0f); break;
                case MD3Shape.Triangle: verts = GeneratePolygon(p.VertexCount, -Mathf.PI / 2f); break;
                case MD3Shape.Pentagon: verts = GeneratePolygon(p.VertexCount, -Mathf.PI / 2f); break;
                case MD3Shape.Octagon:  verts = GeneratePolygon(p.VertexCount, -Mathf.PI / 8f); break;
                case MD3Shape.Cross:    verts = GenerateCross(p.VertexCount, p.InnerRatio); break;
                case MD3Shape.Clover:   verts = GenerateClover(p.VertexCount); break;
                case MD3Shape.Flower:   verts = GenerateFlower(p.VertexCount); break;
                case MD3Shape.Gear:     verts = GenerateGear(p.VertexCount, p.InnerRatio); break;
                case MD3Shape.Shield:   verts = GenerateShield(p.VertexCount); break;
                case MD3Shape.Drop:     verts = GenerateDrop(p.VertexCount); break;
                case MD3Shape.Custom:   verts = GenerateStarOrPolygon(p.VertexCount, p.InnerRatio); break;
                default:                verts = GenerateCircle(64); break;
            }

            if (p.Roundness > 0f)
                verts = ApplyRoundness(verts, p.Roundness);

            return verts;
        }

        /// <summary>
        /// Chaikin corner-cutting subdivision. Each iteration replaces sharp corners
        /// with smoother curves. Roundness 0..1 controls the cut ratio, and iterations
        /// scale with roundness for progressive smoothing.
        /// </summary>
        static Vector2[] ApplyRoundness(Vector2[] verts, float roundness)
        {
            int iterations = Mathf.Clamp(Mathf.CeilToInt(roundness * 3f), 1, 4);
            float cut = Mathf.Lerp(0.1f, 0.25f, roundness);

            var current = verts;
            for (int iter = 0; iter < iterations; iter++)
            {
                int n = current.Length;
                var next = new Vector2[n * 2];
                for (int i = 0; i < n; i++)
                {
                    var a = current[i];
                    var b = current[(i + 1) % n];
                    next[i * 2] = Vector2.Lerp(a, b, cut);
                    next[i * 2 + 1] = Vector2.Lerp(a, b, 1f - cut);
                }
                current = next;
            }

            return current;
        }

        static Vector2[] GenerateCircle(int segments)
        {
            var verts = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float a = i * Mathf.PI * 2f / segments - Mathf.PI * 0.5f;
                verts[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            }
            return verts;
        }

        static Vector2[] GeneratePolygon(int sides, float startAngle = 0f)
        {
            var verts = new Vector2[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides + startAngle;
                verts[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            }
            return verts;
        }

        static Vector2[] GenerateStar(int points, float innerRatio)
        {
            int total = points * 2;
            var verts = new Vector2[total];
            for (int i = 0; i < total; i++)
            {
                float a = i * Mathf.PI / points - Mathf.PI * 0.5f;
                float r = (i % 2 == 0) ? 1f : innerRatio;
                verts[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            }
            return verts;
        }

        static Vector2[] GenerateStarOrPolygon(int points, float innerRatio)
        {
            if (Mathf.Approximately(innerRatio, 1f))
                return GeneratePolygon(points, -Mathf.PI / 2f);
            return GenerateStar(points, innerRatio);
        }

        static Vector2[] GenerateSakura(int petals, float innerRatio)
        {
            // Polar curve approach: r(theta) with smooth cosine modulation
            // Produces round, plump petals with a gentle notch at each tip
            int totalPoints = petals * 24; // 24 points per petal for smoothness
            var verts = new Vector2[totalPoints];
            float baseR = 0.55f; // minimum radius between petals
            float petalR = 1f;   // maximum radius at petal peak

            for (int i = 0; i < totalPoints; i++)
            {
                float theta = i * Mathf.PI * 2f / totalPoints - Mathf.PI * 0.5f;

                // Petal envelope: raised cosine with petals peaks
                // cos(petals * theta) ranges -1..1, peaks at petal centers
                float petalCos = Mathf.Cos(petals * theta);

                // Smooth blend: use power to make petals rounder (wider peak)
                float envelope = (petalCos + 1f) * 0.5f; // 0..1
                envelope = Mathf.Pow(envelope, 0.7f);     // flatten peak for wider petals

                float r = Mathf.Lerp(baseR, petalR, envelope);

                // Gentle notch at petal tip center
                // cos(petals * theta) == 1 at petal center → notch dip
                float notchCos = Mathf.Cos(petals * theta);
                if (notchCos > 0.85f)
                {
                    float notchT = (notchCos - 0.85f) / 0.15f; // 0..1 near tip
                    r -= notchT * notchT * 0.1f; // shallow quadratic dip
                }

                verts[i] = new Vector2(Mathf.Cos(theta) * r, Mathf.Sin(theta) * r);
            }

            return verts;
        }

        static Vector2[] GenerateHeart(int segments)
        {
            // Classic parametric heart curve
            // x(t) = 16 sin³(t)
            // y(t) = 13cos(t) - 5cos(2t) - 2cos(3t) - cos(4t)
            // Produces the iconic heart shape with round lobes and a pointed bottom.
            int n = Mathf.Max(segments, 80);
            var raw = new Vector2[n];

            for (int i = 0; i < n; i++)
            {
                float t = i * Mathf.PI * 2f / n;
                float sinT = Mathf.Sin(t);
                float cosT = Mathf.Cos(t);

                float x = 16f * sinT * sinT * sinT;
                float y = 13f * cosT - 5f * Mathf.Cos(2f * t)
                        - 2f * Mathf.Cos(3f * t) - Mathf.Cos(4f * t);
                raw[i] = new Vector2(x, y);
            }

            // Find bounds
            float minX = raw[0].x, maxX = raw[0].x;
            float minY = raw[0].y, maxY = raw[0].y;
            for (int i = 1; i < n; i++)
            {
                if (raw[i].x < minX) minX = raw[i].x;
                if (raw[i].x > maxX) maxX = raw[i].x;
                if (raw[i].y < minY) minY = raw[i].y;
                if (raw[i].y > maxY) maxY = raw[i].y;
            }

            // Normalize to [-0.95, 0.95] with slight margin, flip Y for screen coords
            float rangeX = maxX - minX;
            float rangeY = maxY - minY;
            float scale = 1.9f / Mathf.Max(rangeX, rangeY);
            float cx = (minX + maxX) * 0.5f;
            float cy = (minY + maxY) * 0.5f;

            var verts = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                verts[i] = new Vector2(
                    (raw[i].x - cx) * scale,
                    -(raw[i].y - cy) * scale // flip Y: math-up → screen-down
                );
            }

            return verts;
        }

        static Vector2[] GenerateCross(int arms, float innerRatio)
        {
            // Cross shape: 12 vertices forming a plus/cross
            float outer = 1f;
            float inner = innerRatio; // arm width ratio
            var verts = new Vector2[12];
            // Top arm
            verts[0]  = new Vector2(-inner, -outer);
            verts[1]  = new Vector2( inner, -outer);
            // Right arm
            verts[2]  = new Vector2( inner, -inner);
            verts[3]  = new Vector2( outer, -inner);
            verts[4]  = new Vector2( outer,  inner);
            verts[5]  = new Vector2( inner,  inner);
            // Bottom arm
            verts[6]  = new Vector2( inner,  outer);
            verts[7]  = new Vector2(-inner,  outer);
            // Left arm
            verts[8]  = new Vector2(-inner,  inner);
            verts[9]  = new Vector2(-outer,  inner);
            verts[10] = new Vector2(-outer, -inner);
            verts[11] = new Vector2(-inner, -inner);
            return verts;
        }

        static Vector2[] GenerateClover(int lobes)
        {
            // Polar rose curve: r = cos(lobes * theta) — produces round lobes
            int n = lobes * 24;
            var verts = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float theta = i * Mathf.PI * 2f / n - Mathf.PI * 0.5f;
                float r = 0.35f + 0.65f * Mathf.Abs(Mathf.Cos(lobes * theta));
                verts[i] = new Vector2(Mathf.Cos(theta) * r, Mathf.Sin(theta) * r);
            }
            return verts;
        }

        static Vector2[] GenerateFlower(int petals)
        {
            // Smooth flower: raised cosine with deeper valleys than Sakura
            int n = petals * 24;
            var verts = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float theta = i * Mathf.PI * 2f / n - Mathf.PI * 0.5f;
                float envelope = (Mathf.Cos(petals * theta) + 1f) * 0.5f;
                envelope = Mathf.Pow(envelope, 0.55f);
                float r = Mathf.Lerp(0.4f, 1f, envelope);
                verts[i] = new Vector2(Mathf.Cos(theta) * r, Mathf.Sin(theta) * r);
            }
            return verts;
        }

        static Vector2[] GenerateGear(int teeth, float innerRatio)
        {
            // Gear: alternating flat outer/inner segments with steep transitions
            int pointsPerTooth = 8;
            int n = teeth * pointsPerTooth;
            var verts = new Vector2[n];
            float toothAngle = Mathf.PI * 2f / teeth;

            for (int t = 0; t < teeth; t++)
            {
                float baseAngle = t * toothAngle - Mathf.PI * 0.5f;
                for (int j = 0; j < pointsPerTooth; j++)
                {
                    float lt = (float)j / pointsPerTooth;
                    float angle = baseAngle + lt * toothAngle;
                    float r;

                    if (lt < 0.15f)       r = innerRatio;                               // inner flat
                    else if (lt < 0.25f)  r = Mathf.Lerp(innerRatio, 1f, (lt - 0.15f) / 0.1f); // rise
                    else if (lt < 0.5f)   r = 1f;                                       // outer flat
                    else if (lt < 0.6f)   r = Mathf.Lerp(1f, innerRatio, (lt - 0.5f) / 0.1f);  // fall
                    else                  r = innerRatio;                               // inner flat

                    verts[t * pointsPerTooth + j] = new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
                }
            }
            return verts;
        }

        static Vector2[] GenerateShield(int segments)
        {
            // Shield: flat top, curved sides tapering to a point at bottom
            int n = Mathf.Max(segments, 64);
            var verts = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float x, y;

                if (t < 0.15f)
                {
                    // Top edge (flat, left to right)
                    float lt = t / 0.15f;
                    x = Mathf.Lerp(-0.85f, 0.85f, lt);
                    y = -1f;
                }
                else if (t < 0.55f)
                {
                    // Right side curve (top-right down to bottom point)
                    float lt = (t - 0.15f) / 0.4f;
                    float angle = Mathf.Lerp(0f, Mathf.PI * 0.5f, lt);
                    x = 0.85f * Mathf.Cos(angle);
                    y = -1f + (1f + 1f) * lt; // -1 to +1
                    x *= (1f - lt * lt * 0.15f); // slight taper
                }
                else if (t < 0.6f)
                {
                    // Bottom point
                    float lt = (t - 0.55f) / 0.05f;
                    x = Mathf.Lerp(0.15f, 0f, lt);
                    y = Mathf.Lerp(0.85f, 1f, lt);
                }
                else if (t < 0.65f)
                {
                    // Bottom point (left side)
                    float lt = (t - 0.6f) / 0.05f;
                    x = Mathf.Lerp(0f, -0.15f, lt);
                    y = Mathf.Lerp(1f, 0.85f, lt);
                }
                else
                {
                    // Left side curve (bottom point up to top-left)
                    float lt = (t - 0.65f) / 0.35f;
                    float angle = Mathf.Lerp(Mathf.PI * 0.5f, 0f, lt);
                    x = -0.85f * Mathf.Cos(angle);
                    y = -1f + (1f + 1f) * (1f - lt);
                    x *= (1f - (1f - lt) * (1f - lt) * 0.15f);
                }

                verts[i] = new Vector2(x, y);
            }
            return verts;
        }

        static Vector2[] GenerateDrop(int segments)
        {
            // Water drop: circle bottom + pointed top
            int n = Mathf.Max(segments, 64);
            var verts = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float theta = t * Mathf.PI * 2f;

                float sinT = Mathf.Sin(theta);
                float cosT = Mathf.Cos(theta);

                // Base circle
                float r = 0.7f;

                // Compress top half into a point
                if (cosT < 0f) // top half (screen coords: y negative = top)
                {
                    float sharpness = Mathf.Pow(Mathf.Abs(cosT), 0.6f);
                    r = Mathf.Lerp(0.7f, 0f, sharpness);
                }

                float x = sinT * r;
                float y = -cosT * r;

                // Shift down so the point is at top
                y += 0.15f;

                verts[i] = new Vector2(x, y);
            }

            // Normalize
            return NormalizeVerts(verts);
        }

        static Vector2[] NormalizeVerts(Vector2[] raw)
        {
            float minX = raw[0].x, maxX = raw[0].x;
            float minY = raw[0].y, maxY = raw[0].y;
            for (int i = 1; i < raw.Length; i++)
            {
                if (raw[i].x < minX) minX = raw[i].x;
                if (raw[i].x > maxX) maxX = raw[i].x;
                if (raw[i].y < minY) minY = raw[i].y;
                if (raw[i].y > maxY) maxY = raw[i].y;
            }
            float range = Mathf.Max(maxX - minX, maxY - minY);
            if (range < 0.001f) return raw;
            float scale = 1.9f / range;
            float cx = (minX + maxX) * 0.5f;
            float cy = (minY + maxY) * 0.5f;

            var verts = new Vector2[raw.Length];
            for (int i = 0; i < raw.Length; i++)
                verts[i] = new Vector2((raw[i].x - cx) * scale, (raw[i].y - cy) * scale);
            return verts;
        }

        // ── Resampling ───────────────────────────────────────

        /// <summary>
        /// Resample a closed polygon to exactly <paramref name="count"/> evenly-spaced
        /// vertices by arc length. This ensures all shapes have the same vertex count
        /// for smooth morphing interpolation.
        /// </summary>
        static Vector2[] Resample(Vector2[] src, int count)
        {
            if (src.Length == count) return src;

            int n = src.Length;

            // Build cumulative arc-length table
            var cumLen = new float[n + 1];
            cumLen[0] = 0f;
            for (int i = 1; i <= n; i++)
                cumLen[i] = cumLen[i - 1] + Vector2.Distance(src[(i - 1) % n], src[i % n]);

            float totalLen = cumLen[n];
            if (totalLen < 0.0001f)
            {
                // Degenerate shape — return copies of first vertex
                var fallback = new Vector2[count];
                for (int i = 0; i < count; i++) fallback[i] = src[0];
                return fallback;
            }

            var result = new Vector2[count];
            int seg = 0; // current segment index

            for (int i = 0; i < count; i++)
            {
                float targetLen = totalLen * i / count;

                // Advance segment until we find the one containing targetLen
                while (seg < n - 1 && cumLen[seg + 1] < targetLen)
                    seg++;

                float segStart = cumLen[seg];
                float segEnd = cumLen[seg + 1];
                float segLen = segEnd - segStart;

                float t = (segLen > 0.0001f) ? (targetLen - segStart) / segLen : 0f;
                result[i] = Vector2.Lerp(src[seg % n], src[(seg + 1) % n], t);
            }

            return result;
        }
    }
}
