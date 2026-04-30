using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ade.Lambda.club51.Case.Create.Update.Models
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class CaseResponse
    {
        public CaseData Data { get; set; }
        public bool IsValid { get; set; }
        public int StatusCode { get; set; }
        public List<string> Messages { get; set; }
        public List<string> Errors { get; set; }
    }
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class CaseData
    {
        public string CaseId { get; set; } 
        public string Result { get; set; } 
    }

}
