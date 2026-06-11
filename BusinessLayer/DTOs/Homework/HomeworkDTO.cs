using System.ComponentModel.DataAnnotations;

namespace BusinessLayer.DTOs.Homework
{
    public sealed class CreateHomeworkRequest
    {
        [Required, MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        public string Type { get; set; } = string.Empty;

        public DateTime DueDate { get; set; }

        public List<HomeworkQuestionRequest> Questions { get; set; } = new();
        public List<HomeworkEssayRequest> Essays { get; set; } = new();
    }

    public sealed class UpdateHomeworkRequest
    {
        [Required, MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Required]
        public string Type { get; set; } = string.Empty;

        public DateTime DueDate { get; set; }

        public List<HomeworkQuestionRequest> Questions { get; set; } = new();
        public List<HomeworkEssayRequest> Essays { get; set; } = new();
    }

    public sealed class HomeworkQuestionRequest
    {
        [Required, MaxLength(1000)]
        public string QuestionText { get; set; } = string.Empty;

        [Range(0.1, 1000)]
        public double Point { get; set; } = 1;

        public List<HomeworkQuestionOptionRequest> Options { get; set; } = new();
    }

    public sealed class HomeworkQuestionOptionRequest
    {
        [Required, MaxLength(500)]
        public string Content { get; set; } = string.Empty;

        public bool IsCorrect { get; set; }
    }

    public sealed class HomeworkEssayRequest
    {
        [Required, MaxLength(1000)]
        public string QuestionText { get; set; } = string.Empty;

        [Range(0.1, 1000)]
        public double Points { get; set; } = 10;
    }

    public sealed class SubmitHomeworkRequest
    {
        public List<SubmitMultipleChoiceAnswerRequest> MultipleChoiceAnswers { get; set; } = new();
        public List<SubmitEssayAnswerRequest> EssayAnswers { get; set; } = new();
    }

    public sealed class SubmitMultipleChoiceAnswerRequest
    {
        public int MultipleChoiceQuestionId { get; set; }
        public int QuestionOptionId { get; set; }
    }

    public sealed class SubmitEssayAnswerRequest
    {
        public int EssayId { get; set; }

        [Required, MaxLength(5000)]
        public string AnswerText { get; set; } = string.Empty;
    }

    public sealed class GradeEssaySubmissionRequest
    {
        public List<GradeEssayAnswerRequest> EssayGrades { get; set; } = new();

        [MaxLength(2000)]
        public string? Feedback { get; set; }
    }

    public sealed class GradeEssayAnswerRequest
    {
        public int EssayAnswerId { get; set; }
        public double Score { get; set; }

        [MaxLength(2000)]
        public string? Feedback { get; set; }
    }

    public sealed class HomeworkResponse
    {
        public int HomeworkId { get; set; }
        public int BookingId { get; set; }
        public int? LessonId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime UploadedAt { get; set; }
        public double TotalPoints { get; set; }
        public List<HomeworkQuestionResponse> Questions { get; set; } = new();
        public List<HomeworkEssayResponse> Essays { get; set; } = new();
        public HomeworkSubmissionResponse? MySubmission { get; set; }
        public List<HomeworkSubmissionResponse> Submissions { get; set; } = new();
    }

    public sealed class HomeworkQuestionResponse
    {
        public int MultipleChoiceQuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public double Point { get; set; }
        public List<HomeworkQuestionOptionResponse> Options { get; set; } = new();
    }

    public sealed class HomeworkQuestionOptionResponse
    {
        public int QuestionOptionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool? IsCorrect { get; set; }
    }

    public sealed class HomeworkEssayResponse
    {
        public int EssayId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public double Points { get; set; }
    }

    public sealed class HomeworkSubmissionResponse
    {
        public int SubmissionId { get; set; }
        public int HomeworkId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public DateTime? GradedAt { get; set; }
        public double TotalScore { get; set; }
        public double MaxScore { get; set; }
        public bool IsGraded { get; set; }
        public string? Feedback { get; set; }
        public List<MultipleChoiceAnswerResponse> MultipleChoiceAnswers { get; set; } = new();
        public List<EssayAnswerResponse> EssayAnswers { get; set; } = new();
    }

    public sealed class MultipleChoiceAnswerResponse
    {
        public int MultipleChoiceQuestionAnswerId { get; set; }
        public int MultipleChoiceQuestionId { get; set; }
        public int QuestionOptionId { get; set; }
        public string SelectedOption { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public double Score { get; set; }
    }

    public sealed class EssayAnswerResponse
    {
        public int EssayAnswerId { get; set; }
        public int EssayId { get; set; }
        public string AnswerText { get; set; } = string.Empty;
        public double Score { get; set; }
        public string? Feedback { get; set; }
    }
}
