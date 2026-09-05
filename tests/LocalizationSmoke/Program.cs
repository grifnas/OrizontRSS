using System.Globalization;
using CititorRSS.Jaws;
using CititorRSS.Jaws.Localization;

var failures = new List<string>();
Check("version", "final product title", AppVersionInfo.ProductTitle, value => value == "Orizont RSS 1.5.3");
foreach (var cultureName in new[] { "en-US", "es-ES", "fr-FR", "de-DE", "pt-BR" })
{
    var culture = CultureInfo.GetCultureInfo(cultureName);
    CultureInfo.CurrentCulture = culture;
    CultureInfo.CurrentUICulture = culture;

    Check(cultureName, "menu", UiText.Translate("_Ajutor"), value => value != "_Ajutor");
    Check(cultureName, "brand", UiText.Translate("Ajutor Orizont RSS"), value => value.Contains("Orizont RSS", StringComparison.Ordinal));
    Check(cultureName, "formatted status", UiText.Format("Verificat: {0} articole disponibile.", 7), value => value.Contains('7') && !value.Contains("{0}", StringComparison.Ordinal));

    var feed = new Feed { Name = "Exemplu", Folder = "Știri", Articles = [new Article()] };
    Check(cultureName, "feed accessible name", feed.DisplayName, value => value.Contains("Exemplu", StringComparison.Ordinal) && value.Contains('1'));

    var article = new Article { Title = "Titlu", IsFavorite = true, ReadLater = true, Tags = ["test"], SourceName = "Sursa Exemplu", IncludeSourceInDisplay = true };
    Check(cultureName, "article accessible name", article.DisplayName, value => value.Contains("Titlu", StringComparison.Ordinal) && value.Contains("test", StringComparison.Ordinal));
    Check(cultureName, "article source accessible name", article.DisplayName, value => value.Contains("Sursa Exemplu", StringComparison.Ordinal));
    Check(cultureName, "article source visual detail", article.VisualDetails, value => value.Contains("Sursa Exemplu", StringComparison.Ordinal));
    Check(cultureName, "localized guide name", UserGuideLocator.FileNameFor(culture), value => value.EndsWith($".{culture.TwoLetterISOLanguageName}.html", StringComparison.OrdinalIgnoreCase));
}

if (failures.Count > 0)
{
    Console.Error.WriteLine(string.Join(Environment.NewLine, failures));
    return 1;
}

Console.WriteLine("Localization smoke test passed for en-US, es-ES, fr-FR, de-DE and pt-BR.");
return 0;

void Check(string culture, string test, string value, Func<string, bool> predicate)
{
    if (!predicate(value)) failures.Add($"{culture}: {test} failed; value: {value}");
}
