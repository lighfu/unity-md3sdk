using UnityEngine;

namespace AjisaiFlow.MD3SDK.Editor
{
    /// <summary>
    /// Generates Material Design 3 tonal palettes and color schemes from a seed color.
    /// </summary>
    internal static class MD3Palette
    {
        public struct TonalPalette
        {
            public float Hue;
            public float Chroma;

            public Color Tone(float t) => MD3HCT.ToColor(Hue, Chroma, t);

            public Color T0   => Tone(0);
            public Color T4   => Tone(4);
            public Color T6   => Tone(6);
            public Color T10  => Tone(10);
            public Color T12  => Tone(12);
            public Color T17  => Tone(17);
            public Color T20  => Tone(20);
            public Color T22  => Tone(22);
            public Color T30  => Tone(30);
            public Color T40  => Tone(40);
            public Color T50  => Tone(50);
            public Color T60  => Tone(60);
            public Color T70  => Tone(70);
            public Color T80  => Tone(80);
            public Color T90  => Tone(90);
            public Color T92  => Tone(92);
            public Color T94  => Tone(94);
            public Color T95  => Tone(95);
            public Color T96  => Tone(96);
            public Color T99  => Tone(99);
            public Color T100 => Tone(100);
        }

        public struct CorePalettes
        {
            public TonalPalette Primary;
            public TonalPalette Secondary;
            public TonalPalette Tertiary;
            public TonalPalette Neutral;
            public TonalPalette NeutralVariant;
            public TonalPalette Error;
        }

        /// <summary>Generate core palettes from a seed color.</summary>
        public static CorePalettes FromSeed(Color seed)
        {
            float h, c, t;
            MD3HCT.FromColor(seed, out h, out c, out t);

            // Ensure minimum chroma for meaningful colors
            if (c < 8f) c = 8f;

            return new CorePalettes
            {
                Primary        = new TonalPalette { Hue = h, Chroma = Mathf.Max(c, 48f) },
                Secondary      = new TonalPalette { Hue = h, Chroma = c * 0.33f },
                Tertiary       = new TonalPalette { Hue = (h + 60f) % 360f, Chroma = Mathf.Max(c * 0.5f, 24f) },
                Neutral        = new TonalPalette { Hue = h, Chroma = 4f },
                NeutralVariant = new TonalPalette { Hue = h, Chroma = 8f },
                Error          = new TonalPalette { Hue = 25f, Chroma = 84f },
            };
        }

        /// <summary>Generate a Light MD3Theme from core palettes.</summary>
        public static MD3Theme ToLightScheme(CorePalettes p)
        {
            return new MD3Theme
            {
                IsDark = false,

                Primary              = p.Primary.T40,
                OnPrimary            = p.Primary.T100,
                PrimaryContainer     = p.Primary.T90,
                OnPrimaryContainer   = p.Primary.T10,

                Secondary            = p.Secondary.T40,
                OnSecondary          = p.Secondary.T100,
                SecondaryContainer   = p.Secondary.T90,
                OnSecondaryContainer = p.Secondary.T10,

                Tertiary             = p.Tertiary.T40,
                OnTertiary           = p.Tertiary.T100,
                TertiaryContainer    = p.Tertiary.T90,
                OnTertiaryContainer  = p.Tertiary.T10,

                Error                = p.Error.T40,
                OnError              = p.Error.T100,
                ErrorContainer       = p.Error.T90,
                OnErrorContainer     = p.Error.T10,

                Surface              = p.Neutral.T99,
                OnSurface            = p.Neutral.T10,
                SurfaceVariant       = p.NeutralVariant.T90,
                OnSurfaceVariant     = p.NeutralVariant.T30,

                Outline              = p.NeutralVariant.T50,
                OutlineVariant       = p.NeutralVariant.T80,

                InverseSurface       = p.Neutral.T20,
                InverseOnSurface     = p.Neutral.T95,
                InversePrimary       = p.Primary.T80,

                SurfaceContainerLowest  = p.Neutral.T100,
                SurfaceContainerLow     = p.Neutral.T96,
                SurfaceContainer        = p.Neutral.T94,
                SurfaceContainerHigh    = p.Neutral.T92,
                SurfaceContainerHighest = p.Neutral.T90,
            };
        }

        /// <summary>Generate a Dark MD3Theme from core palettes.</summary>
        public static MD3Theme ToDarkScheme(CorePalettes p)
        {
            return new MD3Theme
            {
                IsDark = true,

                Primary              = p.Primary.T80,
                OnPrimary            = p.Primary.T20,
                PrimaryContainer     = p.Primary.T30,
                OnPrimaryContainer   = p.Primary.T90,

                Secondary            = p.Secondary.T80,
                OnSecondary          = p.Secondary.T20,
                SecondaryContainer   = p.Secondary.T30,
                OnSecondaryContainer = p.Secondary.T90,

                Tertiary             = p.Tertiary.T80,
                OnTertiary           = p.Tertiary.T20,
                TertiaryContainer    = p.Tertiary.T30,
                OnTertiaryContainer  = p.Tertiary.T90,

                Error                = p.Error.T80,
                OnError              = p.Error.T20,
                ErrorContainer       = p.Error.T30,
                OnErrorContainer     = p.Error.T90,

                Surface              = p.Neutral.T6,
                OnSurface            = p.Neutral.T90,
                SurfaceVariant       = p.NeutralVariant.T30,
                OnSurfaceVariant     = p.NeutralVariant.T80,

                Outline              = p.NeutralVariant.T60,
                OutlineVariant       = p.NeutralVariant.T30,

                InverseSurface       = p.Neutral.T90,
                InverseOnSurface     = p.Neutral.T20,
                InversePrimary       = p.Primary.T40,

                SurfaceContainerLowest  = p.Neutral.T4,
                SurfaceContainerLow     = p.Neutral.T10,
                SurfaceContainer        = p.Neutral.T12,
                SurfaceContainerHigh    = p.Neutral.T17,
                SurfaceContainerHighest = p.Neutral.T22,
            };
        }
    }
}
