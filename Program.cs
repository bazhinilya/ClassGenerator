using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;

namespace ClassGen
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var tableNames = new List<string> { };
            string dbName = "Vacancies";
            using var conn = new SqlConnection($"Server=LAPTOP-SU0HN95T\\SQLEXPRESS;Database={dbName};Trusted_Connection=True;");
            var tableCommand = new SqlCommand
            (
                $@"SELECT 
                    TABLE_SCHEMA, 
                    TABLE_NAME
                FROM INFORMATION_SCHEMA.TABLES is1
                WHERE is1.TABLE_CATALOG = '{dbName}'
                    AND TABLE_TYPE IN ('BASE TABLE', 'VIEW')
                    AND is1.TABLE_NAME != 'sysdiagrams'", conn
            );
            conn.Open();
            SqlDataReader tableDataReader = tableCommand.ExecuteReader();
            while (tableDataReader.Read())
            {
                tableNames.Add(tableDataReader.GetString(1));
            }
            tableDataReader.Close();
            foreach (string tableName in tableNames)
            {
                string modelName = "EmploymentHelper.Models";
                string modelBuilder = $"using System;\nnamespace {modelName}\n{{\npublic class {tableName}\n{{\n";
                var columnsCommand = new SqlCommand
                (
                    $@"SELECT
                        is1.COLUMN_NAME,
                        is1.ORDINAL_POSITION,
                        is1.IS_NULLABLE,
                        is1.DATA_TYPE, 
                        is1.CHARACTER_MAXIMUM_LENGTH,
                        is1.NUMERIC_PRECISION, 
                        is1.NUMERIC_PRECISION_RADIX, 
                        is1.NUMERIC_SCALE 
                    FROM INFORMATION_SCHEMA.COLUMNS is1 
                    WHERE is1.TABLE_NAME = '{tableName}'", conn
                );
                SqlDataReader columnsReader = columnsCommand.ExecuteReader();
                string propertyBuilder = null;
                while (columnsReader.Read())
                {
                    propertyBuilder += $"\tpublic {ConvertSqlType(columnsReader["DATA_TYPE"])} {columnsReader["COLUMN_NAME"]} {{ get; set; }}\n";
                }
                columnsReader.Close();
                modelBuilder += $"{propertyBuilder}\n}}\n}}";
                string path = $"{tableName}.cs";
                File.WriteAllText(path, modelBuilder.ToString());
            }
        }

        private static string ConvertSqlType(object type)
        {
            return type switch
            {
                "nvarchar" or "varchar" => "string",
                "datetime" or "date" or "time" => "DateTime",
                "bit" => "bool",
                "uniqueidentifier" => "Guid",
                "tinyint" => "Byte",
                "numeric" => "Decimal",
                "float" => "double",
                _ => type.ToString(),
            };
        }
    }
}
