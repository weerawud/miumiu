using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

class Portfolio
{
    public DataTable PortfolioTable;
    private DataTable PortfolioSummaryTable;
    private string _constring;
    private MSSQLAdapter _portdb;
    private string _accountname;
    public Dictionary<string, DataTable> tradeResamplingTableDict = new Dictionary<string, DataTable>();
    public Portfolio(string accountname)
    {
        //Init and test connect DB
        _constring = @"server="+System.Environment.MachineName+@"\WiiSQL;Initial Catalog=PortfolioDB;User ID=sa;Password=wee@dcs.c0m";
        _portdb = new MSSQLAdapter(_constring);
        _accountname = accountname;
        //this.PortfolioTable = this.GetNewPortfolioDatafromDB(accountname);
        this.PortfolioTable = this.GetExistingPortfolioDatafromDB(accountname);
        
    }
    private DataTable GetExistingPortfolioDatafromDB(string accountname)
    {
        DataTable dt = new DataTable();
        if (!_portdb.CheckExistingTable(accountname))
            _portdb.CreateFromDataTable(accountname, this.NewPortfolioAccount(accountname));
        dt = _portdb.GetPortfolioFromDB(accountname);
        return dt;
    }
    public void UpdatePortfolioTable(DataRow transDR, string symbol)
    {
        this.PortfolioTable.Rows.Add(transDR.ItemArray);

        this.GetPortfolioSummary(symbol);
    }
    
    public string GetOnhandStatus(string positionside,string symbol)
    {
        DataRow[] dr = this.PortfolioSummaryTable.Select(String.Format("Side='{0}'", positionside));
        string status = "none";
        double onhandvol = Double.Parse(dr[dr.Length - 1]["onhand vol"].ToString());
        if (onhandvol > 0)
            status = positionside;
        return status;
    }
    public double GetOnhandVolume(string positionside, string symbol)
    {
        DataRow[] dr = this.PortfolioSummaryTable.Select(String.Format("Side='{0}'", positionside));
        double onhandvol = Double.Parse(dr[dr.Length - 1]["onhand vol"].ToString());
        return onhandvol;
    }
    public double GetOnhandCost(string positionside, string symbol)
    {
        DataRow[] dr = this.PortfolioSummaryTable.Select(String.Format("Side='{0}'", positionside));
        double onhandcost = Double.Parse(dr[dr.Length - 1]["avg cost"].ToString());
        return onhandcost;
    }

