using System;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace WazaloOrdering.Client.Models
{
    public class PurchaseOrderLineItem
    {
        public long ID { get; set; }
        public string OrderID { get; set; }
        public string SKU { get; set; }
        public int Quantity { get; set; }
        public decimal PriceUSD { get; set; }
        public string Price { get; set; }
        public string Name { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string Country { get; set; }
        public string ZipCode { get; set; }
        public string Telephone { get; set; }
        public string Remarks { get; set; }
        public string BuyerName { get; set; }
        public string BuyerEmail { get; set; }
    }

    public class PurchaseOrderLineItemMap : ClassMap<PurchaseOrderLineItem>
    {
        public PurchaseOrderLineItemMap()
        {
            Map(m => m.OrderID).Name("Order ID");
            Map(m => m.SKU).Name("SKU");
            Map(m => m.Quantity).Name("Quantity");
            Map(m => m.PriceUSD).Name("Price USD");
            Map(m => m.Price).Name("Price");
            Map(m => m.Name).Name("Name");
            Map(m => m.Address1).Name("Address 1");
            Map(m => m.Address2).Name("Address 2");
            Map(m => m.City).Name("City");
            Map(m => m.Province).Name("Province");
            Map(m => m.Country).Name("Country");
            Map(m => m.ZipCode).Name("Zip Code");
            Map(m => m.Telephone).Name("Telephone");
            Map(m => m.Remarks).Name("Remarks");
            Map(m => m.BuyerName).Name("Buyer Name");
            Map(m => m.BuyerEmail).Name("Buyer Email");
        }
    }

    // see https://stackoverflow.com/questions/49049123/csvhelper-writing-null-strings-as-special-string
    // .TypeConverter<CustomNullTypeConverter<string>>();
    public class CustomNullTypeConverter<T> : DefaultTypeConverter
    {
        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            if (value == null)
            {
                return "#NULL#";
            }

            var converter = row.Configuration.TypeConverterCache.GetConverter<T>();
            return converter.ConvertToString(value, row, memberMapData);
        }
    }

    // .TypeConverter<CustomTextTypeConverter<string>>();
    public class CustomTextTypeConverter<T> : DefaultTypeConverter
    {
        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            return string.Format("'{0}", value);
        }
    }
}