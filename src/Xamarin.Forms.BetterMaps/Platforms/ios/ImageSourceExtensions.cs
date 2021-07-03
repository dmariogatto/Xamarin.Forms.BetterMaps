using System.Threading;
using System.Threading.Tasks;
using UIKit;
using Xamarin.Forms.Platform.iOS;

namespace Xamarin.Forms.BetterMaps
{
    internal static class ImageSourceExtensions
    {
        public static Task<UIImage> LoadNativeAsync(this ImageSource source, CancellationToken ct) => source switch
        {
            UriImageSource _ => new ImageLoaderSourceHandler().LoadImageAsync(source, ct),
            FileImageSource _ => new FileImageSourceHandler().LoadImageAsync(source, ct),
            FontImageSource _ => new FontImageSourceHandler().LoadImageAsync(source, ct),
            StreamImageSource _ => new StreamImagesourceHandler().LoadImageAsync(source, ct),
            _ => null
        };

        public static string CacheId(this ImageSource source) => source switch
        {
            UriImageSource uriSource => uriSource.Uri.OriginalString,
            FileImageSource fileSource => fileSource.File,
            FontImageSource fontSource => $"{fontSource.FontFamily}_{fontSource.Glyph}",
            _ => null
        };
    }
}