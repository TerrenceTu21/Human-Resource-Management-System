using fyphrms.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace fyphrms.Services
{
    
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);

        
        Task SendLeaveStatusEmailAsync(Leave leave, string toEmail);

        Task SendClaimStatusEmailAsync(EClaim claim, string toEmail);
    }
}