namespace BusinessLayer.DTOs.User
{
    public class AuthResponseDTO
    {
        public UserResponseDTO User { get; set; }
        public string AccessToken { get; set; }
        public DateTime AccessTokenExpiry { get; set; }
        public string RefreshToken { get; set; }
    }

    public class RefreshTokenRequestDTO
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
