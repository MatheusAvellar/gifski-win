using System;
using System.Windows;

namespace gifski_win {
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window {
    public MainWindow() {
      InitializeComponent();
    }

    private void OnDropFile(object sender, DragEventArgs e) {
      // Handle file being dropped on the window
      // Via https://stackoverflow.com/a/5663329/4824627

      // Hide border, since user has already dropped the file
      dragzone_border.BorderThickness = new Thickness(0);
      if(e.Data.GetDataPresent(DataFormats.FileDrop)) {
        // Note that you can have more than one file
        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

        // TODO: Check if it's actually a video file?

        // Leave this window open as the user might Cancel on the next window
        this.Hide();
        // Send file path to next window
        EditWindow editor = new EditWindow(files[0]) {  Owner = this  };
        editor.Show();
      }
    }

    private void OnDragFileLeave(object sender, DragEventArgs e) {
      dragzone_border.BorderThickness = new Thickness(0);
    }

    private void OnDragFileOver(object sender, DragEventArgs e) {
      dragzone_border.BorderThickness = new Thickness(4);
    }
  }
}
