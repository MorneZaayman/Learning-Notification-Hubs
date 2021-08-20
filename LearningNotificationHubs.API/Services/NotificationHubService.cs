using System.Text;
using System.Xml;
using LearningNotificationHubs.Shared.Dtos;
using LearningNotificationHubs.Shared.Models;

namespace LearningNotificationHubs.API.Services
{
    public class NotificationHubService : INotificationHubService
    {
        private readonly string _namespaceName;
        private readonly string _hubName;
        private readonly string _sasKeyName;
        private readonly string _sasKey;
      
        public NotificationHubService(IConfiguration configuration)
        {
            _namespaceName = configuration.GetSection("NotificationHubService:NamespaceName").Value;
            _hubName = configuration.GetSection("NotificationHubService:HubName").Value;
            _sasKeyName = configuration.GetSection("NotificationHubService:SasKeyName").Value;
            _sasKey = configuration.GetSection("NotificationHubService:SasKey").Value;
        }

        public async Task<string> CreateRegistration(SignInUserDto signInUserDto)
        {
            string createRegistration = CreateRegistrationBody(signInUserDto);

            string resourceUri = $"https://{_namespaceName}.servicebus.windows.net/{_hubName}/registrations?api-version=2015-01";
            HttpRequestMessage request = new HttpRequestMessage()
            {
                RequestUri = new Uri(resourceUri),
                Method = HttpMethod.Post,
                Content = new StringContent(createRegistration, Encoding.UTF8, "application/atom+xml")
            };

            string sasToken = Azure.AzureUtililities.GenerateSASToken(resourceUri, _sasKeyName, _sasKey);
            request.Headers.TryAddWithoutValidation("Authorization", sasToken);

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            string responseContent = await response.Content.ReadAsStringAsync();

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);

            string registrationId = xmlDocument.GetElementsByTagName("title")[0].InnerText;

            return registrationId;
        }

        public async Task<string> UpdateRegistration(SignInUserDto signInDeviceDto, string registrationId)
        {
            string registrationBody = CreateRegistrationBody(signInDeviceDto);

            string resourceUri = $"https://{_namespaceName}.servicebus.windows.net/{_hubName}/registrations/{registrationId}?api-version=2015-01";
            HttpRequestMessage request = new HttpRequestMessage()
            {
                RequestUri = new Uri(resourceUri),
                Method = HttpMethod.Post,
                Content = new StringContent(registrationBody, Encoding.UTF8, "application/atom+xml")
            };

            string sasToken = Azure.AzureUtililities.GenerateSASToken(resourceUri, _sasKeyName, _sasKey);
            request.Headers.TryAddWithoutValidation("Authorization", sasToken);
            request.Headers.TryAddWithoutValidation("If-Match", "*");

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            string contentLocation = response.Headers.First(header => header.Key == "Content-Location").Value.First();
            string newRegistrationId = new Uri(contentLocation).Segments.Last();

            return newRegistrationId;
        }

        public async Task DeleteRegistration(string registrationId)
        {
            string resourceUri = $"https://{_namespaceName}.servicebus.windows.net/{_hubName}/registrations/{registrationId}?api-version=2015-01";
            HttpRequestMessage request = new HttpRequestMessage()
            {
                RequestUri = new Uri(resourceUri),
                Method = HttpMethod.Delete
            };

            string sasToken = Azure.AzureUtililities.GenerateSASToken(resourceUri, _sasKeyName, _sasKey);
            request.Headers.TryAddWithoutValidation("Authorization", sasToken);
            request.Headers.TryAddWithoutValidation("If-Match", "*");

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();
        }
        public async Task SendNotification(string message, string tag)
        {
            string notificationBody = "{\"message\":\"" + message + "\"}";

            string resourceUri = $"https://{_namespaceName}.servicebus.windows.net/{_hubName}/messages/?api-version=2015-01";
            HttpRequestMessage request = new HttpRequestMessage()
            {
                RequestUri = new Uri(resourceUri),
                Method = HttpMethod.Post,
                Content = new StringContent(notificationBody, Encoding.UTF8, "application/json")
            };

            string sasToken = Azure.AzureUtililities.GenerateSASToken(resourceUri, _sasKeyName, _sasKey);
            request.Headers.TryAddWithoutValidation("Authorization", sasToken);
            request.Headers.TryAddWithoutValidation("ServiceBusNotification-Tags", tag);
            request.Headers.TryAddWithoutValidation("ServiceBusNotification-Format", "template");

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();       
        }

        private string CreateRegistrationBody(SignInUserDto signInDeviceDto)
        {
            #region Registration templates
            string windowsRegistration = @"<?xml version=""1.0"" encoding=""utf-8""?>
<entry xmlns=""http://www.w3.org/2005/Atom"">
    <content type=""application/xml"">
        <RegistrationDescription xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" i:type=""WindowsTemplateRegistrationDescription"" xmlns=""http://schemas.microsoft.com/netservices/2010/10/servicebus/connect"">
            <Tags>{0}</Tags>
            <ChannelUri>{1}</ChannelUri>
            <BodyTemplate>
                <![CDATA[<toast><visual><binding template=""ToastText01""><text id=""1"">$(message)</text></binding></visual></toast>]]>
            </BodyTemplate>
            <WnsHeaders>
                <WnsHeader>
                    <Header>X-WNS-Type</Header>
                    <Value>wns/toast</Value>
                </WnsHeader>
            </WnsHeaders>
        </RegistrationDescription>
    </content>
</entry>";
            string appleRegistration = @"<?xml version=""1.0"" encoding=""utf-8""?>
<entry xmlns=""http://www.w3.org/2005/Atom"">
    <content type=""application/xml"">
        <RegistrationDescription xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" i:type=""AppleTemplateRegistrationDescription"" xmlns=""http://schemas.microsoft.com/netservices/2010/10/servicebus/connect"">
            <Tags>{0}</Tags>
            <DeviceToken>{1}</DeviceToken> 
            <BodyTemplate>
                <![CDATA[
                    {""aps"":{""alert"":""$(message)""}}
                ]]>
            </BodyTemplate>
        </RegistrationDescription>
    </content>
</entry>";
            string androidRegistration = @"<?xml version=""1.0"" encoding=""utf-8""?>
<entry xmlns=""http://www.w3.org/2005/Atom"">
    <content type=""application/xml"">
        <RegistrationDescription xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"" i:type=""GcmTemplateRegistrationDescription"" xmlns=""http://schemas.microsoft.com/netservices/2010/10/servicebus/connect"">
            <Tags>{0}</Tags>
            <GcmRegistrationId>{1}</GcmRegistrationId> 
            <BodyTemplate>
                <![CDATA[
                    {
                        ""notification"":{
                            ""title"":""Learning Notification Hubs"",
                            ""body"":""$(message)""
                        }
                    }
                ]]>
            </BodyTemplate>
        </RegistrationDescription>
    </content>
</entry>";
            #endregion

            string createRegistration;
            switch (signInDeviceDto.Platform)
            {
                case Platform.Android:
                    createRegistration = androidRegistration;
                    break;
                case Platform.iOS:
                    createRegistration = appleRegistration;
                    break;
                case Platform.UWP:
                    createRegistration = windowsRegistration;
                    break;
                default:
                    throw new ArgumentException($"{signInDeviceDto.Platform} platform is not supported.");
            }
            createRegistration = createRegistration
                .Replace("{0}", $"User_{signInDeviceDto.Username}")
                .Replace("{1}", signInDeviceDto.PnsToken);

            return createRegistration;
        }
    }
}
