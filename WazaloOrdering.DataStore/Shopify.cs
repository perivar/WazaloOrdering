using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ShopifySharp;
using ShopifySharp.Filters;

namespace WazaloOrdering.DataStore
{
    public class Shopify
    {
        public static async Task<Order> GetShopifyOrder(IConfiguration appConfig, long orderId)
        {
            // get shopify configuration parameters
            string shopifyDomain = appConfig["ShopifyDomain"];
            string shopifyAPIPassword = appConfig["ShopifyAPIPassword"];

            var service = new OrderService(shopifyDomain, shopifyAPIPassword);
            return await service.GetAsync(orderId);
        }

        public static async Task<IEnumerable<Order>> GetShopifyOrders(IConfiguration appConfig, OrderFilter filter)
        {
            // get shopify configuration parameters
            string shopifyDomain = appConfig["ShopifyDomain"];
            string shopifyAPIPassword = appConfig["ShopifyAPIPassword"];

            // first count the total number of orders using the filter
            var service = new OrderService(shopifyDomain, shopifyAPIPassword);
            int orderCount = await service.CountAsync(filter);

            // then iterate through the orders
            var shopifyOrders = new List<Order>();

            int limit = 250;
            if (orderCount > 0)
            {
                int numPages = (int)Math.Ceiling((double)orderCount / limit);
                for (int i = 1; i <= numPages; i++)
                {
                    var ordersOnPage = GetShopifyOrders(appConfig, filter, limit, i).GetAwaiter().GetResult();
                    shopifyOrders.AddRange(ordersOnPage);
                }
            }

            return shopifyOrders;
        }

        public static async Task<IEnumerable<Order>> GetShopifyOrders(IConfiguration appConfig, OrderFilter filter, int limit, int page)
        {
            // get shopify configuration parameters
            string shopifyDomain = appConfig["ShopifyDomain"];
            string shopifyAPIPassword = appConfig["ShopifyAPIPassword"];

            var service = new OrderService(shopifyDomain, shopifyAPIPassword);
            filter.Limit = limit;
            filter.Page = page;

            return await service.ListAsync(filter);
        }

        public static async Task<IEnumerable<ProductImage>> GetShopifyProductImages(IConfiguration appConfig, long productId)
        {
            // get shopify configuration parameters
            var config = new MyConfiguration(appConfig);
            string shopifyDomain = config.GetString("ShopifyDomain");
            string shopifyAPIPassword = config.GetString("ShopifyAPIPassword");

            var service = new ProductImageService(shopifyDomain, shopifyAPIPassword);
            return await service.ListAsync(productId);
        }

    }
}
