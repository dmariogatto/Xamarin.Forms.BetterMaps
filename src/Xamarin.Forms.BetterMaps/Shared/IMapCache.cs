using System;

namespace Xamarin.Forms.BetterMaps
{
    public interface IMapCache
    {
        bool TryGetValue<T>(object key, out T value);
        void SetAbsolute<T>(object key, T value, TimeSpan expires);
        void SetSliding<T>(object key, T value, TimeSpan sliding);
    }
}