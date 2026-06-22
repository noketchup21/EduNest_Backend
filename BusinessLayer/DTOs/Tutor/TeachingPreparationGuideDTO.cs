using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Tutor
{
    public sealed class GenerateTeachingPreparationGuideRequest
    {
        [Range(1, int.MaxValue)]
        public int SubjectId { get; set; }

        [Required, MaxLength(100)]
        public string Technology { get; set; } = string.Empty;

        [Required, MaxLength(50)]
        public string StudentLevel { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? LessonFocus { get; set; }
    }

    public sealed class TeachingPreparationGuideResponse
    {
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string Technology { get; set; } = string.Empty;
        public string StudentLevel { get; set; } = string.Empty;
        public string LessonFocus { get; set; } = string.Empty;
        public string Objective { get; set; } = string.Empty;
        public List<TeachingPreparationGuideSection> Sections { get; set; } = new();
    }

    public sealed class TeachingPreparationGuideSection
    {
        public string Title { get; set; } = string.Empty;
        public List<string> Items { get; set; } = new();
    }
}
