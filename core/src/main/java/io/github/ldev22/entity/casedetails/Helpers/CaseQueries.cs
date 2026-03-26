using Ade.Club51.Case.Details.Config;
using Ade.Club51.Case.Details.Interface;
using Ade.Club51.Case.Details.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ade.Club51.Case.Details.Helpers
{
    public static class CaseQueries
    {
        public static string GetCaseDetailQuery(string db, string schema, string table) => $@"
            SELECT
                C.CASE_INITIATEDONDATE AS InitiatedOn,
                TO_VARCHAR(C.CASE_LASTUPDATED, 'YYYY-MM-DD') AS LastUpdated,
                C.CASE_ASSIGNEDTO AS AssignedTo,
                C.CASE_ID AS CaseId,
                C.CASE_CONTRACTNUMBER AS ContractNumber,
                C.CASE_SOURCE AS Source,
                C.CASE_CUSTOMERTYPE AS ClientType,
                C.CASE_CUSTOMERTINITIALS AS ClientInitials,
                C.CASE_CUSTOMERLASTNAME AS ClientLastName,
                TO_VARCHAR(C.CASE_CUSTOMERDATEOFBIRTH, 'YYYY-MM-DD') AS ClientDob,
                C.CASE_COMPANYNAME AS CompanyName,
                C.CASE_LOCATION AS Location,
                C.CASE_PRODUCT_ID AS ProductCode,
                P.PRODUCT_NAME AS ProductName,
                CASE WHEN C.CASE_ISSPLITCOMMISSION = 1 THEN 'Yes' ELSE 'No' END AS SplitCommission,
                FI.AdviserCode,
                FI.AdviserName,
                FI.FirstName,
                FI.LastName,
                FI.AdviserStatus,
                FI.SalesCode,
                C.CASE_TEAMOWNER_CODE AS teamOwnerCode,
                C.CASE_TEAMOWNER AS teamOwner,
                CASE WHEN C.CASE_MANUALLYMAINTAINED = 1 THEN 'Yes' ELSE 'No' END AS ManuallyUpdated,
                S.STATUS_DESC AS CaseStatus,
                C.CASE_CREATEDBY AS CreatedBy,
                C.CASE_CREATEDON AS CreatedOn,
                C.CASE_LASTMODIFIEDBY AS LastModifiedBy,
                C.CASE_LASTMODIFIED AS LastModifiedDate,
                C.CASE_LASTRUNDATE AS LastRunDate,
                FI.Area,
                FI.Region,
                CASE 
                    WHEN FI.TeamName IS NOT NULL 
                         AND CHARINDEX('(', FI.TeamName) > 0 
                         AND CHARINDEX(')', FI.TeamName) > CHARINDEX('(', FI.TeamName)
                    THEN SUBSTRING(FI.TeamName,
                                   CHARINDEX('(', FI.TeamName) + 1,
                                   CHARINDEX(')', FI.TeamName) - CHARINDEX('(', FI.TeamName) - 1)
                    ELSE NULL
                END AS OmTeamCode,
                FI.TeamName AS TeamName
            FROM {db}.{schema}.{table} C
            LEFT JOIN {db}.CLUB51.PRODUCT P 
                ON C.CASE_PRODUCT_ID = P.PRODUCT_ID
            LEFT JOIN {db}.CLUB51.FIFTYONECLUB_STATUS S
                ON C.CASE_STATUS_ID = S.STATUS_ID
            LEFT JOIN (
                SELECT /*+ MATERIALIZED */
                    FI_CASE_ID,
                    FI_SALESCODE AS AdviserCode,
                    FI_ADVISERNAME AS AdviserName,
                    FI_FIRSTNAME AS FirstName,
                    FI_LASTNAME AS LastName,
                    FI_ADVISERSTATUS AS AdviserStatus,
                    FI_SALESCODE AS SalesCode,
                    FI_AREA AS Area,
                    FI_REGION AS Region,
                    FI_TEAM AS TeamName
                FROM (
                    SELECT *
                    FROM {db}.CLUB51.FIFTYONECLUB_FINANCIALINFORMATION
                    WHERE FI_ISPRIMARY = '1' AND FI_ISDELETED = 0 AND FI_ENDDATE = '9999-12-31'
                    /*QUALIFY ROW_NUMBER() OVER (PARTITION BY FI_CASE_ID ORDER BY FI_ID) = 1*/
                ) Sub
            ) FI 
                ON C.CASE_ID = FI.FI_CASE_ID
            WHERE C.CASE_ID = :CaseId
              AND (C.CASE_ENDDATE IS NULL OR C.CASE_ENDDATE = '9999-12-31') ";




        public static string GetFinancialInfoQuery(string db, string schema, string table) => $@"
        SELECT 
            FI_ID AS BusinessKey,
            FI.FI_CASE_ID AS CaseId,
            FI.FI_CONTRACTNUMBER AS ContractNumber,
            '' AS AssignedTo,
            CASE WHEN FI.FI_ISPRIMARY = 1 THEN 'Yes' ELSE 'No' END AS IsPrimary,
            FI.FI_SPLITTYPE AS SplitType,
            '' AS AdviserCode,
            FI.FI_ADVISERNAME AS AdviserName,
            FI.FI_FIRSTNAME AS FirstName,
            FI.FI_LASTNAME AS LastName,
            FI.FI_ADVISERSTATUS AS AdviserStatus,
            FI.FI_SALESCODE AS SalesCode,
            FI.FI_COMMPERCENTAGE AS CommPercentage,
            FI.FI_FIGPERCENTAGE AS FigPercentage,
            FI.FI_NEGCOMMPERCENTAGE AS NegCommPercentage,
            (FI.FI_FIGPERCENTAGE / 100.0) AS CaseCount,
            FI.FI_CREATEDBY AS CreatedBy,
            FI.FI_CREATEDON AS CreatedOn,
            FI.FI_MODIFIEDBY AS LastModifiedBy,
            FI.FI_MODIFIEDON AS LastModifiedDate,
            '' AS TeamNameChain,
            FI.FI_AREA AS Area,
            FI.FI_REGION AS Region,
            SUBSTRING(
                FI.FI_TEAM,
                CHARINDEX('(', FI.FI_TEAM) + 1,
                CHARINDEX(')', FI.FI_TEAM) - CHARINDEX('(', FI.FI_TEAM) - 1
            ) AS OmTeamCode,
            FI.FI_TEAM AS TeamName
        FROM {db}.{schema}.{table} FI
        WHERE FI.FI_CASE_ID = :CaseId
          AND (FI.FI_ENDDATE IS NULL OR FI.FI_ENDDATE = '9999-12-31')";


        public static string GetContractDetailQuery(string db, string schema, string table) => $@"
         SELECT 
             CD_ID AS BusinessKey,
             CD.CD_CASE_ID AS CaseId,
             CD.CD_CONTRACTNUMBER AS ContractNumber,
             '' AS AssignedTo,
             '' AS IsPrimary,
             CD.CD_PRODUCT_ID AS ProductCode,
             P.PRODUCT_NAME AS ProductName,
             CD.CD_INVESTTYPE AS InvestType,
             CD.CD_INVESTAMOUNT AS InvestAmount,
             PF.PF_DESC AS PayFrequency,
             PM.PM_DESC AS PayMethod,
             CD.CD_COMMALLOWANCE AS CommAllowance,
             CD.CD_FPFEE AS FpFee,
             '' AS CommAllowFp,
             CD.CD_NEGCOMMALLOWANCE AS NegCommAllowance,
             CD.CD_NEGCOMMPERCENTAGE AS NegCommPercentage,
             CD.CD_CREATEDBY AS CreatedBy,
             CD.CD_CREATEDON AS CreatedOn,
             CD.CD_MODIFIEDBY AS LastModifiedBy,
             CD.CD_MODIFIED AS LastModifiedDate
         FROM {db}.{schema}.{table} CD
         LEFT JOIN {db}.CLUB51.PRODUCT P 
             ON CD.CD_PRODUCT_ID = P.PRODUCT_ID
         LEFT JOIN {db}.CLUB51.FIFTYONECLUB_PAYFREQUENCY PF
             ON CD.CD_PF_ID = PF.PF_ID
         LEFT JOIN {db}.CLUB51.FIFTYONECLUB_PAYMETHOD PM
             ON CD.CD_PM_ID = PM.PM_ID
         WHERE CD.CD_CASE_ID = :CaseId
           AND (CD.CD_ENDDATE IS NULL OR CD.CD_ENDDATE = '9999-12-31') AND CD.CD_ISDELETED = 0";


        public static string GetExceptionQuery(string db, string schema, string table) => $@"
        SELECT 
            EE.EE_CASE_ID AS CaseId,
            EE.EE_CONTRACTNUMBER AS ContractNumber,
            EE.EE_NEWBUSINESSSUBMISSIONLOGID AS BusinessKey,
            EE.EE_SALESCODE AS SalesCode,
            '' AS AdviserCode,
            EE.EE_ADVISERNAME AS AdviserName,
            EE.EE_LOCATION AS Location,
            EE.EE_INVESTAMOUNT AS InvestAmount,
            EE.EE_COMMALLOWANCE AS CommAllowance,
            EE.EE_COMMPERCENTAGE AS CommPercentage,
            EE.EE_FIGPERCENTAGE AS FigPercentage,
            EE.EE_TYPE AS ErrorType,
            EE.EE_ERROR AS ErrorMsg,
            EE.EE_CREATEDBY AS CreatedBy,
            EE.EE_CREATEDON AS CreatedOn,
            EE.EE_MODIFIEDBY AS LastModifiedBy,
            EE.EE_MODIFIED AS LastModifiedDate
        FROM {db}.{schema}.{table} EE
        WHERE EE.EE_CASE_ID = :CaseId
        AND (EE.EE_ENDDATE IS NULL OR EE.EE_ENDDATE = '9999-12-31')";

        public static string GetNotesQuery(string db, string schema, string table) => $@"
        SELECT 
            N.NOTE_CASE_ID AS CaseId,
            N.NOTE_CONTENT AS NoteContent,
            N.NOTE_ADDED_BY_NAME AS AddBy,
            TO_CHAR(N.NOTE_START_DATE, 'YYYY-MM-DD HH24:MI') AS AddDate
        FROM {db}.{schema}.{table} N
        WHERE N.NOTE_CASE_ID = :CaseId
        AND (N.NOTE_END_DATE IS NULL OR N.NOTE_END_DATE = '9999-12-31')";
    }

}
