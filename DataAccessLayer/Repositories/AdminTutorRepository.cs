using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace DataAccessLayer.Repositories
{
    public sealed class AdminTutorRepository : IAdminTutorRepository
    {
        private readonly EduNestDbContext _db;

        public AdminTutorRepository(EduNestDbContext db)
        {
            _db = db;
        }

        public async Task<Tutor?> GetTutorWithUserAndBankAsync(int tutorId)
        {
            return await _db.Tutors
                .Include(t => t.User)
                .Include(t => t.BankAccount)
                .FirstOrDefaultAsync(t => t.TutorId == tutorId);
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }
    }
}