    public DataTable GetPortfolioTradeDetails(string symbol)
    {
        DataTable portstatusTable = Portfolio.NewPortfolioStatus(_accountname);
        DataRow[] result = this.PortfolioTable.Select(String.Format("symbol='{0}'", symbol));
        Double totallongvol = 0;
        Double totalshortvol = 0;
        Double totallongval = 0;
        Double totalshortval = 0;
        Double avglongprice = 0;
        Double avgshortprice = 0;
        Double rpllong = 0;
        Double rplshort = 0;
        Double numlongtranc = 0;
        Double numlonggain = 0;
        Double numlongloss = 0;
        Double numshorttranc = 0;
        Double numshortgain = 0;
        Double numshortloss = 0;
        double tmpvol, tmpval, tmpprice, tmpcomm;
        string opentime = "";
        string opendate = "";
        DateTime tdate;
        //Assign Summary Long and Short
        DataTable portstat = Portfolio.NewPortfolioTradeDetails(_accountname);
        for (int i = 0; i < result.Length; i++)
        {
            tdate = (DateTime)(result[i]["datetime"]);
            tmpvol = Double.Parse(result[i]["volume"].ToString());
            tmpprice = Double.Parse(result[i]["price"].ToString());
            tmpcomm = Double.Parse(result[i]["commision"].ToString());
            tmpval = tmpprice * tmpvol;

            rpllong = 0;
            rplshort = 0;
            numlongtranc = 0;
            numlonggain = 0;
            numlongloss = 0;
            numshorttranc = 0;
            numshortgain = 0;
            numshortloss = 0;

            switch (result[i]["status"].ToString())
            {
                case "openlong":
                    {
                        totallongvol = totallongvol + tmpvol;
                        totallongval = totallongval + tmpval;
                        avglongprice = totallongval / totallongvol;
                        opentime = tdate.ToShortTimeString();
                        opendate = tdate.ToShortDateString();
                        
                        
                        break;
                    }
                case "closelong":
                    {
                        
                        //Long
                        DataRow longdr = portstat.NewRow();
                        longdr["open date"] = opendate;
                        longdr["symbol"] = symbol;
                        longdr["open time"] = opentime;
                        longdr["open side"] = "long";
                        longdr["total openvol"] = totallongvol;
                        longdr["avg cost"] = avglongprice;
                        longdr["total openval"] = totallongval;

                        //rpllong = rpllong + ((tmpprice - avglongprice) * tmpvol);
                        rpllong = ((tmpprice - avglongprice) * tmpvol);
                        totallongvol = totallongvol - tmpvol;
                        if (totallongvol == 0)
                            totallongval = 0;
                        else totallongval = totallongval - tmpval;

                        //numlongtranc = numlongtranc + 1;
                        numlongtranc = 1;
                        if ((tmpprice - avglongprice) > 0)
                            numlonggain = 1;
                        //numlonggain = numlonggain + 1;
                        else numlongloss = 1;//numlongloss = numlongloss + 1;
                        longdr["close date"] = tdate.ToShortDateString();
                        longdr["close time"] = tdate.ToShortTimeString();
                        longdr["close side"] = "short";
                        longdr["total closevol"] = tmpvol;
                        longdr["close price"] = tmpprice;
                        longdr["total closeval"] = tmpvol*tmpprice;
                        longdr["realize P/L"] = rpllong;
                        longdr["#transaction"] = numlongtranc;
                        longdr["#gain"] = numlonggain;
                        longdr["#loss"] = numlongloss;
                        longdr["%gain"] = 100 * (numlonggain / numlongtranc);
                        longdr["%loss"] = 100 * (numlongloss / numlongtranc);

                        portstat.Rows.Add(longdr);

                        break;
                    }
                case "openshort":
                    {
                        totalshortvol = totalshortvol + tmpvol;
                        totalshortval = totalshortval + tmpval;
                        avgshortprice = totalshortval / totalshortvol;
                        opentime = tdate.ToShortTimeString();
                        opendate = tdate.ToShortDateString();
                        break;
                    }
                case "closeshort":
                    {

                        //short
                        DataRow shortdr = portstat.NewRow();
                        shortdr["open date"] = opendate;
                        shortdr["symbol"] = symbol;
                        shortdr["open time"] = opentime;
                        shortdr["open side"] = "short";
                        shortdr["total openvol"] = totalshortvol;
                        shortdr["avg cost"] = avgshortprice;
                        shortdr["total openval"] = totalshortval;

                        //rplshort = rplshort + ((avgshortprice - tmpprice) * tmpvol);
                        rplshort = ((avgshortprice - tmpprice) * tmpvol);
                        totalshortvol = totalshortvol - tmpvol;
                        if (totalshortvol == 0)
                            totalshortval = 0;
                        else totalshortval = totalshortval - tmpval;

                        //numshorttranc = numshorttranc + 1;
                        numshorttranc = 1;
                        if ((avgshortprice - tmpprice) > 0)
                            numshortgain = 1;
                        //numshortgain = numshortgain + 1;
                        else numshortloss = 1;//numshortloss = numshortloss + 1;
                        shortdr["close date"] = tdate.ToShortDateString();
                        shortdr["close time"] = tdate.ToShortTimeString();
                        shortdr["close side"] = "long";
                        shortdr["total closevol"] = tmpvol;
                        
                        shortdr["close price"] = tmpprice;
                        shortdr["total closeval"] = tmpvol * tmpprice;
                        shortdr["realize P/L"] = rplshort;
                        shortdr["#transaction"] = numshorttranc;
                        shortdr["#gain"] = numshortgain;
                        shortdr["#loss"] = numshortloss;
                        shortdr["%gain"] = 100 * (numshortgain / numshorttranc);
                        shortdr["%loss"] = 100 * (numshortloss / numshorttranc);

                        portstat.Rows.Add(shortdr);

                        break;
                    }
            }

        }
        return portstat;
    }

