using AccountingBook.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccountingBook.Interfaces
{
    public interface IUserService
    {
        Task<IEnumerable<Users>> GetAllUsersAsync();
    }
}
