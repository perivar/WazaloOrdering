using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

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
                var shopifyProductImages = Shopify.ReadShopifyProductImages(shopifyDomain, shopifyAPIKey, shopifyAPIPassword, shopifyOrderLineItem.ProductId);
                shopifyOrderLineItem.ProductImages = shopifyProductImages;
            }

            return shopifyOrder;
        }

    }
}
