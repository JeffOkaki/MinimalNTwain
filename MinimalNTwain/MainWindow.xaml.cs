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
        public MainWindow()
        {
            InitializeComponent();
            CapturedImages = new List<ImageSource>();
            DoTwainStuff();
        }

        TwainSession Session { get; set; }
        List<ImageSource> CapturedImages { get; set; }

        public void DoTwainStuff()
        {
            var appId = TWIdentity.CreateFromAssembly(DataGroups.Image, Assembly.GetExecutingAssembly());
            Session = new TwainSession(appId);
            Session.TransferReady += Session_TransferReady;
            Session.DataTransferred += Session_DataTransferred;
            Session.Open();
            DataSource myDS = Session.FirstOrDefault();
            myDS.Open();
            myDS.Enable(SourceEnableMode.NoUI, false, new WindowInteropHelper(this).Handle);
        }

        void Session_TransferReady(object sender, TransferReadyEventArgs e)
        {
            var mech = Session.CurrentSource.Capabilities.ICapXferMech.GetCurrent();
            if (mech == XferMech.File)
            {
                var formats = Session.CurrentSource.Capabilities.ICapImageFileFormat.GetValues();
                var wantFormat = formats.Contains(FileFormat.Tiff) ? FileFormat.Tiff : FileFormat.Bmp;

                var fileSetup = new TWSetupFileXfer
                {
                    Format = wantFormat,
                    FileName = GetUniqueName(System.IO.Path.GetTempPath(), "twain-test", "." + wantFormat)
                };
            }
            else if (mech == XferMech.Memory)
            {
                // ?

            }
        }

        void Session_DataTransferred(object sender, DataTransferredEventArgs e)
        {
            ImageSource img = GenerateThumbnail(e);
            if (img != null)
            {
                App.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    CapturedImages.Add(img);
                    TheImage.Source = CapturedImages.First();
                }));
            }
        }




        string GetUniqueName(string dir, string name, string ext)
        {
            var filePath = System.IO.Path.Combine(dir, name + ext);
            int next = 1;
            while (File.Exists(filePath))
            {
                filePath = System.IO.Path.Combine(dir, string.Format("{0} ({1}){2}", name, next++, ext));
            }
            return filePath;
        }

        void _session_DataTransferred(object sender, DataTransferredEventArgs e)
        {
            ImageSource img = GenerateThumbnail(e);
            if (img != null)
            {
                App.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    CapturedImages.Add(img);
                }));
            }
        }

        ImageSource GenerateThumbnail(DataTransferredEventArgs e)
        {
            BitmapSource img = null;

            switch (e.TransferType)
            {
                case XferMech.Native:
                    using (var stream = e.GetNativeImageStream())
                    {
                        if (stream != null)
                        {
                            img = stream.ConvertToWpfBitmap(300, 0);
                        }
                    }
                    break;
                case XferMech.File:
                    img = new BitmapImage(new Uri(e.FileDataPath));
                    if (img.CanFreeze)
                    {
                        img.Freeze();
                    }
                    break;
                case XferMech.Memory:
                    // TODO: build current image from multiple data-xferred event
                    break;
            }

            //if (img != null)
            //{
            //    // from http://stackoverflow.com/questions/18189501/create-thumbnail-image-directly-from-header-less-image-byte-array
            //    var scale = MaxThumbnailSize / img.PixelWidth;
            //    var transform = new ScaleTransform(scale, scale);
            //    var thumbnail = new TransformedBitmap(img, transform);
            //    img = new WriteableBitmap(new TransformedBitmap(img, transform));
            //    img.Freeze();
            //}
            return img;
        }



    }
}
