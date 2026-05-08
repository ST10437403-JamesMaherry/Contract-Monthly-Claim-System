using Contract_Monthly_Claim_System.Data;
using Contract_Monthly_Claim_System.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Contract_Monthly_Claim_System.Services
{
    public interface IAuthenticationService
    {
        string HashPassword(string password, out string salt);
        bool VerifyPassword(string password, string hash, string salt);
        Task<User?> ValidateUserAsync(string email, string password);
        Task<bool> SetUserPasswordAsync(int userId, string password, bool mustChangePassword = false);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthenticationService(ApplicationDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        // Hashes new passwords using ASP.NET Core Identity's PBKDF2-based password hasher.
        public string HashPassword(string password, out string salt)
        {
            salt = string.Empty; // Identity stores the salt inside the password hash.
            return _passwordHasher.HashPassword(new User(), password);
        }

        // Verifies both new Identity hashes and existing seeded SHA256 hashes.
        public bool VerifyPassword(string password, string hash, string salt)
        {
            if (VerifyIdentityPassword(new User(), password, hash) != PasswordVerificationResult.Failed)
            {
                return true;
            }

            return VerifyLegacyPassword(password, hash, salt);
        }

        // Validate user credentials
        public async Task<User?> ValidateUserAsync(string email, string password)
        {
            var normalizedEmail = email.Trim().ToLower();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.email.ToLower() == normalizedEmail);

            if (user == null || string.IsNullOrEmpty(user.passwordHash))
                return null;

            var result = VerifyIdentityPassword(user, password, user.passwordHash);

            if (result == PasswordVerificationResult.Success)
                return user;

            if (result == PasswordVerificationResult.SuccessRehashNeeded || VerifyLegacyPassword(password, user.passwordHash, user.passwordSalt))
            {
                await UpgradePasswordHashAsync(user, password);
                return user;
            }

            return null;
        }

        // Set/update user password (HR only)
        public async Task<bool> SetUserPasswordAsync(int userId, string password, bool mustChangePassword = false)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.passwordHash = _passwordHasher.HashPassword(user, password);
            user.passwordSalt = string.Empty;
            user.mustChangePassword = mustChangePassword;

            await _context.SaveChangesAsync();
            return true;
        }

        // Changes a user's password after verifying the current password.
        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.passwordHash))
                return false;

            if (!VerifyPassword(currentPassword, user.passwordHash, user.passwordSalt))
                return false;

            user.passwordHash = _passwordHasher.HashPassword(user, newPassword);
            user.passwordSalt = string.Empty;
            user.mustChangePassword = false;

            await _context.SaveChangesAsync();
            return true;
        }

        private PasswordVerificationResult VerifyIdentityPassword(User user, string password, string hash)
        {
            try
            {
                return _passwordHasher.VerifyHashedPassword(user, hash, password);
            }
            catch (FormatException)
            {
                return PasswordVerificationResult.Failed;
            }
        }

        private bool VerifyLegacyPassword(string password, string hash, string salt)
        {
            if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(salt))
                return false;

            using (var sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password + salt);
                byte[] hashBytes = sha256.ComputeHash(passwordBytes);
                string computedHash = Convert.ToBase64String(hashBytes);
                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(computedHash),
                    Encoding.UTF8.GetBytes(hash));
            }
        }

        private async Task UpgradePasswordHashAsync(User user, string password)
        {
            user.passwordHash = _passwordHasher.HashPassword(user, password);
            user.passwordSalt = string.Empty;
            await _context.SaveChangesAsync();
        }
    }
}
