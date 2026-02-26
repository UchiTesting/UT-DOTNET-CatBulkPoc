using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using CatBulk.Domain;

namespace CatBulk.Runner
{
    internal class Program
    {
        static void Main(string[] args)
        {
            #region Setup
            string masterConn = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;";
            string connString = @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;Initial Catalog=CatBulkDb;Pooling=true;Connect Timeout=30;";


            int nbCats = 1_000_000;

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

            if (!File.Exists(nativeFile)) throw new Exception("BCP Out file missing");

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
            if (!fi.Exists) throw new Exception("FILE NOT FOUND EXCEPTION. THE WORLD GONNA EXPLODE ! :O ");

            Console.WriteLine($"File {fi.FullName} found.");

            string oneLine = null;

            using (var sr = new StreamReader(csvFile))
            {
                oneLine = sr.ReadLine();
            }

            bool isDimensionCorrect = oneLine.Split(',').Length.Equals(8);

            if (!isDimensionCorrect) throw new Exception("WRONG FILE FORMAT EXCEPTION. THE WORLD GONNA EXPLODE ! :O ");
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
    }
}
