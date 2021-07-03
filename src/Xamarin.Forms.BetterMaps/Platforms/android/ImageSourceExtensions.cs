using Android.Content;
using Android.Graphics;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms.Platform.Android;

namespace Xamarin.Forms.BetterMaps
{
    internal static class ImageSourceExtensions
    {
        public static Task<Bitmap> LoadNativeAsync(this ImageSource source, Context context, CancellationToken ct) => source switch
        {
            UriImageSource _ => new ImageLoaderSourceHandler().LoadImageAsync(source, context, ct),
            FileImageSource _ => new FileImageSourceHandler().LoadImageAsync(source, context, ct),
            FontImageSource _ => new FontImageSourceHandler().LoadImageAsync(source, context, ct),
            StreamImageSource _ => new StreamImagesourceHandler().LoadImageAsync(source, context, ct),
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