using DAL.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface IPaymentRepo : IRepo<Payment, int, bool>
    {
        Task<Payment> GetByTransactionAndProviderAsync(string transactionId, string provider);
        DbContextTransaction BeginTransaction(); // EF6 transactions are synchronous
        Task SaveChangesAsync();

        Task ExecuteInTransactionAsync(Func<Task> work);
    }
}
