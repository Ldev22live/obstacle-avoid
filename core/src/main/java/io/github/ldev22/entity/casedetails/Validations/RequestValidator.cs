using Ade.Club51.Case.Details.Interface;
using Ade.Club51.Case.Details.Models;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ade.Club51.Case.Details.Validations
{
    public class RequestValidator : IRequestValidator
    {

        public ValidationResult Validate(RequestData input)
        {
            LambdaLogger.Log("INFO: Validation started.");
            var errors = new List<string>();

            if (input == null)
            {
                errors.Add("Request cannot be null.");
                LambdaLogger.Log("ERROR: Validation failed - RequestData is null.");
            }
            else
            {
                LambdaLogger.Log($"INFO: Validating CaseId: {input.CaseId}");

                if (string.IsNullOrWhiteSpace(input.CaseId))
                {
                    errors.Add("CaseId is required.");
                    LambdaLogger.Log("ERROR: Validation failed - CaseId is missing or empty.");
                }

                if (input.ContactDetail == null)
                {
                    errors.Add("ContactDetail is required.");
                    LambdaLogger.Log("ERROR: Validation failed - ContactDetail is missing.");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(input.ContactDetail.ContractNumber))
                    {
                        errors.Add("ContactDetail.ContractNumber is required.");
                        LambdaLogger.Log("ERROR: Validation failed - ContractNumber is missing or empty.");
                    }

                    if (string.IsNullOrWhiteSpace(input.ContactDetail.ProductName))
                    {
                        errors.Add("ContactDetail.ProductName is required.");
                        LambdaLogger.Log("ERROR: Validation failed - ProductName is missing or empty.");
                    }

                    if (string.IsNullOrWhiteSpace(input.ContactDetail.CreatedBy))
                    {
                        errors.Add("ContactDetail.CreatedBy is required.");
                        LambdaLogger.Log("ERROR: Validation failed - CreatedBy is missing or empty.");
                    }
                }
               
            }

            var validationResult = new ValidationResult(errors);

            if (errors.Count > 0)
            {
                LambdaLogger.Log($"ERROR: Validation completed with {errors.Count} errors: {JsonConvert.SerializeObject(errors)}");
            }
            else
            {
                LambdaLogger.Log("INFO: Validation passed successfully.");
            }

            return validationResult;
        }


    }
}
