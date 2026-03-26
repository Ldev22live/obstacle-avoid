using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ade.Club51.Case.List.Abstractions
{
    public interface ISnowflakeConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