    public DataTable GetPortfolioSummary(string symbol)
    {
        DataTable portstatusTable = Portfolio.NewPortfolioStatus(_accountname);
        DataRow[] result = this.PortfolioTable.Select(String.Format("symbol='{0}'",symbol));
        Double totallongvol = 0;
        Double totalshortvol = 0;
        Double totallongval = 0;
        Double totalshortval = 0;
        Double avglongprice = 0;
        Double avgshortprice = 0;
        Double rpllong = 0;
        Double rplshort = 0;
        string tradetime = "";
        double tmpvol, tmpval,tmpprice,tmpcomm;
        for (int i = 0; i < result.Length; i++)
        {
            tmpvol = Double.Parse(result[i]["volume"].ToString());
            tmpprice = Double.Parse(result[i]["price"].ToString());
            tmpcomm = Double.Parse(result[i]["commision"].ToString());
            tmpval = tmpprice * tmpvol;
            tradetime = ((DateTime)result[i]["datetime"]).ToShortTimeString();
            switch (result[i]["status"].ToString())
            {
                case "openlong":
                    {
                        totallongvol = totallongvol + tmpvol;
                        totallongval = totallongval + tmpval;
                        avglongprice = totallongval / totallongvol;
                        break;
                    }
                case "closelong":
                    {
                        rpllong = rpllong+((tmpprice - avglongprice) * tmpvol);
                        totallongvol = totallongvol - tmpvol;
                        if (totallongvol == 0)
                            totallongval = 0;
                        else totallongval = totallongval - tmpval;
                        break;
                    }
                case "openshort":
                    {
                        totalshortvol = totalshortvol + tmpvol;
                        totalshortval = totalshortval + tmpval;
                        avgshortprice = totalshortval / totalshortvol;
                        break;
                    }
                case "closeshort":
                    {
                        rplshort = rplshort + ((avgshortprice-tmpprice) * tmpvol);
                        totalshortvol = totalshortvol - tmpvol;
                        if (totalshortvol == 0)
                            totalshortval = 0;
                        else totalshortval = totalshortval - tmpval;
                        break;
                    }
            }
            
        }
        //Assign Summary Long and Short
        DataTable portstat = Portfolio.NewPortfolioStatus(_accountname);
        //Long
        DataRow longdr = portstat.NewRow();
        longdr["Symbol"] = symbol;
        longdr["Side"] = "openlong";
        longdr["Onhand vol"] = totallongvol;
        longdr["Avg cost"] = avglongprice;
        longdr["Total cost"] = totallongvol * avglongprice;
        longdr["Realize P/L"] = rpllong;
        longdr["Time"] = tradetime;
        //short
        DataRow shortdr = portstat.NewRow();
        shortdr["Symbol"] = symbol;
        shortdr["Side"] = "openshort";
        shortdr["Onhand vol"] = totalshortvol;
        shortdr["Avg cost"] = avgshortprice;
        shortdr["Total cost"] = totalshortvol * avgshortprice;
        shortdr["Realize P/L"] = rplshort;
        shortdr["Time"] = tradetime;
        portstat.Rows.Add(longdr);
        portstat.Rows.Add(shortdr);
        
        this.PortfolioSummaryTable = portstat;//update value to PortfolioSummaryTable
        return portstat;
    }

    public String GetPortfolioStatus(string accountname,string symbol,string positionside,double closeprice)
    {
        DataTable dt = this.GetPortfolioSummary(symbol);
        DataRow[] dr = dt.Select(String.Format("Side='{0}'",positionside));
        string status="none";
        double onhandvol = Double.Parse(dr[dr.Length - 1]["onhand vol"].ToString());
        if (onhandvol > 0)
            status = "open" + positionside;
        return status;
    }
    public DataRow GetSimulateSumwithHigLowPrice(DataRow PortTradeRow, DataTable ResampingTable)
    {

        //TimeSpan opentime = TimeSpan.Parse(PortTradeRow["open time"].ToString());
        //TimeSpan closetime = TimeSpan.Parse(PortTradeRow["close time"].ToString());
        //DateTime dateTime = DateTime.ParseExact(text,"hh:mm tt", CultureInfo.InvariantCulture);
        //DateTime dateopentime = DateTime.ParseExact(PortTradeRow["open time"].ToString(), "hh:mm tt", CultureInfo.InvariantCulture);
        TimeSpan opentime = Convert.ToDateTime(PortTradeRow["open time"].ToString()).TimeOfDay;
        TimeSpan closetime = Convert.ToDateTime(PortTradeRow["close time"].ToString()).TimeOfDay;
        double highprice = 0;
        string hightime = "";
        double lowprice = 0;
        string lowtime = "";

        foreach (DataRow r in ResampingTable.Rows)
        {
            if ((((TimeSpan)r["Time"]) >= opentime) && (((TimeSpan)r["Time"]) <= closetime))
            {
                if ((highprice == 0) || (double)r["High"] > highprice)
                {
                    highprice = (double)r["High"];
                    hightime = ((TimeSpan)r["Time"]).ToString();
                }
                if ((lowprice == 0) || (double)r["Low"] < lowprice)
                {
                    lowprice = (double)r["Low"];
                    lowtime = ((TimeSpan)r["Time"]).ToString();
                }
            }
            else if (((TimeSpan)r["Time"]) > closetime)
                break;
        }
        if (PortTradeRow["open side"].ToString() == "long")
        {
            double cost = Convert.ToDouble(PortTradeRow["avg cost"]);
            double openvol = Convert.ToDouble(PortTradeRow["total openvol"]);
            PortTradeRow["high P/L"] = openvol*(highprice-cost);
            PortTradeRow["low P/L"] = openvol * (lowprice - cost);
            PortTradeRow["high P/L time"] = hightime;
            PortTradeRow["low P/L time"] = lowtime;
        } else//short side
        {
            double cost = Convert.ToDouble(PortTradeRow["avg cost"]);
            double openvol = Convert.ToDouble(PortTradeRow["total openvol"]);
            PortTradeRow["high P/L"] = -openvol * (lowprice - cost);
            PortTradeRow["low P/L"] = -openvol * (highprice - cost);
            PortTradeRow["high P/L time"] = hightime;
            PortTradeRow["low P/L time"] = lowtime;
        }
        return PortTradeRow;
    }

