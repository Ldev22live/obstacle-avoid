using Snowflake.Data.Client;

namespace Ade.Club51.Case.Details.Interface
{
    public interface ISnowflakeConnectionFactory
    {
        SnowflakeDbConnection CreateConnection();
    }
}
