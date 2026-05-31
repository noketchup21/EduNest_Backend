using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace DataAccessLayer.Entities
{
    [Table("Tutors")]
    public class Tutor
    {
        [Key]
        public int TutorId { get; set; }

        public int UserId { get; set; }

        [MaxLength(1000)]
        public string Bio { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Revenue { get; set; }

        public double Rating { get; set; }

        public bool IsVerified { get; set; } = false;

        public int? TierId { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [ForeignKey("TierId")]
        public virtual Tier Tier { get; set; }

        public virtual Wallet Wallet { get; set; }
        public virtual TutorBankAccount BankAccount { get; set; }
        public virtual ICollection<TutorSubject> TutorSubjects { get; set; } = new List<TutorSubject>();
        public virtual ICollection<Availability> Availabilities { get; set; } = new List<Availability>();
        public virtual ICollection<ProgressReport> ProgressReports { get; set; } = new List<ProgressReport>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<FavoriteTutor> FavoriteTutors { get; set; } = new List<FavoriteTutor>();
        public virtual ICollection<Payout> Payouts { get; set; } = new List<Payout>();
    }
}
