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
    public sealed partial class AdminUsersPage : Page
    {

        private IMobileServiceTable<TodoItem> usersTable = App.MobileService.GetTable<TodoItem>();
        private IMobileServiceTable<Friends> friendsTable = App.MobileService.GetTable<Friends>();
        private IMobileServiceTable<Results> resultsTable = App.MobileService.GetTable<Results>();

        public AdminUsersPage()
        {
            this.InitializeComponent();

            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                HardwareButtons.BackPressed += BackRequested;
            }

            try
            {
                InitList();
            }
            catch
            {
                warningButton.Text = "There is no internet connection";
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
            this.Frame.Navigate(typeof(AdminPage), null);
        }

        private async void InitList()
        {
            List<TodoItem> items = await usersTable.ToListAsync();
            List<string> usersList = new List<string>();
            foreach(var user in items)
            {
                if(user.Username != null || user.Username != "")
                {
                    usersList.Add(user.Username);
                }                
            }
            usersView.ItemsSource = usersList;
        }

        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (usersView.SelectedItem == null)
            {
                warningButton.Text = "You have to select a user";
                return;
            }

            string username = usersView.SelectedItem.ToString();
            try
            {
                List<TodoItem> items = await usersTable.
                Where(userdata => userdata.Username == username).ToListAsync();
                TodoItem userData = items.First();
                await usersTable.DeleteAsync(userData);
                List<Friends> friendsList = await friendsTable.
                    Where(friendata => friendata.User1 == username || friendata.User2 == username).ToListAsync();
                foreach (var friendship in friendsList)
                {
                    await friendsTable.DeleteAsync(friendship);
                }
                List<Results> resultsList = await resultsTable.
                    Where(resultdata => resultdata.Username == username).ToListAsync();
                foreach (var result in resultsList)
                {
                    await resultsTable.DeleteAsync(result);
                }

                InitList();
            }
            catch (Exception)
            {
                warningButton.Text = "There is no internet connection";
            }
            
        }

        private void ResultsButton_Click(object sender, RoutedEventArgs e)
        {
            if (usersView.SelectedItem == null)
            {
                warningButton.Text = "You have to select a user";
            }
            else
            {
                Friend.name = usersView.SelectedItem.ToString();
                Friend.isFriend = true;
                this.Frame.Navigate(typeof(ResultsPage), null);
            }
        }
    }
}
