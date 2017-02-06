using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Popups;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SmartGym
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IMobileServiceTable<TodoItem> usersTable = App.MobileService.GetTable<TodoItem>();
        
        public MainPage()
        {
            this.InitializeComponent();

            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
            {
                var statusBar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView();
                statusBar.BackgroundColor = Windows.UI.Colors.Transparent;
                statusBar.ForegroundColor = Windows.UI.Colors.White;
            }

            // Temporary
            //usernameTextBox.Text = "Neriya";
            //passwordBox.Password = "123";
            //usernameTextBox.Text = "admin";
            //passwordBox.Password = "admin";
        }

        private async void login_Click(object sender, RoutedEventArgs e)
        {
            statusTextBlock.Foreground = new SolidColorBrush(Colors.Red);

            string username = usernameTextBox.Text;
            string password = passwordBox.Password;

            if (username.Length == 0 ||
                password.Length == 0)
            {
                statusTextBlock.Text = "Please fill in all of the fields";
                return;
            }

            statusTextBlock.Foreground = new SolidColorBrush(Colors.White);
            statusTextBlock.Text = "Logging in...";
            EnableUiControls(false);


            if (username.ToLower() == "admin" && password == "admin")
            {
                this.Frame.Navigate(typeof(AdminPage), null);
                return;
            }

            try
            {
                List<TodoItem> items = await usersTable.
                    Where(userdata => userdata.Username == username && userdata.Password == password).ToListAsync();
                if (items.Count() == 0 ||
                    items.First().Password != password) // an additional check for a password, case-sensitive this time
                {
                    statusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    statusTextBlock.Text = "The user name or password is incorrect";
                    EnableUiControls(true);
                    return;
                }
                CurrentUser.userData = items.First();
            }
            catch (Exception)
            {
                statusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                statusTextBlock.Text = "There is no internet connection";
                EnableUiControls(true);
                return;
            }
      
            this.Frame.Navigate(typeof(MenuPage), null);
        }

        private void register_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(RegisterPage), null);
        }

        private void EnableUiControls(bool enable)
        {
            usernameTextBox.IsEnabled = enable;
            passwordBox.IsEnabled = enable;
            loginButton.IsEnabled = enable;
            registerButton.IsEnabled = enable;
        }
    }
}
