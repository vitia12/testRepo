using System.Collections.Generic;
using System.Linq;
using ElkRiv.Web.PrivateLabel.Areas.Account.Models.Analytics;
using ElkRiv.Web.PrivateLabel.Areas.Account.Models.OrderHistoryModels;

namespace ElkRiv.Web.PrivateLabel.Code.Helpers
{
    public class TransactionCompleteHelper
    {
        public static IList<AnalyticsOrderDetailsItemModel> CombineOrderItems(OrderDetailsModel orderModel)
        {
            var orderItems = orderModel.OrderDetailsItemModels;

            var groupedProducts = orderItems.GroupBy(x => x.OrderItem.ProductConfig.Product, x => x);
            var combinedItems = new List<AnalyticsOrderDetailsItemModel>();
            foreach (var groupedProduct in groupedProducts)
            {
                combinedItems.Add(new AnalyticsOrderDetailsItemModel
                {
                    OrderId = orderModel.Order.Id,
                    ModelNumber = groupedProduct.Key.ModelNumber.Replace("'", "\\'"),
                    ModelName = groupedProduct.Key.ModelName.Replace("'", "\\'"),
                    Category = groupedProduct.Key.ProductTypeInfo.Name.Replace("'", "\\'"),
                    Quantity = groupedProduct.Sum(x => x.OrderItem.Quantity),
                    Price = groupedProduct.Sum(x => x.OrderItem.QuantityPrice)
                });
            }
            return combinedItems;
        }
    }
}
}
}