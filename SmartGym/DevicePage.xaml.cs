using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Phone.UI.Input;
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
    public sealed partial class DevicePage : Page
    {
        private IMobileServiceTable<Devices> devicesTable = App.MobileService.GetTable<Devices>();

        public DevicePage()
        {
            this.InitializeComponent();

            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                HardwareButtons.BackPressed += BackRequested;
            }

            NameBlock.Text = CurrentDevice.currDevice.Name;
            TypeBlock.Text = CurrentDevice.currDevice.Type;
            totalUsesBlock.Text = CurrentDevice.currDevice.TotalUses.ToString();
            if (CurrentDevice.currDevice.isTaken)
            {
                isTakenBlock.Text = "Yes";
            }
            else
            {
                isTakenBlock.Text = "No";
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                HardwareButtons.BackPressed -= BackRequested;
            }
        }

        private void BackRequested(object sender, BackPressedEventArgs e)
        {
            e.Handled = true;
            this.Frame.Navigate(typeof(AdminDevicesPage), null);
        }

        private void resetButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AdminResetPage), null);
        }

        private async void removeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await devicesTable.DeleteAsync(CurrentDevice.currDevice);
            }
            catch (Exception)
            {
                warningBlock.Text = "There is no internet connection";
                return;
            }

            this.Frame.Navigate(typeof(AdminDevicesPage), null);
        }
    }
}
