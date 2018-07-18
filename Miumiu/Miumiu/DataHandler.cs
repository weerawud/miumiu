using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.IO;
using Deedle;
using System.Data.SqlClient;
using System.Globalization;
using Accord.Statistics;
using MathNet.Numerics.Statistics;

abstract class DataHandler
{

//abstract public void GetLatestABar();
    //abstract public void GetLatestBar();
    private string _connectionString;
    private bool _isConnected;
    

    public Boolean IsConnected
    {
        get { return _isConnected; }
        set { _isConnected = value; }
    }
    public String ConnectionString
    {
        get { return _connectionString; }
        set { _connectionString = value; }
    }
    abstract public Boolean InitConnect(string connectionString);
    abstract public Frame<int,string> GetBars(string symbol);
    abstract public Frame<int, string> GetBars(string symbol,string tradedate);
    abstract public DataTable GetBarsTable(string symbol);
    abstract public DataTable GetBarsTable(string symbol,string tradedate);
}
class DataHandlerMSSQL : DataHandler
{
    MSSQLAdapter _mssqlAdapter;
    public Dictionary<string, List<DataRow>> LongBigLotsDict;
    public Dictionary<string, List<DataRow>> ShortBigLotsDict;
    public DataHandlerMSSQL(string instance, string db, string userid, string password)
    {
        ConnectionString = @"server = " + instance + ";Initial Catalog=" + db + ";User ID=" + userid + ";Password=" + password;
        IsConnected= InitConnect(ConnectionString);
        LongBigLotsDict = new Dictionary<string, List<DataRow>>();
        ShortBigLotsDict = new Dictionary<string, List<DataRow>>();
    }
    public DataHandlerMSSQL(string connectionstring)
    {
        ConnectionString = connectionstring;
        IsConnected = InitConnect(ConnectionString);
        LongBigLotsDict = new Dictionary<string, List<DataRow>>();
        ShortBigLotsDict = new Dictionary<string, List<DataRow>>();
    }
    
