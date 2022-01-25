using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using Windows.Media.Control;
using MediaManager = Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager;
using MediaSession = Windows.Media.Control.GlobalSystemMediaTransportControlsSession;
using TransportManager = Windows.Media.Control.GlobalSystemMediaTransportControlsSession;


namespace NowPlaying
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // TODO: Cleanup
        private MediaSession currentSession;
        private MediaManager sessionManager;

        private NotifyIcon ni;
        private ContextMenuStrip contextMenu;

        private Boolean close = false;
        public MainWindow()
        {
            InitializeComponent();
        }

        //On application load set up the media listener to detect the song being changed.
        private async void TextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            sessionManager = await MediaManager.RequestAsync();
            currentSession = sessionManager.GetCurrentSession();

            // Create Context Menu for the Notification Icon
            this.contextMenu = new ContextMenuStrip();

            ToolStripMenuItem item = new ToolStripMenuItem("Exit");
            item.Name = "Exit";
            item.Click += new EventHandler((obj,args)=>
            {
                close = true;
                this.Close();
            });
            contextMenu.Items.Add(item);

            ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = Properties.Resources.appicon;
            ni.Visible = true;
            this.ni.Click += (object o, EventArgs e) => {
                System.Windows.Forms.MouseEventArgs me = (System.Windows.Forms.MouseEventArgs) e;
                if(me.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    if(WindowState == WindowState.Minimized)
                    {
                        Show();
                        WindowState = WindowState.Normal;
                        this.Activate();
                    } else
                    {
                        Show();
                        WindowState = System.Windows.WindowState.Normal;
                    }
                }
                else if(me.Button == System.Windows.Forms.MouseButtons.Right){
                    contextMenu.Show();
                }
            };
            ni.ContextMenuStrip = contextMenu;

            // Create media listener
            currentSession.MediaPropertiesChanged += (GlobalSystemMediaTransportControlsSession s, MediaPropertiesChangedEventArgs e) =>
            {
                UpdateSong();
            };
            UpdateSong();
        }

        // Minimize to system tray when application is closed.
        protected override void OnClosing(CancelEventArgs e)
        {   
            // if the application is closed not from the right-click menu hide it instead
            if(!close){
                e.Cancel = true;
                this.Hide();
            }
            base.OnClosing(e);
        }
        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UpdateSong();
        }

        private void Branding_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var destinationurl = "https://github.com/SubatomicYak/Now-Playing";
            var sInfo = new System.Diagnostics.ProcessStartInfo(destinationurl)
            {
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(sInfo);
        }
        
        private async void UpdateSong()
        {
            var details = await GetSongDetails();
            OutputText(details);
            //  asyncronusly update the song details
            this.Dispatcher.Invoke(() =>
            {
                this.NowPlayingText.Text = details;
                this.ni.Text = details;
            });
        }

        //Asyncronously gets the songs details from the w10 media manager
        private async Task<string> GetSongDetails()
        {
            try
            {
                if (currentSession is object)
                {
                    var info = await currentSession.TryGetMediaPropertiesAsync();

                    return $"🎵: {info?.Title}\n🎤: {info?.Artist}";
                }
                return "🎵:\n🎤:";
            }
            catch (Exception err)
            {
                return err.Message;
            }
        }

        private async void OutputText( string text )
        {
            if (Properties.Settings.Default.willSaveFile)
            {
                string[] output = {
                    text
                };
                await File.WriteAllLinesAsync(Properties.Settings.Default.filePath, output );
            }
        }

        private void WillSaveFile_Loaded(object sender, RoutedEventArgs e)
        {
            this.WillSaveFile.IsChecked = Properties.Settings.Default.willSaveFile;
        }

        private void WillSaveFile_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.willSaveFile = (this.WillSaveFile.IsChecked == true);
            Properties.Settings.Default.Save();
            //Is there a better way? Like binding the isEnabled state of a control to a setting?
            this.SaveFilePathButton.IsEnabled = (this.WillSaveFile.IsChecked == true);
        }

        private void FilePath_Loaded(object sender, RoutedEventArgs e)
        {
            this.FilePath.Text = Properties.Settings.Default.filePath;
        }

        private void FilePath_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        private void SaveFilePathButton_Loaded(object sender, RoutedEventArgs e)
        {
            this.SaveFilePathButton.IsEnabled = Properties.Settings.Default.willSaveFile;
        }
        private void SaveFilePathButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "txt files(*.txt)| *.txt";
            saveFileDialog.InitialDirectory = Properties.Settings.Default.filePath;

            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                this.FilePath.Text = saveFileDialog.FileName;
                Properties.Settings.Default.filePath = saveFileDialog.FileName;
                Properties.Settings.Default.Save();
            }
        }

    }
}
