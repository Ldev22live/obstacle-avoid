using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace Ade.Club51.Case.Details.Models
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public  class ResponseData
    {
        public Data Data { get; set; } = new Data();
    }
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public  class Data
    {
        public string NewContractDetailId { get; set; } = string.Empty;
    }
}
