using Android;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.OS;
#if ANDROIDX
using AndroidX.Core.Content;
#else
using Android.Support.V4.Content;
#endif
using Java.Lang;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.Android;
using ACircle = Android.Gms.Maps.Model.Circle;
using APolygon = Android.Gms.Maps.Model.Polygon;
using APolyline = Android.Gms.Maps.Model.Polyline;
using Math = System.Math;

namespace Xamarin.Forms.BetterMaps.Android
{
    [Preserve(AllMembers = true)]
    public class MapRenderer : ViewRenderer<Map, MapView>, IOnMapReadyCallback
    {
        internal static Bundle Bundle { get; set; }

        private static readonly ConcurrentDictionary<string, MapStyleOptions> MapStyles = new ConcurrentDictionary<string, MapStyleOptions>();

        private readonly Dictionary<string, (Pin pin, Marker marker)> _markers = new Dictionary<string, (Pin, Marker)>();
        private readonly Dictionary<string, (Polyline element, APolyline polyline)> _polylines = new Dictionary<string, (Polyline, APolyline)>();
        private readonly Dictionary<string, (Polygon element, APolygon polygon)> _polygons = new Dictionary<string, (Polygon, APolygon)>();
        private readonly Dictionary<string, (Circle element, ACircle circle)> _circles = new Dictionary<string, (Circle, ACircle)>();

        private static event EventHandler<EventArgs> MapViewCreated;
        private static event EventHandler<EventArgs> MapViewDestroyed;
        private static int MapViewCount = 0;

        private bool _disposed;

        private bool _isVisible = true;

        private Page _parentPage;
        private MapView _mapView;

        protected Map MapModel => Element;
        protected GoogleMap MapNative;

        public MapRenderer(Context context) : base(context)
        {
            AutoPackage = false;
        }

        void IOnMapReadyCallback.OnMapReady(GoogleMap map)
        {
            MapNative = map;
            OnMapReady(map);
        }

        #region Overrides

        public override SizeRequest GetDesiredSize(int widthConstraint, int heightConstraint)
            => new SizeRequest(new Size(Context.ToPixels(40), Context.ToPixels(40)));

        protected override MapView CreateNativeControl()
            => _mapView = new MapView(Context);

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            _disposed = true;

            if (disposing)
            {
                CleanUpMapModelElements(Element);

                if (MapNative != null)
                {
                    CleanUpNativeMap(MapNative);

                    MapNative.Dispose();
                    MapNative = null;

                    _mapView = null;

                    Interlocked.Decrement(ref MapViewCount);
                    MapViewDestroyed?.Invoke(this, new EventArgs());
                }
            }

            base.Dispose(disposing);
        }

