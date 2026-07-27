using DAL;
using DAL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL
{
    public static class ServiceFactory
    {
        public static Services.ProductService ProductData() =>
            new Services.ProductService(DataAccessFactory.ProductData());

        public static Services.UserService UserData() =>
            new Services.UserService(DataAccessFactory.UserData());

        public static Services.OrderService OrderData() =>
            new Services.OrderService(
                DataAccessFactory.OrderData(),
                DataAccessFactory.ProductData(),
                Services.StockService.MapperConfig.GetMapper());
        public static Services.CategoryService CategoryData() =>
            new Services.CategoryService(DataAccessFactory.CategoryData());

        public static Services.PaymentOrchestrator PaymentData()
        {
            // One shared UMSContext for order + payment + stock repos,
            // so ExecuteInTransactionAsync's single SaveChangesAsync()
            // actually persists all three changes together.
            var db = new UMSContext();

            var stockService = new Services.StockService(DataAccessFactory.ProductData(db));

            return new Services.PaymentOrchestrator(
                DataAccessFactory.OrderData(db),
                DataAccessFactory.PaymentData(db),
                stockService,
                new PaymentStrategyFactory());
        }
    }
}
