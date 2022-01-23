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
        // TODO: Toggleable text file output 📝
        // TODO: Toggleable dark mode ☀ 🌙
        // TODO: Toggleable minimize/close to tray
        // TODO: Get album artwork https://www.py4u.net/discuss/217991
        // TODO: Customizable text display(?)
        // TODO: Output errors (no media session found)
        // TODO: Get this shit onto GIT
        // TODO: [BUG]: Fix when song removed from library, it doesnt update with next song.
        MediaSession currentSession;
        MediaManager sessionManager;
        public MainWindow()
        {
            InitializeComponent();
        }

        // Minimize to system tray when application is minimized.
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized) this.Hide();
            base.OnStateChanged(e);
        }

        // Minimize to system tray when application is closed.
        protected override void OnClosing(CancelEventArgs e)
        {
            // setting cancel to true will cancel the close request
            // so the application is not closed
            e.Cancel = true;
            this.Hide();
            base.OnClosing(e);
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

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            UpdateSong();
        }

        //On application load set up the media listener to detect the song being changed.
        private async void TextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            sessionManager = await MediaManager.RequestAsync();
            currentSession = sessionManager.GetCurrentSession();

            System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = Properties.Resources.appicon;
            ni.Visible = true;
            ni.Text = "hello";
            ni.DoubleClick +=
                delegate (object sender, EventArgs args)
                {
                    Show();
                    WindowState = System.Windows.WindowState.Normal;
                };

            currentSession.MediaPropertiesChanged += (GlobalSystemMediaTransportControlsSession s, MediaPropertiesChangedEventArgs e) =>
            {
                UpdateSong();
            };
            UpdateSong();
        }

        private async void UpdateSong()
        {
            var details = await GetSongDetails();
            OutputText(details);
            // this was so easy. It was as easy as I assumed it was. I was asking the wrong questions.
            // I want to scream.
            this.Dispatcher.Invoke(() =>
            {
                this.NowPlayingText.Text = details;
            });
        }

        private async void OutputText( string text )
        {
            string[] output = {
                text
            };
            await File.WriteAllLinesAsync("nowplaying.txt", output );
        }
    }

    public class Song : DependencyObject
    {
        public Song() { }
        public Song(string song)
        {
            SongDetails = song;
        }
        public string SongDetails
        {
            get { return (string)GetValue(SongDetailsProperty); }
            set { 
                SetValue(SongDetailsProperty, value);
            }
        }

        public static readonly DependencyProperty SongDetailsProperty =
            DependencyProperty.Register("SongDetails", typeof(string),
            typeof(Song), new PropertyMetadata("Now Playing"));
    }
}
