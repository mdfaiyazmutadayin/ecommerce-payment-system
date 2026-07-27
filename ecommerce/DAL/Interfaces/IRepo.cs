using System;
using System.Collections.Generic;
using DAL.Models;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Interfaces
{
    public interface IRepo<TEntity, TKey, TReturn>
    {
        TReturn Create(TEntity entity);
        List<TEntity> Get();
        TEntity Get(TKey id);
        TReturn Update(TEntity entity);
        TReturn Delete(TKey id);
    }
}
