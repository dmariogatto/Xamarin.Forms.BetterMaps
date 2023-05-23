using Android.Content;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AGeocoder = Android.Locations.Geocoder;

namespace Xamarin.Forms.BetterMaps.Android
{
    internal static class GeocoderBackend
	{
		private static Context Context;

        private static AGeocoder AGeocoder;
        private static AGeocoder AndroidGeocoder => AGeocoder ??= new AGeocoder(Context);

		public static void Register(Context context)
		{
			if (Context is not null)
				return;

			Context = context;

            Geocoder.GetPositionsForAddressAsyncFunc = GetPositionsForAddressAsync;
            Geocoder.GetAddressesForPositionFuncAsync = GetAddressesForPositionAsync;
		}

		public static async Task<IEnumerable<Position>> GetPositionsForAddressAsync(string address)
		{
			var addresses = await AndroidGeocoder.GetFromLocationNameAsync(address, 5);
			return addresses.Select(p => new Position(p.Latitude, p.Longitude));
		}

		public static async Task<IEnumerable<string>> GetAddressesForPositionAsync(Position position)
		{
			var addresses = await AndroidGeocoder.GetFromLocationAsync(position.Latitude, position.Longitude, 5);
			return addresses.Select(p =>
			{
				IEnumerable<string> lines = Enumerable.Range(0, p.MaxAddressLineIndex + 1).Select(p.GetAddressLine);
				return string.Join("\n", lines);
			});
		}
	}
}