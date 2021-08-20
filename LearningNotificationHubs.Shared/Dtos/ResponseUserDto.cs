using System;
using System.Collections.Generic;

namespace LearningNotificationHubs.Shared.Dtos
{
    public class ResponseUserDto
    {
        public string Username { get; set; }

        public List<ResponseDeviceDto> Devices { get; set; } = new List<ResponseDeviceDto>();


        public ResponseUserDto(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new ArgumentNullException(nameof(username));
            }

            Username = username;
        }
    }
}
