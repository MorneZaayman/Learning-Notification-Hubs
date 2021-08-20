using LearningNotificationHubs.Shared.Models;
using System;

namespace LearningNotificationHubs.Shared.Dtos
{
    public class ResponseDeviceDto
    {
        public string DeviceId { get; set; }
        public string Platform { get; set; }
        public string RegistrationId { get; set; }
        public string PnsToken { get; set; }

        public ResponseDeviceDto(Guid deviceId, Platform platform, string registrationId, string pnsToken)
        {
            DeviceId = deviceId.ToString();
            Platform = platform.ToString();
            RegistrationId = registrationId;
            PnsToken = pnsToken;
        }
    }
}
