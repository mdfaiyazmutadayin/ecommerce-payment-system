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
    public class UserRepo : IUserRepo
    {
        private readonly UMSContext _db;
        public UserRepo(UMSContext db) => _db = db;

        public bool Create(User entity)
        {
            _db.Users.Add(entity);
            return _db.SaveChanges() > 0;
        }

        public bool Delete(int id)
        {
            var existing = Get(id);
            if (existing == null) return false;
            _db.Users.Remove(existing);
            return _db.SaveChanges() > 0;
        }

        public List<User> Get() => _db.Users.ToList();

        public User Get(int id) => _db.Users.Find(id);

        public bool Update(User entity)
        {
            var existing = Get(entity.Id);
            if (existing == null) return false;
            _db.Entry(existing).CurrentValues.SetValues(entity);
            return _db.SaveChanges() > 0;
        }

        public async Task<User> GetByEmailAsync(string email) =>
            await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
    }
}