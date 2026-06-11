using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Tutor
{
    public sealed class FavoriteTutorResponse
    {
        public int FavoriteTutorId { get; set; }
        public int TutorId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Bio { get; set; }
        public double Rating { get; set; }
        public bool IsVerified { get; set; }
        public string? AvatarUrl { get; set; }
        public string? TutorAvatarUrl => AvatarUrl;
    }

    public sealed class CreateTutorReviewRequest
    {
        [Required]
        public int BookingId { get; set; }

        [Required]
        public int TutorId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(2000)]
        public string? Comment { get; set; }
    }

    public sealed class TutorReviewResponse
    {
        public int ReviewId { get; set; }
        public int BookingId { get; set; }
        public int TutorId { get; set; }
        public int UserId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string ReviewerName { get; set; } = string.Empty;
    }
}
