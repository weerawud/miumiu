using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Threading;
using System.Globalization;
using System.Windows.Forms;


class Bot
{
    public Feeder feed;
    public Portfolio port;
    //public GapTradeLogic gaplogic;
    public TradeLogic logic;
    public SearchLogic searchlogic;
    public Order tradeorder;
    //public Dictionary<string, DataTable> TickDict;
    public Dictionary<double[,], Dictionary<string,Property>> ProfitReturnRatePropertyDict;

    private double _interval;
    private string _accountname;

    private string _symbol;
    private Logs _eventlogs;
    string _tradedate;
    public DataTable simulatetestSummary;

    private ProgressBar backtestProgressBar;
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
        //TickDict = new Dictionary<string, DataTable>();
        ProfitReturnRatePropertyDict = new Dictionary<double[,], Dictionary<string,Property>>();

    }
    public Bot(string accountname, Logs eventlogs, SearchLogic logic, DataHandlerMSSQL sqladapter)
    {
        //_tradedate = tradedate;
        _accountname = accountname;
        _eventlogs = eventlogs;
        dmsql = sqladapter;

        port = new Portfolio(_accountname);
        simulatetestSummary = Portfolio.NewPortfolioTradeDetails("weerawud");

        //gaplogic = new GapTradeLogic(Feeder.NewResamplingDataTable());
        this.searchlogic = logic;
        tradeorder = new Order();
        //TickDict = new Dictionary<string, DataTable>();
        ProfitReturnRatePropertyDict = new Dictionary<double[,], Dictionary<string, Property>>();

    }
    public void BindBackTestProgressBar(ref ProgressBar pb)
    {
        this.backtestProgressBar = pb;
        
    }
    public string GetTestID(double gain,double hitrate)
    {

        return logic.GetLogicName() + "-" + _symbol + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + _interval + "-" + gain.ToString()+"-hit%="+hitrate.ToString();
    }
    public string GetTestID(double gain, double hitrate,string _idsymbol)
    {

        return logic.GetLogicName() + "-" + _idsymbol + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + _interval + "-" + gain.ToString() + "-hit%=" + hitrate.ToString();
    }
    public string GetTestID(double gain, double hitrate,TradeLogic slogic)
    {

        return slogic.GetLogicName() + "-" + _symbol + "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + _interval + "-" + gain.ToString() + "-hit%=" + hitrate.ToString();
    }
    public void StoreProfitReturnRateProperty(Dictionary<string,Property> adjustedLogicTable)
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
        string id = GetTestID(rsum, hrate);
        double[,] profitrate = new double[1,2];
        profitrate[0, 0] = rsum;
        profitrate[0, 1] = hrate;
        if (!this.ProfitReturnRatePropertyDict.ContainsKey(profitrate))
        {
            Dictionary<string, Property> bestLogicTable = new Dictionary<string, Property>();
            foreach (KeyValuePair<string, Property> kv in adjustedLogicTable)
                bestLogicTable.Add(kv.Key, kv.Value);
            this.ProfitReturnRatePropertyDict.Add(profitrate, bestLogicTable);
        }
    }

    private void CopyPropertyTable(Dictionary<string,Property> source,Dictionary<string,Property> tobe)
    {
        for (int i = 0; i < tobe.Keys.Count; i++)
        {
            string tmpkey = tobe.ElementAt(i).Key;
            source[tmpkey].Value = tobe[tmpkey].Value;
        }

    }

    public Dictionary<string, Property> GetPropertyTableBestProfit(double returnrate,Dictionary<string,Property> basePropertyDict)
    {
        //find best profit which hit rate condition if not found return base
        double highestprofit = 0;
        Dictionary<string, Property> tmpPropDict = basePropertyDict;
        foreach (KeyValuePair<double[,],Dictionary<string,Property>> kv in ProfitReturnRatePropertyDict)
        {
            //check rate return
            if (kv.Key[0, 1] >= returnrate)
            {
                if ((highestprofit == 0) || (kv.Key[0, 0] > highestprofit))
                {
                    highestprofit = kv.Key[0, 0];
                    tmpPropDict = kv.Value;
                }
            }
            
        }
        return tmpPropDict;
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

    public void WriteTradeResultToDB(string _symbol)
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
        string id = GetTestID(rsum, hrate,_symbol);

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

    public void SimulateTradeUnitSequential(List<string> symbols, List<string> ldate, int resamplinginterval, string starttradetime, string endtradetime, double maxonhandvol,int lookbackperiod)
    {

        simulatetestSummary.Rows.Clear();
        /*
        _symbol = symbol;
        _tradedate = "";
        _interval = resamplinginterval;
        */
        WriteLog("Start simulate " + symbols + " @resampling interval =" + resamplinginterval);



        List<string> tradeddate = new List<string>();
        List<string> dupddate = new List<string>();
        string starttradedate = ldate[0];
        string endtradedate = ldate[ldate.Count - 1];
        TimeSpan starttrade = TimeSpan.Parse(starttradetime);
        TimeSpan endtrade = TimeSpan.Parse(endtradetime);
        Dictionary<string, DataTable> resamplingTable = new Dictionary<string, DataTable>(); 
        string constring = @"server=" + System.Environment.MachineName + @"\WiiSQL;Initial Catalog=AssetDB;User ID=sa;Password=wee@dcs.c0m";

        int pstep = 1000; //step for backtest progressbar

        WriteLog("Start Simulate trade", "Sequential");

        //Initial Global port
        Portfolio port = new Portfolio(_accountname);

        foreach (string s in symbols)
        {
            //Init Port
            port.InitPortfolio(s);
            //Init trade logic
            TradeLogic eachlogic = new BigLotsHFTTradeLogicRevised03(s,Feeder.NewResamplingDataTable(), resamplinginterval);
            eachlogic.InitLogicPropTable(resamplinginterval);

            DataHandlerMSSQL eachdmsql = new DataHandlerMSSQL(constring);
            Feeder eachfeed = new Feeder(resamplinginterval, ldate,s,lookbackperiod);


            WriteLog("Start Query from " + starttradedate + " to " + endtradedate + " " + DateTime.Now.ToLongTimeString(), starttradedate);
            var tradestock = eachdmsql.GetBarsTable(s, starttradedate, endtradedate, starttrade, endtrade);
            WriteLog("Number of ticker = " + tradestock.Rows.Count.ToString(), "Sequential");
            WriteLog("End Query: " + DateTime.Now.ToLongTimeString(), endtradedate);

            //Set Backtest ProgessBar
            backtestProgressBar.Invoke(new SetProgressMaximumDelegate(SetProgressMaximum), tradestock.Rows.Count/pstep);

            //Filter only require time

            /*
            DataTable tmpstock = tradestock.Clone();
            
            foreach (DataRow dr in tradestock.Rows)
            {
                //TimeSpan t = TimeSpan.Parse(dr["<TIME>"].ToString());
                //if (starttrade < t)
                tmpstock.Rows.Add(dr.ItemArray);
            }
            */

            //Init one unit test per on trade symbol/interval
            TradeUnit tu = new TradeUnit(eachlogic, s, port);


            //TimeSpan resamplatesttime = new TimeSpan();
            int count = 0;
            string[] techindic = new string[] { "TotalLongVolumePerTime", "TotalShortVolumePerTime"};
            foreach (DataRow dr in tradestock.Rows)
            {
                eachfeed.ResamplingTick(dr);
                tu.OnData(dr, eachfeed);
                count++;
                if ((count/pstep) > backtestProgressBar.Value)
                    backtestProgressBar.Invoke(new AddProgressValueDelegate(AddProgressValue));
            }
            //return port status back to this.port

            //this.port = tu.port;
            if (tu.port.PortfolioTradeTransactionDict[s].Rows.Count > 0)
            {
                foreach (DataRow r in tu.port.PortfolioTradeTransactionDict[s].Rows)
                {
                    if (!this.port.PortfolioTradeTransactionDict.ContainsKey(s))
                        this.port.InitPortfolio(s);
                    this.port.PortfolioTradeTransactionDict[s].Rows.Add(r.ItemArray);
                }
            }


            WriteLog("End Simulate trade", "Sequential");
            //Store Resampling table
            //weerawud
            //resamplingTable.Add(s+"-"+resamplinginterval, eachfeed.ResamplingDataTable);
        }
        //Summmary portolio 
        foreach (string s in symbols)
        {
            this.simulatetestSummary.Rows.Clear();
            //foreach (DataRow r in (port.GetPortfolioTradeDetails(s)).Rows)
            foreach (DataRow r in (port.SummaryPortfolioTradeDetails(s)).Rows)
            {
                //find high and low price of trade period
                //port.GetSimulateSumwithHigLowPrice(r, eachfeed.ResamplingDataTable);
                //weerawud
                //port.GetSimulateSumwithHigLowPrice(r, resamplingTable[s+"-"+resamplinginterval]);
                //Add result
                this.simulatetestSummary.Rows.Add(r.ItemArray);
            }
            
            WriteLog("Write simulate result to database");
            WriteTradeResultToDB();
            WriteLog("End simulate " + symbols + " @resampling interval =" + resamplinginterval);
        }
        
        string[] l = _eventlogs.Eventlogs.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
        TimeSpan stime = TimeSpan.Parse(l[l.Length - 2].Split('@')[0]);//start time log
        TimeSpan etime = TimeSpan.Parse(l[0].Split('@')[0]);//end time log
        TimeSpan totaltime = (etime - stime);
        WriteLog("Total Simulate Trade Time = " + totaltime.ToString());

    }


    public void SimulateTradeUnitParallel(List<string> symbols, List<string> ldate, int resamplinginterval, string starttradetime, string endtradetime, double maxonhandvol, int lookbackperiod)
    {

        simulatetestSummary.Rows.Clear();
        
        
        
        WriteLog("Start simulate " + symbols + " @resampling interval =" + resamplinginterval);



        List<string> tradeddate = new List<string>();
        List<string> dupddate = new List<string>();
        TimeSpan starttrade = TimeSpan.Parse(starttradetime);
        TimeSpan endtrade = TimeSpan.Parse(endtradetime);

        Dictionary<string, Portfolio> portDict = new Dictionary<string, Portfolio>();
        //Dictionary<string, DataTable> resamplingTable = new Dictionary<string, DataTable>();
        string constring = @"server=" + System.Environment.MachineName + @"\WiiSQL;Initial Catalog=AssetDB;User ID=sa;Password=wee@dcs.c0m";

        int pstep = 1; //step for backtest progressbar

        WriteLog("Start Simulate trade", "Parallel");

        //Initial Global port
        Portfolio eachsymbolport = new Portfolio(_accountname);

        foreach (string s in symbols)
        {
            
            //Init Port
            eachsymbolport.InitPortfolio(s);
            //Init trade logic
            //Set Backtest ProgessBar
            backtestProgressBar.Invoke(new SetProgressMaximumDelegate(SetProgressMaximum), ldate.Count);


            int eachldatecount = 0;
            Parallel.ForEach(ldate, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount*2 },
                //Parallel.ForEach(ldate, new ParallelOptions { MaxDegreeOfParallelism = 8 },
            eachldate =>
            {
                Portfolio eachport = new Portfolio(_accountname);
                eachport.InitPortfolio(s);
                DataHandlerMSSQL eachdmsql = new DataHandlerMSSQL(constring);
                TradeLogic eachlogic = new PercentVolTradeLogic(s, Feeder.NewResamplingDataTable(), resamplinginterval);
                eachlogic.InitLogicPropTable(resamplinginterval);
                
                //Feeder eachfeed = new Feeder(resamplinginterval, ldate, s, lookbackperiod);
                Feeder eachfeed = new Feeder(resamplinginterval, eachldate, s, lookbackperiod);
                var tradestock = eachdmsql.GetBarsTable(s, eachldate, eachldate, starttrade, endtrade);
                //var tradestock = eachdmsql.GetBarsTable(s, eachldate);
                
                //Init one unit test per on trade symbol/interval
                TradeUnit eachtu = new TradeUnit(eachlogic, s, eachport);
                
                //TradeUnit eachtu = new TradeUnit(eachlogic, s,ref eachsymbolport);
                //TimeSpan resamplatesttime = new TimeSpan();

                //string[] techindic = new string[] { "TotalLongVolumePerTime", "TotalShortVolumePerTime" };
                foreach (DataRow dr in tradestock.Rows)
                {
                    eachfeed.ResamplingTick(dr);
                    eachtu.OnData(dr, eachfeed);

                }

                if (eachtu.port.PortfolioTradeTransactionDict[s].Rows.Count > 0)
                {
                    foreach (DataRow r in eachtu.port.PortfolioTradeTransactionDict[s].Rows)
                    {
                        //if (!eachsymbolport.PortfolioTradeTransactionDict.ContainsKey(s))
                        //    eachsymbolport.InitPortfolio(s);
                        eachsymbolport.PortfolioTradeTransactionDict[s].Rows.Add(r.ItemArray);
                        //add resampling datatable to each symbo port
                        string tmpdate = ((DateTime)r["datetime"]).ToShortDateString();
                        if (!eachsymbolport.tradeResamplingTableDict.ContainsKey(tmpdate))
                            eachsymbolport.tradeResamplingTableDict.Add(tmpdate, new DataTable());
                        eachsymbolport.tradeResamplingTableDict[tmpdate] = eachfeed.ResamplingDataTable;

                    }
                    
                }
                
                backtestProgressBar.Invoke(new AddProgressValueDelegate(AddProgressValue));
                eachldatecount++;
                WriteLog(eachldatecount +"/"+ ldate.Count+" Number of ticker = " + tradestock.Rows.Count.ToString() +" Simulate Completed", "Parallel");
            }

            );


            //Sort PortfolioTradeTransactionDict[s]
            if (eachsymbolport.PortfolioTradeTransactionDict[s].Rows.Count > 0)
            {


                IEnumerable<DataRow> orderedRows = eachsymbolport.PortfolioTradeTransactionDict[s].AsEnumerable().OrderBy(r => r.Field<DateTime>("datetime"));
                eachsymbolport.PortfolioTradeTransactionDict[s] = orderedRows.CopyToDataTable();

                foreach (DataRow r in eachsymbolport.PortfolioTradeTransactionDict[s].Rows)
                {
                    if (!this.port.PortfolioTradeTransactionDict.ContainsKey(s))
                        port.InitPortfolio(s);
                    port.PortfolioTradeTransactionDict[s].Rows.Add(r.ItemArray);
                }
            }
            portDict.Add(s, eachsymbolport);

            WriteLog("End Simulate trade", "Parallel");
            //Store Resampling table
            //weerawud
            //resamplingTable.Add(s+"-"+resamplinginterval, eachfeed.ResamplingDataTable);
        }
        //Summmary portolio 
        foreach (string s in symbols)
        {
            _symbol = s;
            _interval = resamplinginterval;
            if (port.PortfolioTradeTransactionDict.Count > 0)
            {
                this.simulatetestSummary.Rows.Clear();
                //foreach (DataRow r in (port.GetPortfolioTradeDetails(s)).Rows)

                foreach (DataRow r in (port.SummaryPortfolioTradeDetails(s)).Rows)
                {
                    string tmpdate = r["open date"].ToString();
                    //find high and low price of trade period
                    port.GetSimulateSumwithHigLowPrice(r, portDict[s].tradeResamplingTableDict[tmpdate]);
                    //weerawud
                    //port.GetSimulateSumwithHigLowPrice(r, resamplingTable[s+"-"+resamplinginterval]);
                    //Add result
                    
                    this.simulatetestSummary.Rows.Add(r.ItemArray);
                }
            }
            WriteLog("Write simulate result to database");
            WriteTradeResultToDB();
            WriteLog("End simulate " + symbols + " @resampling interval =" + resamplinginterval);
        }

        string[] l = _eventlogs.Eventlogs.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
        TimeSpan stime = TimeSpan.Parse(l[l.Length - 2].Split('@')[0]);//start time log
        TimeSpan etime = TimeSpan.Parse(l[0].Split('@')[0]);//end time log
        TimeSpan totaltime = (etime - stime);
        WriteLog("Total Simulate Trade Time = " + totaltime.ToString());

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
    public void SimulateTradeSeach(string symbol, List<string> ldate, int resamplinginterval, string starttradetime, string endtradetime)
    {
        WriteLog("Start simulate trade search " + symbol + " @resampling interval =" + resamplinginterval);
        SearchLogic searchlogic = new SearchLogic();
        
        Parallel.ForEach(ldate, new ParallelOptions { MaxDegreeOfParallelism = 8 },
            eachldate => {

                
                
                
                string constring = @"server=" + System.Environment.MachineName + @"\WiiSQL;Initial Catalog=AssetDB;User ID=sa;Password=wee@dcs.c0m";
                DataHandlerMSSQL eachdmsql = new DataHandlerMSSQL(constring);

                WriteLog("Start Query: " + DateTime.Now.ToLongTimeString());

                
                var stock = eachdmsql.GetBarsTable(symbol,eachldate,eachldate,TimeSpan.Parse(starttradetime),TimeSpan.Parse(endtradetime));

                WriteLog("Number of ticker = " + stock.Rows.Count.ToString(), eachldate);
                WriteLog("End Query: " + DateTime.Now.ToLongTimeString());

                if (stock.Rows.Count > 0)
                {
                    WriteLog("Create Intraday Table: " + DateTime.Now.ToLongTimeString());
                    var intstock = eachdmsql.GetResamplingBarsTable(resamplinginterval, stock);
                    WriteLog("Finish Intraday Table: " + DateTime.Now.ToLongTimeString());
                    WriteLog(intstock.Rows.Count.ToString());

                    WriteLog("Calculate Technical Indic: " + DateTime.Now.ToLongTimeString());
                    string[] indic = { "PeriodHigh", "PeriodLow", "PeriodGapHighLow", "TotalLongVolumePerTime", "TotalShortVolumePerTime", "TotalVolumePerTime", "Change%", "Volatility", "SumLongVolume%", "SumShortVolume%", "GapHighLow", "DayHigh", "DayLow" };
                    var techStock = dmsql.CalculateTechnicalIndicator(intstock, indic, 20);
                    WriteLog("Finish Calculate Table: " + DateTime.Now.ToLongTimeString());
                    WriteLog(intstock.Rows.Count.ToString());

                    WriteLog("Start Search for " + symbol + " " + eachldate);
                    searchlogic.SearchTradeReturnTable(techStock);
                    WriteLog("End Search for " + symbol + " " + eachldate);
                }

                





                /*
                WriteLog("Start Simulate trade", eachldate);
                //DataTable stock;

                TradeUnit tu = new TradeUnit(eachlogic, _accountname, symbol, maxonhandvol);

                int count = 0;
                TimeSpan resamplatesttime = new TimeSpan();
                foreach (DataRow dr in tmpstock.Rows)
                {
                    count++;

                    eachfeed.ResamplingTick(dr);

                    DataTable resampdt = eachfeed.ResamplingDataTable;
                    if (((TimeSpan)resampdt.Rows[resampdt.Rows.Count - 1]["Time"]) >= resamplatesttime)
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
                */
            }
            );
        WriteLog("End simulate " + symbol + " @resampling interval =" + resamplinginterval);
        string[] l = _eventlogs.Eventlogs.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
        TimeSpan stime = TimeSpan.Parse(l[l.Length - 2].Split('@')[0]);//start time log
        TimeSpan etime = TimeSpan.Parse(l[0].Split('@')[0]);//end time log
        TimeSpan totaltime = (etime - stime);
        WriteLog("Total Simulate Trade Search Time = " + totaltime.ToString());
    }

    public void SimulateBestTradeUnit(string symbol, List<string> ldate, int resamplinginterval, string starttradetime, string endtradetime, double expectreturnrate, double maxonhandvol)
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

        //Get List properties name that want to adjust
        TradeLogic tmplogic = new BigLotsHFTTradeLogicRevised01(symbol, Feeder.NewResamplingDataTable(), resamplinginterval);
        List<string> propnameList = tmplogic.GetAdjustedPropertyNamesList();
        Dictionary<string, double[]> propDict = new Dictionary<string, double[]>();
        foreach (string p in propnameList)
        {
            double[] propvalrange = tmplogic.GetLogicPropTableValueRange(p);
            propDict.Add(p, propvalrange);
        }
        //End Get Value

        foreach (KeyValuePair<string, double[]> kv in propDict)
        {
            //property then loop each value
            foreach (double val in kv.Value)
            {
                //Initial base propertytabledict
                tmplogic.InitLogicPropTable(resamplinginterval);
                //Reassign best property
                Dictionary<string, Property> tmpBestPropTable = this.GetPropertyTableBestProfit(expectreturnrate, tmplogic.LogicPropTable);
                this.CopyPropertyTable(tmplogic.LogicPropTable, tmpBestPropTable);
                //Change new property value
                tmplogic.ChangeLogicPropTable(kv.Key, val);

                Parallel.ForEach(ldate, new ParallelOptions { MaxDegreeOfParallelism = 2 },
                    eachldate => {


                        //Start Process trade
                        TradeLogic eachlogic = new BigLotsHFTTradeLogicRevised01(symbol,Feeder.NewResamplingDataTable(), resamplinginterval);


                        //Dynamic assign best property
                        eachlogic.LogicPropTable = tmplogic.LogicPropTable;

                        string constring = @"server=" + System.Environment.MachineName + @"\WiiSQL;Initial Catalog=AssetDB;User ID=sa;Password=wee@dcs.c0m";
                        DataHandlerMSSQL eachdmsql = new DataHandlerMSSQL(constring);


                        //_tradedate = eachldate;
                        //feed = new Feeder(resamplinginterval, eachldate);

                        Feeder eachfeed = new Feeder(resamplinginterval, eachldate, symbol);
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
                            //TimeSpan t = TimeSpan.Parse(dr["<TIME>"].ToString());
                            //if (starttrade < t)
                            tmpstock.Rows.Add(dr.ItemArray);
                        }

                        WriteLog("Start Simulate trade", eachldate);
                        //DataTable stock;

                        TradeUnit tu = new TradeUnit(eachlogic, _accountname, symbol, maxonhandvol);

                        int count = 0;
                        TimeSpan resamplatesttime = new TimeSpan();
                        foreach (DataRow dr in tmpstock.Rows)
                        {
                            count++;

                            eachfeed.ResamplingTick(dr);

                            DataTable resampdt = eachfeed.ResamplingDataTable;
                            if (((TimeSpan)resampdt.Rows[resampdt.Rows.Count - 1]["Time"]) >= resamplatesttime)
                                tu.OnData(dr, eachfeed);
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

                WriteLog(String.Format("Store Profit Rate Return Property for {0} = {1}", kv.Key, val.ToString()));
                //Adjusted logic table
                StoreProfitReturnRateProperty(tmplogic.LogicPropTable);

                WriteLog("Write simulate result to database");
                WriteTradeResultToDB(tmplogic);
                this.simulatetestSummary.Rows.Clear();
                WriteLog("End simulate " + symbol + " @resampling interval =" + resamplinginterval);

            }
        }
        string[] l = _eventlogs.Eventlogs.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
        TimeSpan stime = TimeSpan.Parse(l[l.Length - 2].Split('@')[0]);//start time log
        TimeSpan etime = TimeSpan.Parse(l[0].Split('@')[0]);//end time log
        TimeSpan totaltime = (etime - stime);
        WriteLog("Total Simulate Trade Time = " + totaltime.ToString());
    }



    public void SearchDailyParallel(List<string> symbols, List<string> ldate, int resamplinginterval, string starttradetime, string endtradetime, double maxonhandvol, int lookbackperiod)
    {
        
        WriteLog("Start parallel search daily @resampling interval =" + resamplinginterval);



        List<string> tradeddate = new List<string>();
        TimeSpan starttrade = TimeSpan.Parse(starttradetime);
        TimeSpan endtrade = TimeSpan.Parse(endtradetime);

        string constring = @"server=" + System.Environment.MachineName + @"\WiiSQL;Initial Catalog=AssetDB;User ID=sa;Password=wee@dcs.c0m";

        int pstep = 1; //step for backtest progressbar
        int maxbar = ldate.Count * symbols.Count;
        int eachldatecount = 0;
        //Set Backtest ProgessBar
        backtestProgressBar.Invoke(new SetProgressMaximumDelegate(SetProgressMaximum), maxbar);

        foreach (string s in symbols)
        {

                


            
            Parallel.ForEach(ldate, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 },
            //Parallel.ForEach(ldate, new ParallelOptions { MaxDegreeOfParallelism = 8 },
            eachldate =>
            {
                DataHandlerMSSQL eachdmsql = new DataHandlerMSSQL(constring);
                SearchDailyLogic eachsearchlogic = new SearchDailyLogic(resamplinginterval);
                eachsearchlogic.InitLogicPropTable(resamplinginterval); 


                //Feeder eachfeed = new Feeder(resamplinginterval, ldate, s, lookbackperiod);
                Feeder eachfeed = new Feeder(resamplinginterval, eachldate, s, lookbackperiod);
                var tradestock = eachdmsql.GetBarsTable(s, eachldate, eachldate, starttrade, endtrade);
                foreach (DataRow dr in tradestock.Rows)
                {
                    eachfeed.ResamplingTick(dr);
                }
                DataTable resampdt = eachfeed.ResamplingDataTable;
                //Search



                backtestProgressBar.Invoke(new AddProgressValueDelegate(AddProgressValue));
                eachldatecount++;
                WriteLog(s+" : "+eachldatecount + "/" + maxbar + " Number of ticker = " + tradestock.Rows.Count.ToString() + " Simulate Completed", "Parallel");
            }

            );
            
        }
        

        string[] l = _eventlogs.Eventlogs.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
        TimeSpan stime = TimeSpan.Parse(l[l.Length - 2].Split('@')[0]);//start time log
        TimeSpan etime = TimeSpan.Parse(l[0].Split('@')[0]);//end time log
        TimeSpan totaltime = (etime - stime);
        WriteLog("Total Simulate Trade Time = " + totaltime.ToString());
    }


    //Progress Bar
    private delegate void AddProgressValueDelegate();
    private void AddProgressValue()
    {
        backtestProgressBar.PerformStep();
    }

    private delegate void SetProgressMaximumDelegate(int max);
    private void SetProgressMaximum(int max)
    {
        backtestProgressBar.Value = 0;
        backtestProgressBar.Step = 1;
        backtestProgressBar.Maximum = max;
        backtestProgressBar.Refresh();
    }

}