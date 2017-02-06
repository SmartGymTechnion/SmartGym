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
using Windows.UI;
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
    public sealed partial class StatisticsPage : Page
    {

        private IMobileServiceTable<TodoItem> usersTable = App.MobileService.GetTable<TodoItem>();
        private IMobileServiceTable<Devices> devicesTable = App.MobileService.GetTable<Devices>();
        private IMobileServiceTable<Results> resultsTable = App.MobileService.GetTable<Results>();

        public StatisticsPage()
        {
            this.InitializeComponent();

            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                HardwareButtons.BackPressed += BackRequested;
            }       
            calculateStats();                   
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
            this.Frame.Navigate(typeof(AdminPage), null);
        }

        private async Task OnConnectionError(string msg)
        {
            var dialog = new MessageDialog(msg);
            await dialog.ShowAsync();
            this.Frame.Navigate(typeof(AdminPage), null);
        }

        private async void calculateStats()
        {
            try
            {
                List<TodoItem> usersData = await usersTable.ToListAsync();
                numOfUsersBlock.Text = usersData.Count().ToString();
                List<Devices> devicesData = await devicesTable.ToListAsync();
                numOfDevicesBlock.Text = devicesData.Count().ToString();
                List<Devices> activeDevicesData = await devicesTable.
                         Where(devicedata => devicedata.isTaken == true).ToListAsync();
                numOfActiveBlock.Text = activeDevicesData.Count().ToString();
                if (devicesData.Count() != 0)
                {
                    Devices mostActiveDevice = devicesData.First();
                    foreach (var device in devicesData)
                    {
                        if (device.TotalUses > mostActiveDevice.TotalUses)
                        {
                            mostActiveDevice = device;
                        }
                    }
                    mostActiveBlock.Text = mostActiveDevice.Name;
                    deviceUsesBlock.Text = mostActiveDevice.TotalUses.ToString();
                }
                SortedDictionary<string, int> mostActiveUserMap = new SortedDictionary<string, int>();
                List<Results> resultsData = await resultsTable.
                         Where(resultsdata => resultsdata.SetId == 1).ToListAsync();
                foreach (var res in resultsData)
                {
                    if (mostActiveUserMap.ContainsKey(res.Username))
                    {
                        int val = 0;
                        mostActiveUserMap.TryGetValue(res.Username, out val);
                        val++;
                        mostActiveUserMap.Remove(res.Username);
                        mostActiveUserMap.Add(res.Username, val);
                    }
                    else
                    {
                        mostActiveUserMap.Add(res.Username, 1);
                    }
                }
                KeyValuePair<string, int> mostActiveUser = mostActiveUserMap.First();
                foreach (var user in mostActiveUserMap)
                {
                    if (user.Value > mostActiveUser.Value)
                    {
                        mostActiveUser = user;
                    }
                }
                activeUserBlock.Text = mostActiveUser.Key;
                numOfExercisesBlock.Text = mostActiveUser.Value.ToString();
            }
            catch (Exception)
            {
                await OnConnectionError("There is no internet connection");
            }
        }
    }
}
