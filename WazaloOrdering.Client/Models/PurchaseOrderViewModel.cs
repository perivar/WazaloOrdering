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

        [Display(Name = "To")]
        public string EmailTo { get; set; }

        [Display(Name = "CC")]
        public string EmailCC { get; set; }

        [Display(Name = "Body")]
        public string EmailBody { get; set; }

        public List<PurchaseOrder> PurchaseOrders { get; set; }
    }
}