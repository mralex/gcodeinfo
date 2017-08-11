using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace GCodeInfo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

        }

        private void loadButtonClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FileName;
                string filename = System.IO.Path.GetFileName(path);

                fileLabel.Content = filename;

                ParseFile(path);
            }
        }

        private void ParseFile(string filename)
        {
            GCodeParser parser = new GCodeParser(filename);

            // TODO Handle exceptions that we'll figure out later...

            parser.Parse();

            layersLabel.Content = parser.Model.Layers.ToString();
            filamentLabel.Content = $"{parser.Model.TotalFilament.ToString("N2")}mm";

            widthLabel.Content = $"{parser.Model.Width.ToString("N2")}mm";
            heightLabel.Content = $"{parser.Model.Height.ToString("N2")}mm";
            depthLabel.Content = $"{parser.Model.Depth.ToString("N2")}mm";
        }
    }
}
