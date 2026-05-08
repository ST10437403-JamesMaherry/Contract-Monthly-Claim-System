using Contract_Monthly_Claim_System.Models;
using System.Globalization;
using System.Text;

namespace Contract_Monthly_Claim_System.Services
{
    public interface IReportExportService
    {
        byte[] GeneratePaymentReportCsv(List<Claim> claims, List<User> users);
        byte[] GenerateUserReportCsv(List<User> users, string? filterRole = null);
        byte[] GenerateMonthlyPaymentBatchCsv(List<Claim> claims, List<User> users, int year, int month);
        List<Claim> GetMonthlyPaymentBatchClaims(List<Claim> claims, int year, int month);
    }

    public class ReportExportService : IReportExportService
    {
        private static readonly int[] PayableStatusIds =
        {
            (int)ClaimStatusType.ApprovedByManager,
            (int)ClaimStatusType.Paid
        };

        public byte[] GeneratePaymentReportCsv(List<Claim> claims, List<User> users)
        {
            var payableClaims = claims
                .Where(IsPayable)
                .OrderBy(c => c.submissionDate)
                .ThenBy(c => c.claimId)
                .ToList();

            var rows = new List<string[]>
            {
                new[]
                {
                    "Claim ID",
                    "Lecturer",
                    "Email",
                    "Submission Date",
                    "Hours",
                    "Rate",
                    "Total",
                    "Status"
                }
            };

            foreach (var claim in payableClaims)
            {
                var user = ResolveUser(claim, users);

                rows.Add(new[]
                {
                    claim.claimId.ToString(CultureInfo.InvariantCulture),
                    FormatUserName(user),
                    user?.email ?? string.Empty,
                    claim.submissionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    claim.hoursWorked.ToString("F2", CultureInfo.InvariantCulture),
                    claim.hourlyRate.ToString("F2", CultureInfo.InvariantCulture),
                    claim.totalAmount.ToString("F2", CultureInfo.InvariantCulture),
                    GetStatusName(claim)
                });
            }

            rows.Add(new[]
            {
                "TOTAL",
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                payableClaims.Sum(c => c.totalAmount).ToString("F2", CultureInfo.InvariantCulture),
                string.Empty
            });

            return ToCsvBytes(rows);
        }

        public byte[] GenerateUserReportCsv(List<User> users, string? filterRole = null)
        {
            var filteredUsers = string.IsNullOrWhiteSpace(filterRole)
                ? users
                : users.Where(u => u.userRole == filterRole).ToList();

            var rows = new List<string[]>
            {
                new[]
                {
                    "User ID",
                    "First Name",
                    "Last Name",
                    "Email",
                    "Phone",
                    "Role",
                    "Hourly Rate",
                    "Must Change Password"
                }
            };

            foreach (var user in filteredUsers.OrderBy(u => u.userRole).ThenBy(u => u.lastName).ThenBy(u => u.firstName))
            {
                rows.Add(new[]
                {
                    user.userId.ToString(CultureInfo.InvariantCulture),
                    user.firstName,
                    user.lastName,
                    user.email,
                    user.phoneNumber,
                    user.userRole,
                    user.hourlyRate.ToString("F2", CultureInfo.InvariantCulture),
                    user.mustChangePassword ? "Yes" : "No"
                });
            }

            return ToCsvBytes(rows);
        }

        public byte[] GenerateMonthlyPaymentBatchCsv(List<Claim> claims, List<User> users, int year, int month)
        {
            var batchClaims = GetMonthlyPaymentBatchClaims(claims, year, month);
            var batchId = $"CMCS-{year:D4}{month:D2}";

            var rows = new List<string[]>
            {
                new[]
                {
                    "Batch ID",
                    "Payment Month",
                    "Claim ID",
                    "Lecturer ID",
                    "Lecturer Name",
                    "Email",
                    "Submission Date",
                    "Hours",
                    "Rate",
                    "Amount",
                    "Status"
                }
            };

            foreach (var claim in batchClaims)
            {
                var user = ResolveUser(claim, users);

                rows.Add(new[]
                {
                    batchId,
                    $"{year:D4}-{month:D2}",
                    claim.claimId.ToString(CultureInfo.InvariantCulture),
                    claim.userId.ToString(CultureInfo.InvariantCulture),
                    FormatUserName(user),
                    user?.email ?? string.Empty,
                    claim.submissionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    claim.hoursWorked.ToString("F2", CultureInfo.InvariantCulture),
                    claim.hourlyRate.ToString("F2", CultureInfo.InvariantCulture),
                    claim.totalAmount.ToString("F2", CultureInfo.InvariantCulture),
                    GetStatusName(claim)
                });
            }

            return ToCsvBytes(rows);
        }

        public List<Claim> GetMonthlyPaymentBatchClaims(List<Claim> claims, int year, int month)
        {
            return claims
                .Where(IsPayable)
                .Where(c => c.submissionDate.Year == year && c.submissionDate.Month == month)
                .OrderBy(c => c.User?.lastName)
                .ThenBy(c => c.User?.firstName)
                .ThenBy(c => c.claimId)
                .ToList();
        }

        private static bool IsPayable(Claim claim)
        {
            return PayableStatusIds.Contains(claim.statusId);
        }

        private static User? ResolveUser(Claim claim, List<User> users)
        {
            return claim.User ?? users.FirstOrDefault(u => u.userId == claim.userId);
        }

        private static string FormatUserName(User? user)
        {
            return user == null
                ? "Unknown lecturer"
                : $"{user.firstName} {user.lastName}".Trim();
        }

        private static string GetStatusName(Claim claim)
        {
            if (!string.IsNullOrWhiteSpace(claim.Status?.statusName))
                return claim.Status.statusName;

            return claim.statusId switch
            {
                (int)ClaimStatusType.Submitted => "Submitted",
                (int)ClaimStatusType.ApprovedByCoordinator => "Approved by Coordinator",
                (int)ClaimStatusType.ApprovedByManager => "Approved by Manager",
                (int)ClaimStatusType.RejectedByCoordinator => "Rejected by Coordinator",
                (int)ClaimStatusType.RejectedByManager => "Rejected by Manager",
                (int)ClaimStatusType.Paid => "Paid",
                _ => "Unknown"
            };
        }

        private static byte[] ToCsvBytes(List<string[]> rows)
        {
            var builder = new StringBuilder();

            foreach (var row in rows)
            {
                builder.AppendLine(string.Join(",", row.Select(EscapeCsvValue)));
            }

            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
                .GetBytes(builder.ToString());
        }

        private static string EscapeCsvValue(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\r') || value.Contains('\n');

            if (!needsQuotes)
                return value;

            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
