using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.DTOs.User
{
    public class UserUpdateDto
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public bool? IsActive { get; set; }
    }
}
