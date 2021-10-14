using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace Xamarin.Forms.BetterMaps
{
    public class Pin : Element
    {
        public static readonly BindableProperty PinIdProperty =
            BindableProperty.Create(nameof(PinId), typeof(Guid), typeof(Pin), default(Guid));

        public static readonly BindableProperty TagsProperty =
            BindableProperty.Create(nameof(Tags), typeof(Dictionary<string, string>), typeof(Pin), new Dictionary<string, string>());
    
        public static readonly BindableProperty TintColorProperty =
            BindableProperty.Create(nameof(TintColor), typeof(Color), typeof(Pin), Color.Transparent);

        public static readonly BindableProperty PositionProperty =
            BindableProperty.Create(nameof(Position), typeof(Position), typeof(Pin), default(Position));

        public static readonly BindableProperty AddressProperty =
            BindableProperty.Create(nameof(Address), typeof(string), typeof(Pin), default(string));

        public static readonly BindableProperty LabelProperty =
            BindableProperty.Create(nameof(Label), typeof(string), typeof(Pin), default(string));

        public static readonly BindableProperty AnchorProperty =
            BindableProperty.Create(nameof(Anchor), typeof(Point), typeof(Pin), new Point(0.5, 1.0));

        public static readonly BindableProperty ImageSourceProperty =
            BindableProperty.Create(nameof(ImageSource), typeof(ImageSource), typeof(Pin));

        public static readonly BindableProperty ZIndexProperty =
            BindableProperty.Create(nameof(ZIndex), typeof(int), typeof(Pin));

        public static readonly BindableProperty CanShowInfoWindowProperty =
            BindableProperty.Create(nameof(CanShowInfoWindow), typeof(bool), typeof(Pin), true);

        public Guid PinId
        {
            get => (Guid)GetValue(PinIdProperty);
            set => SetValue(PinIdProperty, value);
        }
        
        public Dictionary<string, string> Tags
        {
            get => (Dictionary<string, string>)GetValue(TagsProperty);
            set => SetValue(TagsProperty, value);
        }
        
        public Color TintColor
        {
            get => (Color)GetValue(TintColorProperty);
            set => SetValue(TintColorProperty, value);
        }

        public string Address
        {
            get => (string)GetValue(AddressProperty);
            set => SetValue(AddressProperty, value);
        }

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }

        public Position Position
        {
            get => (Position)GetValue(PositionProperty);
            set => SetValue(PositionProperty, value);
        }

        public Point Anchor
        {
            get => (Point)GetValue(AnchorProperty);
            set => SetValue(AnchorProperty, value);
        }

        public ImageSource ImageSource
        {
            get => (ImageSource)GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }

        public int ZIndex
        {
            get => (int)GetValue(ZIndexProperty);
            set => SetValue(ZIndexProperty, value);
        }

        public bool CanShowInfoWindow
        {
            get => (bool)GetValue(CanShowInfoWindowProperty);
            set => SetValue(CanShowInfoWindowProperty, value);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public object NativeId { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public CancellationTokenSource ImageSourceCts { get; set; }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((Pin)obj);
        }

        public override int GetHashCode()
            => HashCode.Combine(TintColor, Label, Address, Position, ZIndex);

        public static bool operator ==(Pin left, Pin right)
            => Equals(left, right);

        public static bool operator !=(Pin left, Pin right)
            => !Equals(left, right);

        private bool Equals(Pin other) => other is Pin pin && 
            PinId == pin.PinId &&
            TintColor == pin.TintColor &&
            Label == pin.Label &&
            Address == pin.Address &&
            Position == pin.Position &&
            ZIndex == pin.ZIndex;
    }
}