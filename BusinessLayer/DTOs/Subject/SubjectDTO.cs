using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs.Subject
{
    public class SubjectResponseDTO
    {
        public int SubjectId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class CreateSubjectDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
    }

    public class UpdateSubjectDTO
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}
