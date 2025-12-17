using fyphrms.Models;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Threading.Tasks;

namespace fyphrms.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _senderEmail;
        private readonly string _senderPassword;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            
            _smtpServer = _configuration["EmailSettings:SmtpServer"];
            _smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587"); 
            _senderEmail = _configuration["EmailSettings:SenderEmail"];
            _senderPassword = _configuration["EmailSettings:SenderPassword"];
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            try
            {
                using var client = new SmtpClient(_smtpServer, _smtpPort)
                {
                    Credentials = new System.Net.NetworkCredential(_senderEmail, _senderPassword),
                    EnableSsl = true
                };

                using var mailMessage = new MailMessage(_senderEmail, toEmail, subject, message)
                {
                    IsBodyHtml = true 
                };

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent successfully to {ToEmail}.", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail} using SMTP.", toEmail);
                
            }
        }

        
        public async Task SendLeaveStatusEmailAsync(Leave leave, string toEmail)
        {
            try
            {
                
                string statusColor;
                string statusDisplay;
                string commentTitle;

                switch (leave.Status)
                {
                    case "Approved":
                        statusColor = "#4CAF50"; // Green
                        statusDisplay = "APPROVED";
                        commentTitle = "Approval Comment";
                        break;
                    case "Rejected":
                        statusColor = "#F44336"; // Red
                        statusDisplay = "REJECTED";
                        commentTitle = "Rejection Reason";
                        break;
                    default:
                        statusColor = "#FFC107"; // Orange/Amber
                        statusDisplay = leave.Status.ToUpper();
                        commentTitle = "HR Comment";
                        break;
                }

                
                string employeeName = leave.Employee?.FirstName ?? "Employee";

                
                int totalDays = (leave.EndDate - leave.StartDate).Days + 1;

                
                string commentContent = leave.RejectReason ?? (leave.Status == "Approved" ? "Your leave has been approved by HR." : "No specific comment provided.");

                string commentSection = $@"
                <tr>
                    <td colspan='2' style='padding: 10px 20px; border-top: 1px solid #eee;'>
                        <p style='margin: 0; font-weight: bold; color: #333;'>{commentTitle}:</p>
                        <p style='margin: 5px 0 0 0; color: #555;'>{commentContent}</p>
                    </td>
                </tr>";

                
                string body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; background-color: #f7f7f7; padding: 20px; }}
                        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }}
                        .header {{ background-color: {statusColor}; color: #ffffff; padding: 20px; text-align: center; }}
                        .header h2 {{ margin: 0; font-size: 24px; }}
                        .content {{ padding: 20px 30px; color: #333333; }}
                        .details-table {{ width: 100%; border-collapse: collapse; margin-top: 15px; }}
                        .details-table th, .details-table td {{ padding: 10px; border-bottom: 1px solid #eee; text-align: left; }}
                        .details-table th {{ background-color: #f9f9f9; font-weight: 600; width: 30%; }}
                        .footer {{ padding: 20px 30px; text-align: center; border-top: 1px solid #eee; font-size: 12px; color: #999; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Leave Request Status: {statusDisplay}</h2>
                        </div>
                        <div class='content'>
                            <p>Dear <strong>{employeeName}</strong>,</p>
                            
                            <p>We are writing to inform you that your leave request has been processed by the HR department. Below are the details and the final status:</p>

                            <table class='details-table'>
                                <tr><th>Status</th><td><strong style='color: {statusColor};'>{statusDisplay}</strong></td></tr>
                                <tr><th>Leave ID</th><td>{leave.LeaveID}</td></tr>
                                <tr><th>Leave Type</th><td>{leave.LeaveType.TypeName}</td></tr>
                                <tr><th>Start Date</th><td>{leave.StartDate:dd MMM yyyy}</td></tr>
                                <tr><th>End Date</th><td>{leave.EndDate:dd MMM yyyy}</td></tr>
                                <tr><th>Duration</th><td>{totalDays} day(s)</td></tr>
                                <tr><th>Reason Submitted</th><td>{leave.Reason}</td></tr>
                            </table>

                            <div style='background-color: #f9f9f9; border-left: 5px solid {statusColor}; padding: 15px; margin-top: 20px;'>
                                <p style='margin: 0 0 5px 0; font-weight: bold; color: #333;'>{commentTitle}:</p>
                                <p style='margin: 0; color: #555;'>{commentContent}</p>
                            </div>
                            
                            <p style='margin-top: 30px;'>If you have any questions regarding this update, please contact the HR department immediately.</p>
                        </div>
                        <div class='footer'>
                            This is an automated notification. Please do not reply directly to this email.
                        </div>
                    </div>
                </body>
                </html>";

                
                using (var message = new MailMessage(_senderEmail, toEmail))
                {
                    message.Subject = $"Leave Status Update: {leave.LeaveType} - {statusDisplay}";
                    message.Body = body;
                    message.IsBodyHtml = true;

                    using (var client = new SmtpClient(_smtpServer, _smtpPort))
                    {
                        client.EnableSsl = true;
                        client.Credentials = new NetworkCredential(_senderEmail, _senderPassword);
                        await client.SendMailAsync(message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send leave status email to {ToEmail} for Leave ID {LeaveId}.", toEmail, leave.LeaveID);
                
            }
        }

        public async Task SendClaimStatusEmailAsync(EClaim claim, string toEmail)
        {
            try
            {
                
                string employeeFullName = claim.Employee?.FirstName;
                string employeeFriendlyName = employeeFullName?.Split(' ')[0] ?? "Employee";

                
                string statusColor;
                string statusDisplay;
                string commentTitle;

                switch (claim.Status)
                {
                    case "Approved":
                        statusColor = "#4CAF50"; // Green
                        statusDisplay = "APPROVED";
                        commentTitle = "Approval Comment";
                        break;
                    case "Rejected":
                        statusColor = "#F44336"; // Red
                        statusDisplay = "REJECTED";
                        commentTitle = "Rejection Reason";
                        break;
                    default:
                        statusColor = "#FFC107"; // Orange/Amber
                        statusDisplay = claim.Status.ToUpper();
                        commentTitle = "HR Comment";
                        break;
                }

                
                string commentContent = claim.RejectReason ?? (claim.Status == "Approved" ? "Your claim has been processed and approved." : "No specific comment provided.");

                
                string body = $@"
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; background-color: #f7f7f7; padding: 20px; }}
                        .container {{ max-width: 600px; margin: 0 auto; background-color: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }}
                        .header {{ background-color: {statusColor}; color: #ffffff; padding: 20px; text-align: center; }}
                        .header h2 {{ margin: 0; font-size: 24px; }}
                        .content {{ padding: 20px 30px; color: #333333; }}
                        .details-table {{ width: 100%; border-collapse: collapse; margin-top: 15px; }}
                        .details-table th, .details-table td {{ padding: 10px; border-bottom: 1px solid #eee; text-align: left; }}
                        .details-table th {{ background-color: #f9f9f9; font-weight: 600; width: 30%; }}
                        .footer {{ padding: 20px 30px; text-align: center; border-top: 1px solid #eee; font-size: 12px; color: #999; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h2>Claim Status: {statusDisplay}</h2>
                        </div>
                        <div class='content'>
                            <p>Dear <strong>{employeeFriendlyName}</strong>,</p>
                            
                            <p>We are writing to inform you that your claim request has been processed by the HR department. Below are the details and the final status:</p>

                            <table class='details-table'>
                                <tr><th>Status</th><td><strong style='color: {statusColor};'>{statusDisplay}</strong></td></tr>
                                <tr><th>Employee Name</th><td>{employeeFullName}</td></tr>
                                <tr><th>Claim ID</th><td>{claim.ClaimID}</td></tr>
                                <tr><th>Date Submitted</th><td>{claim.ClaimDate:dd MMM yyyy}</td></tr>
                                <tr><th>Amount</th><td>{claim.Amount:C}</td></tr>
                                <tr><th>Description</th><td>{claim.Description}</td></tr>
                                <tr><th>Date of Expense</th><td>{claim.ExpensesDate:dd MMM yyyy}</td></tr>
                            </table>

                            <div style='background-color: #f9f9f9; border-left: 5px solid {statusColor}; padding: 15px; margin-top: 20px;'>
                                <p style='margin: 0 0 5px 0; font-weight: bold; color: #333;'>{commentTitle}:</p>
                                <p style='margin: 0; color: #555;'>{commentContent}</p>
                            </div>
                            
                            <p style='margin-top: 30px;'>If you have any questions regarding this update, please contact the HR department immediately.</p>
                        </div>
                        <div class='footer'>
                            This is an automated notification. Please do not reply directly to this email.
                        </div>
                    </div>
                </body>
                </html>";

                
                using (var message = new MailMessage(_senderEmail, toEmail))
                {
                    message.Subject = $"Claim Status Update: {claim.ClaimID} - {statusDisplay}";
                    message.Body = body;
                    message.IsBodyHtml = true;

                    using (var client = new SmtpClient(_smtpServer, _smtpPort))
                    {
                        client.EnableSsl = true;
                        client.Credentials = new NetworkCredential(_senderEmail, _senderPassword);
                        await client.SendMailAsync(message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send claim status email to {ToEmail} for Claim ID {ClaimId}.", toEmail, claim.ClaimID);
                
            }
        }
    }
}
