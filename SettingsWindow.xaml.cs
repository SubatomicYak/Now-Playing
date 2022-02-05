using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Forms;

namespace NowPlaying
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private Boolean changes = false;
        public SettingsWindow()
        {
            InitializeComponent();
            OutputTextbox.Text = Properties.Settings.Default.fileOutput;
        }
        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            SaveContents();
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            if( changes)
            {
                if (System.Windows.MessageBox.Show(
                    "You have unsaved changes. Continue?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                ) == MessageBoxResult.Yes) { 
                    this.Close();
                }
            }
            else
            {
                this.Close();
            }
        }

        private void FilePath_Loaded(object sender, RoutedEventArgs e)
        {
            this.FilePath.Text = Properties.Settings.Default.filePath;
        }
        private void MediaPropertiesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.OutputTextbox.SelectedText = $"${{{( e.AddedItems[0] as ComboBoxItem ).Content as String}}}";
        }
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if( SaveContents()) this.Close();
        }
        private void OutputTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            changes = true;
        }

        private void SaveFilePathButton_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "txt files(*.txt)| *.txt";
            saveFileDialog.InitialDirectory = Properties.Settings.Default.filePath;

            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                this.FilePath.Text = saveFileDialog.FileName;
                changes = true;
            }
        }
        private void WillSaveFile_Loaded(object sender, RoutedEventArgs e)
        {
            this.WillSaveFile.IsChecked = Properties.Settings.Default.willSaveFile;
        }

        private void WillSaveFile_Checked(object sender, RoutedEventArgs e)
        {
            changes = true;
        }

        private Boolean SaveContents()
        {
            //save willSaveFile
            Properties.Settings.Default.willSaveFile = (this.WillSaveFile.IsChecked == true);
            //save file path, an empty string is a relative path.
            if (this.FilePath.Text == "" || System.IO.File.Exists(this.FilePath.Text))
            {
                Properties.Settings.Default.filePath = this.FilePath.Text;
            }
            else
            {
                System.Windows.MessageBox.Show(
                    "Invalid File Path",
                    "File Path Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    MessageBoxResult.OK
                );
                return false;
            }
            //save output text
            Properties.Settings.Default.fileOutput = OutputTextbox.Text;
            Properties.Settings.Default.Save();
            return true;
        }

    }
}
