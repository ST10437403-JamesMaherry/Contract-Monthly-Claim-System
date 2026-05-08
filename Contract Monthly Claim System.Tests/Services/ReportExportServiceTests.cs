using Contract_Monthly_Claim_System.Models;
using Contract_Monthly_Claim_System.Services;
using System.Text;

namespace Contract_Monthly_Claim_System.Tests.Services
{
    public class ReportExportServiceTests
    {
        [Fact]
        public void GenerateMonthlyPaymentBatchCsv_IncludesOnlyPayableClaimsForSelectedMonth()
        {
            var service = new ReportExportService();
            var users = CreateUsers();
            var claims = new List<Claim>
            {
                CreateClaim(1, 1, ClaimStatusType.ApprovedByManager, new DateTime(2024, 10, 5), 1500),
                CreateClaim(2, 2, ClaimStatusType.Paid, new DateTime(2024, 10, 8), 2100),
                CreateClaim(3, 1, ClaimStatusType.Submitted, new DateTime(2024, 10, 9), 900),
                CreateClaim(4, 1, ClaimStatusType.ApprovedByManager, new DateTime(2024, 11, 1), 3000)
            };

            var csv = ReadCsv(service.GenerateMonthlyPaymentBatchCsv(claims, users, 2024, 10));

            Assert.Contains("CMCS-202410,2024-10,1,1,Matt Jones,mattjones@university.co.za", csv);
            Assert.Contains("CMCS-202410,2024-10,2,2,Victoria Crown,crownvic@university.co.za", csv);
            Assert.DoesNotContain("CMCS-202410,2024-10,3,", csv);
            Assert.DoesNotContain("CMCS-202410,2024-10,4,", csv);
        }

        [Fact]
        public void GenerateUserReportCsv_EscapesCsvValuesAndIncludesPasswordChangeFlag()
        {
            var service = new ReportExportService();
            var users = new List<User>
            {
                new User
                {
                    userId = 10,
                    firstName = "Sam",
                    lastName = "Jones, Jr.",
                    email = "sam@university.co.za",
                    phoneNumber = "+27 11 111 1111",
                    userRole = "Lecturer",
                    hourlyRate = 180,
                    mustChangePassword = true,
                    passwordHash = "hash",
                    passwordSalt = string.Empty
                }
            };

            var csv = ReadCsv(service.GenerateUserReportCsv(users));

            Assert.Contains("\"Jones, Jr.\"", csv);
            Assert.Contains(",Yes", csv);
        }

        private static List<User> CreateUsers()
        {
            return new List<User>
            {
                new User
                {
                    userId = 1,
                    firstName = "Matt",
                    lastName = "Jones",
                    email = "mattjones@university.co.za",
                    phoneNumber = "+27 11 123 4567",
                    userRole = "Lecturer",
                    hourlyRate = 150,
                    passwordHash = "hash",
                    passwordSalt = string.Empty
                },
                new User
                {
                    userId = 2,
                    firstName = "Victoria",
                    lastName = "Crown",
                    email = "crownvic@university.co.za",
                    phoneNumber = "+27 11 123 4568",
                    userRole = "Lecturer",
                    hourlyRate = 175,
                    passwordHash = "hash",
                    passwordSalt = string.Empty
                }
            };
        }

        private static Claim CreateClaim(int claimId, int userId, ClaimStatusType status, DateTime submittedOn, decimal amount)
        {
            return new Claim
            {
                claimId = claimId,
                userId = userId,
                hoursWorked = 10,
                hourlyRate = amount / 10,
                totalAmount = amount,
                statusId = (int)status,
                submissionDate = submittedOn,
                User = default!,
                Status = default!
            };
        }

        private static string ReadCsv(byte[] csvBytes)
        {
            return Encoding.UTF8.GetString(csvBytes).TrimStart('\uFEFF');
        }
    }
}
