using System.ComponentModel.DataAnnotations;

namespace LearningNotificationHubs.Shared.Dtos
{
    public class SendNotificationDto
    {
        [Required]
        public string Username { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
