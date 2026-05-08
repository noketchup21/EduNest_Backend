using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Entities
{
    public class EduNestDbContext : DbContext
    {
        public EduNestDbContext(DbContextOptions<EduNestDbContext> options) : base(options) { }

        // ── Identity ──────────────────────────────────────────────────────────
        public DbSet<User> Users { get; set; }
        public DbSet<Tutor> Tutors { get; set; }
        public DbSet<Parent> Parents { get; set; }
        public DbSet<Student> Students { get; set; }

        // ── Subject & scheduling ──────────────────────────────────────────────
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<TutorSubject> TutorSubjects { get; set; }
        public DbSet<Availability> Availabilities { get; set; }

        // ── Booking & payment ─────────────────────────────────────────────────
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<ProgressReport> ProgressReports { get; set; }
        public DbSet<Review> Reviews { get; set; }

        // ── Classes & content ─────────────────────────────────────────────────
        public DbSet<Class> Classes { get; set; }
        public DbSet<ClassStudent> ClassStudents { get; set; }
        public DbSet<Homework> Homeworks { get; set; }
        public DbSet<Material> Materials { get; set; }

        // ── Assignments & submissions ─────────────────────────────────────────
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<MultipleChoiceQuestion> MultipleChoiceQuestions { get; set; }
        public DbSet<MultipleChoiceQuestionAnswer> MultipleChoiceQuestionAnswers { get; set; }
        public DbSet<Essay> Essays { get; set; }
        public DbSet<EssayAnswer> EssayAnswers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Composite primary keys ────────────────────────────────────────

            modelBuilder.Entity<TutorSubject>()
                .HasKey(ts => new { ts.SubjectId, ts.TutorId });

            modelBuilder.Entity<ClassStudent>()
                .HasKey(cs => new { cs.StudentId, cs.ClassId });

            // ── User 1:1 relationships ────────────────────────────────────────

            modelBuilder.Entity<Tutor>()
                .HasOne(t => t.User)
                .WithOne(u => u.Tutor)
                .HasForeignKey<Tutor>(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Parent>()
                .HasOne(p => p.User)
                .WithOne(u => u.Parent)
                .HasForeignKey<Parent>(p => p.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Parent → Student ──────────────────────────────────────────────

            modelBuilder.Entity<Student>()
                .HasOne(s => s.Parent)
                .WithMany(p => p.Students)
                .HasForeignKey(s => s.ParentId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── TutorSubject ──────────────────────────────────────────────────

            modelBuilder.Entity<TutorSubject>()
                .HasOne(ts => ts.Tutor)
                .WithMany(t => t.TutorSubjects)
                .HasForeignKey(ts => ts.TutorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TutorSubject>()
                .HasOne(ts => ts.Subject)
                .WithMany(s => s.TutorSubjects)
                .HasForeignKey(ts => ts.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Availability ──────────────────────────────────────────────────

            modelBuilder.Entity<Availability>()
                .HasOne(a => a.Tutor)
                .WithMany(t => t.Availabilities)
                .HasForeignKey(a => a.TutorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Availability>()
                .HasOne(a => a.Subject)
                .WithMany(s => s.Availabilities)
                .HasForeignKey(a => a.SubjectId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── Booking ───────────────────────────────────────────────────────
            // Restrict on all three FKs to avoid multiple cascade paths

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Tutor)
                .WithMany(t => t.Bookings)
                .HasForeignKey(b => b.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Subject)
                .WithMany(s => s.Bookings)
                .HasForeignKey(b => b.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Student)
                .WithMany(s => s.Bookings)
                .HasForeignKey(b => b.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Payment ───────────────────────────────────────────────────────

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithMany(b => b.Payments)
                .HasForeignKey(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Lesson ────────────────────────────────────────────────────────

            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Booking)
                .WithMany(b => b.Lessons)
                .HasForeignKey(l => l.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── ProgressReport ────────────────────────────────────────────────

            modelBuilder.Entity<ProgressReport>()
                .HasOne(pr => pr.Lesson)
                .WithMany(l => l.ProgressReports)
                .HasForeignKey(pr => pr.LessonId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProgressReport>()
                .HasOne(pr => pr.Tutor)
                .WithMany(t => t.ProgressReports)
                .HasForeignKey(pr => pr.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProgressReport>()
                .HasOne(pr => pr.Student)
                .WithMany(s => s.ProgressReports)
                .HasForeignKey(pr => pr.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Review ────────────────────────────────────────────────────────

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Tutor)
                .WithMany(t => t.Reviews)
                .HasForeignKey(r => r.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Booking)
                .WithMany(b => b.Reviews)
                .HasForeignKey(r => r.BookingId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Parent)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ParentId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── Class ─────────────────────────────────────────────────────────

            modelBuilder.Entity<Class>()
                .HasOne(c => c.Tutor)
                .WithMany(t => t.Classes)
                .HasForeignKey(c => c.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Class>()
                .HasOne(c => c.Subject)
                .WithMany(s => s.Classes)
                .HasForeignKey(c => c.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── ClassStudent ──────────────────────────────────────────────────

            modelBuilder.Entity<ClassStudent>()
                .HasOne(cs => cs.Student)
                .WithMany(s => s.ClassStudents)
                .HasForeignKey(cs => cs.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClassStudent>()
                .HasOne(cs => cs.Class)
                .WithMany(c => c.ClassStudents)
                .HasForeignKey(cs => cs.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Homework ──────────────────────────────────────────────────────

            modelBuilder.Entity<Homework>()
                .HasOne(h => h.Class)
                .WithMany(c => c.Homeworks)
                .HasForeignKey(h => h.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Material ──────────────────────────────────────────────────────

            modelBuilder.Entity<Material>()
                .HasOne(m => m.Class)
                .WithMany(c => c.Materials)
                .HasForeignKey(m => m.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Submission ────────────────────────────────────────────────────

            modelBuilder.Entity<Submission>()
                .HasOne(s => s.Homework)
                .WithMany(h => h.Submissions)
                .HasForeignKey(s => s.HomeworkId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Submission>()
                .HasOne(s => s.Student)
                .WithMany(st => st.Submissions)
                .HasForeignKey(s => s.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── MultipleChoiceQuestion ────────────────────────────────────────

            modelBuilder.Entity<MultipleChoiceQuestion>()
                .HasOne(q => q.Homework)
                .WithMany(h => h.MultipleChoiceQuestions)
                .HasForeignKey(q => q.HomeworkId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── MultipleChoiceQuestionAnswer ──────────────────────────────────

            modelBuilder.Entity<MultipleChoiceQuestionAnswer>()
                .HasOne(a => a.MultipleChoiceQuestion)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.MultipleChoiceQuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<MultipleChoiceQuestionAnswer>()
                .HasOne(a => a.Submission)
                .WithMany(s => s.MultipleChoiceQuestionAnswers)
                .HasForeignKey(a => a.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Essay ─────────────────────────────────────────────────────────

            modelBuilder.Entity<Essay>()
                .HasOne(e => e.Homework)
                .WithMany(h => h.Essays)
                .HasForeignKey(e => e.HomeworkId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── EssayAnswer ───────────────────────────────────────────────────

            modelBuilder.Entity<EssayAnswer>()
                .HasOne(ea => ea.Essay)
                .WithMany(e => e.EssayAnswers)
                .HasForeignKey(ea => ea.EssayId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<EssayAnswer>()
                .HasOne(ea => ea.Submission)
                .WithMany(s => s.EssayAnswers)
                .HasForeignKey(ea => ea.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Indexes ───────────────────────────────────────────────────────

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.TutorId, b.StudentId, b.Status });

            modelBuilder.Entity<Availability>()
                .HasIndex(a => new { a.TutorId, a.DayOfWeek });

            modelBuilder.Entity<ClassStudent>()
                .HasIndex(cs => cs.ClassId);

            modelBuilder.Entity<Submission>()
                .HasIndex(s => new { s.HomeworkId, s.StudentId })
                .IsUnique(); // one submission per student per homework
        }
    }
}
