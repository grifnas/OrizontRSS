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

// Sincronizarea NewsBlur: autentificare, sincronizarea feedurilor, a stărilor (citit, salvat) și a metadatelor, plus sincronizarea automată.
public partial class MainWindow
{
    private async void NewsBlurAuth_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new NewsBlurAuthWindow(_settings) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        await _store.SaveSettingsAsync(_settings);
        ConfigureNewsBlurAutoSync();
        Say(_settings.NewsBlurConnected
            ? F("NewsBlur este conectat pentru {0}.", _settings.NewsBlurUsername ?? T("utilizatorul curent"))
            : T("Contul NewsBlur a fost deconectat."));
    }
    private async void NewsBlurSync_Click(object sender, RoutedEventArgs e)
    {
        if (RejectDataChangeDuringRefresh(T("sincronizarea abonamentelor NewsBlur"))) return;
        if (!_settings.NewsBlurConnected || string.IsNullOrWhiteSpace(_settings.EncryptedNewsBlurSession))
        {
            Say(T("NewsBlur nu este conectat. Deschide Feeduri, Servicii externe, Autentificare NewsBlur."));
            return;
        }
        string sessionId;
        try { sessionId = SecretProtector.Unprotect(_settings.EncryptedNewsBlurSession); }
        catch
        {
            Say(T("Sesiunea NewsBlur nu poate fi citită pentru acest cont Windows. Autentifică-te din nou."));
            return;
        }

        Say(T("Se sincronizează abonamentele NewsBlur. Așteaptă."));
        IReadOnlyList<NewsBlurSubscription> remote;
        try { remote = await new NewsBlurConnection().GetSubscriptionsAsync(sessionId); }
        catch (Exception exception)
        {
            Say(F("Sincronizarea NewsBlur a eșuat: {0}", Describe(exception)));
            return;
        }

        var remoteSubscriptions = remote
            .Where(subscription => Uri.TryCreate(subscription.Url, UriKind.Absolute, out _))
            .GroupBy(subscription => DuplicateCleaner.FeedKey(subscription.Url), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
        var existingByAddress = _feeds
            .Where(feed => !string.IsNullOrWhiteSpace(DuplicateCleaner.FeedKey(feed.Url)))
            .GroupBy(feed => DuplicateCleaner.FeedKey(feed.Url), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var pendingDeletionAddresses = _settings.NewsBlurPendingFeedDeletions
            .Select(pending => DuplicateCleaner.FeedKey(pending.Url))
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var additions = remoteSubscriptions
            .Where(subscription => !existingByAddress.ContainsKey(DuplicateCleaner.FeedKey(subscription.Url)) &&
                !pendingDeletionAddresses.Contains(DuplicateCleaner.FeedKey(subscription.Url)))
            .ToList();
        var updates = remoteSubscriptions
            .Where(subscription => existingByAddress.TryGetValue(DuplicateCleaner.FeedKey(subscription.Url), out var local) &&
                (!string.Equals(local.Name, subscription.Name, StringComparison.CurrentCulture) || !string.Equals(local.Folder, subscription.Folder, StringComparison.CurrentCultureIgnoreCase)))
            .ToList();
        var existingFolders = _feeds.Select(feed => feed.Folder).Where(folder => !string.IsNullOrWhiteSpace(folder)).ToHashSet(StringComparer.CurrentCultureIgnoreCase);
        var newFolders = remoteSubscriptions.Select(subscription => subscription.Folder).Where(folder => !existingFolders.Contains(folder)).Distinct(StringComparer.CurrentCultureIgnoreCase).Count();

        if (additions.Count == 0 && updates.Count == 0)
        {
            Say(pendingDeletionAddresses.Count > 0
                ? T("Există dezabonări NewsBlur în așteptare. Folosește comanda Sincronizează feedurile și folderele NewsBlur pentru a le confirma.")
                : T("Abonamentele locale sunt deja sincronizate cu NewsBlur. Nu s-a modificat nimic."));
            return;
        }

        var summary = F("NewsBlur a raportat {0} abonamente. Vor fi adăugate {1} feeduri noi, actualizate {2} feeduri și create {3} foldere noi. Feedurile locale care lipsesc din NewsBlur vor fi păstrate. Continui?", remoteSubscriptions.Count, additions.Count, updates.Count, newFolders);
        Say(F("Au fost găsite {0} feeduri noi și {1} feeduri de actualizat în NewsBlur. Se așteaptă confirmarea.", additions.Count, updates.Count));
        if (ShowNewsBlurDecision(
                T("Confirmă sincronizarea NewsBlur"),
                summary,
                new NewsBlurDecisionOption(T("Anulează"), MessageBoxResult.No, IsDefault: true, IsCancel: true),
                new NewsBlurDecisionOption(T("Continuă sincronizarea"), MessageBoxResult.Yes)) != MessageBoxResult.Yes)
        {
            Say(T("Sincronizarea a fost anulată. Nu s-a modificat nimic."));
            return;
        }

        foreach (var subscription in additions)
            _feeds.Add(new Feed { Name = subscription.Name, Url = subscription.Url, Folder = subscription.Folder });
        foreach (var subscription in updates)
        {
            var local = existingByAddress[DuplicateCleaner.FeedKey(subscription.Url)];
            if (!string.IsNullOrWhiteSpace(subscription.Name)) local.Name = subscription.Name;
            local.Folder = subscription.Folder;
        }

        try
        {
            await _store.SaveAsync(_feeds);
        }
        catch (Exception exception)
        {
            Say(F("Feedurile nu au putut fi salvate: {0}", Describe(exception)));
            return;
        }
        RefreshFeedList(_feed);
        RefreshTagFilter();
        RefreshArticleList(_article, selectFirstWhenNoMatch: false);
        Say(F("Sincronizare NewsBlur încheiată. Au fost adăugate {0} feeduri și actualizate {1}; feedurile locale au fost păstrate.", additions.Count, updates.Count));
        SoundAlertService.Play(SoundEvent.SyncDone);
    }

    private async void NewsBlurFeedSync_Click(object sender, RoutedEventArgs e)
    {
        await RunNewsBlurFeedSyncWithGuardAsync(automatic: false);
    }

    private async Task RunNewsBlurFeedSyncWithGuardAsync(bool automatic)
    {
        if (_closeInProgress || _isClosingAfterSave) return;
        if (_newsBlurFeedSyncRunning || (automatic && (_isRefreshing || _newsBlurStateSyncRunning)))
        {
            if (!automatic) Say(T("Sincronizarea feedurilor și folderelor NewsBlur este deja în curs."));
            return;
        }
        _newsBlurFeedSyncRunning = true;
        var cancellation = new CancellationTokenSource();
        _newsBlurFeedSyncCancellation = cancellation;
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _newsBlurFeedSyncCompletion = completion;
        try { await RunNewsBlurFeedSyncAsync(automatic, cancellation.Token); }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            try
            {
                await _store.SaveAsync(_feeds);
                await _store.SaveSettingsAsync(_settings);
                RefreshFeedList(_feed);
                RefreshTagFilter();
                RefreshArticleList(_article, selectFirstWhenNoMatch: false);
                Say(T("Sincronizarea s-a oprit. Rezultatele locale au fost salvate; modificările deja confirmate în NewsBlur vor fi verificate la următoarea sincronizare."));
            }
            catch (Exception exception)
            {
                Say(F("Feedurile nu au putut fi salvate: {0}", Describe(exception)));
                throw;
            }
        }
        catch (Exception exception) { Say(F("Sincronizarea feedurilor NewsBlur a eșuat: {0}", Describe(exception))); }
        finally
        {
            _newsBlurFeedSyncRunning = false;
            completion.TrySetResult(true);
            if (ReferenceEquals(_newsBlurFeedSyncCompletion, completion)) _newsBlurFeedSyncCompletion = null;
            if (ReferenceEquals(_newsBlurFeedSyncCancellation, cancellation)) _newsBlurFeedSyncCancellation = null;
            cancellation.Dispose();
        }
    }

    private async Task RunNewsBlurFeedSyncAsync(bool automatic, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (RejectDataChangeDuringRefresh(T("sincronizarea feedurilor și folderelor NewsBlur"), allowDuringNewsBlurFeedSync: true)) return;
        if (!_settings.NewsBlurConnected || string.IsNullOrWhiteSpace(_settings.EncryptedNewsBlurSession))
        {
            Say(T("NewsBlur nu este conectat. Deschide Feeduri, Servicii externe, Autentificare NewsBlur."));
            return;
        }

        string sessionId;
        try { sessionId = SecretProtector.Unprotect(_settings.EncryptedNewsBlurSession); }
        catch
        {
            Say(T("Sesiunea NewsBlur nu poate fi citită pentru acest cont Windows. Autentifică-te din nou."));
            return;
        }

        Say(T("Se sincronizează feedurile și folderele NewsBlur. Așteaptă."));
        var connection = new NewsBlurConnection();
        IReadOnlyList<NewsBlurSubscription> remoteSubscriptions;
        IReadOnlyList<NewsBlurFeedInfo> remoteFeeds;
        try
        {
            remoteSubscriptions = await connection.GetSubscriptionsAsync(sessionId, cancellationToken);
            remoteFeeds = await connection.GetFeedIndexAsync(sessionId, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            Say(F("Sincronizarea feedurilor NewsBlur a eșuat: {0}", Describe(exception)));
            return;
        }

        if (!NewsBlurFeedSnapshotPolicy.IsComplete(remoteSubscriptions, remoteFeeds, out var snapshotProblem))
        {
            var snapshotMessage = F("NewsBlur a returnat liste incomplete sau neconcordante ({0}); nu s-a modificat nimic. Reîncearcă sincronizarea mai târziu.", snapshotProblem);
            Say(snapshotMessage);
            if (!automatic && snapshotProblem.Contains("repetate", StringComparison.OrdinalIgnoreCase))
            {
                var remoteDuplicates = DescribeRemoteDuplicateFeeds(remoteSubscriptions, remoteFeeds);
                ShowNewsBlurDecision(
                    T("Feeduri duplicate în răspunsul NewsBlur"),
                    remoteDuplicates.Count == 0
                        ? F("{0}\n\nVerificarea locală nu a putut identifica adresele duplicate. Nicio modificare nu a fost făcută.", snapshotMessage)
                        : F("{0}\n\nFeeduri duplicate raportate de NewsBlur:\n{1}\n\nAceste duplicate sunt în contul NewsBlur. Curățarea locală din Orizont nu le va șterge de pe server.", snapshotMessage, string.Join("\n\n", remoteDuplicates)),
                    new NewsBlurDecisionOption(T("Închide"), MessageBoxResult.Cancel, IsDefault: true, IsCancel: true));
            }
            return;
        }

        var accountUsername = _settings.NewsBlurUsername?.Trim();
        if (string.IsNullOrWhiteSpace(accountUsername))
        {
            Say(T("Numele contului NewsBlur nu este disponibil. Autentifică-te din nou înainte de sincronizare."));
            return;
        }
        if (NewsBlurBootstrapPolicy.RequiresInitialImport(accountUsername, _settings.NewsBlurBootstrapCompletedUsername))
        {
            var existingDeviceLink = NewsBlurBootstrapPolicy.HasExistingRemoteLink(_feeds, remoteFeeds);
            if (existingDeviceLink)
            {
                var previousBootstrapAccount = _settings.NewsBlurBootstrapCompletedUsername;
                _settings.NewsBlurBootstrapCompletedUsername = accountUsername;
                try { await _store.SaveSettingsAsync(_settings); }
                catch (Exception exception)
                {
                    _settings.NewsBlurBootstrapCompletedUsername = previousBootstrapAccount;
                    Say(F("Asocierea NewsBlur existentă a fost găsită, dar marcajul local nu s-a putut salva. Sincronizarea bidirecțională a fost oprită pentru siguranță. Detalii: {0}", Describe(exception)));
                    return;
                }
                Say(T("S-a recunoscut o asociere NewsBlur existentă; se continuă cu sincronizarea bidirecțională."));
            }
            else
            {
                await RunNewsBlurInitialBootstrapAsync(accountUsername, remoteSubscriptions, remoteFeeds, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return;
            }
        }

        var remoteByAddress = remoteSubscriptions
            .Where(item => Uri.TryCreate(item.Url, UriKind.Absolute, out _))
            .GroupBy(item => NewsBlurFeedSnapshotPolicy.AddressKey(item.Url), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var remoteInfoByAddress = remoteFeeds
            .Where(item => Uri.TryCreate(item.Url, UriKind.Absolute, out _))
            .GroupBy(item => NewsBlurFeedSnapshotPolicy.AddressKey(item.Url), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var remoteInfoById = remoteFeeds
            .Where(item => !string.IsNullOrWhiteSpace(item.FeedId))
            .GroupBy(item => item.FeedId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var localFeeds = _feeds.Where(feed => NewsBlurBootstrapPolicy.IsSyncEligible(feed) && Uri.TryCreate(feed.Url, UriKind.Absolute, out _)).ToList();
        var remoteIdentityMismatch = localFeeds.FirstOrDefault(feed =>
            !string.IsNullOrWhiteSpace(feed.NewsBlurFeedId) &&
            remoteInfoById.TryGetValue(feed.NewsBlurFeedId, out var remote) &&
            !string.Equals(NewsBlurFeedSnapshotPolicy.AddressKey(feed.Url), NewsBlurFeedSnapshotPolicy.AddressKey(remote.Url), StringComparison.OrdinalIgnoreCase));
        if (remoteIdentityMismatch is not null)
        {
            Say(F("Identitatea feedului {0} nu corespunde adresei asociate din NewsBlur; nu s-a modificat nimic pentru a evita o dezabonare sau o dublură incorectă.", remoteIdentityMismatch.Name));
            return;
        }
        var duplicateLocalFeeds = NewsBlurFeedSnapshotPolicy.FindDuplicateLocalFeeds(localFeeds);
        if (duplicateLocalFeeds.Count > 0)
        {
            var duplicateDetails = string.Join("\n\n", duplicateLocalFeeds.Select((group, index) =>
                F("Grupul {0}: adresă normalizată {1}\n{2}", index + 1,
                    NewsBlurFeedSnapshotPolicy.AddressKey(group[0].Url),
                    string.Join("\n", group.Select(feed => F("• {0}; folder: {1}; adresă: {2}", feed.Name, feed.Folder, feed.Url))))));
            Say(T("Există adrese de feed duplicate în lista locală; sincronizarea a fost oprită fără modificări."));
            if (!automatic)
            {
                var duplicateDialog = new NewsBlurDecisionWindow(
                    T("Feeduri duplicate în lista locală"),
                    F("Sincronizarea s-a oprit fără să modifice datele. Au fost găsite {0} grupuri de feeduri cu aceeași adresă. Consultă lista de mai jos. Pentru curățare, deschide instrumentul existent; acesta va cere o confirmare separată. După verificare, pornește din nou sincronizarea.", duplicateLocalFeeds.Count) + "\n\n" + duplicateDetails,
                    [
                        new NewsBlurDecisionOption(T("Anulează"), MessageBoxResult.Cancel, IsDefault: true, IsCancel: true),
                        new NewsBlurDecisionOption(T("Deschide curățarea duplicatelor"), MessageBoxResult.Yes, OpensDuplicateCleanup: true)
                    ]) { Owner = this };
                duplicateDialog.ShowDialog();
                if (duplicateDialog.OpensDuplicateCleanup)
                {
                    var cleaned = await CleanDuplicatesAsync(
                        allowDuringNewsBlurFeedSync: true,
                        synchronizeNewsBlurAfterCleanup: false);
                    if (cleaned) await RunNewsBlurFeedSyncAsync(automatic, cancellationToken);
                }
            }
            return;
        }
        var localDemoAddresses = _feeds.Where(feed => !NewsBlurBootstrapPolicy.IsSyncEligible(feed))
            .Where(feed => Uri.TryCreate(feed.Url, UriKind.Absolute, out _))
            .Select(feed => NewsBlurFeedSnapshotPolicy.AddressKey(feed.Url))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var localByAddress = localFeeds
            .GroupBy(feed => NewsBlurFeedSnapshotPolicy.AddressKey(feed.Url), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var pendingDeletes = new List<(NewsBlurPendingFeedDeletion Pending, NewsBlurFeedInfo Remote)>();
        var pendingAlreadyResolved = new List<NewsBlurPendingFeedDeletion>();
        foreach (var pending in _settings.NewsBlurPendingFeedDeletions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var key = NewsBlurFeedSnapshotPolicy.AddressKey(pending.Url);
            if (localByAddress.ContainsKey(key))
            {
                pendingAlreadyResolved.Add(pending);
                continue;
            }
            if (!string.IsNullOrWhiteSpace(pending.FeedId) && remoteInfoById.TryGetValue(pending.FeedId, out var remoteInfo))
                pendingDeletes.Add((pending, remoteInfo));
            else if (remoteInfoByAddress.TryGetValue(key, out remoteInfo))
                pendingDeletes.Add((pending, remoteInfo));
            else
                pendingAlreadyResolved.Add(pending);
        }

        var remoteMissingKnown = localByAddress
            .Where(pair => NewsBlurFeedSnapshotPolicy.IsRemoteDeletion(pair.Value, remoteFeeds))
            .Select(pair => pair.Value)
            .ToList();
        var remoteMissingSet = remoteMissingKnown.ToHashSet();
        var localOnly = localByAddress
            .Where(pair => !remoteByAddress.ContainsKey(pair.Key) && !remoteMissingSet.Contains(pair.Value))
            .Select(pair => pair.Value)
            .ToList();
        var pendingAddresses = _settings.NewsBlurPendingFeedDeletions
            .Select(item => NewsBlurFeedSnapshotPolicy.AddressKey(item.Url))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var remoteOnly = remoteByAddress
            .Where(pair => !localByAddress.ContainsKey(pair.Key) && !localDemoAddresses.Contains(pair.Key) && !pendingAddresses.Contains(pair.Key))
            .Select(pair => pair.Value)
            .ToList();
        var remoteMetadataChanges = remoteByAddress.Where(pair => localByAddress.TryGetValue(pair.Key, out var local) &&
            (!string.Equals(local.Name, pair.Value.Name, StringComparison.CurrentCulture) ||
              !string.Equals(local.Folder, pair.Value.Folder, StringComparison.CurrentCultureIgnoreCase))).ToList();

        var nameAddressMismatches = NewsBlurFeedSnapshotPolicy.FindNameAddressMismatches(localOnly, remoteOnly);
        if (nameAddressMismatches.Count > 0)
        {
            var mismatchSummary = T("Oglindirea NewsBlur a fost oprită: un feed local și unul remote au același nume, dar adrese diferite. Nu s-a modificat nimic.");
            Say(mismatchSummary);
            if (!automatic)
            {
                var mismatchDetails = string.Join("\n\n", nameAddressMismatches.Select(item =>
                    F("Posibilă nepotrivire pentru „{0}”: adresa locală {1}; adresa NewsBlur {2}.", item.LocalFeed.Name, item.LocalFeed.Url, item.RemoteFeed.Url)));
                ShowNewsBlurDecision(
                    T("Verifică abonamentele NewsBlur"),
                    mismatchSummary + "\n\n" + mismatchDetails,
                    new NewsBlurDecisionOption(T("Închide"), MessageBoxResult.Cancel, IsDefault: true, IsCancel: true));
            }
            return;
        }

        var pendingCleanup = pendingAlreadyResolved.ToHashSet();
        var removeRemoteMissingLocally = remoteMissingKnown.Count > 0;

        if (localOnly.Count == 0 && remoteOnly.Count == 0 && remoteMetadataChanges.Count == 0 && pendingDeletes.Count == 0 && !removeRemoteMissingLocally)
        {
            if (pendingCleanup.Count > 0)
            {
                _settings.NewsBlurPendingFeedDeletions.RemoveAll(pendingCleanup.Contains);
                try { await _store.SaveSettingsAsync(_settings); }
                catch (Exception exception)
                {
                    Say(F("Cererile de dezabonare rezolvate nu au putut fi scoase din coada locală: {0}", Describe(exception)));
                    return;
                }
                Say(T("Feedurile și folderele sunt deja sincronizate cu NewsBlur. Cererile de ștergere rezolvate au fost închise."));
            }
            else Say(T("Feedurile și folderele sunt deja sincronizate cu NewsBlur. Nu s-a modificat nimic."));
            return;
        }

        Say(F("Se oglindesc automat abonamentele NewsBlur: {0} de trimis, {1} de importat, {2} cu metadate diferite, {3} dezabonări confirmate local și {4} eliminări remote de aplicat local.", localOnly.Count, remoteOnly.Count, remoteMetadataChanges.Count, pendingDeletes.Count, remoteMissingKnown.Count));

        var pushed = 0;
        var pushFailures = 0;
        var remoteUnsubscribed = 0;
        var remoteUnsubscribeFailures = 0;
        var pushedMetadata = 0;
        var metadataApplied = 0;
        var conflictsKeptLocal = 0;
        var conflictsUsedNewsBlur = 0;
        var conflictsDeferred = 0;
        var metadataConflictFailures = 0;
        var createdFolders = 0;
        var completedPendingDeletes = new HashSet<NewsBlurPendingFeedDeletion>(pendingCleanup);

        void RecordMetadataOutcome(NewsBlurMetadataFieldOutcome outcome)
        {
            switch (outcome)
            {
                case NewsBlurMetadataFieldOutcome.PushedLocal:
                    pushedMetadata++;
                    break;
                case NewsBlurMetadataFieldOutcome.AppliedFromNewsBlur:
                    metadataApplied++;
                    break;
                case NewsBlurMetadataFieldOutcome.ConflictKeptLocal:
                    pushedMetadata++;
                    conflictsKeptLocal++;
                    break;
                case NewsBlurMetadataFieldOutcome.ConflictUsedNewsBlur:
                    metadataApplied++;
                    conflictsUsedNewsBlur++;
                    break;
                case NewsBlurMetadataFieldOutcome.ConflictDeferred:
                    conflictsDeferred++;
                    break;
                case NewsBlurMetadataFieldOutcome.Failed:
                    metadataConflictFailures++;
                    break;
            }
        }

        foreach (var item in pendingDeletes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await connection.DeleteFeedAsync(sessionId, item.Remote.FeedId, item.Remote.Folder, cancellationToken);
                completedPendingDeletes.Add(item.Pending);
                remoteUnsubscribed++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                remoteUnsubscribeFailures++;
                Say(F("Dezabonarea feedului {0} din NewsBlur a eșuat: {1}", item.Pending.Name, Describe(exception)));
            }
        }
        var removedLocalFeeds = 0;
        var removedLocalArticles = 0;
        if (removeRemoteMissingLocally)
        {
            removedLocalFeeds = remoteMissingKnown.Count;
            removedLocalArticles = remoteMissingKnown.Sum(feed => feed.Articles.Count);
            foreach (var feed in remoteMissingKnown) _feeds.Remove(feed);
            if (_feed is not null && remoteMissingSet.Contains(_feed)) { _feed = null; _article = null; }
        }
        var knownRemoteFolders = remoteByAddress.Values
            .Select(item => item.Folder)
            .Where(NewsBlurFolderMapping.IsNamedFolder)
            .ToHashSet(StringComparer.CurrentCultureIgnoreCase);
        async Task EnsureRemoteFolderPathAsync(string? folder)
        {
            var parent = (string?)null;
            var path = string.Empty;
            foreach (var part in (folder ?? string.Empty).Split(" / ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                cancellationToken.ThrowIfCancellationRequested();
                path = string.IsNullOrWhiteSpace(path) ? part : $"{path} / {part}";
                if (!knownRemoteFolders.Contains(path))
                {
                    try
                    {
                        await connection.AddFolderAsync(sessionId, part, parent, cancellationToken);
                        knownRemoteFolders.Add(path);
                        createdFolders++;
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                    catch (Exception exception)
                    {
                        // The folder may already exist but contain no feeds, so it may be absent from the flat feed index.
                        Say(F("Folderul {0} nu a putut fi creat sau confirmat în NewsBlur; se încearcă totuși operația pentru feed. Detalii: {1}", path, Describe(exception)));
                    }
                }
                parent = part;
            }
        }
        foreach (var feed in localOnly)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await EnsureRemoteFolderPathAsync(feed.Folder);
                await connection.AddFeedAsync(sessionId, feed.Url, feed.Folder, cancellationToken: cancellationToken);
                feed.NewsBlurPendingLocalMetadataSync = true;
                pushed++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception exception)
            {
                pushFailures++;
                Say(F("Feedul {0} nu a putut fi trimis în NewsBlur: {1}", feed.Name, Describe(exception)));
            }
        }
        foreach (var subscription in remoteOnly)
        {
            cancellationToken.ThrowIfCancellationRequested();
            remoteInfoByAddress.TryGetValue(NewsBlurFeedSnapshotPolicy.AddressKey(subscription.Url), out var info);
            var name = string.IsNullOrWhiteSpace(info?.Name) ? subscription.Name : info.Name;
            var folder = NewsBlurFolderMapping.FromNewsBlurFolder(info?.Folder ?? subscription.Folder);
            var imported = new Feed
            {
                Name = string.IsNullOrWhiteSpace(name) ? subscription.Url : name,
                Url = subscription.Url,
                Folder = folder,
                NewsBlurFeedId = info?.FeedId,
                NewsBlurLastName = string.IsNullOrWhiteSpace(name) ? subscription.Url : name,
                NewsBlurLastFolder = folder
            };
            _feeds.Add(imported);
        }
        foreach (var pair in remoteByAddress)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!localByAddress.TryGetValue(pair.Key, out var local)) continue;
            var remote = pair.Value;
            if (remoteInfoByAddress.TryGetValue(pair.Key, out var info)) local.NewsBlurFeedId = info.FeedId;
            var remoteNameValue = string.IsNullOrWhiteSpace(remote.Name) ? local.Name : remote.Name;
            var remoteFolderValue = NewsBlurFolderMapping.FromNewsBlurFolder(remote.Folder);
            if (local.NewsBlurPendingLocalMetadataSync)
            {
                var metadataReady = true;
                if (!string.Equals(local.Name, remoteNameValue, StringComparison.CurrentCulture))
                {
                    try
                    {
                        await connection.RenameFeedAsync(sessionId, local.NewsBlurFeedId!, local.Name, cancellationToken);
                        local.NewsBlurLastName = local.Name;
                        pushedMetadata++;
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                    catch (Exception exception)
                    {
                        metadataReady = false;
                        metadataConflictFailures++;
                        Say(F("Numele local al feedului {0} nu a putut fi trimis în NewsBlur: {1}", local.Name, Describe(exception)));
                    }
                }
                else local.NewsBlurLastName = remoteNameValue;
                if (!string.Equals(local.Folder, remoteFolderValue, StringComparison.CurrentCultureIgnoreCase))
                {
                    try
                    {
                        await EnsureRemoteFolderPathAsync(local.Folder);
                        await connection.MoveFeedAsync(sessionId, local.NewsBlurFeedId!, remoteFolderValue, local.Folder, cancellationToken);
                        local.NewsBlurLastFolder = local.Folder;
                        pushedMetadata++;
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                    catch (Exception exception)
                    {
                        metadataReady = false;
                        metadataConflictFailures++;
                        Say(F("Folderul local al feedului {0} nu a putut fi trimis în NewsBlur: {1}", local.Name, Describe(exception)));
                    }
                }
                else local.NewsBlurLastFolder = remoteFolderValue;
                if (metadataReady) local.NewsBlurPendingLocalMetadataSync = false;
                continue;
            }
            var hasBaseline = !string.IsNullOrWhiteSpace(local.NewsBlurLastName) && !string.IsNullOrWhiteSpace(local.NewsBlurLastFolder);
            if (!hasBaseline)
            {
                local.Name = string.IsNullOrWhiteSpace(remote.Name) ? local.Name : remote.Name;
                local.Folder = string.IsNullOrWhiteSpace(remote.Folder) ? "Neorganizate" : remote.Folder;
                SetNewsBlurFeedBaseline(local, remote);
                if (!string.Equals(local.Name, remote.Name, StringComparison.CurrentCulture) || !string.Equals(local.Folder, remote.Folder, StringComparison.CurrentCultureIgnoreCase)) metadataApplied++;
                continue;
            }
            var remoteName = string.IsNullOrWhiteSpace(remote.Name) ? local.NewsBlurLastName! : remote.Name;
            var remoteFolder = remoteFolderValue;
            RecordMetadataOutcome(await SyncNewsBlurMetadataFieldAsync(
                local,
                T("numele"),
                local.Name,
                remoteName,
                local.NewsBlurLastName!,
                StringComparison.CurrentCulture,
                () => connection.RenameFeedAsync(sessionId, local.NewsBlurFeedId!, local.Name, cancellationToken),
                value => local.Name = value,
                value => local.NewsBlurLastName = value,
                automatic,
                cancellationToken));
            RecordMetadataOutcome(await SyncNewsBlurMetadataFieldAsync(
                local,
                T("folderul"),
                local.Folder,
                remoteFolder,
                local.NewsBlurLastFolder!,
                StringComparison.CurrentCultureIgnoreCase,
                async () =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await EnsureRemoteFolderPathAsync(local.Folder);
                    await connection.MoveFeedAsync(sessionId, local.NewsBlurFeedId!, remoteFolder, local.Folder, cancellationToken);
                },
                value => local.Folder = value,
                value => local.NewsBlurLastFolder = value,
                automatic,
                cancellationToken));
        }
        foreach (var pair in remoteByAddress)
            if (localByAddress.TryGetValue(pair.Key, out var local) && remoteInfoByAddress.TryGetValue(pair.Key, out var info))
                local.NewsBlurFeedId = info.FeedId;

        try { await _store.SaveAsync(_feeds); }
        catch (Exception exception)
        {
            Say(F("Feedurile nu au putut fi salvate: {0}", Describe(exception)));
            return;
        }
        if (completedPendingDeletes.Count > 0)
        {
            _settings.NewsBlurPendingFeedDeletions.RemoveAll(completedPendingDeletes.Contains);
            try { await _store.SaveSettingsAsync(_settings); }
            catch (Exception exception)
            {
                Say(F("Feedurile au fost sincronizate, dar coada locală de dezabonare nu a putut fi actualizată. Următoarea sincronizare va verifica din nou starea. Detalii: {0}", Describe(exception)));
            }
        }
        RefreshFeedList(_feed);
        RefreshTagFilter();
        RefreshArticleList(_article, selectFirstWhenNoMatch: false);
        Say(F("Oglindire NewsBlur încheiată. Feeduri trimise: {0}, eșuate: {1}, foldere create: {2}, importate local: {3}, metadate trimise: {4}, preluate: {5}, conflicte rezolvate cu Orizont: {6}, cu NewsBlur: {7}, amânate: {8}, erori la conflicte: {9}, dezabonări NewsBlur reușite: {10}, eșuate: {11}, feeduri și articole eliminate local după ștergerea din NewsBlur: {12} și {13}.", pushed, pushFailures, createdFolders, remoteOnly.Count, pushedMetadata, metadataApplied, conflictsKeptLocal, conflictsUsedNewsBlur, conflictsDeferred, metadataConflictFailures, remoteUnsubscribed, remoteUnsubscribeFailures, removedLocalFeeds, removedLocalArticles));
    }

    private async Task RunNewsBlurInitialBootstrapAsync(
        string accountUsername,
        IReadOnlyList<NewsBlurSubscription> remoteSubscriptions,
        IReadOnlyList<NewsBlurFeedInfo> remoteFeeds,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var remoteByAddress = remoteSubscriptions
            .Where(item => Uri.TryCreate(item.Url, UriKind.Absolute, out _))
            .GroupBy(item => NewsBlurFeedSnapshotPolicy.AddressKey(item.Url), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var remoteInfoByAddress = remoteFeeds
            .Where(item => Uri.TryCreate(item.Url, UriKind.Absolute, out _))
            .GroupBy(item => NewsBlurFeedSnapshotPolicy.AddressKey(item.Url), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var localFeeds = _feeds.Where(feed => NewsBlurBootstrapPolicy.IsSyncEligible(feed) && Uri.TryCreate(feed.Url, UriKind.Absolute, out _)).ToList();
        var localByAddress = localFeeds
            .GroupBy(feed => NewsBlurFeedSnapshotPolicy.AddressKey(feed.Url), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var demoAddresses = _feeds.Where(feed => !NewsBlurBootstrapPolicy.IsSyncEligible(feed))
            .Where(feed => Uri.TryCreate(feed.Url, UriKind.Absolute, out _))
            .Select(feed => NewsBlurFeedSnapshotPolicy.AddressKey(feed.Url))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pendingDeletionAddresses = _settings.NewsBlurPendingFeedDeletions
            .Select(item => NewsBlurFeedSnapshotPolicy.AddressKey(item.Url))
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var remoteOnly = remoteByAddress
            .Where(pair => !localByAddress.ContainsKey(pair.Key) && !demoAddresses.Contains(pair.Key) && !pendingDeletionAddresses.Contains(pair.Key))
            .Select(pair => pair.Value)
            .ToList();
        var matched = remoteByAddress
            .Where(pair => localByAddress.ContainsKey(pair.Key))
            .ToList();
        var metadataUpdates = matched.Count(pair =>
        {
            var local = localByAddress[pair.Key];
            var remoteName = remoteInfoByAddress.TryGetValue(pair.Key, out var info) && !string.IsNullOrWhiteSpace(info.Name)
                ? info.Name
                : pair.Value.Name;
            var remoteFolder = NewsBlurFolderMapping.FromNewsBlurFolder(
                remoteInfoByAddress.TryGetValue(pair.Key, out info) ? info.Folder : pair.Value.Folder);
            return !string.Equals(local.Name, remoteName, StringComparison.CurrentCulture) ||
                !string.Equals(local.Folder, remoteFolder, StringComparison.CurrentCultureIgnoreCase);
        });
        var localOnlyCount = localByAddress.Keys.Count(key => !remoteByAddress.ContainsKey(key));
        var ignoredDemoCount = _feeds.Count(DemoFeedCatalog.IsDemo);
        var summary = F(
            "Prima sincronizare NewsBlur pentru acest profil va importa {0} feeduri, va actualiza numele/folderele pentru {1} feeduri deja locale și va păstra local {2} feeduri care nu există în NewsBlur. Cele {3} feeduri demonstrative vor rămâne locale. În această etapă NewsBlur nu va primi feeduri, mutări sau dezabonări. Continui?",
            remoteOnly.Count, metadataUpdates, localOnlyCount, ignoredDemoCount);
        Say(T("Prima sincronizare NewsBlur pe acest profil: import numai din NewsBlur. Se așteaptă confirmarea."));
        if (ShowNewsBlurDecision(
                T("Inițializare NewsBlur pe acest calculator"),
                summary,
                new NewsBlurDecisionOption(T("Anulează"), MessageBoxResult.No, IsDefault: true, IsCancel: true),
                new NewsBlurDecisionOption(T("Continuă sincronizarea"), MessageBoxResult.Yes)) != MessageBoxResult.Yes)
        {
            Say(T("Inițializarea NewsBlur a fost anulată. Nu s-au modificat datele locale sau NewsBlur."));
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        foreach (var pair in remoteByAddress)
        {
            if (!localByAddress.TryGetValue(pair.Key, out var local)) continue;
            var remote = pair.Value;
            remoteInfoByAddress.TryGetValue(pair.Key, out var info);
            var remoteName = info is not null && !string.IsNullOrWhiteSpace(info.Name) ? info.Name : remote.Name;
            var remoteFolder = NewsBlurFolderMapping.FromNewsBlurFolder(info?.Folder ?? remote.Folder);
            if (info is not null) local.NewsBlurFeedId = info.FeedId;
            if (!string.IsNullOrWhiteSpace(remoteName)) local.Name = remoteName;
            local.Folder = remoteFolder;
            local.NewsBlurLastName = local.Name;
            local.NewsBlurLastFolder = local.Folder;
        }
        foreach (var subscription in remoteOnly)
        {
            remoteInfoByAddress.TryGetValue(NewsBlurFeedSnapshotPolicy.AddressKey(subscription.Url), out var info);
            var name = info is not null && !string.IsNullOrWhiteSpace(info.Name) ? info.Name : subscription.Name;
            var folder = NewsBlurFolderMapping.FromNewsBlurFolder(info?.Folder ?? subscription.Folder);
            _feeds.Add(new Feed
            {
                Name = string.IsNullOrWhiteSpace(name) ? subscription.Url : name,
                Url = subscription.Url,
                Folder = folder,
                NewsBlurFeedId = info?.FeedId,
                NewsBlurLastName = string.IsNullOrWhiteSpace(name) ? subscription.Url : name,
                NewsBlurLastFolder = folder
            });
        }

        var previousBootstrapAccount = _settings.NewsBlurBootstrapCompletedUsername;
        try
        {
            await _store.SaveAsync(_feeds);
            _settings.NewsBlurBootstrapCompletedUsername = accountUsername;
            await _store.SaveSettingsAsync(_settings);
        }
        catch (Exception exception)
        {
            _settings.NewsBlurBootstrapCompletedUsername = previousBootstrapAccount;
            Say(F("Inițializarea a fost citită, dar nu a putut fi salvată complet. Reîncearcă sincronizarea; NewsBlur nu a fost modificat. Detalii: {0}", Describe(exception)));
            return;
        }

        RefreshFeedList(_feed);
        RefreshTagFilter();
        RefreshArticleList(_article, selectFirstWhenNoMatch: false);
        Say(F("Inițializare NewsBlur încheiată pentru {0}: {1} feeduri importate, {2} feeduri locale păstrate fără trimitere, {3} exemple demonstrative ignorate. NewsBlur nu a fost modificat. Următoarea sincronizare pentru acest cont va fi bidirecțională.", accountUsername, remoteOnly.Count, localOnlyCount, ignoredDemoCount));
    }

    private async void NewsBlurStateSync_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await RunNewsBlurStateSyncAsync(false);
        }
        catch (Exception exception) { Say(F("Sincronizarea stărilor NewsBlur a eșuat: {0}", Describe(exception))); }
    }

    private async void NewsBlurAutoSyncTimer_Tick(object? sender, EventArgs e)
    {
        if (!_settings.NewsBlurAutoSyncEnabled || !_settings.NewsBlurConnected || string.IsNullOrWhiteSpace(_settings.EncryptedNewsBlurSession)) return;
        try
        {
            await RunNewsBlurFeedSyncWithGuardAsync(automatic: true);
            if (_closeInProgress || _isClosingAfterSave || _isRefreshing || _newsBlurFeedSyncRunning || _newsBlurStateSyncRunning) return;
            await RefreshFeedsAsync(_feeds.ToList(), fromAutoSync: true);
        }
        catch (Exception exception) { Say(F("Sincronizarea automată NewsBlur a eșuat: {0}", Describe(exception))); }
    }

    private async Task<NewsBlurSyncOutcome> RunNewsBlurStateSyncAsync(
        bool fromAutoSync,
        IReadOnlyCollection<Feed>? refreshTargets = null,
        bool allowDuringRefresh = false,
        CancellationToken cancellationToken = default)
    {
        var requestedFeeds = (refreshTargets ?? _feeds).Distinct().ToList();
        var fallbackFeeds = requestedFeeds.Where(NewsBlurBootstrapPolicy.IsSyncEligible).ToList();
        if (_newsBlurFeedSyncRunning)
        {
            if (!fromAutoSync) Say(T("Așteaptă încheierea sincronizării feedurilor NewsBlur înainte de sincronizarea articolelor."));
            return new NewsBlurSyncOutcome(false, true, false, fallbackFeeds);
        }
        if (_newsBlurStateSyncRunning)
        {
            if (!fromAutoSync) Say(T("Sincronizarea stărilor NewsBlur este deja în curs."));
            return new NewsBlurSyncOutcome(false, true, false, []);
        }
        if (_closeInProgress || _isClosingAfterSave)
            return new NewsBlurSyncOutcome(false, true, false, fallbackFeeds);
        _newsBlurStateSyncRunning = true;
        var stateCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellationToken = stateCancellation.Token;
        _newsBlurStateSyncCancellation = stateCancellation;
        var stateCompletion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _newsBlurStateSyncCompletion = stateCompletion;
        try
        {
        if (!allowDuringRefresh && RejectDataChangeDuringRefresh(T("sincronizarea stărilor NewsBlur")))
            return new NewsBlurSyncOutcome(false, true, false, []);
        if (!_settings.NewsBlurConnected || string.IsNullOrWhiteSpace(_settings.EncryptedNewsBlurSession))
        {
            Say(T("NewsBlur nu este conectat. Deschide Feeduri, Servicii externe, Autentificare NewsBlur."));
            return new NewsBlurSyncOutcome(false, false, false, fallbackFeeds);
        }
        if (NewsBlurBootstrapPolicy.RequiresInitialImport(_settings.NewsBlurUsername, _settings.NewsBlurBootstrapCompletedUsername))
        {
            if (!fromAutoSync)
                Say(T("Sincronizează mai întâi feedurile și folderele NewsBlur. Sincronizarea articolelor și stărilor începe după inițializarea acestui profil."));
            return new NewsBlurSyncOutcome(false, false, false, fallbackFeeds);
        }

        string sessionId;
        try { sessionId = SecretProtector.Unprotect(_settings.EncryptedNewsBlurSession); }
        catch
        {
            Say(T("Sesiunea NewsBlur nu poate fi citită pentru acest cont Windows. Autentifică-te din nou."));
            return new NewsBlurSyncOutcome(false, false, false, fallbackFeeds);
        }

        if (_settings.AutoCleanupEnabled)
            await CleanupExpiredArticlesAsync(false);
        cancellationToken.ThrowIfCancellationRequested();
        var retentionCutoff = DateTimeOffset.Now.AddDays(-_settings.RetentionDays);

        Say(refreshTargets is null
            ? T("Se sincronizează articolele și stările NewsBlur. Prima rulare stabilește baza fără să suprascrie stări.")
            : F("Se preiau articolele din NewsBlur pentru {0} feeduri.", requestedFeeds.Count));
        var connection = new NewsBlurConnection();
        IReadOnlyList<NewsBlurFeedInfo> remoteFeeds;
        try { remoteFeeds = await connection.GetFeedIndexAsync(sessionId, cancellationToken); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            var cancelledSummary = T("Sincronizarea s-a oprit. Rezultatele locale au fost salvate; modificările deja confirmate în NewsBlur vor fi verificate la următoarea sincronizare.");
            if (!allowDuringRefresh) Say(cancelledSummary);
            return new NewsBlurSyncOutcome(false, false, true, [], CompletionSummary: cancelledSummary);
        }
        catch (Exception exception)
        {
            Say(F("Sincronizarea NewsBlur a eșuat: {0}", Describe(exception)));
            return new NewsBlurSyncOutcome(false, false, false, fallbackFeeds);
        }

        var remoteByUrl = remoteFeeds
            .Where(feed => !string.IsNullOrWhiteSpace(feed.Url))
            .GroupBy(feed => SyncAddressKey(feed.Url), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var toRead = new List<(Article Article, string Hash)>();
        var toUnread = new List<(Article Article, string Hash)>();
        var toStar = new List<(Article Article, string Hash)>();
        var toUnstar = new List<(Article Article, string Hash)>();
        var baselineCreated = 0;
        var matched = 0;
        var appliedRemote = 0;
        var conflicts = 0;
        var skippedTags = 0;
        var imported = 0;
        var skippedExpiredByRetention = 0;
        var fallback = new List<Feed>();
        var cancelled = false;
        var remoteSuccesses = 0;
        var feedsToProcess = requestedFeeds.Where(NewsBlurBootstrapPolicy.IsSyncEligible).ToList();

        foreach (var feed in feedsToProcess)
        {
            if (cancellationToken.IsCancellationRequested) { cancelled = true; break; }
            var feedFailed = false;
            if (!remoteByUrl.TryGetValue(SyncAddressKey(feed.Url), out var remoteFeed))
            {
                fallback.Add(feed);
                feedFailed = true;
            }
            else
            {
            feed.NewsBlurFeedId = remoteFeed.FeedId;
            var localArticles = feed.Articles ?? [];
            var byHash = localArticles.Where(article => !string.IsNullOrWhiteSpace(article.NewsBlurStoryHash))
                .GroupBy(article => article.NewsBlurStoryHash!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var byLink = localArticles.Where(article => !string.IsNullOrWhiteSpace(article.Link))
                .GroupBy(article => SyncAddressKey(article.Link), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var byId = localArticles.Where(article => !string.IsNullOrWhiteSpace(article.Id))
                .GroupBy(article => article.Id, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            DateTimeOffset? oldest = localArticles.Count == 0 ? null : localArticles.Min(article => article.Published);
            IReadOnlyList<NewsBlurStory> stories;
            try { stories = await connection.GetStoriesAsync(sessionId, remoteFeed.FeedId, oldest, cancellationToken: cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                cancelled = true;
                break;
            }
            catch (Exception exception)
            {
                Say(F("Sincronizarea feedului NewsBlur {0} a eșuat: {1}", feed.Name, Describe(exception)));
                fallback.Add(feed);
                feedFailed = true;
                stories = [];
            }

            if (!feedFailed)
            {
            var newInFeed = 0;
            foreach (var story in stories)
            {
                if (cancellationToken.IsCancellationRequested) { cancelled = true; break; }
                var article = FindLocalArticle(story, byHash, byLink, byId, localArticles);
                if (article is null)
                {
                    var published = story.Published ?? DateTimeOffset.Now;
                    var importedState = new Article { Published = published };
                    ApplyNewsBlurSavedState(importedState, story.IsStarred);
                    if (_settings.AutoCleanupEnabled && !ArticleRetention.ShouldKeep(importedState, retentionCutoff))
                    {
                        skippedExpiredByRetention++;
                        continue;
                    }
                    var content = string.IsNullOrWhiteSpace(story.Content) ? story.Title : story.Content;
                    article = new Article
                    {
                        Id = string.IsNullOrWhiteSpace(story.Link) ? $"newsblur:{story.StoryHash}" : story.Link,
                        Title = string.IsNullOrWhiteSpace(story.Title) ? T("Articol NewsBlur fără titlu") : story.Title,
                        Content = content,
                        FullContent = string.IsNullOrWhiteSpace(story.Content) ? null : story.Content,
                        Link = story.Link,
                        Published = published,
                        IsRead = story.IsRead,
                        IsFavorite = importedState.IsFavorite,
                        ReadLater = importedState.ReadLater,
                        Tags = story.UserTags.ToList(),
                        NewsBlurStoryHash = story.StoryHash
                    };
                    var articleStore = feed.Articles ??= [];
                    articleStore.Add(article);
                    localArticles = articleStore;
                    imported++;
                    newInFeed++;
                    SetNewsBlurBaseline(article, story);
                    // Establish the remote baseline BEFORE additive local rules; next sync sees the local change.
                    ArticleRules.ApplyIncoming(_settings.ArticleRules, feed, article);
                    matched++;
                    continue;
                }
                matched++;
                article.NewsBlurStoryHash = story.StoryHash;
                var hasBaseline = article.NewsBlurLastRead.HasValue && article.NewsBlurLastStarred.HasValue && article.NewsBlurLastTags is not null &&
                    article.NewsBlurLastLocalRead.HasValue && article.NewsBlurLastLocalStarred.HasValue && article.NewsBlurLastLocalTags is not null &&
                    article.NewsBlurLastLocalSaved.HasValue;
                if (!hasBaseline)
                {
                    SetNewsBlurBaseline(article, story);
                    baselineCreated++;
                    continue;
                }

                var reconciliation = NewsBlurArticleStateReconciler.Reconcile(article, story, NewsBlurSavedState, ApplyNewsBlurSavedState);
                if (reconciliation.ReadAction == NewsBlurReadSyncAction.SendRead) toRead.Add((article, story.StoryHash));
                if (reconciliation.ReadAction == NewsBlurReadSyncAction.SendUnread) toUnread.Add((article, story.StoryHash));
                if (reconciliation.SendStar) toStar.Add((article, story.StoryHash));
                if (reconciliation.SendUnstar) toUnstar.Add((article, story.StoryHash));
                if (reconciliation.SendStarForTags) toStar.Add((article, story.StoryHash));
                if (reconciliation.SkippedTags) skippedTags++;
                appliedRemote += reconciliation.AppliedRemote;
                conflicts += reconciliation.Conflicts;
            }
            if (!cancelled)
            {
                feed.ConsecutiveFailures = 0;
                feed.LastError = null;
                feed.LastSuccessfulUpdate = DateTimeOffset.Now;
                if (newInFeed > 0) feed.LastArticleReceivedOn = DateTimeOffset.Now;
                remoteSuccesses++;
            }
            }
            }
            if (allowDuringRefresh)
            {
                _refreshProcessed++;
                SetProgressStatus(F("NewsBlur: {0} din {1} feeduri verificate; {2} reușite, {3} vor fi încercate prin RSS.",
                    _refreshProcessed, _refreshTotal, remoteSuccesses, fallback.Count),
                    feedFailed || cancelled || _refreshProcessed % 10 == 0 || _refreshProcessed == _refreshTotal);
            }
            if (cancelled) break;
        }

        var stateSyncFailed = false;
        try
        {
            await connection.MarkStoriesReadAsync(sessionId, toRead.Select(item => item.Hash), true, cancellationToken);
            foreach (var item in toRead) { item.Article.NewsBlurLastRead = true; item.Article.NewsBlurLastLocalRead = item.Article.IsRead; }
            await connection.MarkStoriesReadAsync(sessionId, toUnread.Select(item => item.Hash), false, cancellationToken);
            foreach (var item in toUnread) { item.Article.NewsBlurLastRead = false; item.Article.NewsBlurLastLocalRead = item.Article.IsRead; }
            foreach (var item in toStar)
            {
                await connection.MarkStoryStarredAsync(sessionId, item.Hash, true, item.Article.Tags, cancellationToken);
                item.Article.NewsBlurLastStarred = true;
                item.Article.NewsBlurLastTags = (item.Article.Tags ?? []).ToList();
                item.Article.NewsBlurLastLocalStarred = item.Article.IsFavorite;
                item.Article.NewsBlurLastLocalTags = (item.Article.Tags ?? []).ToList();
                item.Article.NewsBlurLastLocalSaved = NewsBlurSavedState(item.Article);
            }
            foreach (var item in toUnstar)
            {
                await connection.MarkStoryStarredAsync(sessionId, item.Hash, false, cancellationToken: cancellationToken);
                item.Article.NewsBlurLastStarred = false;
                item.Article.NewsBlurLastLocalStarred = item.Article.IsFavorite;
                item.Article.NewsBlurLastLocalSaved = NewsBlurSavedState(item.Article);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            cancelled = true;
        }
        catch (Exception exception)
        {
            Say(F("Sincronizarea stărilor NewsBlur a eșuat: {0}", Describe(exception)));
            stateSyncFailed = true;
        }

        try
        {
            await _store.SaveAsync(_feeds);
        }
        catch (Exception exception)
        {
            Say(F("Feedurile nu au putut fi salvate: {0}", Describe(exception)));
            return new NewsBlurSyncOutcome(true, false, cancelled, [], imported);
        }
        RefreshFeedList(_feed);
        RefreshTagFilter();
        RefreshArticleList(_article, selectFirstWhenNoMatch: false);
        var syncSummary = cancelled
            ? T("Sincronizarea s-a oprit. Rezultatele locale au fost salvate; modificările deja confirmate în NewsBlur vor fi verificate la următoarea sincronizare.")
            : F("Sincronizare bidirecțională încheiată. {0} articole asociate, {1} importate din NewsBlur, {2} baze locale create, {3} stări preluate, {4} stări trimise, {5} conflicte păstrate neschimbate și {6} etichete amânate până la salvarea articolului.", matched, imported, baselineCreated, appliedRemote, toRead.Count + toUnread.Count + toStar.Count + toUnstar.Count, conflicts, skippedTags);
        if (stateSyncFailed) syncSummary += " " + T("Articolele primite au fost salvate local, dar unele stări nu au putut fi trimise sau preluate.");
        if (skippedExpiredByRetention > 0)
            syncSummary += F(" {0} articole expirate au fost ignorate local; nu au fost șterse din NewsBlur.", skippedExpiredByRetention);
        if (!allowDuringRefresh) Say(syncSummary);
        if (!allowDuringRefresh && !cancelled) SoundAlertService.Play(SoundEvent.SyncDone);
        return new NewsBlurSyncOutcome(true, false, cancelled, cancelled ? [] : fallback, imported, syncSummary);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _store.SaveAsync(_feeds);
                RefreshFeedList(_feed);
                RefreshTagFilter();
                RefreshArticleList(_article, selectFirstWhenNoMatch: false);
            }
            catch (Exception exception)
            {
                Say(F("Feedurile nu au putut fi salvate: {0}", Describe(exception)));
                throw;
            }
            var cancelledSummary = T("Sincronizarea s-a oprit. Rezultatele locale au fost salvate; modificările deja confirmate în NewsBlur vor fi verificate la următoarea sincronizare.");
            if (!allowDuringRefresh) Say(cancelledSummary);
            return new NewsBlurSyncOutcome(false, false, true, [], CompletionSummary: cancelledSummary);
        }
        finally
        {
            _newsBlurStateSyncRunning = false;
            stateCompletion.TrySetResult(true);
            if (ReferenceEquals(_newsBlurStateSyncCompletion, stateCompletion)) _newsBlurStateSyncCompletion = null;
            if (ReferenceEquals(_newsBlurStateSyncCancellation, stateCancellation)) _newsBlurStateSyncCancellation = null;
            stateCancellation.Dispose();
        }
    }

    private static string SyncAddressKey(string value)
    {
        if (Uri.TryCreate(value?.Trim(), UriKind.Absolute, out var uri))
            return uri.GetLeftPart(UriPartial.Path).TrimEnd('/').ToLowerInvariant();
        return (value ?? string.Empty).Trim().TrimEnd('/').ToLowerInvariant();
    }

    private void ConfigureNewsBlurAutoSync()
    {
        _newsBlurAutoSyncTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMinutes(30) };
        _newsBlurAutoSyncTimer.Tick -= NewsBlurAutoSyncTimer_Tick;
        _newsBlurAutoSyncTimer.Tick += NewsBlurAutoSyncTimer_Tick;
        _newsBlurAutoSyncTimer.Interval = TimeSpan.FromMinutes(Math.Clamp(_settings.NewsBlurAutoSyncMinutes, 15, 180));
        _newsBlurAutoSyncTimer.Stop();
        if (_startupSucceeded && _settings.NewsBlurAutoSyncEnabled && _settings.NewsBlurConnected && !string.IsNullOrWhiteSpace(_settings.EncryptedNewsBlurSession))
            _newsBlurAutoSyncTimer.Start();
    }

    private static Article? FindLocalArticle(NewsBlurStory story, IReadOnlyDictionary<string, Article> byHash, IReadOnlyDictionary<string, Article> byLink, IReadOnlyDictionary<string, Article> byId, IReadOnlyList<Article> articles)
    {
        if (byHash.TryGetValue(story.StoryHash, out var byStoryHash)) return byStoryHash;
        if (!string.IsNullOrWhiteSpace(story.Link) && byLink.TryGetValue(SyncAddressKey(story.Link), out var byStoryLink)) return byStoryLink;
        if (!string.IsNullOrWhiteSpace(story.Link) && byId.TryGetValue(story.Link, out var byStoryId)) return byStoryId;
        if (string.IsNullOrWhiteSpace(story.Title)) return null;
        return articles.FirstOrDefault(article => string.Equals(article.Title.Trim(), story.Title.Trim(), StringComparison.CurrentCultureIgnoreCase) &&
            (!story.Published.HasValue || Math.Abs((article.Published - story.Published.Value).TotalDays) <= 2));
    }

    private static void SetNewsBlurFeedBaseline(Feed feed, NewsBlurSubscription remote)
    {
        feed.NewsBlurLastName = remote.Name;
        feed.NewsBlurLastFolder = remote.Folder;
    }

    private static void SetNewsBlurFeedBaseline(Feed feed, NewsBlurFeedInfo remote)
    {
        feed.NewsBlurLastName = remote.Name;
        feed.NewsBlurLastFolder = remote.Folder;
    }

    private async Task<NewsBlurMetadataFieldOutcome> SyncNewsBlurMetadataFieldAsync(
        Feed feed,
        string fieldName,
        string localValue,
        string remoteValue,
        string baselineValue,
        StringComparison comparison,
        Func<Task> pushRemote,
        Action<string> applyLocal,
        Action<string> setBaseline,
        bool automatic,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var localChanged = !string.Equals(localValue, baselineValue, comparison);
        var remoteChanged = !string.Equals(remoteValue, baselineValue, comparison);
        var conflict = NewsBlurMetadataConflictPolicy.IsConflict(localChanged, remoteChanged, localValue, remoteValue, comparison);
        if (conflict)
        {
            if (automatic) return NewsBlurMetadataFieldOutcome.ConflictDeferred;
            var choice = AskNewsBlurMetadataConflict(feed, fieldName, localValue, remoteValue);
            if (choice == MessageBoxResult.Cancel) return NewsBlurMetadataFieldOutcome.ConflictDeferred;
            if (choice == MessageBoxResult.No)
            {
                applyLocal(remoteValue);
                setBaseline(remoteValue);
                return NewsBlurMetadataFieldOutcome.ConflictUsedNewsBlur;
            }
            if (choice != MessageBoxResult.Yes) return NewsBlurMetadataFieldOutcome.ConflictDeferred;
            cancellationToken.ThrowIfCancellationRequested();
            return await PushNewsBlurMetadataValueAsync(feed, fieldName, localValue, pushRemote, setBaseline, true, cancellationToken);
        }

        if (localChanged && !remoteChanged)
            return await PushNewsBlurMetadataValueAsync(feed, fieldName, localValue, pushRemote, setBaseline, false, cancellationToken);

        if (remoteChanged && !string.Equals(localValue, remoteValue, comparison))
        {
            applyLocal(remoteValue);
            setBaseline(remoteValue);
            return NewsBlurMetadataFieldOutcome.AppliedFromNewsBlur;
        }

        // Unchanged fields and edits that converged independently share the NewsBlur value as their new baseline.
        setBaseline(remoteValue);
        return NewsBlurMetadataFieldOutcome.Unchanged;
    }

    private async Task<NewsBlurMetadataFieldOutcome> PushNewsBlurMetadataValueAsync(
        Feed feed,
        string fieldName,
        string localValue,
        Func<Task> pushRemote,
        Action<string> setBaseline,
        bool resolvingConflict,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(feed.NewsBlurFeedId))
        {
            Say(F("Nu se poate sincroniza {0} pentru feedul {1}: lipsește identificatorul NewsBlur.", fieldName, feed.Name));
            return NewsBlurMetadataFieldOutcome.Failed;
        }
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            await pushRemote();
            setBaseline(localValue);
            return resolvingConflict ? NewsBlurMetadataFieldOutcome.ConflictKeptLocal : NewsBlurMetadataFieldOutcome.PushedLocal;
        }
        catch (Exception exception)
        {
            Say(F("Actualizarea {0} pentru feedul {1} a eșuat; valorile rămân în conflict pentru o nouă încercare. Detalii: {2}", fieldName, feed.Name, Describe(exception)));
            return NewsBlurMetadataFieldOutcome.Failed;
        }
    }

    private MessageBoxResult AskNewsBlurMetadataConflict(Feed feed, string fieldName, string localValue, string remoteValue)
    {
        Say(F("Conflict NewsBlur pentru {0} în feedul {1}. Se așteaptă alegerea.", fieldName, feed.Name));
        var message = F("Conflict de sincronizare pentru {0} în feedul {1}.\n\nValoarea din Orizont RSS: {2}\nValoarea din NewsBlur: {3}\n\nAlege dacă păstrezi varianta locală, preiei varianta NewsBlur sau amâni rezolvarea acestui câmp. Dacă amâni, sincronizarea celorlalte câmpuri continuă.", fieldName, feed.Name, localValue, remoteValue);
        return ShowNewsBlurDecision(
            T("Rezolvă conflictul NewsBlur"),
            message,
            new NewsBlurDecisionOption(T("Amână"), MessageBoxResult.Cancel, IsDefault: true, IsCancel: true),
            new NewsBlurDecisionOption(T("Folosește NewsBlur"), MessageBoxResult.No),
            new NewsBlurDecisionOption(T("Păstrează Orizont RSS"), MessageBoxResult.Yes));
    }

    private MessageBoxResult ShowNewsBlurDecision(string title, string message, params NewsBlurDecisionOption[] options)
    {
        var dialog = new NewsBlurDecisionWindow(title, message, options) { Owner = this };
        dialog.ShowDialog();
        return dialog.Decision;
    }

    private static List<string> DescribeRemoteDuplicateFeeds(
        IReadOnlyList<NewsBlurSubscription> subscriptions,
        IReadOnlyList<NewsBlurFeedInfo> feeds)
    {
        var descriptions = new List<string>();
        var duplicateSubscriptions = subscriptions
            .GroupBy(item => NewsBlurFeedSnapshotPolicy.AddressKey(item.Url), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1);
        foreach (var group in duplicateSubscriptions)
            descriptions.Add(F("Abonamente: {0}\n{1}", group.Key, string.Join("\n", group.Select(item => F("• {0}; folder: {1}; adresă: {2}", item.Name, item.Folder, item.Url)))));

        var duplicateFeeds = feeds
            .GroupBy(item => NewsBlurFeedSnapshotPolicy.AddressKey(item.Url), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1);
        foreach (var group in duplicateFeeds)
            descriptions.Add(F("Indexul de feeduri: {0}\n{1}", group.Key, string.Join("\n", group.Select(item => F("• {0}; folder: {1}; adresă: {2}; identificator: {3}", item.Name, item.Folder, item.Url, item.FeedId)))));

        var duplicateIds = feeds
            .Where(item => !string.IsNullOrWhiteSpace(item.FeedId))
            .GroupBy(item => item.FeedId, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1);
        foreach (var group in duplicateIds)
            descriptions.Add(F("Identificator repetat {0}: {1}", group.Key, string.Join("; ", group.Select(item => F("{0} ({1})", item.Name, item.Url)))));

        return descriptions;
    }

    private bool NewsBlurSavedState(Article article) => _settings.NewsBlurSavedStoryMode switch
    {
        "ReadLater" => article.ReadLater,
        "Both" => article.IsFavorite || article.ReadLater,
        _ => article.IsFavorite
    };

    private void ApplyNewsBlurSavedState(Article article, bool saved)
    {
        switch (_settings.NewsBlurSavedStoryMode)
        {
            case "ReadLater":
                article.ReadLater = saved;
                break;
            case "Both":
                article.IsFavorite = saved;
                article.ReadLater = saved;
                break;
            default:
                article.IsFavorite = saved;
                break;
        }
    }

    private void SetNewsBlurBaseline(Article article, NewsBlurStory story)
    {
        article.NewsBlurLastRead = story.IsRead;
        article.NewsBlurLastStarred = story.IsStarred;
        article.NewsBlurLastTags = story.UserTags.ToList();
        article.NewsBlurLastLocalRead = article.IsRead;
        article.NewsBlurLastLocalStarred = article.IsFavorite;
        article.NewsBlurLastLocalTags = (article.Tags ?? []).ToList();
        article.NewsBlurLastLocalSaved = NewsBlurSavedState(article);
        if (article.AutomationPendingNewsBlurBaseline)
        {
            article.Tags = (article.Tags ?? []).Concat(story.UserTags).Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
            article.NewsBlurLastLocalTags = story.UserTags.ToList();
            if (article.AutomationAddedReadLater)
                article.NewsBlurLastLocalSaved = _settings.NewsBlurSavedStoryMode switch
                {
                    "ReadLater" => false,
                    "Both" => article.IsFavorite,
                    _ => article.IsFavorite
                };
            article.AutomationPendingNewsBlurBaseline = false;
            article.AutomationAddedReadLater = false;
        }
    }
}
