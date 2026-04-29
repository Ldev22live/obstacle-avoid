using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ade.Club51.Case.List.Validations.Response
{

    public class GenericValidatableResponse<T> : ValidatableResponse
    {
        public T Data { get; set; }

        public int Total { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public GenericValidatableResponse(T data)
        {
            Data = data;
        }
    }
}
