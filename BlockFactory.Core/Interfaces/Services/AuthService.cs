
using BlockFactory.Core.Interfaces;
using BlockFactory.Core.Interfaces.Services;
using BlockFactory.Core.Models.Auth;
using BlockFactory.Core.Models.Finance;
using BlockFactory.Core.Session;
using Microsoft.EntityFrameworkCore;

namespace BlockFactory.Core.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _uow;

        public AuthService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        // ─── تسجيل الدخول ──────────────────────────
        public async Task<AuthResult> LoginAsync(
            string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) ||
                    string.IsNullOrWhiteSpace(password))
                    return AuthResult.Fail("يرجى إدخال اسم المستخدم وكلمة المرور");

                // البحث عن المستخدم
                var user = await _uow.Users
                    .Query()
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u =>
                        u.Username == username.Trim() &&
                        u.IsActive &&
                        !u.IsDeleted);

                if (user == null)
                    return AuthResult.Fail("اسم المستخدم غير موجود");

                // التحقق من كلمة المرور
                bool isValid = BCrypt.Net.BCrypt
                    .Verify(password, user.PasswordHash);

                if (!isValid)
                    return AuthResult.Fail("كلمة المرور غير صحيحة");

                // تحديث وقت آخر دخول
                user.LastLoginAt = DateTime.Now;
                _uow.Users.Update(user);
                await _uow.SaveChangesAsync();

                // حفظ الجلسة
                CurrentSession.Instance.SetSession(user);

                // تسجيل النشاط
                await LogActivityAsync("Login", "User", user.Id);

                return AuthResult.Ok(user);
            }
            catch (Exception ex)
            {
                return AuthResult.Fail($"خطأ في تسجيل الدخول: {ex.Message}");
            }
        }

        // ─── تسجيل الخروج ──────────────────────────
        public async Task LogoutAsync()
        {
            await LogActivityAsync("Logout", "User",
                CurrentSession.Instance.UserId);

            CurrentSession.Instance.ClearSession();
        }

        // ─── تغيير كلمة المرور ─────────────────────
        public async Task<bool> ChangePasswordAsync(
            int userId, string oldPassword, string newPassword)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) return false;

            bool isValid = BCrypt.Net.BCrypt
                .Verify(oldPassword, user.PasswordHash);

            if (!isValid) return false;

            if (newPassword.Length < 6) return false;

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.Now;

            _uow.Users.Update(user);
            await _uow.SaveChangesAsync();

            await LogActivityAsync("ChangePassword", "User", userId);

            return true;
        }

        // ─── إنشاء مستخدم جديد ─────────────────────
        public async Task<AuthResult> CreateUserAsync(
            string fullName, string username,
            string password, int roleId)
        {
            // التحقق من عدم تكرار اسم المستخدم
            bool exists = await _uow.Users.AnyAsync(
                u => u.Username == username);

            if (exists)
                return AuthResult.Fail("اسم المستخدم موجود مسبقاً");

            if (password.Length < 6)
                return AuthResult.Fail("كلمة المرور يجب أن تكون 6 أحرف على الأقل");

            var user = new User
            {
                FullName = fullName,
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                RoleId = roleId,
                IsActive = true,
                CreatedAt = DateTime.Now,
                CreatedByUserId = CurrentSession.Instance.UserId
            };

            await _uow.Users.AddAsync(user);
            await _uow.SaveChangesAsync();

            await LogActivityAsync("CreateUser", "User", user.Id);

            return AuthResult.Ok(user);
        }

        public async Task<int?> GetRoleIdByNameAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return null;

            var role = await _uow.Roles
                .Query()
                .FirstOrDefaultAsync(r =>
                    r.Name == roleName.Trim());

            return role?.Id;
        }

        // ─── تفعيل / إلغاء تفعيل ───────────────────
        public async Task<bool> DeactivateUserAsync(int userId)
        {
            // لا يمكن إلغاء تفعيل المدير الرئيسي
            if (userId == 1) return false;

            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) return false;

            user.IsActive = false;
            user.UpdatedAt = DateTime.Now;
            _uow.Users.Update(user);
            await _uow.SaveChangesAsync();

            await LogActivityAsync("DeactivateUser", "User", userId);
            return true;
        }

        public async Task<bool> ActivateUserAsync(int userId)
        {
            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) return false;

            user.IsActive = true;
            user.UpdatedAt = DateTime.Now;
            _uow.Users.Update(user);
            await _uow.SaveChangesAsync();

            await LogActivityAsync("ActivateUser", "User", userId);
            return true;
        }

        // ─── استعلامات ─────────────────────────────
        public async Task<User?> GetUserByIdAsync(int userId)
            => await _uow.Users
                .Query()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);

        public async Task<IEnumerable<User>> GetAllUsersAsync()
            => await _uow.Users
                .Query()
                .Include(u => u.Role)
                .OrderBy(u => u.FullName)
                .ToListAsync();

        // ─── تسجيل الأنشطة ─────────────────────────
        public async Task LogActivityAsync(
            string action, string entityName,
            int? entityId = null,
            string? oldValues = null,
            string? newValues = null)
        {
            try
            {
                var log = new ActivityLog
                {
                    Action = action,
                    EntityName = entityName,
                    EntityId = entityId,
                    OldValues = oldValues,
                    NewValues = newValues,
                    UserId = CurrentSession.Instance.UserId,
                    LoggedAt = DateTime.Now,
                    CreatedAt = DateTime.Now
                };

                await _uow.ActivityLogs.AddAsync(log);
                await _uow.SaveChangesAsync();
            }
            catch
            {
                // لا نوقف التطبيق بسبب فشل تسجيل النشاط
            }
        }
    }
}
