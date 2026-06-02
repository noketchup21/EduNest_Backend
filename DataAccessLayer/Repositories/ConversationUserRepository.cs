using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataAccessLayer.Entities;
using DataAccessLayer.IRepositories;

namespace DataAccessLayer.Repositories
{
    public class ConversationUserRepository : GenericRepository<ConversationUser>, IConversationUserRepository
    {
        public ConversationUserRepository(EduNestDbContext context) : base(context)
        {
        }
    }
}