        protected override void OnElementChanged(ElementChangedEventArgs<Map> e)
        {
            base.OnElementChanged(e);

            Page getParentPage(Element element)
            {
                var current = element;

                while (current != null)
                {
                    if (current is Page pg) return pg;
                    current = current.Parent;
                }

                return null;
            }

            var oldMapView = _mapView;

            _mapView = CreateNativeControl();
            _mapView.OnCreate(Bundle);
            _mapView.OnResume();
            SetNativeControl(_mapView);

            if (e.OldElement != null)
            {
                CleanUpMapModelElements(e.OldElement);
            }

            if (MapNative != null)
            {
                CleanUpNativeMap(MapNative);

                MapNative.Dispose();
                MapNative = null;
            }

            if (oldMapView != null)
            {
                oldMapView.Dispose();
                oldMapView = null;

                Interlocked.Decrement(ref MapViewCount);
                MapViewDestroyed?.Invoke(this, new EventArgs());
            }

            Interlocked.Increment(ref MapViewCount);
            MapViewCreated?.Invoke(this, new EventArgs());

            if (e.NewElement != null)
            {
                var mapModel = e.NewElement;

                Control.GetMapAsync(this);

                MessagingCenter.Subscribe<Map, MapSpan>(this, Map.MoveToRegionMessageName, OnMoveToRegionMessage, mapModel);

                mapModel.Pins.CollectionChanged += OnPinCollectionChanged;
                mapModel.MapElements.CollectionChanged += OnMapElementCollectionChanged;

                // Android does not like multiple active MapViews
                MapViewCreated += OnMapViewCreated;
                MapViewDestroyed += OnMapViewDestroyed;

                _parentPage = getParentPage(mapModel);
                if (_parentPage != null)
                {
                    _parentPage.Appearing += PageOnAppearing;
                    _parentPage.Disappearing += PageOnDisappearing;
                }
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            if (MapNative == null) return;

            if (e.PropertyName == Map.MapThemeProperty.PropertyName)
                LoadMapStyle(MapNative, MapModel.MapTheme, Context);
            else if (e.PropertyName == Map.MapTypeProperty.PropertyName)
                SetMapType();
            else if (e.PropertyName == Map.IsShowingUserProperty.PropertyName)
                SetUserVisible();
            else if (e.PropertyName == Map.ShowUserLocationButtonProperty.PropertyName)
                MapNative.UiSettings.MyLocationButtonEnabled = MapModel.ShowUserLocationButton;
            else if (e.PropertyName == Map.ShowCompassProperty.PropertyName)
                MapNative.UiSettings.CompassEnabled = MapModel.ShowCompass;
            else if (e.PropertyName == Map.SelectedPinProperty.PropertyName)
                UpdateSelectedPin();
            else if (e.PropertyName == Map.HasScrollEnabledProperty.PropertyName)
                MapNative.UiSettings.ScrollGesturesEnabled = MapModel.HasScrollEnabled;
            else if (e.PropertyName == Map.HasZoomEnabledProperty.PropertyName)
                MapNative.UiSettings.ZoomGesturesEnabled = MapModel.HasZoomEnabled;
            else if (e.PropertyName == Map.TrafficEnabledProperty.PropertyName)
                MapNative.TrafficEnabled = MapModel.TrafficEnabled;
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            base.OnLayout(changed, l, t, r, b);

            if (changed && Element.MoveToLastRegionOnLayoutChange)
            {
                MoveToRegion(Element.LastMoveToRegion, false);
            }

            if (MapNative != null)
                UpdateVisibleRegion(MapNative.CameraPosition.Target);

        }
        #endregion

        protected virtual void OnMapReady(GoogleMap map)
        {
            if (map == null) return;

            LoadMapStyle(MapNative, MapModel.MapTheme, Context);

            MapNative.CameraIdle += OnCameraIdle;
            map.MarkerClick += OnMarkerClick;
            map.InfoWindowClick += OnInfoWindowClick;
            map.InfoWindowClose += OnInfoWindowClose;
            map.InfoWindowLongClick += OnInfoWindowLongClick;
            map.MapClick += OnMapClick;

            map.TrafficEnabled = MapModel.TrafficEnabled;
            map.UiSettings.MyLocationButtonEnabled = MapModel.ShowUserLocationButton;
            map.UiSettings.CompassEnabled = MapModel.ShowCompass;

            map.UiSettings.ZoomControlsEnabled = false;

            map.UiSettings.ZoomGesturesEnabled = MapModel.HasZoomEnabled;
            map.UiSettings.ScrollGesturesEnabled = MapModel.HasScrollEnabled;
            map.UiSettings.MapToolbarEnabled = false;

            SetUserVisible();
            SetMapType();

            MoveToRegion(Element.LastMoveToRegion, false);
            OnPinCollectionChanged(Element.Pins, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnMapElementCollectionChanged(Element.MapElements, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            UpdateSelectedPin();
        }

        protected virtual MarkerOptions CreateMarker(Pin pin)
        {
            var opts = new MarkerOptions();

            opts.SetPosition(new LatLng(pin.Position.Latitude, pin.Position.Longitude));
            opts.SetTitle(pin.Label);
            opts.SetSnippet(pin.Address);
            opts.Anchor((float)pin.Anchor.X, (float)pin.Anchor.Y);
            opts.InvokeZIndex(pin.ZIndex);

            var bitmap = GetMarkerBitmap(pin.ImageSource, pin.TintColor);

            opts.SetIcon(bitmap != null
                ? BitmapDescriptorFactory.FromBitmap(bitmap)
                : BitmapDescriptorFactory.DefaultMarker(((float)pin.TintColor.Hue * 360f) % 360f));

            return opts;
        }

        #region Map
        private void OnMapClick(object sender, GoogleMap.MapClickEventArgs e)
        {
            MapModel.SendMapClicked(new Position(e.Point.Latitude, e.Point.Longitude));
        }

        private void MoveToRegion(MapSpan span, bool animate)
        {
            if (MapNative == null) return;

            span = span.ClampLatitude(85, -85);
            var ne = new LatLng(span.Center.Latitude + span.LatitudeDegrees / 2,
                span.Center.Longitude + span.LongitudeDegrees / 2);
            var sw = new LatLng(span.Center.Latitude - span.LatitudeDegrees / 2,
                span.Center.Longitude - span.LongitudeDegrees / 2);
            CameraUpdate update = CameraUpdateFactory.NewLatLngBounds(new LatLngBounds(sw, ne), 0);

            try
            {
                if (animate)
                    MapNative.AnimateCamera(update);
                else
                    MapNative.MoveCamera(update);
            }
            catch (IllegalStateException exc)
            {
                System.Diagnostics.Debug.WriteLine("MoveToRegion exception: " + exc);
                Log.Warning("Xamarin.Forms MapRenderer", $"MoveToRegion exception: {exc}");
            }
        }

        private void OnMoveToRegionMessage(Map s, MapSpan a)
        {
            MoveToRegion(a, true);
        }

        private void SetMapType()
        {
            if (MapNative == null) return;

            var mapType = MapModel?.MapType ?? MapType.Street;
            MapNative.MapType = mapType switch
            {
                MapType.Street => GoogleMap.MapTypeNormal,
                MapType.Satellite => GoogleMap.MapTypeSatellite,
                MapType.Hybrid => GoogleMap.MapTypeHybrid,
                _ => throw new NotSupportedException($"Unknown map type '{mapType}'")
            };
        }

        private void UpdateVisibleRegion(LatLng pos)
        {
            if (MapNative == null) return;

            Projection projection = MapNative.Projection;
            int width = Control.Width;
            int height = Control.Height;
            LatLng ul = projection.FromScreenLocation(new global::Android.Graphics.Point(0, 0));
            LatLng ur = projection.FromScreenLocation(new global::Android.Graphics.Point(width, 0));
            LatLng ll = projection.FromScreenLocation(new global::Android.Graphics.Point(0, height));
            LatLng lr = projection.FromScreenLocation(new global::Android.Graphics.Point(width, height));
            double dlat = Math.Max(Math.Abs(ul.Latitude - lr.Latitude), Math.Abs(ur.Latitude - ll.Latitude));
            double dlong = Math.Max(Math.Abs(ul.Longitude - lr.Longitude), Math.Abs(ur.Longitude - ll.Longitude));
            Element.SetVisibleRegion(new MapSpan(new Position(pos.Latitude, pos.Longitude), dlat, dlong, MapNative.CameraPosition.Bearing));
        }

        private void SetUserVisible()
        {
            if (MapNative == null) return;

            if (MapModel.IsShowingUser)
            {
                var coarseLocationPermission = ContextCompat.CheckSelfPermission(Context, Manifest.Permission.AccessCoarseLocation);
                var fineLocationPermission = ContextCompat.CheckSelfPermission(Context, Manifest.Permission.AccessFineLocation);

                if (coarseLocationPermission == Permission.Granted || fineLocationPermission == Permission.Granted)
                {
                    MapNative.MyLocationEnabled = true;
                }
                else
                {
                    Log.Warning("Xamarin.Forms.MapRenderer", "Missing location permissions for ShowUserLocation");
                    MapNative.MyLocationEnabled = false;
                }
            }
            else
            {
                MapNative.MyLocationEnabled = false;
            }
        }

        private void UpdateSelectedPin()
        {
            var pin = MapModel.SelectedPin;

            if (pin == null)
            {
                _markers.Values.ForEach(i => i.marker.HideInfoWindow());
            }
            else if (GetMarkerForPin(pin) is Marker m)
            {
                m.ShowInfoWindow();
            }
        }

        private void OnCameraIdle(object sender, EventArgs args)
        {
            UpdateVisibleRegion(MapNative.CameraPosition.Target);
        }
        #endregion

        #region Pins
        private void AddPins(IList<Pin> pins)
        {
            if (MapNative == null) return;

            foreach (var p in pins)
            {
                var opts = CreateMarker(p);
                var marker = MapNative.AddMarker(opts);

                p.PropertyChanged += PinOnPropertyChanged;

                // associate pin with marker for later lookup in event handlers
                p.MarkerId = marker.Id;

                if (ReferenceEquals(p, MapModel.SelectedPin))
                    marker.ShowInfoWindow();

                _markers.Add(marker.Id, (p, marker));
            }
        }

        private void PinOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var pin = (Pin)sender;
            var marker = GetMarkerForPin(pin);

            if (marker == null) return;

            if (e.PropertyName == Pin.LabelProperty.PropertyName)
            {
                marker.Title = pin.Label;
                if (marker.IsInfoWindowShown)
                    marker.ShowInfoWindow();
            }
            else if (e.PropertyName == Pin.AddressProperty.PropertyName)
            {
                marker.Snippet = pin.Address;
                if (marker.IsInfoWindowShown)
                    marker.ShowInfoWindow();
            }
            else if (e.PropertyName == Pin.PositionProperty.PropertyName)
                marker.Position = new LatLng(pin.Position.Latitude, pin.Position.Longitude);
            else if (e.PropertyName == Pin.AnchorProperty.PropertyName)
                marker.SetAnchor((float)pin.Anchor.X, (float)pin.Anchor.Y);
            else if (e.PropertyName == Pin.ZIndexProperty.PropertyName)
                marker.ZIndex = pin.ZIndex;
            else if (e.PropertyName == Pin.ImageSourceProperty.PropertyName ||
                     e.PropertyName == Pin.TintColorProperty.PropertyName)
            {
                var bitmap = GetMarkerBitmap(pin.ImageSource, pin.TintColor);

                marker.SetIcon(bitmap != default
                    ? BitmapDescriptorFactory.FromBitmap(bitmap)
                    : BitmapDescriptorFactory.DefaultMarker(((float)pin.TintColor.Hue * 360f) % 360f));
            }
        }

        private Bitmap GetMarkerBitmap(ImageSource imgSource, Color tint)
        {
            if (imgSource == null)
                return default;

            var bitmap = default(Bitmap);

            if (tint != Color.Transparent)
            {
                var cacheKey = imgSource.CacheId() is string cId && !string.IsNullOrEmpty(cId)
                    ? $"TintedBitmap_{HashCode.Combine(cId, tint)}"
                    : string.Empty;

                var filteredBitmap = default(Bitmap);

                if (FormsBetterMaps.Cache == null || !FormsBetterMaps.Cache.TryGetValue(cacheKey, out filteredBitmap))
                {
                    // a necessary evil
                    bitmap = imgSource.LoadNativeAsync(Context).Result;

                    filteredBitmap = bitmap.Copy(bitmap.GetConfig(), true);
                    var paint = new Paint();
                    var filter = new PorterDuffColorFilter(tint.ToAndroid(), PorterDuff.Mode.SrcIn);
                    paint.SetColorFilter(filter);
                    var canvas = new Canvas(filteredBitmap);
                    canvas.DrawBitmap(filteredBitmap, 0, 0, paint);

                    if (!string.IsNullOrEmpty(cacheKey))
                        FormsBetterMaps.Cache?.SetSliding(cacheKey, filteredBitmap, TimeSpan.FromMinutes(2));
                }

                bitmap = filteredBitmap;
            }

            return bitmap ?? imgSource.LoadNativeAsync(Context).Result;
        }

        protected Marker GetMarkerForPin(Pin pin)
            => pin?.MarkerId != null && _markers.TryGetValue((string)pin.MarkerId, out var i) ? i.marker : null;

        protected Pin GetPinForMarker(Marker marker)
            => _markers.TryGetValue(marker.Id, out var i) ? i.pin : null;

        private void OnMarkerClick(object sender, GoogleMap.MarkerClickEventArgs e)
        {
            var pin = GetPinForMarker(e.Marker);

            if (!ReferenceEquals(pin, MapModel.SelectedPin))
            {
                MapModel.SelectedPin = pin;
            }

            if (pin == null) return;

            // Setting e.Handled = true will prevent the info window from being presented
            var handled = MapModel.SendPinClick(pin);
            e.Handled = handled;
        }

        private void OnInfoWindowClose(object sender, GoogleMap.InfoWindowCloseEventArgs e)
        {
            var pin = GetPinForMarker(e.Marker);
            if (pin != null && ReferenceEquals(pin, MapModel.SelectedPin))
            {
                MapModel.SelectedPin = null;
            }
        }

        private void OnInfoWindowClick(object sender, GoogleMap.InfoWindowClickEventArgs e)
        {
            var marker = e.Marker;
            var pin = GetPinForMarker(marker);

            if (pin == null) return;

            // SendInfoWindowClick() returns the value of PinClickedEventArgs.HideInfoWindow
            var hideInfoWindow = MapModel.SendInfoWindowClick(pin);
            if (hideInfoWindow) marker.HideInfoWindow();
        }

        private void OnInfoWindowLongClick(object sender, GoogleMap.InfoWindowLongClickEventArgs e)
        {
            var marker = e.Marker;
            var pin = GetPinForMarker(marker);

            if (pin == null) return;

            // SendInfoWindowLongClick() returns the value of PinClickedEventArgs.HideInfoWindow
            var hideInfoWindow = MapModel.SendInfoWindowLongClick(pin);
            if (hideInfoWindow) marker.HideInfoWindow();
        }

        private void OnPinCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Device.IsInvokeRequired)
                Device.BeginInvokeOnMainThread(() => PinCollectionChanged(e));
            else
                PinCollectionChanged(e);
        }

