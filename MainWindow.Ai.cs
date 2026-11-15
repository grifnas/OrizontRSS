using System.Net.Http;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using CititorRSS.Jaws.Localization;
using CititorRSS.Jaws.Services;
using CititorRSS.Jaws.Services.Update;
using CititorRSS.Jaws.Services.Content;
using CititorRSS.Jaws.Views;

namespace CititorRSS.Jaws;

// Funcțiile de inteligență artificială: setările, comenzile Rezumat, Traducere, Explică, Ideile principale și Discută, notele AI și cererea către furnizor.
public partial class MainWindow
{
    private async void AiSettings_Click(object sender, RoutedEventArgs e)
    {
        await OpenSettingsAsync(SettingsWindow.SettingsSection.Ai);
    }

    private async void AiSummarize_Click(object sender, RoutedEventArgs e) => await AskAiAsync(T("Rezumat"), T("Rezuma articolul în limba interfeței. Evidențiază faptele, contextul și incertitudinile."));
    private async void AiTranslate_Click(object sender, RoutedEventArgs e) => await AskAiAsync(T("Traducere"), T("Tradu articolul complet în limba interfeței. Nu adăuga comentarii, păstrează structura paragrafului."), isTranslation: true);
    private async void AiExplain_Click(object sender, RoutedEventArgs e) => await AskAiAsync(T("Explicație"), T("Explică articolul în limba interfeței, simplu și clar, pentru un cititor nespecialist."));
    private async void AiKeyPoints_Click(object sender, RoutedEventArgs e) => await AskAiAsync(T("Ideile principale"), T("Extrage ideile principale ale articolului într-o listă clară, în limba interfeței."));
    private async void AiDiscuss_Click(object sender, RoutedEventArgs e)
    {
        if (_article is null) { Say("Alege mai întâi un articol."); return; }
        var dialog = new AiQuestionWindow(AiProviderIds.DisplayName(_settings.AiDefaultProvider)) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        await AskAiAsync(T("Răspuns AI"), dialog.UserQuestion);
    }
    private void ShowAiNotes_Click(object sender, RoutedEventArgs e)
    {
        if (_article is null) { Say("Alege mai întâi un articol."); return; }
        _article.AiNotes ??= [];
        if (_article.AiNotes.Count == 0) { Say("Acest articol nu are încă notițe AI salvate."); return; }
        new AiNotesWindow(_article) { Owner = this }.ShowDialog();
    }

    private async Task AskAiAsync(string title, string request, bool isTranslation = false)
    {
        if (_article is null) { ShowAiProblem(T("Alege mai întâi un articol."), MessageBoxImage.Information); return; }
        var provider = AiProviderIds.Normalize(_settings.AiDefaultProvider);
        var providerName = AiProviderIds.DisplayName(provider);
        var enabled = provider switch
        {
            AiProviderIds.OpenAI => _settings.OpenAiEnabled,
            AiProviderIds.Mistral => _settings.MistralEnabled,
            AiProviderIds.DeepSeek => _settings.DeepSeekEnabled,
            _ => _settings.GeminiEnabled
        };
        if (!enabled)
        {
            ShowAiProblem(F("Furnizorul AI implicit nu este activat. Deschide Setări Inteligență artificială și verifică cheia API pentru {0}.", providerName), MessageBoxImage.Warning);
            return;
        }
        string key;
        var encryptedKey = provider switch
        {
            AiProviderIds.OpenAI => _settings.EncryptedOpenAiKey,
            AiProviderIds.Mistral => _settings.EncryptedMistralKey,
            AiProviderIds.DeepSeek => _settings.EncryptedDeepSeekKey,
            _ => _settings.EncryptedGeminiKey
        };
        try { key = SecretProtector.Unprotect(encryptedKey); }
        catch { ShowAiProblem(F("Cheia API pentru {0} nu poate fi citită pentru acest cont Windows. Introdu cheia din nou în Setări Inteligență artificială.", providerName), MessageBoxImage.Warning); return; }
        if (string.IsNullOrWhiteSpace(key)) { ShowAiProblem(F("Nu există o cheie API pentru {0}. Deschide Setări Inteligență artificială.", providerName), MessageBoxImage.Warning); return; }
        var source = !string.IsNullOrWhiteSpace(Reader.Text) ? Reader.Text : !string.IsNullOrWhiteSpace(_article.FullContent) ? _article.FullContent : _article.Content;
        if (string.IsNullOrWhiteSpace(source)) { ShowAiProblem(T("Articolul nu conține text pentru furnizorul AI selectat. Încearcă să aduci textul complet."), MessageBoxImage.Warning); return; }
        var article = _article;
        var model = provider switch
        {
            AiProviderIds.Mistral => _settings.MistralModel,
            AiProviderIds.DeepSeek => _settings.DeepSeekModel,
            _ => _settings.OpenAiModel
        };
        if (_aiCancellation is not null) { Say(T("O cerere AI este deja în curs. Apasă Escape pentru a o opri.")); return; }
        Say(F("AI: {0} pregătește {1} pentru: {2}. Așteaptă sau apasă Escape pentru a opri.", providerName, title.ToLower(CultureInfo.CurrentCulture), article.Title));
        var cancellation = new CancellationTokenSource();
        _aiCancellation = cancellation;
        try
        {
            var prompt = $"Limba interfeței: {CultureInfo.CurrentUICulture.NativeName}\nTitlu articol: {article.Title}\nSursă: {article.Link}\n\nCerere: {request}\n\nArticol:\n{source}";
            string response;
            try
            {
                response = await new AiProviderService().GenerateAsync(provider, key, model, _settings.AiInstructions, prompt, cancellation.Token);
            }
            finally
            {
                _aiCancellation = null;
                cancellation.Dispose();
            }
            SoundAlertService.Play(SoundEvent.AiReady);
            var initialPrompt = prompt;
            new AiResponseWindow(
                title,
                response,
                article.Title,
                article.Link,
                followUp => new AiProviderService().GenerateAsync(provider, key, model, _settings.AiInstructions, $"{initialPrompt}\n\n{followUp}"),
                async noteContent =>
                {
                    article.AiNotes ??= [];
                    article.AiNotes.Add(new AiNote { Title = title, Content = noteContent });
                    await _store.SaveAsync(_feeds);
                },
                _speech!,
                providerName,
                isTranslation,
                source,
                isTranslation ? CultureInfo.CurrentUICulture.NativeName : null) { Owner = this }.ShowDialog();
            Say(T("Răspunsul AI a fost închis. Ești înapoi la lista de articole."));
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { Say(T("Cererea AI a fost oprită. Nu s-a afișat niciun răspuns.")); }
        catch (TaskCanceledException) { ShowAiProblem(F("{0} nu a răspuns în 45 de secunde. Încearcă din nou mai târziu.", providerName), MessageBoxImage.Error); }
        catch (HttpRequestException) { ShowAiProblem(F("Nu s-a putut ajunge la {0}. Verifică internetul, firewall-ul sau proxy-ul.", providerName), MessageBoxImage.Error); }
        catch (Exception exception) { ShowAiProblem(F("Furnizorul {0} nu a putut executa comanda {1}. Detalii: {2}", providerName, title, exception.Message), MessageBoxImage.Error); }
    }
    private void ShowAiProblem(string message, MessageBoxImage icon)
    {
        Say(F("AI: {0}", message.Replace(Environment.NewLine, " ")));
        MessageBox.Show(this, T(message), T("AI — stare comandă"), MessageBoxButton.OK, icon);
    }
}
