using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Ade.Club51.Case.List.Models.Response
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ClientResponse
    {
        [JsonPropertyName("CASEID")]
        public string CaseId { get; set; }

        [JsonPropertyName("CASESOURCE")]
        public string Source { get; set; }

        [JsonPropertyName("CONTRACTNUMBER")]
        public string ContractNumber { get; set; }

        [JsonPropertyName("CUSTOMERLASTNAME")]
        public string CustomerLastName { get; set; }

        [JsonPropertyName("CUSTOMERINITIALS")]
        public string CustomerInitials { get; set; }

        [JsonPropertyName("COMPANYNAME")]
        public string CompanyName { get; set; }

        [JsonPropertyName("SALESCODE")]
        public string SalesCode { get; set; }

        [JsonPropertyName("ADVISERID")]
        public string AdviserId { get; set; }

        [JsonPropertyName("ADVISERUSERNAME")]
        public string adviserName { get; set; }

        [JsonPropertyName("FIRSTNAME")]
        public string FirstName { get; set; }

        [JsonPropertyName("ISSPLITCOMMISSION")]
        public int IsSplitCommission { get; set; }

        [JsonPropertyName("CASESTATEID")]
        public int CaseStateId { get; set; }

        [JsonPropertyName("PRODUCTCODE")]
        public string ProductCode { get; set; }

        [JsonPropertyName("PRODUCTNAME")]
        public string ProductName { get; set; }

        [JsonPropertyName("OWNERID")]
        public string OwnerId { get; set; }

        [JsonPropertyName("OWNERNAME")]
        public string OwnerName { get; set; }

        [JsonPropertyName("ADVISERTEAMNAME")]

        public string AdviserTeamName { get; set; }

        [JsonPropertyName("INITIATEDON")]

        public DateTime InitiatedOn { get; set; }

        [JsonPropertyName("CREATEDON")]
        public DateTime CreatedOn { get; set; }


        [JsonPropertyName("MODIFIEDON")]
        public DateTime ModifiedOn { get; set; }

        [JsonPropertyName("STATUS")]
        public int Status { get; set; }

        [JsonPropertyName("REGION")]
        public string Region { get; set; }

        [JsonPropertyName("AREA")]
        public string Area { get; set; }

    }
}