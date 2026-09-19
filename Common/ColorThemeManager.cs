using System.Windows;
using System.Windows.Media;

namespace CititorRSS.Jaws;

/// <summary>Applies the small set of accessible application colour schemes.</summary>
public static class ColorThemeManager
{
    public const string Windows = "Windows";
    public const string BlackOnWhite = "BlackOnWhite";
    public const string WhiteOnBlack = "WhiteOnBlack";
    public const string BlackOnYellow = "BlackOnYellow";
    public const string YellowOnNavy = "YellowOnNavy";

    private static string _activeScheme = Windows;
    private static bool _subscribed;

    public static string Normalize(string? value) => value switch
    {
        BlackOnWhite => BlackOnWhite,
        WhiteOnBlack => WhiteOnBlack,
        BlackOnYellow => BlackOnYellow,
        YellowOnNavy => YellowOnNavy,
        _ => Windows
    };

    public static IReadOnlyList<string> SchemeIds { get; } =
        [Windows, BlackOnWhite, WhiteOnBlack, BlackOnYellow, YellowOnNavy];

    public static void Apply(string? value)
    {
        _activeScheme = Normalize(value);
        SubscribeToWindowsChanges();
        if (Application.Current is null) return;

        var palette = CreatePalette(_activeScheme);
        Set("ThemeWindowBrush", palette.Window);
        Set("ThemeWindowTextBrush", palette.WindowText);
        Set("ThemeControlBrush", palette.Control);
        Set("ThemeControlTextBrush", palette.ControlText);
        Set("ThemeBorderBrush", palette.Border);
        Set("ThemeMenuBrush", palette.Menu);
        Set("ThemeMenuTextBrush", palette.MenuText);
        Set("ThemeHighlightBrush", palette.Highlight);
        Set("ThemeHighlightTextBrush", palette.HighlightText);
        Set("ThemeSecondaryTextBrush", palette.SecondaryText);
    }

    private static void SubscribeToWindowsChanges()
    {
        if (_subscribed) return;
        SystemParameters.StaticPropertyChanged += (_, _) =>
        {
            if (_activeScheme == Windows) Apply(Windows);
        };
        _subscribed = true;
    }

    private static void Set(string key, Brush brush) => Application.Current!.Resources[key] = brush;

    private static Palette CreatePalette(string scheme)
    {
        if (scheme == Windows)
        {
            return new Palette(
                SystemColors.WindowBrush,
                SystemColors.WindowTextBrush,
                SystemColors.ControlBrush,
                SystemColors.ControlTextBrush,
                SystemColors.ActiveBorderBrush,
                SystemColors.MenuBrush,
                SystemColors.MenuTextBrush,
                SystemColors.HighlightBrush,
                SystemColors.HighlightTextBrush,
                SystemColors.WindowTextBrush);
        }

        return scheme switch
        {
            BlackOnWhite => Palette.From("FFFFFF", "000000", "F5F5F5", "000000", "000000", "FFFFFF", "000000", "000080", "FFFFFF"),
            WhiteOnBlack => Palette.From("000000", "FFFFFF", "101010", "FFFFFF", "FFFFFF", "000000", "FFFFFF", "FFFF00", "000000"),
            BlackOnYellow => Palette.From("FFFF00", "000000", "FFF200", "000000", "000000", "FFFF00", "000000", "000080", "FFFFFF"),
            YellowOnNavy => Palette.From("000080", "FFFF00", "000080", "FFFF00", "FFFF00", "000080", "FFFF00", "FFFF00", "000000"),
            _ => CreatePalette(Windows)
        };
    }

    private sealed record Palette(
        Brush Window,
        Brush WindowText,
        Brush Control,
        Brush ControlText,
        Brush Border,
        Brush Menu,
        Brush MenuText,
        Brush Highlight,
        Brush HighlightText,
        Brush SecondaryText)
    {
        public static Palette From(
            string window,
            string windowText,
            string control,
            string controlText,
            string border,
            string menu,
            string menuText,
            string highlight,
            string highlightText) => new(
                Brush(window), Brush(windowText), Brush(control), Brush(controlText),
                Brush(border), Brush(menu), Brush(menuText), Brush(highlight),
                Brush(highlightText), Brush(windowText));

        private static SolidColorBrush Brush(string hex)
        {
            var color = (Color)ColorConverter.ConvertFromString("#" + hex)!;
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }
    }
}
