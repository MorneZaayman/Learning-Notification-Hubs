
using LearningNotificationHubs.Shared.Dtos;

namespace LearningNotificationHubs.API.Services
{
    public interface INotificationHubService
    {
        Task<(string registrationId, string notificationHubNamespaceName)> CreateRegistration(SignInUserDto signInUserDto);
        Task UpdateRegistration(SignInUserDto signInDeviceDto, string registrationId, string namespaceName);
        Task DeleteRegistration(string registrationId, string namespaceName);
        Task SendNotification(string message, string tag);
    }
}
