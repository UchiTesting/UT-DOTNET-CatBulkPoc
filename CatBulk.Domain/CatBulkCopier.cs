using System.Data.SqlClient;

namespace CatBulk.Domain
{
    /// <summary>
    /// Provides functionality to efficiently perform bulk copy operations of cat data into the 'dbo.Cat' table in a SQL
    /// Server database.
    /// </summary>
    /// <remarks>This static class is intended for high-performance data transfer scenarios where large
    /// volumes of cat records need to be inserted into the database. The bulk copy operation uses a default batch size
    /// of 5,000 records and streams data for optimal throughput. Ensure that the provided connection string is valid
    /// and that the destination table exists before invoking the bulk copy operation.</remarks>
    public static class CatBulkCopier
    {
        public static void RunSqlBulkCopy(string connString, int count)
        {
            SqlConnection connection = null;
            try
            {
                connection = new SqlConnection(connString);
                connection.Open();
                SqlBulkCopy bulk = new SqlBulkCopy(connection)
                {
                    DestinationTableName = "dbo.Cat",
                    BatchSize = 5000,
                    EnableStreaming = true
                };

                // Map only the real column without PK (CatId)
                bulk.ColumnMappings.Add("Name", "Name");
                bulk.ColumnMappings.Add("OwnerLastName", "OwnerLastName");
                bulk.ColumnMappings.Add("OwnerFirstName", "OwnerFirstName");
                bulk.ColumnMappings.Add("Age", "Age");
                bulk.ColumnMappings.Add("Gender", "Gender");
                bulk.ColumnMappings.Add("Fur", "Fur");
                bulk.ColumnMappings.Add("EyeColor", "EyeColor");

                CatReader reader = new CatReader(CatGenerator.Generate(count));
                bulk.WriteToServer(reader);
            }
            finally
            {
                if (connection != null) connection.Dispose();
            }
        }
    }
}