using Contract_Monthly_Claim_System.Data;
using Contract_Monthly_Claim_System.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Contract_Monthly_Claim_System.Services
{
    public interface IAuthenticationService
    {
        string HashPassword(string password, out string salt);
        bool VerifyPassword(string password, string hash, string salt);
        Task<User> ValidateUserAsync(string email, string password);
        Task<bool> SetUserPasswordAsync(int userId, string password);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly ApplicationDbContext _context;

        public AuthenticationService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hash password with unique salt
        public string HashPassword(string password, out string salt)
        {
            // Generate random salt
            byte[] saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            salt = Convert.ToBase64String(saltBytes);

            // Hash password with salt
            using (var sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password + salt);
                byte[] hashBytes = sha256.ComputeHash(passwordBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        // Verify password against stored hash
        public bool VerifyPassword(string password, string hash, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password + salt);
                byte[] hashBytes = sha256.ComputeHash(passwordBytes);
                string computedHash = Convert.ToBase64String(hashBytes);
                return computedHash == hash;
            }
        }

        // Validate user credentials
        public async Task<User> ValidateUserAsync(string email, string password)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.email == email);

            if (user == null || string.IsNullOrEmpty(user.passwordHash))
                return null;

            if (VerifyPassword(password, user.passwordHash, user.passwordSalt))
                return user;

            return null;
        }

        // Set/update user password (HR only)
        public async Task<bool> SetUserPasswordAsync(int userId, string password)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.passwordHash = HashPassword(password, out string salt);
            user.passwordSalt = salt;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
