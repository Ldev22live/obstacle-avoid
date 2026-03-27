using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace Ade.Club51.Lambda.Contract.Update.Models
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ResponseData
    {
        public Data Data { get; set; } = new Data();
        public bool IsValid { get; set; } = false;
        public long StatusCode { get; set; } = 400;
        public List<string> Messages { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();

    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class Data
    {
        public string NewContractDetailId { get; set; } = string.Empty;
        public string NewCaseId { get; set; } = string.Empty;
    }
}