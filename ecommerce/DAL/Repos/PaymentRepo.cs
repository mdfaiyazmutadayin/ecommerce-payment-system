using DAL.Interfaces;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repos
{
    public class PaymentRepo : IPaymentRepo
    {
        private readonly UMSContext _db;
        public PaymentRepo(UMSContext db) => _db = db;

        public bool Create(Payment entity)
        {
            _db.Payments.Add(entity);
            return _db.SaveChanges() > 0;
        }

        public bool Delete(int id)
        {
            var exobj = Get(id);
            if (exobj == null) return false;
            _db.Payments.Remove(exobj);
            return _db.SaveChanges() > 0;
        }

        public List<Payment> Get() => _db.Payments.ToList();

        public Payment Get(int id) => _db.Payments.Find(id);

        public Payment GetByTransactionId(string provider, string transactionId) =>
            _db.Payments.FirstOrDefault(p =>
                p.Provider == provider.ToLower() && p.TransactionId == transactionId);

        public bool Update(Payment entity)
        {
            var exobj = Get(entity.Id);
            if (exobj == null) return false;
            _db.Entry(exobj).CurrentValues.SetValues(entity);
            return _db.SaveChanges() > 0;
        }

        public async Task<Payment> GetByTransactionAndProviderAsync(string transactionId, string provider)
        {
            return await _db.Payments
                .FirstOrDefaultAsync(p => p.TransactionId == transactionId && p.Provider == provider);
        }

        public DbContextTransaction BeginTransaction()
        {
            return _db.Database.BeginTransaction();
        }

        public async Task SaveChangesAsync()
        {
            await _db.SaveChangesAsync();
        }

        public async Task ExecuteInTransactionAsync(Func<Task> work)
        {
            using (var trx = _db.Database.BeginTransaction())
            {
                await work();
                await _db.SaveChangesAsync();
                trx.Commit();
            }
        }
    }
}
