using Ade.Club51.Case.Details.Models;
using Amazon.Runtime.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ade.Club51.Case.Details.Interface
{
    public interface ICaseDetailsService
    {
        Task<ResponseData> UpdateContractDetail(RequestData input);
    }
}
