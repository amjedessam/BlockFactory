using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BlockFactory.Core.Models.Auth;

namespace BlockFactory.Core.Interfaces.Services
{
    public interface IAuthService
    {
        Task<AuthResult> LoginAsync(string username, string password);
        Task LogoutAsync();
        Task<bool> ChangePasswordAsync(
            int userId, string oldPassword, string newPassword);
        Task<User?> GetUserByIdAsync(int userId);
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<AuthResult> CreateUserAsync(
            string fullName, string username,
            string password, int roleId);
        /// <summary>معرف الدور حسب الاسم (مثل Admin أو Accountant).</summary>
        Task<int?> GetRoleIdByNameAsync(string roleName);
        Task<bool> DeactivateUserAsync(int userId);
        Task<bool> ActivateUserAsync(int userId);
        Task LogActivityAsync(string action, string entityName,
            int? entityId = null, string? oldValues = null,
            string? newValues = null);
    }

    public class AuthResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public User? User { get; set; }

        public static AuthResult Ok(User user) => new()
        {
            Success = true,
            Message = "تم تسجيل الدخول بنجاح",
            User = user
        };

        public static AuthResult Fail(string message) => new()
        {
            Success = false,
            Message = message
        };
    }
}
