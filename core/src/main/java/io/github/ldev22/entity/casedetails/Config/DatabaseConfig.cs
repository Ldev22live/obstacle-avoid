using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ade.Club51.Case.Details.Config
{
    public class DatabaseConfig
    {
        public string DbEdrAccount { get; }
        public string DbEdrServer { get; }
        public string DbEdrDb { get; }
        public string DbEdrUser { get; }
        public string DbEdrPassword { get; }
        public string DbEdrSchemaClun51 { get; }
        public string DbEdrPort { get; }
        public string DbEdrTableCase { get; }
        public string DbEdrTableFinancialInformation { get; }
        public string DbEdrTableContractDetail { get; }
        public string DbEdrTableExceptionExclusionLog { get; }
        public string DbEdrTableNotes { get; }
        public string DbEdrProcViewed { get; }
        public string DbEdrProcContractDetailUpdate { get; }

        public DatabaseConfig() {
            DbEdrAccount = Environment.GetEnvironmentVariable("DB_EDR_ACCOUNT")
                  ?? throw new ArgumentNullException("DB_EDR_ACCOUNT environment variable is not set.");
            DbEdrServer = Environment.GetEnvironmentVariable("DB_EDR_SERVER")
                ?? throw new ArgumentNullException("DB_EDR_SERVER environment variable is not set.");
            DbEdrDb = Environment.GetEnvironmentVariable("DB_EDR_DB")
                ?? throw new ArgumentNullException("DB_EDR_DB environment variable is not set.");
            DbEdrUser = Environment.GetEnvironmentVariable("DB_EDR_USER")
                ?? throw new ArgumentNullException("DB_EDR_USER environment variable is not set.");
            DbEdrPassword = Environment.GetEnvironmentVariable("DB_EDR_PASSWORD")
                ?? throw new ArgumentNullException("DB_EDR_PASSWORD environment variable is not set.");
            DbEdrPort = Environment.GetEnvironmentVariable("DB_EDR_PORT")
              ?? throw new ArgumentNullException("DB_EDR_PORT environment variable is not set.");
            DbEdrSchemaClun51 = Environment.GetEnvironmentVariable("DB_EDR_SCHEMA_CLUB51") ??
                throw new ArgumentNullException("DB_EDR_SCHEMA_CLUB51 environment variable is not set.");
            DbEdrTableCase = Environment.GetEnvironmentVariable("DB_EDR_TABLE_CASE") ?? 
                throw new ArgumentNullException("DB_EDR_TABLE_CASE environment variable is not set.");
            DbEdrTableFinancialInformation = Environment.GetEnvironmentVariable("DB_EDR_TABLE_FINANCIALINFORMATION") ??
                throw new ArgumentNullException("DB_EDR_TABLE_FINANCIALINFORMATION environment variable is not set.");
            DbEdrTableContractDetail = Environment.GetEnvironmentVariable("DB_EDR_TABLE_CONTRACTDETAIL") ??
                throw new ArgumentNullException("DB_EDR_TABLE_CONTRACTDETAIL environment variable is not set.");
            DbEdrTableExceptionExclusionLog = Environment.GetEnvironmentVariable("DB_EDR_TABLE_EXCEPTIONEXCLUSIONLOG") ??
                throw new ArgumentNullException("DB_EDR_TABLE_EXCEPTIONEXCLUSIONLOG environment variable is not set.");
            DbEdrTableNotes = Environment.GetEnvironmentVariable("DB_EDR_TABLE_NOTES") ??
                throw new ArgumentNullException("DB_EDR_TABLE_NOTES environment variable is not set.");
            DbEdrProcViewed = Environment.GetEnvironmentVariable("DB_EDR_PROC_SP_LOG_RECENTLY_VIEWED_CASE") ??
               throw new ArgumentNullException("DB_EDR_PROC_SP_LOG_RECENTLY_VIEWED_CASE environment variable is not set.");

            DbEdrProcContractDetailUpdate = Environment.GetEnvironmentVariable("DB_EDR_PROC_SP_CONTRACT_DETAIL_UPDATE") ??
               throw new ArgumentNullException("DB_EDR_PROC_SP_CONTRACT_DETAIL_UPDATE environment variable is not set.");
        } 
    }

}
