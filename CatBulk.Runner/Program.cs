using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using CatBulk.Domain;

namespace CatBulk.Runner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // deal with params here and setup options

            #region Setup

            int x = CheckForParameter<int>("num-cats", args);

            if (x != default(int))
                GlobalAppParams.NumberOfCatsToGenerate = x;

            string y = CheckForParameter<string>("master-cnx-str", args);
            if (!string.IsNullOrEmpty(y))
                GlobalAppParams.MasterConnectionString = y;
            
            string z = CheckForParameter<string>("ops-cnx-str", args);
            if(!string.IsNullOrEmpty(y))
                GlobalAppParams.OperationalConnectionString = z;
            
            Console.WriteLine($"Current number of cats to generate: {GlobalAppParams.NumberOfCatsToGenerate}");
            Console.WriteLine($"Current mst cnx string: {GlobalAppParams.MasterConnectionString}");
            Console.WriteLine($"Current ops cnx string: {GlobalAppParams.OperationalConnectionString}");
            
            string masterConn = GlobalAppParams.MasterConnectionString;
            string connString = GlobalAppParams.OperationalConnectionString;
            int nbCats = GlobalAppParams.NumberOfCatsToGenerate;
            #endregion

            #region Preparation
            Console.WriteLine("=== Cat Bulk POC ===");

            EnsureDatabase(masterConn);
            EnsureTable(connString);
            TruncateTable(connString);
            #endregion

            EncapsulateCSVDemo(nbCats);

            EncapsulateBcpDemo(connString);

            TruncateTable(connString);

            EncapsulateSbcDemo(connString, nbCats);

            Console.WriteLine("=== SUCCESS. You may now commit and enjoy your evening. ===");
        }

        private static void EncapsulateSbcDemo(string connString, int nbCats)
        {
            Console.WriteLine(">>> Running SqlBulkCopy");
            RunSqlBulkCopy(connString, nbCats);
        }

        private static void EncapsulateBcpDemo(string connString)
        {
            string nativeFile = "cats_native.dat";

            Console.WriteLine(">>> Running BCP OUT Native (-n)");
            GenerateBcpNative(nativeFile);

            if (!File.Exists(nativeFile)) throw new BulkFileException("BCP Out file missing");

            TruncateTable(connString);

            Console.WriteLine(">>> Running BCP IN Native (-n)");
            RunBcpNative(nativeFile);
        }

        private static void EncapsulateCSVDemo(int nbCats)
        {
            Console.WriteLine(">>> Generating CSV");
            string csvFile = "cats.csv";
            WriteCsv(csvFile, nbCats);
            CheckCsvFile(csvFile);

            Console.WriteLine(">>> Running BCP CSV");
            RunBcpCsv(csvFile);
        }

        private static void CheckCsvFile(string csvFile)
        {
            FileInfo fi = new FileInfo(csvFile);
            if (!fi.Exists) throw new BulkFileException(GlobalAppConstants.Messages.File.NotFound);

            Console.WriteLine($"File {fi.FullName} found.");

            string oneLine = null;

            using (var sr = new StreamReader(csvFile))
            {
                oneLine = sr.ReadLine();
            }

            bool isDimensionCorrect = oneLine.Split(',').Length.Equals(8);

            if (!isDimensionCorrect) throw new BulkFileException(GlobalAppConstants.Messages.File.WrongFormat);
        }

        static void EnsureDatabase(string masterConn)
        {
            using (var conn = new SqlConnection(masterConn))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "IF DB_ID('CatBulkDb') IS NULL CREATE DATABASE CatBulkDb;";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        static void EnsureTable(string connString)
        {
            using (var conn = new SqlConnection(connString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
IF OBJECT_ID('dbo.Cat', 'U') IS NULL
CREATE TABLE dbo.Cat
(
    Id INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    OwnerLastName NVARCHAR(100) NOT NULL,
    OwnerFirstName NVARCHAR(100) NOT NULL,
    Age INT NOT NULL,
    Gender CHAR(1) NOT NULL,
    Fur NVARCHAR(50) NOT NULL,
    EyeColor NVARCHAR(50) NOT NULL
);";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Permanently removes all rows from the 'Cat' table in the database specified by the connection string.
        /// </summary>
        /// <remarks>This operation cannot be rolled back and does not log individual row deletions. Use
        /// caution, as all data in the 'Cat' table will be lost.</remarks>
        /// <param name="connString">The connection string used to establish a connection to the database. Must reference a database containing
        /// the 'Cat' table.</param>
        static void TruncateTable(string connString)
        {
            using (var conn = new SqlConnection(connString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "TRUNCATE TABLE dbo.Cat;";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        static void WriteCsv(string path, int count)
        {
            using (var writer = new StreamWriter(path))
            {
                foreach (var cat in CatGenerator.Generate(count))
                {
                    writer.WriteLine($"{cat.CatId},{cat.Name},{cat.OwnerLastName},{cat.OwnerFirstName},{cat.Age},{cat.Gender},{cat.Fur},{cat.EyeColor}");
                }
            }
        }

        static void RunBcpCsv(string file)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "bcp",
                Arguments =
                    $"CatBulkDb.dbo.Cat  in {file} -c -t, -S (localdb)\\MSSQLLocalDB -T",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            var process = Process.Start(psi);

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            Console.WriteLine("ExitCode: " + process.ExitCode);
            Console.WriteLine(output);
            Console.WriteLine(error);
        }

        /// <summary>
        /// Exports data via BCP OUT. The previous step in the PoC should have bulked data. We use that to get a bulk ready file
        /// </summary>
        /// <param name="fileName"></param>
        private static void GenerateBcpNative(string fileName)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "bcp",
                Arguments = string.Format("CatBulkDb.dbo.Cat out {0} -n -S (localdb)\\MSSQLLocalDB -T", fileName),
                UseShellExecute = false
            };
            Process.Start(psi).WaitForExit();
        }


        private static void RunBcpNative(string file)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "bcp",
                Arguments = string.Format("CatBulkDb.dbo.Cat in {0} -n -S (localdb)\\MSSQLLocalDB -T", file),
                UseShellExecute = false
            };
            Process.Start(psi).WaitForExit();
        }

        static void RunSqlBulkCopy(string connString, int count)
        {
            using (var connection = new SqlConnection(connString))
            {
                connection.Open();

                using (var bulk = new SqlBulkCopy(connection))
                {
                    bulk.DestinationTableName = "dbo.Cat";
                    bulk.BatchSize = 5000;
                    bulk.EnableStreaming = true;

                    bulk.ColumnMappings.Add("Name", "Name");
                    bulk.ColumnMappings.Add("OwnerLastName", "OwnerLastName");
                    bulk.ColumnMappings.Add("OwnerFirstName", "OwnerFirstName");
                    bulk.ColumnMappings.Add("Age", "Age");
                    bulk.ColumnMappings.Add("Gender", "Gender");
                    bulk.ColumnMappings.Add("Fur", "Fur");
                    bulk.ColumnMappings.Add("EyeColor", "EyeColor");

                    using (var reader = new CatReader(CatGenerator.Generate(count)))
                    {
                        bulk.WriteToServer(reader);
                    }
                }
            }
        }

        // PSEUDOCODE / PLAN:
        // 1. Build a regex that matches a parameter name (provided in parameterName) optionally followed by =value
        // 2. Use named groups "paramName" and "paramValue" so we can locate the captured value easily.
        // 3. If the parameter is present:
        //    a. If a value was captured, convert it to T using Convert.ChangeType and return the casted T.
        //    b. If no value captured and T is bool, return true (treat as switch present).
        //    c. Otherwise return default(T).
        // 4. If the parameter is not present return default(T).
        //
        // Implementation notes:
        // - Use Regex.Escape(parameterName) so special characters in parameterName are matched literally.
        // - Use Group.Value to obtain the captured string; do not pass a Group object to Convert.ChangeType.
        // - Use typeof(T) for the Convert.ChangeType target Type and cast the resulting object to T.
        // - Return default(T) instead of null to handle value types.

        static T CheckForParameter<T>(string parameterName, string argumentLine)
        {
            if (string.IsNullOrEmpty(parameterName) || string.IsNullOrEmpty(argumentLine))
                return default(T);

            // Build pattern to match: /paramName, -paramName, /paramName=value, -paramName="value", etc.
            string pattern = @"[-/]{1,2}(?<paramName>" + Regex.Escape(parameterName) + @")(=?['""]?(?<paramValue>[^'""]+)['""]?)?\s?";
            var paramRegex = new Regex(pattern, RegexOptions.IgnoreCase);

            var match = paramRegex.Match(argumentLine);

            if (!match.Success)
                return default(T);

            var valueGroup = match.Groups["paramValue"];

            // If no explicit value provided and T is bool, treat presence as true
            if (!valueGroup.Success || string.IsNullOrEmpty(valueGroup.Value))
            {
                if (typeof(T) == typeof(bool))
                    return (T)(object)true;

                return default(T);
            }

            // Convert the captured string value to the requested type T and return it
            try
            {
                object converted = Convert.ChangeType(valueGroup.Value, typeof(T));
                return (T)converted;
            }
            catch
            {
                // Conversion failed -> return default(T)
                return default(T);
            }
        }

        static T CheckForParameter<T>(string parameterName, string[] argumentsArray)
        {
            // Linearize parameters
            string argumentLine = string.Join(" ", argumentsArray);
            var x = CheckForParameter<T>(parameterName, argumentLine);
            return x;
        }
    }
}