    public DataRow GetSimulateSumwithHigLowPriceAllDate(DataRow PortTradeRow, DataTable ResampingTable)
    {

        //TimeSpan opentime = TimeSpan.Parse(PortTradeRow["open time"].ToString());
        //TimeSpan closetime = TimeSpan.Parse(PortTradeRow["close time"].ToString());
        //DateTime dateTime = DateTime.ParseExact(text,"hh:mm tt", CultureInfo.InvariantCulture);
        //DateTime dateopentime = DateTime.ParseExact(PortTradeRow["open time"].ToString(), "hh:mm tt", CultureInfo.InvariantCulture);
        TimeSpan opentime = Convert.ToDateTime(PortTradeRow["open time"].ToString()).TimeOfDay;
        TimeSpan closetime = Convert.ToDateTime(PortTradeRow["close time"].ToString()).TimeOfDay;
        double highprice = 0;
        string hightime = "";
        double lowprice = 0;
        string lowtime = "";

        foreach (DataRow r in ResampingTable.Rows)
        {
            if ((((TimeSpan)r["Time"]) >= opentime) && (((TimeSpan)r["Time"]) <= closetime))
            {
                if ((highprice == 0) || (double)r["High"] > highprice)
                {
                    highprice = (double)r["High"];
                    hightime = ((TimeSpan)r["Time"]).ToString();
                }
                if ((lowprice == 0) || (double)r["Low"] < lowprice)
                {
                    lowprice = (double)r["Low"];
                    lowtime = ((TimeSpan)r["Time"]).ToString();
                }
            }
            else if (((TimeSpan)r["Time"]) > closetime)
                break;
        }
        if (PortTradeRow["open side"].ToString() == "long")
        {
            double cost = Convert.ToDouble(PortTradeRow["avg cost"]);
            double openvol = Convert.ToDouble(PortTradeRow["total openvol"]);
            PortTradeRow["high P/L"] = openvol * (highprice - cost);
            PortTradeRow["low P/L"] = openvol * (lowprice - cost);
            PortTradeRow["high P/L time"] = hightime;
            PortTradeRow["low P/L time"] = lowtime;
        }
        else//short side
        {
            double cost = Convert.ToDouble(PortTradeRow["avg cost"]);
            double openvol = Convert.ToDouble(PortTradeRow["total openvol"]);
            PortTradeRow["high P/L"] = -openvol * (lowprice - cost);
            PortTradeRow["low P/L"] = -openvol * (highprice - cost);
            PortTradeRow["high P/L time"] = hightime;
            PortTradeRow["low P/L time"] = lowtime;
        }
        return PortTradeRow;
    }
    private static DataTable NewPortfolioStatus(string accountname)
    {
        DataTable dt = new DataTable(accountname);
        DataColumn[] col = new DataColumn[7];
        col[0] = new DataColumn("symbol", typeof(System.String));
        col[1] = new DataColumn("side", typeof(System.String));
        col[2] = new DataColumn("onhand vol", typeof(System.Double));//none,openlong,openshort,closelong,closeshort
        col[3] = new DataColumn("avg cost", typeof(System.Double));
        col[4] = new DataColumn("total cost", typeof(System.Double));
        col[5] = new DataColumn("realize P/L", typeof(System.Double));
        col[6] = new DataColumn("time", typeof(System.String));

        dt.Columns.AddRange(col);
        return dt;
    }
    private DataTable GetNewPortfolioDatafromDB(string accountname)
    {
        DataTable dt =new DataTable();
        _portdb.DeleteTable(accountname);
        _portdb.CreateFromDataTable(accountname,this.NewPortfolioAccount(accountname));
        dt = _portdb.GetPortfolioFromDB(accountname);
        return dt;
    }
    private void DropPortfolioTableOnDB(string accountname)
    {
        _portdb.DeleteTable(accountname);
    }
    private DataTable NewPortfolioAccount(string accountname)
    {
        DataTable dt = new DataTable(accountname);
        DataColumn[] col = new DataColumn[7];
        col[0] = new DataColumn("datetime", typeof(System.DateTime)); //eg. 20150906
        col[1] = new DataColumn("market", typeof(System.String));//TFEX,SET
        col[2] = new DataColumn("symbol", typeof(System.String));
        col[3] = new DataColumn("status", typeof(System.String));//none,openlong,openshort,closelong,closeshort
        col[4] = new DataColumn("volume", typeof(System.Double)); 
        col[5] = new DataColumn("price", typeof(System.Double));
        col[6] = new DataColumn("commision", typeof(System.Double));
        dt.Columns.AddRange(col);
        dt.PrimaryKey = new DataColumn[] {dt.Columns["datetime"],dt.Columns["symbol"] };
        return dt;
    }
    
