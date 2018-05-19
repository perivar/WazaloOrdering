using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using WazaloOrdering.DataStore;
using ShopifySharp;

namespace WazaloOrdering.Client.Models
{
    public class OrdersViewModel
    {
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime DateStart { get; set; }

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime DateEnd { get; set; }

        public string Filter { get; set; }

        public IEnumerable<Order> ShopifyOrders { get; set; }

        public List<SelectListItem> StatusList { get; set; }
        public string StatusId { get; set; }

        public List<SelectListItem> FulfillmentStatusList { get; set; }
        public string FulfillmentStatusId { get; set; }

        public List<SelectListItem> FinancialStatusList { get; set; }
        public string FinancialStatusId { get; set; }
    }
}