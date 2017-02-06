using Microsoft.WindowsAzure.MobileServices;
using NfcNdefTagReader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Networking.Proximity;
using Windows.Networking.Sockets;
using Windows.Phone.UI.Input;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SmartGym
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AdminScanPage : Page
    {
        private Windows.UI.Core.CoreDispatcher messageDispatcher = Window.Current.CoreWindow.Dispatcher;
        private ProximityDevice device;
        private bool subscribedToNfc;
        private long subscriptionId;
        private string scannedName, scannedType;
        private IMobileServiceTable<Devices> devicesTable = App.MobileService.GetTable<Devices>();

        public AdminScanPage()
        {
            this.InitializeComponent();

            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                HardwareButtons.BackPressed += BackRequested;
            }
            
            touchCheckImage.Visibility = Visibility.Collapsed;
            notFoundTextBlock.Visibility = Visibility.Collapsed;
            devNameTextBlock.Visibility = Visibility.Collapsed;
            addDevButton.Visibility = Visibility.Collapsed;

            debugTextBlock.Visibility = Visibility.Collapsed;

            device = ProximityDevice.GetDefault();
            if (!SubscribeToNFC())
            {
                var task = OnConnectionError("Could not subscribe to NFC");
                return;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                HardwareButtons.BackPressed -= BackRequested;
            }

            if (subscribedToNfc)
            {
                UnsubscribeFromNFC();
            }
        }

        private void BackRequested(object sender, BackPressedEventArgs e)
        {
            e.Handled = true;
            this.Frame.Navigate(typeof(AdminDevicesPage), null);
        }

        private bool SubscribeToNFC()
        {
            try
            {
                //device.DeviceArrived += DeviceArrived;
                subscriptionId = device.SubscribeForMessage("NDEF", MessageReceived);
                subscribedToNfc = true;
                return true;
            }
            catch(Exception)
            {
                return false;
            }
        }

        private bool UnsubscribeFromNFC()
        {
            if (!subscribedToNfc)
            {
                return false;
            }

            try
            {
                device.StopSubscribingForMessage(subscriptionId);
                //device.DeviceArrived -= DeviceArrived;
                subscribedToNfc = false;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async void MessageReceived(ProximityDevice sender, ProximityMessage message)
        {
            string text = GetNfcText(message);

            Debug.WriteLine("Detected NFC text: " + text);

            string[] split = text.Split(new char[] { '@' }, 2);

            if (split.Length < 2 ||
                (split[1] != "Strap" &&
                split[1] != "Weights" &&
                split[1] != "Machines"))
            {
                return;
            }

            if (subscribedToNfc)
            {
                UnsubscribeFromNFC();
            }

            scannedName = split[0];
            scannedType = split[1];

            // Run HandleTouchedDevice on the UI thread.
            await messageDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                async () =>
                {
                    nfcGuideImage.Visibility = Visibility.Collapsed;
                    touchCheckImage.Visibility = Visibility.Visible;
                    await HandleTouchedDevice();
                });
        }

        private string GetNfcText(ProximityMessage message)
        {
            using (var buf = DataReader.FromBuffer(message.Data))
            {
                var recordList = new List<NdefRecord>();
                NdefRecordUtility.ReadNdefRecord(buf, recordList);

                for (int i = 0; i < recordList.Count; i++)
                {
                    NdefRecord record = recordList.ElementAt(i);

                    if (!record.IsSpRecord)
                    {
                        if (System.Text.Encoding.UTF8.GetString(record.Type, 0, record.TypeLength) == "Sp")
                        {
                            //output = output + "\n --End of Record No." + recordNumber; spRecordNumber = 0;
                            continue;
                        }
                        else
                        {
                            //recordNumber++;
                            //output = output + "\n --Record No." + recordNumber;
                        }
                    }
                    else
                    {
                        //if (spRecordNumber == 0)
                        //{
                        //    recordNumber++;
                        //    output = output + "\n --Record No." + recordNumber;
                        //}
                        //
                        //spRecordNumber++;
                        //output = output + "\n Sp sub-record No." + spRecordNumber;
                    }

                    //output = output + "\n MB:" + ((record.Mb) ? "1;" : "0;");
                    //output = output + " ME:" + ((record.Me) ? "1;" : "0;");
                    //output = output + " CF:" + ((record.Cf) ? "1;" : "0;");
                    //output = output + " SR:" + ((record.Sr) ? "1;" : "0;");
                    //output = output + " IL:" + ((record.Il) ? "1;" : "0;");

                    //string typeName = NdefRecordUtility.GetTypeNameFormat(record);
                    //
                    //if (record.TypeLength > 0)
                    //{
                    //    output = output + "\n Type: " + typeName + ":"
                    //        + System.Text.Encoding.UTF8.GetString(record.Type, 0, record.TypeLength);
                    //}
                    //
                    //if ((record.Il) && (record.IdLength > 0))
                    //{
                    //    output = output + "\n Id:"
                    //        + System.Text.Encoding.UTF8.GetString(record.Id, 0, record.IdLength);
                    //}

                    if ((record.PayloadLength > 0) && (record.Payload != null))
                    {
                        if ((record.Tnf == 0x01)
                            && (System.Text.Encoding.UTF8.GetString(record.Type, 0, record.TypeLength) == "U"))
                        {
                            //NdefUriRtd uri = new NdefUriRtd();
                            //NdefRecordUtility.ReadUriRtd(record, uri);
                            //output = output + "\n Uri: " + uri.GetFullUri();
                        }
                        else if ((record.Tnf == 0x01)
                            && (System.Text.Encoding.UTF8.GetString(record.Type, 0, record.TypeLength) == "T"))
                        {
                            NdefTextRtd text = new NdefTextRtd();
                            NdefRecordUtility.ReadTextRtd(record, text);
                            //output = output + "\n Language: " + text.Language;
                            //output = output + "\n Encoding: " + text.Encoding;
                            //output = output + "\n Text: " + text.Text;

                            return text.Text;
                        }
                        else
                        {
                            //if (record.Tnf == 0x01)
                            //{
                            //    output = output + "\n Payload:"
                            //        + System.Text.Encoding.UTF8.GetString(record.Payload, 0, record.Payload.Length);
                            //}
                        }
                    }

                    //if (!record.IsSpRecord)
                    //{
                    //    output = output + "\n --End of Record No." + recordNumber;
                    //}
                }
            }

            return "";
        }

        private async Task HandleTouchedDevice()
        {
            try
            {
                List<Devices> items = await devicesTable.
                       Where(devicesdata => (devicesdata.Name == scannedName)).ToListAsync();
                if (items.Count() == 0)
                {
                    // The device does not exist.
                    notFoundTextBlock.Visibility = Visibility.Visible;
                    devNameTextBlock.Visibility = Visibility.Visible;
                    addDevButton.Visibility = Visibility.Visible;

                    devNameTextBlock.Text += scannedName;
                    return;
                }

                CurrentDevice.currDevice = items.First();
            }
            catch (Exception)
            {
                await OnConnectionError("There is no internet connection");
                return;
            }

            this.Frame.Navigate(typeof(DevicePage), null);
        }

        private async Task OnConnectionError(string msg)
        {
            var dialog = new MessageDialog(msg);
            await dialog.ShowAsync();
            this.Frame.Navigate(typeof(AdminDevicesPage), null);
        }

        private async void addDevButton_Click(object sender, RoutedEventArgs e)
        {
            var newDevice = new Devices {
                Name = scannedName,
                TotalUses = 0,
                isTaken = false,
                Type = scannedType
            };
            await devicesTable.InsertAsync(newDevice);
            CurrentDevice.currDevice = newDevice;
            this.Frame.Navigate(typeof(DevicePage), null);
        }

        /*private async Task<bool> BluetoothTest()
        {
            bool succeeded = false;

            try
            {
                var writer = new DataWriter(BluetoothConnection.bluetoothSocket.OutputStream);

                writer.WriteString("oneTESTone");

                var bytesSent = await writer.StoreAsync();

                var reader = new DataReader(BluetoothConnection.bluetoothSocket.InputStream);
                //reader.InputStreamOptions = InputStreamOptions.Partial;

                var bytesToRead = await reader.LoadAsync(bytesSent + 1);
                var sentString = reader.ReadString(bytesToRead);

                if(sentString == "ONETESTONE\n")
                {
                    succeeded = true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BluetoothTest: " + ex.Message);
            }

            return succeeded;
        }*/
    }
}
