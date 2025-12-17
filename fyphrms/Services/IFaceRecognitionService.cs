using System.Threading.Tasks;

namespace fyphrms.Services
{
    
    public interface IFaceRecognitionService
    {
        
        Task<(bool IsVerified, string Message)> VerifyFaceAsync(
            int employeeId,
            string imageDataBase64,
            string referenceImageUrl);
    }
}