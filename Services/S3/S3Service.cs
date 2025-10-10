using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;

namespace RadiatorStockAPI.Services.S3
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
                ContentType = file.ContentType,
                CannedACL = S3CannedACL.PublicRead  // ✅ ADDED - Makes image publicly accessible
            };

            await _s3Client.PutObjectAsync(request);

            // ✅ CHANGED - Return direct public URL (never expires)
            var region = _configuration["AWS:S3:Region"] ?? "us-east-2";
            return $"https://{_bucketName}.s3.{region}.amazonaws.com/{key}";
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
