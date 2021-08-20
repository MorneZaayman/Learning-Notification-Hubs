
using LearningNotificationHubs.Shared.Dtos;

namespace LearningNotificationHubs.API.Services
{
    public interface INotificationHubService
    {
        Task<string> CreateRegistration(SignInUserDto signInDeviceDto);
        Task<string> UpdateRegistration(SignInUserDto signInDeviceDto, string registrationId);
        Task DeleteRegistration(string registrationId);
        //Task<List<string>> GetRegistrations();
        Task SendNotification(string message, string tag);
    }
}
