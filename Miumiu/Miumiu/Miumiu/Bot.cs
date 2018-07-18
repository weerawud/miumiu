using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Threading;
using System.Globalization;

class Bot
{
    public Feeder feed;
    public Portfolio port;
    //public GapTradeLogic gaplogic;
    public TradeLogic logic;
    public Order tradeorder;
    public Dictionary<string, DataTable> TickDict;
    
    private double _interval;
    private string _accountname;

    private string _symbol;
    private Logs _eventlogs;
    string _tradedate;
    public DataTable simulatetestSummary;
    DataHandlerMSSQL dmsql;
    public Bot(string accountname,Logs eventlogs,TradeLogic logic,DataHandlerMSSQL sqladapter)
    {
        //_tradedate = tradedate;
        _accountname = accountname;
        _eventlogs = eventlogs;
        dmsql = sqladapter;

        port = new Portfolio(_accountname);
        simulatetestSummary = Portfolio.NewPortfolioTradeDetails("weerawud");

        //gaplogic = new GapTradeLogic(Feeder.NewResamplingDataTable());
        this.logic = logic;
        tradeorder = new Order();
        TickDict = new Dictionary<string, DataTable>();

    }
    public string GetTestID(double gain,double hitrate)
    {

        return logic.GetLogicName() + "-" + _symbol + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + _interval + "-" + gain.ToString()+"-hit%="+hitrate.ToString();
    }
    public string GetTestID(double gain, double hitrate,TradeLogic slogic)
    {

        return slogic.GetLogicName() + "-" + _symbol + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + _interval + "-" + gain.ToString() + "-hit%=" + hitrate.ToString();
    }
    public void WriteTradeResultToDB()
    {
        //Realize gain/loss
        DataTable portsumdetails = simulatetestSummary;
        double gainsum = 0;
        double losssum = 0;

        double rsum=0;
        foreach (DataRow dr in portsumdetails.Rows)
        {
            rsum = rsum + (Double)dr["realize P/L"] - 1;
            gainsum = gainsum + (Double)dr["#gain"];
            losssum = losssum + (Double)dr["#loss"];
        }
        double hrate = ((gainsum) / (gainsum + losssum)) * 100;
        string id = GetTestID(rsum,hrate);

        DataHandlerMSSQL tmpadapter;
        string constring;
        DataTable logicpropertytable = logic.GetPropertyTable(id);
        constring = @"server=" + System.Environment.MachineName + @"\WiiSQL;Initial Catalog=PropertyDB;User ID=sa;Password=wee@dcs.c0m";
        tmpadapter = new DataHandlerMSSQL(constring);
        tmpadapter.WriteDataTableToDB(id, logicpropertytable);

        
        
        constring = @"server=" + System.Environment.MachineName + @"\WiiSQL;Initial Catalog=PortTestSummaryDB;User ID=sa;Password=wee@dcs.c0m";
        tmpadapter = new DataHandlerMSSQL(constring);
        tmpadapter.WriteDataTableToDB(id, portsumdetails);
        
    }
    public void WriteTradeResultToDB(TradeLogic slogic)
    {
        //Realize gain/loss
        DataTable portsumdetails = simulatetestSummary;
        double gainsum = 0;
        double losssum = 0;

        double rsum = 0;
        foreach (DataRow dr in portsumdetails.Rows)
        {
            rsum = rsum + (Double)dr["realize P/L"] - 1;
            gainsum = gainsum + (Double)dr["#gain"];
            losssum = losssum + (Double)dr["#loss"];
        }
        double hrate = ((gainsum) / (gainsum + losssum)) * 100;
        string id = GetTestID(rsum, hrate,slogic);

        DataHandlerMSSQL tmpadapter;
        string constring;
        DataTable logicpropertytable = slogic.GetPropertyTable(id);
        constring = @"server=" + System.Environment.MachineName + @"\WiiSQL;Initial Catalog=PropertyDB;User ID=sa;Password=wee@dcs.c0m";
        tmpadapter = new DataHandlerMSSQL(constring);
        tmpadapter.WriteDataTableToDB(id, logicpropertytable);



        constring = @"server=" + System.Environment.MachineName + @"\WiiSQL;Initial Catalog=PortTestSummaryDB;User ID=sa;Password=wee@dcs.c0m";
        tmpadapter = new DataHandlerMSSQL(constring);
        tmpadapter.WriteDataTableToDB(id, portsumdetails);

    }
    public void SimulateTrade(string symbol)
    {
        DataTable stock;
        if (TickDict.TryGetValue(symbol, out stock))
        {
            foreach (DataRow dr in stock.Rows)
            {
                DataRow resampdr = Feeder.NewResamplingDataTable().NewRow();

                feed.ResamplingTick(dr);

                
                //logic.UpdateHistoricalTable(feed.GetResamplingTable());
                if (logic.HistoricalTable.Rows.Count > 1)
                {
                    double closeprice = Double.Parse(dr["<CLOSE>"].ToString());
                    //)2 check port status by input close price
                    string longstatus = port.GetOnhandStatus("openlong", symbol);
                    string shortstatus = port.GetOnhandStatus("openshort", symbol);
                    resampdr.ItemArray = feed.GetResamplingBar(dr["<DATE>"].ToString(), dr["<TIME>"].ToString()).ItemArray;
                    //3) check return status from tradelogic
                    DataRow signaldr;
                    if (longstatus == "openlong")
                    {
                        double onhandcost = port.GetOnhandCost("openlong", symbol);
                        double onhandvol = port.GetOnhandVolume("openlong", symbol);
                        signaldr = logic.FeedTickTrade(resampdr, "closelong", onhandcost, onhandvol);
                    }
                    else if (shortstatus == "openshort")
                    {
                        double onhandcost = port.GetOnhandCost("openshort", symbol);
                        double onhandvol = port.GetOnhandVolume("openshort", symbol);
                        signaldr = logic.FeedTickTrade(resampdr, "closeshort", onhandcost, onhandvol);
                    }
                    else signaldr = logic.FeedTickTrade(resampdr, "none", 0, 0);
                    string signalstatus = signaldr["status"].ToString();

                    if (signalstatus == "openlong" || signalstatus == "openshort" || signalstatus == "closelong" || signalstatus == "closeshort")
                    {

                        DataRow transdr = tradeorder.GetTransaction(dr["<DATE>"].ToString(), dr["<TIME>"].ToString(), "TFEX", symbol, signaldr, port.PortfolioTable.NewRow());
                        port.UpdatePortfolioTable(transdr, symbol);
                        //dt.Rows.Add(transdr.ItemArray);
                    }
                }
                else
                    port.GetPortfolioSummary(symbol); //first time summary port when load first tick   
            }
        }
    }
    /* Backup
    public void SimulateTradeUnit(string symbol,List<string> ldate,int resamplinginterval,string starttradetime,string endtradetime)
    {
        
        simulatetestSummary.Rows.Clear();
        
        _symbol = symbol;
        _tradedate = "";
        _interval = resamplinginterval;
        
        //string simid = GetTestID();
        WriteLog("Start simulate "+symbol+" @resampling interval ="+resamplinginterval);

        for (int i = 0; i < ldate.Count; i++)
        {
            logic.InitLogicPropTable(resamplinginterval);
            _tradedate = ldate[i];
            feed = new Feeder(resamplinginterval, _tradedate);


            //Query from SQL database
            
            TimeSpan starttrade = TimeSpan.Parse(starttradetime);
            TimeSpan endtrade = TimeSpan.Parse(endtradetime);

            WriteLog("Start Query: " + DateTime.Now.ToLongTimeString());
            //var tradestock = dmsql.GetBarsTable(symbol, _tradedate, _tradedate);
            var tradestock = dmsql.GetBarsTable(symbol, _tradedate, _tradedate, starttrade, endtrade);
            WriteLog("Number of ticker = " + tradestock.Rows.Count.ToString());
            WriteLog("End Query: " + DateTime.Now.ToLongTimeString());
            

            
            //Filter only require time
            
            DataTable tmpstock = tradestock.Clone();

            foreach (DataRow dr in tradestock.Rows)
            {
                TimeSpan t = TimeSpan.Parse(dr["<TIME>"].ToString());
                if (starttrade < t)
                    tmpstock.Rows.Add(dr.ItemArray);
            }
            //Add to symbol dict
            LoadTick(tmpstock, symbol);

            WriteLog("Start Simulate trade");
            DataTable stock;
            TradeUnit tu = new TradeUnit(logic, _accountname, symbol);
            
            if (TickDict.TryGetValue(symbol, out stock))
            {
                int count = 0;
                TimeSpan resamplatesttime = new TimeSpan();
                foreach (DataRow dr in stock.Rows)
                {
                    count++;
                    
                    feed.ResamplingTick(dr);
                    
                    DataTable resampdt = feed.ResamplingDataTable;
                    if(((TimeSpan)resampdt.Rows[resampdt.Rows.Count-1]["Time"])>resamplatesttime)
                        tu.Trade(dr, feed, resampdt);
                    resamplatesttime = ((TimeSpan)resampdt.Rows[resampdt.Rows.Count - 1]["Time"]);
                }
                this.port = tu.port;

            }
            
            WriteLog("End Simulate trade");
            foreach (DataRow r in port.GetPortfolioTradeDetails(symbol).Rows)
                this.simulatetestSummary.Rows.Add(r.ItemArray);
            //Thread.Sleep(50);
            
        }
        WriteLog("Write simulate result to database");
        WriteTradeResultToDB();
        WriteLog("End simulate " + symbol + " @resampling interval =" + resamplinginterval);
        string[] l = _eventlogs.Eventlogs.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
        TimeSpan stime = TimeSpan.Parse(l[l.Length-2].Split('@')[0]);//start time log
        TimeSpan etime = TimeSpan.Parse(l[0].Split('@')[0]);//end time log
        TimeSpan totaltime = (etime-stime);
        WriteLog("Total Simulate Trade Time = " + totaltime.ToString());
        
    }
    */