    public static DataTable NewPortfolioTradeDetails(string accountname)
    {
        DataTable dt = new DataTable(accountname);
        DataColumn[] col = new DataColumn[23];

        col[0] = new DataColumn("symbol", typeof(System.String));
        col[1] = new DataColumn("open date", typeof(System.String));
        col[2] = new DataColumn("open time", typeof(System.String));
        col[3] = new DataColumn("open side", typeof(System.String));
        col[4] = new DataColumn("total openvol", typeof(System.Double));//none,openlong,openshort,closelong,closeshort
        col[5] = new DataColumn("avg cost", typeof(System.Double));
        col[6] = new DataColumn("total openval", typeof(System.Double));
        col[7] = new DataColumn("close date", typeof(System.String));
        col[8] = new DataColumn("close side", typeof(System.String));
        col[9] = new DataColumn("close time", typeof(System.String));
        col[10] = new DataColumn("total closevol", typeof(System.Double));//none,openlong,openshort,closelong,closeshort
        col[11] = new DataColumn("close price", typeof(System.Double));
        col[12] = new DataColumn("total closeval", typeof(System.Double));
        col[13] = new DataColumn("realize P/L", typeof(System.Double));

        col[14] = new DataColumn("high P/L", typeof(System.Double));
        col[15] = new DataColumn("high P/L time", typeof(System.String));
        col[16] = new DataColumn("low P/L", typeof(System.Double));
        col[17] = new DataColumn("low P/L time", typeof(System.String));
        col[18] = new DataColumn("#transaction", typeof(System.Double));
        col[19] = new DataColumn("#gain", typeof(System.Double));
        col[20] = new DataColumn("#loss", typeof(System.Double));
        col[21] = new DataColumn("%gain", typeof(System.Double));
        col[22] = new DataColumn("%loss", typeof(System.Double));
        
        dt.Columns.AddRange(col);
        return dt;


    }
    public string GetAccountName()
    {
        return _accountname;
    }


    //New Portfolio method for optimize
    //existing

    
    public static DataTable NewPortfolioTradeTransactionTable(string accountname)
    {
        DataTable dt = new DataTable(accountname);
        DataColumn[] col = new DataColumn[7];
        col[0] = new DataColumn("datetime", typeof(System.DateTime)); //eg. 20150906
        col[1] = new DataColumn("market", typeof(System.String));//TFEX,SET
        col[2] = new DataColumn("symbol", typeof(System.String));
        col[3] = new DataColumn("status", typeof(System.String));//none,openlong,openshort,closelong,closeshort
        col[4] = new DataColumn("volume", typeof(System.Double));
        col[5] = new DataColumn("price", typeof(System.Double));
        col[6] = new DataColumn("commision", typeof(System.Double));
        dt.Columns.AddRange(col);
        dt.PrimaryKey = new DataColumn[] { dt.Columns["datetime"], dt.Columns["symbol"] };
        return dt;
    }


