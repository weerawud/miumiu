using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SqlClient;
using System.Globalization;
using System.Data;


    class MSSQLAdapter
    {
        private string _InstanceName;
        private string _Database;
        private string _UserID;
        private string _Password;
        private string _ConnectionString;
        private DataTable _existingAssetTable;
        #region method for access MSSQL AssetDB
        public MSSQLAdapter(string instance, string db, string userid, string password)
        {
            _InstanceName = instance;
            _Database = db;
            _UserID = userid;
            _Password = password;
            _ConnectionString = @"server = " + _InstanceName + ";Initial Catalog=" + _Database + ";User ID=" + _UserID + ";Password=" + _Password;
        }
        public MSSQLAdapter(string connectionString)
        {
            _ConnectionString = connectionString;
        }
        public DataTable CheckExistingTable()
        {
            SqlConnection _connection = new SqlConnection(_ConnectionString);
            DataTable dt = new DataTable();
            try
            {
                _connection.Open();
                //create data adapter
                SqlDataAdapter da = new SqlDataAdapter();
                //Create SQL Command For Delete Duplicate Row
                SqlCommand command = new SqlCommand(
                    "SELECT * FROM information_schema.tables", _connection);
                da.SelectCommand = command;
                da.Fill(dt);
            }
            catch (SqlException e)
            {
                System.Console.WriteLine("Error:" + e);

            }
            finally
            {
                _connection.Close();
                _existingAssetTable = dt;
            }
            return dt;
        }
        
        
        public string GetMSSQLConnectionString()
        {
            
            return _ConnectionString;

        }
        public Boolean IsConnected()
        {
            bool connected = true;
            if (_ConnectionString != "")
            {
                SqlConnection _connection = new SqlConnection(_ConnectionString);
                try
                {
                    _connection.Open();
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine("Error:" + ex);
                    connected = false;
                }
                finally
                {
                    _connection.Close();
                }
            }
            return connected;
        }
        public void SetMSSQLConnectionString(string connectSting)
        {
            _ConnectionString = connectSting;
        }
        public void InsertIntradayTranscationToIndividualTable(Dictionary<string, DataTable> s, string date)
        {
            //Get Exiting Table
            DataTable tablename = CheckExistingTable();
            bool IsExisting = false;
            string temptb;
            int count = 0;
            //_LabelBarUpdateDB.Text = "0 of " + s.Count;
            //_ProgressBarUpdateDB.Maximum = s.Count;
            //_ProgressBarUpdateDB.Value = 0;
            foreach (KeyValuePair<string, DataTable> d in s)
            {
                temptb = d.Key + "Intraday";
                IsExisting = false;
                //Check Is the table already has created?
                for (int i = 0; i < tablename.Rows.Count; i++)
                {
                    if (tablename.Rows[i]["TABLE_NAME"].ToString() == temptb)
                        IsExisting = true;
                }
                //Create Table if the table doesn't exist
                if (!IsExisting)
                {
                    CreateFromDataTable(d.Value.TableName + "Intraday", d.Value);
                }
                //Replace duplicate Row that same date
                if (IsExisting)
                {
                    DeleteDuplicateDateRowInIntradayTable(d.Value.TableName + "Intraday", date);
                }
                //Insert Data To the table
                InsertUpdateDataToTable(d.Value, d.Key + "Intraday");
                //for Bind Control Update Status
                ++count;
                //_LabelBarUpdateDB.Text = count + " of " + s.Count;
                //_ProgressBarUpdateDB.Value += 1;
            }
        }
        public void InsertUpdateDataToTable(DataTable dt, string tablename)
        {
            SqlConnection _connection = new SqlConnection(_ConnectionString);
            SqlBulkCopy bulkCopy = new System.Data.SqlClient.SqlBulkCopy(_connection);
            bulkCopy.DestinationTableName = "[" + tablename + "]";
            try
            {
                _connection.Open();
                bulkCopy.WriteToServer(dt);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Error:" + ex);
            }
            finally
            {
                bulkCopy.Close();
                _connection.Close();
            }
        }
        public void DeleteDuplicateDateRowInIntradayTable(string tablename, string date)
        {
            SqlConnection _connection = new SqlConnection(_ConnectionString);
            DataTable dt = new DataTable();
            try
            {
                _connection.Open();
                //create data adapter
                SqlDataAdapter da = new SqlDataAdapter();
                //Create SQL Command For Delete Duplicate Row
                string sql = String.Format("DELETE FROM {0} WHERE [<DATE>] = {1}", tablename, date);
                SqlCommand command = new SqlCommand(sql, _connection);
                /*
                command.Parameters.Add("@tablename",SqlDbType.VarChar,50);
                command.Parameters.Add("@date",SqlDbType.VarChar,50);
                command.Parameters["@tablename"].Value = tablename;
                command.Parameters["@date"].Value = date;*/
                command.ExecuteNonQuery();
            }
            catch (SqlException e)
            {
                System.Console.WriteLine("Error:" + e);
            }
            finally
            {
                _connection.Close();
            }

        }
        public void CreateFromDataTable(string _tableName, DataTable table)
        {
            string sql = GetCreateFromDataTableSQL(_tableName, table);
            SqlConnection _connection = new SqlConnection(_ConnectionString);
            SqlCommand cmd;
            _connection.Open();
            cmd = new SqlCommand(sql, _connection);
            try
            {
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Error:" + ex);
            }
            finally
            {
                _connection.Close();
            }
        }
        public static string GetCreateFromDataTableSQL(string tableName, DataTable table)
        {
            string sql = "CREATE TABLE [" + tableName + "] (\n";
            // columns
            foreach (DataColumn column in table.Columns)
            {
                sql += "[" + column.ColumnName + "] " + SQLGetType(column) + ",\n";
            }
            //primary keys
            if (table.PrimaryKey.Length > 0)
            {
                sql += "CONSTRAINT [PK_" + tableName + "] PRIMARY KEY CLUSTERED (";
                foreach (DataColumn column in table.PrimaryKey)
                {
                    sql += "[" + column.ColumnName + "],";
                }
                sql = sql.TrimEnd(new char[] { ',' }) + ")\n";
            }
            sql = sql.TrimEnd(new char[] { ',', '\n' }) + ")\n";
            return sql;
        }
        public static string SQLGetType(object type, int columnSize, int numericPrecision, int numericScale)
        {
            switch (type.ToString())
            {
                case "System.String":
                    return "VARCHAR(" + ((columnSize == -1) ? 255 : columnSize) + ")";
                case "System.Decimal":
                    if (numericScale > 0)
                        return "REAL";
                    else if (numericPrecision > 10)
                        return "BIGINT";
                    else
                        return "INT";
                case "System.Double":
                case "System.Single":
                    return "REAL";
                case "System.Int64":
                    return "BIGINT";
                case "System.Int16":
                case "System.Int32":
                    return "INT";
                case "System.DateTime":
                    return "DATETIME";
                default:
                    throw new Exception(type.ToString() + " not implemented.");
            }
        }
        // Overload based on row from schema table 
        public static string SQLGetType(DataRow schemaRow)
        {
            return SQLGetType(schemaRow["DataType"],
                                int.Parse(schemaRow["ColumnSize"].ToString()),
                                int.Parse(schemaRow["NumericPrecision"].ToString()),
                                int.Parse(schemaRow["NumericScale"].ToString()));
        }
        // Overload based on DataColumn from DataTable type
        public static string SQLGetType(DataColumn column)
        {
            return SQLGetType(column.DataType, column.MaxLength, 10, 2);
        }
        #endregion
        public DataTable GetAssetFromIntradayData(string ticker, string startdate, string enddate)
        {
            //Query Ticker from MSdata order by Date
            string qry = String.Format(@"SELECT * FROM {0}Intraday WHERE [<DATE>] <= '{1}' and [<DATE>] >= '{2}' ORDER BY [<DATE>]", ticker, startdate, enddate);
            //End query string
            SqlConnection _connection = new SqlConnection(_ConnectionString);
            DataTable dt = new DataTable();
            try
            {
                _connection.Open();
                //create data adapter
                SqlDataAdapter da = new SqlDataAdapter();
                da.SelectCommand = new SqlCommand(qry, _connection);
                //create and fill data to DataTable
                da.Fill(dt);
            }
            catch (SqlException e)
            {
                System.Console.WriteLine("Error :" + e.Message);
            }
            finally
            {
                _connection.Close();
            }
            dt.TableName = ticker + "Data";
            return dt;
        }
        
        public DataTable GetAssetFromIntradayData(string ticker)
        {
            //Query Ticker from MSdata order by Date
            string qry = String.Format(@"SELECT * FROM {0}Intraday ORDER BY [<DATE>]", ticker);
            //End query string
            SqlConnection _connection = new SqlConnection(_ConnectionString);
            DataTable dt = new DataTable();
            try
            {
                _connection.Open();
                //create data adapter
                SqlDataAdapter da = new SqlDataAdapter();
                da.SelectCommand = new SqlCommand(qry, _connection);
                //create and fill data to DataTable
                da.Fill(dt);
            }
            catch (SqlException e)
            {
                System.Console.WriteLine("Error :" + e.Message);
            }
            finally
            {
                _connection.Close();
            }
            dt.TableName = ticker + "Data";
            return dt;
        }
        public DataTable GetDatesFromIntradayData(string ticker)
        {
            //Query Ticker from MSdata order by Date
            string qry = String.Format(@"SELECT distinct [<DATE>] FROM {0}Intraday ORDER BY [<DATE>]", ticker);
            //End query string
            SqlConnection _connection = new SqlConnection(_ConnectionString);
            DataTable dt = new DataTable();
            try
            {
                _connection.Open();
                //create data adapter
                SqlDataAdapter da = new SqlDataAdapter();
                da.SelectCommand = new SqlCommand(qry, _connection);
                //create and fill data to DataTable
                da.Fill(dt);
            }
            catch (SqlException e)
            {
                System.Console.WriteLine("Error :" + e.Message);
            }
            finally
            {
                _connection.Close();
            }
            dt.TableName = ticker + "Data";
            return dt;
        }
        public DataTable GetTableFromDB(string tablename)
        {
            //Query Ticker from MSdata order by Date
            string qry = String.Format(@"SELECT * FROM {0} ORDER BY [<DATE>]", tablename);
            //End query string
            SqlConnection _connection = new SqlConnection(_ConnectionString);
            DataTable dt = new DataTable();
            try
            {
                _connection.Open();
                //create data adapter
                SqlDataAdapter da = new SqlDataAdapter();
                da.SelectCommand = new SqlCommand(qry, _connection);
                //create and fill data to DataTable
                da.Fill(dt);
            }
            catch (SqlException e)
            {
                System.Console.WriteLine("Error :" + e.Message);
                //tablename = "faield";
            }
            finally
            {
                _connection.Close();
            }
            dt.TableName = tablename;
            return dt;
        }
        public void DeleteTable(string _tableName)
        {
            string sqlTruncate = String.Format(@"DROP TABLE {0}", _tableName);
            SqlConnection _connection = new SqlConnection(_ConnectionString);
            SqlCommand cmd;
            _connection.Open();
            try
            {
                //If Table Existing then clear data
                if (CheckExistingTable(_tableName))
                {
                    cmd = new SqlCommand(sqlTruncate, _connection);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine("Error :" + e.Message);
            }
            finally
            {
                _connection.Close();
            }
        }
        public bool CheckExistingTable(string tablename)
        {
            DataTable existingTable = CheckExistingTable();
            bool IsExisting = false;
            //Check Existing Table in DB
            foreach (DataRow r in existingTable.Rows)
            {
                if (tablename == r["TABLE_NAME"].ToString())
                {
                    IsExisting = true;
                    break;
                }
            }
            return IsExisting;
        }
        public DataTable GetPortfolioFromDB(string tablename)
        {
            //Query Ticker from MSdata order by Date
            string qry = String.Format(@"SELECT * FROM {0} ORDER BY [datetime]", tablename);
            //End query string
            SqlConnection _connection = new SqlConnection(_ConnectionString);
            DataTable dt = new DataTable();
            try
            {
                _connection.Open();
                //create data adapter
                SqlDataAdapter da = new SqlDataAdapter();
                da.SelectCommand = new SqlCommand(qry, _connection);
                //create and fill data to DataTable
                da.Fill(dt);
            }
            catch (SqlException e)
            {
                System.Console.WriteLine("Error :" + e);
                //tablename = "faield";
            }
            finally
            {
                _connection.Close();
            }
            dt.TableName = tablename;
            return dt;
        }

    }

