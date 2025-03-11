using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("This utility connects to an SQL database, selects a scheme and dumps all tables to quoted CSV/Excel files.");
            Console.WriteLine("SQLDump uses integrated windows security to connect to a database/scheme. (run on server!)");
            Console.WriteLine("Usage: SQLDump.exe <server> <database>");
            Console.WriteLine("(C) MITCon NV - M. Verkade - March 2025");
            return;
        }

        string serverName = args[0];
        string databaseName = args[1];
        string connectionString = $"Server={serverName};Database={databaseName};Integrated Security=True;";
        string outputDirectory = Directory.GetCurrentDirectory();

        try
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                DataTable tables = conn.GetSchema("Tables");

                foreach (DataRow row in tables.Rows)
                {
                    if (row["TABLE_TYPE"].ToString() == "BASE TABLE")
                    {
                        string tableName = row["TABLE_NAME"].ToString();
                        Console.WriteLine($"Exporting {tableName}...");
                        ExportTableToCsv(conn, tableName, outputDirectory);
                    }
                }

                Console.WriteLine("\nExport completed! CSV files are saved in:\n" + outputDirectory);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("\nERROR: " + ex.Message);
        }
    }

    static void ExportTableToCsv(SqlConnection conn, string tableName, string outputDir)
    {
        string query = $"SELECT * FROM [{tableName}]";
        string filePath = Path.Combine(outputDir, tableName + ".csv");

        try
        {
            using (SqlCommand cmd = new SqlCommand(query, conn))
            using (SqlDataReader reader = cmd.ExecuteReader())
            using (StreamWriter writer = new StreamWriter(filePath, false, new UTF8Encoding(true)))
            {
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    writer.Write($"\"{reader.GetName(i)}\"");
                    if (i < reader.FieldCount - 1) writer.Write(",");
                }
                writer.WriteLine();

                while (reader.Read())
                {
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        string value = reader.IsDBNull(i) ? "" : reader[i].ToString();
                        value = value.Replace("\"", "\"\"");
                        value = value.Replace(",", ";");
                        writer.Write($"\"{value}\"");

                        if (i < reader.FieldCount - 1) writer.Write(",");
                    }
                    writer.WriteLine();
                }
            }

            Console.WriteLine($"Exported: {tableName}.csv");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR exporting {tableName}: " + ex.Message);
        }
    }
}
