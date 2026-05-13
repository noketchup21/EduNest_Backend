using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;

namespace DataAccessLayer.Repositories
{
    public class TutorRepository : GenericRepository<Tutor>, ITutorRepository
    {
        public TutorRepository(EduNestDbContext context) : base(context)
        {
        }
    }
}
