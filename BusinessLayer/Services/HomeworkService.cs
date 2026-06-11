using BusinessLayer.DTOs.Homework;
using BusinessLayer.IServices;
using DataAccessLayer.Entities;
using Microsoft.EntityFrameworkCore;

namespace BusinessLayer.Services
{
    public sealed class HomeworkService : IHomeworkService
    {
        private readonly EduNestDbContext _db;

        public HomeworkService(EduNestDbContext db)
        {
            _db = db;
        }

        public async Task<List<HomeworkResponse>> GetByLessonAsync(int userId, int lessonId)
        {
            var lesson = await GetLessonForUserAsync(userId, lessonId);
            var isTutor = await IsTutorForLessonAsync(userId, lesson);

            var homeworks = await HomeworkQuery()
                .Where(h => h.LessonId == lesson.LessonId || h.BookingId == lesson.BookingId)
                .OrderByDescending(h => h.UploadedAt)
                .ToListAsync();

            return homeworks
                .Select(h => ToResponse(h, userId, isTutor))
                .ToList();
        }

        public async Task<HomeworkResponse> GetByIdAsync(int userId, int homeworkId)
        {
            var homework = await HomeworkQuery()
                .FirstOrDefaultAsync(h => h.HomeworkId == homeworkId)
                ?? throw new KeyNotFoundException("Homework not found.");

            await EnsureCanViewHomeworkAsync(userId, homework);
            var isTutor = await IsTutorForHomeworkAsync(userId, homework);

            return ToResponse(homework, userId, isTutor);
        }

        public async Task<HomeworkResponse> CreateForLessonAsync(
            int tutorUserId,
            int lessonId,
            CreateHomeworkRequest request)
        {
            var lesson = await GetTutorLessonAsync(tutorUserId, lessonId);

            if (lesson.Booking.Status != "Confirmed" && lesson.Booking.Status != "Completed")
                throw new InvalidOperationException("Homework can only be added after the class is confirmed.");

            var type = NormalizeHomeworkType(request.Type);
            ValidateHomeworkContent(type, request.Questions, request.Essays);

            var homework = new Homework
            {
                LessonId = lesson.LessonId,
                BookingId = lesson.BookingId,
                Type = type,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim() ?? string.Empty,
                DueDate = ToUtc(request.DueDate),
                UploadedAt = DateTime.UtcNow
            };

            ApplyContent(homework, type, request.Questions, request.Essays);

            _db.Homeworks.Add(homework);
            await _db.SaveChangesAsync();

            return await GetByIdAsync(tutorUserId, homework.HomeworkId);
        }

        public async Task<HomeworkResponse> UpdateAsync(
            int tutorUserId,
            int homeworkId,
            UpdateHomeworkRequest request)
        {
            var homework = await HomeworkQuery()
                .FirstOrDefaultAsync(h => h.HomeworkId == homeworkId)
                ?? throw new KeyNotFoundException("Homework not found.");

            await EnsureTutorOwnsHomeworkAsync(tutorUserId, homework);

            if (homework.Submissions.Any())
                throw new InvalidOperationException("Cannot edit homework after submissions have been received.");

            var type = NormalizeHomeworkType(request.Type);
            ValidateHomeworkContent(type, request.Questions, request.Essays);

            homework.Type = type;
            homework.Title = request.Title.Trim();
            homework.Description = request.Description?.Trim() ?? string.Empty;
            homework.DueDate = ToUtc(request.DueDate);

            _db.QuestionOptions.RemoveRange(homework.MultipleChoiceQuestions.SelectMany(q => q.QuestionOptions));
            _db.MultipleChoiceQuestions.RemoveRange(homework.MultipleChoiceQuestions);
            _db.Essays.RemoveRange(homework.Essays);

            homework.MultipleChoiceQuestions.Clear();
            homework.Essays.Clear();

            ApplyContent(homework, type, request.Questions, request.Essays);

            await _db.SaveChangesAsync();

            return await GetByIdAsync(tutorUserId, homework.HomeworkId);
        }