    public void SimulateTradeUnit(string symbol, List<string> ldate, int resamplinginterval, string starttradetime, string endtradetime)
    {

        simulatetestSummary.Rows.Clear();

        _symbol = symbol;
        _tradedate = "";
        _interval = resamplinginterval;

        //string simid = GetTestID();
        WriteLog("Start simulate " + symbol + " @resampling interval =" + resamplinginterval);
        List<string> tradeddate = new List<string>();
        List<string> dupddate = new List<string>();
        //Dictionary<string, DataTable> dictSimulatetestSummary = new Dictionary<string, DataTable>();
        Parallel.ForEach(ldate,new ParallelOptions { MaxDegreeOfParallelism = 6 },
            eachldate => {
                //DataTable tmp simulatetestSummary
                //DataTable tmpsimulatetestSummary = simulatetestSummary.Clone();

                //Add eachladate to List for confirm the date will be duplicate
                //tradeddate.Add(eachldate);

                //Start Process trade
                TradeLogic eachlogic = new BigLotsHFTTradeLogic2(Feeder.NewResamplingDataTable(), resamplinginterval);
                eachlogic.InitLogicPropTable(resamplinginterval);
                string constring = @"server=" + System.Environment.MachineName + @"\WiiSQL;Initial Catalog=AssetDB;User ID=sa;Password=wee@dcs.c0m";
                DataHandlerMSSQL eachdmsql = new DataHandlerMSSQL(constring);

                //_tradedate = eachldate;
                //feed = new Feeder(resamplinginterval, eachldate);

                Feeder eachfeed = new Feeder(resamplinginterval, eachldate); ;
                //Query from SQL database

                TimeSpan starttrade = TimeSpan.Parse(starttradetime);
                TimeSpan endtrade = TimeSpan.Parse(endtradetime);

                WriteLog("Start Query: " + DateTime.Now.ToLongTimeString(), eachldate);
                //var tradestock = dmsql.GetBarsTable(symbol, _tradedate, _tradedate);
                var tradestock = eachdmsql.GetBarsTable(symbol, eachldate, eachldate, starttrade, endtrade);
                WriteLog("Number of ticker = " + tradestock.Rows.Count.ToString(), eachldate);
                WriteLog("End Query: " + DateTime.Now.ToLongTimeString(), eachldate);



                //Filter only require time

                DataTable tmpstock = tradestock.Clone();

                foreach (DataRow dr in tradestock.Rows)
                {
                    TimeSpan t = TimeSpan.Parse(dr["<TIME>"].ToString());
                    if (starttrade < t)
                        tmpstock.Rows.Add(dr.ItemArray);
                }

                WriteLog("Start Simulate trade", eachldate);
                //DataTable stock;

                TradeUnit tu = new TradeUnit(eachlogic, _accountname, symbol);

                int count = 0;
                TimeSpan resamplatesttime = new TimeSpan();
                foreach (DataRow dr in tmpstock.Rows)
                {
                    count++;

                    eachfeed.ResamplingTick(dr);

                    DataTable resampdt = eachfeed.ResamplingDataTable;
                    if (((TimeSpan)resampdt.Rows[resampdt.Rows.Count - 1]["Time"]) > resamplatesttime)
                        tu.Trade(dr, eachfeed, resampdt);
                    resamplatesttime = ((TimeSpan)resampdt.Rows[resampdt.Rows.Count - 1]["Time"]);
                }
                //this.port = tu.port;



                WriteLog("End Simulate trade", eachldate);


                //Summmary portolio 
                foreach (DataRow r in (tu.port.GetPortfolioTradeDetails(symbol)).Rows)
                {
                    //find high and low price of trade period
                    //port.GetSimulateSumwithHigLowPrice(r, eachfeed.ResamplingDataTable);
                    tu.port.GetSimulateSumwithHigLowPrice(r, eachfeed.ResamplingDataTable);
                    //Add result
                    this.simulatetestSummary.Rows.Add(r.ItemArray);
                    //tmpsimulatetestSummary.Rows.Add(r.ItemArray);
                }
                //dictSimulatetestSummary.Add(eachldate, tmpsimulatetestSummary);
                //this.port = tu.port;
            }
        );
        /*
        for (int i = 0; i < ldate.Count; i++)
        {
            
            //Thread.Sleep(50);

        }
        if (tradeddate.Count == ldate.Count)
            foreach (KeyValuePair<string, DataTable> kv in dictSimulatetestSummary)
                foreach (DataRow r in kv.Value.Rows)
                    simulatetestSummary.Rows.Add(r.ItemArray);
        */
        //loop for wait completed all thread
        WriteLog("Write simulate result to database");
        WriteTradeResultToDB();
        WriteLog("End simulate " + symbol + " @resampling interval =" + resamplinginterval);
        string[] l = _eventlogs.Eventlogs.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
        TimeSpan stime = TimeSpan.Parse(l[l.Length - 2].Split('@')[0]);//start time log
        TimeSpan etime = TimeSpan.Parse(l[0].Split('@')[0]);//end time log
        TimeSpan totaltime = (etime - stime);
        WriteLog("Total Simulate Trade Time = " + totaltime.ToString());

    }

