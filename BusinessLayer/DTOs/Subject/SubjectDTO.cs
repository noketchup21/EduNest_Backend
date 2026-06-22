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
        public string? Objective { get; set; }
        public string? LearningGoals { get; set; }
        public string? ExpectedResults { get; set; }
        public string? RequiredTopics { get; set; }
        public string? CommonDifficulties { get; set; }
    }

    public class CreateSubjectDTO
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string? Objective { get; set; }
        public string? LearningGoals { get; set; }
        public string? ExpectedResults { get; set; }
        public string? RequiredTopics { get; set; }
        public string? CommonDifficulties { get; set; }
    }

    public class UpdateSubjectDTO
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Objective { get; set; }
        public string? LearningGoals { get; set; }
        public string? ExpectedResults { get; set; }
        public string? RequiredTopics { get; set; }
        public string? CommonDifficulties { get; set; }
    }
}
