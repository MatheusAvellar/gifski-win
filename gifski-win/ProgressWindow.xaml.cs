using Microsoft.Win32;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using WpfAnimatedGif;

namespace gifski_win {
  /// <summary>
  /// Interaction logic for ProgressWindow.xaml
  /// </summary>
  public partial class ProgressWindow : Window {

    private string path;
    private string outputFullPath;
    private string outputFolderPath;
    bool _shown;

    public ProgressWindow(string path) {
      InitializeComponent();
      this.Focus();
      this.path = path;

      TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
      TaskbarItemInfo.ProgressValue = 0.01;
    }

    private void OnContentRender(object sender, EventArgs e) {
      if(_shown) return;
      _shown = true;

      // Create frames folder inside TEMP directory
      string framesFolderPath = Path.Combine(Path.GetTempPath(), @"_gifski_frames");
      if(!Directory.Exists(framesFolderPath))
        Directory.CreateDirectory(framesFolderPath);

      UpdateETA("Chopping up frames...");

      // Run ffmpeg with the given frames folder
      Task.Run(
        () => Runffmpeg(framesFolderPath))
      .ContinueWith(
        _ => {
          // Now that ffmpeg is done, we are halfway through the process ¯\_(ツ)_/¯
          UpdatePercentage(50);

          // Create output folder inside TEMP directory
          string outputFolderPath = Path.Combine(Path.GetTempPath(), @"_gifski_output");
          if(!Directory.Exists(outputFolderPath))
            Directory.CreateDirectory(outputFolderPath);

          // Run gifski with the given frame and output folder
          Task.Run(
            () => Rungifski(framesFolderPath, outputFolderPath))
          .ContinueWith(
             __ => {

               // We are done!
               UpdatePercentage(100);
               UpdateETA("Done!");

               Dispatcher.Invoke(() => {
                 this.Title = "Gifski – Done!";

                 // We are done, remove taskbar progress
                 // Via https://docs.microsoft.com/en-us/dotnet/api/system.windows.shell.taskbariteminfo.progressstate#examples
                 TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;

                 outputSize.Content = SizeSuffix(new FileInfo(outputFullPath).Length, 2);
               });

               UpdatePreview();
             }
          );
        }
      );
    }

    private void Runffmpeg(string framesFolderPath) {
      // Run ffmpeg to extract frames into newly created folder
      // "-hide_banner" and "-loglevel panic" flags from:
      // Via https://superuser.com/questions/326629/how-can-i-make-ffmpeg-be-quieter-less-verbose
      string ffmpeg_args = "-hide_banner -loglevel panic -i " + path + " " + Path.Combine(framesFolderPath, "f%04d.png");
      RunCommand(ExtractResource("ffmpeg"), ffmpeg_args);
    }

    private void Rungifski(string framesFolderPath, string outputFolderPath) {
      // Save global reference to output path
      this.outputFolderPath = outputFolderPath;
      this.outputFullPath = Path.Combine(outputFolderPath, "file.gif");

      // Run gifski to turn frames into .gif format, saving to newly created folder
      string gifski_args =
        "-o " + outputFullPath
        + " " + Path.Combine(framesFolderPath, "f*.png");
      RunCommand(ExtractResource("gifski"), gifski_args);
      
      // Clean up exported frames after we're done
      Directory.Delete(framesFolderPath, true);
    }

    private void UpdatePercentage(int percent) {
      // Must use Dispatcher since we're not on the UI thread or whatever
      Dispatcher.Invoke(() => {
        progressLabel.Content = percent + "%";
        progressBar.Value = percent;
      });
    }

    private void UpdateETA(string eta) {
      Dispatcher.Invoke(() => {
        etaLabel.Content = eta;
      });
    }

    private void UpdatePreview() {
      Dispatcher.Invoke(() => {
        TimeSpan duration = new TimeSpan(0, 0, 1);

        // Fade controls out
        Fade(new DoubleAnimation {
          From = 1.0, To = 0.0,
          Duration = new Duration(duration)
        }, initialStage.Name);

        // Fade GIF in
        Fade(new DoubleAnimation {
          From = 0.0, To = 1.0,
          Duration = new Duration(duration)
        }, finalStage.Name);

        // Animate output GIF
        var image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri(outputFullPath);
        image.EndInit();
        ImageBehavior.SetAnimatedSource(gifPreview, image);
      });
    }

    private void Fade(DoubleAnimation anim, string name) {
      // Via http://wpf-samples.blogspot.com/2007/01/programmatic-fade-in-out-sample.html

      // Create a storyboard to contain the animations
      Storyboard fadeOutStoryboard = new Storyboard();
      // Configure the animation to target de property Opacity
      Storyboard.SetTargetName(anim, name);
      Storyboard.SetTargetProperty(anim, new PropertyPath(Control.OpacityProperty));
      // Add the animation to the storyboard
      fadeOutStoryboard.Children.Add(anim);
      // Begin the storyboard
      fadeOutStoryboard.Begin(this);
    }

    private string ExtractResource(string resName) {
      // Get resource from Resource Manager
      object ob = Properties.Resources.ResourceManager.GetObject(resName);
      byte[] resBytes = (byte[])ob;

      // Get path to TEMP folder
      string newPath = Path.Combine(Path.GetTempPath(), resName);
      // If a file with the resource's name already exists and has the Hidden attribute
      // (FileMode.Create from further down throws an Exception if the file already exists and is hidden)
      if(File.Exists(newPath) && File.GetAttributes(newPath).HasFlag(FileAttributes.Hidden)) {
        // Get all attributes from the file
        FileAttributes attributes = File.GetAttributes(newPath);
        // Remove only the Hidden attribute
        attributes = RemoveAttribute(attributes, FileAttributes.Hidden);
        // Apply changes to file
        File.SetAttributes(newPath, attributes);
      }
      // Open / Create file, and transfer all bytes from our resource into it
      using(FileStream fsDst = new FileStream(newPath, FileMode.Create, FileAccess.Write)) {
        byte[] bytes = resBytes;
        fsDst.Write(bytes, 0, bytes.Length);
        // FileStream should automatically Close() when inside a 'using' block
        // And Close() itself calls Dispose() – so we are done here
      }
      // Set the Temporary flag on the file, as we will delete it later
      File.SetAttributes(newPath, File.GetAttributes(newPath) | FileAttributes.Temporary);
      // Return the full path to the created file
      return newPath;
    }

