using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace WazaloOrdering.DataStore
{
    public class DataFactory
    {
        static public IConfiguration Configuration { get; set; }
        public static List<ShopifyOrder> GetShopifyOrders(string querySuffix)
        {
            var shopifyOrders = new List<ShopifyOrder>();

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddUserSecrets<DataFactory>();
            Configuration = configurationBuilder.Build();

            // get shopify configuration parameters
            string shopifyDomain = Configuration["ShopifyDomain"];
            string shopifyAPIKey = Configuration["ShopifyAPIKey"];
            string shopifyAPIPassword = Configuration["ShopifyAPIPassword"];

            shopifyOrders = Shopify.ReadShopifyOrders(shopifyDomain, shopifyAPIKey, shopifyAPIPassword, querySuffix);

            // read and store the product image url
            foreach (var shopifyOrder in shopifyOrders)
            {
                foreach (var shopifyOrderLineItem in shopifyOrder.LineItems)
                {
                    var shopifyProductImages = Shopify.ReadShopifyProductImages(shopifyDomain, shopifyAPIKey, shopifyAPIPassword, shopifyOrderLineItem.ProductId);
                    shopifyOrderLineItem.ProductImages = shopifyProductImages;
                }
            }

            return shopifyOrders;
        }
    }
}
