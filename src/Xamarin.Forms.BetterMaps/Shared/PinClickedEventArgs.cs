using System;

namespace Xamarin.Forms.BetterMaps
{
    public class PinClickedEventArgs : EventArgs
    {
        public Pin Pin { get; }
        public bool HideInfoWindow { get; set; }

        public PinClickedEventArgs(Pin pin)
        {
            Pin = pin;
            HideInfoWindow = false;
        }
    }
}