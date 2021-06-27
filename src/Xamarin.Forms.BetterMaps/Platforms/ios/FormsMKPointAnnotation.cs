using CoreGraphics;
using MapKit;
using UIKit;

namespace Xamarin.Forms.BetterMaps.iOS
{
    internal class FormsMKPointAnnotation : MKPointAnnotation
    {
        public UIColor TintColor { get; set; }
        public CGPoint Anchor { get; set; }
        public int ZIndex { get; set; }
        public ImageSource ImageSource { get; set; }
    }
}