using System;

namespace Xamarin.Forms.BetterMaps
{
    internal static class ColorExtensions
    {
        public static float ToAndroidHue(this Color color)
            => ((float)color.Hue * 360f) % 360f;
    }
}