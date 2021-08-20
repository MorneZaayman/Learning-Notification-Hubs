using Foundation;
using System.Text;

namespace LearningNotificationHubs.Xam.iOS.Extensions
{
    // Ref: https://docs.microsoft.com/en-us/azure/developer/mobile-apps/notification-hubs-backend-service-xamarin-forms#configure-the-native-ios-project-for-push-notifications
    internal static class NSDataExtensions
    {
        internal static string ToHexString(this NSData data)
        {
            byte[] bytes = data.ToArray();

            if (bytes == null)
            {
                return null;
            }

            StringBuilder stringBuilder = new StringBuilder(bytes.Length * 2);

            foreach (byte b in bytes)
            {
                stringBuilder.AppendFormat("{0:x2}", b);
            }

            return stringBuilder
                .ToString()
                .ToUpperInvariant();
        }
    }
}