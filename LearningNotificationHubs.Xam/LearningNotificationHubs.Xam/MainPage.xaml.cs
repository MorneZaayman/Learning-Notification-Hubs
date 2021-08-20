using LearningNotificationHubs.Shared.Dtos;
using LearningNotificationHubs.Shared.Models;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace LearningNotificationHubs.Xam
{
    public partial class MainPage : ContentPage 
    {
        private HttpClient _httpClient;

        public Guid DeviceId
        {
            get
            {
                string deviceId = Preferences.Get("deviceId", string.Empty);
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    deviceId = Guid.NewGuid().ToString();
                    Preferences.Set("deviceId", deviceId);
                }

                return Guid.Parse(deviceId);
            }
        }

        public string PnsToken
        {
            get
            {
                return Preferences.Get("pnsToken", string.Empty);
            }

            set
            {
                Preferences.Set("pnsToken", value);
                PnsTokenLabel.Text = $"PnsToken: {value}";
            }
        }

        public string DevicePlatform
        {
            get
            {
                return DeviceInfo.Platform.ToString();
            }
        }

        public MainPage()
        {
            InitializeComponent();

            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri("http://localhost:5000");    

            PlatformLabel.Text = $"Platform: {DevicePlatform}";
            DeviceIdLabel.Text = $"DeviceId: {DeviceId}";
            PnsTokenLabel.Text = $"PnsToken: {PnsToken}";
        }

        private async void SignInButton_Clicked(object sender, EventArgs e)
        {
            string username = UsernameEntry.Text;

            if (string.IsNullOrWhiteSpace(username))
            {
                await DisplayAlert("Error", "You need to provide a username.", "OK");
                return;
            }

            SignInUserDto signInUserDto = new SignInUserDto
            {
                Username = UsernameEntry.Text,
                DeviceId = DeviceId,              
                PnsToken = PnsToken
            };

            if (!Enum.TryParse(DevicePlatform, out Platform platform))
            {
                await DisplayAlert("Error", $"{DevicePlatform} is not supported.", "OK");
                return;
            }
            signInUserDto.Platform = platform;

            StringContent jsonContent = new StringContent(JsonSerializer.Serialize(signInUserDto), Encoding.UTF8, "application/json");

            HttpResponseMessage response = await _httpClient.PostAsync("/auth/signuserin", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Error", response.ReasonPhrase, "OK");
                return;
            }

            UsernameEntry.IsEnabled = false;
            SignInButton.IsEnabled = false;
            SignOutButton.IsEnabled = true;
            await DisplayAlert("Success", $"You were signed in.", "OK");
        }

        private async void SignOutButton_Clicked(object sender, EventArgs e)
        {
            string username = UsernameEntry.Text;

            if (string.IsNullOrWhiteSpace(username))
            {
                await DisplayAlert("Error", "You need to provide a username.", "OK");
                return;
            }

            SignOutUserDto signOutUserDto = new SignOutUserDto
            {
                Username = UsernameEntry.Text,
                DeviceId = DeviceId,
            };

            StringContent jsonContent = new StringContent(JsonSerializer.Serialize(signOutUserDto), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync("/auth/signuserout", jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                await DisplayAlert("Error", response.ReasonPhrase, "OK");
                return;
            }

            UsernameEntry.IsEnabled = true;
            SignInButton.IsEnabled = true;
            SignOutButton.IsEnabled = false;
            await DisplayAlert("Success", $"You were signed out.", "OK");
        }
    }
}
