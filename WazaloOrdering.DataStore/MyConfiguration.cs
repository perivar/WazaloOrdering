using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace WazaloOrdering.DataStore
{
    public class MyConfiguration : IMyConfiguration
    {
        public IConfiguration Configuration { get; set; }
        
        public MyConfiguration(IConfiguration appConfig)
        {
            Configuration = appConfig;
        }

        public string GetString(string key) {
            return Configuration[key];
        }

        public int GetInt(string key) {
            return int.Parse(Configuration[key]);
        }
    }
}