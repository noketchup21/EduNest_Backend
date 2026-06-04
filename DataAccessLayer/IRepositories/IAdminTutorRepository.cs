using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Entities;

namespace DataAccessLayer.IRepositories
{
    public interface IAdminTutorRepository
    {
        Task<Tutor?> GetTutorWithUserAndBankAsync(int tutorId);

        Task SaveChangesAsync();
    }
}
