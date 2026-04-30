using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ade.Lambda.club51.Case.Create.Update.Config
{
    public class DatabaseConfig
    {
        public string Account { get; }
        public string Server { get; }
        public string Database { get; }
        public string UserId { get; }
        public string Password { get; }
        public string Schema { get; }
        public string Warehouse { get; }
        public string Port { get; }
        public string CaseDetailsTable { get; }
        public string FinancialInfoTable { get; }
        public string ContractDetailsTable { get; }
        public string ExceptionsTable { get; }

        public DatabaseConfig()
        {
            Account = Environment.GetEnvironmentVariable("edr_account")
               ?? throw new ArgumentNullException("edr_account environment variable is not set.");
            Server = Environment.GetEnvironmentVariable("edr_server")
                ?? throw new ArgumentNullException("edr_server environment variable is not set.");
            Database = Environment.GetEnvironmentVariable("edr_db")
                ?? throw new ArgumentNullException("edr_db environment variable is not set.");
            UserId = Environment.GetEnvironmentVariable("edr_UserId")
                ?? throw new ArgumentNullException("edr_UserId environment variable is not set.");
            Password = Environment.GetEnvironmentVariable("edr_Password")
                ?? throw new ArgumentNullException("edr_Password environment variable is not set.");
            Port = Environment.GetEnvironmentVariable("edr_port")
              ?? throw new ArgumentNullException("Port environment variable is not set.");
            Schema = Environment.GetEnvironmentVariable("edr_schema") ?? "public"; // Optional schema with default fallback
            Warehouse = Environment.GetEnvironmentVariable("edr_warehouse") ?? "COMPUTE_WH"; // Optional warehouse with default fallback
            CaseDetailsTable = Environment.GetEnvironmentVariable("table_case_details") ?? "CaseDetails";
            FinancialInfoTable = Environment.GetEnvironmentVariable("table_financial_info") ?? "FinancialInformation";
            ContractDetailsTable = Environment.GetEnvironmentVariable("table_contract_details") ?? "ContractDetails";
            ExceptionsTable = Environment.GetEnvironmentVariable("table_exceptions") ?? "Exceptions";
        }
    }

}
