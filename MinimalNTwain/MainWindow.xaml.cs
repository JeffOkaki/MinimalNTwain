using NTwain;
using NTwain.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MinimalNTwain
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Scanner Scanner { get; set;}

        public MainWindow()
        {
            InitializeComponent();
            CreateScanner();
        }

        public void CreateScanner()
        {
            Scanner = new Scanner(this);
            Scanner.ScanningFinshed += DisplayImage;
        }

        public void Scan_Click(object sender, RoutedEventArgs e) => Scanner.Scan();

        public void DisplayImage(object sender, bool scanDone)
        {
            Dispatcher.Invoke(() =>
            {
                TheImage.Source = Scanner.CapturedImages.First();
                TheImage.Height = Scanner.CapturedImages.First().Height;
                TheImage.Width = Scanner.CapturedImages.First().Width;
            });
        }
    }
}