        public async Task DeleteAsync(int tutorUserId, int homeworkId)
        {
            var homework = await HomeworkQuery()
                .FirstOrDefaultAsync(h => h.HomeworkId == homeworkId)
                ?? throw new KeyNotFoundException("Homework not found.");

            await EnsureTutorOwnsHomeworkAsync(tutorUserId, homework);

            _db.Homeworks.Remove(homework);
            await _db.SaveChangesAsync();
        }

        public async Task<HomeworkSubmissionResponse> SubmitAsync(
            int userId,
            int homeworkId,
            SubmitHomeworkRequest request)
        {
            var homework = await HomeworkQuery()
                .FirstOrDefaultAsync(h => h.HomeworkId == homeworkId)
                ?? throw new KeyNotFoundException("Homework not found.");

            if (await IsTutorForHomeworkAsync(userId, homework))
                throw new UnauthorizedAccessException("Tutors cannot submit homework for their own sessions.");

            await EnsureLearnerCanAccessHomeworkAsync(userId, homework);

            var existing = homework.Submissions.FirstOrDefault(s =>
                s.UserId == userId ||
                (s.UserId == null && s.Student != null && s.Student.UserId == userId));

            if (existing != null && existing.GradedAt.HasValue)
                throw new InvalidOperationException("This homework has already been graded.");

            var student = await _db.Students.FirstOrDefaultAsync(s => s.UserId == userId);
            var submission = existing ?? new Submission
            {
                HomeworkId = homework.HomeworkId,
                UserId = userId,
                StudentId = student?.StudentId
            };

            if (existing == null)
            {
                _db.Submissions.Add(submission);
            }
            else
            {
                _db.MultipleChoiceQuestionAnswers.RemoveRange(existing.MultipleChoiceQuestionAnswers);
                _db.EssayAnswers.RemoveRange(existing.EssayAnswers);
                submission.MultipleChoiceQuestionAnswers.Clear();
                submission.EssayAnswers.Clear();
            }

            submission.SubmittedAt = DateTime.UtcNow;
            submission.UserId = userId;
            submission.StudentId = student?.StudentId;
            submission.Feedback = null;

            if (homework.Type == "MultipleChoice")
            {
                SubmitMultipleChoice(homework, submission, request.MultipleChoiceAnswers);
                submission.GradedAt = DateTime.UtcNow;
            }
            else
            {
                SubmitEssay(homework, submission, request.EssayAnswers);
                submission.TotalScore = 0;
                submission.GradedAt = null;
            }

            await _db.SaveChangesAsync();

            var saved = await SubmissionQuery()
                .FirstAsync(s => s.SubmissionId == submission.SubmissionId);

            return ToSubmissionResponse(saved, MaxScore(homework));
        }

