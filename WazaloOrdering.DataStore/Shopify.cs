using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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

        public static async Task<Order> GetShopifyOrder(IConfiguration appConfig, string orderName)
        {
            // get shopify configuration parameters
            string shopifyDomain = appConfig["ShopifyDomain"];
            string shopifyAPIPassword = appConfig["ShopifyAPIPassword"];

            string url = String.Format("https://{0}/admin/orders.json?name={1}&status=any", shopifyDomain, orderName);

            using (var client = new HttpClient())
            {
                var msg = new HttpRequestMessage(method: HttpMethod.Get, requestUri: url);
                msg.Headers.Add("X-Shopify-Access-Token", shopifyAPIPassword);
                msg.Headers.Add("Accept", "application/json");

                // request message is now ready to be sent via HttpClient
                HttpResponseMessage response = client.SendAsync(msg).Result;

                var rawResult = await response.Content.ReadAsStringAsync();

                ShopifyService.CheckResponseExceptions(response, rawResult);

                var serializer = new JsonSerializer { DateParseHandling = DateParseHandling.DateTimeOffset };
                var reader = new JsonTextReader(new StringReader(rawResult));
                var data = serializer.Deserialize<JObject>(reader).SelectToken("orders");
                var result = data.ToObject<List<Order>>();

                return result.FirstOrDefault();
            }
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

        public static async Task<Order> SetOrderNoteAttributes(IConfiguration appConfig, long orderId, Dictionary<string, string> dic)
        {
            List<NoteAttribute> noteAttributes = dic.Select(p => new NoteAttribute { Name = p.Key, Value = p.Value }).ToList();
            return await SetOrderNoteAttributes(appConfig, orderId, noteAttributes);
        }

        public static async Task<Order> SetOrderNoteAttributes(IConfiguration appConfig, long orderId, IEnumerable<NoteAttribute> noteAttributes)
        {
            // get shopify configuration parameters
            var config = new MyConfiguration(appConfig);
            string shopifyDomain = config.GetString("ShopifyDomain");
            string shopifyAPIPassword = config.GetString("ShopifyAPIPassword");

            var service = new OrderService(shopifyDomain, shopifyAPIPassword);
            var order = await service.UpdateAsync(orderId, new Order()
            {
                NoteAttributes = noteAttributes
            });

            return order;
        }

        public static async Task<Fulfillment> FulfillOrder(IConfiguration appConfig, long orderId, string trackingNumber, bool notifyCustomer)
        {
            // get shopify configuration parameters
            var config = new MyConfiguration(appConfig);
            string shopifyDomain = config.GetString("ShopifyDomain");
            string shopifyAPIPassword = config.GetString("ShopifyAPIPassword");

            var service = new FulfillmentService(shopifyDomain, shopifyAPIPassword);
            var fulfillment = new Fulfillment()
            {
                TrackingCompany = "Posten",
                TrackingUrl = $"https://sporing.posten.no/sporing.html?q={trackingNumber}",
                TrackingNumber = trackingNumber
            };

            fulfillment = await service.CreateAsync(orderId, fulfillment, notifyCustomer);
            return fulfillment;
        }

        public static async Task<Fulfillment> FulfillOrderPartially(IConfiguration appConfig, long orderId, long lineItemId, int lineItemQuantity, string trackingNumber, bool notifyCustomer)
        {
            // get shopify configuration parameters
            var config = new MyConfiguration(appConfig);
            string shopifyDomain = config.GetString("ShopifyDomain");
            string shopifyAPIPassword = config.GetString("ShopifyAPIPassword");

            var service = new FulfillmentService(shopifyDomain, shopifyAPIPassword);

            var fulfillment = new Fulfillment()
            {
                TrackingCompany = "Posten",
                TrackingUrl = $"https://sporing.posten.no/sporing.html?q={trackingNumber}",
                TrackingNumber = trackingNumber,
                LineItems = new List<LineItem>()
                {
                    new LineItem()
                    {
                        Id = lineItemId,
                        Quantity = lineItemQuantity
                    }
                }
            };

            fulfillment = await service.CreateAsync(orderId, fulfillment, notifyCustomer);
            return fulfillment;
        }

    }
}
