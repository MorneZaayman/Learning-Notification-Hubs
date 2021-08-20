namespace LearningNotificationHubs.API.Models
{
    public class User
    {
        public string Username { get; set; }
        public List<Device> Devices { get; set; } = new List<Device>();
    }
}
