using BD.WTTS.UI.Styling;

namespace BD.WTTS.UI;

partial class App
{
    const AppTheme _DefaultActualTheme = AppTheme.Dark;

    AppTheme IApplication.DefaultActualTheme => _DefaultActualTheme;

    AppTheme mTheme = _DefaultActualTheme;

    public AppTheme Theme
    {
        get => mTheme;
        set
        {
            if (value == mTheme) return;
            AppTheme switch_value = value;

            if (value == AppTheme.FollowingSystem)
            {
                var dps = IPlatformService.Instance;
                var isLightOrDarkTheme = dps.IsLightOrDarkTheme;
                if (isLightOrDarkTheme.HasValue)
                {
                    switch_value = IApplication.GetAppThemeByIsLightOrDarkTheme(isLightOrDarkTheme.Value);
                    dps.SetLightOrDarkThemeFollowingSystem(true);
                    if (switch_value == mTheme) goto setValue;
                }
            }
            else if (mTheme == AppTheme.FollowingSystem)
            {
                var dps = IPlatformService.Instance;
                dps.SetLightOrDarkThemeFollowingSystem(false);
                var isLightOrDarkTheme = dps.IsLightOrDarkTheme;
                if (isLightOrDarkTheme.HasValue)
                {
                    var mThemeFS = IApplication.GetAppThemeByIsLightOrDarkTheme(isLightOrDarkTheme.Value);
                    if (mThemeFS == switch_value) goto setValue;
                }
            }

            SetThemeNotChangeValue(switch_value);

        setValue: mTheme = value;
        }
    }

    public void SetThemeNotChangeValue(AppTheme value)
    {
        var mode = value switch
        {
            AppTheme.HighContrast => CustomTheme.HighContrastTheme,
            AppTheme.Light => ThemeVariant.Light,
            _ => ThemeVariant.Dark,
        };

        RequestedThemeVariant = mode;
    }

    public static void SetThemeAccent(string? colorHex)
    {
        if (colorHex == null)
        {
            return;
        }
        var thm = Ioc.Get<FluentAvaloniaTheme>()!;

        if (Color.TryParse(colorHex, out var color))
        {
            thm.CustomAccentColor = color;
        }
        else
        {
            if (colorHex.Equals(bool.TrueString, StringComparison.OrdinalIgnoreCase))
            {
                thm.CustomAccentColor = null;
            }
        }

#if WINDOWS
#endif
        if (OperatingSystem.IsWindows())
        {
            if (OperatingSystem.IsWindowsVersionAtLeast(6, 2))
                thm.PreferUserAccentColor = true;
            else
                thm.PreferUserAccentColor = false;
        }
        else
        {
            thm.PreferUserAccentColor = true;
        }
    }
}