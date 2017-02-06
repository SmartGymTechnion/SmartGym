using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Radios;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
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
    public sealed partial class StartPage : Page
    {
        private IMobileServiceTable<Devices> devicesTable = App.MobileService.GetTable<Devices>();
        private IMobileServiceTable<Results> resultsTable = App.MobileService.GetTable<Results>();

        public StartPage()
        {
            this.InitializeComponent();

            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                HardwareButtons.BackPressed += BackRequested;
            }

            initComboBox();
        }

        void initComboBox()
        {
            List<string> data = new List<string>();
            if (CurrentSession.type == "Strap")
            {
                data.Add("Push-ups");
                data.Add("Sit-ups");
            }
            else if (CurrentSession.type == "Weights")
            {
                data.Add("Regular");
                data.Add("Hammers");
                data.Add("Sideways");
            }
            else if (CurrentSession.type == "Machines")
            {
                data.Add("Default");
            }

            exerciseComboBox.ItemsSource = data;
            exerciseComboBox.SelectedIndex = 0;

            if (CurrentSession.type == "Machines")
            {
                mainTextBlock.Visibility = Visibility.Collapsed;
                //exerciseTextBlock.Visibility = Visibility.Collapsed;
                exerciseComboBox.Visibility = Visibility.Collapsed;
            }
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

            try
            {
                await BluetoothConnection.bluetooth.CancelAndDisconnectBluetooth();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("CancelAndDisconnectBluetooth: " + ex.Message);
            }

            try
            {
                await BluetoothConnection.bluetooth.TurnOffBluetooth();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("TurnOffBluetooth: " + ex.Message);
            }

            // Mark device as free
            try
            {
                List<Devices> items = await devicesTable.
                      Where(devicesdata => (devicesdata.Name == CurrentSession.deviceName)).ToListAsync();
                if (items.Count() == 0)
                {
                    await OnConnectionError("Fatal Error");
                }
                Devices dev = items.First();
                dev.isTaken = false;
                await devicesTable.UpdateAsync(dev);
            }
            catch (Exception)
            {
                await OnConnectionError("There is no internet connection");
            }

            Frame.Navigate(typeof(MenuPage), null);
        }

        private void startButton_Click(object sender, RoutedEventArgs e)
        {
            CurrentExercise.type = exerciseComboBox.SelectedItem.ToString();

            try
            {
                CurrentExercise.sets = int.Parse(setsTextBox.Text);
            }
            catch (Exception)
            {
                CurrentExercise.sets = 0;
            }

            try
            {
                CurrentExercise.target = int.Parse(repsTextBox.Text);
            }
            catch (Exception)
            {
                CurrentExercise.target = 0;
            }

            Frame.Navigate(typeof(ExercisePage), null);
        }

        private async void exerciseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                List<Results> resultsData = await resultsTable.
                     Where(resultsdata => resultsdata.Username == CurrentUser.userData.Username && 
                     resultsdata.Exercise == exerciseComboBox.SelectedItem.ToString()).ToListAsync();
                if(resultsData.Count() == 0)
                {
                    lastSets.Text = "0";
                    lastRepeat.Text = "0";
                    lastWeight.Text = "0";
                }
                else
                {
                    Results lastRes = resultsData.First();
                    foreach(var res in resultsData)
                    {
                        if(res.ExerciseId > lastRes.ExerciseId)
                        {
                            lastRes = res;
                        } else if(res.ExerciseId == lastRes.ExerciseId)
                        {
                            if(res.SetId > lastRes.SetId)
                            {
                                lastRes = res;
                            }
                        }
                    }
                    lastSets.Text = lastRes.SetId.ToString();
                    lastRepeat.Text = lastRes.Repetitions.ToString();
                    lastWeight.Text = lastRes.Weight.ToString();
                    setsTextBox.Text = lastRes.SetId.ToString();
                    repsTextBox.Text = lastRes.Repetitions.ToString();
                }
            }
            catch (Exception)
            {
                await OnConnectionError("There is no internet connection");
            }
        }

        private async Task OnConnectionError(string msg)
        {
            var dialog = new MessageDialog(msg);
            await dialog.ShowAsync();
            this.Frame.Navigate(typeof(MenuPage), null);
        }
    }
}
