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
        public DbSet<Tier> Tiers { get; set; }

        // ── Subject & Availability ────────────────────────────────────────────
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<TutorSubject> TutorSubjects { get; set; }
        public DbSet<Availability> Availabilities { get; set; }

        // ── Booking & Payment ─────────────────────────────────────────────────
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }

        // ── Lesson & Tracking ─────────────────────────────────────────────────
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<ProgressReport> ProgressReports { get; set; }
        public DbSet<Review> Reviews { get; set; }

        // ── Learning Content ──────────────────────────────────────────────────
        public DbSet<Homework> Homeworks { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<MultipleChoiceQuestion> MultipleChoiceQuestions { get; set; }
        public DbSet<QuestionOption> QuestionOptions { get; set; }
        public DbSet<MultipleChoiceQuestionAnswer> MultipleChoiceQuestionAnswers { get; set; }
        public DbSet<Essay> Essays { get; set; }
        public DbSet<EssayAnswer> EssayAnswers { get; set; }

        // ── Chat ──────────────────────────────────────────────────────────────
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationUser> ConversationUsers { get; set; }
        public DbSet<Message> Messages { get; set; }

        // ── Wallet & Payout ───────────────────────────────────────────────────
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
        public DbSet<Payout> Payouts { get; set; }
        public DbSet<TutorBankAccount> TutorBankAccounts { get; set; }

        // ── Parent Extras ─────────────────────────────────────────────────────
        public DbSet<FavoriteTutor> FavoriteTutors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ── Composite PKs ─────────────────────────────────────────────────
            modelBuilder.Entity<TutorSubject>()
                .HasKey(ts => new { ts.SubjectId, ts.TutorId });

            modelBuilder.Entity<ConversationUser>()
                .HasKey(cu => new { cu.ConversationId, cu.UserId });

            // ── User → Tutor / Parent / Student (1:1) ─────────────────────────
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

            // ── Tier → Tutor ──────────────────────────────────────────────────
            modelBuilder.Entity<Tutor>()
                .HasOne(t => t.Tier)
                .WithMany(ti => ti.Tutors)
                .HasForeignKey(t => t.TierId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── Tutor → Wallet (1:1) ──────────────────────────────────────────
            modelBuilder.Entity<Wallet>()
                .HasOne(w => w.Tutor)
                .WithOne(t => t.Wallet)
                .HasForeignKey<Wallet>(w => w.TutorId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Tutor → BankAccount (1:1) ─────────────────────────────────────
            modelBuilder.Entity<TutorBankAccount>()
                .HasOne(b => b.Tutor)
                .WithOne(t => t.BankAccount)
                .HasForeignKey<TutorBankAccount>(b => b.TutorId)
                .OnDelete(DeleteBehavior.Cascade);

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
                .OnDelete(DeleteBehavior.Restrict);

            // ── Booking ───────────────────────────────────────────────────────
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Availability)
                .WithMany(a => a.Bookings)
                .HasForeignKey(b => b.AvailabilityId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Parent)
                .WithMany(p => p.Bookings)
                .HasForeignKey(b => b.ParentId)
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

            // ── Attendance ────────────────────────────────────────────────────
            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Lesson)
                .WithMany(l => l.Attendances)
                .HasForeignKey(a => a.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Student)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Attendance>()
                .HasIndex(a => new { a.LessonId, a.StudentId })
                .IsUnique();

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

            // ── Homework → Booking ────────────────────────────────────────────
            modelBuilder.Entity<Homework>()
                .HasOne(h => h.Booking)
                .WithMany(b => b.Homeworks)
                .HasForeignKey(h => h.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Material → Availability ───────────────────────────────────────
            modelBuilder.Entity<Material>()
                .HasOne(m => m.Availability)
                .WithMany(a => a.Materials)
                .HasForeignKey(m => m.AvailabilityId)
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

            modelBuilder.Entity<Submission>()
                .HasIndex(s => new { s.HomeworkId, s.StudentId })
                .IsUnique();

            // ── MultipleChoiceQuestion ────────────────────────────────────────
            modelBuilder.Entity<MultipleChoiceQuestion>()
                .HasOne(q => q.Homework)
                .WithMany(h => h.MultipleChoiceQuestions)
                .HasForeignKey(q => q.HomeworkId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── QuestionOption ────────────────────────────────────────────────
            modelBuilder.Entity<QuestionOption>()
                .HasOne(qo => qo.MultipleChoiceQuestion)
                .WithMany(q => q.QuestionOptions)
                .HasForeignKey(qo => qo.MultipleChoiceQuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── MultipleChoiceQuestionAnswer ──────────────────────────────────
            modelBuilder.Entity<MultipleChoiceQuestionAnswer>()
                .HasOne(a => a.QuestionOption)
                .WithMany(qo => qo.Answers)
                .HasForeignKey(a => a.QuestionOptionId)
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

            // ── Conversation ──────────────────────────────────────────────────
            modelBuilder.Entity<Conversation>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── ConversationUser ──────────────────────────────────────────────
            modelBuilder.Entity<ConversationUser>()
                .HasOne(cu => cu.Conversation)
                .WithMany(c => c.ConversationUsers)
                .HasForeignKey(cu => cu.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ConversationUser>()
                .HasOne(cu => cu.User)
                .WithMany(u => u.ConversationUsers)
                .HasForeignKey(cu => cu.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Message ───────────────────────────────────────────────────────
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Message>()
                .HasOne(m => m.User)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ── WalletTransaction ─────────────────────────────────────────────
            modelBuilder.Entity<WalletTransaction>()
                .HasOne(wt => wt.Wallet)
                .WithMany(w => w.WalletTransactions)
                .HasForeignKey(wt => wt.WalletId)
                .OnDelete(DeleteBehavior.Cascade);

            // ── Payout ────────────────────────────────────────────────────────
            modelBuilder.Entity<Payout>()
                .HasOne(p => p.Tutor)
                .WithMany(t => t.Payouts)
                .HasForeignKey(p => p.TutorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payout>()
                .HasOne(p => p.WalletTransaction)
                .WithOne(wt => wt.Payout)
                .HasForeignKey<Payout>(p => p.WalletTransactionId)
                .OnDelete(DeleteBehavior.SetNull);

            // ── FavoriteTutor ─────────────────────────────────────────────────
            modelBuilder.Entity<FavoriteTutor>()
                .HasOne(f => f.Parent)
                .WithMany(p => p.FavoriteTutors)
                .HasForeignKey(f => f.ParentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FavoriteTutor>()
                .HasOne(f => f.Tutor)
                .WithMany(t => t.FavoriteTutors)
                .HasForeignKey(f => f.TutorId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FavoriteTutor>()
                .HasIndex(f => new { f.ParentId, f.TutorId })
                .IsUnique();

            // ── Indexes ───────────────────────────────────────────────────────
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.AvailabilityId, b.StudentId, b.Status });

            modelBuilder.Entity<Availability>()
                .HasIndex(a => new { a.TutorId, a.DayOfWeek });

            // ── PostgreSQL lowercase naming ───────────────────────────────────  ← ADD HERE
            foreach (var entity in modelBuilder.Model.GetEntityTypes())
            {
                // Lowercase table names
                entity.SetTableName(entity.GetTableName()?.ToLower());

                // Lowercase column names
                foreach (var property in entity.GetProperties())
                    property.SetColumnName(property.GetColumnName().ToLower());

                // Lowercase key names
                foreach (var key in entity.GetKeys())
                    key.SetName(key.GetName()?.ToLower());

                // Lowercase foreign key names
                foreach (var fk in entity.GetForeignKeys())
                    fk.SetConstraintName(fk.GetConstraintName()?.ToLower());

                // Lowercase index names
                foreach (var index in entity.GetIndexes())
                    index.SetDatabaseName(index.GetDatabaseName()?.ToLower());
            }
        }
    }
}