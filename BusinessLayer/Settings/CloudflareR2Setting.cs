namespace BusinessLayer.Settings
{
    public sealed class CloudflareR2Setting
    {
        public const string SectionName = "CloudflareR2";

        public string AccountId { get; set; } = string.Empty;
        public string AccessKeyId { get; set; } = string.Empty;
        public string SecretAccessKey { get; set; } = string.Empty;
        public string BucketName { get; set; } = string.Empty;
        public string? PublicBaseUrl { get; set; }
        public int PresignedUrlExpirationMinutes { get; set; } = 15;
    }
}
