namespace CatBulk.Runner
{
    internal static class GlobalAppConstants
    {
        internal static class Infrastructure
        {
            public const string DefaultMasterConnectionString = "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;";
            public const string DefaultOperationalConnectionString = "Server=(localdb)\\MSSQLLocalDB;Integrated Security=true;Initial Catalog=CatBulkDb;Pooling=true;Connect Timeout=30;";
        }
        internal static class Messages
        {
            internal static class File
            {
                public const string NotFound = "FILE NOT FOUND EXCEPTION. THE WORLD GONNA EXPLODE ! :O ";
                public const string WrongFormat = "WRONG FILE FORMAT EXCEPTION. THE WORLD GONNA EXPLODE ! :O ";
            }
        }
    }
}
