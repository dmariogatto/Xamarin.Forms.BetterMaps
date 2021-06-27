using Android.Content;
using Android.Graphics;
using System.Threading.Tasks;
using Xamarin.Forms.Platform.Android;

namespace Xamarin.Forms.BetterMaps
{
    internal static class ImageSourceExtensions
    {
        public static Task<Bitmap> LoadNativeAsync(this ImageSource source, Context context) => source switch
        {
            UriImageSource _ => new ImageLoaderSourceHandler().LoadImageAsync(source, context),
            FileImageSource _ => new FileImageSourceHandler().LoadImageAsync(source, context),
            FontImageSource _ => new FontImageSourceHandler().LoadImageAsync(source, context),
            StreamImageSource _ => new StreamImagesourceHandler().LoadImageAsync(source, context),
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