    public void SimulateBestTradeUnit(string symbol, List<string> ldate, int resamplinginterval, string starttradetime, string endtradetime)
    {

        simulatetestSummary.Rows.Clear();

        _symbol = symbol;
        _tradedate = "";
        _interval = resamplinginterval;

        //string simid = GetTestID();
        WriteLog("Start simulate " + symbol + " @resampling interval =" + resamplinginterval);
        List<string> tradeddate = new List<string>();
        List<string> dupddate = new List<string>();
        //Dictionary<string, DataTable> dictSimulatetestSummary = new Dictionary<string, DataTable>();
        Parallel.ForEach(ldate, new ParallelOptions { MaxDegreeOfParallelism = 6 },
            eachldate => {
                //DataTable tmp simulatetestSummary
                //DataTable tmpsimulatetestSummary = simulatetestSummary.Clone();

                //Add eachladate to List for confirm the date will be duplicate
                //tradeddate.Add(eachldate);

                //Start Process trade
                TradeLogic eachlogic = new BigLotsHFTTradeLogic2(Feeder.NewResamplingDataTable(), resamplinginterval);

                eachlogic.InitLogicPropTable(resamplinginterval);
                string constring = @"server=" + System.Environment.MachineName + @"\WiiSQL;Initial Catalog=AssetDB;User ID=sa;Password=wee@dcs.c0m";
                DataHandlerMSSQL eachdmsql = new DataHandlerMSSQL(constring);

                //Get List properties name that want to adjust
                //List<string> propnameList = ((BigLotsHFTTradeLogic2)eachlogic).get
                
                
                //_tradedate = eachldate;
                //feed = new Feeder(resamplinginterval, eachldate);

                Feeder eachfeed = new Feeder(resamplinginterval, eachldate); ;
                //Query from SQL database

                TimeSpan starttrade = TimeSpan.Parse(starttradetime);
                TimeSpan endtrade = TimeSpan.Parse(endtradetime);

                WriteLog("Start Query: " + DateTime.Now.ToLongTimeString(), eachldate);
                //var tradestock = dmsql.GetBarsTable(symbol, _tradedate, _tradedate);
                var tradestock = eachdmsql.GetBarsTable(symbol, eachldate, eachldate, starttrade, endtrade);
                WriteLog("Number of ticker = " + tradestock.Rows.Count.ToString(), eachldate);
                WriteLog("End Query: " + DateTime.Now.ToLongTimeString(), eachldate);



                //Filter only require time

                DataTable tmpstock = tradestock.Clone();

                foreach (DataRow dr in tradestock.Rows)
                {
                    TimeSpan t = TimeSpan.Parse(dr["<TIME>"].ToString());
                    if (starttrade < t)
                        tmpstock.Rows.Add(dr.ItemArray);
                }

                WriteLog("Start Simulate trade", eachldate);
                //DataTable stock;

                TradeUnit tu = new TradeUnit(eachlogic, _accountname, symbol);

                int count = 0;
                TimeSpan resamplatesttime = new TimeSpan();
                foreach (DataRow dr in tmpstock.Rows)
                {
                    count++;

                    eachfeed.ResamplingTick(dr);

                    DataTable resampdt = eachfeed.ResamplingDataTable;
                    if (((TimeSpan)resampdt.Rows[resampdt.Rows.Count - 1]["Time"]) > resamplatesttime)
                        tu.Trade(dr, eachfeed, resampdt);
                    resamplatesttime = ((TimeSpan)resampdt.Rows[resampdt.Rows.Count - 1]["Time"]);
                }
                //this.port = tu.port;



                WriteLog("End Simulate trade", eachldate);


                //Summmary portolio 
                foreach (DataRow r in (tu.port.GetPortfolioTradeDetails(symbol)).Rows)
                {
                    //find high and low price of trade period
                    //port.GetSimulateSumwithHigLowPrice(r, eachfeed.ResamplingDataTable);
                    tu.port.GetSimulateSumwithHigLowPrice(r, eachfeed.ResamplingDataTable);
                    //Add result
                    this.simulatetestSummary.Rows.Add(r.ItemArray);
                    //tmpsimulatetestSummary.Rows.Add(r.ItemArray);
                }
                //dictSimulatetestSummary.Add(eachldate, tmpsimulatetestSummary);
                //this.port = tu.port;
            }
        );
        /*
        for (int i = 0; i < ldate.Count; i++)
        {
            
            //Thread.Sleep(50);

        }
        if (tradeddate.Count == ldate.Count)
            foreach (KeyValuePair<string, DataTable> kv in dictSimulatetestSummary)
                foreach (DataRow r in kv.Value.Rows)
                    simulatetestSummary.Rows.Add(r.ItemArray);
        */
        //loop for wait completed all thread
        WriteLog("Write simulate result to database");
        WriteTradeResultToDB();
        WriteLog("End simulate " + symbol + " @resampling interval =" + resamplinginterval);
        string[] l = _eventlogs.Eventlogs.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
        TimeSpan stime = TimeSpan.Parse(l[l.Length - 2].Split('@')[0]);//start time log
        TimeSpan etime = TimeSpan.Parse(l[0].Split('@')[0]);//end time log
        TimeSpan totaltime = (etime - stime);
        WriteLog("Total Simulate Trade Time = " + totaltime.ToString());

    }
    private DataRow GetSimulateSumwithHigLowPrice(DataRow PortTradeRow,DataTable ResampingTable)
    {

        //TimeSpan opentime = TimeSpan.Parse(PortTradeRow["open time"].ToString());
        //TimeSpan closetime = TimeSpan.Parse(PortTradeRow["close time"].ToString());
        //DateTime dateTime = DateTime.ParseExact(text,"hh:mm tt", CultureInfo.InvariantCulture);
        //DateTime dateopentime = DateTime.ParseExact(PortTradeRow["open time"].ToString(), "hh:mm tt", CultureInfo.InvariantCulture);
        TimeSpan opentime = Convert.ToDateTime(PortTradeRow["open time"].ToString()).TimeOfDay;
        TimeSpan closetime = Convert.ToDateTime(PortTradeRow["close time"].ToString()).TimeOfDay;
        double highprice=0;
        string hightime="";
        double lowprice=0;
        string lowtime="";
        
        foreach (DataRow r in ResampingTable.Rows)
        {
            if ((((TimeSpan)r["Time"]) >= opentime) && (((TimeSpan)r["Time"]) <= closetime))
            {
                if ((highprice == 00)||(double)r["High"] > highprice) {
                    highprice = (double)r["High"];
                    hightime = ((TimeSpan)r["Time"]).ToString();
                }
                if ((lowprice == 00) || (double)r["Low"] < lowprice)
                {
                    lowprice = (double)r["Low"];
                    lowtime = ((TimeSpan)r["Time"]).ToString();
                }
            }
            else if (((TimeSpan)r["Time"]) > closetime)
                break;
        }
        PortTradeRow["high price period"] = highprice;
        PortTradeRow["low price period"] = lowprice;
        PortTradeRow["high price time"] = hightime;
        PortTradeRow["low price time"] = lowtime;
        return PortTradeRow;
    }
    public void UpdateSimulateTestSummaryTable()
    {

    }

    public void LoadTick(DataRow tickDR,string symbol)
    {
        if (!TickDict.ContainsKey(symbol))
            TickDict.Add(symbol, tickDR.Table.Clone());
        TickDict[symbol].Rows.Add(tickDR);
    }
    public void LoadTick(DataTable tickTable, string symbol)
    {
        if (!TickDict.ContainsKey(symbol))
            TickDict.Add(symbol, tickTable.Clone());
        TickDict[symbol] = tickTable;
    }
    public void WriteLog(string log)
    {
        //Thread.Sleep(TimeSpan.FromSeconds(5));
        string logline = String.Format("{0}@{2}: {1} ", DateTime.Now.ToString("HH:mm:ss"), log,_tradedate);
        string textlog = logline + Environment.NewLine + _eventlogs.Eventlogs;
        _eventlogs.Eventlogs = textlog;
    }
    public void WriteLog(string log,string tradedate)
    {
        
        //Thread.Sleep(TimeSpan.FromSeconds(5));
        string logline = String.Format("{0}@{2}: {1} ", DateTime.Now.ToString("HH:mm:ss"), log, tradedate);
        string textlog = logline + Environment.NewLine + _eventlogs.Eventlogs;
        _eventlogs.Eventlogs = textlog;
    }

}