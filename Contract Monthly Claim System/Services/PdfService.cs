using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Contract_Monthly_Claim_System.Models;

// Alias to avoid conflict with Contract_Monthly_Claim_System.Models.Document
using QuestPDFDocument = QuestPDF.Fluent.Document;

namespace Contract_Monthly_Claim_System.Services
{
    public interface IPdfService
    {
        byte[] GeneratePaymentReport(List<Claim> claims, List<User> users);
        byte[] GenerateInvoice(Claim claim, User user);
        byte[] GenerateUserReport(List<User> users, string filterRole = null);
    }

    public class PdfService : IPdfService
    {
        public byte[] GeneratePaymentReport(List<Claim> claims, List<User> users)
        {
            var document = QuestPDFDocument.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header()
                        .AlignCenter()
                        .Text("Payment Processing Report")
                        .SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Spacing(20);

                            // Summary Section
                            x.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(summary =>
                            {
                                summary.Item().Text($"Generated on: {DateTime.Now:dd MMMM yyyy}").SemiBold();
                                summary.Item().Text($"Total Claims: {claims.Count}").SemiBold();
                                summary.Item().Text($"Total Amount: R {claims.Sum(c => c.totalAmount):F2}").SemiBold();
                            });

                            // Claims Table
                            x.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1); // Claim ID
                                    columns.RelativeColumn(3); // Lecturer
                                    columns.RelativeColumn(2); // Date
                                    columns.RelativeColumn(1); // Hours
                                    columns.RelativeColumn(2); // Rate
                                    columns.RelativeColumn(2); // Total
                                    columns.RelativeColumn(2); // Status
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Claim ID").FontColor(Colors.White);
                                    header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Lecturer").FontColor(Colors.White);
                                    header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Date").FontColor(Colors.White);
                                    header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Hours").FontColor(Colors.White);
                                    header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Rate (R)").FontColor(Colors.White);
                                    header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Total (R)").FontColor(Colors.White);
                                    header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Status").FontColor(Colors.White);
                                });

                                foreach (var claim in claims.OrderBy(c => c.claimId))
                                {
                                    var user = users.FirstOrDefault(u => u.userId == claim.userId);
                                    var status = claim.statusId == 6 ? "Paid" : "Ready for Payment";

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"#{claim.claimId}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{user?.firstName} {user?.lastName}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(claim.submissionDate.ToString("dd/MM/yyyy"));
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(claim.hoursWorked.ToString("F1"));
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(claim.hourlyRate.ToString("F2"));
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(claim.totalAmount.ToString("F2")).SemiBold();
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(status);
                                }
                            });

                            // Total Row
                            x.Item().AlignRight().Text($"Grand Total: R {claims.Sum(c => c.totalAmount):F2}").SemiBold().FontSize(12);
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] GenerateInvoice(Claim claim, User user)
        {
            var document = QuestPDFDocument.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);

                    page.Header()
                        .Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("INVOICE").Bold().FontSize(20);
                                column.Item().Text($"Invoice #: {claim.claimId}");
                                column.Item().Text($"Date: {DateTime.Now:dd MMMM yyyy}");
                            });

                            row.ConstantItem(150).Height(50).Placeholder();
                        });

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Spacing(15);

                            // Bill To Section
                            column.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(billTo =>
                            {
                                billTo.Item().Text("BILL TO:").Bold();
                                billTo.Item().Text($"{user.firstName} {user.lastName}");
                                billTo.Item().Text(user.email);
                                billTo.Item().Text($"Employee ID: {user.userId}");
                            });

                            // Services Rendered
                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(4); // Description
                                    columns.RelativeColumn(2); // Hours
                                    columns.RelativeColumn(2); // Rate
                                    columns.RelativeColumn(2); // Amount
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Text("Description").Bold();
                                    header.Cell().Text("Hours").Bold();
                                    header.Cell().Text("Rate (R)").Bold();
                                    header.Cell().Text("Amount (R)").Bold();
                                });

                                table.Cell().Text("Contract lecturing services");
                                table.Cell().Text(claim.hoursWorked.ToString("F1"));
                                table.Cell().Text(claim.hourlyRate.ToString("F2"));
                                table.Cell().Text(claim.totalAmount.ToString("F2")).Bold();
                            });

                            // Total
                            column.Item().AlignRight().Text($"TOTAL DUE: R {claim.totalAmount:F2}").Bold().FontSize(14);

                            // Notes
                            if (!string.IsNullOrEmpty(claim.Notes))
                            {
                                column.Item().PaddingTop(10).Text($"Notes: {claim.Notes}").Italic();
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text("Thank you for your service - Contract Monthly Claim System");
                });
            });



            return document.GeneratePdf();
        }

        public byte[] GenerateUserReport(List<User> users, string filterRole = null)
        {
            // Filter users by role if specified
            var filteredUsers = string.IsNullOrEmpty(filterRole)
                ? users
                : users.Where(u => u.userRole == filterRole).ToList();

            var document = QuestPDFDocument.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header()
                        .AlignCenter()
                        .Text($"User Directory Report{(string.IsNullOrEmpty(filterRole) ? "" : $" - {filterRole}s")}")
                        .SemiBold().FontSize(16).FontColor(Colors.Blue.Medium);

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(x =>
                        {
                            x.Spacing(20);

                            // Summary Section
                            x.Item().Background(Colors.Grey.Lighten3).Padding(10).Row(row =>
                            {
                                row.RelativeItem().Text($"Generated on: {DateTime.Now:dd MMMM yyyy}").SemiBold();
                                row.RelativeItem().AlignCenter().Text($"Total Users: {filteredUsers.Count}").SemiBold();
                                if (!string.IsNullOrEmpty(filterRole))
                                {
                                    row.RelativeItem().AlignRight().Text($"Role: {filterRole}").SemiBold();
                                }
                            });

                            // Users Table
                            x.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(1);  // User ID
                                    columns.RelativeColumn(2);  // First Name
                                    columns.RelativeColumn(2);  // Last Name
                                    columns.RelativeColumn(3);  // Email
                                    columns.RelativeColumn(2);  // Phone
                                    columns.RelativeColumn(2);  // Role
                                    columns.RelativeColumn(1.5f); // Hourly Rate
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("User ID").FontColor(Colors.White);
                                    header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("First Name").FontColor(Colors.White);
                                    header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Last Name").FontColor(Colors.White);
                                    header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Email").FontColor(Colors.White);
                                    header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Phone").FontColor(Colors.White);
                                    header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Role").FontColor(Colors.White);
                                    header.Cell().Background(Colors.Grey.Darken3).Padding(5).Text("Rate (R)").FontColor(Colors.White);
                                });

                                foreach (var user in filteredUsers.OrderBy(u => u.userRole).ThenBy(u => u.lastName))
                                {
                                    var roleColor = user.userRole switch
                                    {
                                        "HR" => Colors.Red.Medium,
                                        "Manager" => Colors.Blue.Medium,
                                        "Coordinator" => Colors.Green.Medium,
                                        "Lecturer" => Colors.Orange.Medium,
                                        _ => Colors.Grey.Medium
                                    };

                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{user.userId}");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(user.firstName);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(user.lastName);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(user.email).FontSize(9);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(user.phoneNumber ?? "N/A");
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(user.userRole).FontColor(roleColor).SemiBold();
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(user.hourlyRate > 0 ? user.hourlyRate.ToString("F2") : "N/A");
                                }
                            });

                            // Statistics Section
                            x.Item().Background(Colors.Blue.Lighten5).Padding(10).Column(stats =>
                            {
                                stats.Item().Text("User Statistics").SemiBold().FontSize(12);
                                stats.Item().PaddingTop(5).Row(row =>
                                {
                                    var roleGroups = filteredUsers.GroupBy(u => u.userRole)
                                        .Select(g => new { Role = g.Key, Count = g.Count() })
                                        .OrderByDescending(g => g.Count);

                                    foreach (var group in roleGroups)
                                    {
                                        row.RelativeItem().Text($"{group.Role}: {group.Count}");
                                    }
                                });
                            });
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                            x.Span(" • Contract Monthly Claim System • Confidential");
                        });
                });
            });

            return document.GeneratePdf();
        }
    }
}
