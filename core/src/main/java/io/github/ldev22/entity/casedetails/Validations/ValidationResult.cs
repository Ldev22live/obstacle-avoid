using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ade.Club51.Case.Details.Validations
{
    public class ValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors { get; }

        public ValidationResult(List<string> errors)
        {
            Errors = errors ?? new List<string>();
        }
    }
}
