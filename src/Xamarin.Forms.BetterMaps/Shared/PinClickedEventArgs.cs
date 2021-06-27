using System;

namespace Xamarin.Forms.BetterMaps
{
    public class PinClickedEventArgs : EventArgs
    {
        public Pin Marker { get; }
        public bool HideInfoWindow { get; set; }

        public PinClickedEventArgs(Pin marker)
        {
            Marker = marker;
            HideInfoWindow = false;
        }
    }
}