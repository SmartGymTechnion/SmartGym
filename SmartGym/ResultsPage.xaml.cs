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
    public sealed partial class ResultsPage : Page
    {

        private IMobileServiceTable<Results> resultsTable = App.MobileService.GetTable<Results>();

        public ResultsPage()
        {
            this.InitializeComponent();

            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                HardwareButtons.BackPressed += BackRequested;
            }

            avgWeightText.Visibility = Visibility.Collapsed;
            warningBlock.Visibility = Visibility.Collapsed;
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
            Friend.isFriend = false;
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void comboBox_Loaded(object sender, RoutedEventArgs e)
        {
            List<string> data = new List<string>();
            data.Add("Strap");
            data.Add("Weights");
            data.Add("Machines");

            // ... Get the ComboBox reference.
            var comboBox = sender as ComboBox;

            // ... Assign the ItemsSource to the List.
            typeBox.ItemsSource = data;

            // ... Make the first item selected.
            typeBox.SelectedIndex = 0;
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string value = typeBox.SelectedItem as string;
            List<string> data = new List<string>();
            if (value.CompareTo("Strap") == 0)
            {
                data.Add("Push-ups");
                data.Add("Sit-ups");
                exerciseBox.ItemsSource = data;
                exerciseBox.SelectedIndex = 0;
            }
            if (value.CompareTo("Weights") == 0)
            {
                data.Add("Regular");
                data.Add("Hammers");
                data.Add("Sideways");
                exerciseBox.ItemsSource = data;
                exerciseBox.SelectedIndex = 0;
                avgWeightText.Visibility = Visibility.Visible;
            }
            if (value.CompareTo("Machines") == 0)
            {
                data.Add("Default");
                exerciseBox.ItemsSource = data;
                exerciseBox.SelectedIndex = 0;
            }
        }

        private async Task OnConnectionError(string msg)
        {
            var dialog = new MessageDialog(msg);
            await dialog.ShowAsync();
        }

        private async void exerciseBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            timeListView.ItemsSource = null;
            avgSetBlock.Text = "";
            maxSetBlock.Text = "";
            avgRepBlock.Text = "";
            maxRepBlock.Text = "";
            avgWeightBlock.Text = "";
            var selectedItem = exerciseBox.SelectedItem;
            if (selectedItem == null)
            {
                return;
            }

            string exType = exerciseBox.SelectedItem.ToString();
            if (typeBox.SelectedItem.ToString() != "Strap")
            {
                avgWeightText.Visibility = Visibility.Visible;
            }
            else
            {
                avgWeightText.Visibility = Visibility.Collapsed;
            }

            string username = "";
            if (Friend.isFriend)
            {
                username = Friend.name;
            }
            else
            {
                username = CurrentUser.userData.Username;
            }

            // calculate stats
            try
            {         
                List<Results> items = await resultsTable.
                    Where(resultsdata => resultsdata.Username == username && resultsdata.Exercise == exType).ToListAsync();
                if (items.Count() == 0)
                {
                    warningBlock.Visibility = Visibility.Visible;
                    warningBlock.Text = "There are no results yet";
                }
                else
                {
                    warningBlock.Visibility = Visibility.Collapsed;
                    SortedDictionary<int, int> setsMap = new SortedDictionary<int, int>();
                    int sumRepeat = 0;
                    int maxRepeat = 0;
                    int maxSet = 0;
                    foreach (var res in items)
                    {
                        if (setsMap.ContainsKey(res.ExerciseId))
                        {
                            int val = 0;
                            setsMap.TryGetValue(res.ExerciseId, out val);
                            if (res.SetId > val)
                            {
                                setsMap.Remove(res.ExerciseId);
                                setsMap.Add(res.ExerciseId, res.SetId);
                            }
                        }
                        else
                        {
                            setsMap.Add(res.ExerciseId, res.SetId);
                        }
                        sumRepeat += res.Repetitions;
                        if (res.Repetitions > maxRepeat)
                        {
                            maxRepeat = res.Repetitions;
                        }
                        if (res.SetId > maxSet)
                        {
                            maxSet = res.SetId;
                        }
                    }
                    double avgRepeat = sumRepeat / (double)items.Count();
                    avgRepBlock.Text = avgRepeat.ToString();
                    maxRepBlock.Text = maxRepeat.ToString();
                    maxSetBlock.Text = maxSet.ToString();
                    int setSum = 0;
                    foreach (var data in setsMap)
                    {
                        setSum += data.Value;
                    }
                    double avgSets = setSum / (double)setsMap.Count();
                    avgSetBlock.Text = avgSets.ToString();

                    // Calculate avg weight.
                    if(typeBox.SelectedItem.ToString() != "Strap")
                    {
                        int sum = 0;
                        foreach(var res in items)
                        {
                            sum += res.Weight;
                        }
                        double avgWeight = sum / (double)items.Count();
                        avgWeightBlock.Text = avgWeight.ToString();
                    }
                }
            }
            catch (Exception)
            {
                await OnConnectionError("There is no internet connection");
                return;
            }

            // display Dates combobox
            try
            {
                List<Results> results = await resultsTable.
                    Where(resultsdata => resultsdata.Username == username && resultsdata.Exercise ==
                            exerciseBox.SelectedItem.ToString() && resultsdata.SetId == 1).ToListAsync();
                List<DateTime> datesList = new List<DateTime>();
                foreach (var res in results)
                {
                    datesList.Add(res.Date);
                }
                dateBox.ItemsSource = datesList;
            }
            catch (Exception)
            {
                await OnConnectionError("There is no internet connection");
                return;
            }
        }

        private async void dateBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = dateBox.SelectedItem;
            if (selectedItem == null)
            {
                return;
            }
            string username = "";
            if (Friend.isFriend)
            {
                username = Friend.name;
            }
            else
            {
                username = CurrentUser.userData.Username;
            }
            DateTime time = (DateTime)dateBox.SelectedValue;
            try
            {
                List<Results> results = await resultsTable.
                       Where(resultsdata => resultsdata.Username == username && resultsdata.Exercise ==
                            exerciseBox.SelectedItem.ToString() && resultsdata.Date == time).ToListAsync();
                int ExID = results.First().ExerciseId;
                List<Results> data = await resultsTable.
                       Where(resultsdata => resultsdata.Username == username && resultsdata.ExerciseId == ExID).ToListAsync();
                List<string> timeList = new List<string>();
                foreach (var res in data)
                {
                    string str = String.Format("Set {0}, {1} repetitions",
                        (timeList.Count + 1).ToString(), res.Repetitions.ToString());
                    if (res.Weight > 0)
                    {
                        str += ", weight: " + res.Weight.ToString() + " kg";
                    }
                    timeList.Add(str);
                }
                timeListView.ItemsSource = timeList;
            } catch(Exception)
            {
                await OnConnectionError("There is no internet connection");
            }

        }
    }
}
