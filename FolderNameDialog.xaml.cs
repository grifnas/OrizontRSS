using System.Windows;
using CititorRSS.Jaws.Localization;

namespace CititorRSS.Jaws;

public partial class FolderNameDialog : Window
{
    public string FolderName => FolderNameBox.Text.Trim();
    public FolderNameDialog()
    {
        InitializeComponent();
        Loaded += (_, _) => FolderNameBox.Focus();
    }
    private void Create_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(FolderName))
        {
            MessageBox.Show(this, UiText.Translate("Folderul nu poate fi creat deoarece numele lipsește. Scrie un nume și alege din nou Creează."), UiText.Translate("Numele folderului lipsește"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }
}
