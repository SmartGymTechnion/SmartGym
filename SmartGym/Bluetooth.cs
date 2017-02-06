using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace SmartGym
{
    public class Bluetooth
    {
        private const string bluetoothPass = "1234";
        private StreamSocket bluetoothSocket;
        private RfcommDeviceService bluetoothService;
        private DataReader bluetoothReader;
        private DataWriter bluetoothWriter;

        public async Task<bool> IsBluetoothTurnedOn()
        {
            var result = await Radio.RequestAccessAsync();
            if (result == RadioAccessStatus.Allowed)
            {
                var bluetooth = (await Radio.GetRadiosAsync()).FirstOrDefault(radio => radio.Kind == RadioKind.Bluetooth);
                if (bluetooth != null)
                {
                    return bluetooth.State == RadioState.On;
                }
            }

            return false;
        }

        public async Task<bool> TurnOnBluetooth()
        {
            var result = await Radio.RequestAccessAsync();
            if (result == RadioAccessStatus.Allowed)
            {
                var bluetooth = (await Radio.GetRadiosAsync()).FirstOrDefault(radio => radio.Kind == RadioKind.Bluetooth);
                if (bluetooth != null)
                {
                    return
                        bluetooth.State == RadioState.On ||
                        (await bluetooth.SetStateAsync(RadioState.On)) == RadioAccessStatus.Allowed;
                }
            }

            return false;
        }

        public async Task<bool> TurnOffBluetooth()
        {
            var result = await Radio.RequestAccessAsync();
            if (result == RadioAccessStatus.Allowed)
            {
                var bluetooth = (await Radio.GetRadiosAsync()).FirstOrDefault(radio => radio.Kind == RadioKind.Bluetooth);
                if (bluetooth != null)
                {
                    return
                        bluetooth.State == RadioState.Off ||
                        (await bluetooth.SetStateAsync(RadioState.Off)) == RadioAccessStatus.Allowed;
                }
            }

            return false;
        }

        public async Task<bool> PairBluetooth(string name)
        {
            bool succeeded = false;

            try
            {
                var devices =
                      await DeviceInformation.FindAllAsync(
                        BluetoothDevice.GetDeviceSelectorFromPairingState(false));

                foreach (var d in devices)
                {
                    Debug.WriteLine("PairBluetooth: found " + d.Name);
                }

                var device = devices.Single(x => x.Name == name);

                var customPairing = device.Pairing.Custom;
                var ceremoniesSelected = DevicePairingKinds.ConfirmOnly | DevicePairingKinds.ProvidePin;
                var protectionLevel = DevicePairingProtectionLevel.Default;

                customPairing.PairingRequested += PairingRequestedHandler;
                var result = await customPairing.PairAsync(ceremoniesSelected, protectionLevel);
                customPairing.PairingRequested -= PairingRequestedHandler;

                if (result.Status == DevicePairingResultStatus.Paired)
                {
                    succeeded = true;
                }
                else
                {
                    Debug.WriteLine("PairBluetooth: result.Status == " + result.Status.ToString());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("PairBluetooth: " + ex.Message);
            }

            return succeeded;
        }

        private void PairingRequestedHandler(
            DeviceInformationCustomPairing sender,
            DevicePairingRequestedEventArgs args)
        {
            switch (args.PairingKind)
            {
                case DevicePairingKinds.ConfirmOnly:
                    // Windows itself will pop the confirmation dialog as part of "consent" if this is running on Desktop or Mobile
                    // If this is an App for 'Windows IoT Core' where there is no Windows Consent UX, you may want to provide your own confirmation.
                    args.Accept();
                    break;

                case DevicePairingKinds.ProvidePin:
                    args.Accept(bluetoothPass);
                    break;
            }
        }

        public async Task<bool> ConnectToBluetooth(string name)
        {
            bool succeeded = false;

            try
            {
                var devices =
                      await DeviceInformation.FindAllAsync(
                        RfcommDeviceService.GetDeviceSelector(
                            RfcommServiceId.SerialPort));

                foreach (var d in devices)
                {
                    Debug.WriteLine("ConnectToBluetooth: found " + d.Name);
                }

                var device = devices.Single(x => x.Name == name);

                var service = await RfcommDeviceService.FromIdAsync(
                                                        device.Id);

                var socket = new StreamSocket();

                int errorCount = 0;
                bool connected = false;
                while (!connected)
                {
                    try
                    {
                        await socket.ConnectAsync(
                              service.ConnectionHostName,
                              service.ConnectionServiceName,
                              service.ProtectionLevel);
                        connected = true;
                    }
                    catch (Exception ex)
                    {
                        if (ex.HResult != -2147024637 || ++errorCount == 3)
                        {
                            throw;
                        }

                        Debug.WriteLine("ConnectToBluetooth: retrying after error 0x80070103 (No more data is available), error number " + errorCount);
                    }
                }

                var reader = new DataReader(socket.InputStream);
                var writer = new DataWriter(socket.OutputStream);

                bluetoothService = service;
                bluetoothSocket = socket;
                bluetoothReader = reader;
                bluetoothWriter = writer;
                succeeded = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("ConnectToBluetooth: " + ex.Message);
            }

            return succeeded;
        }

        public async Task<bool> CancelAndDisconnectBluetooth()
        {
            bool succeeded = false;

            try
            {
                await bluetoothSocket.CancelIOAsync();
                bluetoothReader.Dispose();
                bluetoothReader = null;
                bluetoothWriter.Dispose();
                bluetoothWriter = null;
                bluetoothSocket.Dispose();
                bluetoothSocket = null;
                bluetoothService.Dispose();
                bluetoothService = null;

                succeeded = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CancelAndDisconnectBluetooth: " + ex.Message);
            }

            return succeeded;
        }

        public async Task<bool> BluetoothSendCommand(string command, string ack = "")
        {
            try
            {
                bool commandSent = false;

                while (!commandSent)
                {
                    bluetoothWriter.WriteString(command);
                    var bytesSent = await bluetoothWriter.StoreAsync();

                    if (ack.Length > 0)
                    {
                        string reply = await BluetoothReadString();
                        if (reply == ack)
                        {
                            commandSent = true;
                        }
                        else
                        {
                            Debug.WriteLine("BluetoothSendCommand: got reply \"" + reply + "\" instead of ack \"" + ack + "\"");
                            await Task.Delay(200);
                        }
                    }
                    else
                    {
                        commandSent = true;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BluetoothSendCommand: " + ex.Message);
                return false;
            }
        }

        public async Task<string> BluetoothReadString()
        {
            try
            {
                string result;
                do
                {
                    result = "";
                    while (true)
                    {
                        //CancellationTokenSource cts = new CancellationTokenSource(3000); // cancel after x ms
                        //var bytesToRead = await bluetoothReader.LoadAsync(1).AsTask(cts.Token);
                        var bytesToRead = await bluetoothReader.LoadAsync(1);
                        var oneByte = bluetoothReader.ReadString(bytesToRead);

                        if (oneByte == "\r")
                        {
                            continue;
                        }
                        else if (oneByte == "\n")
                        {
                            break;
                        }

                        result += oneByte;
                    }
                }
                while (result == "Ping");

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BluetoothReadString: " + ex.Message);
                return "";
            }
        }
    }
}
