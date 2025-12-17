using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FaceAiSharp; 
using FaceAiSharp.Extensions; 
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Net.Http;

namespace fyphrms.Services
{
    
    public class FaceRecognitionService : IFaceRecognitionService
    {
        private readonly ILogger<FaceRecognitionService> _logger;
        private readonly HttpClient _httpClient;

        
        private readonly IFaceDetectorWithLandmarks _detector;
        private readonly IFaceEmbeddingsGenerator _recognizer;

        private const float SimilarityThreshold = 0.50f;

        public FaceRecognitionService(
            ILogger<FaceRecognitionService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();

            try
            {
                 
                _detector = FaceAiSharpBundleFactory.CreateFaceDetectorWithLandmarks();
                _recognizer = FaceAiSharpBundleFactory.CreateFaceEmbeddingsGenerator();
                _logger.LogInformation("FaceAiSharp models initialized successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize FaceAiSharp models. Ensure FaceAiSharp.Bundle and Microsoft.ML.OnnxRuntime are installed.");
                
                throw;
            }
        }

        
        public async Task<(bool IsVerified, string Message)> VerifyFaceAsync(
            int employeeId,
            string imageDataBase64,
            string referenceImageUrl)
        {
            if (string.IsNullOrEmpty(referenceImageUrl))
            {
                return (false, "Error: Employee profile photo URL is missing.");
            }

            
            using var probeImage = LoadImageFromBase64(imageDataBase64);
            if (probeImage == null) return (false, "Could not decode live webcam image.");

            
            using var referenceImage = await DownloadAndLoadImageAsync(referenceImageUrl);
            if (referenceImage == null) return (false, "Could not download reference profile photo.");

            try
            {
                
                var probeFaces = _detector.DetectFaces(probeImage);
                var liveFace = probeFaces.FirstOrDefault();

                if (liveFace == null)
                {
                    return (false, "Verification failed: No face detected in the live image.");
                }

                
                _recognizer.AlignFaceUsingLandmarks(probeImage, liveFace.Landmarks!);
                var probeEmbedding = _recognizer.GenerateEmbedding(probeImage);


                
                var referenceFaces = _detector.DetectFaces(referenceImage);
                var profileFace = referenceFaces.FirstOrDefault();

                if (profileFace == null)
                {
                    return (false, "Verification failed: No face detected in the reference image.");
                }

                
                _recognizer.AlignFaceUsingLandmarks(referenceImage, profileFace.Landmarks!);
                var referenceEmbedding = _recognizer.GenerateEmbedding(referenceImage);

                
                var dotProduct = probeEmbedding.Dot(referenceEmbedding);
                _logger.LogInformation($"Employee {employeeId} verification dot product: {dotProduct:F4}");

                
                if (dotProduct >= SimilarityThreshold)
                {
                    return (true, $"Verification Success! Confidence: {dotProduct:P2}.");
                }
                else
                {
                    return (false, $"Verification failed. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Face recognition processing error for Employee ID {employeeId}.");
                return (false, "System error during face analysis.");
            }
        }

        
        private Image<Rgb24>? LoadImageFromBase64(string base64Data)
        {
            try
            {
                
                var data = base64Data.Split(',').Last();
                var bytes = Convert.FromBase64String(data);
                return Image.Load<Rgb24>(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load image from Base64 string.");
                return null;
            }
        }

        
        private async Task<Image<Rgb24>?> DownloadAndLoadImageAsync(string url)
        {
            try
            {
                var imageBytes = await _httpClient.GetByteArrayAsync(url);
                return Image.Load<Rgb24>(imageBytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to download or load image from URL: {url}");
                return null;
            }
        }
    }
}