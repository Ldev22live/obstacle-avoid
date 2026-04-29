using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ade.Club51.Case.List.Models.Request
{
    public class RequestData
    {
        public int PageNum { get; set; }
        public int PageSize { get; set; }
        public string Keyword { get; set; }
        public string KeywordType { get; set; }
        public string OrderByColumn { get; set; }
        public string Order { get; set; }
        public List<string> OrgCodeFilter { get; set; }
        public List<string> ViewFilter { get; set; }
        public List<string> AccessControlCriteria { get; set; }
    }
}
