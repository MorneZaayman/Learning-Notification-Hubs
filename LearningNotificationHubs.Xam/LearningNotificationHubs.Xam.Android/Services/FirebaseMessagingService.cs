namespace LearningNotificationHubs.Xam.Droid.Services
{
    [Service]
    [IntentFilter(new[] {"com.google.firebase.MESSAGING_EVENT"})]
    public class MyFirebaseMessagingService : FirebaseMessagingService
    {
        internal static readonly string NOTIFICATION_CHANNEL_ID = "NOTIFICATION_CHANNEL_ID";
        
        public override void OnNewToken(string newToken)
        {
            if (string.IsNullOrWhiteSpace(newToken))
            {
                return;
            }

            if (!(Xamarin.Forms.Application.Current?.MainPage is MainPage mainPage))
            {
                return;
            }
            
            mainPage.PnsToken = newToken;
        }

        public override void OnMessageReceived(RemoteMessage remoteMessage)
        {
            base.OnMessageReceived(remoteMessage);

            string messageBody = remoteMessage.GetNotification()?.Body;

            if (messageBody is null)
            {
                messageBody = remoteMessage.Data.Values.FirstOrDefault() ?? string.Empty;
            }

            // Send local notification
            Intent intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            intent.PutExtra("message", messageBody);

            // Unique require code to avoid PendingIntent collision
            int requestCode = new Random().Next();
            PendingIntent pendingIntent = PendingIntent.GetActivity(this, requestCode, intent, PendingIntentFlags.OneShot);

            NotificationCompat.Builder notificationBuilder = new NotificationCompat.Builder(this, NOTIFICATION_CHANNEL_ID)
                .SetContentTitle("Learning Notification Hubs")
                .SetSmallIcon(Resource.Mipmap.icon)
                .SetContentText(messageBody)
                .SetContentIntent(pendingIntent);

            NotificationManager notificationManager = NotificationManager.FromContext(this);
            notificationManager.Notify(0, notificationBuilder.Build());
        }
    }
}