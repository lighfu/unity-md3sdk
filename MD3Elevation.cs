using UnityEngine;
using UnityEngine.UIElements;

namespace AjisaiFlow.MD3SDK.Editor
{
    public static class MD3Elevation
    {
        // ブラーシミュレーション: 1つの影を N 層に分割して段階的に拡大+透過
        const int BlurLayers = 6;

        /// <summary>
        /// Adds multi-layer blurred shadow as children of <paramref name="target"/> at index 0.
        /// The layers extend beyond the parent via negative insets — requires parent NOT to have overflow:hidden.
        /// Call <see cref="UpdateShadowColor"/> in ApplyColors to adjust alpha for dark/light theme.
        /// </summary>
        public static (VisualElement ambient, VisualElement key) AddInnerShadow(
            VisualElement target, float radius, int level = 2)
        {
            GetParams(level, out var ambSpread, out var ambOffY, out _,
                              out var keySpread, out var keyOffY, out _);

            var ambient = CreateBlurGroup(radius, ambSpread, ambOffY, true);
            var key = CreateBlurGroup(radius, keySpread, keyOffY, true);

            target.Insert(0, ambient);
            target.Insert(1, key);

            return (ambient, key);
        }

        /// <summary>
        /// Adds multi-layer blurred shadow as siblings BEFORE <paramref name="target"/> inside <paramref name="parent"/>.
        /// Use when the target has overflow:hidden.
        /// Returns both layers so they can be removed together with the target.
        /// </summary>
        public static (VisualElement ambient, VisualElement key) AddSiblingShadow(
            VisualElement parent, VisualElement target, float radius, int level = 2)
        {
            GetParams(level, out var ambSpread, out var ambOffY, out _,
                              out var keySpread, out var keyOffY, out _);

            var ambient = CreateBlurGroup(radius, ambSpread, ambOffY, false);
            var key = CreateBlurGroup(radius, keySpread, keyOffY, false);

            int idx = parent.IndexOf(target);
            if (idx < 0) idx = parent.childCount;
            parent.Insert(idx, key);
            parent.Insert(idx, ambient);

            // Sync shadow position with target geometry
            Rect lastRect = Rect.zero;
            target.RegisterCallback<GeometryChangedEvent>(e =>
            {
                var r = target.layout;
                if (r == lastRect) return;
                lastRect = r;
                SyncSiblingGroup(ambient, target, ambSpread, ambOffY);
                SyncSiblingGroup(key, target, keySpread, keyOffY);
            });

            return (ambient, key);
        }

        /// <summary>
        /// Updates shadow layer colors. Call from ApplyColors to adapt to theme.
        /// </summary>
        public static void UpdateShadowColor(
            VisualElement ambient, VisualElement key, bool isDark, int level = 2)
        {
            GetParams(level, out _, out _, out var ambAlpha,
                              out _, out _, out var keyAlpha);

            float mul = isDark ? 0.6f : 1f;
            ApplyBlurGroupColor(ambient, ambAlpha * mul);
            ApplyBlurGroupColor(key, keyAlpha * mul);
        }

        static void GetParams(int level,
            out float ambSpread, out float ambOffY, out float ambAlpha,
            out float keySpread, out float keyOffY, out float keyAlpha)
        {
            switch (level)
            {
                case 1:
                    ambSpread = 2f; ambOffY = 1f; ambAlpha = 0.12f;
                    keySpread = 4f; keyOffY = 2f; keyAlpha = 0.08f;
                    break;
                case 3:
                    ambSpread = 4f; ambOffY = 2f; ambAlpha = 0.14f;
                    keySpread = 16f; keyOffY = 6f; keyAlpha = 0.10f;
                    break;
                default: // level 2
                    ambSpread = 3f; ambOffY = 1f; ambAlpha = 0.12f;
                    keySpread = 8f; keyOffY = 4f; keyAlpha = 0.08f;
                    break;
            }
        }

        /// <summary>
        /// N 層のブラーレイヤーをまとめるコンテナを作成。
        /// 内側のレイヤーは小さく濃く、外側は大きく薄い → ガウシアンブラー風。
        /// </summary>
        static VisualElement CreateBlurGroup(float radius, float spread, float offsetY, bool useInsets)
        {
            var group = new VisualElement();
            group.name = "md3-shadow";
            group.pickingMode = PickingMode.Ignore;
            group.style.position = Position.Absolute;

            for (int i = 0; i < BlurLayers; i++)
            {
                float t = (float)i / (BlurLayers - 1); // 0 (innermost) → 1 (outermost)
                float layerSpread = spread * (0.3f + 0.7f * t); // 内側 30%〜外側 100%
                float layerRadius = radius + layerSpread * 0.5f;

                var layer = new VisualElement();
                layer.pickingMode = PickingMode.Ignore;
                layer.style.position = Position.Absolute;
                layer.style.borderTopLeftRadius = layerRadius;
                layer.style.borderTopRightRadius = layerRadius;
                layer.style.borderBottomLeftRadius = layerRadius;
                layer.style.borderBottomRightRadius = layerRadius;

                if (useInsets)
                {
                    layer.style.top = -layerSpread + offsetY;
                    layer.style.left = -layerSpread;
                    layer.style.right = -layerSpread;
                    layer.style.bottom = -layerSpread - offsetY;
                }

                group.Add(layer);
            }

            return group;
        }

        /// <summary>
        /// ブラーグループ内の各レイヤーに色を適用。
        /// 内側ほど濃く、外側ほど薄くして自然なブラーを実現。
        /// </summary>
        static void ApplyBlurGroupColor(VisualElement group, float totalAlpha)
        {
            if (group == null) return;
            int count = group.childCount;
            if (count == 0)
            {
                // 旧形式 (単一レイヤー) との後方互換
                group.style.backgroundColor = new Color(0, 0, 0, totalAlpha);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                // ガウシアン風の重み: 内側が濃く、外側がなだらかに薄い
                float weight = Mathf.Exp(-2.5f * t * t);
                // 各レイヤーの alpha を正規化 (全体の合計が totalAlpha 程度になるよう)
                float layerAlpha = totalAlpha * weight / (count * 0.4f);
                group[i].style.backgroundColor = new Color(0, 0, 0, layerAlpha);
            }
        }

        /// <summary>
        /// Sibling モードでブラーグループの位置を target に同期。
        /// </summary>
        static void SyncSiblingGroup(VisualElement group, VisualElement target, float spread, float offsetY)
        {
            var r = target.layout;
            if (float.IsNaN(r.x)) return;

            int count = group.childCount;
            if (count == 0)
            {
                // 旧形式
                group.style.left = r.x - spread;
                group.style.top = r.y + offsetY - spread;
                group.style.width = r.width + spread * 2;
                group.style.height = r.height + spread * 2;
                return;
            }

            // グループ自体を target と同じ位置・サイズに配置
            group.style.left = r.x;
            group.style.top = r.y;
            group.style.width = r.width;
            group.style.height = r.height;

            // 各レイヤーを段階的に拡大
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / (count - 1);
                float layerSpread = spread * (0.3f + 0.7f * t);
                var layer = group[i];
                layer.style.left = -layerSpread;
                layer.style.top = offsetY - layerSpread;
                layer.style.width = r.width + layerSpread * 2;
                layer.style.height = r.height + layerSpread * 2;
            }
        }
    }
}
