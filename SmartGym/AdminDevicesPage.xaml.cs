using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Phone.UI.Input;
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
    public sealed partial class AdminDevicesPage : Page
    {
        private IMobileServiceTable<Devices> devicesTable = App.MobileService.GetTable<Devices>();

        public AdminDevicesPage()
        {
            this.InitializeComponent();
            //addItem();
            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                HardwareButtons.BackPressed += BackRequested;
            }

            updateList();
        }

        /*private async void addItem()
        {
            var newDevice = new Devices { Name="test" , TotalUses=137 , isTaken=true , Type="Strap" };
            await devicesTable.InsertAsync(newDevice);
        }*/

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
            this.Frame.Navigate(typeof(AdminPage), null);
        }

        private async void updateList()
        {
            List<string> temp = null;
            try
            {
                List<Devices> items = await devicesTable.ToListAsync();
                if (items.Count() == 0)
                {
                    warningBlock.Visibility = Visibility.Visible;
                    warningBlock.Text = "There are no devices";
                }
                else
                {
                    warningBlock.Visibility = Visibility.Collapsed;
                    temp = new List<string> { };
                    for (var i = 0; i < items.Count; i++)
                    {
                        var itemName = items[i].Name + "\u00A0";
                        string taken;
                        if (items[i].isTaken)
                        {
                            taken = "Yes";
                        }
                        else
                        {
                            taken = "No";
                        }
                        string str = String.Format("{0} {1}",
                            itemName.PadRight(50 - itemName.ToString().Length),
                            taken.ToString());
                        temp.Add(str);
                    }
                    devicesView.ItemsSource = temp;
                }
            }
            catch (Exception)
            {
                await OnConnectionError("There is no internet connection");
                this.Frame.Navigate(typeof(AdminPage), null);
            }
        }

        private async void devicesView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //string deviceName = devicesView.SelectedItem.ToString();
            string[] split = devicesView.SelectedItem.ToString().Split(new char[] { '\u00A0' }, 2);
            try
            {
                List<Devices> items = await devicesTable.
                       Where(devicesdata => (devicesdata.Name == split[0])).ToListAsync();
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
        }

        private void scanDevicebutton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AdminScanPage), null);
        }
    }
}
