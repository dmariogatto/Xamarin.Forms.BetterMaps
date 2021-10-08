using Android.Gms.Maps;
using System;

namespace Xamarin.Forms.BetterMaps.Android
{
    internal class OnGoogleMapReadyEventArgs : EventArgs
    {
        public GoogleMap Map { get; private set; }

        public OnGoogleMapReadyEventArgs(GoogleMap map)
        {
            Map = map;
        }
    }

    internal class OnMapCallback : Java.Lang.Object, IOnMapReadyCallback
    {
        public event EventHandler<OnGoogleMapReadyEventArgs> OnGoogleMapReady;

        public void OnMapReady(GoogleMap map)
        {
            OnGoogleMapReady?.Invoke(this, new OnGoogleMapReadyEventArgs(map));
        }
    }
}