        private void PinCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            var itemsToAdd = e.NewItems?.Cast<Pin>()?.ToList() ?? new List<Pin>(0);
            var itemsToRemove = e.OldItems?.Cast<Pin>()?.Where(p => p.MarkerId != null)?.ToList() ?? new List<Pin>(0);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddPins(itemsToAdd);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemovePins(itemsToRemove);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    RemovePins(itemsToRemove);
                    AddPins(itemsToAdd);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    RemovePins(_markers.Values.Select(i => i.pin).ToList());
                    AddPins(Element.Pins);
                    break;
                case NotifyCollectionChangedAction.Move:
                    //do nothing
                    break;
            }
        }

        private void RemovePins(IList<Pin> pins)
        {
            if (MapNative == null || !_markers.Any()) return;

            foreach (var p in pins)
            {
                p.PropertyChanged -= PinOnPropertyChanged;
                var marker = GetMarkerForPin(p);

                if (marker == null)
                    continue;

                marker.Remove();
                _markers.Remove(marker.Id);
            }
        }
        #endregion

        #region MapElements
        private void MapElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (sender)
            {
                case Polyline polyline:
                    PolylineOnPropertyChanged(polyline, e);
                    break;
                case Polygon polygon:
                    PolygonOnPropertyChanged(polygon, e);
                    break;
                case Circle circle:
                    CircleOnPropertyChanged(circle, e);
                    break;
            }
        }

        private void OnMapElementCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (Device.IsInvokeRequired)
                Device.BeginInvokeOnMainThread(() => MapElementCollectionChanged(e));
            else
                MapElementCollectionChanged(e);
        }

        private void MapElementCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddMapElements(e.NewItems.Cast<MapElement>());
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveMapElements(e.OldItems.Cast<MapElement>());
                    break;
                case NotifyCollectionChangedAction.Replace:
                    RemoveMapElements(e.OldItems.Cast<MapElement>());
                    AddMapElements(e.NewItems.Cast<MapElement>());
                    break;
                case NotifyCollectionChangedAction.Reset:
                    RemoveMapElements(_polylines.Values.Select(i => i.element).ToList());
                    RemoveMapElements(_polygons.Values.Select(i => i.element).ToList());
                    RemoveMapElements(_circles.Values.Select(i => i.element).ToList());

                    AddMapElements(Element.MapElements);
                    break;
            }
        }

        private void AddMapElements(IEnumerable<MapElement> mapElements)
        {
            foreach (var element in mapElements)
            {
                element.PropertyChanged += MapElementPropertyChanged;

                switch (element)
                {
                    case Polyline polyline:
                        AddPolyline(polyline);
                        break;
                    case Polygon polygon:
                        AddPolygon(polygon);
                        break;
                    case Circle circle:
                        AddCircle(circle);
                        break;
                }
            }
        }

        private void RemoveMapElements(IEnumerable<MapElement> mapElements)
        {
            foreach (var element in mapElements)
            {
                element.PropertyChanged -= MapElementPropertyChanged;

                switch (element)
                {
                    case Polyline polyline:
                        RemovePolyline(polyline);
                        break;
                    case Polygon polygon:
                        RemovePolygon(polygon);
                        break;
                    case Circle circle:
                        RemoveCircle(circle);
                        break;
                }

                element.MapElementId = null;
            }
        }
        #endregion

        #region Polylines
        protected virtual PolylineOptions CreatePolylineOptions(Polyline polyline)
        {
            var opts = new PolylineOptions();

            opts.InvokeColor(polyline.StrokeColor.ToAndroid(Color.Black));
            opts.InvokeWidth(polyline.StrokeWidth);

            foreach (var position in polyline.Geopath)
            {
                opts.Points.Add(new LatLng(position.Latitude, position.Longitude));
            }

            return opts;
        }

        protected APolyline GetNativePolyline(Polyline polyline)
            => polyline?.MapElementId != null && _polylines.TryGetValue((string)polyline.MapElementId, out var i) ? i.polyline : null;

        protected Polyline GetFormsPolyline(APolyline polyline)
            => _polylines.TryGetValue(polyline.Id, out var i) ? i.element : null;

        private void PolylineOnPropertyChanged(Polyline formsPolyline, PropertyChangedEventArgs e)
        {
            var nativePolyline = GetNativePolyline(formsPolyline);

            if (nativePolyline == null) return;

            if (e.PropertyName == MapElement.StrokeColorProperty.PropertyName)
                nativePolyline.Color = formsPolyline.StrokeColor.ToAndroid(Color.Black);
            else if (e.PropertyName == MapElement.StrokeWidthProperty.PropertyName)
                nativePolyline.Width = formsPolyline.StrokeWidth;
            else if (e.PropertyName == nameof(Polyline.Geopath))
                nativePolyline.Points = formsPolyline.Geopath.Select(position => new LatLng(position.Latitude, position.Longitude)).ToList();
        }

        private void AddPolyline(Polyline polyline)
        {
            if (MapNative == null) return;

            var options = CreatePolylineOptions(polyline);
            var nativePolyline = MapNative.AddPolyline(options);

            polyline.MapElementId = nativePolyline.Id;

            _polylines.Add(nativePolyline.Id, (polyline, nativePolyline));
        }

        private void RemovePolyline(Polyline polyline)
        {
            var native = GetNativePolyline(polyline);

            if (native != null)
            {
                native.Remove();
                _polylines.Remove(native.Id);
            }
        }
        #endregion

        #region Polygons
        protected virtual PolygonOptions CreatePolygonOptions(Polygon polygon)
        {
            var opts = new PolygonOptions();

            opts.InvokeStrokeColor(polygon.StrokeColor.ToAndroid(Color.Black));
            opts.InvokeStrokeWidth(polygon.StrokeWidth);

            if (!polygon.StrokeColor.IsDefault)
                opts.InvokeFillColor(polygon.FillColor.ToAndroid());

            // Will throw an exception when added to the map if Points is empty
            if (polygon.Geopath.Count == 0)
            {
                opts.Points.Add(new LatLng(0, 0));
            }
            else
            {
                foreach (var position in polygon.Geopath)
                {
                    opts.Points.Add(new LatLng(position.Latitude, position.Longitude));
                }
            }

            return opts;
        }

        protected APolygon GetNativePolygon(Polygon polygon)
            => polygon?.MapElementId != null && _polygons.TryGetValue((string)polygon.MapElementId, out var i) ? i.polygon : null;

        protected Polygon GetFormsPolygon(APolygon polygon)
            => _polygons.TryGetValue(polygon.Id, out var i) ? i.element : null;

        private void PolygonOnPropertyChanged(Polygon polygon, PropertyChangedEventArgs e)
        {
            var nativePolygon = GetNativePolygon(polygon);

            if (nativePolygon == null) return;

            if (e.PropertyName == MapElement.StrokeColorProperty.PropertyName)
                nativePolygon.StrokeColor = polygon.StrokeColor.ToAndroid(Color.Black);
            else if (e.PropertyName == MapElement.StrokeWidthProperty.PropertyName)
                nativePolygon.StrokeWidth = polygon.StrokeWidth;
            else if (e.PropertyName == Polygon.FillColorProperty.PropertyName)
                nativePolygon.FillColor = polygon.FillColor.ToAndroid();
            else if (e.PropertyName == nameof(polygon.Geopath))
                nativePolygon.Points = polygon.Geopath.Select(p => new LatLng(p.Latitude, p.Longitude)).ToList();
        }

        private void AddPolygon(Polygon polygon)
        {
            if (MapNative == null) return;

            var options = CreatePolygonOptions(polygon);
            var nativePolygon = MapNative.AddPolygon(options);

            polygon.MapElementId = nativePolygon.Id;

            _polygons.Add(nativePolygon.Id, (polygon, nativePolygon));
        }

        private void RemovePolygon(Polygon polygon)
        {
            var native = GetNativePolygon(polygon);

            if (native != null)
            {
                native.Remove();
                _polygons.Remove(native.Id);
            }
        }
        #endregion

        #region Circles
        protected virtual CircleOptions CreateCircleOptions(Circle circle)
        {
            var opts = new CircleOptions()
                .InvokeCenter(new LatLng(circle.Center.Latitude, circle.Center.Longitude))
                .InvokeRadius(circle.Radius.Meters)
                .InvokeStrokeWidth(circle.StrokeWidth);

            if (!circle.StrokeColor.IsDefault)
                opts.InvokeStrokeColor(circle.StrokeColor.ToAndroid());

            if (!circle.FillColor.IsDefault)
                opts.InvokeFillColor(circle.FillColor.ToAndroid());

            return opts;
        }

        protected ACircle GetNativeCircle(Circle circle)
            => circle?.MapElementId != null && _circles.TryGetValue((string)circle.MapElementId, out var i) ? i.circle : null;

        protected Circle GetFormsCircle(ACircle circle)
            => _circles.TryGetValue(circle.Id, out var i) ? i.element : null;

        private void CircleOnPropertyChanged(Circle formsCircle, PropertyChangedEventArgs e)
        {
            var nativeCircle = GetNativeCircle(formsCircle);

            if (nativeCircle == null) return;

            if (e.PropertyName == Circle.FillColorProperty.PropertyName)
                nativeCircle.FillColor = formsCircle.FillColor.ToAndroid();
            else if (e.PropertyName == Circle.CenterProperty.PropertyName)
                nativeCircle.Center = new LatLng(formsCircle.Center.Latitude, formsCircle.Center.Longitude);
            else if (e.PropertyName == Circle.RadiusProperty.PropertyName)
                nativeCircle.Radius = formsCircle.Radius.Meters;
            else if (e.PropertyName == MapElement.StrokeColorProperty.PropertyName)
                nativeCircle.StrokeColor = formsCircle.StrokeColor.ToAndroid();
            else if (e.PropertyName == MapElement.StrokeWidthProperty.PropertyName)
                nativeCircle.StrokeWidth = formsCircle.StrokeWidth;
        }

        private void AddCircle(Circle circle)
        {
            if (MapNative == null) return;

            var options = CreateCircleOptions(circle);
            var nativeCircle = MapNative.AddCircle(options);

            circle.MapElementId = nativeCircle.Id;

            _circles.Add(nativeCircle.Id, (circle, nativeCircle));
        }

        private void RemoveCircle(Circle circle)
        {
            var native = GetNativeCircle(circle);

            if (native != null)
            {
                native.Remove();
                _circles.Remove(native.Id);
            }
        }
        #endregion

        private void CleanUpMapModelElements(Map mapModel)
        {
            MessagingCenter.Unsubscribe<Map, MapSpan>(this, Map.MoveToRegionMessageName);

            mapModel.Pins.CollectionChanged -= OnPinCollectionChanged;
            mapModel.MapElements.CollectionChanged -= OnMapElementCollectionChanged;

            if (_parentPage != null)
            {
                _parentPage.Appearing -= PageOnAppearing;
                _parentPage.Disappearing -= PageOnDisappearing;
            }

            _parentPage = null;

            foreach (var kv in _markers)
            {
                kv.Value.pin.PropertyChanged -= PinOnPropertyChanged;
                kv.Value.pin.MarkerId = null;
                kv.Value.marker.Remove();
            }

            foreach (var kv in _polylines)
            {
                kv.Value.element.PropertyChanged -= MapElementPropertyChanged;
                kv.Value.element.MapElementId = null;
                kv.Value.polyline.Remove();
            }

            foreach (var kv in _polygons)
            {
                kv.Value.element.PropertyChanged -= MapElementPropertyChanged;
                kv.Value.element.MapElementId = null;
                kv.Value.polygon.Remove();
            }

            foreach (var kv in _circles)
            {
                kv.Value.element.PropertyChanged -= MapElementPropertyChanged;
                kv.Value.element.MapElementId = null;
                kv.Value.circle.Remove();
            }

            _markers.Clear();
            _polylines.Clear();
            _polygons.Clear();
        }

        private void CleanUpNativeMap(GoogleMap mapNative)
        {
            MapViewCreated -= OnMapViewCreated;
            MapViewDestroyed -= OnMapViewDestroyed;

            mapNative.MyLocationEnabled = false;
            mapNative.TrafficEnabled = false;

            mapNative.CameraIdle -= OnCameraIdle;
            mapNative.MarkerClick -= OnMarkerClick;
            mapNative.InfoWindowClick -= OnInfoWindowClick;
            mapNative.InfoWindowClose -= OnInfoWindowClose;
            mapNative.InfoWindowLongClick -= OnInfoWindowLongClick;
            mapNative.MapClick -= OnMapClick;
        }

        private void OnMapViewCreated(object sender, EventArgs e)
        {
            if (sender != this && MapViewCount > 1 && !_isVisible)
            {
                RemoveMapFromView();
            }
        }

        private void OnMapViewDestroyed(object sender, EventArgs e)
        {
            if (sender != this && MapViewCount == 1)
                AddMapToView();
        }

        private void PageOnAppearing(object sender, EventArgs e)
        {
            _isVisible = true;
            AddMapToView();
        }

        private void PageOnDisappearing(object sender, EventArgs e)
        {
            _isVisible = false;
        }

        private void AddMapToView()
        {
            if (_disposed)
                return;

            if (_mapView != null && ChildCount == 0)
                AddView(_mapView, -1);
        }

        private void RemoveMapFromView()
        {
            if (_disposed)
                return;

            // multiple attached map views cause weird visual gitches
            // e.g. pins that dance around
            if (_mapView != null && ChildCount == 1)
                RemoveView(_mapView);
        }

        private static void LoadMapStyle(GoogleMap map, MapTheme mapTheme, Context context)
        {
            if (mapTheme == MapTheme.System)
            {
                var uiModeFlags = context.Resources.Configuration.UiMode & UiMode.NightMask;
                mapTheme = uiModeFlags switch
                {
                    UiMode.NightYes => MapTheme.Dark,
                    UiMode.NightNo => MapTheme.Light,
                    _ => throw new NotSupportedException($"UiMode {uiModeFlags} not supported"),
                };
            }

            try
            {
                if (FormsBetterMaps.AssetFileNames.TryGetValue(mapTheme, out var assetName))
                {
                    if (!string.IsNullOrEmpty(assetName) && !MapStyles.ContainsKey(assetName))
                    {
                        var assets = context.Assets;
                        using var reader = new StreamReader(assets.Open(assetName));
                        MapStyles.AddOrUpdate(assetName, new MapStyleOptions(reader.ReadToEnd()), (k, v) => v);
                    }

                    map.SetMapStyle(MapStyles[assetName]);
                }
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
    }
}