        public async Task<HomeworkSubmissionResponse> GradeEssaySubmissionAsync(
            int tutorUserId,
            int homeworkId,
            int submissionId,
            GradeEssaySubmissionRequest request)
        {
            var homework = await HomeworkQuery()
                .FirstOrDefaultAsync(h => h.HomeworkId == homeworkId)
                ?? throw new KeyNotFoundException("Homework not found.");

            await EnsureTutorOwnsHomeworkAsync(tutorUserId, homework);

            if (homework.Type != "Essay")
                throw new InvalidOperationException("Only essay homework requires manual grading.");

            var submission = homework.Submissions.FirstOrDefault(s => s.SubmissionId == submissionId)
                ?? throw new KeyNotFoundException("Submission not found.");

            if (!submission.EssayAnswers.Any())
                throw new InvalidOperationException("This submission has no essay answers.");

            foreach (var grade in request.EssayGrades)
            {
                var answer = submission.EssayAnswers.FirstOrDefault(a => a.EssayAnswerId == grade.EssayAnswerId)
                    ?? throw new InvalidOperationException($"Essay answer #{grade.EssayAnswerId} was not found.");

                var max = answer.Essay?.Points ?? 0;

                if (grade.Score < 0 || grade.Score > max)
                    throw new InvalidOperationException($"Score for essay answer #{answer.EssayAnswerId} must be between 0 and {max}.");

                answer.Score = grade.Score;
                answer.Feedback = grade.Feedback?.Trim() ?? string.Empty;
            }

            var gradedIds = request.EssayGrades.Select(g => g.EssayAnswerId).ToHashSet();
            var missing = submission.EssayAnswers
                .Where(a => !gradedIds.Contains(a.EssayAnswerId))
                .Select(a => a.EssayAnswerId)
                .ToList();

            if (missing.Any())
                throw new InvalidOperationException("All essay answers must be graded.");

            submission.TotalScore = submission.EssayAnswers.Sum(a => a.Score);
            submission.Feedback = request.Feedback?.Trim();
            submission.GradedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return ToSubmissionResponse(submission, MaxScore(homework));
        }

        private IQueryable<Homework> HomeworkQuery()
        {
            return _db.Homeworks
                .Include(h => h.Booking)
                    .ThenInclude(b => b.User)
                .Include(h => h.Booking)
                    .ThenInclude(b => b.Parent)
                .Include(h => h.Booking)
                    .ThenInclude(b => b.Student)
                .Include(h => h.Booking)
                    .ThenInclude(b => b.Availability)
                        .ThenInclude(a => a.Tutor)
                            .ThenInclude(t => t.User)
                .Include(h => h.Lesson)
                .Include(h => h.MultipleChoiceQuestions)
                    .ThenInclude(q => q.QuestionOptions)
                .Include(h => h.Essays)
                .Include(h => h.Submissions)
                    .ThenInclude(s => s.User)
                .Include(h => h.Submissions)
                    .ThenInclude(s => s.Student)
                        .ThenInclude(st => st!.User)
                .Include(h => h.Submissions)
                    .ThenInclude(s => s.MultipleChoiceQuestionAnswers)
                        .ThenInclude(a => a.QuestionOption)
                            .ThenInclude(o => o.MultipleChoiceQuestion)
                .Include(h => h.Submissions)
                    .ThenInclude(s => s.EssayAnswers)
                        .ThenInclude(a => a.Essay);
        }

        private IQueryable<Submission> SubmissionQuery()
        {
            return _db.Submissions
                .Include(s => s.User)
                .Include(s => s.Student)
                    .ThenInclude(st => st!.User)
                .Include(s => s.MultipleChoiceQuestionAnswers)
                    .ThenInclude(a => a.QuestionOption)
                        .ThenInclude(o => o.MultipleChoiceQuestion)
                .Include(s => s.EssayAnswers)
                    .ThenInclude(a => a.Essay);
        }

        private async Task<Lesson> GetLessonForUserAsync(int userId, int lessonId)
        {
            var lesson = await _db.Lessons
                .Include(l => l.Booking)
                    .ThenInclude(b => b.User)
                .Include(l => l.Booking)
                    .ThenInclude(b => b.Parent)
                .Include(l => l.Booking)
                    .ThenInclude(b => b.Student)
                .Include(l => l.Booking)
                    .ThenInclude(b => b.Availability)
                        .ThenInclude(a => a.Tutor)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId)
                ?? throw new KeyNotFoundException("Lesson not found.");

            if (await IsTutorForLessonAsync(userId, lesson) || await IsLearnerForLessonAsync(userId, lesson))
                return lesson;

            throw new UnauthorizedAccessException("You cannot access this lesson.");
        }

        private async Task<Lesson> GetTutorLessonAsync(int tutorUserId, int lessonId)
        {
            var lesson = await _db.Lessons
                .Include(l => l.Booking)
                    .ThenInclude(b => b.Availability)
                        .ThenInclude(a => a.Tutor)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId)
                ?? throw new KeyNotFoundException("Lesson not found.");

