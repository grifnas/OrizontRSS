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

// Mutarea focusului între zonele ferestrei: feeduri, selectorul de folder, articole și cititor.
public partial class MainWindow
{
    private void FocusFeeds()
    {
        if (Feeds.SelectedItem is null && Feeds.Items.Count > 0) Feeds.SelectedIndex = 0;
        Feeds.Focus();
        if (Feeds.SelectedIndex >= 0)
        {
            Feeds.ScrollIntoView(Feeds.SelectedItem);
            Feeds.UpdateLayout();
            if (Feeds.ItemContainerGenerator.ContainerFromIndex(Feeds.SelectedIndex) is ListBoxItem row)
            {
                row.Focus();
                Keyboard.Focus(row);
            }
        }
        var selected = Feeds.SelectedItem as Feed;
        Say(selected is null
            ? F("Panou Feeduri. {0} feeduri afișate.", Feeds.Items.Count)
            : F("Panou Feeduri. {0} feeduri afișate. Feed selectat: {1}.", Feeds.Items.Count, selected.Name));
    }
    private void FocusFolderSelector()
    {
        if (AttentionFilter.IsChecked == true)
        {
            AttentionToggleButton.Focus();
            Say("Selectorul de foldere nu este disponibil cât timp sunt afișate feedurile care necesită atenție. Apasă butonul pentru a reveni la foldere.");
            return;
        }
        FolderFilter.Focus();
        var folder = SelectedFolder();
        Say(folder == AllFoldersKey
            ? F("Selector foldere. Este selectată opțiunea Toate folderele. {0} foldere disponibile. Folosește săgețile pentru alegere, apoi Ctrl+Shift+2 pentru articole.", Math.Max(0, FolderFilter.Items.Count - 1))
            : F("Selector foldere. Folder selectat: {0}. Folosește săgețile pentru alegere, apoi Ctrl+Shift+2 pentru articole.", folder));
    }
    private void FocusSelectedFolderArticles()
    {
        _readNowView = false;
        ShowFolderAggregate();
        FocusArticles();
    }
    private void FocusArticles()
    {
        if (Articles.SelectedItem is null && Articles.Items.Count > 0) Articles.SelectedIndex = 0;
        Articles.Focus();
        if (Articles.SelectedIndex >= 0)
        {
            Articles.ScrollIntoView(Articles.SelectedItem);
            Articles.UpdateLayout();
            if (Articles.ItemContainerGenerator.ContainerFromIndex(Articles.SelectedIndex) is ListBoxItem row)
            {
                row.Focus();
                Keyboard.Focus(row);
            }
        }
        var selected = Articles.SelectedItem as Article;
        if (selected is not null)
        {
            var flags = new List<string>();
            if (selected.IsFavorite) flags.Add(T("Favorit"));
            if (selected.ReadLater) flags.Add(T("De citit mai târziu"));
            if (!string.IsNullOrWhiteSpace(selected.FullContent)) flags.Add(T("Text complet disponibil"));
            var prefix = flags.Count > 0 ? string.Join(". ", flags) + ". " : string.Empty;
            Say(F("{0}{1}", prefix, ArticlePanelStatus()));
        }
        else
        {
            Say(ArticlePanelStatus());
        }
    }
    private void FocusReader()
    {
        if (_article is null)
        {
            Reader.Focus();
            Say("Conținut articol, gol. Alege un articol din listă pentru a-l citi.");
            return;
        }
        var article = _article;
        _readerReturnItems = Articles.Items.Cast<Article>().ToList();
        _readerReturnArticle = article;
        foreach (var copy in RelatedArticleCopies([article])) copy.IsRead = true;
        Reader.Focus();
        Reader.CaretIndex = 0;
        Reader.Select(0, 0);
        Say(F("Conținut articol: {0}. Editare numai în citire. Folosește săgețile Sus și Jos pentru citire.", article.Title));
    }
}
