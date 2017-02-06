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
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Foundation.Metadata;
using Windows.Phone.UI.Input;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SmartGym
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FriendsPage : Page
    {
        private IMobileServiceTable<Friends> friendsTable = App.MobileService.GetTable<Friends>();
        private IMobileServiceTable<TodoItem> usersTable = App.MobileService.GetTable<TodoItem>();

        public FriendsPage()
        {
            this.InitializeComponent();

            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                HardwareButtons.BackPressed += BackRequested;
            }
            try
            {
                updateList();
            }
            catch (Exception)
            {
                statusBlock.Text = "There is no internet connection";
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
            this.Frame.Navigate(typeof(MenuPage), null);
        }

        private async void updateList()
        {
            List<string> temp = null;
            try
            {
                List<Friends> items = await friendsTable.
                     Where(friendsdata => friendsdata.User1 == CurrentUser.userData.Username || 
                     friendsdata.User2 == CurrentUser.userData.Username).ToListAsync();
                if (items.Count() == 0)
                {
                    temp = new List<string> { "You have no friends :(" };
                    listView.ItemsSource = temp;
                }
                else
                {
                    temp = new List<string> { };
                    for (var i = 0; i < items.Count; i++)
                    {
                        if (items[i].User1.CompareTo(CurrentUser.userData.Username) == 0)
                        {
                            temp.Add(items[i].User2);
                        }
                        else
                        {
                            temp.Add(items[i].User1);
                        }
                    }
                    listView.ItemsSource = temp;
                }
            }
            catch (Exception)
            {
                statusBlock.Text = "There is no internet connection";
            }

        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MenuPage), null);
        }

        private async void button2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                List<TodoItem> items = await usersTable.
                Where(userdata => userdata.Id == CurrentUser.userData.Id).ToListAsync();
                TodoItem current = items.First();
                CurrentUser.userData = current;
                updateList();
            }
            catch (Exception)
            {
                statusBlock.Text = "There is no internet connection";
            }           
        }

        private async void button_Click(object sender, RoutedEventArgs e)
        {
            string friendName = textBox.Text;
            if (friendName.Length == 0)
            {
                statusBlock.Text = "Please insert a user name";
                return;
            }

            try
            {
                List<TodoItem> items = await usersTable.
                    Where(userdata => userdata.Username == friendName).ToListAsync();
                if (items.Count() == 0)
                {
                    statusBlock.Text = "This user does not exist";
                    return;
                }

                string currentName = CurrentUser.userData.Username;
                List<Friends> friendship = await friendsTable.
                    Where(friendsdata => (friendsdata.User1 == currentName && friendsdata.User2 == friendName)
                        || (friendsdata.User1 == friendName && friendsdata.User2 == currentName)).ToListAsync();
                if (friendship.Count() != 0)
                {
                    statusBlock.Text = "You are already friends";
                }
                else
                {
                    var newFriendship = new Friends { User1 = currentName, User2 = friendName };
                    await friendsTable.InsertAsync(newFriendship);
                }
                updateList();
            }
            catch (Exception)
            {
                statusBlock.Text = "There is no internet connection";
            }

        }

        private void ResultsButton_Click(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedItem == null)
            {
                statusBlock.Text = "You have to select a friend";
            }
            else
            {
                Friend.name = listView.SelectedItem.ToString();
                Friend.isFriend = true;
                this.Frame.Navigate(typeof(ResultsPage), null);
            }
        }
    }
}
