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

// Actualizarea feedurilor RSS: comenzile Actualizează, Oprește și Reîncearcă, apoi descărcarea și îmbinarea articolelor.
public partial class MainWindow
{
    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        if (_feed is null) { Say("Alege un feed pentru actualizare."); return; }
        await RefreshFeedsAsync([_feed]);
    }
    private async void RefreshAll_Click(object sender, RoutedEventArgs e)
    {
        var useNewsBlur = _settings.NewsBlurConnected && !string.IsNullOrWhiteSpace(_settings.EncryptedNewsBlurSession);
        var eligible = useNewsBlur ? _feeds.ToList() : _feeds.Where(feed => !feed.NeedsAttention).ToList();
        var skipped = _feeds.Count - eligible.Count;
        if (skipped > 0) Say(F("{0} feeduri necesită atenție și nu vor fi actualizate automat. Selectează un feed și alege Actualizează feedul pentru reîncercare.", skipped));
        await RefreshFeedsAsync(eligible);
    }
    private void StopRefresh_Click(object sender, RoutedEventArgs e)
    {
        if (!_isRefreshing || _refreshCancellation is null)
        {
            Say("Nu există nicio actualizare în curs.");
            return;
        }
        if (_refreshCancellation.IsCancellationRequested)
        {
            Say("Oprirea a fost deja cerută. Se întrerupe conexiunea curentă.");
            return;
        }
        var answer = MessageBox.Show(this,
            F("Au fost procesate {0} din {1} feeduri. Oprești actualizarea acum? Conexiunea curentă va fi întreruptă, iar datele deja primite vor fi păstrate.", _refreshProcessed, _refreshTotal),
            T("Oprește actualizarea"), MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
        if (answer != MessageBoxResult.Yes) return;
        _refreshCancellation.Cancel();
        StopRefreshMenuItem.IsEnabled = true;
        Say(F("S-a cerut oprirea actualizării. Procesate {0} din {1}; se întrerupe conexiunea curentă.", _refreshProcessed, _refreshTotal));
    }

    private async void RetryFailed_Click(object sender, RoutedEventArgs e)
    {
        var failed = _lastFailedFeeds.Where(_feeds.Contains).Distinct().ToList();
        if (failed.Count == 0)
        {
            RetryFailedMenuItem.IsEnabled = true;
            Say("Nu există feeduri cu eroare de reîncercat din ultima actualizare.");
            return;
        }
        await RefreshFeedsAsync(failed);
    }

    private async Task RefreshFeedsAsync(List<Feed> feedsToUpdate, bool fromAutoSync = false)
    {
        if (_closeInProgress || _isClosingAfterSave) return;
        if (feedsToUpdate.Count == 0) { Say("Nu există feeduri de actualizat."); return; }
        if (_newsBlurFeedSyncRunning) { Say(T("Așteaptă încheierea sincronizării feedurilor NewsBlur înainte de actualizarea articolelor.")); return; }
        if (_isRefreshing) { Say("O actualizare a feedurilor este deja în curs."); return; }

        var newsBlurCandidates = feedsToUpdate
            .Where(feed => !DemoFeedCatalog.IsLocal(feed))
            .Distinct()
            .ToList();
        var canUseNewsBlur = newsBlurCandidates.Count > 0 && _settings.NewsBlurConnected &&
            !string.IsNullOrWhiteSpace(_settings.EncryptedNewsBlurSession);
        if (!canUseNewsBlur)
        {
            await RefreshFeedsViaRssAsync(feedsToUpdate);
            return;
        }

        if (_newsBlurStateSyncRunning)
        {
            Say(T("Sincronizarea stărilor NewsBlur este deja în curs. Încearcă actualizarea după încheierea ei."));
            return;
        }

        _isRefreshing = true;
        _refreshCancellation = new CancellationTokenSource();
        _refreshCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _refreshProcessed = 0;
        _refreshTotal = newsBlurCandidates.Count;
        StopRefreshMenuItem.IsEnabled = true;
        RetryFailedMenuItem.IsEnabled = true;
        NewsBlurSyncOutcome outcome;
        try
        {
            outcome = await RunNewsBlurStateSyncAsync(
                fromAutoSync: fromAutoSync,
                refreshTargets: newsBlurCandidates,
                allowDuringRefresh: true,
                cancellationToken: _refreshCancellation.Token);
        }
        catch (Exception exception)
        {
            RuntimeLog.Write($"Preluarea din NewsBlur a eșuat: {RuntimeLog.Summarize(exception)}.");
            Say(F("Preluarea din NewsBlur a eșuat; se încearcă fluxurile RSS directe: {0}", Describe(exception)));
            outcome = new NewsBlurSyncOutcome(false, false, _refreshCancellation.IsCancellationRequested, newsBlurCandidates);
        }
        finally
        {
            _isRefreshing = false;
            StopRefreshMenuItem.IsEnabled = true;
            _refreshCancellation?.Dispose();
            _refreshCancellation = null;
            _refreshCompletion?.TrySetResult(true);
            _refreshCompletion = null;
        }

        if (_closeInProgress || _isClosingAfterSave) return;
        if (outcome.IsBusy) return;
        if (outcome.IsCancelled)
        {
            Say(F("Preluarea din NewsBlur a fost oprită după verificarea a {0} din {1} feeduri. Datele deja primite au fost păstrate.", _refreshProcessed, _refreshTotal));
            return;
        }

        var fallbackFeeds = outcome.IsReady ? outcome.RssFallbackFeeds.ToList() : newsBlurCandidates;
        if (fallbackFeeds.Count > 0)
        {
            var fallbackSet = fallbackFeeds.ToHashSet();
            fallbackFeeds.AddRange(feedsToUpdate.Where(feed => DemoFeedCatalog.IsLocal(feed) && !fallbackSet.Contains(feed)));
            Say(F("NewsBlur nu a putut furniza {0} feeduri; se încearcă RSS direct doar pentru acestea.", outcome.IsReady ? outcome.RssFallbackFeeds.Count : newsBlurCandidates.Count));
            await RefreshFeedsViaRssAsync(fallbackFeeds, outcome.IsReady ? outcome.ImportedArticles : 0);
        }
        else if (feedsToUpdate.Any(DemoFeedCatalog.IsLocal))
        {
            Say(T("Feedurile locale demonstrative nu se actualizează de pe internet; articolele sunt deja disponibile."));
            PlayNewsBlurRefreshAlert(outcome);
        }
        else
        {
            _lastFailedFeeds = [];
            RetryFailedMenuItem.IsEnabled = true;
            PlayNewsBlurRefreshAlert(outcome);
        }

        if (outcome.IsReady && !string.IsNullOrWhiteSpace(outcome.CompletionSummary))
            Say(outcome.CompletionSummary);
    }

    private void PlayNewsBlurRefreshAlert(NewsBlurSyncOutcome outcome)
    {
        if (!outcome.IsReady) return;
        SoundAlertService.RefreshFinished(
            _settings.SoundAlertsEnabled,
            _settings.SoundAlertOnSuccess,
            _settings.SoundAlertOnNewArticles,
            _settings.SoundAlertOnErrors,
            hasErrors: false,
            newArticleCount: outcome.ImportedArticles);
    }

    private async Task RefreshFeedsViaRssAsync(List<Feed> feedsToUpdate, int previouslyImportedArticles = 0)
    {
        if (_closeInProgress || _isClosingAfterSave) return;
        if (feedsToUpdate.Count == 0) { Say("Nu există feeduri de actualizat."); return; }
        var skippedLocalDemoFeeds = feedsToUpdate.Count(DemoFeedCatalog.IsLocal);
        feedsToUpdate = feedsToUpdate.Where(feed => !DemoFeedCatalog.IsLocal(feed)).ToList();
        if (feedsToUpdate.Count == 0)
        {
            Say(T("Feedurile locale nu se actualizează de pe internet; articolele demonstrative sunt deja disponibile."));
            return;
        }
        if (_isRefreshing) { Say("O actualizare a feedurilor este deja în curs."); return; }
        _isRefreshing = true;
        _refreshCancellation = new CancellationTokenSource();
        _refreshCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _refreshProcessed = 0;
        _refreshTotal = feedsToUpdate.Count;
        StopRefreshMenuItem.IsEnabled = true;
        RetryFailedMenuItem.IsEnabled = true;
        var articlePosition = Articles.Items.Count > 0 ? CaptureArticlePosition() : null;
        var timer = Stopwatch.StartNew();
        try
        {
            var selected = _feed;
            var successes = 0;
            var failures = 0;
            var articlesAdded = 0;
            var articlesIgnoredByRetention = 0;
            var expiredArticlesRemoved = 0;
            var failureNames = new List<string>();
            var failedFeeds = new List<Feed>();
            var cancelled = false;
            Say(F("Se actualizează {0} feeduri. Procesul poate dura câteva minute. Escape poate opri procesul și întrerupe conexiunea curentă.", feedsToUpdate.Count));
            // Descărcările rulează în paralel; fiecare feed este prelucrat aici, pe firul interfeței,
            // în ordinea în care se termină. Versiunea reținută se trimite numai dacă feedul are articole.
            var downloadRequests = feedsToUpdate.ToDictionary(
                feed => feed,
                feed => (Url: feed.Url, ETag: feed.Articles.Count > 0 ? feed.RssETag : null, LastModified: feed.Articles.Count > 0 ? feed.RssLastModified : null));
            var pendingDownloads = FeedDownloadScheduler.Start(
                feedsToUpdate,
                (feed, token) =>
                {
                    var request = downloadRequests[feed];
                    return _rss.LoadIfChangedAsync(request.Url, request.ETag, request.LastModified, token);
                },
                _refreshCancellation.Token);
            while (pendingDownloads.Count > 0)
            {
                if (_refreshCancellation.IsCancellationRequested) { cancelled = true; break; }
                var completedDownload = await Task.WhenAny(pendingDownloads);
                pendingDownloads.Remove(completedDownload);
                var download = await completedDownload;
                var feed = download.Feed;
                var failed = false;
                var feedCancelled = false;
                try
                {
                    download.Error?.Throw();
                    var downloadResult = download.Result!;
                    if (downloadResult.NotModified)
                    {
                        // Serverul a confirmat că fluxul nu s-a schimbat: articolele rămân, feedul este la zi.
                        feed.ConsecutiveFailures = 0;
                        feed.LastError = null;
                        feed.LastSuccessfulUpdate = DateTimeOffset.Now;
                        successes++;
                        continue;
                    }
                    var downloaded = DuplicateCleaner.DeduplicateArticles(downloadResult.Articles, out _);
                    ArticleRules.SeedExisting(feed);
                    // Fluxul RSS conține doar ultimele articole; cele ieșite din flux rămân local.
                    DateTimeOffset? retentionCutoff = _settings.AutoCleanupEnabled ? DateTimeOffset.Now.AddDays(-_settings.RetentionDays) : null;
                    var merge = ArticleRefreshMerge.Merge(feed.Articles, downloaded, retentionCutoff);
                    articlesIgnoredByRetention += merge.IgnoredByRetention;
                    var accepted = merge.Articles;
                    foreach (var article in accepted) ArticleRules.ApplyIncoming(_settings.ArticleRules, feed, article);
                    var newlyReceived = merge.NewlyReceived;
                    articlesAdded += newlyReceived;
                    feed.Articles = accepted;
                    // Versiunea serverului se reține numai după îmbinarea reușită a articolelor.
                    feed.RssETag = downloadResult.ETag;
                    feed.RssLastModified = downloadResult.LastModified;
                    feed.ConsecutiveFailures = 0;
                    feed.LastError = null;
                    feed.LastSuccessfulUpdate = DateTimeOffset.Now;
                    if (newlyReceived > 0) feed.LastArticleReceivedOn = DateTimeOffset.Now;
                    successes++;
                }
                catch (OperationCanceledException) when (_refreshCancellation.IsCancellationRequested)
                {
                    feedCancelled = true;
                    cancelled = true;
                }
                catch (Exception exception)
                {
                    failed = true;
                    failures++;
                    failureNames.Add(feed.Name);
                    failedFeeds.Add(feed);
                    feed.ConsecutiveFailures++;
                    feed.LastError = Describe(exception);
                    RuntimeLog.Write($"Feed cu eroare: {feed.Name}: {RuntimeLog.Summarize(exception)}.");
                }
                finally
                {
                    if (!feedCancelled)
                    {
                        _refreshProcessed++;
                        var progress = F("Actualizare: {0} din {1} feeduri procesate; {2} reușite, {3} cu eroare, {4} articole noi.", _refreshProcessed, _refreshTotal, successes, failures, articlesAdded);
                        SetProgressStatus(progress, failed || _refreshProcessed % 10 == 0 || _refreshProcessed == _refreshTotal);
                    }
                }
                if (feedCancelled) break;
            }
            if (_refreshCancellation.IsCancellationRequested && _refreshProcessed < _refreshTotal) cancelled = true;
            _lastFailedFeeds = failedFeeds;
            RetryFailedMenuItem.IsEnabled = true;
            if (_settings.AutoCleanupEnabled)
                expiredArticlesRemoved = ArticleRetention.RemoveExpired(_feeds, DateTimeOffset.Now.AddDays(-_settings.RetentionDays));
            await _store.SaveAsync(_feeds);
            RefreshFeedList(selected);
            RefreshTagFilter();
            if (selected is not null)
            {
                _feed = selected;
                RefreshArticleList(selectFirstWhenNoMatch: false);
            }
            else if (_folderAggregateView)
            {
                ShowFolderAggregate(selectFirstWhenNoMatch: false);
            }
            if (articlePosition is not null) await RestoreCapturedPositionAsync(articlePosition);
            var needsAttention = _feeds.Count(feed => feed.NeedsAttention);
            var elapsed = timer.Elapsed;
            var duration = elapsed.TotalMinutes >= 1
                ? F("{0} minute și {1} secunde", (int)elapsed.TotalMinutes, elapsed.Seconds)
                : F("{0} secunde", Math.Max(1, (int)elapsed.TotalSeconds));
            var result = cancelled
                ? F("Actualizare oprită. {0} din {1} feeduri procesate; {2} reușite, {3} cu eroare, {4} articole noi. Durată: {5}.", _refreshProcessed, _refreshTotal, successes, failures, articlesAdded, duration)
                : F("Actualizare încheiată. {0} feeduri reușite, {1} feeduri cu eroare, {2} articole noi. Durată: {3}.", successes, failures, articlesAdded, duration);
            if (_settings.AutoCleanupEnabled && articlesIgnoredByRetention > 0) result += F(" {0} articole obișnuite mai vechi de {1} zile au fost ignorate conform regulii de păstrare.", articlesIgnoredByRetention, _settings.RetentionDays);
            if (expiredArticlesRemoved > 0) result += F(" {0} articole obișnuite expirate au fost șterse local; favoritele și articolele pentru mai târziu au fost păstrate.", expiredArticlesRemoved);
            if (needsAttention > 0)
            {
                var failedAttentionCount = _feeds.Count(feed => feed.ConsecutiveFailures >= 3);
                var silentFeeds = _feeds.Count(feed => feed.HasThreeMonthSilence && feed.ConsecutiveFailures < 3);
                result += F(" {0} feeduri necesită atenție: {1} după 3 erori consecutive și {2} fără articole de peste 3 luni.", needsAttention, failedAttentionCount, silentFeeds);
            }
            if (failureNames.Count > 0) result += F(" Primele feeduri cu eroare: {0}.", string.Join(", ", failureNames.Take(3)));
            if (failureNames.Count > 0) result += UiText.Translate(" Comanda Reîncearcă feedurile cu eroare este disponibilă în meniul Feeduri.");
            if (skippedLocalDemoFeeds > 0) result += " " + T("Feedurile locale nu se actualizează de pe internet; articolele demonstrative sunt deja disponibile.");
            Say(result);
            RuntimeLog.Write($"Actualizare RSS {(cancelled ? "oprită" : "încheiată")}: {_refreshProcessed} din {_refreshTotal} feeduri, {successes} reușite, {failures} cu eroare, {articlesAdded} articole noi, {(int)elapsed.TotalSeconds} s.");
            if (!cancelled) SoundAlertService.RefreshFinished(
                _settings.SoundAlertsEnabled,
                _settings.SoundAlertOnSuccess,
                _settings.SoundAlertOnNewArticles,
                _settings.SoundAlertOnErrors,
                failures > 0,
                articlesAdded + previouslyImportedArticles);
        }
        catch (Exception exception)
        {
            RuntimeLog.Write($"Actualizarea RSS nu a putut fi finalizată sau salvată: {RuntimeLog.Summarize(exception)}.");
            Say(F("Actualizarea nu a putut fi finalizată sau salvată: {0} Datele deja salvate anterior nu au fost șterse.", Describe(exception)));
            SoundAlertService.RefreshFinished(
                _settings.SoundAlertsEnabled,
                _settings.SoundAlertOnSuccess,
                _settings.SoundAlertOnNewArticles,
                _settings.SoundAlertOnErrors,
                hasErrors: true,
                newArticleCount: 0);
        }
        finally
        {
            timer.Stop();
            _isRefreshing = false;
            StopRefreshMenuItem.IsEnabled = true;
            _refreshCancellation?.Dispose();
            _refreshCancellation = null;
            _refreshCompletion?.TrySetResult(true);
            _refreshCompletion = null;
        }
    }
}
