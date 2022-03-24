using Android.Gms.Maps;
using Android.Runtime;
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
        private bool _disposed;

        public OnMapCallback()
        {
        }

        public OnMapCallback(IntPtr handle, JniHandleOwnership transfer)
            : base(handle, transfer)
        {
        }

        public event EventHandler<OnGoogleMapReadyEventArgs> OnGoogleMapReady;

        public void OnMapReady(GoogleMap map)
        {
            if (_disposed)
                return;

            OnGoogleMapReady?.Invoke(this, new OnGoogleMapReadyEventArgs(map));
        }

        protected override void Dispose(bool disposing)
        {
            _disposed = true;
            base.Dispose(disposing);
        }
    }
}
