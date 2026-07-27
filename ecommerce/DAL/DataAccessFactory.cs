using DAL.Models;
using DAL.Interfaces;
using DAL.Repos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL
{
    public static class DataAccessFactory
    {
        public static IOrderRepo OrderData() => new OrderRepo(new UMSContext());
        public static IProductRepo ProductData() => new ProductRepo(new UMSContext());
        public static IPaymentRepo PaymentData() => new PaymentRepo(new UMSContext());
        public static IUserRepo UserData() => new UserRepo(new UMSContext());

        // Overloads that reuse a caller-supplied context, so multiple repos
        // can share one unit-of-work / one SaveChanges() call.
        public static IOrderRepo OrderData(UMSContext db) => new OrderRepo(db);
        public static IProductRepo ProductData(UMSContext db) => new ProductRepo(db);
        public static IPaymentRepo PaymentData(UMSContext db) => new PaymentRepo(db);

        public static ICategoryRepo CategoryData() => new CategoryRepo(new UMSContext());
    }
}
