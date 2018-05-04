using System;
using System.Collections.Generic;

namespace WazaloOrdering.DataStore
{
    public class ShopifyOrder
    {
        public long Id { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime ProcessedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime CancelledAt { get; set; }

        public string Name { get; set; }
        public string FinancialStatus { get; set; }
        public string FulfillmentStatus { get; set; }

        public string Gateway { get; set; }
        public string PaymentId { get; set; }

        public decimal TotalPrice { get; set; }
        public decimal TotalTax { get; set; }

        public string CustomerEmail { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerAddress2 { get; set; }
        public string CustomerZipCode { get; set; }
        public string CustomerCity { get; set; }

        public string Note { get; set; }

        // Fulfillments
        public List<ShopifyOrderFulfillment> Fulfillments { get; set; }

        // Line Items
        public List<ShopifyOrderLineItem> LineItems { get; set; }

        public bool Cancelled
        {
            get
            {
                return (CancelledAt != default(DateTime) ? true : false);
            }
        }

        public override string ToString()
        {
            return string.Format("{0} {1:dd-MM-yyyy} {2} {3} {4} {5} {6:C} {7}", Id, CreatedAt, Name, FinancialStatus, FulfillmentStatus, Gateway, TotalPrice, CustomerName);
        }
    }

    public class ShopifyOrderLineItem
    {
        public int FulfillableQuantity { get; set; }
        public string FulfillmentService { get; set; }
        public string FulfillmentStatus { get; set; }
        public int Grams { get; set; }
        public string Id { get; set; }
        public decimal Price { get; set; }
        public string ProductId { get; set; }
        public int Quantity { get; set; }
        public bool RequiresShipping { get; set; }
        public string Sku { get; set; }
        public string Title { get; set; }
        public string VariantId { get; set; }
        public string VariantTitle { get; set; }
        public string Vendor { get; set; }
        public string Name { get; set; }
        public bool GiftCard { get; set; }
        public bool Taxable { get; set; }
        public decimal TotalDiscount { get; set; }

        // Line Items
        public List<ShopifyProductImage> ProductImages { get; set; }
    }

    public class ShopifyOrderFulfillment
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Service { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string TrackingCompany { get; set; }
        public string ShipmentStatus { get; set; }
        public string LocationId { get; set; }
        public string TrackingNumber { get; set; }
        //public string TrackingNumbers { get; set; }
        public string TrackingUrl { get; set; }
        //public string TrackingUrls { get; set; }
        //public string Receipt { get; set; }: {},
    }

    public class ShopifyProductImage
    {
        public string Id { get; set; }
        public string ProductId { get; set; }
        public int Position { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string Alt { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string Src { get; set; }

    }
}
