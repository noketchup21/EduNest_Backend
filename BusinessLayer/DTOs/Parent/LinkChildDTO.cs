using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs.Parent
{
    public class LinkChildDTO
    {
        public string ChildEmail { get; set; }
    }

    public class ChildResponseDTO
    {
        public int StudentId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public decimal Grade { get; set; }
        public string School { get; set; }
    }
}
