using Ade.Club51.Case.List.Abstractions;
using Ade.Club51.Case.List.Models.Request;
using Ade.Club51.Case.List.Models.Response;
using Ade.Club51.Case.List.Validations.Response;
using Amazon.Lambda.Core;
using Amazon.Runtime.Internal;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Ade.Club51.Case.List.Services
{
    public class ClubSearchService : IClubSearchService
    {

        private readonly ISnowflakeConnectionFactory _connectionFactory;

        private readonly IDbConnection _connection;

        private static String _dbName;
        private static String _schemaName;
        public ClubSearchService(ISnowflakeConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new NullReferenceException(typeof(ISnowflakeConnectionFactory).Name);
            _connection = _connectionFactory.CreateConnection();
            _connection.Open();
            _dbName = _connection.Database;
            _schemaName = "CLUB51";
            LambdaLogger.Log($"Dbname{_dbName}");
        }
        public async Task<GenericValidatableResponse<List<ClientResponse>>> GetCaseList(RequestData input)
        {
            var response = new GenericValidatableResponse<List<ClientResponse>>(new List<ClientResponse>());
            try
            {
                response.Total = GetTotalRecords(input);
                response.Data = await GetSearchData(input);
            }
            catch (Exception exception)
            {
                exception = exception.InnerException ?? exception;
                response.AddError($"An error has occurred. Error: {exception.Message}");
                LambdaLogger.Log("ERROR: " + JsonConvert.SerializeObject(exception));
            }
            return response;
        }

        public int GetTotalRecords(RequestData requestData)
        {
            int totalRecords = 0;
            try
            {
                if (requestData.OrgCodeFilter != null &&  requestData.OrgCodeFilter.Count > 0)
                {
                    using var command = _connection.CreateCommand();
                    // Get records without pagination
                    var sqlQuery =  CaseListQuery(requestData).ToString();
                    //CountQuery(requestData);

                    command.CommandText = sqlQuery;

                    totalRecords = ReturnTotalRecords(command);
                }
            }
            catch (Exception exception)
            {
                var innerException = exception.InnerException ?? exception;
                LambdaLogger.Log($"ERROR: {JsonConvert.SerializeObject(innerException)}");
            }
            return totalRecords;
        }

        public async Task<List<ClientResponse>> GetSearchData(RequestData request)
        {
            var searchResults = new List<ClientResponse>();
            try
            {
                using var command = _connection.CreateCommand();
                var sqlQuery = string.Empty;
                if (request.KeywordType == "USERCODE")
                    // Get recently viewed cases  
                    sqlQuery = RecentlyViewedCasesQuery(request);
                else
                { 
                    var baseQuery= CaseListQuery(request);
                    baseQuery.AppendLine($" ORDER BY {request.OrderByColumn} {request.Order} LIMIT {request.PageSize} OFFSET {(request.PageNum - 1) * request.PageSize};");
                    sqlQuery= baseQuery.ToString();
                    // Get records with pagination
                }
                Console.WriteLine($"Sql query :{sqlQuery}");

                command.CommandText = sqlQuery;
                // Call the method that returns a list of ClientResponse
                searchResults = ReturnSearchCase(command);
            }
            catch (Exception exception)
            {
                var innerException = exception.InnerException ?? exception;
                LambdaLogger.Log($"ERROR: {JsonConvert.SerializeObject(innerException)}");
            }

            return searchResults;
        }

        //public static string CountQuery(RequestData request)
        //{
        //    var sqlBuilder = new StringBuilder();

        //    sqlBuilder.AppendLine("SELECT DISTINCT COUNT(*)");
        //    sqlBuilder.Append(BuildBaseQuery(request));

        //    return sqlBuilder.ToString();
        //}

        public static StringBuilder CaseListQuery(RequestData request)
        {
            if (string.IsNullOrEmpty(request.OrderByColumn))
                request.OrderByColumn = "CASE_CONTRACTNUMBER";

            if (string.IsNullOrEmpty(request.Order))
                request.Order = "DESC";

            var sqlBuilder = new StringBuilder();

            sqlBuilder.AppendLine("SELECT ");
            sqlBuilder.AppendLine("    m.CASE_ID AS CASEID,");
            sqlBuilder.AppendLine("    m.CASE_SOURCE AS SOURCE,");
            sqlBuilder.AppendLine("    m.CASE_CONTRACTNUMBER AS CONTRACTNUMBER,");
            sqlBuilder.AppendLine("    m.CASE_CUSTOMERLASTNAME AS CUSTOMERLASTNAME,");
            sqlBuilder.AppendLine("    m.CASE_CUSTOMERTINITIALS AS CUSTOMERINITIALS,");
            sqlBuilder.AppendLine("    m.CASE_COMPANYNAME AS COMPANYNAME,");

            // Adviser fields
            sqlBuilder.AppendLine("    COALESCE(f.FI_SALESCODE, '') AS SALESCODE,");
            sqlBuilder.AppendLine("    COALESCE(f.FI_SALESCODE, '') AS ADVISERID,");
            sqlBuilder.AppendLine("    COALESCE(TRIM(COALESCE(f.FI_FIRSTNAME, '') || ' ' || COALESCE(f.FI_LASTNAME, '')), '') AS ADVISERNAME,");
            sqlBuilder.AppendLine("    COALESCE(f.FI_FIRSTNAME, '') AS FIRSTNAME,");
            sqlBuilder.AppendLine("    COALESCE(f.FI_TEAM, '') AS ADVISERTEAMNAME,");

            sqlBuilder.AppendLine("    m.CASE_ISSPLITCOMMISSION AS ISSPLITCOMMISSION,");
            sqlBuilder.AppendLine("    m.CASE_STATE_ID AS CASESTATEID,");
            sqlBuilder.AppendLine("    p.PRODUCT_CODE AS PRODUCTCODE,");
            sqlBuilder.AppendLine("    p.PRODUCT_NAME AS PRODUCTNAME,");
            sqlBuilder.AppendLine("    m.CASE_TEAMOWNER_CODE AS OWNERID,");
            sqlBuilder.AppendLine("    m.CASE_TEAMOWNER AS OWNERNAME,");

            sqlBuilder.AppendLine("    TO_CHAR(m.CASE_INITIATEDONDATE, 'YYYY-MM-DD HH24:MI') AS INITIATEDON,");
            sqlBuilder.AppendLine("    TO_CHAR(m.CASE_CREATEDON, 'YYYY-MM-DD HH24:MI') AS CREATEDON,");
            sqlBuilder.AppendLine("    TO_CHAR(m.CASE_LASTMODIFIED, 'YYYY-MM-DD HH24:MI') AS MODIFIEDON,");

            sqlBuilder.AppendLine("    m.CASE_STATUS_ID AS STATUS,");
            sqlBuilder.AppendLine("    COALESCE(f.FI_REGION, '') AS REGION,");
            sqlBuilder.AppendLine("    COALESCE(f.FI_AREA, '') AS AREA");

            sqlBuilder.Append(BuildBaseQuery(request));

            return sqlBuilder;
        }

        public static string RecentlyViewedCasesQuery(RequestData request)
        {
            var sqlBuilder = new StringBuilder();

            sqlBuilder.AppendLine("WITH RankedCases AS (");
            sqlBuilder.AppendLine("    SELECT");
            sqlBuilder.AppendLine("        m.CASE_ID AS CASEID,");
            sqlBuilder.AppendLine("        m.CASE_SOURCE AS SOURCE,");
            sqlBuilder.AppendLine("        m.CASE_CONTRACTNUMBER AS CONTRACTNUMBER,");
            sqlBuilder.AppendLine("        m.CASE_CUSTOMERLASTNAME AS CUSTOMERLASTNAME,");
            sqlBuilder.AppendLine("        m.CASE_CUSTOMERTINITIALS AS CUSTOMERINITIALS,");
            sqlBuilder.AppendLine("        m.CASE_COMPANYNAME AS COMPANYNAME,");

            // Adviser fields
            sqlBuilder.AppendLine("        COALESCE(f.FI_SALESCODE, '') AS SALESCODE,");
            sqlBuilder.AppendLine("        COALESCE(f.FI_SALESCODE, '') AS ADVISERID,");
            sqlBuilder.AppendLine("        COALESCE(TRIM(COALESCE(f.FI_FIRSTNAME, '') || ' ' || COALESCE(f.FI_LASTNAME, '')), '') AS ADVISERNAME,");
            sqlBuilder.AppendLine("        COALESCE(f.FI_FIRSTNAME, '') AS FIRSTNAME,");
            sqlBuilder.AppendLine("        COALESCE(f.FI_TEAM, '') AS ADVISERTEAMNAME,");

            sqlBuilder.AppendLine("        m.CASE_ISSPLITCOMMISSION AS ISSPLITCOMMISSION,");
            sqlBuilder.AppendLine("        m.CASE_STATE_ID AS CASESTATEID,");
            sqlBuilder.AppendLine("        p.PRODUCT_CODE AS PRODUCTCODE,");
            sqlBuilder.AppendLine("        p.PRODUCT_NAME AS PRODUCTNAME,");
            sqlBuilder.AppendLine("        m.CASE_TEAMOWNER_CODE AS OWNERID,");
            sqlBuilder.AppendLine("        m.CASE_TEAMOWNER AS OWNERNAME,");

            sqlBuilder.AppendLine("        TO_CHAR(m.CASE_INITIATEDONDATE, 'YYYY-MM-DD HH24:MI') AS INITIATEDON,");
            sqlBuilder.AppendLine("        TO_CHAR(m.CASE_CREATEDON, 'YYYY-MM-DD HH24:MI') AS CREATEDON,");
            sqlBuilder.AppendLine("        TO_CHAR(m.CASE_LASTMODIFIED, 'YYYY-MM-DD HH24:MI') AS MODIFIEDON,");

            sqlBuilder.AppendLine("        m.CASE_STATUS_ID AS STATUS,");
            sqlBuilder.AppendLine("        COALESCE(f.FI_REGION, '') AS REGION,");
            sqlBuilder.AppendLine("        COALESCE(f.FI_AREA, '') AS AREA,");

            sqlBuilder.AppendLine("        r.VC_INSERT_DATE AS ViewDate,");
            sqlBuilder.AppendLine("        ROW_NUMBER() OVER (PARTITION BY m.CASE_ID ORDER BY r.VC_INSERT_DATE DESC) AS rn");
            sqlBuilder.AppendLine("    FROM");
            sqlBuilder.AppendLine($"        {_dbName}.{_schemaName}.FIFTYONECLUB_CASE m");
            sqlBuilder.AppendLine($"    INNER JOIN {_dbName}.{_schemaName}.FIFTYONECLUB_FINANCIALINFORMATION f");
            sqlBuilder.AppendLine("        ON f.FI_CASE_ID = m.CASE_ID");
            sqlBuilder.AppendLine("        AND f.FI_ISPRIMARY = 1");
            sqlBuilder.AppendLine("        AND f.FI_ISDELETED = '0'");
            sqlBuilder.AppendLine("        AND f.FI_ENDDATE = '9999-12-31'");
            sqlBuilder.AppendLine($"    INNER JOIN {_dbName}.{_schemaName}.PRODUCT p ON p.PRODUCT_ID = m.CASE_PRODUCT_ID");
            sqlBuilder.AppendLine($"    INNER JOIN {_dbName}.{_schemaName}.FIFTYONECLUB_RECENTLY_VIEWED_CASE r ON r.VC_CASE_ID = m.CASE_ID");
            sqlBuilder.AppendLine("    WHERE m.CASE_ENDDATE = '9999-12-31'");
            sqlBuilder.AppendLine($"      AND r.VC_USER_CODE = '{request.Keyword}'");
            sqlBuilder.AppendLine(")");

            sqlBuilder.AppendLine("SELECT * FROM RankedCases");
            sqlBuilder.AppendLine("WHERE rn = 1");
            sqlBuilder.AppendLine("ORDER BY ViewDate DESC");
            sqlBuilder.AppendLine("LIMIT 10000;");

            return sqlBuilder.ToString();
        }


        //public static string BuildBaseQuery(RequestData request)
        //{
        //    var sql = new StringBuilder();

        //    sql.AppendLine("FROM");
        //    sql.AppendLine($"    {_dbName}.{_schemaName}.FIFTYONECLUB_CASE m");

        //    //// Financial information (PRIMARY adviser only, without killing LEFT JOIN)
        //    //sql.AppendLine($"LEFT JOIN {_dbName}.{_schemaName}.FIFTYONECLUB_FINANCIALINFORMATION f ON");
        //    //sql.AppendLine("    f.FI_CASE_ID = m.CASE_ID");
        //    //sql.AppendLine("    AND f.FI_ISDELETED = '0'");
        //    //sql.AppendLine("    AND f.FI_enddate = '9999-12-31'");
        //    //sql.AppendLine("    AND f.FI_ISPRIMARY = 1");

        //    bool isSearch =! string.IsNullOrWhiteSpace(request.Keyword);

        //    sql.AppendLine($"LEFT JOIN {_dbName}.{_schemaName}.FIFTYONECLUB_FINANCIALINFORMATION f ON");
        //    sql.AppendLine("    f.FI_CASE_ID = m.CASE_ID");
        //    sql.AppendLine("    AND f.FI_ISDELETED = '0'");
        //    sql.AppendLine("    AND f.FI_enddate = '9999-12-31'");

        //    if (!isSearch)
        //    {
        //        sql.AppendLine("    AND f.FI_ISPRIMARY = 1");
        //    }

        //    // Product is mandatory
        //    sql.AppendLine($"INNER JOIN {_dbName}.{_schemaName}.PRODUCT p");
        //    sql.AppendLine("    ON p.PRODUCT_ID = m.CASE_PRODUCT_ID");

        //    // Base condition
        //    sql.AppendLine("WHERE m.case_enddate = '9999-12-31'");

        //    bool isContractSearch =
        //        string.Equals(request.KeywordType, "CONTRACT_NUMBER", StringComparison.OrdinalIgnoreCase);

        //    // --------------------------------------------------
        //    // Org filter (skip for contract number search)
        //    // --------------------------------------------------
        //    if (!isContractSearch &&
        //        request.OrgCodeFilter != null &&
        //        request.OrgCodeFilter.Count > 0)
        //    {
        //        sql.AppendLine(
        //            $" AND m.\"CASE_TEAMOWNER_CODE\" IN ({string.Join(", ", request.OrgCodeFilter.Select(v => $"'{v}'"))})"
        //        );
        //    }

        //    // --------------------------------------------------
        //    // View / Status filter (skip for contract number search)
        //    // --------------------------------------------------
        //    if (!isContractSearch &&
        //        request.ViewFilter != null &&
        //        request.ViewFilter.Count > 0)
        //    {
        //        sql.AppendLine(
        //            $" AND m.\"CASE_STATUS_ID\" IN ({string.Join(", ", request.ViewFilter.Select(v => $"'{v}'"))})"
        //        );
        //    }

        //    // --------------------------------------------------
        //    // Keyword logic
        //    // --------------------------------------------------
        //    if (!string.IsNullOrWhiteSpace(request.Keyword))
        //    {
        //        string keyword = request.Keyword.Replace("'", "''");

        //        if (!string.IsNullOrWhiteSpace(request.KeywordType))
        //        {
        //            sql.AppendLine(GetKeywordCondition(request.KeywordType, keyword));
        //        }
        //        else
        //        {
        //            sql.AppendLine(" AND (");
        //            sql.AppendLine($"     UPPER(m.\"CASE_CONTRACTNUMBER\") LIKE UPPER('%{keyword}%') OR");
        //            sql.AppendLine($"     UPPER(f.\"FI_SALESCODE\") LIKE UPPER('%{keyword}%') OR");
        //            sql.AppendLine($"     UPPER(f.\"FI_FIRSTNAME\") LIKE UPPER('%{keyword}%') OR");
        //            sql.AppendLine($"     UPPER(f.\"FI_LASTNAME\") LIKE UPPER('%{keyword}%') OR");
        //            sql.AppendLine($"     UPPER(p.\"PRODUCT_NAME\") LIKE UPPER('%{keyword}%') OR");
        //            sql.AppendLine($"     UPPER(m.\"CASE_CREATEDBY\") LIKE UPPER('%{keyword}%')");
        //            sql.AppendLine(" )");
        //        }
        //    }

        //    return sql.ToString();
        //}

        public static string BuildBaseQuery(RequestData request)
        {
            var sql = new StringBuilder();

            sql.AppendLine("FROM");
            sql.AppendLine($"    {_dbName}.{_schemaName}.FIFTYONECLUB_CASE m");

            sql.AppendLine($"INNER JOIN {_dbName}.{_schemaName}.FIFTYONECLUB_FINANCIALINFORMATION f");
            sql.AppendLine("    ON f.FI_CASE_ID = m.CASE_ID");
            sql.AppendLine("    AND f.FI_ISPRIMARY = 1");
            sql.AppendLine("    AND f.FI_ISDELETED = '0'");
            sql.AppendLine("    AND f.FI_ENDDATE = '9999-12-31'"); // ⛔ closed financial rows excluded

            sql.AppendLine($"INNER JOIN {_dbName}.{_schemaName}.PRODUCT p");
            sql.AppendLine("    ON p.PRODUCT_ID = m.CASE_PRODUCT_ID");

            sql.AppendLine("WHERE m.CASE_ENDDATE = '9999-12-31'");

            bool isContractSearch =
                string.Equals(request.KeywordType, "CONTRACT_NUMBER", StringComparison.OrdinalIgnoreCase);

            if (!isContractSearch &&
                request.OrgCodeFilter != null &&
                request.OrgCodeFilter.Count > 0)
            {
                sql.AppendLine(
                    $" AND m.\"CASE_TEAMOWNER_CODE\" IN ({string.Join(", ", request.OrgCodeFilter.Select(v => $"'{v}'"))})"
                );
            }

            if (!isContractSearch &&
                request.ViewFilter != null &&
                request.ViewFilter.Count > 0)
            {
                sql.AppendLine(
                    $" AND m.\"CASE_STATUS_ID\" IN ({string.Join(", ", request.ViewFilter.Select(v => $"'{v}'"))})"
                );
            }

            if (!string.IsNullOrWhiteSpace(request.Keyword))
            {
                string keyword = request.Keyword.Replace("'", "''");

                if (!string.IsNullOrWhiteSpace(request.KeywordType))
                {
                    sql.AppendLine(GetKeywordCondition(request.KeywordType, keyword));
                }
                else
                {
                    sql.AppendLine(" AND (");
                    sql.AppendLine($"     UPPER(m.\"CASE_CONTRACTNUMBER\") LIKE UPPER('%{keyword}%') OR");
                    sql.AppendLine($"     UPPER(f.\"FI_SALESCODE\") LIKE UPPER('%{keyword}%') OR");
                    sql.AppendLine($"     UPPER(f.\"FI_FIRSTNAME\") LIKE UPPER('%{keyword}%') OR");
                    sql.AppendLine($"     UPPER(f.\"FI_LASTNAME\") LIKE UPPER('%{keyword}%') OR");
                    sql.AppendLine($"     UPPER(p.\"PRODUCT_NAME\") LIKE UPPER('%{keyword}%') OR");
                    sql.AppendLine($"     UPPER(m.\"CASE_CREATEDBY\") LIKE UPPER('%{keyword}%')");
                    sql.AppendLine(" )");
                }
            }

            return sql.ToString();
        }

        private static string GetKeywordCondition(string keywordType, string keyword)
        {
            return keywordType switch
            {
                "SALES_CODE" => $" AND UPPER(f.\"FI_SALESCODE\") LIKE UPPER('%{keyword}%')",
                "CONTRACT_NUMBER" => $" AND UPPER(m.\"CASE_CONTRACTNUMBER\") LIKE UPPER('%{keyword}%')",
                "ADVISER_NAME" => $" AND UPPER(f.\"FI_FIRSTNAME\") LIKE UPPER('%{keyword}%')",
                "ADVISER_SURNAME" => $" AND UPPER(f.\"FI_LASTNAME\") LIKE UPPER('%{keyword}%')",
                "PRODUCT" => $" AND UPPER(p.\"PRODUCT_NAME\") LIKE UPPER('%{keyword}%')",
                "CREATEDBY" => $" AND UPPER(m.\"CASE_CREATEDBY\") LIKE UPPER('%{keyword}%')",
                _ => string.Empty,
            };
        }

        public static int ReturnTotalRecords(IDbCommand command)
        {
            int total = 0;
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    total++;
                }
            }
            return total;
        }

        public static List<ClientResponse> ReturnSearchCase(IDbCommand command)
        {
            var searchData = new List<ClientResponse>();
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var rowData = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string fieldName = reader.GetName(i);
                    object fieldValue = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    rowData.Add(fieldName, fieldValue);
                }
                string searchDataInfo = JsonConvert.SerializeObject(rowData);
                Console.WriteLine($"SnowFlake Response: {searchDataInfo}");
                if (!string.IsNullOrEmpty(searchDataInfo))
                {
                    var clientResponse = JsonConvert.DeserializeObject<ClientResponse>(searchDataInfo);
                    if (clientResponse != null)
                    {
                        searchData.Add(clientResponse);
                    }
                }

            }
            return searchData;
        }

    }
}
