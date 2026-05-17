using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs.Tutor
{
    public class TutorSubjectResponseDTO
    {
        public int TutorId { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; }
        public string SubjectDescription { get; set; }
        public string Level { get; set; }
        public decimal PricePerCourse { get; set; }
    }

    public class AddTutorSubjectDTO
    {
        public int SubjectId { get; set; }
        public string Level { get; set; }
        public decimal PricePerCourse { get; set; }
    }

    public class UpdateTutorSubjectDTO
    {
        public string? Level { get; set; }
        public decimal? PricePerCourse { get; set; }
    }
}
