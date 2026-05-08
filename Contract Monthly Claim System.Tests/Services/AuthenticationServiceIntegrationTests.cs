using Contract_Monthly_Claim_System.Data;
using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Contract_Monthly_Claim_System.Tests.Services
{
    public class AuthenticationServiceIntegrationTests
    {
        [Fact]
        public async Task ChangePasswordAsync_UpdatesStoredPasswordAndClearsFirstLoginFlag()
        {
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await using var context = await CreateContextAsync(connection);
            var service = new AuthenticationService(context);

            var tempHash = service.HashPassword("TempPass123", out var tempSalt);
            var user = new User
            {
                firstName = "New",
                lastName = "Lecturer",
                email = "newlecturer@university.co.za",
                phoneNumber = "+27 11 000 0000",
                userRole = "Lecturer",
                hourlyRate = 175,
                passwordHash = tempHash,
                passwordSalt = tempSalt,
                mustChangePassword = true
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var loginWithTemporaryPassword = await service.ValidateUserAsync(user.email, "TempPass123");

            Assert.NotNull(loginWithTemporaryPassword);
            Assert.True(loginWithTemporaryPassword.mustChangePassword);

            var changed = await service.ChangePasswordAsync(user.userId, "TempPass123", "PermanentPass123");

            var updatedUser = await context.Users.SingleAsync(u => u.userId == user.userId);
            var oldPasswordLogin = await service.ValidateUserAsync(user.email, "TempPass123");
            var newPasswordLogin = await service.ValidateUserAsync(user.email, "PermanentPass123");

            Assert.True(changed);
            Assert.False(updatedUser.mustChangePassword);
            Assert.Null(oldPasswordLogin);
            Assert.NotNull(newPasswordLogin);
            Assert.False(newPasswordLogin.mustChangePassword);
        }

        [Fact]
        public async Task ChangePasswordAsync_RejectsIncorrectCurrentPassword()
        {
            await using var connection = new SqliteConnection("DataSource=:memory:");
            await using var context = await CreateContextAsync(connection);
            var service = new AuthenticationService(context);

            var tempHash = service.HashPassword("TempPass123", out var tempSalt);
            var user = new User
            {
                firstName = "New",
                lastName = "Lecturer",
                email = "wrongcurrent@university.co.za",
                phoneNumber = "+27 11 000 0001",
                userRole = "Lecturer",
                hourlyRate = 175,
                passwordHash = tempHash,
                passwordSalt = tempSalt,
                mustChangePassword = true
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var changed = await service.ChangePasswordAsync(user.userId, "WrongPassword", "PermanentPass123");
            var updatedUser = await context.Users.SingleAsync(u => u.userId == user.userId);

            Assert.False(changed);
            Assert.True(updatedUser.mustChangePassword);
            Assert.True(service.VerifyPassword("TempPass123", updatedUser.passwordHash, updatedUser.passwordSalt));
        }

        private static async Task<ApplicationDbContext> CreateContextAsync(SqliteConnection connection)
        {
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;

            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();

            return context;
        }
    }
}