    private static DataTable NewPortfolioStatusTable(string accountname)
    {
        DataTable dt = new DataTable(accountname);
        DataColumn[] col = new DataColumn[7];
        col[0] = new DataColumn("symbol", typeof(System.String));
        col[1] = new DataColumn("side", typeof(System.String));
        col[2] = new DataColumn("onhand vol", typeof(System.Double));//none,openlong,openshort,closelong,closeshort
        col[3] = new DataColumn("avg cost", typeof(System.Double));
        col[4] = new DataColumn("total cost", typeof(System.Double));
        col[5] = new DataColumn("realize P/L", typeof(System.Double));
        col[6] = new DataColumn("datetime", typeof(System.DateTime));


        dt.Columns.AddRange(col);
        return dt;
    }

    public Dictionary<string, DataTable> PortfolioStatusDict = new Dictionary<string, DataTable>();
    public Dictionary<string, DataTable> PortfolioTradeTransactionDict = new Dictionary<string, DataTable>();
    public void InitPortfolio(string symbol)
    {
        PortfolioStatusDict.Add(symbol, Portfolio.NewPortfolioStatusTable(_accountname));
        PortfolioTradeTransactionDict.Add(symbol, Portfolio.NewPortfolioTradeTransactionTable (_accountname));
    }
    public bool IsLong(string symbol)
    {
        bool islong = false;

        if (VolOnhand(symbol) > 0)
            islong = true;
        return islong;
    }
    public bool IsShort(string symbol)
    {
        bool isshort = false;
        if (VolOnhand(symbol) < 0)
            isshort = true;
        return isshort;
    }
    public double VolOnhand(string symbol)
    {
        double ovol = 0;
        if (PortfolioStatusDict.ContainsKey(symbol))
        {
            if (PortfolioStatusDict[symbol].Rows.Count > 0)
            {
                if ((Double)PortfolioStatusDict[symbol].Rows[0]["onhand vol"] > 0)//openlong
                    ovol = (Double)PortfolioStatusDict[symbol].Rows[0]["onhand vol"];
                else if ((Double)PortfolioStatusDict[symbol].Rows[1]["onhand vol"] > 0)//openshort
                    ovol = -1*(Double)PortfolioStatusDict[symbol].Rows[1]["onhand vol"];

            }
        }
        return ovol;
    }
    public double AvgOnhandPrice(string symbol)
    {
        double oprice = 0;
        if (PortfolioStatusDict.ContainsKey(symbol))
        {
            if (PortfolioStatusDict[symbol].Rows.Count > 0)
            {
                if ((Double)PortfolioStatusDict[symbol].Rows[0]["onhand vol"] > 0)//openlong
                    oprice = (Double)PortfolioStatusDict[symbol].Rows[0]["avg cost"];
                else if ((Double)PortfolioStatusDict[symbol].Rows[1]["onhand vol"] > 0)//openshort
                    oprice = (Double)PortfolioStatusDict[symbol].Rows[1]["avg cost"];

            }
        }
        return oprice;
    }
    public double CostOnhand(string symbol)
    {
        double ocost = 0;
        if (PortfolioStatusDict.ContainsKey(symbol))
        {
            if (PortfolioStatusDict[symbol].Rows.Count > 0)
            {
                ocost = (Double)PortfolioStatusDict[symbol].Rows[PortfolioStatusDict[symbol].Rows.Count - 1]["total cost"];

            }
        }
        return ocost;
    }
    public void UpdatePortfolioTradeTransaction(DataRow transDR, string symbol)
    {
        this.PortfolioTradeTransactionDict[symbol].Rows.Add(transDR.ItemArray);
        this.SummaryTransactionToPortStatus(symbol);
    }

