using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace WazaloOrdering.DataStore
{
    public interface IMyConfiguration
    {
        IConfiguration Configuration { get; set; }

        string GetString(string key);

        int GetInt(string key);
    }
}