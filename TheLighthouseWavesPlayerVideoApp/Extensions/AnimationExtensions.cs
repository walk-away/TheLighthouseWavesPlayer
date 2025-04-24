using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace TheLighthouseWavesPlayerVideoApp.Extensions
{
    public static class AnimationExtensions
    {
        public static Task<bool> FadeInWithScaleAsync(this VisualElement element, uint duration = 250, double startOpacity = 0, double endOpacity = 1)
        {
            element.Opacity = startOpacity;
            element.Scale = 0.8;
            
            return Task.WhenAll(
                element.FadeTo(endOpacity, duration, Easing.CubicOut),
                element.ScaleTo(1, duration, Easing.SpringOut)
            ).ContinueWith(t => true);
        }
        
        public static Task<bool> FadeOutWithScaleAsync(this VisualElement element, uint duration = 250, double endOpacity = 0)
        {
            return Task.WhenAll(
                element.FadeTo(endOpacity, duration, Easing.CubicIn),
                element.ScaleTo(0.8, duration, Easing.CubicIn)
            ).ContinueWith(t => true);
        }
        
        public static Task<bool> HeartBeatAsync(this VisualElement element, uint duration = 500, double scale = 1.2)
        {
            return Task.WhenAll(
                element.ScaleTo(scale, duration / 2, Easing.CubicOut),
                element.ScaleTo(1, duration / 2, Easing.CubicIn).ContinueWith(t => true)
            ).ContinueWith(t => true);
        }
    }
}