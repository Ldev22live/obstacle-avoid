using Ade.Lambda.club51.Case.Create.Update.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ade.Lambda.club51.Case.Create.Update.Service
{
    public class InputValidator
    {
        public static CaseResponse ValidateInput(EventModel input)
        {
            var response = new CaseResponse
            {
                Messages = new List<string>(),
                Errors = new List<string>()
            };

            // Validate input is not null
            if (input == null)
            {
                response.Data = new CaseData { };
                response.IsValid = false;
                response.StatusCode = 400; // Bad Request
                response.Errors.Add("Input cannot be null.");
                return response;
            }

            // Validate specific properties of the input
            if (string.IsNullOrWhiteSpace(input.EventType))
            {
                response.Data = new CaseData { };
                response.IsValid = false;
                response.StatusCode = 400; // Bad Request
                response.Errors.Add("EventType cannot be null or empty.");
                return response;
            }
            if (input.CaseId == null)
            {
                response.Data = new CaseData { };
                response.IsValid = false;
                response.StatusCode = 400; // Bad Request
                response.Errors.Add("case Id cannot be null.");
                return response;
            }
            if (input.Detail == null)
            {
                response.Data = new CaseData { };
                response.IsValid = false;
                response.StatusCode = 400; // Bad Request
                response.Errors.Add("Detail cannot be null.");
                return response;
            }

            // Validate CaseDetails
            if (input.Detail.CaseDetails == null)
            {
                response.Data = null;
                response.IsValid = false;
                response.StatusCode = 400; // Bad Request
                response.Errors.Add("CaseDetails cannot be null.");
                return response;
            }
            if (input.Detail.FinancialInformation == null)
            {
                response.Data = new CaseData { };
                response.IsValid = false;
                response.StatusCode = 400; // Bad Request
                response.Errors.Add("Financial Information cannot be null.");
                return response;
            }

            if (string.IsNullOrWhiteSpace(input.Detail.CaseDetails.ContractNumber))
            {
                response.Data = new CaseData { };
                response.IsValid = false;
                response.StatusCode = 400; // Bad Request
                response.Errors.Add("IncidentId cannot be null or empty.");
                return response;
            }

       

            response.IsValid = true;
            return response;
        }
    }
}
