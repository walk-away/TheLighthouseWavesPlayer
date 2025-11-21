using CommunityToolkit.Maui.Behaviors;

namespace TheLighthouseWavesPlayerVideoApp.Behaviors;

public static class TintBehavior
{
    public static readonly BindableProperty TintColorProperty =
        BindableProperty.CreateAttached(
            "TintColor",
            typeof(Color),
            typeof(TintBehavior),
            null,
            propertyChanged: OnTintColorChanged);

    public static Color GetTintColor(BindableObject view)
    {
        ArgumentNullException.ThrowIfNull(view);
        return (Color)view.GetValue(TintColorProperty);
    }

    public static void SetTintColor(BindableObject view, Color value)
    {
        ArgumentNullException.ThrowIfNull(view);
        view.SetValue(TintColorProperty, value);
    }

    private static void OnTintColorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is ImageButton imageButton && newValue is Color color)
        {
            var existingBehavior = imageButton.Behaviors
                .OfType<IconTintColorBehavior>()
                .FirstOrDefault();

            if (existingBehavior == null)
            {
                imageButton.Behaviors.Add(new IconTintColorBehavior { TintColor = color });
            }
            else
            {
                existingBehavior.TintColor = color;
            }
        }
    }
}
