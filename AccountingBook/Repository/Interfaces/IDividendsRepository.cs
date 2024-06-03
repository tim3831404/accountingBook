using AccountingBook.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccountingBook.Repository.Interfaces
{
    public interface IDividendsRepository
    {
        Task<HashSet<int>> GetTransationIdInDividends();
    }
}