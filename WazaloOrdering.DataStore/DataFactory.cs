using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

        public static Order GetShopifyOrder(IConfiguration appConfig, string orderName)
        {
            try
            {
                orderName = Regex.Match( orderName, @"\d+" ).Value;
                return Shopify.GetShopifyOrder(appConfig, orderName).GetAwaiter().GetResult();
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

        public static Order SetOrderNoteAttributes(IConfiguration appConfig, long orderId, Dictionary<string, string> dic)
        {
            try
            {
                return Shopify.SetOrderNoteAttributes(appConfig, orderId, dic).GetAwaiter().GetResult();
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        public static Order SetOrderNoteAttributes(IConfiguration appConfig, long orderId, IEnumerable<NoteAttribute> noteAttributes)
        {
            try
            {
                return Shopify.SetOrderNoteAttributes(appConfig, orderId, noteAttributes).GetAwaiter().GetResult();
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        public static Fulfillment FulfillOrder(IConfiguration appConfig, long orderId, string trackingNumber, bool notifyCustomer)
        {
            try
            {
                return Shopify.FulfillOrder(appConfig, orderId, trackingNumber, notifyCustomer).GetAwaiter().GetResult();
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        public static Fulfillment FulfillOrderPartially(IConfiguration appConfig, long orderId, long lineItemId, int lineItemQuantity, string trackingNumber, bool notifyCustomer)
        {
            try
            {
                return Shopify.FulfillOrderPartially(appConfig, orderId, lineItemId, lineItemQuantity, trackingNumber, notifyCustomer).GetAwaiter().GetResult();
            }
            catch (System.Exception)
            {
                return null;
            }
        }

    }
}
