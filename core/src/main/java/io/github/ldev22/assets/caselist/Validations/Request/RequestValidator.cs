using Ade.Club51.Case.List.Models.Request;
using Amazon.Runtime.Internal;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ade.Club51.Case.List.Validations.Request
{
    public class RequestValidator : AbstractValidator<RequestData>
    {
        public RequestValidator()
        {
            //RuleFor(request => request.OrgCodeFilter)
            //    .NotEmpty()
            //    .WithMessage("OrgCodeFilter cannot be null or empty");
        }

    }
}
