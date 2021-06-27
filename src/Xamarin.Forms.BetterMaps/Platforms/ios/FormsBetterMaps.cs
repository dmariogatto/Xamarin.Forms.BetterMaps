using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.BetterMaps;
using Xamarin.Forms.BetterMaps.iOS;

[assembly: ExportRenderer(typeof(Map), typeof(MapRenderer))]

namespace Xamarin
{
    public static class FormsBetterMaps
	{
        public static bool Initialized;

        public static bool? IsiOs13OrNewer;
        public static bool? IsiOs14OrNewer;

        public static IMapCache Cache { get; private set; }

        internal static bool iOs13OrNewer
            => IsiOs13OrNewer ??= UIDevice.CurrentDevice.CheckSystemVersion(13, 0);

        internal static bool iOs14OrNewer
            => IsiOs14OrNewer ??= UIDevice.CurrentDevice.CheckSystemVersion(14, 0);

        public static void Init(IMapCache mapCache = null)
		{
			if (Initialized)
				return;
			GeocoderBackend.Register();
            Initialized = true;
            Cache = mapCache;
        }
	}
}