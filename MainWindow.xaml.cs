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
using System.Text.RegularExpressions;
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

        private SettingsWindow settingsWindow;

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
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if( settingsWindow is null || !settingsWindow.IsLoaded )
            {
                settingsWindow = new SettingsWindow();
                settingsWindow.ShowDialog();
            } else
            {
                settingsWindow.ShowDialog();
                settingsWindow.Focus();
            }
            UpdateSong();
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
                    string[] properties = new string[]{"AlbumArtist", "AlbumTitle", "AlbumTrackCount", "Artist" , "Genres", "PlaybackType", "Subtitle", "Thumbnail", "Title", "TrackNumber"};
                    string output = Properties.Settings.Default.fileOutput;
                    //TODO: PLEASE find a better way to do this
                    foreach (string prop in properties)
                    {
                        string propOut = "";
                        switch(prop)
                        {
                            case "AlbumArtist":
                                propOut = info.AlbumArtist;
                                break;
                            case "AlbumTrackCount":
                                propOut = $"{info.AlbumTrackCount}";
                                break;
                            case "AlbumTitle":
                                propOut = info.AlbumTitle;
                                break;
                            case "Artist":
                                propOut = info.Artist;
                                break;
                            case "Genres":
                                foreach (string genre in info.Genres)
                                {
                                    propOut += $"{genre},";
                                }
                                break;
                            case "PlaybackType":
                                propOut = info.PlaybackType.ToString();
                                break;
                            case "Subtitle":
                                propOut = info.Subtitle;
                                break;
                            case "Title":
                                propOut = info.Title;
                                break;
                            case "TrackNumber":
                                propOut = info.TrackNumber.ToString();
                                break;
                        }
                        output = Regex.Replace(output, @"\$\{"+ prop + @"\}", propOut);
                    }
                    return output;
                }
                return "No Media Session Found";
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
