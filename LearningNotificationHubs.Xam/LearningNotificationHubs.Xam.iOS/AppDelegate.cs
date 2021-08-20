
using Foundation;
using LearningNotificationHubs.Xam.iOS.Extensions;
using UIKit;
using UserNotifications;

namespace LearningNotificationHubs.Xam.iOS
{
    // The UIApplicationDelegate for the application. This class is responsible for launching the 
    // User Interface of the application, as well as listening (and optionally responding) to 
    // application events from iOS.
    [Register("AppDelegate")]
    public partial class AppDelegate : Xamarin.Forms.Platform.iOS.FormsApplicationDelegate
    {
        //
        // This method is invoked when the application has loaded and is ready to run. In this 
        // method you should instantiate the window, load the UI into it and then make the window
        // visible.
        //
        // You have 17 seconds to return from this method, or iOS will terminate your application.
        //
        public override bool FinishedLaunching(UIApplication app, NSDictionary options)
        {
            Xamarin.Forms.Forms.Init();
            LoadApplication(new App());

            // Register for push notifications
            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Alert | UNAuthorizationOptions.Sound, (granted, error) =>
                {
                    if (granted)
                    {
                        InvokeOnMainThread(UIApplication.SharedApplication.RegisterForRemoteNotifications);
                    }
                });
            }
            else if (UIDevice.CurrentDevice.CheckSystemVersion(8, 0))
            {
                UIUserNotificationSettings notificationSettings = UIUserNotificationSettings.GetSettingsForTypes(UIUserNotificationType.Alert | UIUserNotificationType.Badge | UIUserNotificationType.Sound, null);
                UIApplication.SharedApplication.RegisterUserNotificationSettings(notificationSettings);
                UIApplication.SharedApplication.RegisterForRemoteNotifications();
            }
            else
            {
                UIRemoteNotificationType notificationTypes = UIRemoteNotificationType.Alert | UIRemoteNotificationType.Badge | UIRemoteNotificationType.Sound;
                UIApplication.SharedApplication.RegisterForRemoteNotificationTypes(notificationTypes);
            }     

            return base.FinishedLaunching(app, options);
        }

        public override void RegisteredForRemoteNotifications(UIApplication application, NSData deviceToken)
        {
            string deviceTokenHexString = deviceToken.ToHexString();
            
            if (string.IsNullOrWhiteSpace(deviceTokenHexString))
            {
                return;
            }

            if (!(Xamarin.Forms.Application.Current?.MainPage is MainPage mainPage))
            {
                return;
            }

            mainPage.PnsToken = deviceToken.ToString(NSStringEncoding.UTF8);
        }

        public override void ReceivedRemoteNotification(UIApplication application, NSDictionary userInfo)
        {
            // Make sure we have a payload
            if (userInfo is null || !userInfo.ContainsKey(new NSString("aps")))
            {
                return;
            }

            NSDictionary aps = userInfo.ObjectForKey(new NSString("aps")) as NSDictionary;

            string payload = string.Empty;

            NSString payloadKey = new NSString("alert");

            if (aps.ContainsKey(payloadKey))
            {
                payload = aps[payloadKey].ToString();
            }

            if (!string.IsNullOrWhiteSpace(payload))
            {
                UILocalNotification notification = new UILocalNotification();
                notification.AlertAction = "Learning Notification Hubs";
                notification.AlertBody = payload;
                UIApplication.SharedApplication.ScheduleLocalNotification(notification);
            }                           
        }
    }
}
