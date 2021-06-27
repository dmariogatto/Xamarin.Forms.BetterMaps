# Xamarin.Forms.BetterMaps

[![](https://img.shields.io/nuget/v/Xamarin.Forms.BetterMaps.svg)](https://nuget.org/packages/Xamarin.Forms.BetterMaps)

A more useful maps control for Android & iOS, based off of [Xamarin.Forms.Maps](https://nuget.org/packages/Xamarin.Forms.Maps).

## Why?

- Custom pins (including image, tint colour, anchor & z-index)
- User location button
- Compass button
- Map themes (light & dark)
- Tapped events
  - Map tapped
  - Pin tapped
  - Info window tapped
  - Info window long tapped

## Setup

This is designed to be a simple drop in replacement for anyone using [Xamarin.Forms.Maps](https://nuget.org/packages/Xamarin.Forms.Maps). Care has also been taken to maximise performance, e.g. mapping the pins to their native views is tracked using dictionaries, instead of scanning through lists.

__New Map Properties/Events__

| Property                 | Description                                                              |
|--------------------------|--------------------------------------------------------------------------|
| `MapTheme`               | The current theme of the Map (either System, Light or Dark)              |
| `ShowUserLocationButton` | Display the button that allows user to centre map on their location      |
| `ShowCompass`            | Display compass, which shows true north & allows user to reorient map    |
| `SelectedPin`            | The currently focused pin                                                |

| Event                    | Description                                                              |
|--------------------------|--------------------------------------------------------------------------|
| `SelectedPinChanged`     | Fired when selected pin is changed                                       |
| `PinClicked`             | Fired when user taps on a pin                                            |
| `InfoWindowClicked`      | Fired when user taps the info window (visible when pin is selected)      |
| `InfoWindowLongClicked`  | Fired when user long taps the info window (visible when pin is selected) |

__New Pin Properties__

| Property                  | Description                                                                     |
|---------------------------|---------------------------------------------------------------------------------|
| `TintColor`               | Sets a tint colour for the pin, or image (if `ImageSource` is set)              |
| `Anchor`                  | The coordinates to anchor the pin (e.g. to centre pin on location `(0.5, 0.5)`) |
| `ImageSourceProperty`     | Name of file image resource (i.e. custom pin image)                             |
| `ZIndex`                  | The z-index of the pin                                                          |

### Android

```csharp
public override bool FinishedLaunching(UIApplication app, NSDictionary options)
{
    ...    
    Xamarin.FormsBetterMaps.Init(this, savedInstanceState);

    // Light/dark theme need custom JSON style files (https://mapstyle.withgoogle.com/) added to 'Assets'
    Xamarin.FormsBetterMaps.SetLightThemeAsset("map.style.light.json");
    Xamarin.FormsBetterMaps.SetDarkThemeAsset("map.style.dark.json");    
    ...
}
```

### iOS

```csharp
public override bool FinishedLaunching(UIApplication app, NSDictionary options)
{
    ...
    Xamarin.FormsBetterMaps.Init();    
    ...
}
```

### Example

I dogfood my own packages, see an example at [AdelaideFuel](https://github.com/dmariogatto/adelaidefuel).
