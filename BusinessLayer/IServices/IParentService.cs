using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusinessLayer.DTOs.Parent;

namespace BusinessLayer.IServices
{
    public interface IParentService
    {
        Task<string> LinkChildAsync(int parentUserId, LinkChildDTO dto);
        Task<string> UnlinkChildAsync(int parentUserId, int studentId);
        Task<IEnumerable<ChildResponseDTO>> GetMyChildrenAsync(int parentUserId);
    }
}
