using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace WazaloOrdering.DataStore
{
    public class MyConfiguration : IMyConfiguration
    {
        private IConfiguration configuration; 

        public IConfiguration Configuration() {
            return configuration;
        }
        public string GetString(string key) {
            return configuration[key];
        }

        public int GetInt(string key) {
            return int.Parse(configuration[key]);
        }

        public MyConfiguration()
        {
            var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.development.json", optional: true)
                .AddUserSecrets<MyConfiguration>()
                .AddEnvironmentVariables();

            configuration = configurationBuilder.Build();
        }
    }
}