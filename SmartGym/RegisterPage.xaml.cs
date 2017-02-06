using Microsoft.WindowsAzure.MobileServices;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Foundation.Metadata;
using Windows.Phone.UI.Input;
using Windows.UI;
using System.ComponentModel.DataAnnotations;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SmartGym
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RegisterPage : Page
    {
        private IMobileServiceTable<TodoItem> usersTable = App.MobileService.GetTable<TodoItem>();

        public RegisterPage()
        {
            this.InitializeComponent();

            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                HardwareButtons.BackPressed += BackRequested;
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
            this.Frame.Navigate(typeof(MainPage), null);
        }

        private async Task InsertnewUser(TodoItem newUser)
        {
            // This code inserts a new user into the database. After the operation completes
            // and the mobile app backend has assigned an id, the item is added to the CollectionView.
            await usersTable.InsertAsync(newUser);
        }

        private async void RegisterButton_Click(object sender, RoutedEventArgs e)
        {
            statusTextBlock.Foreground = new SolidColorBrush(Colors.Red);

            string username = usernameTextBox.Text;
            string password = passwordBox.Password;
            string repassword = confirmPasswordBox.Password;
            string email = emailTextBox.Text;
            bool isMale = maleRadioButton.IsChecked ?? false;
            bool isFemale = femaleRadioButton.IsChecked ?? false;

            if (username.Length == 0 ||
                password.Length == 0 ||
                repassword.Length == 0 ||
                email.Length == 0 ||
                (!isMale && !isFemale))
            {
                statusTextBlock.Text = "Please fill in all of the fields";
                return;
            }

            if (!password.Equals(repassword))
            {
                statusTextBlock.Text = "The two passwords do not match";
                return;
            }

            if (!new EmailAddressAttribute().IsValid(email))
            {
                statusTextBlock.Text = "The email is not valid";
                return;
            }

            statusTextBlock.Foreground = new SolidColorBrush(Colors.White);
            statusTextBlock.Text = "Registering...";
            EnableUiControls(false);
            try
            {
                List<TodoItem> items = await usersTable.
               Where(userdata => userdata.Username == username).ToListAsync();
                if (items.Count() != 0)
                {
                    statusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                    statusTextBlock.Text = "Username already exists";
                    EnableUiControls(true);
                    return;
                }

                var newUser = new TodoItem { Username = username, Password = password };
                await InsertnewUser(newUser);

                CurrentUser.userData = newUser;
            }
            catch(Exception)
            {
                statusTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                statusTextBlock.Text = "There is no internet connection";
                EnableUiControls(true);
                return;
            }

            this.Frame.Navigate(typeof(MenuPage), null);
        }

        private void EnableUiControls(bool enable)
        {
            usernameTextBox.IsEnabled = enable;
            passwordBox.IsEnabled = enable;
            confirmPasswordBox.IsEnabled = enable;
            emailTextBox.IsEnabled = enable;
            maleRadioButton.IsEnabled = enable;
            femaleRadioButton.IsEnabled = enable;
            registerButton.IsEnabled = enable;
        }
    }
}
