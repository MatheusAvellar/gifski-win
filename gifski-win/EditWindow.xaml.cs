using System;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace gifski_win {
  /// <summary>
  /// Interaction logic for EditWindow.xaml
  /// </summary>
  public partial class EditWindow : Window {

    private string path;

    public EditWindow(string path) {
      InitializeComponent();

      this.Focus();
      this.path = path;

      // Fetch video path sent from previous window
      videoplayer.Source = new Uri(path);
    }

    private void CancelClick(object sender, RoutedEventArgs e) {
      // On cancel, reopen first window and close editor window
      this.Owner.Show();
      this.Close();
    }

    private void ConvertClick(object sender, RoutedEventArgs e) {
      if(path == null)
        return;

      // Start conversion
      ProgressWindow prg = new ProgressWindow(path);
      prg.Show();

      // Kill previous windows, as we won't be using them anymore
      this.Owner.Close();
      this.Close();
    }
  }
}
