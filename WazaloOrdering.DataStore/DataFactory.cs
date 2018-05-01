using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace WazaloOrdering.DataStore
{
    public class DataFactory
    {
        static public IConfiguration Configuration { get; set; }
        
        public static List<ShopifyOrder> GetShopifyOrders()
        {
            var shopifyOrders = new List<ShopifyOrder>();

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddUserSecrets<DataFactory>();
            Configuration = configurationBuilder.Build();

            // get shopify configuration parameters
            string shopifyDomain = Configuration["ShopifyDomain"];
            string shopifyAPIKey = Configuration["ShopifyAPIKey"];
            string shopifyAPIPassword = Configuration["ShopifyAPIPassword"];

            // add date filter, created_at_min and created_at_max
            var date = new Date();
            var from = date.FirstDayOfTheYear; 
            var to = date.CurrentDate;
            string querySuffix = string.Format(CultureInfo.InvariantCulture, "status=any&created_at_min={0:yyyy-MM-ddTHH:mm:sszzz}&created_at_max={1:yyyy-MM-ddTHH:mm:sszzz}", from, to);
            shopifyOrders = Shopify.ReadShopifyOrders(shopifyDomain, shopifyAPIKey, shopifyAPIPassword, querySuffix);
            Console.Out.WriteLine("Successfully read all Shopify orders ...");
            
            return shopifyOrders;
        }
    }
}
