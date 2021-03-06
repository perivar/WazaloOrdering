using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using WazaloOrdering.DataStore;

namespace WazaloOrdering.Client.Models
{
    public class PurchaseOrderViewModel
    {
        [Display(Name = "Id")]
        public long OrderId { get; set; }

        [Display(Name = "Order Name")]
        public string OrderName { get; set; }

        [Display(Name = "To")]
        public string EmailTo { get; set; }

        [Display(Name = "CC")]
        public string EmailCC { get; set; }

        [Display(Name = "Body")]
        public string EmailBody { get; set; }

        [Display(Name = "Total Cost")]
        public decimal TotalCost { get; set; }

        public List<PurchaseOrderLineItem> PurchaseOrderLineItems { get; set; }
    }
}