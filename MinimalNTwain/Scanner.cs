using System;
using System.Collections.Generic;
using System.Linq;
using NTwain;
using NTwain.Data;
using System.IO;
using System.Reflection;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;

namespace MinimalNTwain
{
    public class Scanner
    {
        private TwainSession Session { get; set; }
        public List<ImageSource> CapturedImages { get; set; }
        private IntPtr WindowHandle { get; set; }
        public event EventHandler<bool> ScanningFinshed;
        
        public Scanner(Window window)
        {
            CapturedImages = new List<ImageSource>();
            WindowHandle = new WindowInteropHelper(window).Handle;
        }

        public void Scan()
        {
            var applicationID = TWIdentity.CreateFromAssembly(DataGroups.Image, Assembly.GetExecutingAssembly());
            Session = new TwainSession(applicationID);
            Session.TransferReady += Session_TransferReady;
            Session.DataTransferred += Session_DataTransferred;
            Session.SourceDisabled += Session_SourceDisabled;
            Session.Open();
            DataSource dataSource = Session.FirstOrDefault();
            dataSource.Open();
            dataSource.Enable(SourceEnableMode.NoUI, false, WindowHandle);
        }

        private void Session_SourceDisabled(object sender, EventArgs e)
        {
            OnScanningFinshed();
        }

        protected virtual void OnScanningFinshed()
        {
            ScanningFinshed?.Invoke(this, true);
        }

        private void Session_TransferReady(object sender, TransferReadyEventArgs e)
        {
            var transferMechanism = Session.CurrentSource.Capabilities.ICapXferMech.GetCurrent();
            if (transferMechanism == XferMech.File)
            {
                var formats = Session.CurrentSource.Capabilities.ICapImageFileFormat.GetValues();
                var scannerImageFormat = formats.Contains(FileFormat.Tiff) ? FileFormat.Tiff : FileFormat.Bmp;

                var fileSetup = new TWSetupFileXfer
                {
                    Format = scannerImageFormat,
                    FileName = GetUniqueName(Path.GetTempPath(), "twain-test", "." + scannerImageFormat)
                };
            }
        }

        private void Session_DataTransferred(object sender, DataTransferredEventArgs e)
        {
            ImageSource image = GenerateThumbnail(e);
            if (image != null)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    CapturedImages.Add(image);
                }));
            }
        }

        private string GetUniqueName(string dir, string name, string ext)
        {
            var filePath = Path.Combine(dir, name + ext);
            int next = 1;
            while (File.Exists(filePath))
            {
                filePath = Path.Combine(dir, string.Format("{0} ({1}){2}", name, next++, ext));
            }
            return filePath;
        }

        private ImageSource GenerateThumbnail(DataTransferredEventArgs e)
        {
            BitmapSource image = null;

            switch (e.TransferType)
            {
                case XferMech.Native:
                    using (var stream = e.GetNativeImageStream())
                    {
                        if (stream != null)
                        {
                            image = stream.ConvertToWpfBitmap(300, 0);
                        }
                    }
                    break;
                case XferMech.File:
                    image = new BitmapImage(new Uri(e.FileDataPath));
                    if (image.CanFreeze)
                    {
                        image.Freeze();
                    }
                    break;
                case XferMech.Memory:
                    break;
            }

            return image;
        }
    }
}
