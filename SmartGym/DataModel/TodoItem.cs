using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Windows.Networking.Sockets;
using Windows.Devices.Bluetooth.Rfcomm;

namespace SmartGym
{
    public class TodoItem
    {
        public string Id { get; set; }

        [JsonProperty(PropertyName = "username")]
        public string Username { get; set; }

        [JsonProperty(PropertyName = "password")]
        public string Password { get; set; }

        [JsonProperty(PropertyName = "complete")]
        public bool Complete { get; set; }

    }

    public class Friends
    {
        public string Id { get; set; }

        [JsonProperty(PropertyName = "user1")]
        public string User1 { get; set; }

        [JsonProperty(PropertyName = "user2")]
        public string User2 { get; set; }

        [JsonProperty(PropertyName = "complete")]
        public bool Complete { get; set; }
    }

    public class Results
    {
        public string Id { get; set; }

        [JsonProperty(PropertyName = "username")]
        public string Username { get; set; }

        [JsonProperty(PropertyName = "exercise")]
        public string Exercise { get; set; }

        [JsonProperty(PropertyName = "repetitions")]
        public int Repetitions { get; set; }

        [JsonProperty(PropertyName = "ExerciseID")]
        public int ExerciseId { get; set; }

        [JsonProperty(PropertyName = "setID")]
        public int SetId { get; set; }

        [JsonProperty(PropertyName = "weight")]
        public int Weight { get; set; }

        [JsonProperty(PropertyName = "createdAt")]
        public DateTime Date { get; set; }
    }

    public class Devices
    {
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "isTaken")]
        public bool isTaken { get; set; }

        [JsonProperty(PropertyName = "TotalUses")]
        public int TotalUses { get; set; }

        [JsonProperty(PropertyName = "Type")]
        public string Type { get; set; }
    }

    public static class CurrentUser
    {
        public static TodoItem userData;
    }

    public static class BluetoothConnection
    {
        public static Bluetooth bluetooth = new Bluetooth { };
    }

    public static class CurrentSession
    {
        public static string type;
        public static string deviceName;
    }

    public static class CurrentExercise
    {
        public static string type;
        public static int sets;
        public static int target;
    }

    public static class Friend
    {
        public static bool isFriend;
        public static string name;
    }

    public static class CurrentDevice
    {
        public static Devices currDevice;
    }
}
