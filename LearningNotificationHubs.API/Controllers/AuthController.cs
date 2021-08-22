using LearningNotificationHubs.API.Data;
using LearningNotificationHubs.API.Models;
using LearningNotificationHubs.API.Services;
using LearningNotificationHubs.Shared.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LearningNotificationHubs.API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly INotificationHubService _notificationHubService;
        public AuthController(ApplicationDbContext applicationDbContext, INotificationHubService notificationHubService)
        {
            _applicationDbContext = applicationDbContext;
            _notificationHubService = notificationHubService;
        }

        [HttpPost("SignUserIn")]
        public async Task<IActionResult> SignUserIn(SignInUserDto signInUserDto)
        {
            // Check if this user exists.
            User user = _applicationDbContext.Users.FirstOrDefault(u => u.Username == signInUserDto.Username);

            // If the user does not exist, create the user and the device.
            if (user is null)
            {
                user = new User
                {
                    Username = signInUserDto.Username,
                };

                await _applicationDbContext.Users.AddAsync(user);

                // Register with Azure Notification Hub
                var installationIdAndNamespaceName = await _notificationHubService.CreateRegistration(signInUserDto);

                Device device = new Device
                {
                    Id = signInUserDto.DeviceId.Value,
                    Platform = signInUserDto.Platform.Value,
                    PnsToken = signInUserDto.PnsToken,
                    Username = signInUserDto.Username,
                    RegistrationId = installationIdAndNamespaceName.registrationId,
                    NotificationHubNamespaceName = installationIdAndNamespaceName.notificationHubNamespaceName
                };

                await _applicationDbContext.Devices.AddAsync(device);
            }

            // Check if this device has been assigned to the user.
            else
            {

                Device device = await _applicationDbContext.Devices
                    .FirstOrDefaultAsync(d => d.Id == signInUserDto.DeviceId && d.Username == signInUserDto.Username);

                // If the device has not been assigned, assign it.
                if (device is null)
                {
                    var installationIdAndNamespaceName = await _notificationHubService.CreateRegistration(signInUserDto);

                    device = new Device
                    {
                        Id = signInUserDto.DeviceId.Value,
                        Platform = signInUserDto.Platform.Value,
                        PnsToken = signInUserDto.PnsToken,
                        Username = signInUserDto.Username,
                        RegistrationId = installationIdAndNamespaceName.registrationId,
                        NotificationHubNamespaceName = installationIdAndNamespaceName.notificationHubNamespaceName
                    };

                    await _applicationDbContext.Devices.AddAsync(device);
                }
                else
                // Update the registration
                {
                    await _notificationHubService.UpdateRegistration(signInUserDto, device.RegistrationId, device.NotificationHubNamespaceName);

                    device.RegistrationId = signInUserDto.PnsToken;
                }
            }

            // Finally, save the changes.
            await _applicationDbContext.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("SignUserOut")]
        public async Task<IActionResult> SignUserOut(SignOutUserDto signOutUserDto)
        {

            // Check if this user exists.
            User user = _applicationDbContext.Users.FirstOrDefault(u => u.Username == signOutUserDto.Username);

            // If the user does not exist, return BadRequest
            if (user is null)
            {
                return BadRequest("The user does not exist.");
            }

            // Check if this device has been assigned to the user.
            Device device = await _applicationDbContext.Devices
                .FirstOrDefaultAsync(d => d.Id == signOutUserDto.DeviceId && d.Username == signOutUserDto.Username);

            // If the device has not been assigned, unassign it.
            if (device is not null)
            {
                await _notificationHubService.DeleteRegistration(device.RegistrationId, device.NotificationHubNamespaceName);

                _applicationDbContext.Devices.Remove(device);
                await _applicationDbContext.SaveChangesAsync();
            }

            // If the user has no more devices left, delete them.
            int deviceCount = await _applicationDbContext.Devices
                .Where(d => d.Username == signOutUserDto.Username)
                .CountAsync();

            if (deviceCount == 0)
            {
                _applicationDbContext.Remove(user);
            }

            // Finally, save the changes.
            await _applicationDbContext.SaveChangesAsync();

            return Ok();
        }
    }
}
