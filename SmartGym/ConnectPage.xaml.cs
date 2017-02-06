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
    public sealed partial class ConnectPage : Page
    {
        private Windows.UI.Core.CoreDispatcher messageDispatcher = Window.Current.CoreWindow.Dispatcher;
        private ProximityDevice device;
        private bool subscribedToNfc;
        private long subscriptionId;
        private bool isConnecting;
        private IMobileServiceTable<Devices> devicesTable = App.MobileService.GetTable<Devices>();

        public ConnectPage()
        {
            this.InitializeComponent();

            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                HardwareButtons.BackPressed += BackRequested;
            }

            isConnecting = false;

            touchCheckImage.Visibility = Visibility.Collapsed;
            connectingTextBlock.Visibility = Visibility.Collapsed;
            connectingCheckImage.Visibility = Visibility.Collapsed;

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

            if (!isConnecting)
            {
                this.Frame.Navigate(typeof(MenuPage), null);
            }
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

            CurrentSession.deviceName = split[0];
            CurrentSession.type = split[1];
            isConnecting = true;

            // Run ConnectAndNavigate on the UI thread.
            await messageDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                async () =>
                {
                    nfcGuideImage.Visibility = Visibility.Collapsed;
                    touchCheckImage.Visibility = Visibility.Visible;
                    await ConnectAndNavigate(split[0]);
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

        private async Task ConnectAndNavigate(string bluetoothName)
        {
            Devices dev = null;
            try
            {
                List<Devices> items = await devicesTable.
                       Where(devicesdata => (devicesdata.Name == bluetoothName)).ToListAsync();
                if (items.Count() == 0)
                {
                    await OnConnectionError("The device was not found in the system");
                    return;
                }

                dev = items.First();
                if (dev.isTaken)
                {
                    await OnConnectionError("The device is taken");
                    return;
                }
            }
            catch (Exception)
            {
                await OnConnectionError("There is no internet connection");
                return;
            }

            bool result;

            connectingTextBlock.Visibility = Visibility.Visible;

            if (!await BluetoothConnection.bluetooth.IsBluetoothTurnedOn())
            {
                debugTextBlock.Text = "Making sure that Bluetooth is on...";
                result = await BluetoothConnection.bluetooth.TurnOnBluetooth();
                debugTextBlock.Text += " " + result + "\n";

                if (!result)
                {
                    await OnConnectionError("Could not turn on Bluetooth");
                    return;
                }

                await Task.Delay(1000);
            }

            debugTextBlock.Text += "Connecting...";
            result = await BluetoothConnection.bluetooth.ConnectToBluetooth(bluetoothName);
            debugTextBlock.Text += " " + result + "\n";

            if (!result)
            {
                debugTextBlock.Text += "Pairing...";
                result = await BluetoothConnection.bluetooth.PairBluetooth(bluetoothName);
                debugTextBlock.Text += " " + result + "\n";

                if (result)
                {
                    await Task.Delay(1000);

                    debugTextBlock.Text += "Connecting again...";
                    result = await BluetoothConnection.bluetooth.ConnectToBluetooth(bluetoothName);
                    debugTextBlock.Text += " " + result + "\n";
                }

                if (!result)
                {
                    await BluetoothConnection.bluetooth.TurnOffBluetooth();
                    await OnConnectionError("Could not connect to Bluetooth");
                    return;
                }
            }

            //debugTextBlock.Text += "Testing stuff...";
            //result = await BluetoothTest();
            //debugTextBlock.Text += " " + result + "\n";

            //await Task.Delay(3000);

            // Mark device as taken
            dev.isTaken = true;
            dev.TotalUses++;
            try
            {
                await devicesTable.UpdateAsync(dev);
            }
            catch (Exception)
            {
                await BluetoothConnection.bluetooth.CancelAndDisconnectBluetooth();
                await BluetoothConnection.bluetooth.TurnOffBluetooth();
                await OnConnectionError("There is no internet connection");
                return;
            }
            
            connectingCheckImage.Visibility = Visibility.Visible;
            await Task.Delay(200);

            this.Frame.Navigate(typeof(StartPage), null);
        }

        private async Task OnConnectionError(string msg)
        {
            var dialog = new MessageDialog(msg);
            await dialog.ShowAsync();
            this.Frame.Navigate(typeof(MenuPage), null);
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
