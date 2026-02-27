namespace CatBulk.Runner
{
    internal class GlobalAppParams
    {
        public static int NumberOfCatsToGenerate { get; set; } = 100_000;
        public static string MasterConnectionString { get; set; } = GlobalAppConstants.Infrastructure.DefaultMasterConnectionString;
        public static string OperationalConnectionString { get; set; } = "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;Initial Catalog=CatBulkDb;Pooling=true;Connect Timeout=30;";
    }
}
