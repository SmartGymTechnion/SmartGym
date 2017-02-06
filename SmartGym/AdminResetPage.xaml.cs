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
    public sealed partial class AdminResetPage : Page
    {
        private Task resetTask = null;
        private bool doneConnecting = false;

        public AdminResetPage()
        {
            this.InitializeComponent();

            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                HardwareButtons.BackPressed += BackRequested;
            }

            resetTask = ResetDevice();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                HardwareButtons.BackPressed -= BackRequested;
            }
        }

        private async void BackRequested(object sender, BackPressedEventArgs e)
        {
            e.Handled = true;

            if (doneConnecting)
            {
                if (resetTask.Status != TaskStatus.RanToCompletion)
                {
                    await BluetoothConnection.bluetooth.CancelAndDisconnectBluetooth();
                    await BluetoothConnection.bluetooth.TurnOffBluetooth();
                    resetTask.Wait();
                }

                this.Frame.Navigate(typeof(DevicePage), null);
            }
        }

        private async Task ResetDevice()
        {
            var bluetoothName = CurrentDevice.currDevice.Name;

            bool result;

            if (!await BluetoothConnection.bluetooth.IsBluetoothTurnedOn())
            {
                AddAndScroll(outputListView, "Making sure that Bluetooth is on...");
                result = await BluetoothConnection.bluetooth.TurnOnBluetooth();
                AddAndScroll(outputListView, "    " + result.ToString());

                if (!result)
                {
                    await OnConnectionError("Could not turn on Bluetooth");
                    return;
                }
                
                await Task.Delay(1000);
            }

            AddAndScroll(outputListView, "Connecting to " + bluetoothName + "...");
            result = await BluetoothConnection.bluetooth.ConnectToBluetooth(bluetoothName);
            AddAndScroll(outputListView, "    " + result.ToString());

            if (!result)
            {
                AddAndScroll(outputListView, "Pairing...");
                result = await BluetoothConnection.bluetooth.PairBluetooth(bluetoothName);
                AddAndScroll(outputListView, "    " + result.ToString());

                if (result)
                {
                    await Task.Delay(1000);

                    AddAndScroll(outputListView, "Connecting again...");
                    result = await BluetoothConnection.bluetooth.ConnectToBluetooth(bluetoothName);
                    AddAndScroll(outputListView, "    " + result.ToString());
                }

                if (!result)
                {
                    await BluetoothConnection.bluetooth.TurnOffBluetooth();
                    await OnConnectionError("Could not connect to Bluetooth");
                    return;
                }
            }

            doneConnecting = true;

            AddAndScroll(outputListView, "Connected to " + bluetoothName + "!");

            bool sendOk = await BluetoothConnection.bluetooth.BluetoothSendCommand("Reset", "OkReset");
            if (!sendOk)
            {
                await BluetoothConnection.bluetooth.CancelAndDisconnectBluetooth();
                await BluetoothConnection.bluetooth.TurnOffBluetooth();
                await OnConnectionError("BluetoothSendCommand failed");
                return;
            }

            var reply = await BluetoothConnection.bluetooth.BluetoothReadString();
            while (reply != "ResetComplete")
            {
                if (reply == "")
                {
                    await BluetoothConnection.bluetooth.CancelAndDisconnectBluetooth();
                    await BluetoothConnection.bluetooth.TurnOffBluetooth();
                    await OnConnectionError("BluetoothReadString failed");
                    return;
                }

                AddAndScroll(outputListView, reply);

                reply = await BluetoothConnection.bluetooth.BluetoothReadString();
            }

            AddAndScroll(outputListView, reply);
            AddAndScroll(outputListView, "Reset completed!");

            await BluetoothConnection.bluetooth.CancelAndDisconnectBluetooth();
            await BluetoothConnection.bluetooth.TurnOffBluetooth();
        }

        private void AddAndScroll(ListView outputListView, string v)
        {
            outputListView.Items.Add(v);
            outputListView.ScrollIntoView(outputListView.Items[outputListView.Items.Count - 1], ScrollIntoViewAlignment.Leading);
        }

        private async Task OnConnectionError(string msg)
        {
            var dialog = new MessageDialog(msg);
            await dialog.ShowAsync();
            this.Frame.Navigate(typeof(DevicePage), null);
        }
    }
}
