using System;
using System.ComponentModel.DataAnnotations;

namespace LearningNotificationHubs.Shared.Dtos
{
    public class SignOutUserDto
    {
        [Required]
        public string Username { get; set; }  

        [Required]
        public Guid? DeviceId { get; set; }
    }
}
