using System;
using System.Linq;
using System.Threading.Tasks;
using CIA.Core.Constants;
using CIA.Core.DTOs;
using CIA.Core.Helpers;
using CIA.Data.Repositories;
using NLog;

namespace CIA.Services.Auth
{
    public interface IAuthService
    {
        Task<LoginResultDto> LoginAsync(LoginDto loginDto);
        Task LogoutAsync(int userId);
        Task<bool> ChangePasswordAsync(ChangePasswordDto dto);
        Task<UserDto> GetCurrentUserAsync(int userId);
        bool HasPermission(UserDto user, string permission);
    }

    public class AuthService : IAuthService
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<LoginResultDto> LoginAsync(LoginDto loginDto)
        {
            Logger.Info($"Giriş denemesi: {loginDto.Username}");

            if (string.IsNullOrWhiteSpace(loginDto.Username) || string.IsNullOrWhiteSpace(loginDto.Password))
            {
                return new LoginResultDto { Success = false, Message = "Kullanıcı adı ve şifre gereklidir." };
            }

            var user = await _unitOfWork.Users.GetByUsernameAsync(loginDto.Username);

            if (user == null)
            {
                Logger.Warn($"Kullanıcı bulunamadı: {loginDto.Username}");
                return new LoginResultDto { Success = false, Message = "Kullanıcı adı veya şifre hatalı." };
            }

            if (!user.IsActive)
            {
                return new LoginResultDto { Success = false, Message = "Hesabınız devre dışı bırakılmıştır." };
            }

            if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
            {
                var remaining = (user.LockedUntil.Value - DateTime.UtcNow).Minutes;
                return new LoginResultDto
                {
                    Success = false,
                    Message = $"Hesabınız kilitlenmiştir. {remaining} dakika sonra tekrar deneyin."
                };
            }

            if (!SecurityHelper.VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                await _unitOfWork.Users.IncrementFailedLoginAsync(user.Id);

                if (user.FailedLoginAttempts + 1 >= AppConstants.MaxLoginAttempts)
                {
                    var lockUntil = DateTime.UtcNow.AddMinutes(AppConstants.LockoutDurationMinutes);
                    await _unitOfWork.Users.LockUserAsync(user.Id, lockUntil);
                    Logger.Warn($"Hesap kilitlendi: {loginDto.Username}");
                    return new LoginResultDto
                    {
                        Success = false,
                        Message = $"Çok fazla başarısız giriş denemesi. Hesabınız {AppConstants.LockoutDurationMinutes} dakika kilitlendi."
                    };
                }

                Logger.Warn($"Hatalı şifre: {loginDto.Username}");
                return new LoginResultDto { Success = false, Message = "Kullanıcı adı veya şifre hatalı." };
            }

            await _unitOfWork.Users.UpdateLastLoginAsync(user.Id);

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Roles = user.UserRoles?.Select(ur => ur.Role?.Name).Where(r => r != null).ToList()
                        ?? new System.Collections.Generic.List<string>()
            };

            Logger.Info($"Başarılı giriş: {loginDto.Username}");

            return new LoginResultDto
            {
                Success = true,
                Message = "Giriş başarılı.",
                User = userDto,
                Token = SecurityHelper.GenerateSecureToken()
            };
        }

        public async Task LogoutAsync(int userId)
        {
            Logger.Info($"Çıkış yapıldı: UserId={userId}");
            await Task.CompletedTask;
        }

        public async Task<bool> ChangePasswordAsync(ChangePasswordDto dto)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(dto.UserId);
            if (user == null) return false;

            if (!SecurityHelper.VerifyPassword(dto.CurrentPassword, user.PasswordHash))
                return false;

            if (dto.NewPassword != dto.ConfirmPassword) return false;

            var (isValid, _) = SecurityHelper.ValidatePasswordStrength(dto.NewPassword);
            if (!isValid) return false;

            user.PasswordHash = SecurityHelper.HashPassword(dto.NewPassword);
            await _unitOfWork.SaveChangesAsync();

            Logger.Info($"Şifre değiştirildi: UserId={dto.UserId}");
            return true;
        }

        public async Task<UserDto> GetCurrentUserAsync(int userId)
        {
            var user = await _unitOfWork.Users.GetWithRolesAsync(userId);
            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FullName = user.FullName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                Roles = user.UserRoles?.Select(ur => ur.Role?.Name).Where(r => r != null).ToList()
                        ?? new System.Collections.Generic.List<string>()
            };
        }

        public bool HasPermission(UserDto user, string permission)
        {
            if (user == null) return false;
            if (user.Roles.Contains("Admin")) return true;

            switch (permission)
            {
                case "ViewData": return user.Roles.Contains("Analyst") || user.Roles.Contains("Viewer") || user.Roles.Contains("Operator");
                case "ImportData": return user.Roles.Contains("Analyst") || user.Roles.Contains("Operator");
                case "RunAnalysis": return user.Roles.Contains("Analyst");
                case "GenerateReports": return user.Roles.Contains("Analyst");
                case "ManageUsers": return user.Roles.Contains("Admin");
                case "ManageSites": return user.Roles.Contains("Analyst") || user.Roles.Contains("Operator");
                default: return false;
            }
        }
    }
}
