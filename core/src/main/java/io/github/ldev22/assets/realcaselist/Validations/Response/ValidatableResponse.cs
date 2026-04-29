using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Ade.Club51.Case.List.Helpers;

namespace Ade.Club51.Case.List.Validations.Response
{
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class ValidatableResponse
    {
        public bool IsValid { get { return !Errors.Any(); } }
        public HttpStatusCode StatusCode { get; set; }
        public List<string> Messages { get; set; }
        public List<string> Errors { get; set; }

        public ValidatableResponse()
        {
            Messages = new List<string>();
            Errors = new List<string>();
        }

        public void ClearMessages()
        {
            Messages.Clear();
        }

        public void AddError(string error)
        {
            if (error.HasValue())
                Errors.Add(error);
        }

        public void AddMessage(string message)
        {
            if (message.HasValue())
                Messages.Add(message);
        }

        public void SetStatusCode()
        {
            if (IsValid)
            {
                StatusCode = HttpStatusCode.OK;
            }
            else
            {
                StatusCode = HttpStatusCode.BadRequest;
            }
        }

        public void MergeResponses(ValidatableResponse validatableResponse)
        {
            if (validatableResponse != null)
            {
                validatableResponse.Errors.ForEach(error =>
                {
                    AddError(error);
                });

                validatableResponse.Messages.ForEach(message =>
                {
                    AddMessage(message);
                });
            }
        }

        public T MergeResponsesAndReturnEntity<T>(GenericValidatableResponse<T> validatableResponse)
        {
            var response = default(T);

            MergeResponses((ValidatableResponse)validatableResponse);

            if (IsValid)
                response = validatableResponse.Data;

            return response;
        }
    }
}
