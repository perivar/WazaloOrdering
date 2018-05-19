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
        public static List<ShopifyOrder> GetShopifyOrders(IConfiguration appConfig, string querySuffix)
        {
            // get shopify configuration parameters
            var config = new MyConfiguration(appConfig);
            string shopifyDomain = config.GetString("ShopifyDomain");
            string shopifyAPIKey = config.GetString("ShopifyAPIKey");
            string shopifyAPIPassword = config.GetString("ShopifyAPIPassword");

            var shopifyOrders = new List<ShopifyOrder>();
            shopifyOrders = Shopify.ReadShopifyOrders(shopifyDomain, shopifyAPIKey, shopifyAPIPassword, querySuffix);

            /*
                // read and store the product image url
                foreach (var shopifyOrder in shopifyOrders)
                {
                    foreach (var shopifyOrderLineItem in shopifyOrder.LineItems)
                    {
                        var shopifyProductImages = Shopify.ReadShopifyProductImages(shopifyDomain, shopifyAPIKey, shopifyAPIPassword, shopifyOrderLineItem.ProductId);
                        shopifyOrderLineItem.ProductImages = shopifyProductImages;
                    }
                }
             */
            return shopifyOrders;
        }

        public static ShopifyOrder GetShopifyOrder(IConfiguration appConfig, string orderId, string querySuffix)
        {
            // get shopify configuration parameters
            var config = new MyConfiguration(appConfig);
            string shopifyDomain = config.GetString("ShopifyDomain");
            string shopifyAPIKey = config.GetString("ShopifyAPIKey");
            string shopifyAPIPassword = config.GetString("ShopifyAPIPassword");

            var shopifyOrder = Shopify.ReadShopifyOrder(shopifyDomain, shopifyAPIKey, shopifyAPIPassword, orderId, querySuffix);

            foreach (var shopifyOrderLineItem in shopifyOrder.LineItems)
            {
                //var shopifyProductImages = Shopify.ReadShopifyProductImages(shopifyDomain, shopifyAPIKey, shopifyAPIPassword, shopifyOrderLineItem.ProductId);
                //shopifyOrderLineItem.ProductImages = shopifyProductImages;
                var shopifyProductImages = GetShopifyProductImages(appConfig, long.Parse(shopifyOrderLineItem.ProductId));
                var shopifyProductImages2 = new List<ShopifyProductImage>();
                foreach (var shopifyProductImage in shopifyProductImages)
                {
                    var shopifyProductImage2 = new ShopifyProductImage();
                    shopifyProductImage2.Src = shopifyProductImage.Src;
                    shopifyProductImages2.Add(shopifyProductImage2);
                }
                shopifyOrderLineItem.ProductImages = shopifyProductImages2;
            }

            return shopifyOrder;
        }

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
