using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ade.Club51.Lambda.Contract.Update.Models
{
    public class RequestData
    {
        public string CaseId { get; set; }

        public ContactDetail ContactDetail { get; set; }

        public Submission Submission { get; set; }
    }

    public class ContactDetail
    {
        public string ContractDetailId { get; set; }
        public string ContractNumber { get; set; }
        public string ProductName { get; set; }
        public string InvestType { get; set; }
        public string InvestAmount { get; set; }
        public string PayMethod { get; set; }
        public string PayFrequency { get; set; }
        public string CommAllowance { get; set; }
        public string FpFee { get; set; }
        public string NegCommAllowance { get; set; }
        public string NegCommPercentage { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
    }

    public class Submission
    {
        public string CaseCount { get; set; }
        public string Adviser { get; set; }
        public string CaseNumber { get; set; }
        public string CaseStatusId { get; set; }
        public string CommAllowance { get; set; }
        public string ContractNumber { get; set; }
        public string CreatedBy { get; set; }
        public string FigPercentage { get; set; }
        public string FpFee { get; set; }
        public string InvestmentAmount { get; set; }
        public string ModifiedBy { get; set; }
        public string NegCommAllowance { get; set; }
        public string New51ClubsubmissionId { get; set; }
        public string PremiumId { get; set; }
        public string ProductCode { get; set; }
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string SalesCode { get; set; }
        public string SplitCommissionId { get; set; }
        public string StatusReason { get; set; }
        public string Team { get; set; }
    }

}
