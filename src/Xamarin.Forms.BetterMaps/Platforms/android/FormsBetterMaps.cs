using Android.App;
using Android.Gms.Common;
using Android.Gms.Maps;
using Android.OS;
using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.BetterMaps;
using Xamarin.Forms.BetterMaps.Android;

[assembly: ExportRenderer(typeof(Map), typeof(MapRenderer))]

namespace Xamarin
{
    public static class FormsBetterMaps
	{
		internal static readonly Dictionary<MapTheme, string> AssetFileNames = new Dictionary<MapTheme, string>();

		public static bool IsInitialized { get; private set; }
		public static IMapCache Cache { get; private set; }

		public static void Init(Activity activity, Bundle bundle, IMapCache mapCache = null)
		{
			if (IsInitialized)
				return;
			IsInitialized = true;
			Cache = mapCache;

			MapRenderer.Bundle = bundle;

#pragma warning disable 618
			if (GooglePlayServicesUtil.IsGooglePlayServicesAvailable(activity) == ConnectionResult.Success)
#pragma warning restore 618
			{
				try
				{
					MapsInitializer.Initialize(activity);
				}
				catch (Exception e)
				{
					Console.WriteLine("Google Play Services Not Found");
					Console.WriteLine("Exception: {0}", e);
				}
			}

			new GeocoderBackend(activity).Register();
		}

		public static void SetLightThemeAsset(string assetFileName)
			=> AssetFileNames[MapTheme.Light] = assetFileName;

		public static void SetDarkThemeAsset(string assetFileName)
			=> AssetFileNames[MapTheme.Dark] = assetFileName;
	}
}