using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Phone.UI.Input;
using Windows.Storage.Streams;
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
    public sealed partial class ExercisePage : Page
    {
        private IMobileServiceTable<Results> resultsTable = App.MobileService.GetTable<Results>();
        private Windows.UI.Core.CoreDispatcher messageDispatcher = Window.Current.CoreWindow.Dispatcher;
        private Task<bool> bgTask = null;
        private int uniqueExerciseId = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        private int currentSetId = 1;
        private int lastWeight = 0;
        private MediaElement speakMediaElement = new MediaElement();
        private Timer noteResetTimer;

        public ExercisePage()
        {
            this.InitializeComponent();

            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                HardwareButtons.BackPressed += BackRequested;
            }

            noteResetTimer = new Timer(RestoreNoteTimerCallback, this, Timeout.Infinite, Timeout.Infinite);

            titleTextBlock.Text = CurrentSession.type + " workout";

            UpdatedExerciseText();
            SetMotivationalNote();

            nextSetButton.IsEnabled = false;
            finishButton.IsEnabled = false;

            bgTask = RunAsync();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            noteResetTimer.Dispose();
            noteResetTimer = null;

            if (ApiInformation.IsApiContractPresent("Windows.Phone.PhoneContract", 1, 0))
            {
                HardwareButtons.BackPressed -= BackRequested;
            }
        }

        private async void BackRequested(object sender, BackPressedEventArgs e)
        {
            e.Handled = true;

            await SendStopSet();

            this.Frame.Navigate(typeof(StartPage), null);
        }

        private void UpdatedExerciseText()
        {
            var text = "";

            if (CurrentSession.type != "Machines")
            {
                text += "Exercise: " + CurrentExercise.type + "\n";
            }

            if (CurrentExercise.sets > 0)
            {
                text += "Set: " + currentSetId + " out of " + CurrentExercise.sets.ToString();
                if (currentSetId == CurrentExercise.sets)
                {
                    text += " (last set)";
                }
                else if (currentSetId > CurrentExercise.sets)
                {
                    text += " (extra set)";
                }
                text += "\n";
            }
            else
            {
                text += "Set: " + currentSetId + "\n";
            }

            if (CurrentExercise.target > 0)
            {
                text += "Target repetitions: " + CurrentExercise.target.ToString() + "\n";
            }

            if (lastWeight > 0)
            {
                text += "Weight: " + lastWeight.ToString() + " kg\n";
            }

            ExerciseTextBlock.Text = text;
        }

        private void SetMotivationalNote()
        {
            string[] names = {
                "Your only limit is you",
                "Nothing worth having comes easy",
                "I can and I will",
                "Go hard or go home",
                "You got this!",
                "Champions train, losers complain",
                "If you think you can, you can",
                "Believe you can and you will",
                "Make this count!",
                "Greatness is earned, never awarded",
                "It never gets easier, you get better",
                "Push yourself, nobody else will",
                "No pain - no gain",
                "Practice like a champion",
            };

            noteTextBlock.Foreground = new SolidColorBrush(Colors.White);
            noteTextBlock.Text = names[new Random().Next(0, names.Length)];

            noteResetTimer?.Change(10000, Timeout.Infinite);
        }

        private void SetNoteForUser(string note, Brush color, int timeout = Timeout.Infinite)
        {
            noteTextBlock.Foreground = color;
            noteTextBlock.Text = note;

            noteResetTimer?.Change(timeout, Timeout.Infinite);
        }

        private static async void RestoreNoteTimerCallback(object o)
        {
            var page = o as ExercisePage;

            // Run in the UI thread.
            await page.messageDispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    page.SetMotivationalNote();
                });
        }

        private async Task<bool> RunAsync()
        {
            bool sendOk;
            string reply;

            if (CurrentSession.type == "Weights" || CurrentSession.type == "Strap")
            {
                SetNoteForUser("Initializing, please wait...", new SolidColorBrush(Colors.Red));

                Debug.WriteLine("Sending SetMode...");

                sendOk = await BluetoothConnection.bluetooth.BluetoothSendCommand("SetMode=" + CurrentExercise.type, "OkSetMode");
                if (!sendOk)
                {
                    await OnCommunicationError();
                    return false;
                }

                Debug.WriteLine("Got OkSetMode");
            }

            Debug.WriteLine("Sending StartSet...");

            sendOk = await BluetoothConnection.bluetooth.BluetoothSendCommand("StartSet", "OkStartSet");
            if (!sendOk)
            {
                await OnCommunicationError();
                return false;
            }

            Debug.WriteLine("Got OkStartSet");

            reply = await BluetoothConnection.bluetooth.BluetoothReadString();
            while (reply != "OkStopSet")
            {
                if (reply == "")
                {
                    Debug.WriteLine("BluetoothReadString failed");
                    await OnCommunicationError();
                    return false;
                }

                Debug.WriteLine("CMD: " + reply);

                string[] split = reply.Split(new char[] { '=' }, 2);

                switch (split[0])
                {
                    case "Lifted":
                    case "Weight":
                        lastWeight = int.Parse(split[1]);
                        UpdatedExerciseText();
                        break;

                    case "SomeValue":
                        // Can be ignored.
                        break;

                    case "Print":
                        SetNoteForUser(split[1], new SolidColorBrush(Colors.Orange), 2000);
                        break;

                    case "Error":
                        SetNoteForUser(split[1], new SolidColorBrush(Colors.Red));
                        break;

                    case "SetCounter":
                        await SetRepetitionCount(int.Parse(split[1]));
                        break;
                }

                reply = await BluetoothConnection.bluetooth.BluetoothReadString();
            }

            Debug.WriteLine("RunAsync done");
            return true;
        }

        private async Task SetRepetitionCount(int num)
        {
            resultTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            resultTextBlock.Text = num.ToString();

            var speakText = resultTextBlock.Text;

            if (CurrentExercise.target > 0)
            {
                if (num == CurrentExercise.target)
                {
                    if (CurrentExercise.sets > 0 &&
                        currentSetId == CurrentExercise.sets)
                    {
                        speakText += ", last set complete";
                    }
                    else
                    {
                        speakText += ", set complete";
                    }
                }

                if (num >= CurrentExercise.target)
                {
                    resultTextBlock.Foreground = new SolidColorBrush(Colors.Green);
                }
            }

            await SpeakText(speakText);

            nextSetButton.IsEnabled = true;
            finishButton.IsEnabled = true;
        }

        private async Task SendStopSet()
        {
            bool sendOk = await BluetoothConnection.bluetooth.BluetoothSendCommand("StopSet");
            if (!sendOk)
            {
                await OnCommunicationError();
                return;
            }

            while (bgTask != null)
            {
                int timeout = 5000;
                if (await Task.WhenAny(bgTask, Task.Delay(timeout)) == bgTask)
                {
                    bool bgTaskResult = bgTask.Result;
                    bgTask = null;
                    if (!bgTaskResult)
                    {
                        await OnCommunicationError();
                        return;
                    }
                }
                else
                {
                    sendOk = await BluetoothConnection.bluetooth.BluetoothSendCommand("StopSet");
                    if (!sendOk)
                    {
                        await OnCommunicationError();
                        return;
                    }
                }
            }
        }

        private async Task OnCommunicationError(string str = "Bluetooth communication error")
        {
            var dialog = new MessageDialog(str);
            await dialog.ShowAsync();
            Frame.Navigate(typeof(StartPage), null);
        }

        private async void nextSetButton_Click(object sender, RoutedEventArgs e)
        {
            nextSetButton.IsEnabled = false;
            finishButton.IsEnabled = false;

            await SendStopSet();
            if (!await SaveResult())
            {
                return;
            }

            currentSetId++;
            if (CurrentExercise.sets > 0 &&
                currentSetId == CurrentExercise.sets)
            {
                await SpeakText("Last set");
            }
            else
            {
                await SpeakText("Set number " + currentSetId.ToString());
            }

            UpdatedExerciseText();
            SetMotivationalNote();
            resultTextBlock.Foreground = new SolidColorBrush(Colors.Black);
            resultTextBlock.Text = "0";

            bgTask = RunAsync();

            finishButton.IsEnabled = true;
        }

        private async void finishButton_Click(object sender, RoutedEventArgs e)
        {
            await SpeakText("You go girl! Saving results...");

            nextSetButton.IsEnabled = false;
            finishButton.IsEnabled = false;

            await SendStopSet();
            if (!await SaveResult())
            {
                return;
            }

            Frame.Navigate(typeof(StartPage), null);
        }

        private async Task<bool> SaveResult()
        {
            int result = int.Parse(resultTextBlock.Text);
            if (result == 0)
            {
                return true;
            }

            var newResult = new Results
            {
                Username = CurrentUser.userData.Username,
                Exercise = CurrentExercise.type,
                Repetitions = result,
                ExerciseId = uniqueExerciseId,
                SetId = currentSetId,
                Weight = lastWeight
            };

            try
            {
                await resultsTable.InsertAsync(newResult);
            }
            catch (Exception)
            {
                // Don't return false here, as we have already
                // sent StopSet to the device.
                //noteTextBlock.Text = "There is no internet connection";
                //return false;

                var dialog = new MessageDialog("No internet connection, result wasn't saved");
                await dialog.ShowAsync();
            }

            return true;
        }

        private async Task SpeakText(string text)
        {
            var synth = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();
            Windows.Media.SpeechSynthesis.SpeechSynthesisStream stream = await synth.SynthesizeTextToStreamAsync(text);

            speakMediaElement.Stop();
            speakMediaElement.SetSource(stream, stream.ContentType);
            speakMediaElement.Play();
        }
    }
}
