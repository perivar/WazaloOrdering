using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using WazaloOrdering.DataStore;
using System.ComponentModel.DataAnnotations;

namespace WazaloOrdering.Client.Models
{
    public class OrdersViewModel
    {
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime DateStart { get; set; }

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime DateEnd { get; set; }

        public string Filter { get; set; }

        public List<ShopifyOrder> ShopifyOrders { get; set; }

        public List<SelectListItem> StatusList { get; set; }
        public int[] StatusIds { get; set; }

        public List<SelectListItem> FulfillmentStatusList { get; set; }
        public int[] FulfillmentStatusIds { get; set; }

        public List<SelectListItem> FinancialStatusList { get; set; }
        public int[] FinancialStatusIds { get; set; }
    }
}