    public DataTable SummaryTransactionToPortStatus(string symbol)
    {
        DataTable portstatusTable = Portfolio.NewPortfolioStatus(_accountname);
        DataRow[] result = PortfolioTradeTransactionDict[symbol].Select(String.Format("symbol='{0}'", symbol));
        Double totallongvol = 0;
        Double totalshortvol = 0;
        Double totallongval = 0;
        Double totalshortval = 0;
        Double avglongprice = 0;
        Double avgshortprice = 0;
        Double rpllong = 0;
        Double rplshort = 0;
        string market = "";
        string tradetime = "";
        double tmpvol, tmpval, tmpprice, tmpcomm;
        for (int i = 0; i < result.Length; i++)
        {
            tmpvol = Double.Parse(result[i]["volume"].ToString());
            tmpprice = Double.Parse(result[i]["price"].ToString());
            tmpcomm = Double.Parse(result[i]["commision"].ToString());
            tmpval = tmpprice * tmpvol;
            tradetime = ((DateTime)result[i]["datetime"]).ToShortTimeString();
            market = result[i]["market"].ToString();
            switch (result[i]["status"].ToString())
            {
                case "openlong":
                    {
                        totallongvol = totallongvol + tmpvol;
                        totallongval = totallongval + tmpval;
                        avglongprice = totallongval / totallongvol;
                        break;
                    }
                case "closelong":
                    {
                        rpllong = rpllong + ((tmpprice - avglongprice) * tmpvol);
                        totallongvol = totallongvol - tmpvol;
                        if (totallongvol == 0)
                            totallongval = 0;
                        else totallongval = totallongval - tmpval;
                        break;
                    }
                case "openshort":
                    {
                        totalshortvol = totalshortvol + tmpvol;
                        totalshortval = totalshortval + tmpval;
                        avgshortprice = totalshortval / totalshortvol;
                        break;
                    }
                case "closeshort":
                    {
                        rplshort = rplshort + ((avgshortprice - tmpprice) * tmpvol);
                        totalshortvol = totalshortvol - tmpvol;
                        if (totalshortvol == 0)
                            totalshortval = 0;
                        else totalshortval = totalshortval - tmpval;
                        break;
                    }
            }

        }
        //Assign Summary Long and Short
        DataTable portstat = Portfolio.NewPortfolioStatus(_accountname);
        //Long
        DataRow longdr = portstat.NewRow();
        longdr["Symbol"] = symbol;
        longdr["Side"] = "openlong";
        longdr["Onhand vol"] = totallongvol;
        longdr["Avg cost"] = avglongprice;
        
        longdr["Total cost"] = totallongvol * avglongprice;
        longdr["Realize P/L"] = rpllong;
        if (market == "TFEX")
        {
            longdr["Total cost"] = (double)longdr["Total cost"] * 100;
            //longdr["Realize P/L"] = (double)longdr["Realize P/L"] * 1000;
        }
        longdr["Time"] = tradetime;
        //short
        DataRow shortdr = portstat.NewRow();
        shortdr["Symbol"] = symbol;
        shortdr["Side"] = "openshort";
        shortdr["Onhand vol"] = totalshortvol;
        shortdr["Avg cost"] = avgshortprice;
        shortdr["Total cost"] = totalshortvol * avgshortprice;
        shortdr["Realize P/L"] = rplshort;
        if (market == "TFEX")
        {
            shortdr["Total cost"] = (double)shortdr["Total cost"] * 100;
            //shortdr["Realize P/L"] = shortdr["Realize P/L"] * 1000;
        }
        
        shortdr["Time"] = tradetime;
        portstat.Rows.Add(longdr);
        portstat.Rows.Add(shortdr);

        //add weerawud
        this.PortfolioStatusDict[symbol] = portstat;
        return portstat;
    }

