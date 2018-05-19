﻿using System;
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
        public static int CountShopifyProductImages(string shopifyDomain, string shopifyAPIKey, string shopifyAPIPassword, string productId)
        {
            // GET /admin/products/#{product_id}/images/count.json
            // Retrieve a count of all the product images

            string url = String.Format("https://{0}/admin/products/{1}/count.json", shopifyDomain, productId);

            using (var client = new WebClient())
            {
                // make sure we read in utf8
                client.Encoding = System.Text.Encoding.UTF8;

                // have to use the header field since normal GET url doesn't work, i.e.
                // string url = String.Format("https://{0}:{1}@{2}/admin/orders.json", shopifyAPIKey, shopifyAPIPassword, shopifyDomain);
                // https://stackoverflow.com/questions/28177871/shopify-and-private-applications
                client.Headers.Add("X-Shopify-Access-Token", shopifyAPIPassword);
                string json = client.DownloadString(url);

                // parse json
                dynamic jsonDe = JsonConvert.DeserializeObject(json);

                return jsonDe.count;
            }
        }

        public static List<ShopifyProductImage> ReadShopifyProductImages(string shopifyDomain, string shopifyAPIKey, string shopifyAPIPassword, string productId)
        {
            var productImages = new List<ShopifyProductImage>();

            // GET /admin/products/#{product_id}/images.json
            // Retrieve a all product images for a product

            string url = String.Format("https://{0}/admin/products/{1}/images.json", shopifyDomain, productId);

            using (var client = new WebClient())
            {
                // make sure we read in utf8
                client.Encoding = System.Text.Encoding.UTF8;

                // have to use the header field since normal GET url doesn't work, i.e.
                // string url = String.Format("https://{0}:{1}@{2}/admin/orders.json", shopifyAPIKey, shopifyAPIPassword, shopifyDomain);
                // https://stackoverflow.com/questions/28177871/shopify-and-private-applications
                client.Headers.Add("X-Shopify-Access-Token", shopifyAPIPassword);
                string json = client.DownloadString(url);

                // parse json
                dynamic jsonDe = JsonConvert.DeserializeObject(json);

                foreach (var image in jsonDe.images)
                {
                    var shopifyProductImage = new ShopifyProductImage();
                    shopifyProductImage.Id = image.id;
                    shopifyProductImage.ProductId = image.product_id;
                    shopifyProductImage.Position = image.position;
                    shopifyProductImage.CreatedAt = image.created_at;
                    shopifyProductImage.UpdatedAt = image.updated_at;
                    shopifyProductImage.Alt = image.alt;
                    shopifyProductImage.Width = image.width;
                    shopifyProductImage.Height = image.height;
                    shopifyProductImage.Src = image.src;

                    productImages.Add(shopifyProductImage);
                }
            }

            return productImages;
        }

        public static int CountShopifyOrders(string shopifyDomain, string shopifyAPIKey, string shopifyAPIPassword, string querySuffix)
        {
            // GET /admin/orders/count.json
            // Retrieve a count of all the orders

            string url = String.Format("https://{0}/admin/orders/count.json?{1}", shopifyDomain, querySuffix);

            using (var client = new WebClient())
            {
                // make sure we read in utf8
                client.Encoding = System.Text.Encoding.UTF8;

                // have to use the header field since normal GET url doesn't work, i.e.
                // string url = String.Format("https://{0}:{1}@{2}/admin/orders.json", shopifyAPIKey, shopifyAPIPassword, shopifyDomain);
                // https://stackoverflow.com/questions/28177871/shopify-and-private-applications
                client.Headers.Add("X-Shopify-Access-Token", shopifyAPIPassword);
                string json = client.DownloadString(url);

                // parse json
                dynamic jsonDe = JsonConvert.DeserializeObject(json);

                return jsonDe.count;
            }
        }
        public static void ReadShopifyOrdersPage(List<ShopifyOrder> shopifyOrders, string shopifyDomain, string shopifyAPIKey, string shopifyAPIPassword, int limit, int page, string querySuffix)
        {
            // GET /admin/orders.json?limit=250&page=1
            // Retrieve a list of Orders(OPEN Orders by default, use status=any for ALL orders)

            // parameters:
            // financial_status=paid
            // status=any

            // By default that Orders API endpoint can give you a maximum of 50 orders. 
            // You can increase the limit to 250 orders by adding &limit=250 to the URL. 
            // If your query has more than 250 results then you can page through them 
            // by using the page URL parameter: https://help.shopify.com/api/reference/order
            // limit: Amount of results (default: 50)(maximum: 250)
            // page: Page to show, (default: 1)

            string url = String.Format("https://{0}/admin/orders.json?limit={1}&page={2}&{3}", shopifyDomain, limit, page, querySuffix);

            using (var client = new WebClient())
            {
                // make sure we read in utf8
                client.Encoding = System.Text.Encoding.UTF8;

                // have to use the header field since normal GET url doesn't work, i.e.
                // string url = String.Format("https://{0}:{1}@{2}/admin/orders.json", shopifyAPIKey, shopifyAPIPassword, shopifyDomain);
                // https://stackoverflow.com/questions/28177871/shopify-and-private-applications
                client.Headers.Add("X-Shopify-Access-Token", shopifyAPIPassword);
                string json = client.DownloadString(url);

                // parse json
                dynamic jsonDe = JsonConvert.DeserializeObject(json);

                foreach (var order in jsonDe.orders)
                {
                    var shopifyOrder = ParseShopifyOrder(order);

                    // add the order
                    shopifyOrders.Add(shopifyOrder);
                }
            }
        }

        public static ShopifyOrder ReadShopifyOrder(string shopifyDomain, string shopifyAPIKey, string shopifyAPIPassword, string orderId, string querySuffix)
        {
            // GET /admin/orders/#{id}.json
            // Receive a single Order

            // Get only particular fields
            // GET /admin/orders/#{order_id}.json?fields=id,line_items,name,total_price

            string url = String.Format("https://{0}/admin/orders/{1}.json?{2}", shopifyDomain, orderId, querySuffix);

            using (var client = new WebClient())
            {
                // make sure we read in utf8
                client.Encoding = System.Text.Encoding.UTF8;

                // have to use the header field since normal GET url doesn't work, i.e.
                // https://stackoverflow.com/questions/28177871/shopify-and-private-applications
                client.Headers.Add("X-Shopify-Access-Token", shopifyAPIPassword);
                string json = client.DownloadString(url);

                // parse json
                dynamic jsonDe = JsonConvert.DeserializeObject(json);

                var shopifyOrder = ParseShopifyOrder(jsonDe.order);

                return shopifyOrder;
            }
        }

        private static ShopifyOrder ParseShopifyOrder(dynamic order)
        {
            var shopifyOrder = new ShopifyOrder();

            shopifyOrder.Id = order.id;
            shopifyOrder.ClosedAt = (order.closed_at == null ? new DateTime() : order.closed_at);
            shopifyOrder.CreatedAt = order.created_at;
            shopifyOrder.ProcessedAt = order.processed_at;
            shopifyOrder.UpdatedAt = order.updated_at;
            shopifyOrder.Name = order.name;
            shopifyOrder.FinancialStatus = order.financial_status;
            string fulfillmentStatusTmp = order.fulfillment_status;
            fulfillmentStatusTmp = (fulfillmentStatusTmp == null ? "unfulfilled" : fulfillmentStatusTmp);
            shopifyOrder.FulfillmentStatus = fulfillmentStatusTmp;

            shopifyOrder.Gateway = order.gateway;
            if (null != order.payment_details)
            {
                shopifyOrder.PaymentId = string.Format("{0} {1}", order.payment_details.credit_card_company, order.payment_details.credit_card_bin);
            }

            shopifyOrder.TotalPrice = order.total_price;
            shopifyOrder.TotalTax = order.total_tax;
            shopifyOrder.CustomerEmail = order.contact_email;

            shopifyOrder.CustomerName = string.Format("{0} {1}", order.customer.first_name, order.customer.last_name);
            shopifyOrder.CustomerAddress = order.customer.default_address.address1;
            shopifyOrder.CustomerAddress2 = order.customer.default_address.address2;
            shopifyOrder.CustomerCity = order.customer.default_address.city;
            shopifyOrder.CustomerZipCode = order.customer.default_address.zip;
            shopifyOrder.CustomerCountry = order.customer.default_address.country;

            // check if cancelled_at exists (meaning the order has been cancelled)
            var cancelledAt = order.cancelled_at;
            if (cancelledAt != null && cancelledAt.Type != JTokenType.Null)
            {
                shopifyOrder.CancelledAt = order.cancelled_at;
            }

            // also add note
            shopifyOrder.Note = order.note;

            if (shopifyOrder.Name.Equals("#1103"))
            {
                // breakpoint here
            }
            if (shopifyOrder.CustomerEmail.Equals("janne.braseth@gmail.com"))
            {
                // breakpoint here
            }

            if (order.refunds != null)
            {
                decimal refundSubTotal = 0;
                decimal refundTotalTax = 0;

                // calculate refund
                foreach (var refund in order.refunds)
                {
                    var refundItems = refund.refund_line_items;
                    foreach (var refundItem in refundItems)
                    {
                        refundSubTotal += (decimal)refundItem.subtotal;
                        refundTotalTax += (decimal)refundItem.total_tax;
                    }

                    var orderAdjustments = refund.order_adjustments;
                    foreach (var orderAdjustment in orderAdjustments)
                    {
                        refundSubTotal += -((decimal)orderAdjustment.amount);
                        refundTotalTax += -((decimal)orderAdjustment.tax_amount);
                    }
                }

                // perform refund
                shopifyOrder.TotalPrice -= refundSubTotal;
                shopifyOrder.TotalTax -= refundTotalTax;
            }

            if (order.line_items.HasValues)
            {
                var shopifyOrderLineItems = new List<ShopifyOrderLineItem>();
                foreach (var line_item in order.line_items)
                {
                    var shopifyOrderLineItem = new ShopifyOrderLineItem();
                    shopifyOrderLineItem.FulfillableQuantity = line_item.fulfillable_quantity;
                    shopifyOrderLineItem.FulfillmentService = line_item.fulfillment_service;
                    shopifyOrderLineItem.FulfillmentStatus = line_item.fulfillment_status;
                    shopifyOrderLineItem.Grams = line_item.grams;
                    shopifyOrderLineItem.Id = line_item.id;
                    shopifyOrderLineItem.Price = line_item.price;
                    shopifyOrderLineItem.ProductId = line_item.product_id;
                    shopifyOrderLineItem.Quantity = line_item.quantity;
                    shopifyOrderLineItem.RequiresShipping = line_item.requires_shipping;
                    shopifyOrderLineItem.Sku = line_item.sku;
                    shopifyOrderLineItem.Title = line_item.title;
                    shopifyOrderLineItem.VariantId = line_item.variant_id;
                    shopifyOrderLineItem.VariantTitle = line_item.variant_title;
                    shopifyOrderLineItem.Vendor = line_item.vendor;
                    shopifyOrderLineItem.Name = line_item.name;
                    shopifyOrderLineItem.GiftCard = line_item.gift_card;
                    shopifyOrderLineItem.Taxable = line_item.taxable;
                    shopifyOrderLineItem.TotalDiscount = line_item.total_discount;
                    shopifyOrderLineItems.Add(shopifyOrderLineItem);
                }
                shopifyOrder.LineItems = shopifyOrderLineItems;
            }

            if (order.fulfillments.HasValues)
            {
                var shopifyOrderFulfillments = new List<ShopifyOrderFulfillment>();
                foreach (var fulfillment in order.fulfillments)
                {
                    var shopifyOrderFulfillment = new ShopifyOrderFulfillment();
                    shopifyOrderFulfillment.Id = fulfillment.id;
                    shopifyOrderFulfillment.OrderId = fulfillment.order_id;
                    shopifyOrderFulfillment.Status = fulfillment.status;
                    shopifyOrderFulfillment.CreatedAt = fulfillment.created_at;
                    shopifyOrderFulfillment.Service = fulfillment.service;
                    shopifyOrderFulfillment.UpdatedAt = fulfillment.updated_at;
                    shopifyOrderFulfillment.TrackingCompany = fulfillment.tracking_company;
                    shopifyOrderFulfillment.ShipmentStatus = fulfillment.shipment_status;
                    shopifyOrderFulfillment.LocationId = fulfillment.location_id;
                    shopifyOrderFulfillment.TrackingNumber = fulfillment.tracking_number;
                    shopifyOrderFulfillment.TrackingUrl = fulfillment.tracking_url;
                    shopifyOrderFulfillments.Add(shopifyOrderFulfillment);
                }
                shopifyOrder.Fulfillments = shopifyOrderFulfillments;
            }

            return shopifyOrder;
        }

        public static List<ShopifyOrder> ReadShopifyOrders(string shopifyDomain, string shopifyAPIKey, string shopifyAPIPassword, int totalShopifyOrders, string querySuffix)
        {
            // https://ecommerce.shopify.com/c/shopify-apis-and-technology/t/paginate-api-results-113066
            // Use the /admin/products/count.json to get the count of all products. 
            // Then divide that number by 250 to get the total amount of pages.

            // the web api requires a pagination to read in all orders
            // max orders per page is 250

            var shopifyOrders = new List<ShopifyOrder>();

            int limit = 250;
            if (totalShopifyOrders > 0)
            {
                int numPages = (int)Math.Ceiling((double)totalShopifyOrders / limit);
                for (int i = 1; i <= numPages; i++)
                {
                    ReadShopifyOrdersPage(shopifyOrders, shopifyDomain, shopifyAPIKey, shopifyAPIPassword, limit, i, querySuffix);
                }
            }

            return shopifyOrders;
        }

        public static List<ShopifyOrder> ReadShopifyOrders(string shopifyDomain, string shopifyAPIKey, string shopifyAPIPassword, string querySuffix = "status=any")
        {
            int totalShopifyOrders = CountShopifyOrders(shopifyDomain, shopifyAPIKey, shopifyAPIPassword, querySuffix);
            return ReadShopifyOrders(shopifyDomain, shopifyAPIKey, shopifyAPIPassword, totalShopifyOrders, querySuffix);
        }

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