    private static FileAttributes RemoveAttribute(FileAttributes attributes, FileAttributes attributesToRemove) {
      return attributes & ~attributesToRemove;
    }

    private void RunCommand(string filePath, string arguments) {
      // Starts process without a window
      ProcessStartInfo startInfo = new ProcessStartInfo {
        FileName = filePath,
        Arguments = arguments,
        UseShellExecute = false,
        CreateNoWindow = true,
        WindowStyle = ProcessWindowStyle.Hidden,
        RedirectStandardOutput = true,
        RedirectStandardError = true
      };

      Process process = new Process {
        StartInfo = startInfo
      };
      process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
      process.ErrorDataReceived += new DataReceivedEventHandler(ErrorHandler);
      process.Start();
      process.BeginOutputReadLine();
      process.BeginErrorReadLine();
      process.WaitForExit();

      // Delete temporary .exe copy
      File.Delete(filePath);
    }

    private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine) {
      if(outLine.Data == null)
        return;

      // Fetch current status from gifski output
      Regex reg = new Regex(@"^Frame ([0-9]+) / ([0-9]+)  [#_\.]* ([0-9a-z]+) ");
      Match mat = reg.Match(outLine.Data);
      if(mat.Success) {
        // Get the current percentage from frame count
        double current = Convert.ToDouble(mat.Groups[1].Value);
        double total = Convert.ToDouble(mat.Groups[2].Value);
        // We multiply by 50 because we only have 50% left (initial 50% were from ffmpeg)
        double percentage = (current / total) * 50;
        UpdatePercentage((int)percentage + 50);

        // Update progress on taskbar
        // Via http://physudo-e.blogspot.com/2013/09/c-wpf-show-progress-in-task-bar.html
        Dispatcher.Invoke(() => {
          TaskbarItemInfo.ProgressValue = (percentage + 50) / 100;
        });

        UpdateETA("About " + mat.Groups[3].Value + " left");
      }
    }

    private static void ErrorHandler(object sendingProcess, DataReceivedEventArgs outLine) {
      if(outLine.Data == null)
        return;

      Console.WriteLine("!  " + outLine.Data);
    }

    private void OnImageMouseDown(object sender, MouseButtonEventArgs e) {
      // Drag image from inside of the application to the outside
      // Via https://social.msdn.microsoft.com/Forums/windows/en-US/f57ffd5d-0fe3-4f64-bfd6-428f58998603/drag-and-drop-between-my-application-and-windows-explorer#625980f0-2301-4236-be46-824815470d24

      // Put the full file path in a string array
      string[] files = new string[1] { outputFullPath };
      // Create a DataObject holding this array as a filedrop
      DataObject data = new DataObject(DataFormats.FileDrop, files);
      // Add the selection as textdata
      // [Note: I'm personally not sure what this does, but it doesn't work without it]
      data.SetData(DataFormats.StringFormat, files[0]);
      // Call to DoDragDrop, which makes the magic happen
      DragDrop.DoDragDrop((DependencyObject)e.Source, data, DragDropEffects.Copy);
    }

    private void OnClickCopy(object sender, RoutedEventArgs e) {
      // Save output file to clipboard
      Clipboard.SetFileDropList(new StringCollection { outputFullPath });
      copyFeedback.Opacity = 1;

      // Fade text out
      Fade(new DoubleAnimation {
        From = 1.0,
        To = 0.0,
        Duration = new Duration(new TimeSpan(0, 0, 2))
      }, copyFeedback.Name);
    }

    private void OnClickSaveAs(object sender, RoutedEventArgs e) {
      // Define file format options, default filename
      SaveFileDialog saveFileDialog = new SaveFileDialog {
        Filter = "High Quality Gifski™ GIF (*.gif)|*.gif",
        DefaultExt = "*.gif",
        FileName = "file.gif"
      };
      if(saveFileDialog.ShowDialog() == true) {
        File.Copy(outputFullPath, saveFileDialog.FileName, true);
      }
    }

    private void OnProgressWindowClosed(object sender, EventArgs e) {
      //Directory.Delete(outputFolderPath, true);
      // Errors out, it's technically still open :(
    }


    // Via https://stackoverflow.com/a/14488941/4824627
    static readonly string[] SizeSuffixes =
                   { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
    static string SizeSuffix(long value, int decimalPlaces = 1) {
      if(decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
      if(value < 0) { return "-" + SizeSuffix(-value); }
      if(value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

      // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
      int mag = (int)Math.Log(value, 1024);

      // 1L << (mag * 10) == 2 ^ (10 * mag) 
      // [i.e. the number of bytes in the unit corresponding to mag]
      decimal adjustedSize = (decimal)value / (1L << (mag * 10));

      // make adjustment when the value is large enough that
      // it would round up to 1000 or more
      if(Math.Round(adjustedSize, decimalPlaces) >= 1000) {
        mag += 1;
        adjustedSize /= 1024;
      }

      return string.Format("{0:n" + decimalPlaces + "} {1}",
          adjustedSize,
          SizeSuffixes[mag]);
    }
  }
}
