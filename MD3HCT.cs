using UnityEngine;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// HCT (Hue-Chroma-Tone) color space for Material Design 3.
    /// Uses CIELAB L* for Tone and HSL-based Hue/Chroma with perceptual corrections.
    /// Provides the foundation for seed-color-based palette generation.
    /// </summary>
    internal static class MD3HCT
    {
        /// <summary>Convert sRGB Color to HCT (Hue 0-360, Chroma 0-~130, Tone 0-100).</summary>
        public static void FromColor(Color rgb, out float hue, out float chroma, out float tone)
        {
            tone = RgbToTone(rgb);
            RgbToHueChroma(rgb, out hue, out chroma);
        }

        /// <summary>Convert HCT to sRGB Color. Clamps chroma if out of sRGB gamut.</summary>
        public static Color ToColor(float hue, float chroma, float tone)
        {
            if (tone <= 0f) return Color.black;
            if (tone >= 100f) return Color.white;
            if (chroma < 0.5f) return ToneToGray(tone);

            // Binary search: find max chroma that fits in sRGB gamut at this hue+tone
            float lo = 0f, hi = chroma;
            Color result = ToneToGray(tone);

            for (int i = 0; i < 16; i++)
            {
                float mid = (lo + hi) * 0.5f;
                var candidate = HctToRgbUnclamped(hue, mid, tone);
                if (IsInGamut(candidate))
                {
                    result = candidate;
                    lo = mid;
                }
                else
                {
                    hi = mid;
                }
            }

            return result;
        }

        /// <summary>Get a color at a specific tone, preserving hue and chroma.</summary>
        public static Color AtTone(Color rgb, float targetTone)
        {
            float h, c, t;
            FromColor(rgb, out h, out c, out t);
            return ToColor(h, c, targetTone);
        }

        /// <summary>Get a color at a specific tone from HCT components.</summary>
        public static Color AtTone(float hue, float chroma, float targetTone)
        {
            return ToColor(hue, chroma, targetTone);
        }

        // ── Tone (CIELAB L*) ─────────────────────────────────

        /// <summary>Convert sRGB to CIELAB L* (0-100), perceptually uniform lightness.</summary>
        static float RgbToTone(Color rgb)
        {
            float y = 0.2126f * Linearize(rgb.r) + 0.7152f * Linearize(rgb.g) + 0.0722f * Linearize(rgb.b);
            return YToLstar(y);
        }

        static float YToLstar(float y)
        {
            if (y <= 0.008856f)
                return 903.3f * y;
            return 116f * Mathf.Pow(y, 1f / 3f) - 16f;
        }

        static float LstarToY(float lstar)
        {
            if (lstar <= 8f)
                return lstar / 903.3f;
            float t = (lstar + 16f) / 116f;
            return t * t * t;
        }

        static Color ToneToGray(float tone)
        {
            float y = LstarToY(tone);
            float srgb = Delinearize(y);
            return new Color(srgb, srgb, srgb);
        }

        // ── Hue & Chroma (HSL-based) ─────────────────────────

        static void RgbToHueChroma(Color rgb, out float hue, out float chroma)
        {
            float r = rgb.r, g = rgb.g, b = rgb.b;
            float max = Mathf.Max(r, Mathf.Max(g, b));
            float min = Mathf.Min(r, Mathf.Min(g, b));
            float delta = max - min;

            // Hue
            if (delta < 0.0001f)
            {
                hue = 0f;
            }
            else if (Mathf.Approximately(max, r))
            {
                hue = 60f * (((g - b) / delta) % 6f);
            }
            else if (Mathf.Approximately(max, g))
            {
                hue = 60f * (((b - r) / delta) + 2f);
            }
            else
            {
                hue = 60f * (((r - g) / delta) + 4f);
            }
            if (hue < 0f) hue += 360f;

            // Chroma: scale HSL saturation to perceptual chroma
            // Max chroma in sRGB is roughly 48-130 depending on hue
            float l = (max + min) * 0.5f;
            float sat = (delta < 0.0001f) ? 0f :
                delta / (1f - Mathf.Abs(2f * l - 1f));
            sat = Mathf.Clamp01(sat);

            // Scale to approximate M3 chroma range
            chroma = sat * HueMaxChroma(hue) * 1.2f;
        }

        /// <summary>Approximate max chroma for a given hue in sRGB gamut.</summary>
        static float HueMaxChroma(float hue)
        {
            // Rough approximation: most hues max at ~48-50 chroma in M3 terms
            // Blue-purple range has higher chroma potential
            float h = hue % 360f;
            if (h >= 240f && h <= 300f) return 70f; // blue-purple
            if (h >= 0f && h <= 40f) return 65f;    // red-orange
            if (h >= 100f && h <= 160f) return 55f;  // green
            return 50f;
        }

        // ── HCT → RGB (unclamped) ────────────────────────────

        static Color HctToRgbUnclamped(float hue, float chroma, float tone)
        {
            // Target luminance from tone
            float targetY = LstarToY(tone);

            // Convert hue + chroma to HSL saturation
            float maxC = HueMaxChroma(hue);
            float sat = Mathf.Clamp01(chroma / (maxC * 1.2f));

            // HSL lightness from target luminance (approximate)
            // L* is perceptually uniform, HSL L is not. We iterate to find the right HSL L.
            float hslL = FindHslLightness(hue, sat, targetY);

            return HslToRgb(hue, sat, hslL);
        }

        /// <summary>Find HSL lightness that produces the target luminance Y.</summary>
        static float FindHslLightness(float hue, float sat, float targetY)
        {
            float lo = 0f, hi = 1f;
            for (int i = 0; i < 20; i++)
            {
                float mid = (lo + hi) * 0.5f;
                var rgb = HslToRgb(hue, sat, mid);
                float y = 0.2126f * Linearize(rgb.r) + 0.7152f * Linearize(rgb.g) + 0.0722f * Linearize(rgb.b);
                if (y < targetY)
                    lo = mid;
                else
                    hi = mid;
            }
            return (lo + hi) * 0.5f;
        }

        // ── sRGB linearization ───────────────────────────────

        static float Linearize(float srgb)
        {
            if (srgb <= 0.04045f)
                return srgb / 12.92f;
            return Mathf.Pow((srgb + 0.055f) / 1.055f, 2.4f);
        }

        static float Delinearize(float linear)
        {
            if (linear <= 0.0031308f)
                return Mathf.Clamp01(linear * 12.92f);
            return Mathf.Clamp01(1.055f * Mathf.Pow(linear, 1f / 2.4f) - 0.055f);
        }

        // ── HSL ↔ RGB ────────────────────────────────────────

        static Color HslToRgb(float h, float s, float l)
        {
            if (s < 0.001f)
                return new Color(l, l, l);

            float c = (1f - Mathf.Abs(2f * l - 1f)) * s;
            float hPrime = (h % 360f) / 60f;
            float x = c * (1f - Mathf.Abs(hPrime % 2f - 1f));
            float m = l - c * 0.5f;

            float r, g, b;
            if (hPrime < 1f)      { r = c; g = x; b = 0; }
            else if (hPrime < 2f) { r = x; g = c; b = 0; }
            else if (hPrime < 3f) { r = 0; g = c; b = x; }
            else if (hPrime < 4f) { r = 0; g = x; b = c; }
            else if (hPrime < 5f) { r = x; g = 0; b = c; }
            else                  { r = c; g = 0; b = x; }

            return new Color(
                Mathf.Clamp01(r + m),
                Mathf.Clamp01(g + m),
                Mathf.Clamp01(b + m)
            );
        }

        static bool IsInGamut(Color c)
        {
            return c.r >= -0.001f && c.r <= 1.001f &&
                   c.g >= -0.001f && c.g <= 1.001f &&
                   c.b >= -0.001f && c.b <= 1.001f;
        }
    }
}
