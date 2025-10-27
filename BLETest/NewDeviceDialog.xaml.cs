using QRCoder;
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

namespace BLETest
{
    /// <summary>
    /// NewDeviceDialog.xaml の相互作用ロジック
    /// </summary>
    public partial class NewDeviceDialog : Window
    {
        public string DeviceId { get; set;  }
        public string publicKey { get; set; }
        public NewDeviceDialog(string deviceId, string publicKey)
        {
            InitializeComponent();
            DeviceId = deviceId;
            this.publicKey = publicKey;

            using (var qrGenerator = new QRCodeGenerator())
            {
                var qrCodeData = qrGenerator.CreateQrCode($"{deviceId},{publicKey}", QRCodeGenerator.ECCLevel.Q);
                var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeBytes = qrCode.GetGraphic(20);
                using (var ms = new System.IO.MemoryStream(qrCodeBytes))
                {
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = ms;
                    bitmapImage.EndInit();
                    QrImg.Source = bitmapImage;
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void Window_Initialized(object sender, EventArgs e)
        {

        }
    }
}
