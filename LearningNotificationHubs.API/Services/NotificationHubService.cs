using System.Text;
using System.Xml;
using System.Xml.Serialization;
using LearningNotificationHubs.Shared.Dtos;
using LearningNotificationHubs.Shared.Models;
using LearningNotificationHubs.API.Services.Azure;

namespace LearningNotificationHubs.API.Services
{
    public class NotificationHubService : INotificationHubService
    {
        private readonly int _devicesPerNamespace;
        private readonly List<NotificationHubNamespaceCredentials> _notificationHubNamespaces = new List<NotificationHubNamespaceCredentials>();

        public NotificationHubService(IConfiguration configuration)
        {
            var notificationHubServiceConfiguration = configuration.GetSection("NotificationHubService");

            var namespaceCount = notificationHubServiceConfiguration.GetValue<int>("Namespaces");
            _devicesPerNamespace = notificationHubServiceConfiguration.GetValue<int>("DevicesPerNamespace");

            for (var count = 1; count <= namespaceCount; count++)
            {
                _notificationHubNamespaces.Add(new NotificationHubNamespaceCredentials(count, configuration));
            }
        }

        public async Task<(string registrationId, string notificationHubNamespaceName)> CreateRegistration(SignInUserDto signInUserDto)
        {
            // Find the first namespace with a free slot.
            NotificationHubNamespaceCredentials usableNotificationHubNamespace = null;
            foreach (var notificationHubNamespace in _notificationHubNamespaces)
            {
                var registrations = await GetRegistrations(notificationHubNamespace);
                if (registrations.Count() < _devicesPerNamespace)
                {
                    usableNotificationHubNamespace = notificationHubNamespace;
                    break;
                }
            }

            if (usableNotificationHubNamespace is null)
            {
                throw new Exception("All the Notification Hub namespaces are full.");
            }

            string createRegistration = CreateRegistrationBody(signInUserDto);

            string resourceUri = $"https://{usableNotificationHubNamespace.NamespaceName}.servicebus.windows.net/{usableNotificationHubNamespace.HubName}/registrations?api-version=2015-01";
            HttpRequestMessage request = new HttpRequestMessage()
            {
                RequestUri = new Uri(resourceUri),
                Method = HttpMethod.Post,
                Content = new StringContent(createRegistration, Encoding.UTF8, "application/atom+xml")
            };

            string sasToken = Azure.AzureUtililities.GenerateSASToken(resourceUri, usableNotificationHubNamespace.SasKeyName, usableNotificationHubNamespace.SasKey);
            request.Headers.TryAddWithoutValidation("Authorization", sasToken);

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            string responseContent = await response.Content.ReadAsStringAsync();

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(responseContent);

            string registrationId = xmlDocument.GetElementsByTagName("title")[0].InnerText;

            return (registrationId, usableNotificationHubNamespace.NamespaceName);
        }

        public async Task UpdateRegistration(SignInUserDto signInDeviceDto, string registrationId, string namespaceName)
        {
            var notificationHubNamespace = _notificationHubNamespaces.First(nhn => nhn.NamespaceName == namespaceName);

            string registrationBody = CreateRegistrationBody(signInDeviceDto);

            string resourceUri = $"https://{notificationHubNamespace.NamespaceName}.servicebus.windows.net/{notificationHubNamespace.HubName}/registrations/{registrationId}?api-version=2015-01";
            HttpRequestMessage request = new HttpRequestMessage()
            {
                RequestUri = new Uri(resourceUri),
                Method = HttpMethod.Put,
                Content = new StringContent(registrationBody, Encoding.UTF8, "application/atom+xml")
            };

            string sasToken = Azure.AzureUtililities.GenerateSASToken(resourceUri, notificationHubNamespace.SasKeyName, notificationHubNamespace.SasKey);
            request.Headers.TryAddWithoutValidation("Authorization", sasToken);
            request.Headers.TryAddWithoutValidation("If-Match", "*");

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();
        }

        public async Task DeleteRegistration(string registrationId, string namespaceName)
        {
            var notificationHubNamespace = _notificationHubNamespaces.First(nhn => nhn.NamespaceName == namespaceName);

            string resourceUri = $"https://{notificationHubNamespace.NamespaceName}.servicebus.windows.net/{notificationHubNamespace.HubName}/registrations/{registrationId}?api-version=2015-01";
            HttpRequestMessage request = new HttpRequestMessage()
            {
                RequestUri = new Uri(resourceUri),
                Method = HttpMethod.Delete
            };

            string sasToken = Azure.AzureUtililities.GenerateSASToken(resourceUri, notificationHubNamespace.SasKeyName, notificationHubNamespace.SasKey);
            request.Headers.TryAddWithoutValidation("Authorization", sasToken);
            request.Headers.TryAddWithoutValidation("If-Match", "*");

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();
        }



        public async Task SendNotification(string message, string tag)
        {
            // Send the notification to each namespace.
            var sendNotificationTasks = new List<Task<HttpResponseMessage>>();
            foreach (var notificationHubNamespace in _notificationHubNamespaces)
            {
                string notificationBody = "{\"message\":\"" + message + "\"}";

                string resourceUri = $"https://{notificationHubNamespace.NamespaceName}.servicebus.windows.net/{notificationHubNamespace.HubName}/messages/?api-version=2015-01";
                HttpRequestMessage request = new HttpRequestMessage()
                {
                    RequestUri = new Uri(resourceUri),
                    Method = HttpMethod.Post,
                    Content = new StringContent(notificationBody, Encoding.UTF8, "application/json")
                };

                string sasToken = Azure.AzureUtililities.GenerateSASToken(resourceUri, notificationHubNamespace.SasKeyName, notificationHubNamespace.SasKey);
                request.Headers.TryAddWithoutValidation("Authorization", sasToken);
                request.Headers.TryAddWithoutValidation("ServiceBusNotification-Tags", tag);
                request.Headers.TryAddWithoutValidation("ServiceBusNotification-Format", "template");

                HttpClient httpClient = new HttpClient();

                sendNotificationTasks.Add(httpClient.SendAsync(request));
            }

            await Task.WhenAll(sendNotificationTasks);

            foreach (var sendNotificationTask in sendNotificationTasks)
            {
                sendNotificationTask.Result.EnsureSuccessStatusCode();
            }
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

        private async Task<List<string>> GetRegistrations(NotificationHubNamespaceCredentials notificationHubNamespace)
        {
            string resourceUri = $"https://{notificationHubNamespace.NamespaceName}.servicebus.windows.net/{notificationHubNamespace.HubName}/registrations/?api-version=2015-01";
            HttpRequestMessage request = new HttpRequestMessage()
            {
                RequestUri = new Uri(resourceUri),
                Method = HttpMethod.Get
            };

            string sasToken = Azure.AzureUtililities.GenerateSASToken(resourceUri, notificationHubNamespace.SasKeyName, notificationHubNamespace.SasKey);
            request.Headers.TryAddWithoutValidation("Authorization", sasToken);

            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.SendAsync(request);

            response.EnsureSuccessStatusCode();

            XmlSerializer xmlSerializer = new XmlSerializer(typeof(Azure.feed));
            Stream stream = await response.Content.ReadAsStreamAsync();

            Azure.feed feedVar = xmlSerializer.Deserialize(stream) as Azure.feed;

            // The feed contains a list of entries. Each entry has a 'title' that is its ID.
            List<string> entries = new List<string>();
            if (feedVar.entry is not null)
            {
                foreach (Azure.feedEntry entry in feedVar.entry)
                {
                    entries.Add(entry.title.Value);
                }
            }

            return entries;
        }
    }
}
