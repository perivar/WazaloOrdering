using System;
using Xunit;
using WazaloOrdering.DataStore;
using Serilog;

namespace WazaloOrdering.DataStoreTest
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("consoleapp.log")
                .CreateLogger();

            var orders = DataFactory.GetShopifyOrders();
            foreach (var order in orders)
            {
                Log.Information(order.ToString());
            }
        }
    }
}
