using LearningNotificationHubs.Shared.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace LearningNotificationHubs.Shared.Dtos
{
    public class SignInUserDto
    {
        [Required]
        public string Username { get; set; }

        [Required]
        public Guid? DeviceId { get; set; }

        [Required]
        public Platform? Platform { get; set; }

        [Required]
        public string PnsToken { get; set; }    // Called GcmRegistrationId (Android), DeviceToken (iOS) and ChannelUri (UWP)
    }
}
