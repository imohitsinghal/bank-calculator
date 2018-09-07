using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SureTax.RenewalReport
{
    public class Utility
    {
        public List<T> GetList<T>(string command, string connection, params IDataParameter[] sqlParams) where T : new()
        {
            using (var con = new SqlConnection(connection))
            {
                con.Open();
                SqlCommand sqlcommand = new SqlCommand(command, con);
                if (sqlParams != null)
                {
                    foreach (IDataParameter para in sqlParams)
                    {
                        sqlcommand.Parameters.Add(para);
                    }
                }
                DataObjectMapper<T> resultsMapper = new DataObjectMapper<T>();
                sqlcommand.CommandTimeout = 6000;

                SqlDataReader reader = sqlcommand.ExecuteReader();

                List<T> results = resultsMapper.MapResultsToObject(reader);
                return results;
            }
        }

        public void ExecuteNonQuery(string command, string connection, params IDataParameter[] sqlParams)
        {
            using (var con = new SqlConnection(connection))
            {
                con.Open();
                SqlCommand sqlcommand = new SqlCommand(command, con);
                if (sqlParams != null)
                {
                    foreach (IDataParameter para in sqlParams)
                    {
                        sqlcommand.Parameters.Add(para);
                    }
                }
                sqlcommand.CommandTimeout = 6000;
                sqlcommand.ExecuteNonQuery();
            }

        }

        public void WriteToServer(string connectionstring, string destinationTableName, DataTable tableData)
        {
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connectionstring, SqlBulkCopyOptions.KeepIdentity))
            {
                bulkCopy.DestinationTableName = destinationTableName;

                foreach (DataColumn dataColumn in tableData.Columns)
                {
                    bulkCopy.ColumnMappings.Add(dataColumn.ColumnName, dataColumn.ColumnName);
                }

                bulkCopy.WriteToServer(tableData);
            }
        }
        
        public void LogError(Exception serviceException, string env = "")
        {
            StringBuilder messageBuilder = new StringBuilder();

            try
            {
                messageBuilder.AppendLine();
                messageBuilder.AppendLine("------------------------------------------------------------------------------------------");
                messageBuilder.Append(String.Format("The Exception is for {0} Envirnment:-",env));

                messageBuilder.Append("Exception :: " + serviceException.ToString());
                if (serviceException.InnerException != null)
                {
                    messageBuilder.Append("InnerException :: " + serviceException.InnerException.ToString());
                }
                messageBuilder.AppendLine();
                messageBuilder.AppendLine("------------------------------------------------------------------------------------------");
                messageBuilder.AppendLine();

                LogFileWrite( messageBuilder.ToString());
            }
            catch
            {
                messageBuilder.Append("Exception:: Unknown Exception.");
                LogFileWrite(messageBuilder.ToString());
            }

        }

        /// <summary>
        /// This method is for writting the Log file with message parameter
        /// </summary>
        /// <param name="message"></param>
        private void LogFileWrite(string message)
        {
            FileStream fileStream = null;
            StreamWriter streamWriter = null;
            try
            {
                var dirPath = Assembly.GetExecutingAssembly().Location;
                dirPath = Path.GetDirectoryName(dirPath);
                string logFilePath = Path.GetFullPath(Path.Combine(dirPath, @"Logs\"));

                logFilePath = logFilePath + "Log" + "-" + DateTime.Today.ToString("yyyyMMdd") + "." + "txt";

                if (logFilePath.Equals("")) return;
                #region Create the Log file directory if it does not exists 
                DirectoryInfo logDirInfo = null;
                FileInfo logFileInfo = new FileInfo(logFilePath);
                logDirInfo = new DirectoryInfo(logFileInfo.DirectoryName);
                if (!logDirInfo.Exists) logDirInfo.Create();
                #endregion Create the Log file directory if it does not exists

                if (!logFileInfo.Exists)
                {
                    fileStream = logFileInfo.Create();
                }
                else
                {
                    fileStream = new FileStream(logFilePath, FileMode.Append);
                }
                streamWriter = new StreamWriter(fileStream);
                streamWriter.WriteLine(message);
            }
            finally
            {
                if (streamWriter != null) streamWriter.Close();
                if (fileStream != null) fileStream.Close();
            }

        }
    }
}


public class DataObjectMapper<T> where T : new()
{
    public List<T> MapResultsToObject(DbDataReader reader)
    {
        List<T> objects = new List<T>();

        PropertyInfo[] _propertyInfo = typeof(T).GetProperties();
        reader.Cast<IDataRecord>().ToList().ForEach
        (i =>
        {
            T obj = new T();
            List<string> lstProps = Enumerable.Range(1, i.FieldCount).Select((e, index) => i.GetName(index).ToString().ToLower()).ToList();
            foreach (PropertyInfo prop in _propertyInfo)
            {
                if ((lstProps.Any(a => a == prop.Name.ToLower())))
                {
                    prop.SetValue(obj, i[prop.Name] != System.DBNull.Value ? i[prop.Name] : null, null);
                }
            }
            objects.Add(obj);
        }
        );
        return objects;
    }
}