            if (!await IsTutorForLessonAsync(tutorUserId, lesson))
                throw new UnauthorizedAccessException("This lesson does not belong to the tutor.");

            return lesson;
        }

        private async Task EnsureCanViewHomeworkAsync(int userId, Homework homework)
        {
            if (await IsTutorForHomeworkAsync(userId, homework))
                return;

            await EnsureLearnerCanAccessHomeworkAsync(userId, homework);
        }

        private async Task EnsureTutorOwnsHomeworkAsync(int tutorUserId, Homework homework)
        {
            if (!await IsTutorForHomeworkAsync(tutorUserId, homework))
                throw new UnauthorizedAccessException("This homework does not belong to the tutor.");
        }

        private async Task EnsureLearnerCanAccessHomeworkAsync(int userId, Homework homework)
        {
            if (homework.Booking.UserId == userId ||
                homework.Booking.Parent?.UserId == userId ||
                homework.Booking.Student?.UserId == userId)
            {
                return;
            }

            var parentId = await _db.Parents
                .Where(p => p.UserId == userId)
                .Select(p => (int?)p.ParentId)
                .FirstOrDefaultAsync();

            if (parentId.HasValue && homework.Booking.Student?.ParentId == parentId.Value)
                return;

            throw new UnauthorizedAccessException("You cannot access this homework.");
        }

        private async Task<bool> IsTutorForHomeworkAsync(int userId, Homework homework)
        {
            var tutorId = await _db.Tutors
                .Where(t => t.UserId == userId)
                .Select(t => (int?)t.TutorId)
                .FirstOrDefaultAsync();

            return tutorId.HasValue && homework.Booking.Availability.TutorId == tutorId.Value;
        }

        private async Task<bool> IsTutorForLessonAsync(int userId, Lesson lesson)
        {
            var tutorId = await _db.Tutors
                .Where(t => t.UserId == userId)
                .Select(t => (int?)t.TutorId)
                .FirstOrDefaultAsync();

            return tutorId.HasValue && lesson.Booking.Availability.TutorId == tutorId.Value;
        }

        private async Task<bool> IsLearnerForLessonAsync(int userId, Lesson lesson)
        {
            if (lesson.Booking.UserId == userId ||
                lesson.Booking.Parent?.UserId == userId ||
                lesson.Booking.Student?.UserId == userId)
            {
                return true;
            }

            var parentId = await _db.Parents
                .Where(p => p.UserId == userId)
                .Select(p => (int?)p.ParentId)
                .FirstOrDefaultAsync();

            return parentId.HasValue && lesson.Booking.Student?.ParentId == parentId.Value;
        }

        private static void ApplyContent(
            Homework homework,
            string type,
            List<HomeworkQuestionRequest> questions,
            List<HomeworkEssayRequest> essays)
        {
            if (type == "MultipleChoice")
            {
                foreach (var question in questions)
                {
                    homework.MultipleChoiceQuestions.Add(new MultipleChoiceQuestion
                    {
                        QuestionText = question.QuestionText.Trim(),
                        Point = question.Point,
                        QuestionOptions = question.Options.Select(o => new QuestionOption
                        {
                            Content = o.Content.Trim(),
                            IsCorrect = o.IsCorrect
                        }).ToList()
                    });
                }

                return;
            }

            foreach (var essay in essays)
            {
                homework.Essays.Add(new Essay
                {
                    QuestionText = essay.QuestionText.Trim(),
                    Points = essay.Points
                });
            }
        }

        private static void SubmitMultipleChoice(
            Homework homework,
            Submission submission,
            List<SubmitMultipleChoiceAnswerRequest> answers)
        {
            var questions = homework.MultipleChoiceQuestions.ToList();

            if (answers.Count != questions.Count)
                throw new InvalidOperationException("Every multiple choice question must be answered.");

            var total = 0d;

            foreach (var question in questions)
            {
                var answer = answers.FirstOrDefault(a => a.MultipleChoiceQuestionId == question.MultipleChoiceQuestionId)
                    ?? throw new InvalidOperationException($"Question #{question.MultipleChoiceQuestionId} was not answered.");

                var option = question.QuestionOptions.FirstOrDefault(o => o.QuestionOptionId == answer.QuestionOptionId)
                    ?? throw new InvalidOperationException($"Selected option does not belong to question #{question.MultipleChoiceQuestionId}.");

                if (option.IsCorrect)
                    total += question.Point;

                submission.MultipleChoiceQuestionAnswers.Add(new MultipleChoiceQuestionAnswer
                {
                    QuestionOptionId = option.QuestionOptionId,
                    SelectedOption = option.Content
                });
            }

            submission.TotalScore = total;
        }

        private static void SubmitEssay(
            Homework homework,
            Submission submission,
            List<SubmitEssayAnswerRequest> answers)
        {
            var essays = homework.Essays.ToList();

            if (answers.Count != essays.Count)
                throw new InvalidOperationException("Every essay prompt must be answered.");

            foreach (var essay in essays)
            {
                var answer = answers.FirstOrDefault(a => a.EssayId == essay.EssayId)
                    ?? throw new InvalidOperationException($"Essay #{essay.EssayId} was not answered.");

                submission.EssayAnswers.Add(new EssayAnswer
                {
                    EssayId = essay.EssayId,
                    AnswerText = answer.AnswerText.Trim(),
                    Score = 0
                });
            }
        }

        private static void ValidateHomeworkContent(
            string type,
            List<HomeworkQuestionRequest> questions,
            List<HomeworkEssayRequest> essays)
        {
            if (type == "MultipleChoice")
            {
                if (!questions.Any())
                    throw new InvalidOperationException("Multiple choice homework needs at least one question.");

                foreach (var question in questions)
                {
                    if (string.IsNullOrWhiteSpace(question.QuestionText))
                        throw new InvalidOperationException("Question text is required.");

                    if (question.Options.Count < 2)
                        throw new InvalidOperationException("Each multiple choice question needs at least two options.");

                    if (!question.Options.Any(o => o.IsCorrect))
                        throw new InvalidOperationException("Each multiple choice question needs a correct answer.");

                    if (question.Options.Any(o => string.IsNullOrWhiteSpace(o.Content)))
                        throw new InvalidOperationException("Option content is required.");
                }

                return;
            }

            if (!essays.Any())
                throw new InvalidOperationException("Essay homework needs at least one prompt.");

            if (essays.Any(e => string.IsNullOrWhiteSpace(e.QuestionText)))
                throw new InvalidOperationException("Essay prompt is required.");
        }

        private static string NormalizeHomeworkType(string type)
        {
            var value = type.Trim();

            if (value.Equals("MultipleChoice", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("Multiple Choice", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("multiple_choice", StringComparison.OrdinalIgnoreCase))
            {
                return "MultipleChoice";
            }

            if (value.Equals("Essay", StringComparison.OrdinalIgnoreCase))
                return "Essay";

            throw new InvalidOperationException("Homework type must be MultipleChoice or Essay.");
        }

        private static HomeworkResponse ToResponse(Homework homework, int userId, bool includeTutorData)
        {
            var maxScore = MaxScore(homework);
            var mySubmission = homework.Submissions.FirstOrDefault(s =>
                s.UserId == userId ||
                (s.UserId == null && s.Student != null && s.Student.UserId == userId));

            return new HomeworkResponse
            {
                HomeworkId = homework.HomeworkId,
                BookingId = homework.BookingId,
                LessonId = homework.LessonId,
                Type = homework.Type,
                Title = homework.Title,
                Description = homework.Description,
                DueDate = homework.DueDate,
                UploadedAt = homework.UploadedAt,
                TotalPoints = maxScore,
                Questions = homework.MultipleChoiceQuestions
                    .OrderBy(q => q.MultipleChoiceQuestionId)
                    .Select(q => new HomeworkQuestionResponse
                    {
                        MultipleChoiceQuestionId = q.MultipleChoiceQuestionId,
                        QuestionText = q.QuestionText,
                        Point = q.Point,
                        Options = q.QuestionOptions
                            .OrderBy(o => o.QuestionOptionId)
                            .Select(o => new HomeworkQuestionOptionResponse
                            {
                                QuestionOptionId = o.QuestionOptionId,
                                Content = o.Content,
                                IsCorrect = includeTutorData ? o.IsCorrect : null
                            })
                            .ToList()
                    })
                    .ToList(),
                Essays = homework.Essays
                    .OrderBy(e => e.EssayId)
                    .Select(e => new HomeworkEssayResponse
                    {
                        EssayId = e.EssayId,
                        QuestionText = e.QuestionText,
                        Points = e.Points
                    })
                    .ToList(),
                MySubmission = mySubmission == null ? null : ToSubmissionResponse(mySubmission, maxScore),
                Submissions = includeTutorData
                    ? homework.Submissions
                        .OrderByDescending(s => s.SubmittedAt)
                        .Select(s => ToSubmissionResponse(s, maxScore))
                        .ToList()
                    : new List<HomeworkSubmissionResponse>()
            };
        }

        private static HomeworkSubmissionResponse ToSubmissionResponse(Submission submission, double maxScore)
        {
            return new HomeworkSubmissionResponse
            {
                SubmissionId = submission.SubmissionId,
                HomeworkId = submission.HomeworkId,
                StudentId = submission.StudentId ?? 0,
                StudentName = submission.User?.Name ??
                    submission.Student?.User?.Name ??
                    $"Student #{submission.StudentId ?? submission.UserId ?? 0}",
                SubmittedAt = submission.SubmittedAt,
                GradedAt = submission.GradedAt,
                TotalScore = submission.TotalScore,
                MaxScore = maxScore,
                IsGraded = submission.GradedAt.HasValue,
                Feedback = submission.Feedback,
                MultipleChoiceAnswers = submission.MultipleChoiceQuestionAnswers
                    .OrderBy(a => a.MultipleChoiceQuestionAnswerId)
                    .Select(a =>
                    {
                        var question = a.QuestionOption.MultipleChoiceQuestion;
                        var isCorrect = a.QuestionOption.IsCorrect;

                        return new MultipleChoiceAnswerResponse
                        {
                            MultipleChoiceQuestionAnswerId = a.MultipleChoiceQuestionAnswerId,
                            MultipleChoiceQuestionId = question.MultipleChoiceQuestionId,
                            QuestionOptionId = a.QuestionOptionId,
                            SelectedOption = a.SelectedOption,
                            IsCorrect = isCorrect,
                            Score = isCorrect ? question.Point : 0
                        };
                    })
                    .ToList(),
                EssayAnswers = submission.EssayAnswers
                    .OrderBy(a => a.EssayAnswerId)
                    .Select(a => new EssayAnswerResponse
                    {
                        EssayAnswerId = a.EssayAnswerId,
                        EssayId = a.EssayId,
                        AnswerText = a.AnswerText,
                        Score = a.Score,
                        Feedback = a.Feedback
                    })
                    .ToList()
            };
        }

        private static double MaxScore(Homework homework)
        {
            return homework.Type == "MultipleChoice"
                ? homework.MultipleChoiceQuestions.Sum(q => q.Point)
                : homework.Essays.Sum(e => e.Points);
        }

        private static DateTime ToUtc(DateTime value)
        {
            if (value.Kind == DateTimeKind.Utc)
                return value;

            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
    }
}
