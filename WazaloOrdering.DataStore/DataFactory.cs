using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ShopifySharp;
using ShopifySharp.Filters;

namespace WazaloOrdering.DataStore
{
    public class DataFactory
    {
        public static Order GetShopifyOrder(IConfiguration appConfig, long orderId)
        {
            try
            {
                return Shopify.GetShopifyOrder(appConfig, orderId).GetAwaiter().GetResult();
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        public static IEnumerable<Order> GetShopifyOrders(IConfiguration appConfig, OrderFilter filter)
        {
            try
            {
                return Shopify.GetShopifyOrders(appConfig, filter).GetAwaiter().GetResult();
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        public static IEnumerable<ProductImage> GetShopifyProductImages(IConfiguration appConfig, long productId)
        {
            try
            {
                return Shopify.GetShopifyProductImages(appConfig, productId).GetAwaiter().GetResult();
            }
            catch (System.Exception)
            {
                return null;
            }
        }

    }
}
