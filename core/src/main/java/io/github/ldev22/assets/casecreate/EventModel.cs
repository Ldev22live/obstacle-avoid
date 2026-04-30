using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Ade.Lambda.club51.Case.Create.Update.Models
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class EventModel
    {
        public string CaseId { get; set; } = string.Empty;
        public Detail Detail { get; set; } = new Detail();
        public string EventType { get; set; } = string.Empty;
    }
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class Detail
    {
        public CaseDetails CaseDetails { get; set; } = new CaseDetails();
        public List<ContractDetail> ContractDetails { get; set; } = new List<ContractDetail>();
        public List<ExceptionElement> Exceptions { get; set; } = new List<ExceptionElement>();
        public List<FinancialInformation> FinancialInformation { get; set; } = new List<FinancialInformation>();
    }
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class CaseDetails
    {
        public string AssignedTo { get; set; } = string.Empty;
        public string ClientDateOfBirth { get; set; } = string.Empty;
        public string ClientInitials { get; set; } = string.Empty;
        public string ClientLastName { get; set; } = string.Empty;
        public string ClientType { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ContractNumber { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedOn { get; set; } = string.Empty;
        public string InitiatedOnDate { get; set; } = string.Empty;
        public string LastModified { get; set; } = string.Empty;
        public string LastModifiedBy { get; set; } = string.Empty;
        public string LastRunDate { get; set; } = string.Empty;
        public string LastUpdated { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string SplitCommission { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string TeamOwner { get; set; } = string.Empty;
        public string TeamOwnerCode { get; set; } = string.Empty;
    }
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ContractDetail
    {
        public string CommAllowance { get; set; } = string.Empty;
        public string ContractNumber { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedOn { get; set; } = string.Empty;

        private string? _fpFee;

        public string? FpFee
        {
            get => string.IsNullOrWhiteSpace(_fpFee) ? null : _fpFee;
            set => _fpFee = value;
        }

        public string InvestAmount { get; set; } = string.Empty;
        public string InvestType { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = string.Empty;
        public string ModifiedOn { get; set; } = string.Empty;
        public string NegCommAllowance { get; set; } = string.Empty;
        public string NegCommPercentage { get; set; } = string.Empty;
        public string ContractDetailId { get; set; } = string.Empty;
        public string PayFrequency { get; set; } = string.Empty;
        public string PayMethod { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
    }
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ExceptionElement
    {
        public string NewBusinessSubmissionLogId { get; set; } = string.Empty;
        public string IncidentId { get; set; } = string.Empty;
        public string ContractNumber { get; set; } = string.Empty;
        public string SalesCode { get; set; } = string.Empty;
        public string AdviserName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string InvestAmount { get; set; } = string.Empty;
        public string CommAllowance { get; set; } = string.Empty;
        public string CommPercentage { get; set; } = string.Empty;
        public string FigPercentage { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedOn { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = string.Empty;
        public string ModifiedOn { get; set; } = string.Empty;
    }

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class FinancialInformation
    {
        public string AdviserName { get; set; } = string.Empty;
        public string AdviserStatus { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string CommPercentage { get; set; } = string.Empty;
        public string ContractNumber { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public string CreatedOn { get; set; } = string.Empty;
        public string FigPercentage { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string IsPrimary { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = string.Empty;
        public string ModifiedOn { get; set; } = string.Empty;
        public string NegCommPercentage { get; set; } = string.Empty;
        public string FinancialInfoId { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string SalesCode { get; set; } = string.Empty;
        public string SplitType { get; set; } = string.Empty;
        public string Team { get; set; } = string.Empty;
    }
}