    public override bool InitConnect(string connectionString)
    {
        bool isConnect = false;
        _mssqlAdapter = new MSSQLAdapter(connectionString);
        isConnect = _mssqlAdapter.IsConnected();
        return isConnect;
    }
    public override Frame<int,string> GetBars(string symbol)
    {
        var frameSymbol = Frame.CreateEmpty<int, string>();
        string queryString = String.Format(@"SELECT * FROM {0}Intraday ORDER BY [<DATE>]", symbol);
        using (SqlConnection connection = new SqlConnection(ConnectionString))
        {
            SqlCommand command = new SqlCommand(queryString, connection);
            connection.Open();
            SqlDataReader reader = command.ExecuteReader();
            //Query Table to Deedle DataFrame
            frameSymbol = Frame.ReadReader(reader);
            reader.Close();
        }
        
        return frameSymbol;
    }
    public override Frame<int, string> GetBars(string symbol,string tradedate)
    {
        var frameSymbol = Frame.CreateEmpty<int, string>();
        string queryString = String.Format(@"SELECT * FROM {0}Intraday WHERE [<DATE>] <= '{1}' and [<DATE>] >= '{2}' ORDER BY [<TIME>]", symbol, tradedate, tradedate);
        using (SqlConnection connection = new SqlConnection(ConnectionString))
        {
            SqlCommand command = new SqlCommand(queryString, connection);
            connection.Open();
            SqlDataReader reader = command.ExecuteReader();
            //Query Table to Deedle DataFrame
            frameSymbol = Frame.ReadReader(reader);
            reader.Close();
        }
        return frameSymbol;
    }
    public override DataTable GetBarsTable(string symbol)
    {
        var dt = new DataTable(symbol);
        string queryString = String.Format(@"SELECT * FROM {0}Intraday ORDER BY [<DATE>]", symbol);
        using (SqlConnection connection = new SqlConnection(ConnectionString))
        {
            SqlCommand command = new SqlCommand(queryString, connection);
            connection.Open();

            //create data adapter
            SqlDataAdapter da = new SqlDataAdapter();
            da.SelectCommand = new SqlCommand(queryString, connection);
            //create and fill data to DataTable
            da.Fill(dt);
        }
        return dt;
    }
    public DataTable GetDailyBarsTable(string symbol)
    {
        var dt = new DataTable(symbol);
        string queryString = String.Format(@"SELECT * FROM {0} ORDER BY [<DATE>]", symbol);
        using (SqlConnection connection = new SqlConnection(ConnectionString))
        {
            SqlCommand command = new SqlCommand(queryString, connection);
            connection.Open();

            //create data adapter
            SqlDataAdapter da = new SqlDataAdapter();
            da.SelectCommand = new SqlCommand(queryString, connection);
            //create and fill data to DataTable
            da.Fill(dt);
        }
        return dt;
    }
    public DataTable GetDatesBarsTable(string symbol)
    {
        var dt = new DataTable(symbol);
        string queryString = String.Format(@"SELECT distinct [<DATE>] FROM [{0}Intraday] ORDER BY [<DATE>]", symbol);
        using (SqlConnection connection = new SqlConnection(ConnectionString))
        {
            SqlCommand command = new SqlCommand(queryString, connection);
            connection.Open();

            //create data adapter
            SqlDataAdapter da = new SqlDataAdapter();
            da.SelectCommand = new SqlCommand(queryString, connection);
            //create and fill data to DataTable
            da.Fill(dt);
        }
        return dt;
    }
    public DataTable CheckExistingTable()
    {
        SqlConnection _connection = new SqlConnection(ConnectionString);
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
        }
        return dt;
    }
    public DataTable RemoveDuplicateTFEX(string symbol)
    {
        var dt = new DataTable(symbol);
        string queryString = String.Format(@"SELECT * FROM {0}Intraday WHERE [<DATE>] >= '{1}' and [<DATE>]<= '{2}' ORDER BY [<DATE>]", symbol,"20150223","20150522");
        using (SqlConnection connection = new SqlConnection(ConnectionString))
        {
            SqlCommand command = new SqlCommand(queryString, connection);
            connection.Open();

            //create data adapter
            SqlDataAdapter da = new SqlDataAdapter();
            da.SelectCommand = new SqlCommand(queryString, connection);
            //create and fill data to DataTable
            da.Fill(dt);
        }
        Dictionary<string, List<DataRow>> dictDT = new Dictionary<string, List<DataRow>>();
        foreach (DataRow r in dt.Rows)
        {
            string tmpkey = r["<DATE>"].ToString()+r["<TIME>"].ToString();
            if (!dictDT.ContainsKey(tmpkey))
                dictDT.Add(tmpkey, new List<DataRow>());
            dictDT[tmpkey].Add(r);
        }
        //Remove
        foreach (KeyValuePair<string, List<DataRow>> kvp in dictDT)
        {
            int tmpcount = kvp.Value.Count;
            if (kvp.Value.Count > 1)
            {
                while (kvp.Value.Count > tmpcount / 2)
                    kvp.Value.RemoveAt(kvp.Value.Count - 1);
            }

        }
        DataTable newDT = dt.Clone();
        foreach (KeyValuePair<string, List<DataRow>> kvp in dictDT)
        {
            foreach(DataRow r in kvp.Value)
                newDT.Rows.Add(r.ItemArray);

        }
        return newDT;

    }
    public override DataTable GetBarsTable(string symbol,string tradedate)
    {
        var dt = new DataTable(symbol);
        string queryString = String.Format(@"SELECT * FROM [{0}Intraday] WHERE [<DATE>] <= '{1}' and [<DATE>] >= '{2}' ORDER BY [<TIME>]", symbol, tradedate, tradedate);
        using (SqlConnection connection = new SqlConnection(ConnectionString))
        {
            SqlCommand command = new SqlCommand(queryString, connection);
            connection.Open();

            //create data adapter
            SqlDataAdapter da = new SqlDataAdapter();
            da.SelectCommand = new SqlCommand(queryString, connection);
            //create and fill data to DataTable
            da.Fill(dt);
        }
        return dt;
    }
    public DataTable GetBarsTable(string symbol, string startdate,string enddate)
    {
        var dt = new DataTable(symbol);
        string queryString = String.Format(@"SELECT * FROM {0}Intraday WHERE [<DATE>] <= '{1}' and [<DATE>] >= '{2}' ORDER BY [<DATE>],[<TIME>]", symbol, enddate, startdate);
        
        using (SqlConnection connection = new SqlConnection(ConnectionString))
        {
            SqlCommand command = new SqlCommand(queryString, connection);
            connection.Open();

            //create data adapter
            SqlDataAdapter da = new SqlDataAdapter();
            da.SelectCommand = new SqlCommand(queryString, connection);
            //create and fill data to DataTable
            da.Fill(dt);
        }
        return dt;
    }
    public DataTable GetBarsTable(string symbol, string startdate, string enddate,TimeSpan stime, TimeSpan etime)
    {
        var tmpdt = this.GetBarsTable(symbol, startdate, enddate);
        DataTable dt = tmpdt.Clone();
        foreach(DataRow dr in tmpdt.Rows){
            TimeSpan ctime = TimeSpan.Parse(dr["<TIME>"].ToString());
            if ((ctime >= stime) && (ctime <= etime))
                dt.Rows.Add(dr.ItemArray);
        }
        return dt;
    }
    public DataTable SaveResamplingBarsTableToCSV(string symbol, string startdate, string enddate, int interval, string filepath)
    {
        var bar = this.GetBarsTable(symbol, startdate, enddate);
        var resampbar = this.GetResamplingBarsTable(interval, bar);
        this.ToCSV(filepath, resampbar);
        return resampbar;
    }
    public DataTable GetResamplingBarsTable(string symbol, string startdate, string enddate, int interval)
    {
        var bar = this.GetBarsTable(symbol, startdate, enddate);
        var resampbar = this.GetResamplingBarsTable(interval, bar);
        return resampbar;
    }
    public DataTable GetResamplingBarsTableFillEmpty(string symbol, string startdate, string enddate, int interval)
    {
        var bar = this.GetBarsTable(symbol, startdate, enddate);
        var resampbar = this.GetResamplingBarsTableFillEmpty(interval, bar);
        return resampbar;
    }
    private void ToCSV(string filename, DataTable dGV)
    {
        string stOutput = "";
        // Export titles:
        string sHeaders = "";

        for (int j = 0; j < dGV.Columns.Count; j++)
        {
            sHeaders = sHeaders.ToString() + Convert.ToString(dGV.Columns[j].ToString()) + ",";
            
                
            
        }
        stOutput += sHeaders + "\r\n";
        // Export data.
        for (int i = 0; i < dGV.Rows.Count - 1; i++)
        {
            string stLine = "";
            
            for (int j = 0; j < dGV.Columns.Count; j++)
            {
                string tmptext = "";
                if (j == 0){
                    tmptext = Convert.ToString(dGV.Rows[i][j].ToString()).Split(' ')[0];
                    tmptext = DateTime.Parse(tmptext).ToString("yyyyMMdd");
                }
                else tmptext = Convert.ToString(dGV.Rows[i][j].ToString());
                
                stLine = stLine.ToString() + tmptext + ",";
            }
            stOutput += stLine + "\r\n";
        }
        Encoding utf16 = Encoding.GetEncoding(1254);
        byte[] output = utf16.GetBytes(stOutput);
        FileStream fs = new FileStream(filename, FileMode.Create);
        BinaryWriter bw = new BinaryWriter(fs);
        bw.Write(output, 0, output.Length); //write the encoded file
        bw.Flush();
        bw.Close();
        fs.Close();
    }
    public DataTable GetQueryTable(DataTable bar)
    {
        DataTable dt = new DataTable(bar.TableName);
        //Prepare DataTable for create DataRow for tradetime Dict
        DataTable tmptradedt = new DataTable();
        DataColumn[] tradecol = new DataColumn[4];
        tradecol[0] = new DataColumn("Close", typeof(System.Double));
        tradecol[1] = new DataColumn("Volume", typeof(System.Double));
        tradecol[2] = new DataColumn("Value", typeof(System.Double));
        tradecol[3] = new DataColumn("Date", typeof(System.DateTime));
        tmptradedt.Columns.AddRange(tradecol);
        dt = tmptradedt.Clone();
        foreach (DataRow r in bar.Rows)
        {
            DateTime tradetime = this.ToDateTime(r["<DATE>"].ToString(), "yyyyMMdd").Add(TimeSpan.Parse(r["<TIME>"].ToString()));
            DataRow tradedr = tmptradedt.NewRow();
            //Round up to most nearest interval
            
            double tmpprice = Double.Parse(r["<CLOSE>"].ToString());
            double tmpvol = 0;
            if (r["<VOL>"].ToString().TrimEnd(null).EndsWith("M"))
                tmpvol = 1000000 * Double.Parse(r["<VOL>"].ToString().Remove(r["<VOL>"].ToString().LastIndexOf('M'), 1));
            else tmpvol = Double.Parse(r["<VOL>"].ToString());

            //negative volume in short side
            if (r["<OPENINT>"].ToString().Trim() == "S")
                tmpvol = -1 * tmpvol;
            //Caculate value1
            double tmpval = tmpprice * tmpvol;
            tradedr["Close"] = tmpprice;
            tradedr["Volume"] = tmpvol;
            tradedr["Value"] = tmpval;
            tradedr["Date"] = tradetime;
            dt.Rows.Add(tradedr.ItemArray);
        }
        return dt;
    }
    public DataTable GetResamplingBarsTable(int secondinterval, DataTable bar)
    {
        DataTable dt = new DataTable(bar.TableName);

        //Get List of Trade date from bar table
        DataTable dtDate = bar.DefaultView.ToTable(true, "<DATE>");
        Dictionary<double, List<DataRow>> priceDict = new Dictionary<double, List<DataRow>>();

        if (secondinterval > 0)
        {
            
            //Dictionary<TimeSpan, DataRow> intraDict = this.IntradayTimeTradeDict(secondinterval);
            Dictionary<DateTime, DataRow> intraDict = this.IntradayTimeTradeDict(dtDate,secondinterval);
            Dictionary<DateTime, List<DataRow>> tradetimeDict = new Dictionary<DateTime, List<DataRow>>();
            //Dictionary<TimeSpan, List<Double>> priceDict = new Dictionary<TimeSpan,List<Double>>();
            
            
            //Prepare DataTable for create DataRow for tradetime Dict
            DataTable tmptradedt = new DataTable();
            DataColumn[] tradecol = new DataColumn[4];
            tradecol[0] = new DataColumn("Close", typeof(System.Double));
            tradecol[1] = new DataColumn("Volume", typeof(System.Double));
            tradecol[2] = new DataColumn("Value", typeof(System.Double));
            tradecol[3] = new DataColumn("Date", typeof(System.DateTime));
            tmptradedt.Columns.AddRange(tradecol);
            
            //Grouping into each trade tine period
            foreach (DataRow r in bar.Rows)
            {
                
                //TimeSpan tradetime = TimeSpan.Parse(r["<TIME>"].ToString());
                DateTime tradetime = this.ToDateTime(r["<DATE>"].ToString(),"yyyyMMdd").Add(TimeSpan.Parse(r["<TIME>"].ToString()));
                //DateTime tradedate = (DateTime)r["<DATE>"].
                DataRow tradedr = tmptradedt.NewRow();
                //Round up to most nearest interval
                //tradetime.TimeOfDay
                tradedr["Date"] = tradetime;
                tradetime=tradetime.Date+this.RoundTo(tradetime.TimeOfDay,secondinterval);
                DataRow tmprow = intraDict[tradetime];
                double tmpprice = Double.Parse(r["<CLOSE>"].ToString());
                double tmpvol = 0;
                if (r["<VOL>"].ToString().TrimEnd(null).EndsWith("M"))
                    tmpvol = 1000000 * Double.Parse(r["<VOL>"].ToString().Remove(r["<VOL>"].ToString().LastIndexOf('M'), 1));
                else tmpvol = Double.Parse(r["<VOL>"].ToString());

                //negative volume in short side
                if (r["<OPENINT>"].ToString().Trim() == "S")
                    tmpvol = -1*tmpvol;
                //Caculate value1
                double tmpval = tmpprice * tmpvol;
                
                tradedr["Close"] = tmpprice;
                tradedr["Volume"] = tmpvol;
                tradedr["Value"] = tmpval;
                //Add price to price dict
                if (!tradetimeDict.ContainsKey(tradetime))
                    tradetimeDict.Add(tradetime, new List<DataRow>());
                tradetimeDict[tradetime].Add(tradedr);

                //Add price to price dict
                if (!priceDict.ContainsKey(Math.Round(tmpprice)))
                    priceDict.Add(Math.Round(tmpprice), new List<DataRow>());
                
                priceDict[Math.Round(tmpprice)].Add(tradedr);

                
                //Check first resampling this interval
                //Open
                if (tmprow["Open"] == DBNull.Value)
                {
                    tmprow["Open"] = tmpprice;
                }
                //High
                if (tmprow["High"] == DBNull.Value)
                {
                    tmprow["High"] = tmpprice;
                }
                else if (tmpprice > (Double)tmprow["High"])
                    tmprow["High"] = tmpprice;
                //Low
                if (tmprow["Low"] == DBNull.Value)
                {
                    tmprow["Low"] = tmpprice;
                }
                else if (tmpprice < (Double)tmprow["Low"])
                    tmprow["Low"] = tmpprice;
                //Close
                tmprow["Close"] = tmpprice;
                //Volume
                if (tmprow["Volume"] == DBNull.Value)
                {
                    tmprow["Volume"] = tmpvol;
                }
                else tmprow["Volume"] = (Double)tmprow["Volume"] + tmpvol;
                //Value
                if (tmprow["Value"] == DBNull.Value)
                {
                    tmprow["Value"] = tmpval;
                }
                else tmprow["Value"] = (Double)tmprow["Value"] + tmpval;
                
                


                //Assign new resampling value to the row
                intraDict[tradetime] = tmprow;
                
            }
            DataRow prevRow = dt.NewRow();
            
            foreach (KeyValuePair<DateTime, List<DataRow>> kvp in tradetimeDict)
            {
                //Init temporary sum transaction vol
                double tmplongtps = 0;
                double tmpshortps = 0;
                //Initial Colume for dt (result table)
                if (dt.Columns.Count == 0)
                    dt = intraDict[kvp.Key].Table.Clone();

                //Add Date & Time
                intraDict[kvp.Key]["Date"] = kvp.Key.Date;
                intraDict[kvp.Key]["Time"] = kvp.Key.TimeOfDay;

                if (kvp.Value.Count > 0)
                {
                    
                    
                    //Loop for cal each transaction in one tradetime period
                    List<Double> tradeClose = new List<double>();
                    List<Double> tradeVolume = new List<double>();
                    List<Double> tradeValue = new List<double>();
                    Dictionary<string, List<DataRow>> LongBigLotsDict = new Dictionary<string, List<DataRow>>();
                    Dictionary<string, List<DataRow>> ShortBigLotsDict = new Dictionary<string, List<DataRow>>();
                    foreach (DataRow r in kvp.Value)
                    {
                        double tmpvoltps = (Double)r["Volume"];
                        if (tmpvoltps > 0)
                        {
                            //for biglots
                            if (!LongBigLotsDict.ContainsKey(r["Date"].ToString()))
                                LongBigLotsDict.Add(r["Date"].ToString(), new List<DataRow>());
                            LongBigLotsDict[r["Date"].ToString()].Add(r);
                        }
                        else if (tmpvoltps < 0)
                        {
                            //for biglots
                            if (!ShortBigLotsDict.ContainsKey(r["Date"].ToString()))
                                ShortBigLotsDict.Add(r["Date"].ToString(), new List<DataRow>());
                            ShortBigLotsDict[r["Date"].ToString()].Add(r);
                        }
                        tradeClose.Add((Double)r["Close"]);
                        tradeVolume.Add((Double)r["Volume"]);
                        tradeValue.Add((Double)r["Value"]);
                    }
                    //Start Find Biglot > 100 contact per sec &&
                    double totallongtmpbtvol = 0;
                    double totallongtmpbttrans = 0;
                    double totallongtmpbtval = 0;
                    double totallongtmpbtcost = 0;
                    foreach (KeyValuePair<string, List<DataRow>> kv in LongBigLotsDict)
                    {
                        double tmpbtvol = 0;
                        double tmpbtval = 0;
                        foreach (DataRow r in kv.Value)
                        {
                            tmpbtvol = tmpbtvol + (Double)r["Volume"];
                            tmpbtval = tmpbtval + (Double)r["Value"];
                        }
                        if (tmpbtvol > 100)
                        {
                            totallongtmpbtvol = totallongtmpbtvol + tmpbtvol;
                            totallongtmpbtval = totallongtmpbtval + tmpbtval;
                            totallongtmpbttrans = totallongtmpbttrans + kv.Value.Count;
                            //Add all transactions that make the Biglots
                            if (!this.LongBigLotsDict.ContainsKey(kv.Key))
                                this.LongBigLotsDict.Add(kv.Key, new List<DataRow>());
                            this.LongBigLotsDict[kv.Key].AddRange(kv.Value); 
                        }

                    }
                    totallongtmpbtcost = totallongtmpbtval / totallongtmpbtvol;

                    double totalshorttmpbtvol = 0;
                    double totalshorttmpbttrans = 0;
                    double totalshorttmpbtval = 0;
                    double totalshorttmpbtcost = 0;
                    foreach (KeyValuePair<string, List<DataRow>> kv in ShortBigLotsDict)
                    {
                        double tmpbtvol = 0;
                        double tmpbtval = 0;
                        foreach (DataRow r in kv.Value)
                        {
                            tmpbtvol = tmpbtvol + (Double)r["Volume"];
                            tmpbtval = tmpbtval + (Double)r["Value"];
                        }
                        if (tmpbtvol < -100)
                        {
                            totalshorttmpbtvol = totalshorttmpbtvol + tmpbtvol;
                            totalshorttmpbtval = totalshorttmpbtval + tmpbtval;
                            totalshorttmpbttrans = totalshorttmpbttrans + kv.Value.Count;
                            //Add all transactions that make the Biglots
                            if (!this.ShortBigLotsDict.ContainsKey(kv.Key))
                                this.ShortBigLotsDict.Add(kv.Key, new List<DataRow>());
                            this.ShortBigLotsDict[kv.Key].AddRange(kv.Value);
                        }
                    }
                    totalshorttmpbtcost = totalshorttmpbtval / totalshorttmpbtvol;

                    intraDict[kvp.Key]["SumShortVol Biglots"] = totalshorttmpbtvol;
                    intraDict[kvp.Key]["SumLongVol Biglots"] = totallongtmpbtvol;
                    intraDict[kvp.Key]["SumShortTrans Biglots"] = totalshorttmpbttrans;
                    intraDict[kvp.Key]["SumLongTrans Biglots"] = totallongtmpbttrans;
                    intraDict[kvp.Key]["AvgShortCost Biglots"] = totalshorttmpbtcost;
                    intraDict[kvp.Key]["AvgLongCost Biglots"] = totallongtmpbtcost;
                    //End BigLot


                    //Calculate Standard Deviation
                    
                    intraDict[kvp.Key]["SD"] = Math.Round(Statistics.StandardDeviation(tradeClose.ToArray()), 3);

                    //Calculate Max Long/Short Volume
                    if (tradeVolume.Max() >= 0)
                        intraDict[kvp.Key]["MaxLongVolume"] = tradeVolume.Max();
                    else intraDict[kvp.Key]["MaxLongVolume"] = 0;
                    if (tradeVolume.Min() < 0)
                        intraDict[kvp.Key]["MaxShortVolume"] = tradeVolume.Min();
                    else intraDict[kvp.Key]["MaxShortVolume"] = 0;
                    
                    //Calculate Max Long/Short Value
                    if (tradeValue.Max() >= 0)
                        intraDict[kvp.Key]["MaxLongValue"] = tradeValue.Max();
                    else intraDict[kvp.Key]["MaxLongValue"] = 0;
                    if (tradeValue.Min() < 0)
                        intraDict[kvp.Key]["MaxShortValue"] = tradeValue.Min();
                    else intraDict[kvp.Key]["MaxShortValue"] = 0;

                    //Calculate Total Long/Short Value
                    foreach (double vol in tradeVolume)
                    {
                        if (intraDict[kvp.Key]["TotalLongVolume"] == DBNull.Value)
                            intraDict[kvp.Key]["TotalLongVolume"] = 0;
                        if (intraDict[kvp.Key]["TotalShortVolume"] == DBNull.Value)
                            intraDict[kvp.Key]["TotalShortVolume"] = 0;

                        if (vol >= 0) //Long
                        {
                            intraDict[kvp.Key]["TotalLongVolume"] = (Double)intraDict[kvp.Key]["TotalLongVolume"] + vol;
                            tmplongtps = tmplongtps + 1;
                        }
                        else //Short
                        {
                            intraDict[kvp.Key]["TotalShortVolume"] = (Double)intraDict[kvp.Key]["TotalShortVolume"] + vol;
                            tmpshortps = tmpshortps + 1;
                        }
                        
                    }
                    //Assign transcation per interval
                    intraDict[kvp.Key]["Long TPS"] = tmplongtps;
                    intraDict[kvp.Key]["Short TPS"] = tmpshortps;
                    //Calculate Total Long/Short Value
                    foreach(double val in tradeValue)
                    {
                        if (intraDict[kvp.Key]["TotalLongValue"] == DBNull.Value)
                            intraDict[kvp.Key]["TotalLongValue"] = 0;
                        if (intraDict[kvp.Key]["TotalShortValue"] == DBNull.Value)
                            intraDict[kvp.Key]["TotalShortValue"] = 0;

                        if (val >= 0) //Long
                            intraDict[kvp.Key]["TotalLongValue"] = (Double)intraDict[kvp.Key]["TotalLongValue"] + val;
                        else //Short
                            intraDict[kvp.Key]["TotalShortValue"] = (Double)intraDict[kvp.Key]["TotalShortValue"] + val;
                    }
                    //Calculate %Volume Long/short
                    double abstotalvol = (Double)intraDict[kvp.Key]["TotalLongVolume"] + Math.Abs((Double)intraDict[kvp.Key]["TotalShortVolume"]);
                    intraDict[kvp.Key]["%Volume Long"] = Math.Round(100 * (Double)intraDict[kvp.Key]["TotalLongVolume"] / abstotalvol, 2);
                    intraDict[kvp.Key]["%Volume Short"] = Math.Round(100 * Math.Abs((Double)intraDict[kvp.Key]["TotalShortVolume"] / abstotalvol), 2);

                    //Calculate %Value Long/short
                    double abstotalval = (Double)intraDict[kvp.Key]["TotalLongValue"] + Math.Abs((Double)intraDict[kvp.Key]["TotalShortValue"]);
                    intraDict[kvp.Key]["%Value Long"] = Math.Round(100 * (Double)intraDict[kvp.Key]["TotalLongValue"] / abstotalval, 2);
                    intraDict[kvp.Key]["%Value Short"] = Math.Round(100 * Math.Abs((Double)intraDict[kvp.Key]["TotalShortValue"] / abstotalval), 2);

                    //Calculate Volume:TransactionFrequncy
                    double vf = ((Double)intraDict[kvp.Key]["TotalLongVolume"] + Math.Abs((Double)intraDict[kvp.Key]["TotalShortVolume"]))/tradeVolume.Count();
                    intraDict[kvp.Key]["VF"] = Math.Round(vf, 2);

                    //Store for prevRow
                    prevRow = intraDict[kvp.Key];
                }
                else//Fill in empty cell
                {
                    intraDict[kvp.Key]["Open"] = prevRow["Close"];
                    intraDict[kvp.Key]["High"] = prevRow["Close"];
                    intraDict[kvp.Key]["Low"] = prevRow["Close"];
                    intraDict[kvp.Key]["Close"] = prevRow["Close"];
                    intraDict[kvp.Key]["Volume"] = 0;
                    intraDict[kvp.Key]["Value"] = 0;
                    intraDict[kvp.Key]["SD"] = 0;
                    intraDict[kvp.Key]["VF"] = 0;
               
                }
                //Add Completed Row (non empty cell) to the result table
                dt.Rows.Add(intraDict[kvp.Key].ItemArray);
                
            }
            
        }
        else
        {
            dt = bar;
        }
        return dt;
    }
    public DataTable GetResamplingBarsTableFillEmpty(int secondinterval, DataTable bar)
    {
        DataTable dt = GetResamplingBarsTable(secondinterval, bar);
        DataTable dtDate = bar.DefaultView.ToTable(true, "<DATE>");
        
        Dictionary<DateTime, DataRow> intraDict = this.IntradayTimeTradeDict(dtDate, secondinterval);
        
        //Fill row to timeline
        if (dt.Rows.Count > 0)
        {
            foreach (DataRow r in dt.Rows)
            {
                //TimeSpan tradetime = TimeSpan.Parse(r["<TIME>"].ToString());
                DateTime tradetime = ((DateTime)r["Date"]).Add((TimeSpan)r["Time"]);
                intraDict[tradetime] = r;
            }
            
        }
        //Refill empty row
        double prevclose = -1;
        DataTable fillemptydt = dt.Clone();
        DateTime tradedate = this.ToDateTime(dtDate.Rows[0]["<DATE>"].ToString(), "yyyyMMdd");
        foreach (KeyValuePair<DateTime,DataRow> kvp in intraDict)
        {
            if ((kvp.Value["Volume"] != DBNull.Value))
            {
                fillemptydt.Rows.Add(kvp.Value.ItemArray);
                prevclose = (double)kvp.Value["Close"];
                
            }
            else if (prevclose > 0)
            {
                DataRow tmprow = dt.NewRow();
                //tmprow["Date"] = tradedate;

                tmprow["Date"] = kvp.Key.Date;
                tmprow["Time"] = kvp.Key.TimeOfDay;
                tmprow["Open"] = prevclose;
                tmprow["High"] = prevclose;
                tmprow["Low"] = prevclose;
                tmprow["Close"] = prevclose;
                tmprow["Volume"] = 0;
                tmprow["Value"] = 0;
                tmprow["SD"] = 0;
                tmprow["VF"] = 0;
                fillemptydt.Rows.Add(tmprow.ItemArray);
            }
        }
        return fillemptydt;
    }
    public DataTable GetResamplingBars(int secondinterval, DataTable bars)
    {
        DataTable dtAll = new DataTable(bars.TableName);

        //Get List of Trade date from bar table
        DataTable dtDate = bars.DefaultView.ToTable(true, "<DATE>");
        DataView dv = new DataView(bars);
        
        foreach (DataRow rDate in dtDate.Rows)
        {
            string tmpDate = rDate["<DATE>"].ToString();
            //Split each day
            DataTable bar = bars.AsEnumerable()
                .Where(row => row.Field<String>("<DATE>") == tmpDate)
                .CopyToDataTable();
            DataTable dt = new DataTable();

            if (secondinterval > 0)
            {

                Dictionary<TimeSpan, DataRow> intraDict = this.IntradayTimeTradeDict(secondinterval);
                //Dictionary<DateTime, DataRow> intraDict = this.IntradayTimeTradeDict(dtDate, secondinterval);
                Dictionary<TimeSpan, List<DataRow>> tradetimeDict = new Dictionary<TimeSpan, List<DataRow>>();
                //Dictionary<TimeSpan, List<Double>> priceDict = new Dictionary<TimeSpan, List<Double>>();


                //Prepare DataTable for create DataRow for tradetime Dict
                DataTable tmptradedt = new DataTable();
                DataColumn[] tradecol = new DataColumn[3];
                tradecol[0] = new DataColumn("Close", typeof(System.Double));
                tradecol[1] = new DataColumn("Volume", typeof(System.Double));
                tradecol[2] = new DataColumn("Value", typeof(System.Double));
                tmptradedt.Columns.AddRange(tradecol);

                //Grouping into each trade tine period
                foreach (DataRow r in bar.Rows)
                {

                    TimeSpan tradetime = TimeSpan.Parse(r["<TIME>"].ToString());
                    //DateTime tradetime = this.ToDateTime(r["<DATE>"].ToString(), "yyyyMMdd").Add(TimeSpan.Parse(r["<TIME>"].ToString()));
                    //DateTime tradedate = (DateTime)r["<DATE>"].
                    DataRow tradedr = tmptradedt.NewRow();
                    //Round up to most nearest interval
                    //tradetime.TimeOfDay

                    tradetime = this.RoundTo(tradetime, secondinterval);
                    DataRow tmprow = intraDict[tradetime];
                    double tmpprice = Double.Parse(r["<CLOSE>"].ToString());
                    double tmpvol=0;
                    if(r["<VOL>"].ToString().TrimEnd(null).EndsWith("M"))
                        tmpvol = 1000000*Double.Parse( r["<VOL>"].ToString().Remove(r["<VOL>"].ToString().LastIndexOf('M'), 1) );
                    else tmpvol = Double.Parse(r["<VOL>"].ToString());

                    //negative volume in short side
                    if (r["<OPENINT>"].ToString().Trim() == "S")
                        tmpvol = -1 * tmpvol;
                    //Caculate value1
                    double tmpval = tmpprice * tmpvol;

                    tradedr["Close"] = tmpprice;
                    tradedr["Volume"] = tmpvol;
                    tradedr["Value"] = tmpval;
                    
                    
                    if (!tradetimeDict.ContainsKey(tradetime))
                        tradetimeDict.Add(tradetime, new List<DataRow>());
                    tradetimeDict[tradetime].Add(tradedr);



                    //Check first resampling this interval
                    //Open
                    if (tmprow["Open"] == DBNull.Value)
                    {
                        tmprow["Open"] = tmpprice;
                    }
                    //High
                    if (tmprow["High"] == DBNull.Value)
                    {
                        tmprow["High"] = tmpprice;
                    }
                    else if (tmpprice > (Double)tmprow["High"])
                        tmprow["High"] = tmpprice;
                    //Low
                    if (tmprow["Low"] == DBNull.Value)
                    {
                        tmprow["Low"] = tmpprice;
                    }
                    else if (tmpprice < (Double)tmprow["Low"])
                        tmprow["Low"] = tmpprice;
                    //Close
                    tmprow["Close"] = tmpprice;
                    //Volume
                    if (tmprow["Volume"] == DBNull.Value)
                    {
                        tmprow["Volume"] = tmpvol;
                    }
                    else tmprow["Volume"] = (Double)tmprow["Volume"] + tmpvol;
                    //Value
                    if (tmprow["Value"] == DBNull.Value)
                    {
                        tmprow["Value"] = tmpval;
                    }
                    else tmprow["Value"] = (Double)tmprow["Value"] + tmpval;




                    //Assign new resampling value to the row
                    intraDict[tradetime] = tmprow;

                }
                DataRow prevRow = dt.NewRow();
                foreach (KeyValuePair<TimeSpan, List<DataRow>> kvp in tradetimeDict)
                {

                    //Initial Colume for dt (result table)
                    if (dt.Columns.Count == 0)
                        dt = intraDict[kvp.Key].Table.Clone();

                    //Add Date & Time
                    intraDict[kvp.Key]["Date"] = this.ToDateTime(bar.Rows[0]["<DATE>"].ToString(), "yyyyMMdd").Add(kvp.Key);
                    //intraDict[kvp.Key]["Date"] = kvp.Key;
                    intraDict[kvp.Key]["Time"] = kvp.Key;

                    if (kvp.Value.Count > 0)
                    {


                        //Loop for cal each transaction in one tradetime period
                        List<Double> tradeClose = new List<double>();
                        List<Double> tradeVolume = new List<double>();
                        List<Double> tradeValue = new List<double>();
                        foreach (DataRow r in kvp.Value)
                        {
                            tradeClose.Add((Double)r["Close"]);
                            tradeVolume.Add((Double)r["Volume"]);
                            tradeValue.Add((Double)r["Value"]);
                        }

                        //Calculate Standard Deviation
                        intraDict[kvp.Key]["SD"] = Math.Round(Statistics.StandardDeviation(tradeClose.ToArray()), 3);

                        //Calculate Max Long/Short Volume
                        if (tradeVolume.Max() >= 0)
                            intraDict[kvp.Key]["MaxLongVolume"] = tradeVolume.Max();
                        else intraDict[kvp.Key]["MaxLongVolume"] = 0;
                        if (tradeVolume.Min() < 0)
                            intraDict[kvp.Key]["MaxShortVolume"] = tradeVolume.Min();
                        else intraDict[kvp.Key]["MaxShortVolume"] = 0;

                        //Calculate Max Long/Short Value
                        if (tradeValue.Max() >= 0)
                            intraDict[kvp.Key]["MaxLongValue"] = tradeValue.Max();
                        else intraDict[kvp.Key]["MaxLongValue"] = 0;
                        if (tradeValue.Min() < 0)
                            intraDict[kvp.Key]["MaxShortValue"] = tradeValue.Min();
                        else intraDict[kvp.Key]["MaxShortValue"] = 0;

                        //Calculate Total Long/Short Value
                        foreach (double vol in tradeVolume)
                        {
                            if (intraDict[kvp.Key]["TotalLongVolume"] == DBNull.Value)
                                intraDict[kvp.Key]["TotalLongVolume"] = 0;
                            if (intraDict[kvp.Key]["TotalShortVolume"] == DBNull.Value)
                                intraDict[kvp.Key]["TotalShortVolume"] = 0;

                            if (vol >= 0) //Long
                                intraDict[kvp.Key]["TotalLongVolume"] = (Double)intraDict[kvp.Key]["TotalLongVolume"] + vol;
                            else //Short
                                intraDict[kvp.Key]["TotalShortVolume"] = (Double)intraDict[kvp.Key]["TotalShortVolume"] + vol;
                        }

                        //Calculate Total Long/Short Value
                        foreach (double val in tradeValue)
                        {
                            if (intraDict[kvp.Key]["TotalLongValue"] == DBNull.Value)
                                intraDict[kvp.Key]["TotalLongValue"] = 0;
                            if (intraDict[kvp.Key]["TotalShortValue"] == DBNull.Value)
                                intraDict[kvp.Key]["TotalShortValue"] = 0;

                            if (val >= 0) //Long
                                intraDict[kvp.Key]["TotalLongValue"] = (Double)intraDict[kvp.Key]["TotalLongValue"] + val;
                            else //Short
                                intraDict[kvp.Key]["TotalShortValue"] = (Double)intraDict[kvp.Key]["TotalShortValue"] + val;
                        }
                        //Calculate %Volume Long/short
                        double abstotalvol = (Double)intraDict[kvp.Key]["TotalLongVolume"] + Math.Abs((Double)intraDict[kvp.Key]["TotalShortVolume"]);
                        intraDict[kvp.Key]["%Volume Long"] = Math.Round(100 * (Double)intraDict[kvp.Key]["TotalLongVolume"] / abstotalvol, 2);
                        intraDict[kvp.Key]["%Volume Short"] = Math.Round(100 * Math.Abs((Double)intraDict[kvp.Key]["TotalShortVolume"] / abstotalvol), 2);

                        //Calculate %Value Long/short
                        double abstotalval = (Double)intraDict[kvp.Key]["TotalLongValue"] + Math.Abs((Double)intraDict[kvp.Key]["TotalShortValue"]);
                        intraDict[kvp.Key]["%Value Long"] = Math.Round(100 * (Double)intraDict[kvp.Key]["TotalLongValue"] / abstotalval, 2);
                        intraDict[kvp.Key]["%Value Short"] = Math.Round(100 * Math.Abs((Double)intraDict[kvp.Key]["TotalShortValue"] / abstotalval), 2);

                        //Calculate Volume:TransactionFrequncy
                        double vf = ((Double)intraDict[kvp.Key]["TotalLongVolume"] + Math.Abs((Double)intraDict[kvp.Key]["TotalShortVolume"])) / tradeVolume.Count();
                        intraDict[kvp.Key]["VF"] = Math.Round(vf, 2);

                        //Store for prevRow
                        prevRow = intraDict[kvp.Key];
                    }
                    else//Fill in empty cell
                    {
                        intraDict[kvp.Key]["Open"] = prevRow["Close"];
                        intraDict[kvp.Key]["High"] = prevRow["Close"];
                        intraDict[kvp.Key]["Low"] = prevRow["Close"];
                        intraDict[kvp.Key]["Close"] = prevRow["Close"];
                        intraDict[kvp.Key]["Volume"] = 0;
                        intraDict[kvp.Key]["Value"] = 0;
                        intraDict[kvp.Key]["SD"] = 0;
                        intraDict[kvp.Key]["VF"] = 0;

                    }
                    //Add Completed Row (non empty cell) to the result table
                    dt.Rows.Add(intraDict[kvp.Key].ItemArray);

                }

            }
            else
            {
                dt = bar;
            }
            //Insert each row for each day to return DataTable
            if (dtAll.Columns.Count == 0)
                dtAll = dt.Clone();
            else foreach (DataRow r in dt.Rows)
                    dtAll.Rows.Add(r.ItemArray);
        }
        return dtAll;
    }
    public DataTable CalculateTechnicalIndicator(DataTable tradeTable, string[] indicName,int period)
    {
        //Get List of Trade date from bar table
        DataTable dtAll = new DataTable(tradeTable.TableName);
        DataTable dtDate = tradeTable.DefaultView.ToTable(true, "Date");
        
        foreach (DataRow rdate in dtDate.Rows)
        {
            DateTime tmpDate = ((DateTime)rdate["Date"]);
            /*DataTable dt = tradeTable.AsEnumerable()
                .Where(row => row.Field<DateTime>("Date") == tmpDate)
                .CopyToDataTable();*/
            DataTable dt = (from DataRow dr in tradeTable.Rows
                                where (DateTime)dr["Date"] == tmpDate
                                select dr).CopyToDataTable();
            //string periodtxt = " (p=" + period.ToString()+")";
            double sumshortvol = 0;
            double sumlongvol = 0;
            double sumlongval = 0;
            double sumshortval = 0;
            double prevhigh = 0;
            double prevlow = 0;
            double sumexpprice =0;
            for (int i = 0; i < dt.Rows.Count; i++)
            {

                //prepare sum Long/Short Volume
                sumlongvol = sumlongvol + (Double)dt.Rows[i]["TotalLongVolume"];
                sumshortvol = sumshortvol + Math.Abs((Double)dt.Rows[i]["TotalShortVolume"]);
                sumlongval = sumlongval + (Double)dt.Rows[i]["TotalLongValue"];
                sumshortval = sumshortval + Math.Abs((Double)dt.Rows[i]["TotalShortValue"]);
                sumexpprice = (sumlongval-sumshortval)/(sumlongvol-sumshortvol);

                //prepare for Swing

                for (int j = 0; j < indicName.Length; j++)
                {
                    if (i == 0)
                    {//Add Column
                        string colname = indicName[j];
                        if (!dt.Columns.Contains(colname))//Check existing column
                            dt.Columns.Add(colname, typeof(System.Double));
                    }

                    //Check each calculation
                    //Change %
                    if (indicName[j] == "Change%")
                    {
                        double pchange = 100 * ((Double)dt.Rows[i]["Close"] - (Double)dt.Rows[i]["Open"]) / (Double)dt.Rows[i]["Open"];
                        string colname = indicName[j];
                        dt.Rows[i][colname] = Math.Round(pchange, 3);
                    }

                    //Volatility% 
                    if (indicName[j] == "Volatility")
                    {
                        //not cal if less than require period
                        if (i >= period)
                        {
                            List<Double> val = new List<double>();
                            for (int k = 1; k < period; k++)
                            {
                                double pchange = 100 * ((Double)dt.Rows[i - k]["Close"] - (Double)dt.Rows[i - k]["Open"]) / (Double)dt.Rows[i - k]["Open"];
                                val.Add(pchange);
                                double volat = Math.Sqrt(period) * Statistics.StandardDeviation(val.ToArray());
                                string colname = indicName[j];
                                dt.Rows[i][colname] = Math.Round(volat, 3);
                            }
                        }
                    }
                    //Volume per time 
                    if (indicName[j] == "TotalLongVolumePerTime")
                    {
                        //not cal if less than require period

                        if (i >= period)
                        {
                            double sec = ((TimeSpan)dt.Rows[i]["Time"] - (TimeSpan)dt.Rows[i - 1]["Time"]).TotalSeconds;
                            double periodvolume = 0;
                            for (int k = 1; k < period; k++)
                            {
                                periodvolume = periodvolume + (Double)dt.Rows[i - k]["TotalLongVolume"];

                            }
                            string colname = indicName[j];
                            dt.Rows[i][colname] = Math.Round(periodvolume / (period * sec), 1);
                        }
                    }
                    if (indicName[j] == "TotalShortVolumePerTime")
                    {
                        //not cal if less than require period

                        if (i >= period)
                        {
                            double sec = ((TimeSpan)dt.Rows[i]["Time"] - (TimeSpan)dt.Rows[i - 1]["Time"]).TotalSeconds;
                            double periodvolume = 0;
                            for (int k = 1; k < period; k++)
                            {
                                periodvolume = periodvolume + (Double)dt.Rows[i - k]["TotalShortVolume"];

                            }
                            string colname = indicName[j];
                            dt.Rows[i][colname] = Math.Round(periodvolume / (period * sec), 1);
                        }
                    }
                    if (indicName[j] == "TotalVolumePerTime")
                    {
                        if (i >= period)
                        {
                            string colname = indicName[j];
                            dt.Rows[i][colname] = Math.Round((Double)dt.Rows[i]["TotalLongVolumePerTime"] + Math.Abs((Double)dt.Rows[i]["TotalShortVolumePerTime"]),2);
                        }
            
                    }
                    /*
                    //PeriodHigh% 
                    if (indicName[j] == "PeriodHigh")
                    {
                        //not cal if less than require period
                        if (i >= period)
                        {
                            double periodhigh = 0;
                            for (int k = 1; k < period; k++)
                            {
                                if (periodhigh == 0 || periodhigh < (Double)dt.Rows[i - k]["High"])
                                    periodhigh = (Double)dt.Rows[i - k]["High"];
                            }
                            string colname = indicName[j];
                            dt.Rows[i][colname] = periodhigh;
                        }
                    }
                    //PeriodLow% 
                    
                    if (indicName[j] == "PeriodLow")
                    {
                        //not cal if less than require period
                        if (i >= period)
                        {
                            double periodlow = 0;
                            for (int k = 1; k < period; k++)
                            {
                                if (periodlow == 0 || periodlow > (Double)dt.Rows[i - k]["Low"])
                                    periodlow = (Double)dt.Rows[i - k]["Low"];
                            }
                            string colname = indicName[j];
                            dt.Rows[i][colname] = periodlow;
                        }
                    }
                    */
                    //Sum %Short/Long
                    if (indicName[j] == "SumLongVolume%")
                    {
                        string colname = indicName[j];
                        dt.Rows[i][colname] = Math.Round(sumlongvol / (sumlongvol + sumshortvol) * 100, 2);
                    }
                    if (indicName[j] == "SumShortVolume%")
                    {
                        string colname = indicName[j];
                        dt.Rows[i][colname] = Math.Round(sumshortvol / (sumlongvol + sumshortvol) * 100, 2);
                    }
                    //GapHighLow
                    if (indicName[j] == "GapHighLow")
                    {
                        string colname = indicName[j];
                        if (prevhigh == 0 || prevhigh < (Double)dt.Rows[i]["High"])
                            prevhigh = (Double)dt.Rows[i]["High"];
                        if (prevlow == 0 || prevlow > (Double)dt.Rows[i]["Low"])
                            prevlow = (Double)dt.Rows[i]["Low"];
                        dt.Rows[i][colname] = Math.Round(prevhigh - prevlow, 2);
                    }
                    
                    //PeriodGapHighLow
                    if (indicName[j] == "PeriodGapHighLow")
                    {
                        //not cal if less than require period
                        if (i >= period)
                        {
                            string colname = indicName[j];
                            
                            double periodlow = 0;
                            double periodhigh = 0;
                            for (int k = 1; k < period; k++)
                            {
                                if (periodhigh == 0 || periodhigh < (Double)dt.Rows[i-k]["High"])
                                    periodhigh = (Double)dt.Rows[i-k]["High"];
                                if (periodlow == 0 || periodlow > (Double)dt.Rows[i-k]["Low"])
                                    periodlow = (Double)dt.Rows[i-k]["Low"];
                            }

                            dt.Rows[i][colname] = Math.Round(periodhigh - periodlow, 2);
                            
                            if(indicName.Contains("PeriodHigh"))
                            {
                                dt.Rows[i]["PeriodHigh"] = periodhigh;
                            }
                            if (indicName.Contains("PeriodLow"))
                            {
                                dt.Rows[i]["PeriodLow"] = periodlow;
                            }
                            
                            
                        }
                        
                    }
                    //ExpectPrice
                    if (indicName[j] == "ExpectPrice")
                    {
                        string colname = indicName[j];
                        dt.Rows[i][colname] = Math.Round(sumexpprice,2);
                    }
                    //DayHigh
                    if (indicName[j] == "DayHigh")
                    {
                        string colname = indicName[j];
                        dt.Rows[i][colname] = prevhigh;
                    }
                    //DayLow
                    if (indicName[j] == "DayLow")
                    {
                        string colname = indicName[j];
                        dt.Rows[i][colname] = prevlow;
                    }
                    
                }
            }
            //insert each day calculate into return DataTable
            if (dtAll.Columns.Count <= 0)
                dtAll = dt.Clone();
            foreach (DataRow rdt in dt.Rows)
                dtAll.Rows.Add(rdt.ItemArray);
        }
        return dtAll;
    }
    public DataTable GetBestTrade(DataTable tradeDataTable,double gain,double lost,double interval)
    {
        DataTable dt = this.OrderTable();
        for (int i = 0; i < tradeDataTable.Rows.Count; i++)
        {
            
            for (int j = i; j < tradeDataTable.Rows.Count; j++)
            {
                DataRow tmpdr = dt.NewRow();
                //Gain
                if ((Double)tradeDataTable.Rows[j]["Close"] - (Double)tradeDataTable.Rows[i]["Open"] <= -1 * gain)
                {
                    dt.Rows.Add(tmpdr);
                    tmpdr["Date"] = tradeDataTable.Rows[i]["Date"];
                    tmpdr["OpenPosition"] = "S";
                    tmpdr["OpenTime"] = tradeDataTable.Rows[i]["Time"];
                    tmpdr["OpenPrice"] = tradeDataTable.Rows[i]["Open"];
                    tmpdr["ClosePosition"] = "L";
                    tmpdr["CloseTime"] = tradeDataTable.Rows[j]["Time"];
                    tmpdr["ClosePrice"] = tradeDataTable.Rows[j]["Close"];
                    tmpdr["Interval"] = interval;
                    break;
                }
                //Lost
                if (Math.Abs((Double)tradeDataTable.Rows[j]["Close"] - (Double)tradeDataTable.Rows[i]["Open"]) >= lost)
                {
                    break;
                }
            }
            //Long side
            for (int j = i; j < tradeDataTable.Rows.Count; j++)
            {
                DataRow tmpdr = dt.NewRow();
                //Gain
                if ((Double)tradeDataTable.Rows[j]["Close"] - (Double)tradeDataTable.Rows[i]["Open"] >= gain)
                {
                    dt.Rows.Add(tmpdr);
                    tmpdr["Date"] = tradeDataTable.Rows[i]["Date"];
                    tmpdr["OpenPosition"] = "L";
                    tmpdr["OpenTime"] = tradeDataTable.Rows[i]["Time"];
                    tmpdr["OpenPrice"] = tradeDataTable.Rows[i]["Open"];
                    tmpdr["ClosePosition"] = "S";
                    tmpdr["CloseTime"] = tradeDataTable.Rows[j]["Time"];
                    tmpdr["ClosePrice"] = tradeDataTable.Rows[j]["Close"];
                    tmpdr["Interval"] = interval;
                    break;
                }
                //Lost
                if ((Double)tradeDataTable.Rows[j]["Close"] - (Double)tradeDataTable.Rows[i]["Open"] <= -1*lost)
                {
                    break;
                }
            }
            //Short side
            
        }
        return dt;

    }
    public DataTable GetPrevTrade(DataTable tradeDataTable, DataTable queryTable, int previousperiod)
    {
        DataTable dt = tradeDataTable.Clone();
        List<TimeSpan> queryList = new List<TimeSpan>();
        foreach (DataRow r in queryTable.Rows)
        {
            TimeSpan ts = (TimeSpan)r["OpenTime"];
            for (int i = 0; i < previousperiod; i++)
            {
                ts = ts-TimeSpan.FromSeconds(((Double)r["Interval"]));
                queryList.Add(ts);
            }

        }
        //Query
        foreach (DataRow r in tradeDataTable.Rows)
            if (queryList.Contains((TimeSpan)r["Time"]))
                dt.Rows.Add(r.ItemArray);
        
        return dt;
    }
    private DataTable OrderTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[8];
        col[0] = new DataColumn("Date", typeof(System.DateTime));
        col[1] = new DataColumn("OpenPosition", typeof(System.String));
        col[2] = new DataColumn("OpenTime", typeof(System.TimeSpan));
        col[3] = new DataColumn("OpenPrice", typeof(System.Double));
        col[4] = new DataColumn("ClosePosition", typeof(System.String));
        col[5] = new DataColumn("CloseTime", typeof(System.TimeSpan));
        col[6] = new DataColumn("ClosePrice", typeof(System.Double));
        col[7] = new DataColumn("Interval", typeof(System.Double));
        dt.Columns.AddRange(col);
        return dt;
    }
    private Dictionary<TimeSpan, DataRow> IntradayTimeTradeDict(int secondinterval)
    {
        Dictionary<TimeSpan, DataRow> intraDict = new Dictionary<TimeSpan, DataRow>();
        if (secondinterval > 0)
        {
            TimeSpan interval = new TimeSpan(0, 0, secondinterval);
            //Start trade time
            TimeSpan startTradeTimeMorning = new TimeSpan(9, 45, 0);
            //End trade time
            TimeSpan endTradeTimeMorning = new TimeSpan(12, 30, 0);

            TimeSpan startTradeTimeAfternoon = new TimeSpan(14, 15, 0);
            TimeSpan endTradeTimeAfternoon = new TimeSpan(17, 00, 0);
            DataTable dt = new DataTable();
            DataColumn[] col = new DataColumn[22];
            col[0] = new DataColumn("Date", typeof(System.DateTime));
            col[1] = new DataColumn("Time", typeof(System.TimeSpan));
            col[2] = new DataColumn("Open", typeof(System.Double));
            col[3] = new DataColumn("High", typeof(System.Double));
            col[4] = new DataColumn("Low", typeof(System.Double));
            col[5] = new DataColumn("Close", typeof(System.Double));
            col[6] = new DataColumn("Volume", typeof(System.Double));
            col[7] = new DataColumn("Value", typeof(System.Double));
            col[8] = new DataColumn("MaxLongVolume", typeof(System.Double));
            col[9] = new DataColumn("MaxShortVolume", typeof(System.Double));
            col[10] = new DataColumn("MaxLongValue", typeof(System.Double));
            col[11] = new DataColumn("MaxShortValue", typeof(System.Double));
            col[12] = new DataColumn("TotalLongVolume", typeof(System.Double));
            col[13] = new DataColumn("TotalShortVolume", typeof(System.Double));
            col[14] = new DataColumn("TotalLongValue", typeof(System.Double));
            col[15] = new DataColumn("TotalShortValue", typeof(System.Double));
            col[16] = new DataColumn("%Volume Long", typeof(System.Double));
            col[17] = new DataColumn("%Volume Short", typeof(System.Double));
            col[18] = new DataColumn("%Value Long", typeof(System.Double));
            col[19] = new DataColumn("%Value Short", typeof(System.Double));
            col[20] = new DataColumn("SD", typeof(System.Double));
            col[21] = new DataColumn("VF", typeof(System.Double));
            


            dt.Columns.AddRange(col);
           
            //Create Morning interval Dict
            while (startTradeTimeMorning <= endTradeTimeMorning)
            {
                intraDict.Add(startTradeTimeMorning, dt.NewRow());
                startTradeTimeMorning = startTradeTimeMorning.Add(interval);
            }
            //Create Morning interval Dict
            while (startTradeTimeAfternoon <= endTradeTimeAfternoon)
            {
                intraDict.Add(startTradeTimeAfternoon, dt.NewRow());
                startTradeTimeAfternoon = startTradeTimeAfternoon.Add(interval);
            }
        }
        return intraDict;
        
    }
    private Dictionary<DateTime, DataRow> IntradayTimeTradeDict(DataTable dateListTable,int secondinterval)
    {
        Dictionary<DateTime, DataRow> intraDict = new Dictionary<DateTime, DataRow>();
        if (secondinterval > 0)
        {
            
            DataTable dt = new DataTable();
            DataColumn[] col = new DataColumn[30];
            col[0] = new DataColumn("Date", typeof(System.DateTime));
            col[1] = new DataColumn("Time", typeof(System.TimeSpan));
            col[2] = new DataColumn("Open", typeof(System.Double));
            col[3] = new DataColumn("High", typeof(System.Double));
            col[4] = new DataColumn("Low", typeof(System.Double));
            col[5] = new DataColumn("Close", typeof(System.Double));
            col[6] = new DataColumn("Volume", typeof(System.Double));
            col[7] = new DataColumn("Value", typeof(System.Double));
            col[8] = new DataColumn("MaxLongVolume", typeof(System.Double));
            col[9] = new DataColumn("MaxShortVolume", typeof(System.Double));
            col[10] = new DataColumn("MaxLongValue", typeof(System.Double));
            col[11] = new DataColumn("MaxShortValue", typeof(System.Double));
            col[12] = new DataColumn("TotalLongVolume", typeof(System.Double));
            col[13] = new DataColumn("TotalShortVolume", typeof(System.Double));
            col[14] = new DataColumn("TotalLongValue", typeof(System.Double));
            col[15] = new DataColumn("TotalShortValue", typeof(System.Double));
            col[16] = new DataColumn("%Volume Long", typeof(System.Double));
            col[17] = new DataColumn("%Volume Short", typeof(System.Double));
            col[18] = new DataColumn("%Value Long", typeof(System.Double));
            col[19] = new DataColumn("%Value Short", typeof(System.Double));
            col[20] = new DataColumn("SD", typeof(System.Double));
            col[21] = new DataColumn("VF", typeof(System.Double));
            col[22] = new DataColumn("Long TPS", typeof(System.Double));
            col[23] = new DataColumn("Short TPS", typeof(System.Double));
            col[24] = new DataColumn("SumShortVol Biglots", typeof(System.Double));
            col[25] = new DataColumn("SumLongVol Biglots", typeof(System.Double));
            col[26] = new DataColumn("SumShortTrans Biglots", typeof(System.Double));
            col[27] = new DataColumn("SumLongTrans Biglots", typeof(System.Double));
            col[28] = new DataColumn("AvgShortCost Biglots", typeof(System.Double));
            col[29] = new DataColumn("AvgLongCost Biglots", typeof(System.Double));

            dt.Columns.AddRange(col);

            foreach (DataRow dr in dateListTable.Rows)
            {
                TimeSpan interval = new TimeSpan(0, 0, secondinterval);
                //Start trade time
                TimeSpan startTradeTimeMorning = new TimeSpan(9, 45, 0);
                //End trade time
                TimeSpan endTradeTimeMorning = new TimeSpan(12, 30, 0);

                TimeSpan startTradeTimeAfternoon = new TimeSpan(14, 15, 0);
                TimeSpan endTradeTimeAfternoon = new TimeSpan(17, 00, 0);
                //Create Morning interval Dict
                DateTime tmpdate = this.ToDateTime(dr["<DATE>"].ToString(), "yyyyMMdd");

                while (startTradeTimeMorning <= endTradeTimeMorning)
                {
                    
                    intraDict.Add(tmpdate.Date+startTradeTimeMorning, dt.NewRow());
                    startTradeTimeMorning = startTradeTimeMorning.Add(interval);
                }
                //Create Morning interval Dict
                while (startTradeTimeAfternoon <= endTradeTimeAfternoon)
                {
                    intraDict.Add(tmpdate.Date + startTradeTimeAfternoon, dt.NewRow());
                    startTradeTimeAfternoon = startTradeTimeAfternoon.Add(interval);
                }
            }
        }
        return intraDict;

    }
    public DataTable GetClassifiedTable(DataTable bar)
    {
        bar.Columns.Add("Hash1", typeof(System.String));
        bar.Columns.Add("Hash2", typeof(System.String));

        for (int i = 0; i < bar.Rows.Count;i++ )
        {
            string hcode1 = "";

            double open = (Double)bar.Rows[i]["Open"];
            double high = (Double)bar.Rows[i]["High"];
            double low = (Double)bar.Rows[i]["Low"];
            double close = (Double)bar.Rows[i]["Close"];

            //Hash2
            //char 1
            if (open == high) hcode1 = "1";
            else hcode1 = "0";
            //char 2
            if (open == low) hcode1 = hcode1 + "1";
            else hcode1 = hcode1 + "0";
            //char 3
            if (open == close) hcode1 = hcode1 + "1";
            else hcode1 = hcode1 + "0";
            //char 4
            if (open > close) hcode1 = hcode1 + "1";
            else hcode1 = hcode1 + "0";
            //char 5
            if (open < close) hcode1 = hcode1 + "1";
            else hcode1 = hcode1 + "0";
            //char 6
            if (close == high) hcode1 = hcode1 + "1";
            else hcode1 = hcode1 + "0";
            //char 7
            if (close == low) hcode1 = hcode1 + "1";
            else hcode1 = hcode1 + "0";
            bar.Rows[i]["Hash1"] = hcode1;
            
            //Hash2
            if (i > 0)
            {
                string hcode2 = "";
                double prevopen = (Double)bar.Rows[i - 1]["Close"];
                double prevhigh = (Double)bar.Rows[i - 1]["Open"];
                double prevlow = (Double)bar.Rows[i - 1]["High"];
                double prevclose = (Double)bar.Rows[i - 1]["Low"];
                double prevdayhigh = (Double)bar.Rows[i - 1]["DayHigh"];
                double prevdaylow = (Double)bar.Rows[i - 1]["DayLow"];
                //char 1
                if (high > prevhigh) hcode2 = "1";
                else hcode2 = "0";
                //char 2
                if (low < prevlow) hcode2 = hcode2 + "1";
                else hcode2 = hcode2 + "0";
                //char 3
                if (close > prevhigh) hcode2 = hcode2 + "1";
                else hcode2 = hcode2 + "0";
                //char 4
                if (close < prevlow) hcode2 = hcode2 + "1";
                else hcode2 = hcode2 + "0";
                //char 5
                if (close > prevclose) hcode2 = hcode2 + "1";
                else hcode2 = hcode2 + "0";
                //char 6
                if (open > prevhigh) hcode2 = hcode2 + "1";
                else hcode2 = hcode2 + "0";
                //char 7
                if (open < prevlow) hcode2 = hcode2 + "1";
                else hcode2 = hcode2 + "0";
                //char 8
                if (open > prevopen) hcode2 = hcode2 + "1";
                else hcode2 = hcode2 + "0";
                //char 9
                if (high > prevdayhigh) hcode2 = hcode2 + "1";
                else hcode2 = hcode2 + "0";
                //char 10
                if (low < prevdaylow) hcode2 = hcode2 + "1";
                else hcode2 = hcode2 + "0";
                //char 11
                if (close > prevdayhigh) hcode2 = hcode2 + "1";
                else hcode2 = hcode2 + "0";
                //char 12
                if (close < prevdaylow) hcode2 = hcode2 + "1";
                else hcode2 = hcode2 + "0";

                bar.Rows[i]["Hash2"] = hcode2;
            }
            else bar.Rows[i]["Hash2"] = "000000000000"; //12 chars
        }
        
        return bar;
    }
    public void WriteDataTableToDB(string tablename, DataTable dattable)
    {
        _mssqlAdapter.CreateFromDataTable(tablename, dattable);
        _mssqlAdapter.InsertUpdateDataToTable(dattable, tablename);
    }
    #region private method
    private DateTime ToDateTime(string strdate, string format)
    {
        return DateTime.ParseExact(strdate, format, CultureInfo.InvariantCulture);
    }
    private string ToDateTime(DateTime dtDate, string format)
    {
        return dtDate.ToString(format, CultureInfo.InvariantCulture);
    }
    private TimeSpan RoundTo(TimeSpan timeSpan, int n)
    {
        /*
        TimeSpan roundtime = new TimeSpan();
        foreach(TimeSpan t in listtime)
            if (t >= timeSpan)
            {
                roundtime = t;
                break;
            }
        return roundtime;
        */
        return TimeSpan.FromSeconds(n * Math.Ceiling(timeSpan.TotalSeconds / n));
    }
    #endregion
}

