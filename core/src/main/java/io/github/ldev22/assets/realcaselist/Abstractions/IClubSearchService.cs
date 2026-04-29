using Ade.Club51.Case.List.Models.Request;
using Ade.Club51.Case.List.Models.Response;
using Ade.Club51.Case.List.Validations.Response;
using Amazon.Runtime.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ade.Club51.Case.List.Abstractions
{
    public interface IClubSearchService
    {
        Task<GenericValidatableResponse<List<ClientResponse>>> GetCaseList(RequestData inputData);
    }
}
