using Contract_Monthly_Claim_System.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Contract_Monthly_Claim_System.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets for entities
        public DbSet<User> Users { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<ClaimStatus> ClaimStatuses { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure primary keys with auto-increment
            modelBuilder.Entity<ClaimStatus>()
                .HasKey(cs => cs.statusId);
            modelBuilder.Entity<ClaimStatus>()
                .Property(cs => cs.statusId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<User>()
                .HasKey(u => u.userId);
            modelBuilder.Entity<User>()
                .Property(u => u.userId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Claim>()
                .HasKey(c => c.claimId);
            modelBuilder.Entity<Claim>()
                .Property(c => c.claimId)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Document>()
                .HasKey(d => d.documentId);
            modelBuilder.Entity<Document>()
                .Property(d => d.documentId)
                .ValueGeneratedOnAdd();

            // Configure decimal precision for financial fields
            modelBuilder.Entity<User>()
                .Property(u => u.hourlyRate)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Claim>()
                .Property(c => c.hoursWorked)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Claim>()
                .Property(c => c.hourlyRate)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Claim>()
                .Property(c => c.totalAmount)
                .HasColumnType("decimal(18,2)");

            // Configure string field lengths
            modelBuilder.Entity<User>()
                .Property(u => u.firstName)
                .HasMaxLength(100);

            modelBuilder.Entity<User>()
                .Property(u => u.lastName)
                .HasMaxLength(100);

            modelBuilder.Entity<User>()
                .Property(u => u.email)
                .HasMaxLength(255);

            modelBuilder.Entity<User>()
                .Property(u => u.phoneNumber)
                .HasMaxLength(20);

            modelBuilder.Entity<User>()
                .Property(u => u.userRole)
                .HasMaxLength(50);

            modelBuilder.Entity<Document>()
                .Property(d => d.fileName)
                .HasMaxLength(255);

            modelBuilder.Entity<Document>()
                .Property(d => d.fileType)
                .HasMaxLength(50);

            modelBuilder.Entity<ClaimStatus>()
                .Property(cs => cs.statusName)
                .HasMaxLength(50);

            modelBuilder.Entity<Claim>()
                .Property(c => c.Notes)
                .HasMaxLength(500);

            // Configure relationships
            modelBuilder.Entity<Claim>()
                .HasOne(c => c.User)
                .WithMany(u => u.Claims)
                .HasForeignKey(c => c.userId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Document>()
                .HasOne(d => d.Claim)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.claimId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Claim>()
                .HasOne(c => c.Status)
                .WithMany(s => s.Claims)
                .HasForeignKey(c => c.statusId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed claim statuses
            modelBuilder.Entity<ClaimStatus>().HasData(
                new ClaimStatus { statusId = 1, statusName = "Submitted" },
                new ClaimStatus { statusId = 2, statusName = "Approved by Coordinator" },
                new ClaimStatus { statusId = 3, statusName = "Approved by Manager" },
                new ClaimStatus { statusId = 4, statusName = "Rejected by Coordinator" },
                new ClaimStatus { statusId = 5, statusName = "Rejected by Manager" },
                new ClaimStatus { statusId = 6, statusName = "Paid" }
            );

            // Create password hashes for seeded users
            var mattPassword = CreatePassword("password123");
            var victoriaPassword = CreatePassword("password123");
            var sarahPassword = CreatePassword("password123");
            var davidPassword = CreatePassword("password123");
            var hrPassword = CreatePassword("admin123");

            // Seed users with password hashes
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    userId = 1,
                    firstName = "Matt",
                    lastName = "Jones",
                    email = "mattjones@university.co.za",
                    phoneNumber = "+27 11 123 4567",
                    userRole = "Lecturer",
                    hourlyRate = 150.00m,
                    passwordHash = mattPassword.hash,
                    passwordSalt = mattPassword.salt
                },
                new User
                {
                    userId = 2,
                    firstName = "Victoria",
                    lastName = "Crown",
                    email = "crownvic@university.co.za",
                    phoneNumber = "+27 11 123 4568",
                    userRole = "Lecturer",
                    hourlyRate = 175.00m,
                    passwordHash = victoriaPassword.hash,
                    passwordSalt = victoriaPassword.salt
                },
                new User
                {
                    userId = 3,
                    firstName = "Sarah",
                    lastName = "Wilson",
                    email = "sarahw@university.co.za",
                    phoneNumber = "+27 11 123 4569",
                    userRole = "Coordinator",
                    hourlyRate = 200.00m,
                    passwordHash = sarahPassword.hash,
                    passwordSalt = sarahPassword.salt
                },
                new User
                {
                    userId = 4,
                    firstName = "David",
                    lastName = "Brown",
                    email = "davidb@university.co.za",
                    phoneNumber = "+27 11 123 4570",
                    userRole = "Manager",
                    hourlyRate = 250.00m,
                    passwordHash = davidPassword.hash,
                    passwordSalt = davidPassword.salt
                },
                new User
                {
                    userId = 5,
                    firstName = "HR",
                    lastName = "Administrator",
                    email = "hr@university.co.za",
                    phoneNumber = "+27 11 123 4571",
                    userRole = "HR",
                    hourlyRate = 0.00m,
                    passwordHash = hrPassword.hash,
                    passwordSalt = hrPassword.salt
                }
            );

            // Seed sample claims
            var baseDate = new DateTime(2024, 11, 1, 12, 0, 0);

            modelBuilder.Entity<Claim>().HasData(
                new Claim
                {
                    claimId = 1,
                    userId = 1,
                    hoursWorked = 20,
                    hourlyRate = 150,
                    totalAmount = 3000,
                    statusId = 1,
                    submissionDate = baseDate.AddDays(-2),
                    Notes = "Regular teaching hours"
                },
                new Claim
                {
                    claimId = 2,
                    userId = 1,
                    hoursWorked = 15,
                    hourlyRate = 150,
                    totalAmount = 2250,
                    statusId = 2,
                    submissionDate = baseDate.AddDays(-5),
                    Notes = "Extra marking hours"
                },
                new Claim
                {
                    claimId = 3,
                    userId = 2,
                    hoursWorked = 18,
                    hourlyRate = 175,
                    totalAmount = 3150,
                    statusId = 3,
                    submissionDate = baseDate.AddDays(-1),
                    Notes = "Research supervision"
                },
                new Claim
                {
                    claimId = 4,
                    userId = 2,
                    hoursWorked = 12,
                    hourlyRate = 175,
                    totalAmount = 2100,
                    statusId = 4,
                    submissionDate = baseDate.AddDays(-3),
                    Notes = "Exam preparation"
                }
            );
        }

        // Helper method to create password hashes
        private (string hash, string salt) CreatePassword(string password)
        {
            byte[] saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            string salt = Convert.ToBase64String(saltBytes);

            using (var sha256 = SHA256.Create())
            {
                byte[] passwordBytes = Encoding.UTF8.GetBytes(password + salt);
                byte[] hashBytes = sha256.ComputeHash(passwordBytes);
                return (Convert.ToBase64String(hashBytes), salt);
            }
        }
    }
}
