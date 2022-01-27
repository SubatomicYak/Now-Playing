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


namespace NowPlaying
{
    /// <summary>
    /// Main window of the application. Displays Now Playing information, while also outputting it to file.
    /// </summary>
    public partial class MainWindow : Window
    {
        private MediaSession currentSession;
        private MediaManager sessionManager;

        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenu;

        private Boolean programWillClose = false;
        public MainWindow()
        {
            InitializeComponent();

            // Create Context Menu for the Notification Icon
            contextMenu = new ContextMenuStrip();

            ToolStripMenuItem item = new ToolStripMenuItem("Exit");
            item.Name = "Exit";
            item.Click += new EventHandler((obj,args)=>
            {
                programWillClose = true;
                this.Close();
            });
            contextMenu.Items.Add(item);

            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Icon = Properties.Resources.appicon;
            notifyIcon.Visible = true;
            notifyIcon.Click += (object o, EventArgs e) => {
                var me = (System.Windows.Forms.MouseEventArgs) e;
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
            notifyIcon.ContextMenuStrip = contextMenu;
        }

        // Capture close event and check if its in tray, to do so. If not move to tray.
        protected override void OnClosing(CancelEventArgs e)
        {   
            if(!programWillClose){
                e.Cancel = true;
                this.Hide();
            }
            base.OnClosing(e);
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

        private void FilePath_Loaded(object sender, RoutedEventArgs e)
        {
            this.FilePath.Text = Properties.Settings.Default.filePath;
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

        //On application load set up the media listener to detect the song being changed.
        private async void TextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize mediamanagers and create listeners
            sessionManager = await MediaManager.RequestAsync();
            currentSession = sessionManager.GetCurrentSession();

            currentSession.MediaPropertiesChanged += (GlobalSystemMediaTransportControlsSession s, MediaPropertiesChangedEventArgs e) =>
            {
                UpdateSong();
            };
            UpdateSong();
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UpdateSong();
        }
        
        private void WillSaveFile_Loaded(object sender, RoutedEventArgs e)
        {
            this.WillSaveFile.IsChecked = Properties.Settings.Default.willSaveFile;
        }

        private void WillSaveFile_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.willSaveFile = (this.WillSaveFile.IsChecked == true);
            Properties.Settings.Default.Save();
        }

        private async void UpdateSong()
        {
            var details = await GetSongDetails();
            OutputText(details);
            //  asyncronusly update the song details
            this.Dispatcher.Invoke(() =>
            {
                this.NowPlayingText.Text = details;
                //quick hack to fix the NotifyIcon limit bug. A more couth fix is needed.
                notifyIcon.Text = Truncate(details, 60, "...");
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

        public static string Truncate(string str, int length, string append = "")
        {
            if(str.Length > length)
            {
                return str.Substring(0, length) + append;
            }
            return str;
        }

    }
}