    public DataTable SummaryPortfolioTradeDetails(string symbol)
    {
        DataTable portstatusTable = Portfolio.NewPortfolioStatus(_accountname);
        //DataRow[] result = this.PortfolioTable.Select(String.Format("symbol='{0}'", symbol));
        DataRow[] result = this.PortfolioTradeTransactionDict[symbol].Select(String.Format("symbol='{0}'", symbol));
        Double totallongvol = 0;
        Double totalshortvol = 0;
        Double totallongval = 0;
        Double totalshortval = 0;
        Double avglongprice = 0;
        Double avgshortprice = 0;
        Double rpllong = 0;
        Double rplshort = 0;
        Double numlongtranc = 0;
        Double numlonggain = 0;
        Double numlongloss = 0;
        Double numshorttranc = 0;
        Double numshortgain = 0;
        Double numshortloss = 0;
        double tmpvol, tmpval, tmpprice, tmpcomm;
        string opentime = "";
        string opendate = "";
        DateTime tdate;
        //Assign Summary Long and Short
        DataTable portstat = Portfolio.NewPortfolioTradeDetails(_accountname);
        for (int i = 0; i < result.Length; i++)
        {
            tdate = (DateTime)(result[i]["datetime"]);
            tmpvol = Double.Parse(result[i]["volume"].ToString());
            tmpprice = Double.Parse(result[i]["price"].ToString());
            tmpcomm = Double.Parse(result[i]["commision"].ToString());
            tmpval = tmpprice * tmpvol;

            rpllong = 0;
            rplshort = 0;
            numlongtranc = 0;
            numlonggain = 0;
            numlongloss = 0;
            numshorttranc = 0;
            numshortgain = 0;
            numshortloss = 0;

            switch (result[i]["status"].ToString())
            {
                case "openlong":
                    {
                        totallongvol = totallongvol + tmpvol;
                        totallongval = totallongval + tmpval;
                        avglongprice = totallongval / totallongvol;
                        opentime = tdate.ToShortTimeString();
                        opendate = tdate.ToShortDateString();


                        break;
                    }
                case "closelong":
                    {

                        //Long
                        DataRow longdr = portstat.NewRow();
                        longdr["open date"] = opendate;
                        longdr["symbol"] = symbol;
                        longdr["open time"] = opentime;
                        longdr["open side"] = "long";
                        longdr["total openvol"] = totallongvol;
                        longdr["avg cost"] = avglongprice;
                        longdr["total openval"] = totallongval;

                        //rpllong = rpllong + ((tmpprice - avglongprice) * tmpvol);
                        rpllong = ((tmpprice - avglongprice) * tmpvol);
                        totallongvol = totallongvol - tmpvol;
                        if (totallongvol == 0)
                            totallongval = 0;
                        else totallongval = totallongval - tmpval;

                        //numlongtranc = numlongtranc + 1;
                        numlongtranc = 1;
                        if ((tmpprice - avglongprice) > 0)
                            numlonggain = 1;
                        //numlonggain = numlonggain + 1;
                        else numlongloss = 1;//numlongloss = numlongloss + 1;
                        longdr["close date"] = tdate.ToShortDateString();
                        longdr["close time"] = tdate.ToShortTimeString();
                        longdr["close side"] = "short";
                        longdr["total closevol"] = tmpvol;
                        longdr["close price"] = tmpprice;
                        longdr["total closeval"] = tmpvol * tmpprice;
                        longdr["realize P/L"] = rpllong;
                        longdr["#transaction"] = numlongtranc;
                        longdr["#gain"] = numlonggain;
                        longdr["#loss"] = numlongloss;
                        longdr["%gain"] = 100 * (numlonggain / numlongtranc);
                        longdr["%loss"] = 100 * (numlongloss / numlongtranc);

                        portstat.Rows.Add(longdr);

                        break;
                    }
                case "openshort":
                    {
                        totalshortvol = totalshortvol + tmpvol;
                        totalshortval = totalshortval + tmpval;
                        avgshortprice = totalshortval / totalshortvol;
                        opentime = tdate.ToShortTimeString();
                        opendate = tdate.ToShortDateString();
                        break;
                    }
                case "closeshort":
                    {

                        //short
                        DataRow shortdr = portstat.NewRow();
                        shortdr["open date"] = opendate;
                        shortdr["symbol"] = symbol;
                        shortdr["open time"] = opentime;
                        shortdr["open side"] = "short";
                        shortdr["total openvol"] = totalshortvol;
                        shortdr["avg cost"] = avgshortprice;
                        shortdr["total openval"] = totalshortval;

                        //rplshort = rplshort + ((avgshortprice - tmpprice) * tmpvol);
                        rplshort = ((avgshortprice - tmpprice) * tmpvol);
                        totalshortvol = totalshortvol - tmpvol;
                        if (totalshortvol == 0)
                            totalshortval = 0;
                        else totalshortval = totalshortval - tmpval;

                        //numshorttranc = numshorttranc + 1;
                        numshorttranc = 1;
                        if ((avgshortprice - tmpprice) > 0)
                            numshortgain = 1;
                        //numshortgain = numshortgain + 1;
                        else numshortloss = 1;//numshortloss = numshortloss + 1;
                        shortdr["close date"] = tdate.ToShortDateString();
                        shortdr["close time"] = tdate.ToShortTimeString();
                        shortdr["close side"] = "long";
                        shortdr["total closevol"] = tmpvol;

                        shortdr["close price"] = tmpprice;
                        shortdr["total closeval"] = tmpvol * tmpprice;
                        shortdr["realize P/L"] = rplshort;
                        shortdr["#transaction"] = numshorttranc;
                        shortdr["#gain"] = numshortgain;
                        shortdr["#loss"] = numshortloss;
                        shortdr["%gain"] = 100 * (numshortgain / numshorttranc);
                        shortdr["%loss"] = 100 * (numshortloss / numshorttranc);

                        portstat.Rows.Add(shortdr);

                        break;
                    }
            }

        }
        return portstat;
    }

}

