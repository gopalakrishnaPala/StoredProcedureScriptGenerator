using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreProcScriptGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var serverName = "";
            var databaseName = "";
            var directory = @"C:\StoredProcedures";

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var connectionString = $@"Integrated Security=SSPI;Persist Security Info=False;Initial Catalog={databaseName};Data Source={serverName}";

            foreach (var storedProcName in GetStoredProcedures(connectionString))
            {
                var storeProcedureContent = DbHelper.storeProcedureContent(connectionString, storedProcName);
                var ifExistsStatement = GetIfExistsStatement(storedProcName);

                using (StreamWriter outputFile = new StreamWriter(Path.Combine(directory, $"{storedProcName}.sql")))
                {
                    foreach (string line in ifExistsStatement)
                        outputFile.WriteLine(line);

                    foreach (string line in storeProcedureContent)
                        outputFile.WriteLine(line);
                }
            }
        }

        private static List<string> GetIfExistsStatement(string storedProcName)
        {
            List<string> ifExistsStatement = new List<string>();

            if (storedProcName != null)
            {
                ifExistsStatement.Add($"IF EXISTS(SELECT * FROM sys.objects WHERE type = 'P' and name = '{storedProcName}')");
                ifExistsStatement.Add($"BEGIN");
                ifExistsStatement.Add($"\t DROP PROCEDURE { storedProcName }");
                ifExistsStatement.Add($"END");
                ifExistsStatement.Add($"\nGO");
                ifExistsStatement.Add("\n");
            }

            return ifExistsStatement;
        }

        private static IEnumerable<string> GetStoredProcedures(string connectionString)
        {
            var storedProcedures = DbHelper.GetStoredProcedures(connectionString);

            foreach (DataRow storedProcedureDetails in storedProcedures.Rows)
            {
                //var schema = Convert.ToString(storedProcedureDetails["schemaName"]);
                var name = Convert.ToString(storedProcedureDetails["Name"]);
                yield return name;
            }
        }
    }

    static class DbHelper
    {
        public static DataTable GetStoredProcedures(string connectionString)
        {
            string query = "SELECT o.name as Name, s.name as schemaName  FROM sys.objects O INNER JOIN sys.schemas S ON S.schema_id = o.schema_id WHERE type ='P'";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlDataAdapter dataAdapter = new SqlDataAdapter(query, conn))
                {
                    // create the DataSet 
                    DataSet dataSet = new DataSet();
                    // fill the DataSet using our DataAdapter 
                    dataAdapter.Fill(dataSet);
                    return dataSet.Tables[0];
                }
            }
        }

        public static List<string> storeProcedureContent(string connectionString, string name)
        {
            List<string> storeProcedureContent = new List<string>();
            string query = "sys.sp_helptext";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand(query, conn))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@objname", name);

                    SqlDataAdapter sqlDataAdapter = new SqlDataAdapter
                    {
                        SelectCommand = command
                    };
                    DataSet resultSet = new DataSet();
                    sqlDataAdapter.Fill(resultSet);

                    if (resultSet.Tables.Count > 0)
                    {
                        if (resultSet.Tables[0].Rows.Count > 0)
                        {
                            foreach (DataRow dr in resultSet.Tables[0].Rows)
                            {
                                storeProcedureContent.Add(dr.ItemArray[0].ToString().Trim('\r', '\n'));
                            }

                        }
                    }
                }
            }

            return storeProcedureContent;
        }
    }
}
