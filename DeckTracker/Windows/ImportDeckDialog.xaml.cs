using System.Threading.Tasks;
using System.Windows;

namespace DeckTracker.Windows
{
    public partial class ImportDeckDialog
    {
        private readonly TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
        public string DeckList { get; set; }

        public ImportDeckDialog()
        {
            InitializeComponent();
            DeckListTextBox.Focus();
        }

        private void ImportButton_OnClick(object sender, RoutedEventArgs e)
        {
            ImportButton.IsEnabled = false;
            tcs.SetResult(DeckList);
        }

        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            CancelButton.IsEnabled = false;
            tcs.SetResult(null);
        }

        internal Task<string> WaitForButtonPressAsync() => tcs.Task;
    }
}
