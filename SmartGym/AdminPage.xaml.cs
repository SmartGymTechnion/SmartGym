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
    public sealed partial class AdminPage : Page
    {
        public AdminPage()
        {
            this.InitializeComponent();
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainPage), null);
        }

        private void UsersButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AdminUsersPage), null);
        }

        private void DevicesList_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(AdminDevicesPage), null);
        }

        private void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(StatisticsPage), null);
        }
    }
}
