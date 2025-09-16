using Amazon.S3;
using Amazon.S3.Model;

namespace RadiatorStockAPI.Services
{
    public class S3Service : IS3Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly IConfiguration _configuration;
        private readonly string _bucketName;

        public S3Service(IAmazonS3 s3Client, IConfiguration configuration)
        {
            _s3Client = s3Client;
            _configuration = configuration;
            _bucketName = _configuration["AWS:S3:BucketName"] ??
                throw new InvalidOperationException("S3 bucket name not configured");
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            var key = $"radiators/{Guid.NewGuid()}_{file.FileName}";

            using var stream = file.OpenReadStream();

            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = stream,
                ContentType = file.ContentType
            };

            await _s3Client.PutObjectAsync(request);

            // Generate a pre-signed URL with 7 days expiry (maximum allowed)
            var urlRequest = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddDays(7), // Changed from 1 year to 7 days
                Verb = HttpVerb.GET
            };

            return await _s3Client.GetPreSignedURLAsync(urlRequest);
        }
        public async Task<bool> DeleteImageAsync(string key)
        {
            try
            {
                await _s3Client.DeleteObjectAsync(_bucketName, key);
                return true;
            }
            catch
            {
                return false;
            }
        }




    }
}