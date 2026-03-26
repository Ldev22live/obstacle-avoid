using Ade.Club51.Case.Details.Config;
using Ade.Club51.Case.Details.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ade.Club51.Case.Details.Interface
{
    public interface IQueryBuilder
    {
        QuerySet BuildAllQueries(DatabaseConfig config, string caseId);
    }
}
