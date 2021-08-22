namespace LearningNotificationHubs.API.Services.Azure
{
    public class NotificationHubNamespaceCredentials
    {
        public string NamespaceName { get; }
        public string HubName { get; }
        public string SasKeyName { get; }
        public string SasKey { get; }

        public NotificationHubNamespaceCredentials(int count, IConfiguration configuration)
        {
            var notificationHubServiceConfiguration = configuration.GetSection("NotificationHubService");

            NamespaceName = notificationHubServiceConfiguration.GetValue<string>($"Namespace{count}:NamespaceName");
            HubName = notificationHubServiceConfiguration.GetValue<string>($"Namespace{count}:HubName");
            SasKeyName = notificationHubServiceConfiguration.GetValue<string>($"Namespace{count}:SasKeyName");
            SasKey = notificationHubServiceConfiguration.GetValue<string>($"Namespace{count}:SasKey");
        }
    }
}