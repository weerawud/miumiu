using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Deedle;
using System.Data.SqlClient;
using System.Globalization;
using Accord.Statistics;

abstract class TradeLogic 
{
    public DataTable HistoricalTable;
    public List<Feeder> FeederDict = new List<Feeder>();
    public DataTable SignalTable;
    public Dictionary<String,Property> LogicPropTable;
    public Dictionary<String, string> GlobalVarTable;
    public Double MaxOnhandVol;
    public Double CurrentOnhandVol;
    public Double TradeVol;
    public DateTime NextTradeDateTime;
    abstract public void AddFeeder(Feeder _feed);
    abstract public void UpdateHistoricalTable(DataTable dt);
    abstract public DataTable NewSignalTable();
    abstract public void InitLogicPropTable(int interval);
    abstract public List<string> GetAdjustedPropertyNamesList();

    abstract public DataRow OnData(string symbol, DataRow resamplingdr, ref Portfolio port);

    abstract public DataRow OpenLongStatus(Portfolio port);
    abstract public DataRow OpenShortStatus(Portfolio port);
    abstract public DataRow CloseLongStatus(Portfolio port);
    abstract public DataRow CloseShortStatus(Portfolio port);

    public TradeLogic()
    {
        //Init variable
        MaxOnhandVol = 1;
        TradeVol = 1;
        CurrentOnhandVol = 0;
    }
    public string GetLogicName()
    {
        return this.GetType().Name;
    }
    public string symbol;
    public void ChangeLogicPropTable(string propname, double val)
    {
        LogicPropTable[propname].Value = val;
    }
    public void InitLogicPropTable(Dictionary<string, Property> propDict, int interval)
    {
        this.LogicPropTable = propDict;
        this.ChangeLogicPropTable("Interval", interval);
    }
    public DataTable GetPropertyTable(string id)
    {
        DataTable dt = this.NewLogicPropertyTable();
        dt.TableName = this.GetType().Name;
        foreach (KeyValuePair<string, Property> kv in LogicPropTable)
        {
            DataRow dr = dt.NewRow();
            dr["id"] = id;
            dr["propertyname"] = kv.Key;
            dr["value"] = kv.Value.Value;
            dr["min"] = kv.Value.Min;
            dr["max"] = kv.Value.Max;
            dr["step"] = kv.Value.Step;
            dt.Rows.Add(dr.ItemArray);
        }
        return dt;
    }
    public double[] GetLogicPropTableValueRange(string propname)
    {
        return LogicPropTable[propname].GetValueRange();
    }
    private DataTable NewLogicPropertyTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[6];
        col[0] = new DataColumn("id", typeof(System.String));
        col[1] = new DataColumn("propertyname", typeof(System.String));
        col[2] = new DataColumn("value", typeof(System.Double));
        col[3] = new DataColumn("min", typeof(System.Double));
        col[4] = new DataColumn("max", typeof(System.Double));
        col[5] = new DataColumn("step", typeof(System.Double));

        dt.Columns.AddRange(col);
        return dt;
    }
    public DataRow signalTradeNone;

}

class BigLotsHFTTradeLogicRevised01 : TradeLogic
{
    TimeSpan NextTradeTime;
    public BigLotsHFTTradeLogicRevised01(string _symbol,DataTable newResamplingTable, int interval)
    {
        //Update historical table
        symbol = _symbol;
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.GlobalVarTable = new Dictionary<String, string>();
        this.InitLogicPropTable(interval);
        
        //Init Next trade time
        NextTradeTime = TimeSpan.FromSeconds(0);
        NextTradeDateTime = new DateTime();
        //signalTradeNone = GetSingnal("none", 0, 0);
        
    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume", 400, 200, 1000, 200));//400
        LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", -400, -1000, -200, 200));//400
        LogicPropTable.Add("TotalLongTrans", new Property("TotalLongTrans", 200, 100, 200, 100));
        LogicPropTable.Add("TotalShortTrans", new Property("TotalShortTrans", 200, 100, 200, 100));
        LogicPropTable.Add("SwingGap", new Property("SwingGap", 7, 0.5, 2, 0.5));
        LogicPropTable.Add("TargetGap", new Property("TargetGap", 4, 1, 5, 1));
        LogicPropTable.Add("DayGap", new Property("DayGap", 3, 5, 12, 1));
        LogicPropTable.Add("LongGap", new Property("LongGap", -0.5, 0, 3, 0.5));
        LogicPropTable.Add("ShortGap", new Property("ShortGap", -0.5, 0, 3, 0.5));

        
        LogicPropTable.Add("TimeLookBack", new Property("TimeLookBack", 1200, 100, 100, 100));
        double targettimetrade = TimeSpan.Parse("16:00:00").TotalSeconds;
        LogicPropTable.Add("TargetTimeTrade", new Property("TargetTimeTrade", targettimetrade, targettimetrade, targettimetrade, 0));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 2, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 2, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 6, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 6, 1, 30, 1));
        double openstarttimetrade = TimeSpan.Parse("09:30:00").TotalSeconds;//15:40 best
        double openendtimetrade = TimeSpan.Parse("16:25:00").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("OpenSignalGap", new Property("OpenSignalGap", 0, 1, 10, 1));
        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 30, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -30, -500, -40, 50));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", -6, -8, -2, 1));
        LogicPropTable.Add("StopGain", new Property("StopGain", 0, 2, 5, 0.5));
        LogicPropTable.Add("StopProfitLoss", new Property("StopProfitLoss", -3, -8, -2, 1));
        //double openstarttimetrade = TimeSpan.Parse("14:20:00").TotalSeconds;
        //double openendtimetrade = TimeSpan.Parse("16:25:00").TotalSeconds;
        //double endtimetrade = TimeSpan.Parse("16:54:00").TotalSeconds;
        
        LogicPropTable.Add("TotalLongVolPercent", new Property("TotalLongVolPercent", 35, -8, -2, 1));
        LogicPropTable.Add("TotalShortVolPercent", new Property("TotalShortVolPercent", 35, -8, -2, 1));
        //new trade property
        
        double endtimetrade = TimeSpan.Parse("16:55:00").TotalSeconds;
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));
        double nexttradetime = TimeSpan.Parse("00:01:00").TotalSeconds;
        LogicPropTable.Add("NextTradeTime", new Property("NextTradeTime", nexttradetime, nexttradetime, nexttradetime, 0));
        LogicPropTable.Add("MaxLongOnhandVol", new Property("MaxLongOnhandVol", 1, 1, 10, 1));
        LogicPropTable.Add("MaxShortOnhandVol", new Property("MaxShortOnhandVol", -1, 1, 10, 1));
        LogicPropTable.Add("MaxOnhandCost", new Property("MaxOnhandCost", 1000000, 100000, 1000000, 100000));
        this.GlobalVarTable.Clear();
        this.InitVarTable();
    }
    public override void AddFeeder(Feeder _feed)
    {
        
    }
    public override List<string> GetAdjustedPropertyNamesList()
    {
        List<string> propList = new List<string>();

        propList.Add("TargetGap");
        //propList.Add("DayGap");
        //propList.Add("TotalLongVolume");
        //propList.Add("TotalShortVolume");
        return propList;
    }


    public void InitVarTable()
    {
        this.GlobalVarTable.Add("Lower High-Gap", "0");
        this.GlobalVarTable.Add("BreakHigh", "0");
        this.GlobalVarTable.Add("Higher Low+Gap", "0");
        this.GlobalVarTable.Add("BreakLow", "0");
        this.GlobalVarTable.Add("PrevProfit", "0");
        this.GlobalVarTable.Add("OpenAfternoon", "0");
        this.GlobalVarTable.Add("ReverseLong", "0");
        this.GlobalVarTable.Add("ReverseShort", "0");
        this.GlobalVarTable.Add("StopTrade", "0");
    }
    public void ClearVarTable()
    {
        this.GlobalVarTable["Lower High-Gap"] = "0";
        this.GlobalVarTable["BreakHigh"] = "0";
        this.GlobalVarTable["Higher Low+Gap"] = "0";
        this.GlobalVarTable["BreakLow"] = "0";
        this.GlobalVarTable["OpenAfternoon"] = "0";
        this.GlobalVarTable["ReverseLong"] = "0";
        this.GlobalVarTable["ReverseShort"] = "0";
        this.GlobalVarTable["StopTrade"] = "0";

    }
    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[7];
        
        col[0] = new DataColumn("datetime", typeof(System.DateTime)); //eg. 9:45 
        col[1] = new DataColumn("status", typeof(System.String));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", typeof(System.Double)); //price
        col[3] = new DataColumn("openshort", typeof(System.Double));//price
        col[4] = new DataColumn("closelong", typeof(System.Double));//price
        col[5] = new DataColumn("closeshort", typeof(System.Double));//price
        col[6] = new DataColumn("tradevol", typeof(System.Double));//price
        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }
    
    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
        sigrow["tradevol"] = this.TradeVol;
        switch (status)
        {
            case "none":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "dup":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openlong":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closelong":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = price;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closeshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = price;
                    break;
                }
            default:
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
        }
        return sigrow;
    }



    private double CloseStopLoss(double onhandcost, string positionside)
    {
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stoploss = this.LogicPropTable["StopLoss"].Value;
        double stoplossprice = 0;
        double close = 0;
        double profit = 0;
        if (stoploss != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
            {
                profit = close - onhandcost;
            }
            else if (positionside == "openshort")
            {
                profit = onhandcost - close;
            }
            if (profit < stoploss)
            {

                stoplossprice = close;


            }
        }
        return stoplossprice;
    }
    private double CloseStopGain(double onhandcost, string positionside)
    {
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopgain = this.LogicPropTable["StopGain"].Value;
        double stopgainprice = 0;
        double close = 0;
        double profit = 0;
        if (stopgain != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit > stopgain)
            {

                stopgainprice = close;
            }
        }
        return stopgainprice;
    }
    private double CloseStopProfitLoss(double onhandcost, string positionside)
    {
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopprofitloss = this.LogicPropTable["StopProfitLoss"].Value;
        double prevprofit = Double.Parse(this.GlobalVarTable["PrevProfit"]);
        double stopprofitlossprice = 0;
        double close = 0;
        double profit = 0;
        double profitdiff = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
        if (positionside == "openlong")
            profit = close - onhandcost;
        else if (positionside == "openshort")
            profit = onhandcost - close;
        this.GlobalVarTable["PrevProfit"] = profit.ToString();//Update previous profit

        if ((stopprofitloss != 0) && (profit > 0) && (prevprofit > 0))
        {
            profitdiff = profit - prevprofit;
            if (profitdiff < stopprofitloss)
                stopprofitlossprice = close;
        }
        return stopprofitlossprice;

    }


    public override DataRow OpenLongStatus(Portfolio port)
    {
        double onhandcost = port.CostOnhand(symbol);
        double price = 0;//not hit any condition

        //Start default trade vol = 1
        this.TradeVol = 1;

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        TimeSpan targettimetrade = TimeSpan.FromSeconds((Double)this.LogicPropTable["TargetTimeTrade"].Value);
        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double high = (Double)this.HistoricalTable.Rows[counthistrow - 1]["High"];
        double low = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Low"];
        double vol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Volume"];
        double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];



        double targetgap = this.LogicPropTable["TargetGap"].Value;
        double targetlongvolpercent = this.LogicPropTable["TotalLongVolPercent"].Value;
        double targetshortvolpercent = this.LogicPropTable["TotalShortVolPercent"].Value;
        double ltimesec = this.LogicPropTable["TimeLookBack"].Value;

        double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
        double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
        double tlongtrans = this.LogicPropTable["TotalLongTrans"].Value;
        double tshorttrans = this.LogicPropTable["TotalShortTrans"].Value;
        double tlonggap = this.LogicPropTable["LongGap"].Value;
        double tshortgap = this.LogicPropTable["ShortGap"].Value;
        double tswinggap = this.LogicPropTable["SwingGap"].Value;
        double tdaygap = this.LogicPropTable["DayGap"].Value;

        double lastbiglotslongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
        double lastbiglotsshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];

        /*
        if (time == (TimeSpan.Parse("14:30:00")))
            this.VarTable["OpenAfternoon"] = open.ToString();
        */
        if ((counthistrow > monitor) && (time >= openstarttime) && (time <= openendtime) && (time != (TimeSpan.Parse("14:15:00")) && (time != (TimeSpan.Parse("09:45:00")))))
        {

            found = 0;

            //Check gap high low lookback unitil expect x seconds
            double tmpvol = 0;
            double tmpnewshorthigh = 0;
            double tmpnewlonglow = 0;
            double tmpcount = 0;
            double tmpclose = (Double)this.HistoricalTable.Rows[counthistrow - 2]["Close"];

            double tmphigh = 0;
            double tmplow = 0;
            double tmphl = 0;
            TimeSpan gaptime = TimeSpan.FromSeconds(0);
            double tmpdayhigh = (Double)this.HistoricalTable.Rows[counthistrow - 2]["DayHigh"];
            double tmpdaylow = (Double)this.HistoricalTable.Rows[counthistrow - 2]["DayLow"];


            for (int m = 2; m < monitor; m++)
            {
                double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                double daylowprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                double biglotclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Close"];
                double totallongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalLongVolume"];
                double totalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"];
                double percentlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["%Volume Long"];
                double percentshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["%Volume Short"];

                double biglotgaphl = dayhighprice - daylowprice;
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];

                if (totallongvol > tlongvol)
                {
                    if (biglotclose <= tmpclose - tlonggap)
                        found++;
                }
                /*if (totalshortvol < tshortvol)
                {
                    if (biglotclose >= tmpclose + tshortgap)
                        found++;
                }*/



            }

            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = close;
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("openlong", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    public override DataRow OpenShortStatus(Portfolio port)
    {
        double onhandcost = port.CostOnhand(symbol);
        double price = 0;//not hit any condition
                         //Start Trade vol = 1
        this.TradeVol = 1;

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        TimeSpan targettimetrade = TimeSpan.FromSeconds((Double)this.LogicPropTable["TargetTimeTrade"].Value);
        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double high = (Double)this.HistoricalTable.Rows[counthistrow - 1]["High"];
        double low = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Low"];
        double vol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Volume"];
        double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];

        double targetgap = this.LogicPropTable["TargetGap"].Value;
        double targetlongvolpercent = this.LogicPropTable["TotalLongVolPercent"].Value;
        double targetshortvolpercent = this.LogicPropTable["TotalShortVolPercent"].Value;
        double ltimesec = this.LogicPropTable["TimeLookBack"].Value;

        double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
        double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
        double tlongtrans = this.LogicPropTable["TotalLongTrans"].Value;
        double tshorttrans = this.LogicPropTable["TotalShortTrans"].Value;
        double tlonggap = this.LogicPropTable["LongGap"].Value;
        double tshortgap = this.LogicPropTable["ShortGap"].Value;
        double tswinggap = this.LogicPropTable["SwingGap"].Value;
        double tdaygap = this.LogicPropTable["DayGap"].Value;

        double lastbiglotslongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
        double lastbiglotsshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];

        if ((counthistrow > monitor) && (time >= openstarttime) && (time <= openendtime) && (time != (TimeSpan.Parse("14:15:00")) && (time != (TimeSpan.Parse("09:45:00")))))
        {
            found = 0;

            //Check gap high low lookback unitil expect x seconds
            double tmpvol = 0;
            double tmpnewshorthigh = 0;
            double tmpnewlonglow = 0;
            double tmpcount = 0;
            double tmpclose = (Double)this.HistoricalTable.Rows[counthistrow - 2]["Close"];


            for (int m = 2; m < monitor; m++)
            {
                double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                double daylowprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                double biglotclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Close"];
                double totallongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalLongVolume"];
                double totalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"];
                double percentlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["%Volume Long"];
                double percentshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["%Volume Short"];

                double biglotgaphl = dayhighprice - daylowprice;
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];


                if (totalshortvol < tshortvol)
                {
                    if (biglotclose >= tmpclose + tshortgap)
                        found++;
                }/*
                if (totallongvol > tlongvol)
                {
                    if (biglotclose <= tmpclose - tlonggap)
                        found++;
                }*/



            }
            if ((found >= this.LogicPropTable["SignalShort"].Value))
            {
                price = open;
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("openshort", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    public override DataRow CloseLongStatus(Portfolio port)
    {
        double onhandcost = port.CostOnhand(symbol);
        double price = 0;
        double stoplossprice = this.CloseStopLoss(onhandcost, "openlong");
        double stopgainprice = this.CloseStopGain(onhandcost, "openlong");
        double stopprofitlossprice = this.CloseStopProfitLoss(onhandcost, "openlong");
        if (stoplossprice > 0)
            price = stoplossprice; //Stop loss
        else if (stopgainprice > 0)
            price = stopgainprice;//Stop gain
        else if (stopprofitlossprice > 0)//Stop profit loss
            price = stopprofitlossprice;

        else
        {
            //price = this.CloseLongCheckGap();
            //price = this.OpenShortStatus(onhandcost);
            int counthistrow = this.HistoricalTable.Rows.Count;
            double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];
            double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
            double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
            if (biglotssumshortvol < tshortvol)
                price = close;
            /*
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            
            if (time >= endtime)
                price = close;*/
        }

        if (price > 0)
        {
            //Update tradevol, close all onhandvol
            this.TradeVol = this.CurrentOnhandVol;
            this.ClearVarTable();

            if (stoplossprice > 0)
            {
                GlobalVarTable["ReverseShort"] = "1";
                GlobalVarTable["StopTrade"] = "1";
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("closelong", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    public override DataRow CloseShortStatus(Portfolio port)
    {
        double onhandcost = port.CostOnhand(symbol);
        double price = 0;
        double stoplossprice = this.CloseStopLoss(onhandcost, "openshort");
        double stopgainprice = this.CloseStopGain(onhandcost, "openshort");
        double stopprofitlossprice = this.CloseStopProfitLoss(onhandcost, "openshort");
        if (stoplossprice > 0)
            price = stoplossprice; //Stop loss
        else if (stopgainprice > 0)
            price = stopgainprice;//Stop gain
        else if (stopprofitlossprice > 0)
            price = stopprofitlossprice;//Stop loss profit
        else
        {
            //price = this.CloseLongCheckGap();
            //price = this.OpenShortStatus(onhandcost);
            int counthistrow = this.HistoricalTable.Rows.Count;
            double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];
            double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
            double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
            if (biglotssumlongvol > tlongvol)
                price = close;
            /*
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            
            if (time >= endtime)
                price = close;*/
        }
        if (price > 0)
        {
            //Update tradevol, close all onhandvol
            this.TradeVol = this.CurrentOnhandVol;
            this.ClearVarTable();
            if (stoplossprice > 0)
            {
                GlobalVarTable["ReverseLong"] = "1";
                GlobalVarTable["StopTrade"] = "1";
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("closeshort", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    private double CloseStopLoss(Portfolio port,string positionside)
    {
        double onhandcost = port.CostOnhand(symbol);
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stoploss = this.LogicPropTable["StopLoss"].Value;
        double stoplossprice = 0;
        double close = 0;
        double profit = 0;
        if (stoploss != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
            {
                profit = close - onhandcost;
            }
            else if (positionside == "openshort")
            {
                profit = onhandcost - close;
            }
            if (profit < stoploss)
            {

                stoplossprice = close;


            }
        }
        return stoplossprice;
    }
    private double CloseStopGain(Portfolio port,string positionside)
    {
        double onhandcost = port.CostOnhand(symbol);
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopgain = this.LogicPropTable["StopGain"].Value;
        double stopgainprice = 0;
        double close = 0;
        double profit = 0;
        if (stopgain != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit > stopgain)
            {

                stopgainprice = close;
            }
        }
        return stopgainprice;
    }
    private double CloseStopProfitLoss(Portfolio port,string positionside)
    {
        double onhandcost = port.CostOnhand(symbol);
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopprofitloss = this.LogicPropTable["StopProfitLoss"].Value;
        double prevprofit = Double.Parse(this.GlobalVarTable["PrevProfit"]);
        double stopprofitlossprice = 0;
        double close = 0;
        double profit = 0;
        double profitdiff = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
        if (positionside == "openlong")
            profit = close - onhandcost;
        else if (positionside == "openshort")
            profit = onhandcost - close;
        this.GlobalVarTable["PrevProfit"] = profit.ToString();//Update previous profit

        if ((stopprofitloss != 0) && (profit > 0) && (prevprofit > 0))
        {
            profitdiff = profit - prevprofit;
            if (profitdiff < stopprofitloss)
                stopprofitlossprice = close;
        }
        return stopprofitlossprice;

    }

    public override DataRow OnData(string symbol, DataRow resamplingtick, ref Portfolio port)
    {
        double onhandvol = port.VolOnhand(symbol);
        //first init signalTradeNone for reduce CPU time
        if (signalTradeNone == null)
            signalTradeNone = GetSingnal("null", 0, 0);

        DataRow tradesignal = signalTradeNone;//GetSignal("none",0,0);
        
        DateTime tradedatetime = ((DateTime)resamplingtick[0]).Add((TimeSpan)resamplingtick[1]);
        //DateTime endtradedatetime = ((DateTime)resamplingtick[0]).Add(TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value));

        if (tradedatetime >= NextTradeDateTime)
        {
            if (port.IsLong(symbol))// hold long
            {

                //Check OpenLong
                if (port.VolOnhand(symbol) < (double)LogicPropTable["MaxLongOnhandVol"].Value)
                {
                    tradesignal = this.OpenLongStatus(port);

                }
                if ((double)tradesignal["tradevol"] == 0)//if no openlong check close status
                {
                    tradesignal = this.CloseLongStatus(port);
                }

            }
            else if (port.IsShort(symbol) )// hold short
            {

                //Check Openshort
                if (port.VolOnhand(symbol) > (double)LogicPropTable["MaxShortOnhandVol"].Value)
                {
                    tradesignal = this.OpenShortStatus(port);

                }
                if ((double)tradesignal["tradevol"] == 0)//if no openlong check close status
                {
                    tradesignal = this.CloseShortStatus(port);
                }
            }
            else //none hold
            {
                //check openlong first if not found check openshort
                tradesignal = this.OpenLongStatus(port);
                if ((double)tradesignal["tradevol"] == 0)//if no openlong check close status
                {
                    tradesignal = this.OpenShortStatus(port);
                }
            }
            //Check trade 
            if ((double)tradesignal["tradevol"] != 0)
                //NextTradeTime = (TimeSpan)resamplingtick[1] + TimeSpan.FromSeconds((Double)this.LogicPropTable["NextTradeTime"].Value);
                NextTradeDateTime = ((DateTime)resamplingtick[0]).Add((TimeSpan)resamplingtick[1]+TimeSpan.FromSeconds((Double)this.LogicPropTable["NextTradeTime"].Value));
        }
        this.SignalTable.Rows.Add(tradesignal.ItemArray);
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }

    private DataRow GetSingnal(string status, double price, double volume)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
        sigrow["tradevol"] = volume;
        switch (status)
        {
            case "none":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "dup":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openlong":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closelong":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = price;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closeshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = price;
                    break;
                }
            default:
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
        }
        return sigrow;
    }

}


class BigLotsHFTTradeLogicRevised02 : TradeLogic
{
    TimeSpan NextTradeTime;
    public BigLotsHFTTradeLogicRevised02(string _symbol, DataTable newResamplingTable, int interval)
    {
        //Update historical table
        symbol = _symbol;
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.GlobalVarTable = new Dictionary<String, string>();
        this.InitLogicPropTable(interval);

        //Init Next trade time
        NextTradeTime = TimeSpan.FromSeconds(0);
        NextTradeDateTime = new DateTime();
        //signalTradeNone = GetSingnal("none", 0, 0);

    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume", 300, 200, 1000, 200));//400
        LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", -300, -1000, -200, 200));//400
        LogicPropTable.Add("TotalLongTrans", new Property("TotalLongTrans", 200, 100, 200, 100));
        LogicPropTable.Add("TotalShortTrans", new Property("TotalShortTrans", 200, 100, 200, 100));
        LogicPropTable.Add("SwingGap", new Property("SwingGap", 7, 0.5, 2, 0.5));
        LogicPropTable.Add("TargetGap", new Property("TargetGap", 4, 1, 5, 1));
        LogicPropTable.Add("DayGap", new Property("DayGap", 3, 5, 12, 1));
        LogicPropTable.Add("LongGap", new Property("LongGap", 2, 0, 3, 0.5));
        LogicPropTable.Add("ShortGap", new Property("ShortGap", 2, 0, 3, 0.5));
        double endtimetrade = TimeSpan.Parse("16:54:00").TotalSeconds;
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", -4, -8, -2, 1));
        LogicPropTable.Add("StopGain", new Property("StopGain", 12, 2, 5, 0.5));
        LogicPropTable.Add("StopProfitLoss", new Property("StopProfitLoss",0, -8, -2, 1));



        LogicPropTable.Add("TimeLookBack", new Property("TimeLookBack", 1200, 100, 100, 100));
        double targettimetrade = TimeSpan.Parse("16:00:00").TotalSeconds;
        LogicPropTable.Add("TargetTimeTrade", new Property("TargetTimeTrade", targettimetrade, targettimetrade, targettimetrade, 0));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 3, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 3, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 6, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 6, 1, 30, 1));
        double openstarttimetrade = TimeSpan.Parse("09:30:00").TotalSeconds;//15:40 best
        double openendtimetrade = TimeSpan.Parse("16:25:00").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("OpenSignalGap", new Property("OpenSignalGap", 0, 1, 10, 1));
        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 30, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -30, -500, -40, 50));
        
        //double openstarttimetrade = TimeSpan.Parse("14:20:00").TotalSeconds;
        //double openendtimetrade = TimeSpan.Parse("16:25:00").TotalSeconds;
        //double endtimetrade = TimeSpan.Parse("16:54:00").TotalSeconds;

        LogicPropTable.Add("TotalLongVolPercent", new Property("TotalLongVolPercent", 35, -8, -2, 1));
        LogicPropTable.Add("TotalShortVolPercent", new Property("TotalShortVolPercent", 35, -8, -2, 1));
        //new trade property

        
        double nexttradetime = TimeSpan.Parse("00:01:00").TotalSeconds;
        LogicPropTable.Add("NextTradeTime", new Property("NextTradeTime", nexttradetime, nexttradetime, nexttradetime, 0));
        LogicPropTable.Add("MaxLongOnhandVol", new Property("MaxLongOnhandVol", 1, 1, 10, 1));
        LogicPropTable.Add("MaxShortOnhandVol", new Property("MaxShortOnhandVol", -1, 1, 10, 1));
        LogicPropTable.Add("MaxOnhandCost", new Property("MaxOnhandCost", 1000000, 100000, 1000000, 100000));
        this.GlobalVarTable.Clear();
        this.InitVarTable();
    }
    public override void AddFeeder(Feeder _feed)
    {

    }
    public override List<string> GetAdjustedPropertyNamesList()
    {
        List<string> propList = new List<string>();

        propList.Add("TargetGap");
        //propList.Add("DayGap");
        //propList.Add("TotalLongVolume");
        //propList.Add("TotalShortVolume");
        return propList;
    }


    public void InitVarTable()
    {
        this.GlobalVarTable.Add("Lower High-Gap", "0");
        this.GlobalVarTable.Add("BreakHigh", "0");
        this.GlobalVarTable.Add("Higher Low+Gap", "0");
        this.GlobalVarTable.Add("BreakLow", "0");
        this.GlobalVarTable.Add("PrevProfit", "0");
        this.GlobalVarTable.Add("OpenAfternoon", "0");
        this.GlobalVarTable.Add("ReverseLong", "0");
        this.GlobalVarTable.Add("ReverseShort", "0");
        this.GlobalVarTable.Add("StopTrade", "0");
    }
    public void ClearVarTable()
    {
        this.GlobalVarTable["Lower High-Gap"] = "0";
        this.GlobalVarTable["BreakHigh"] = "0";
        this.GlobalVarTable["Higher Low+Gap"] = "0";
        this.GlobalVarTable["BreakLow"] = "0";
        this.GlobalVarTable["OpenAfternoon"] = "0";
        this.GlobalVarTable["ReverseLong"] = "0";
        this.GlobalVarTable["ReverseShort"] = "0";
        this.GlobalVarTable["StopTrade"] = "0";

    }
    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[7];

        col[0] = new DataColumn("datetime", typeof(System.DateTime)); //eg. 9:45 
        col[1] = new DataColumn("status", typeof(System.String));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", typeof(System.Double)); //price
        col[3] = new DataColumn("openshort", typeof(System.Double));//price
        col[4] = new DataColumn("closelong", typeof(System.Double));//price
        col[5] = new DataColumn("closeshort", typeof(System.Double));//price
        col[6] = new DataColumn("tradevol", typeof(System.Double));//price
        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }

    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
        sigrow["tradevol"] = this.TradeVol;
        switch (status)
        {
            case "none":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "dup":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openlong":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closelong":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = price;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closeshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = price;
                    break;
                }
            default:
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
        }
        return sigrow;
    }



    private double CloseStopLoss(double onhandcost, string positionside)
    {
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stoploss = this.LogicPropTable["StopLoss"].Value;
        double stoplossprice = 0;
        double close = 0;
        double profit = 0;
        if (stoploss != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
            {
                profit = close - onhandcost;
            }
            else if (positionside == "openshort")
            {
                profit = onhandcost - close;
            }
            if (profit < stoploss)
            {

                stoplossprice = close;


            }
        }
        return stoplossprice;
    }
    private double CloseStopGain(double onhandcost, string positionside)
    {
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopgain = this.LogicPropTable["StopGain"].Value;
        double stopgainprice = 0;
        double close = 0;
        double profit = 0;
        if (stopgain != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit > stopgain)
            {

                stopgainprice = close;
            }
        }
        return stopgainprice;
    }
    private double CloseStopProfitLoss(double onhandcost, string positionside)
    {
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopprofitloss = this.LogicPropTable["StopProfitLoss"].Value;
        double prevprofit = Double.Parse(this.GlobalVarTable["PrevProfit"]);
        double stopprofitlossprice = 0;
        double close = 0;
        double profit = 0;
        double profitdiff = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
        if (positionside == "openlong")
            profit = close - onhandcost;
        else if (positionside == "openshort")
            profit = onhandcost - close;
        this.GlobalVarTable["PrevProfit"] = profit.ToString();//Update previous profit

        if ((stopprofitloss != 0) && (profit > 0) && (prevprofit > 0))
        {
            profitdiff = profit - prevprofit;
            if (profitdiff < stopprofitloss)
                stopprofitlossprice = close;
        }
        return stopprofitlossprice;

    }


    public override DataRow OpenLongStatus(Portfolio port)
    {
        double onhandcost = port.CostOnhand(symbol);
        double price = 0;//not hit any condition

        //Start default trade vol = 1
        this.TradeVol = 1;

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        TimeSpan targettimetrade = TimeSpan.FromSeconds((Double)this.LogicPropTable["TargetTimeTrade"].Value);
        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double high = (Double)this.HistoricalTable.Rows[counthistrow - 1]["High"];
        double low = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Low"];
        double vol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Volume"];
        double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];



        double targetgap = this.LogicPropTable["TargetGap"].Value;
        double targetlongvolpercent = this.LogicPropTable["TotalLongVolPercent"].Value;
        double targetshortvolpercent = this.LogicPropTable["TotalShortVolPercent"].Value;
        double ltimesec = this.LogicPropTable["TimeLookBack"].Value;

        double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
        double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
        double tlongtrans = this.LogicPropTable["TotalLongTrans"].Value;
        double tshorttrans = this.LogicPropTable["TotalShortTrans"].Value;
        double tlonggap = this.LogicPropTable["LongGap"].Value;
        double tshortgap = this.LogicPropTable["ShortGap"].Value;
        double tswinggap = this.LogicPropTable["SwingGap"].Value;
        double tdaygap = this.LogicPropTable["DayGap"].Value;

        double lastbiglotslongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
        double lastbiglotsshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];

        /*
        if (time == (TimeSpan.Parse("14:30:00")))
            this.VarTable["OpenAfternoon"] = open.ToString();
        */
        if ((counthistrow > monitor) && (time >= openstarttime) && (time <= openendtime) && (time != (TimeSpan.Parse("14:15:00")) && (time != (TimeSpan.Parse("09:45:00")))))
        {

            found = 0;

            //Check gap high low lookback unitil expect x seconds
            double tmpvol = 0;
            double tmpnewshorthigh = 0;
            double tmpnewlonglow = 0;
            double tmpcount = 0;
            double tmpclose = (Double)this.HistoricalTable.Rows[counthistrow - 2]["Close"];

            double tmphigh = 0;
            double tmplow = 0;
            double tmphl = 0;
            TimeSpan gaptime = TimeSpan.FromSeconds(0);
            double tmpdayhigh = (Double)this.HistoricalTable.Rows[counthistrow - 2]["DayHigh"];
            double tmpdaylow = (Double)this.HistoricalTable.Rows[counthistrow - 2]["DayLow"];


            for (int m = 2; m < monitor; m++)
            {
                double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                double daylowprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                double biglotclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Close"];
                double totallongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalLongVolume"];
                double totalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"];
                double percentlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["%Volume Long"];
                double percentshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["%Volume Short"];

                double biglotgaphl = dayhighprice - daylowprice;
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];
                if ((biglotgaphl > tdaygap) && (dayhighprice - biglotclose < tshortgap))
                {
                    if (totallongvol > tlongvol)
                        found++;
                }

                
                /*if (totalshortvol < tshortvol)
                {
                    if (biglotclose >= tmpclose + tshortgap)
                        found++;
                }*/



            }

            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = close;
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("openlong", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    public override DataRow OpenShortStatus(Portfolio port)
    {
        double onhandcost = port.CostOnhand(symbol);
        double price = 0;//not hit any condition
                         //Start Trade vol = 1
        this.TradeVol = 1;

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        TimeSpan targettimetrade = TimeSpan.FromSeconds((Double)this.LogicPropTable["TargetTimeTrade"].Value);
        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double high = (Double)this.HistoricalTable.Rows[counthistrow - 1]["High"];
        double low = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Low"];
        double vol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Volume"];
        double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];

        double targetgap = this.LogicPropTable["TargetGap"].Value;
        double targetlongvolpercent = this.LogicPropTable["TotalLongVolPercent"].Value;
        double targetshortvolpercent = this.LogicPropTable["TotalShortVolPercent"].Value;
        double ltimesec = this.LogicPropTable["TimeLookBack"].Value;

        double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
        double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
        double tlongtrans = this.LogicPropTable["TotalLongTrans"].Value;
        double tshorttrans = this.LogicPropTable["TotalShortTrans"].Value;
        double tlonggap = this.LogicPropTable["LongGap"].Value;
        double tshortgap = this.LogicPropTable["ShortGap"].Value;
        double tswinggap = this.LogicPropTable["SwingGap"].Value;
        double tdaygap = this.LogicPropTable["DayGap"].Value;

        double lastbiglotslongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
        double lastbiglotsshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];

        if ((counthistrow > monitor) && (time >= openstarttime) && (time <= openendtime) && (time != (TimeSpan.Parse("14:15:00")) && (time != (TimeSpan.Parse("09:45:00")))))
        {
            found = 0;

            //Check gap high low lookback unitil expect x seconds
            double tmpvol = 0;
            double tmpnewshorthigh = 0;
            double tmpnewlonglow = 0;
            double tmpcount = 0;
            double tmpclose = (Double)this.HistoricalTable.Rows[counthistrow - 2]["Close"];


            for (int m = 2; m < monitor; m++)
            {
                double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                double daylowprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                double biglotclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Close"];
                double totallongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalLongVolume"];
                double totalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"];
                double percentlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["%Volume Long"];
                double percentshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["%Volume Short"];

                double biglotgaphl = dayhighprice - daylowprice;
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];


                if ((biglotgaphl > tdaygap) && (biglotclose - daylowprice < tlonggap))
                {
                    if (totalshortvol < tshortvol)
                        found++;
                }
                



            }
            if ((found >= this.LogicPropTable["SignalShort"].Value))
            {
                price = open;
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("openshort", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    public override DataRow CloseLongStatus(Portfolio port)
    {
        double onhandcost = port.AvgOnhandPrice(symbol);
        double price = 0;
        double stoplossprice = this.CloseStopLoss(onhandcost, "openlong");
        double stopgainprice = this.CloseStopGain(onhandcost, "openlong");
        double stopprofitlossprice = this.CloseStopProfitLoss(onhandcost, "openlong");
        if (stoplossprice > 0)
            price = stoplossprice; //Stop loss
        else if (stopgainprice > 0)
            price = stopgainprice;//Stop gain
        else if (stopprofitlossprice > 0)//Stop profit loss
            price = stopprofitlossprice;

        else
        {
            
            //price = this.CloseLongCheckGap();
            //price = this.OpenShortStatus(onhandcost);
            int counthistrow = this.HistoricalTable.Rows.Count;
            double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];
            double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
            double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
            /*
            if (biglotssumshortvol < tshortvol)
                price = close;
            */
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            
            if (time >= endtime)
                price = close;
        }

        if (price > 0)
        {
            //Update tradevol, close all onhandvol
            this.TradeVol = this.CurrentOnhandVol;
            this.ClearVarTable();

            if (stoplossprice > 0)
            {
                GlobalVarTable["ReverseShort"] = "1";
                GlobalVarTable["StopTrade"] = "1";
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("closelong", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    public override DataRow CloseShortStatus(Portfolio port)
    {
        double onhandcost = port.AvgOnhandPrice(symbol);
        double price = 0;
        double stoplossprice = this.CloseStopLoss(onhandcost, "openshort");
        double stopgainprice = this.CloseStopGain(onhandcost, "openshort");
        double stopprofitlossprice = this.CloseStopProfitLoss(onhandcost, "openshort");
        if (stoplossprice > 0)
            price = stoplossprice; //Stop loss
        else if (stopgainprice > 0)
            price = stopgainprice;//Stop gain
        else if (stopprofitlossprice > 0)
            price = stopprofitlossprice;//Stop loss profit
        else
        {
            //price = this.CloseLongCheckGap();
            //price = this.OpenShortStatus(onhandcost);
            
            int counthistrow = this.HistoricalTable.Rows.Count;
            double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];
            double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
            double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
            /*
            if (biglotssumlongvol > tlongvol)
                price = close;
            */
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            
            if (time >= endtime)
                price = close;
        }
        if (price > 0)
        {
            //Update tradevol, close all onhandvol
            this.TradeVol = this.CurrentOnhandVol;
            this.ClearVarTable();
            if (stoplossprice > 0)
            {
                GlobalVarTable["ReverseLong"] = "1";
                GlobalVarTable["StopTrade"] = "1";
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("closeshort", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    private double CloseStopLoss(Portfolio port, string positionside)
    {
        double onhandcost = port.CostOnhand(symbol) / port.VolOnhand(symbol);
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stoploss = this.LogicPropTable["StopLoss"].Value;
        double stoplossprice = 0;
        double close = 0;
        double profit = 0;
        if (stoploss != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
            {
                profit = close - onhandcost;
            }
            else if (positionside == "openshort")
            {
                profit = onhandcost - close;
            }
            if (profit < stoploss)
            {

                stoplossprice = close;


            }
        }
        return stoplossprice;
    }
    private double CloseStopGain(Portfolio port, string positionside)
    {
        double onhandcost = port.CostOnhand(symbol) / port.VolOnhand(symbol);
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopgain = this.LogicPropTable["StopGain"].Value;
        double stopgainprice = 0;
        double close = 0;
        double profit = 0;
        if (stopgain != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit > stopgain)
            {

                stopgainprice = close;
            }
        }
        return stopgainprice;
    }
    private double CloseStopProfitLoss(Portfolio port, string positionside)
    {
        double onhandcost = port.CostOnhand(symbol);
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopprofitloss = this.LogicPropTable["StopProfitLoss"].Value;
        double prevprofit = Double.Parse(this.GlobalVarTable["PrevProfit"]);
        double stopprofitlossprice = 0;
        double close = 0;
        double profit = 0;
        double profitdiff = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
        if (positionside == "openlong")
            profit = close - onhandcost;
        else if (positionside == "openshort")
            profit = onhandcost - close;
        this.GlobalVarTable["PrevProfit"] = profit.ToString();//Update previous profit

        if ((stopprofitloss != 0) && (profit > 0) && (prevprofit > 0))
        {
            profitdiff = profit - prevprofit;
            if (profitdiff < stopprofitloss)
                stopprofitlossprice = close;
        }
        return stopprofitlossprice;

    }

    public override DataRow OnData(string symbol, DataRow resamplingtick, ref Portfolio port)
    {
        double onhandvol = port.VolOnhand(symbol);
        //first init signalTradeNone for reduce CPU time
        if (signalTradeNone == null)
            signalTradeNone = GetSingnal("null", 0, 0);

        DataRow tradesignal = signalTradeNone;//GetSignal("none",0,0);

        DateTime tradedatetime = ((DateTime)resamplingtick[0]).Add((TimeSpan)resamplingtick[1]);
        //DateTime endtradedatetime = ((DateTime)resamplingtick[0]).Add(TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value));

        if (tradedatetime >= NextTradeDateTime)
        {
            if (port.IsLong(symbol))// hold long
            {

                //Check OpenLong
                if (port.VolOnhand(symbol) < (double)LogicPropTable["MaxLongOnhandVol"].Value)
                {
                    tradesignal = this.OpenLongStatus(port);

                }
                if ((double)tradesignal["tradevol"] == 0)//if no openlong check close status
                {
                    tradesignal = this.CloseLongStatus(port);
                }

            }
            else if (port.IsShort(symbol))// hold short
            {

                //Check Openshort
                if (port.VolOnhand(symbol) > (double)LogicPropTable["MaxShortOnhandVol"].Value)
                {
                    tradesignal = this.OpenShortStatus(port);

                }
                if ((double)tradesignal["tradevol"] == 0)//if no openlong check close status
                {
                    tradesignal = this.CloseShortStatus(port);
                }
            }
            else //none hold
            {
                //check openlong first if not found check openshort
                tradesignal = this.OpenLongStatus(port);
                if ((double)tradesignal["tradevol"] == 0)//if no openlong check close status
                {
                    tradesignal = this.OpenShortStatus(port);
                }
            }
            //Check trade 
            if ((double)tradesignal["tradevol"] != 0)
                //NextTradeTime = (TimeSpan)resamplingtick[1] + TimeSpan.FromSeconds((Double)this.LogicPropTable["NextTradeTime"].Value);
                NextTradeDateTime = ((DateTime)resamplingtick[0]).Add((TimeSpan)resamplingtick[1] + TimeSpan.FromSeconds((Double)this.LogicPropTable["NextTradeTime"].Value));
        }
        this.SignalTable.Rows.Add(tradesignal.ItemArray);
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }

    private DataRow GetSingnal(string status, double price, double volume)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
        sigrow["tradevol"] = volume;
        switch (status)
        {
            case "none":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "dup":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openlong":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closelong":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = price;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closeshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = price;
                    break;
                }
            default:
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
        }
        return sigrow;
    }

}



class BigLotsHFTTradeLogicRevised02Reverse : TradeLogic
{
    TimeSpan NextTradeTime;
    public BigLotsHFTTradeLogicRevised02Reverse(string _symbol, DataTable newResamplingTable, int interval)
    {
        //Update historical table
        symbol = _symbol;
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.GlobalVarTable = new Dictionary<String, string>();
        this.InitLogicPropTable(interval);

        //Init Next trade time
        NextTradeTime = TimeSpan.FromSeconds(0);
        NextTradeDateTime = new DateTime();
        //signalTradeNone = GetSingnal("none", 0, 0);

    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume", 0, 200, 1000, 200));//400
        LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", 0, -1000, -200, 200));//400
        LogicPropTable.Add("TotalLongTrans", new Property("TotalLongTrans", 200, 100, 200, 100));
        LogicPropTable.Add("TotalShortTrans", new Property("TotalShortTrans", 200, 100, 200, 100));
        LogicPropTable.Add("SwingGap", new Property("SwingGap", 7, 0.5, 2, 0.5));
        LogicPropTable.Add("TargetGap", new Property("TargetGap", 4, 1, 5, 1));
        LogicPropTable.Add("DayGap", new Property("DayGap", 6, 5, 12, 1));
        LogicPropTable.Add("LongGap", new Property("LongGap", 1, 0, 3, 0.5));
        LogicPropTable.Add("ShortGap", new Property("ShortGap", 1, 0, 3, 0.5));
        double endtimetrade = TimeSpan.Parse("16:30:00").TotalSeconds;
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", -1.6, -8, -2, 1));
        LogicPropTable.Add("StopGain", new Property("StopGain", 15, 2, 5, 0.5));
        LogicPropTable.Add("StopProfitLoss", new Property("StopProfitLoss", 0, -8, -2, 1));



        LogicPropTable.Add("TimeLookBack", new Property("TimeLookBack", 1200, 100, 100, 100));
        double targettimetrade = TimeSpan.Parse("16:00:00").TotalSeconds;
        LogicPropTable.Add("TargetTimeTrade", new Property("TargetTimeTrade", targettimetrade, targettimetrade, targettimetrade, 0));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 2, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 2, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 6, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 6, 1, 30, 1));
        double openstarttimetrade = TimeSpan.Parse("15:00:00").TotalSeconds;//15:40 best
        double openendtimetrade = TimeSpan.Parse("16:00:00").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("OpenSignalGap", new Property("OpenSignalGap", 0, 1, 10, 1));
        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 30, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -30, -500, -40, 50));

        //double openstarttimetrade = TimeSpan.Parse("14:20:00").TotalSeconds;
        //double openendtimetrade = TimeSpan.Parse("16:25:00").TotalSeconds;
        //double endtimetrade = TimeSpan.Parse("16:54:00").TotalSeconds;

        LogicPropTable.Add("TotalLongVolPercent", new Property("TotalLongVolPercent", 35, -8, -2, 1));
        LogicPropTable.Add("TotalShortVolPercent", new Property("TotalShortVolPercent", 35, -8, -2, 1));
        //new trade property


        double nexttradetime = TimeSpan.Parse("00:01:00").TotalSeconds;
        LogicPropTable.Add("NextTradeTime", new Property("NextTradeTime", nexttradetime, nexttradetime, nexttradetime, 0));
        LogicPropTable.Add("MaxLongOnhandVol", new Property("MaxLongOnhandVol", 1, 1, 10, 1));
        LogicPropTable.Add("MaxShortOnhandVol", new Property("MaxShortOnhandVol", -1, 1, 10, 1));
        LogicPropTable.Add("MaxOnhandCost", new Property("MaxOnhandCost", 1000000, 100000, 1000000, 100000));
        this.GlobalVarTable.Clear();
        this.InitVarTable();
    }
    public override void AddFeeder(Feeder _feed)
    {

    }
    public override List<string> GetAdjustedPropertyNamesList()
    {
        List<string> propList = new List<string>();

        propList.Add("TargetGap");
        //propList.Add("DayGap");
        //propList.Add("TotalLongVolume");
        //propList.Add("TotalShortVolume");
        return propList;
    }


    public void InitVarTable()
    {
        this.GlobalVarTable.Add("Lower High-Gap", "0");
        this.GlobalVarTable.Add("BreakHigh", "0");
        this.GlobalVarTable.Add("Higher Low+Gap", "0");
        this.GlobalVarTable.Add("BreakLow", "0");
        this.GlobalVarTable.Add("PrevProfit", "0");
        this.GlobalVarTable.Add("OpenAfternoon", "0");
        this.GlobalVarTable.Add("ReverseLong", "0");
        this.GlobalVarTable.Add("ReverseShort", "0");
        this.GlobalVarTable.Add("StopTrade", "0");
    }
    public void ClearVarTable()
    {
        this.GlobalVarTable["Lower High-Gap"] = "0";
        this.GlobalVarTable["BreakHigh"] = "0";
        this.GlobalVarTable["Higher Low+Gap"] = "0";
        this.GlobalVarTable["BreakLow"] = "0";
        this.GlobalVarTable["OpenAfternoon"] = "0";
        this.GlobalVarTable["ReverseLong"] = "0";
        this.GlobalVarTable["ReverseShort"] = "0";
        this.GlobalVarTable["StopTrade"] = "0";

    }
    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[7];

        col[0] = new DataColumn("datetime", typeof(System.DateTime)); //eg. 9:45 
        col[1] = new DataColumn("status", typeof(System.String));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", typeof(System.Double)); //price
        col[3] = new DataColumn("openshort", typeof(System.Double));//price
        col[4] = new DataColumn("closelong", typeof(System.Double));//price
        col[5] = new DataColumn("closeshort", typeof(System.Double));//price
        col[6] = new DataColumn("tradevol", typeof(System.Double));//price
        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }

    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
        sigrow["tradevol"] = this.TradeVol;
        switch (status)
        {
            case "none":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "dup":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openlong":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closelong":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = price;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closeshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = price;
                    break;
                }
            default:
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
        }
        return sigrow;
    }



    private double CloseStopLoss(double onhandcost, string positionside)
    {
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stoploss = this.LogicPropTable["StopLoss"].Value;
        double stoplossprice = 0;
        double close = 0;
        double profit = 0;
        if (stoploss != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
            {
                profit = close - onhandcost;
            }
            else if (positionside == "openshort")
            {
                profit = onhandcost - close;
            }
            if (profit < stoploss)
            {

                stoplossprice = close;


            }
        }
        return stoplossprice;
    }
    private double CloseStopGain(double onhandcost, string positionside)
    {
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopgain = this.LogicPropTable["StopGain"].Value;
        double stopgainprice = 0;
        double close = 0;
        double profit = 0;
        if (stopgain != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit > stopgain)
            {

                stopgainprice = close;
            }
        }
        return stopgainprice;
    }
    private double CloseStopProfitLoss(double onhandcost, string positionside)
    {
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopprofitloss = this.LogicPropTable["StopProfitLoss"].Value;
        double prevprofit = Double.Parse(this.GlobalVarTable["PrevProfit"]);
        double stopprofitlossprice = 0;
        double close = 0;
        double profit = 0;
        double profitdiff = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
        if (positionside == "openlong")
            profit = close - onhandcost;
        else if (positionside == "openshort")
            profit = onhandcost - close;
        this.GlobalVarTable["PrevProfit"] = profit.ToString();//Update previous profit

        if ((stopprofitloss != 0) && (profit > 0) && (prevprofit > 0))
        {
            profitdiff = profit - prevprofit;
            if (profitdiff < stopprofitloss)
                stopprofitlossprice = close;
        }
        return stopprofitlossprice;

    }


    public override DataRow OpenLongStatus(Portfolio port)
    {
        double onhandcost = port.CostOnhand(symbol);
        double price = 0;//not hit any condition

        //Start default trade vol = 1
        this.TradeVol = 1;

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        TimeSpan targettimetrade = TimeSpan.FromSeconds((Double)this.LogicPropTable["TargetTimeTrade"].Value);
        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double high = (Double)this.HistoricalTable.Rows[counthistrow - 1]["High"];
        double low = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Low"];
        double vol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Volume"];
        double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];



        double targetgap = this.LogicPropTable["TargetGap"].Value;
        double targetlongvolpercent = this.LogicPropTable["TotalLongVolPercent"].Value;
        double targetshortvolpercent = this.LogicPropTable["TotalShortVolPercent"].Value;
        double ltimesec = this.LogicPropTable["TimeLookBack"].Value;

        double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
        double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
        double tlongtrans = this.LogicPropTable["TotalLongTrans"].Value;
        double tshorttrans = this.LogicPropTable["TotalShortTrans"].Value;
        double tlonggap = this.LogicPropTable["LongGap"].Value;
        double tshortgap = this.LogicPropTable["ShortGap"].Value;
        double tswinggap = this.LogicPropTable["SwingGap"].Value;
        double tdaygap = this.LogicPropTable["DayGap"].Value;

        double lastbiglotslongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
        double lastbiglotsshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];

        /*
        if (time == (TimeSpan.Parse("14:30:00")))
            this.VarTable["OpenAfternoon"] = open.ToString();
        */
        if ((counthistrow > monitor) && (time >= openstarttime) && (time <= openendtime) && (time != (TimeSpan.Parse("14:15:00")) && (time != (TimeSpan.Parse("09:45:00")))))
        {

            found = 0;

            //Check gap high low lookback unitil expect x seconds
            double tmpvol = 0;
            double tmpnewshorthigh = 0;
            double tmpnewlonglow = 0;
            double tmpcount = 0;
            double tmpclose = (Double)this.HistoricalTable.Rows[counthistrow - 2]["Close"];

            double tmphigh = 0;
            double tmplow = 0;
            double tmphl = 0;
            TimeSpan gaptime = TimeSpan.FromSeconds(0);
            double tmpdayhigh = (Double)this.HistoricalTable.Rows[counthistrow - 2]["DayHigh"];
            double tmpdaylow = (Double)this.HistoricalTable.Rows[counthistrow - 2]["DayLow"];


            for (int m = 2; m < monitor; m++)
            {
                double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                double daylowprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                double biglotclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Close"];
                double totallongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalLongVolume"];
                double totalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"];
                double percentlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["%Volume Long"];
                double percentshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["%Volume Short"];

                double biglotgaphl = dayhighprice - daylowprice;
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];
                
                /*
                if ((biglotgaphl > tdaygap) && (biglotclose - daylowprice < tlonggap))
                {
                    if ((totalshortvol < tshortvol))
                        found++;
                }*/
                if ((biglotgaphl <= tdaygap) && (dayhighprice - biglotclose < tshortgap))
                {
                    if (totallongvol > tlongvol)
                        found++;
                }



            }

            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = close;
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("openlong", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    public override DataRow OpenShortStatus(Portfolio port)
    {
        double onhandcost = port.CostOnhand(symbol);
        double price = 0;//not hit any condition
                         //Start Trade vol = 1
        this.TradeVol = 1;

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        TimeSpan targettimetrade = TimeSpan.FromSeconds((Double)this.LogicPropTable["TargetTimeTrade"].Value);
        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double high = (Double)this.HistoricalTable.Rows[counthistrow - 1]["High"];
        double low = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Low"];
        double vol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Volume"];
        double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];

        double targetgap = this.LogicPropTable["TargetGap"].Value;
        double targetlongvolpercent = this.LogicPropTable["TotalLongVolPercent"].Value;
        double targetshortvolpercent = this.LogicPropTable["TotalShortVolPercent"].Value;
        double ltimesec = this.LogicPropTable["TimeLookBack"].Value;

        double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
        double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
        double tlongtrans = this.LogicPropTable["TotalLongTrans"].Value;
        double tshorttrans = this.LogicPropTable["TotalShortTrans"].Value;
        double tlonggap = this.LogicPropTable["LongGap"].Value;
        double tshortgap = this.LogicPropTable["ShortGap"].Value;
        double tswinggap = this.LogicPropTable["SwingGap"].Value;
        double tdaygap = this.LogicPropTable["DayGap"].Value;

        double lastbiglotslongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
        double lastbiglotsshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];

        if ((counthistrow > monitor) && (time >= openstarttime) && (time <= openendtime) && (time != (TimeSpan.Parse("14:15:00")) && (time != (TimeSpan.Parse("09:45:00")))))
        {
            found = 0;

            //Check gap high low lookback unitil expect x seconds
            double tmpvol = 0;
            double tmpnewshorthigh = 0;
            double tmpnewlonglow = 0;
            double tmpcount = 0;
            double tmpclose = (Double)this.HistoricalTable.Rows[counthistrow - 2]["Close"];


            for (int m = 2; m < monitor; m++)
            {
                double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                double daylowprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                double biglotclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Close"];
                double totallongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalLongVolume"];
                double totalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"];
                double percentlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["%Volume Long"];
                double percentshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["%Volume Short"];

                double biglotgaphl = dayhighprice - daylowprice;
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];


                
                /*if ((biglotgaphl > tdaygap) && (dayhighprice - biglotclose < tshortgap))
                {
                    if ((totallongvol > tlongvol))
                        found++;
                }*/
                
                if ((biglotgaphl <= tdaygap) && (biglotclose - daylowprice < tlonggap))
                {
                    if (totalshortvol < tshortvol)
                        found++;
                }



            }
            if ((found >= this.LogicPropTable["SignalShort"].Value))
            {
                price = open;
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("openshort", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    public override DataRow CloseLongStatus(Portfolio port)
    {
        double onhandcost = port.AvgOnhandPrice(symbol);
        double price = 0;
        double stoplossprice = this.CloseStopLoss(onhandcost, "openlong");
        double stopgainprice = this.CloseStopGain(onhandcost, "openlong");
        double stopprofitlossprice = this.CloseStopProfitLoss(onhandcost, "openlong");
        if (stoplossprice > 0)
            price = stoplossprice; //Stop loss
        else if (stopgainprice > 0)
            price = stopgainprice;//Stop gain
        else if (stopprofitlossprice > 0)//Stop profit loss
            price = stopprofitlossprice;

        else
        {

            //price = this.CloseLongCheckGap();
            //price = this.OpenShortStatus(onhandcost);
            int counthistrow = this.HistoricalTable.Rows.Count;
            double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];
            double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
            double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
            /*
            if (biglotssumshortvol < tshortvol)
                price = close;
            */
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);

            if (time >= endtime)
                price = close;
        }

        if (price > 0)
        {
            //Update tradevol, close all onhandvol
            this.TradeVol = this.CurrentOnhandVol;
            this.ClearVarTable();

            if (stoplossprice > 0)
            {
                GlobalVarTable["ReverseShort"] = "1";
                GlobalVarTable["StopTrade"] = "1";
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("closelong", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    public override DataRow CloseShortStatus(Portfolio port)
    {
        double onhandcost = port.AvgOnhandPrice(symbol);
        double price = 0;
        double stoplossprice = this.CloseStopLoss(onhandcost, "openshort");
        double stopgainprice = this.CloseStopGain(onhandcost, "openshort");
        double stopprofitlossprice = this.CloseStopProfitLoss(onhandcost, "openshort");
        if (stoplossprice > 0)
            price = stoplossprice; //Stop loss
        else if (stopgainprice > 0)
            price = stopgainprice;//Stop gain
        else if (stopprofitlossprice > 0)
            price = stopprofitlossprice;//Stop loss profit
        else
        {
            //price = this.CloseLongCheckGap();
            //price = this.OpenShortStatus(onhandcost);

            int counthistrow = this.HistoricalTable.Rows.Count;
            double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];
            double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
            double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
            /*
            if (biglotssumlongvol > tlongvol)
                price = close;
            */
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);

            if (time >= endtime)
                price = close;
        }
        if (price > 0)
        {
            //Update tradevol, close all onhandvol
            this.TradeVol = this.CurrentOnhandVol;
            this.ClearVarTable();
            if (stoplossprice > 0)
            {
                GlobalVarTable["ReverseLong"] = "1";
                GlobalVarTable["StopTrade"] = "1";
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("closeshort", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    private double CloseStopLoss(Portfolio port, string positionside)
    {
        double onhandcost = port.CostOnhand(symbol) / port.VolOnhand(symbol);
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stoploss = this.LogicPropTable["StopLoss"].Value;
        double stoplossprice = 0;
        double close = 0;
        double profit = 0;
        if (stoploss != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
            {
                profit = close - onhandcost;
            }
            else if (positionside == "openshort")
            {
                profit = onhandcost - close;
            }
            if (profit < stoploss)
            {

                stoplossprice = close;


            }
        }
        return stoplossprice;
    }
    private double CloseStopGain(Portfolio port, string positionside)
    {
        double onhandcost = port.CostOnhand(symbol) / port.VolOnhand(symbol);
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopgain = this.LogicPropTable["StopGain"].Value;
        double stopgainprice = 0;
        double close = 0;
        double profit = 0;
        if (stopgain != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit > stopgain)
            {

                stopgainprice = close;
            }
        }
        return stopgainprice;
    }
    private double CloseStopProfitLoss(Portfolio port, string positionside)
    {
        double onhandcost = port.CostOnhand(symbol);
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopprofitloss = this.LogicPropTable["StopProfitLoss"].Value;
        double prevprofit = Double.Parse(this.GlobalVarTable["PrevProfit"]);
        double stopprofitlossprice = 0;
        double close = 0;
        double profit = 0;
        double profitdiff = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
        if (positionside == "openlong")
            profit = close - onhandcost;
        else if (positionside == "openshort")
            profit = onhandcost - close;
        this.GlobalVarTable["PrevProfit"] = profit.ToString();//Update previous profit

        if ((stopprofitloss != 0) && (profit > 0) && (prevprofit > 0))
        {
            profitdiff = profit - prevprofit;
            if (profitdiff < stopprofitloss)
                stopprofitlossprice = close;
        }
        return stopprofitlossprice;

    }

    public override DataRow OnData(string symbol, DataRow resamplingtick, ref Portfolio port)
    {
        double onhandvol = port.VolOnhand(symbol);
        //first init signalTradeNone for reduce CPU time
        if (signalTradeNone == null)
            signalTradeNone = GetSingnal("null", 0, 0);

        DataRow tradesignal = signalTradeNone;//GetSignal("none",0,0);

        DateTime tradedatetime = ((DateTime)resamplingtick[0]).Add((TimeSpan)resamplingtick[1]);
        //DateTime endtradedatetime = ((DateTime)resamplingtick[0]).Add(TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value));

        if (tradedatetime >= NextTradeDateTime)
        {
            if (port.IsLong(symbol))// hold long
            {

                //Check OpenLong
                if (port.VolOnhand(symbol) < (double)LogicPropTable["MaxLongOnhandVol"].Value)
                {
                    tradesignal = this.OpenLongStatus(port);

                }
                if ((double)tradesignal["tradevol"] == 0)//if no openlong check close status
                {
                    tradesignal = this.CloseLongStatus(port);
                }

            }
            else if (port.IsShort(symbol))// hold short
            {

                //Check Openshort
                if (port.VolOnhand(symbol) > (double)LogicPropTable["MaxShortOnhandVol"].Value)
                {
                    tradesignal = this.OpenShortStatus(port);

                }
                if ((double)tradesignal["tradevol"] == 0)//if no openlong check close status
                {
                    tradesignal = this.CloseShortStatus(port);
                }
            }
            else //none hold
            {
                //check openlong first if not found check openshort
                tradesignal = this.OpenLongStatus(port);
                if ((double)tradesignal["tradevol"] == 0)//if no openlong check close status
                {
                    tradesignal = this.OpenShortStatus(port);
                }
            }
            //Check trade 
            if ((double)tradesignal["tradevol"] != 0)
                //NextTradeTime = (TimeSpan)resamplingtick[1] + TimeSpan.FromSeconds((Double)this.LogicPropTable["NextTradeTime"].Value);
                NextTradeDateTime = ((DateTime)resamplingtick[0]).Add((TimeSpan)resamplingtick[1] + TimeSpan.FromSeconds((Double)this.LogicPropTable["NextTradeTime"].Value));
        }
        this.SignalTable.Rows.Add(tradesignal.ItemArray);
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }

    private DataRow GetSingnal(string status, double price, double volume)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
        sigrow["tradevol"] = volume;
        switch (status)
        {
            case "none":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "dup":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openlong":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closelong":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = price;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closeshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = price;
                    break;
                }
            default:
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
        }
        return sigrow;
    }

}




class BigLotsHFTTradeLogicRevised03 : TradeLogic
{
    TimeSpan NextTradeTime;
    public BigLotsHFTTradeLogicRevised03(string _symbol, DataTable newResamplingTable, int interval)
    {
        //Update historical table
        symbol = _symbol;
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.GlobalVarTable = new Dictionary<String, string>();
        this.InitLogicPropTable(interval);

        //Init Next trade time
        NextTradeTime = TimeSpan.FromSeconds(0);
        NextTradeDateTime = new DateTime();
        //signalTradeNone = GetSingnal("none", 0, 0);

    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume", 0, 200, 1000, 200));//400
        LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", 0, -1000, -200, 200));//400
        LogicPropTable.Add("TotalLongTrans", new Property("TotalLongTrans", 200, 100, 200, 100));
        LogicPropTable.Add("TotalShortTrans", new Property("TotalShortTrans", 200, 100, 200, 100));
        LogicPropTable.Add("SwingGap", new Property("SwingGap", 7, 0.5, 2, 0.5));
        LogicPropTable.Add("TargetGap", new Property("TargetGap", 4, 1, 5, 1));
        LogicPropTable.Add("DayGap", new Property("DayGap", 6, 5, 12, 1));
        LogicPropTable.Add("LongGap", new Property("LongGap", 2, 0, 3, 0.5));
        LogicPropTable.Add("ShortGap", new Property("ShortGap", 2, 0, 3, 0.5));
        LogicPropTable.Add("PercentGap", new Property("PercentGap", 37, 0, 100, 1));
        double endtimetrade = TimeSpan.Parse("11:00:00").TotalSeconds;
        double openstarttimetrade = TimeSpan.Parse("10:05:00").TotalSeconds;//15:40 best
        double openendtimetrade = TimeSpan.Parse("10:40:00").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", -1.2, -8, -2, 1));
        LogicPropTable.Add("StopGain", new Property("StopGain", 4.5, 2, 5, 0.5));
        LogicPropTable.Add("StopProfitLoss", new Property("StopProfitLoss", -2.5, -8, -2, 1));
        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 1, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -1, -500, -40, 50));


        LogicPropTable.Add("TimeLookBack", new Property("TimeLookBack", 1200, 100, 100, 100));
        double targettimetrade = TimeSpan.Parse("16:00:00").TotalSeconds;
        LogicPropTable.Add("TargetTimeTrade", new Property("TargetTimeTrade", targettimetrade, targettimetrade, targettimetrade, 0));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 1, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 1, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 3, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 3, 1, 30, 1));
        
        
        LogicPropTable.Add("OpenSignalGap", new Property("OpenSignalGap", 0, 1, 10, 1));
        

        //double openstarttimetrade = TimeSpan.Parse("14:20:00").TotalSeconds;
        //double openendtimetrade = TimeSpan.Parse("16:25:00").TotalSeconds;
        //double endtimetrade = TimeSpan.Parse("16:54:00").TotalSeconds;

        LogicPropTable.Add("TotalLongVolPercent", new Property("TotalLongVolPercent", 35, -8, -2, 1));
        LogicPropTable.Add("TotalShortVolPercent", new Property("TotalShortVolPercent", 35, -8, -2, 1));
        //new trade property


        double nexttradetime = TimeSpan.Parse("00:01:00").TotalSeconds;
        LogicPropTable.Add("NextTradeTime", new Property("NextTradeTime", nexttradetime, nexttradetime, nexttradetime, 0));
        LogicPropTable.Add("MaxLongOnhandVol", new Property("MaxLongOnhandVol", 1, 1, 10, 1));
        LogicPropTable.Add("MaxShortOnhandVol", new Property("MaxShortOnhandVol", -1, 1, 10, 1));
        LogicPropTable.Add("MaxOnhandCost", new Property("MaxOnhandCost", 1000000, 100000, 1000000, 100000));
        this.GlobalVarTable.Clear();
        this.InitVarTable();
    }
    public override void AddFeeder(Feeder _feed)
    {

    }
    public override List<string> GetAdjustedPropertyNamesList()
    {
        List<string> propList = new List<string>();

        propList.Add("TargetGap");
        //propList.Add("DayGap");
        //propList.Add("TotalLongVolume");
        //propList.Add("TotalShortVolume");
        return propList;
    }


    public void InitVarTable()
    {
        this.GlobalVarTable.Add("Lower High-Gap", "0");
        this.GlobalVarTable.Add("BreakHigh", "0");
        this.GlobalVarTable.Add("Higher Low+Gap", "0");
        this.GlobalVarTable.Add("BreakLow", "0");
        this.GlobalVarTable.Add("PrevProfit", "0");
        this.GlobalVarTable.Add("OpenAfternoon", "0");
        this.GlobalVarTable.Add("ReverseLong", "0");
        this.GlobalVarTable.Add("ReverseShort", "0");
        this.GlobalVarTable.Add("StopTrade", "0");
    }
    public void ClearVarTable()
    {
        this.GlobalVarTable["Lower High-Gap"] = "0";
        this.GlobalVarTable["BreakHigh"] = "0";
        this.GlobalVarTable["Higher Low+Gap"] = "0";
        this.GlobalVarTable["BreakLow"] = "0";
        this.GlobalVarTable["OpenAfternoon"] = "0";
        this.GlobalVarTable["ReverseLong"] = "0";
        this.GlobalVarTable["ReverseShort"] = "0";
        this.GlobalVarTable["StopTrade"] = "0";

    }
    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[7];

        col[0] = new DataColumn("datetime", typeof(System.DateTime)); //eg. 9:45 
        col[1] = new DataColumn("status", typeof(System.String));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", typeof(System.Double)); //price
        col[3] = new DataColumn("openshort", typeof(System.Double));//price
        col[4] = new DataColumn("closelong", typeof(System.Double));//price
        col[5] = new DataColumn("closeshort", typeof(System.Double));//price
        col[6] = new DataColumn("tradevol", typeof(System.Double));//price
        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }

    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
        sigrow["tradevol"] = this.TradeVol;
        switch (status)
        {
            case "none":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "dup":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openlong":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closelong":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = price;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closeshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = price;
                    break;
                }
            default:
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
        }
        return sigrow;
    }



    private double CloseStopLoss(double onhandcost, string positionside)
    {
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stoploss = this.LogicPropTable["StopLoss"].Value;
        double stoplossprice = 0;
        double close = 0;
        double profit = 0;
        if (stoploss != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
            {
                profit = close - onhandcost;
            }
            else if (positionside == "openshort")
            {
                profit = onhandcost - close;
            }
            if (profit < stoploss)
            {

                stoplossprice = close;


            }
        }
        return stoplossprice;
    }
    private double CloseStopGain(double onhandcost, string positionside)
    {
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopgain = this.LogicPropTable["StopGain"].Value;
        double stopgainprice = 0;
        double close = 0;
        double profit = 0;
        if (stopgain != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit > stopgain)
            {

                stopgainprice = close;
            }
        }
        return stopgainprice;
    }
    private double CloseStopProfitLoss(double onhandcost, string positionside)
    {
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopprofitloss = this.LogicPropTable["StopProfitLoss"].Value;
        double prevprofit = Double.Parse(this.GlobalVarTable["PrevProfit"]);
        double stopprofitlossprice = 0;
        double close = 0;
        double profit = 0;
        double profitdiff = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
        if (positionside == "openlong")
            profit = close - onhandcost;
        else if (positionside == "openshort")
            profit = onhandcost - close;
        if (prevprofit < profit)
            this.GlobalVarTable["PrevProfit"] = profit.ToString();//Update previous profit

        if ((stopprofitloss != 0) && (profit > 0) && (prevprofit > 0))
        {
            profitdiff = profit - prevprofit;
            if (profitdiff < stopprofitloss)
                stopprofitlossprice = close;
        }
        return stopprofitlossprice;

    }


    public override DataRow OpenLongStatus(Portfolio port)
    {
        double onhandcost = port.CostOnhand(symbol);
        double price = 0;//not hit any condition

        //Start default trade vol = 1
        this.TradeVol = 1;

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        TimeSpan targettimetrade = TimeSpan.FromSeconds((Double)this.LogicPropTable["TargetTimeTrade"].Value);
        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double high = (Double)this.HistoricalTable.Rows[counthistrow - 1]["High"];
        double low = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Low"];
        double vol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Volume"];
        double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];



        double targetgap = this.LogicPropTable["TargetGap"].Value;
        double targetlongvolpercent = this.LogicPropTable["TotalLongVolPercent"].Value;
        double targetshortvolpercent = this.LogicPropTable["TotalShortVolPercent"].Value;
        double ltimesec = this.LogicPropTable["TimeLookBack"].Value;

        double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
        double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
        double tlongtrans = this.LogicPropTable["TotalLongTrans"].Value;
        double tshorttrans = this.LogicPropTable["TotalShortTrans"].Value;
        double tlonggap = this.LogicPropTable["LongGap"].Value;
        double tshortgap = this.LogicPropTable["ShortGap"].Value;
        double tswinggap = this.LogicPropTable["SwingGap"].Value;
        double tdaygap = this.LogicPropTable["DayGap"].Value;
        double tpercentgap = this.LogicPropTable["PercentGap"].Value;

        double lastbiglotslongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
        double lastbiglotsshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];

        /*
        if (time == (TimeSpan.Parse("14:30:00")))
            this.VarTable["OpenAfternoon"] = open.ToString();
        */
        if ((port.VolOnhand(symbol) < 1)&&(counthistrow > monitor) && (time >= openstarttime) && (time <= openendtime) && (time != (TimeSpan.Parse("14:15:00")) && (time != (TimeSpan.Parse("09:45:00")))))
        {

            found = 0;

            //Check gap high low lookback unitil expect x seconds
            double tmpvol = 0;
            double tmpnewshorthigh = 0;
            double tmpnewlonglow = 0;
            double tmpcount = 0;
            double tmpclose = (Double)this.HistoricalTable.Rows[counthistrow - 2]["Close"];

            double tmphigh = 0;
            double tmplow = 0;
            double tmphl = 0;
            TimeSpan gaptime = TimeSpan.FromSeconds(0);
            double tmpdayhigh = (Double)this.HistoricalTable.Rows[counthistrow - 2]["DayHigh"];
            double tmpdaylow = (Double)this.HistoricalTable.Rows[counthistrow - 2]["DayLow"];


            for (int m = 2; m < monitor; m++)
            {
                double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                double daylowprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                double biglotclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Close"];
                double totallongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalLongVolume"];
                double totalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"];
                double percentlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVolume%"];
                double percentshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVolume%"];

                double biglotgaphl = dayhighprice - daylowprice;
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];

                /*
                if ((biglotgaphl > tdaygap) && (biglotclose == daylowprice ))
                {
                    //if ((close-daylowprice > tlonggap))
                    if((close+tlonggap >= dayhighprice))
                        found++;
                }
                */
                if (percentlongvol <tpercentgap)
                {
                
                        found++;
                }



            }

            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = close;
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("openlong", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    public override DataRow OpenShortStatus(Portfolio port)
    {
        double onhandcost = port.CostOnhand(symbol);
        double price = 0;//not hit any condition
                         //Start Trade vol = 1
        this.TradeVol = 1;

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        TimeSpan targettimetrade = TimeSpan.FromSeconds((Double)this.LogicPropTable["TargetTimeTrade"].Value);
        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double high = (Double)this.HistoricalTable.Rows[counthistrow - 1]["High"];
        double low = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Low"];
        double vol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Volume"];
        double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];

        double targetgap = this.LogicPropTable["TargetGap"].Value;
        double targetlongvolpercent = this.LogicPropTable["TotalLongVolPercent"].Value;
        double targetshortvolpercent = this.LogicPropTable["TotalShortVolPercent"].Value;
        double ltimesec = this.LogicPropTable["TimeLookBack"].Value;

        double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
        double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
        double tlongtrans = this.LogicPropTable["TotalLongTrans"].Value;
        double tshorttrans = this.LogicPropTable["TotalShortTrans"].Value;
        double tlonggap = this.LogicPropTable["LongGap"].Value;
        double tshortgap = this.LogicPropTable["ShortGap"].Value;
        double tswinggap = this.LogicPropTable["SwingGap"].Value;
        double tdaygap = this.LogicPropTable["DayGap"].Value;
        double tpercentgap = this.LogicPropTable["PercentGap"].Value;

        double lastbiglotslongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
        double lastbiglotsshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];

        if ((port.VolOnhand(symbol) > -1) && (counthistrow > monitor) && (time >= openstarttime) && (time <= openendtime) && (time != (TimeSpan.Parse("14:15:00")) && (time != (TimeSpan.Parse("09:45:00")))))
        {
            found = 0;

            //Check gap high low lookback unitil expect x seconds
            double tmpvol = 0;
            double tmpnewshorthigh = 0;
            double tmpnewlonglow = 0;
            double tmpcount = 0;
            double tmpclose = (Double)this.HistoricalTable.Rows[counthistrow - 2]["Close"];


            for (int m = 2; m < monitor; m++)
            {
                double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                double daylowprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                double biglotclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Close"];
                double totallongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalLongVolume"];
                double totalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"];
                double percentlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVolume%"];
                double percentshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVolume%"];

                double biglotgaphl = dayhighprice - daylowprice;
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];


                /*
                if ((biglotgaphl > tdaygap) && (biglotclose == dayhighprice))
                {
                    //if ((dayhighprice-close > tlonggap))
                    if ((close-tshortgap < daylowprice))
                        found++;
                }
                */
                if(percentshortvol < tpercentgap)
                {
                    found++;
                }



            }
            if ((found >= this.LogicPropTable["SignalShort"].Value))
            {
                price = open;
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("openshort", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    public override DataRow CloseLongStatus(Portfolio port)
    {
        double onhandcost = port.AvgOnhandPrice(symbol);
        double price = 0;
        double stoplossprice = this.CloseStopLoss(onhandcost, "openlong");
        double stopgainprice = this.CloseStopGain(onhandcost, "openlong");
        double stopprofitlossprice = this.CloseStopProfitLoss(onhandcost, "openlong");
        if (stoplossprice > 0)
            price = stoplossprice; //Stop loss
        else if (stopgainprice > 0)
            price = stopgainprice;//Stop gain
        else if (stopprofitlossprice > 0)//Stop profit loss
            price = stopprofitlossprice;

        else
        {

            //price = this.CloseLongCheckGap();
            //price = this.OpenShortStatus(onhandcost);
            int counthistrow = this.HistoricalTable.Rows.Count;
            double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];
            double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
            double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
            /*
            if (biglotssumshortvol < tshortvol)
                price = close;
            */
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);

            if (time >= endtime)
                price = close;
        }

        if (price > 0)
        {
            //Update tradevol, close all onhandvol
            this.TradeVol = this.CurrentOnhandVol;
            this.ClearVarTable();

            if (stoplossprice > 0)
            {
                GlobalVarTable["ReverseShort"] = "1";
                GlobalVarTable["StopTrade"] = "1";
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("closelong", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    public override DataRow CloseShortStatus(Portfolio port)
    {
        double onhandcost = port.AvgOnhandPrice(symbol);
        double price = 0;
        double stoplossprice = this.CloseStopLoss(onhandcost, "openshort");
        double stopgainprice = this.CloseStopGain(onhandcost, "openshort");
        double stopprofitlossprice = this.CloseStopProfitLoss(onhandcost, "openshort");
        if (stoplossprice > 0)
            price = stoplossprice; //Stop loss
        else if (stopgainprice > 0)
            price = stopgainprice;//Stop gain
        else if (stopprofitlossprice > 0)
            price = stopprofitlossprice;//Stop loss profit
        else
        {
            //price = this.CloseLongCheckGap();
            //price = this.OpenShortStatus(onhandcost);

            int counthistrow = this.HistoricalTable.Rows.Count;
            double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];
            double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
            double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
            /*
            if (biglotssumlongvol > tlongvol)
                price = close;
            */
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);

            if (time >= endtime)
                price = close;
        }
        if (price > 0)
        {
            //Update tradevol, close all onhandvol
            this.TradeVol = this.CurrentOnhandVol;
            this.ClearVarTable();
            if (stoplossprice > 0)
            {
                GlobalVarTable["ReverseLong"] = "1";
                GlobalVarTable["StopTrade"] = "1";
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("closeshort", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    private double CloseStopLoss(Portfolio port, string positionside)
    {
        double onhandcost = port.CostOnhand(symbol) / port.VolOnhand(symbol);
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stoploss = this.LogicPropTable["StopLoss"].Value;
        double stoplossprice = 0;
        double close = 0;
        double profit = 0;
        if (stoploss != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
            {
                profit = close - onhandcost;
            }
            else if (positionside == "openshort")
            {
                profit = onhandcost - close;
            }
            if (profit < stoploss)
            {

                stoplossprice = close;


            }
        }
        return stoplossprice;
    }
    private double CloseStopGain(Portfolio port, string positionside)
    {
        double onhandcost = port.CostOnhand(symbol) / port.VolOnhand(symbol);
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopgain = this.LogicPropTable["StopGain"].Value;
        double stopgainprice = 0;
        double close = 0;
        double profit = 0;
        if (stopgain != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit > stopgain)
            {

                stopgainprice = close;
            }
        }
        return stopgainprice;
    }
    private double CloseStopProfitLoss(Portfolio port, string positionside)
    {
        double onhandcost = port.CostOnhand(symbol);
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopprofitloss = this.LogicPropTable["StopProfitLoss"].Value;
        double prevprofit = Double.Parse(this.GlobalVarTable["PrevProfit"]);
        double stopprofitlossprice = 0;
        double close = 0;
        double profit = 0;
        double profitdiff = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
        if (positionside == "openlong")
            profit = close - onhandcost;
        else if (positionside == "openshort")
            profit = onhandcost - close;
        if(prevprofit<profit)
            this.GlobalVarTable["PrevProfit"] = profit.ToString();//Update previous profit

        if ((stopprofitloss != 0) && (profit > 0) && (prevprofit > 0))
        {
            profitdiff = profit - prevprofit;
            if (profitdiff < stopprofitloss)
                stopprofitlossprice = close;
        }
        return stopprofitlossprice;

    }

    public override DataRow OnData(string symbol, DataRow resamplingtick, ref Portfolio port)
    {
        double onhandvol = port.VolOnhand(symbol);
        //first init signalTradeNone for reduce CPU time
        if (signalTradeNone == null)
            signalTradeNone = GetSingnal("null", 0, 0);

        DataRow tradesignal = signalTradeNone;//GetSignal("none",0,0);

        DateTime tradedatetime = ((DateTime)resamplingtick[0]).Add((TimeSpan)resamplingtick[1]);
        //DateTime endtradedatetime = ((DateTime)resamplingtick[0]).Add(TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value));

        if (tradedatetime >= NextTradeDateTime)
        {
            if (port.IsLong(symbol))// hold long
            {

                //Check OpenLong
                if (port.VolOnhand(symbol) < (double)LogicPropTable["MaxLongOnhandVol"].Value)
                {
                    tradesignal = this.OpenLongStatus(port);

                }
                if ((double)tradesignal["tradevol"] == 0)//if no openlong check close status
                {
                    tradesignal = this.CloseLongStatus(port);
                }

            }
            else if (port.IsShort(symbol))// hold short
            {

                //Check Openshort
                if (port.VolOnhand(symbol) > (double)LogicPropTable["MaxShortOnhandVol"].Value)
                {
                    tradesignal = this.OpenShortStatus(port);

                }
                if ((double)tradesignal["tradevol"] == 0)//if no openlong check close status
                {
                    tradesignal = this.CloseShortStatus(port);
                }
            }
            else //none hold
            {
                //check openlong first if not found check openshort
                tradesignal = this.OpenLongStatus(port);
                if ((double)tradesignal["tradevol"] == 0)//if no openlong check close status
                {
                    tradesignal = this.OpenShortStatus(port);
                }
            }
            //Check trade 
            if ((double)tradesignal["tradevol"] != 0)
                //NextTradeTime = (TimeSpan)resamplingtick[1] + TimeSpan.FromSeconds((Double)this.LogicPropTable["NextTradeTime"].Value);
                NextTradeDateTime = ((DateTime)resamplingtick[0]).Add((TimeSpan)resamplingtick[1] + TimeSpan.FromSeconds((Double)this.LogicPropTable["NextTradeTime"].Value));
        }
        this.SignalTable.Rows.Add(tradesignal.ItemArray);
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }

    private DataRow GetSingnal(string status, double price, double volume)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
        sigrow["tradevol"] = volume;
        switch (status)
        {
            case "none":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "dup":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openlong":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closelong":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = price;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closeshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = price;
                    break;
                }
            default:
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
        }
        return sigrow;
    }

}



class AfternoonPercentTradeLogic : TradeLogic
{
    TimeSpan NextTradeTime;
    public AfternoonPercentTradeLogic(string _symbol, DataTable newResamplingTable, int interval)
    {
        //Update historical table
        symbol = _symbol;
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.GlobalVarTable = new Dictionary<String, string>();
        this.InitLogicPropTable(interval);

        //Init Next trade time
        NextTradeTime = TimeSpan.FromSeconds(0);
        NextTradeDateTime = new DateTime();
        //signalTradeNone = GetSingnal("none", 0, 0);

    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume", 0, 200, 1000, 200));//400
        LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", 0, -1000, -200, 200));//400
        LogicPropTable.Add("TotalLongTrans", new Property("TotalLongTrans", 200, 100, 200, 100));
        LogicPropTable.Add("TotalShortTrans", new Property("TotalShortTrans", 200, 100, 200, 100));
        LogicPropTable.Add("SwingGap", new Property("SwingGap", 7, 0.5, 2, 0.5));
        LogicPropTable.Add("TargetGap", new Property("TargetGap", 4, 1, 5, 1));
        LogicPropTable.Add("DayGap", new Property("DayGap", 6, 5, 12, 1));
        LogicPropTable.Add("LongGap", new Property("LongGap", 2, 0, 3, 0.5));
        LogicPropTable.Add("ShortGap", new Property("ShortGap", 2, 0, 3, 0.5));
        LogicPropTable.Add("PercentGap", new Property("PercentGap", 59, 0, 100, 1));
        double endtimetrade = TimeSpan.Parse("16:40:00").TotalSeconds;
        double openstarttimetrade = TimeSpan.Parse("10:40:00").TotalSeconds;//15:40 best
        double openendtimetrade = TimeSpan.Parse("15:30:00").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", -5, -8, -2, 1));
        LogicPropTable.Add("StopGain", new Property("StopGain", 0, 2, 5, 0.5));
        LogicPropTable.Add("StopProfitLoss", new Property("StopProfitLoss", 0, -8, -2, 1));
        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 1, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -1, -500, -40, 50));
        LogicPropTable.Add("MaxLongOnhandVol", new Property("MaxLongOnhandVol", 1, 1, 10, 1));
        LogicPropTable.Add("MaxShortOnhandVol", new Property("MaxShortOnhandVol", -1, 1, 10, 1));
        LogicPropTable.Add("MaxOnhandCost", new Property("MaxOnhandCost", 1000000, 100000, 1000000, 100000));


        LogicPropTable.Add("TimeLookBack", new Property("TimeLookBack", 1200, 100, 100, 100));
        double targettimetrade = TimeSpan.Parse("16:00:00").TotalSeconds;
        LogicPropTable.Add("TargetTimeTrade", new Property("TargetTimeTrade", targettimetrade, targettimetrade, targettimetrade, 0));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 1, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 1, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 3, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 3, 1, 30, 1));

        
        LogicPropTable.Add("OpenSignalGap", new Property("OpenSignalGap", 0, 1, 10, 1));


        //double openstarttimetrade = TimeSpan.Parse("14:20:00").TotalSeconds;
        //double openendtimetrade = TimeSpan.Parse("16:25:00").TotalSeconds;
        //double endtimetrade = TimeSpan.Parse("16:54:00").TotalSeconds;

        LogicPropTable.Add("TotalLongVolPercent", new Property("TotalLongVolPercent", 35, -8, -2, 1));
        LogicPropTable.Add("TotalShortVolPercent", new Property("TotalShortVolPercent", 35, -8, -2, 1));
        //new trade property


        double nexttradetime = TimeSpan.Parse("00:01:00").TotalSeconds;
        LogicPropTable.Add("NextTradeTime", new Property("NextTradeTime", nexttradetime, nexttradetime, nexttradetime, 0));
        
        this.GlobalVarTable.Clear();
        this.InitVarTable();
    }
    public override void AddFeeder(Feeder _feed)
    {

    }
    public override List<string> GetAdjustedPropertyNamesList()
    {
        List<string> propList = new List<string>();

        propList.Add("TargetGap");
        //propList.Add("DayGap");
        //propList.Add("TotalLongVolume");
        //propList.Add("TotalShortVolume");
        return propList;
    }


    public void InitVarTable()
    {
        this.GlobalVarTable.Add("Lower High-Gap", "0");
        this.GlobalVarTable.Add("BreakHigh", "0");
        this.GlobalVarTable.Add("Higher Low+Gap", "0");
        this.GlobalVarTable.Add("BreakLow", "0");
        this.GlobalVarTable.Add("PrevProfit", "0");
        this.GlobalVarTable.Add("OpenAfternoon", "0");
        this.GlobalVarTable.Add("ReverseLong", "0");
        this.GlobalVarTable.Add("ReverseShort", "0");
        this.GlobalVarTable.Add("StopTrade", "0");
    }
    public void ClearVarTable()
    {
        this.GlobalVarTable["Lower High-Gap"] = "0";
        this.GlobalVarTable["BreakHigh"] = "0";
        this.GlobalVarTable["Higher Low+Gap"] = "0";
        this.GlobalVarTable["BreakLow"] = "0";
        this.GlobalVarTable["OpenAfternoon"] = "0";
        this.GlobalVarTable["ReverseLong"] = "0";
        this.GlobalVarTable["ReverseShort"] = "0";
        this.GlobalVarTable["StopTrade"] = "0";

    }
    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[7];

        col[0] = new DataColumn("datetime", typeof(System.DateTime)); //eg. 9:45 
        col[1] = new DataColumn("status", typeof(System.String));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", typeof(System.Double)); //price
        col[3] = new DataColumn("openshort", typeof(System.Double));//price
        col[4] = new DataColumn("closelong", typeof(System.Double));//price
        col[5] = new DataColumn("closeshort", typeof(System.Double));//price
        col[6] = new DataColumn("tradevol", typeof(System.Double));//price
        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }

    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
        sigrow["tradevol"] = this.TradeVol;
        switch (status)
        {
            case "none":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "dup":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openlong":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closelong":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = price;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closeshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = price;
                    break;
                }
            default:
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
        }
        return sigrow;
    }



    private double CloseStopLoss(double onhandcost, string positionside)
    {
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stoploss = this.LogicPropTable["StopLoss"].Value;
        double stoplossprice = 0;
        double close = 0;
        double profit = 0;
        if (stoploss != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
            {
                profit = close - onhandcost;
            }
            else if (positionside == "openshort")
            {
                profit = onhandcost - close;
            }
            if (profit < stoploss)
            {

                stoplossprice = close;


            }
        }
        return stoplossprice;
    }
    private double CloseStopGain(double onhandcost, string positionside)
    {
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopgain = this.LogicPropTable["StopGain"].Value;
        double stopgainprice = 0;
        double close = 0;
        double profit = 0;
        if (stopgain != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit > stopgain)
            {

                stopgainprice = close;
            }
        }
        return stopgainprice;
    }
    private double CloseStopProfitLoss(double onhandcost, string positionside)
    {
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopprofitloss = this.LogicPropTable["StopProfitLoss"].Value;
        double prevprofit = Double.Parse(this.GlobalVarTable["PrevProfit"]);
        double stopprofitlossprice = 0;
        double close = 0;
        double profit = 0;
        double profitdiff = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
        if (positionside == "openlong")
            profit = close - onhandcost;
        else if (positionside == "openshort")
            profit = onhandcost - close;
        if (prevprofit < profit)
            this.GlobalVarTable["PrevProfit"] = profit.ToString();//Update previous profit

        if ((stopprofitloss != 0) && (profit > 0) && (prevprofit > 0))
        {
            profitdiff = profit - prevprofit;
            if (profitdiff < stopprofitloss)
                stopprofitlossprice = close;
        }
        return stopprofitlossprice;

    }


    public override DataRow OpenLongStatus(Portfolio port)
    {
        double onhandcost = port.CostOnhand(symbol);
        double price = 0;//not hit any condition

        //Start default trade vol = 1
        this.TradeVol = 1;

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        TimeSpan targettimetrade = TimeSpan.FromSeconds((Double)this.LogicPropTable["TargetTimeTrade"].Value);
        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double high = (Double)this.HistoricalTable.Rows[counthistrow - 1]["High"];
        double low = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Low"];
        double vol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Volume"];
        double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];



        double targetgap = this.LogicPropTable["TargetGap"].Value;
        double targetlongvolpercent = this.LogicPropTable["TotalLongVolPercent"].Value;
        double targetshortvolpercent = this.LogicPropTable["TotalShortVolPercent"].Value;
        double ltimesec = this.LogicPropTable["TimeLookBack"].Value;

        double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
        double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
        double tlongtrans = this.LogicPropTable["TotalLongTrans"].Value;
        double tshorttrans = this.LogicPropTable["TotalShortTrans"].Value;
        double tlonggap = this.LogicPropTable["LongGap"].Value;
        double tshortgap = this.LogicPropTable["ShortGap"].Value;
        double tswinggap = this.LogicPropTable["SwingGap"].Value;
        double tdaygap = this.LogicPropTable["DayGap"].Value;
        double tpercentgap = this.LogicPropTable["PercentGap"].Value;

        double lastbiglotslongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
        double lastbiglotsshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];

        /*
        if (time == (TimeSpan.Parse("14:30:00")))
            this.VarTable["OpenAfternoon"] = open.ToString();
        */
        if ((port.VolOnhand(symbol) < 1) && (counthistrow > monitor) && (time >= openstarttime) && (time <= openendtime) && (time != (TimeSpan.Parse("14:15:00")) && (time != (TimeSpan.Parse("09:45:00")))))
        {

            found = 0;

            //Check gap high low lookback unitil expect x seconds
            double tmpvol = 0;
            double tmpnewshorthigh = 0;
            double tmpnewlonglow = 0;
            double tmpcount = 0;
            double tmpclose = (Double)this.HistoricalTable.Rows[counthistrow - 2]["Close"];

            double tmphigh = 0;
            double tmplow = 0;
            double tmphl = 0;
            TimeSpan gaptime = TimeSpan.FromSeconds(0);
            double tmpdayhigh = (Double)this.HistoricalTable.Rows[counthistrow - 2]["DayHigh"];
            double tmpdaylow = (Double)this.HistoricalTable.Rows[counthistrow - 2]["DayLow"];


            for (int m = 2; m < monitor; m++)
            {
                double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                double daylowprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                double biglotclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Close"];
                double totallongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalLongVolume"];
                double totalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"];
                double percentlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVolume%"];
                double percentshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVolume%"];

                double biglotgaphl = dayhighprice - daylowprice;
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];

                /*
                if ((biglotgaphl > tdaygap) && (biglotclose == daylowprice ))
                {
                    //if ((close-daylowprice > tlonggap))
                    if((close+tlonggap >= dayhighprice))
                        found++;
                }
                */
                if (percentlongvol > tpercentgap)
                {

                    found++;

                }



            }

            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = close;
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("openlong", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    public override DataRow OpenShortStatus(Portfolio port)
    {
        double onhandcost = port.CostOnhand(symbol);
        double price = 0;//not hit any condition
                         //Start Trade vol = 1
        this.TradeVol = 1;

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        TimeSpan targettimetrade = TimeSpan.FromSeconds((Double)this.LogicPropTable["TargetTimeTrade"].Value);
        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double high = (Double)this.HistoricalTable.Rows[counthistrow - 1]["High"];
        double low = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Low"];
        double vol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Volume"];
        double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];

        double targetgap = this.LogicPropTable["TargetGap"].Value;
        double targetlongvolpercent = this.LogicPropTable["TotalLongVolPercent"].Value;
        double targetshortvolpercent = this.LogicPropTable["TotalShortVolPercent"].Value;
        double ltimesec = this.LogicPropTable["TimeLookBack"].Value;

        double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
        double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
        double tlongtrans = this.LogicPropTable["TotalLongTrans"].Value;
        double tshorttrans = this.LogicPropTable["TotalShortTrans"].Value;
        double tlonggap = this.LogicPropTable["LongGap"].Value;
        double tshortgap = this.LogicPropTable["ShortGap"].Value;
        double tswinggap = this.LogicPropTable["SwingGap"].Value;
        double tdaygap = this.LogicPropTable["DayGap"].Value;
        double tpercentgap = this.LogicPropTable["PercentGap"].Value;

        double lastbiglotslongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
        double lastbiglotsshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];

        if ((port.VolOnhand(symbol) > -1) && (counthistrow > monitor) && (time >= openstarttime) && (time <= openendtime) && (time != (TimeSpan.Parse("14:15:00")) && (time != (TimeSpan.Parse("09:45:00")))))
        {
            found = 0;

            //Check gap high low lookback unitil expect x seconds
            double tmpvol = 0;
            double tmpnewshorthigh = 0;
            double tmpnewlonglow = 0;
            double tmpcount = 0;
            double tmpclose = (Double)this.HistoricalTable.Rows[counthistrow - 2]["Close"];


            for (int m = 2; m < monitor; m++)
            {
                double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                double daylowprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                double biglotclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Close"];
                double totallongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalLongVolume"];
                double totalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"];
                double percentlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVolume%"];
                double percentshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVolume%"];

                double biglotgaphl = dayhighprice - daylowprice;
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];


                /*
                if ((biglotgaphl > tdaygap) && (biglotclose == dayhighprice))
                {
                    //if ((dayhighprice-close > tlonggap))
                    if ((close-tshortgap < daylowprice))
                        found++;
                }
                */
                if (percentshortvol > tpercentgap)
                {
                    found++;

                }



            }
            if ((found >= this.LogicPropTable["SignalShort"].Value))
            {
                price = open;
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("openshort", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    public override DataRow CloseLongStatus(Portfolio port)
    {
        double onhandcost = port.AvgOnhandPrice(symbol);
        double price = 0;
        double stoplossprice = this.CloseStopLoss(onhandcost, "openlong");
        double stopgainprice = this.CloseStopGain(onhandcost, "openlong");
        double stopprofitlossprice = this.CloseStopProfitLoss(onhandcost, "openlong");
        if (stoplossprice > 0)
            price = stoplossprice; //Stop loss
        else if (stopgainprice > 0)
            price = stopgainprice;//Stop gain
        else if (stopprofitlossprice > 0)//Stop profit loss
            price = stopprofitlossprice;

        else
        {

            //price = this.CloseLongCheckGap();
            //price = this.OpenShortStatus(onhandcost);
            int counthistrow = this.HistoricalTable.Rows.Count;
            double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];
            double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
            double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
            /*
            if (biglotssumshortvol < tshortvol)
                price = close;
            */
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);

            if (time >= endtime)
                price = close;
        }

        if (price > 0)
        {
            //Update tradevol, close all onhandvol
            this.TradeVol = this.CurrentOnhandVol;
            this.ClearVarTable();

            if (stoplossprice > 0)
            {
                GlobalVarTable["ReverseShort"] = "1";
                GlobalVarTable["StopTrade"] = "1";
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("closelong", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    public override DataRow CloseShortStatus(Portfolio port)
    {
        double onhandcost = port.AvgOnhandPrice(symbol);
        double price = 0;
        double stoplossprice = this.CloseStopLoss(onhandcost, "openshort");
        double stopgainprice = this.CloseStopGain(onhandcost, "openshort");
        double stopprofitlossprice = this.CloseStopProfitLoss(onhandcost, "openshort");
        if (stoplossprice > 0)
            price = stoplossprice; //Stop loss
        else if (stopgainprice > 0)
            price = stopgainprice;//Stop gain
        else if (stopprofitlossprice > 0)
            price = stopprofitlossprice;//Stop loss profit
        else
        {
            //price = this.CloseLongCheckGap();
            //price = this.OpenShortStatus(onhandcost);

            int counthistrow = this.HistoricalTable.Rows.Count;
            double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];
            double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
            double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
            /*
            if (biglotssumlongvol > tlongvol)
                price = close;
            */
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);

            if (time >= endtime)
                price = close;
        }
        if (price > 0)
        {
            //Update tradevol, close all onhandvol
            this.TradeVol = this.CurrentOnhandVol;
            this.ClearVarTable();
            if (stoplossprice > 0)
            {
                GlobalVarTable["ReverseLong"] = "1";
                GlobalVarTable["StopTrade"] = "1";
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("closeshort", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    private double CloseStopLoss(Portfolio port, string positionside)
    {
        double onhandcost = port.CostOnhand(symbol) / port.VolOnhand(symbol);
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stoploss = this.LogicPropTable["StopLoss"].Value;
        double stoplossprice = 0;
        double close = 0;
        double profit = 0;
        if (stoploss != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
            {
                profit = close - onhandcost;
            }
            else if (positionside == "openshort")
            {
                profit = onhandcost - close;
            }
            if (profit < stoploss)
            {

                stoplossprice = close;


            }
        }
        return stoplossprice;
    }
    private double CloseStopGain(Portfolio port, string positionside)
    {
        double onhandcost = port.CostOnhand(symbol) / port.VolOnhand(symbol);
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopgain = this.LogicPropTable["StopGain"].Value;
        double stopgainprice = 0;
        double close = 0;
        double profit = 0;
        if (stopgain != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit > stopgain)
            {

                stopgainprice = close;
            }
        }
        return stopgainprice;
    }
    private double CloseStopProfitLoss(Portfolio port, string positionside)
    {
        double onhandcost = port.CostOnhand(symbol);
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopprofitloss = this.LogicPropTable["StopProfitLoss"].Value;
        double prevprofit = Double.Parse(this.GlobalVarTable["PrevProfit"]);
        double stopprofitlossprice = 0;
        double close = 0;
        double profit = 0;
        double profitdiff = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
        if (positionside == "openlong")
            profit = close - onhandcost;
        else if (positionside == "openshort")
            profit = onhandcost - close;
        if (prevprofit < profit)
            this.GlobalVarTable["PrevProfit"] = profit.ToString();//Update previous profit

        if ((stopprofitloss != 0) && (profit > 0) && (prevprofit > 0))
        {
            profitdiff = profit - prevprofit;
            if (profitdiff < stopprofitloss)
                stopprofitlossprice = close;
        }
        return stopprofitlossprice;

    }

    public override DataRow OnData(string symbol, DataRow resamplingtick, ref Portfolio port)
    {
        double onhandvol = port.VolOnhand(symbol);
        //first init signalTradeNone for reduce CPU time
        if (signalTradeNone == null)
            signalTradeNone = GetSingnal("null", 0, 0);

        DataRow tradesignal = signalTradeNone;//GetSignal("none",0,0);

        DateTime tradedatetime = ((DateTime)resamplingtick[0]).Add((TimeSpan)resamplingtick[1]);
        //DateTime endtradedatetime = ((DateTime)resamplingtick[0]).Add(TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value));

        if (tradedatetime >= NextTradeDateTime)
        {
            if (port.IsLong(symbol))// hold long
            {
                
                //Check OpenLong
                if ((port.VolOnhand(symbol) < (double)LogicPropTable["MaxLongOnhandVol"].Value))
                {
                    tradesignal = this.OpenLongStatus(port);

                }
                if ((double)tradesignal["tradevol"] == 0)//if no openlong check close status
                {
                    tradesignal = this.CloseLongStatus(port);
                }

            }
            else if (port.IsShort(symbol))// hold short
            {

                //Check Openshort
                if ((port.VolOnhand(symbol) > (double)LogicPropTable["MaxShortOnhandVol"].Value))
                {
                    tradesignal = this.OpenShortStatus(port);

                }
                if ((double)tradesignal["tradevol"] == 0)//if no openlong check close status
                {
                    tradesignal = this.CloseShortStatus(port);
                }
            }
            else//none hold
            {
                //check openlong first if not found check openshort
                tradesignal = this.OpenLongStatus(port);
                if ((double)tradesignal["tradevol"] == 0)//if no openlong check close status
                {
                    tradesignal = this.OpenShortStatus(port);
                }
            }
            //Check trade 
            if ((double)tradesignal["tradevol"] != 0)
            {
                //NextTradeTime = (TimeSpan)resamplingtick[1] + TimeSpan.FromSeconds((Double)this.LogicPropTable["NextTradeTime"].Value);
                NextTradeDateTime = ((DateTime)resamplingtick[0]).Add((TimeSpan)resamplingtick[1] + TimeSpan.FromSeconds((Double)this.LogicPropTable["NextTradeTime"].Value));
            }
        }
        this.SignalTable.Rows.Add(tradesignal.ItemArray);
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }

    private DataRow GetSingnal(string status, double price, double volume)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
        sigrow["tradevol"] = volume;
        switch (status)
        {
            case "none":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "dup":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openlong":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closelong":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = price;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closeshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = price;
                    break;
                }
            default:
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
        }
        return sigrow;
    }

}



class PercentVolTradeLogic : TradeLogic
{
    TimeSpan NextTradeTime;
    public PercentVolTradeLogic(string _symbol, DataTable newResamplingTable, int interval)
    {
        //Update historical table
        symbol = _symbol;
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.GlobalVarTable = new Dictionary<String, string>();
        this.InitLogicPropTable(interval);

        //Init Next trade time
        NextTradeTime = TimeSpan.FromSeconds(0);
        NextTradeDateTime = new DateTime();
        //signalTradeNone = GetSingnal("none", 0, 0);

    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume", 0, 200, 1000, 200));//400
        LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", 0, -1000, -200, 200));//400
        LogicPropTable.Add("TotalLongTrans", new Property("TotalLongTrans", 200, 100, 200, 100));
        LogicPropTable.Add("TotalShortTrans", new Property("TotalShortTrans", 200, 100, 200, 100));
        LogicPropTable.Add("SwingGap", new Property("SwingGap", 7, 0.5, 2, 0.5));
        LogicPropTable.Add("TargetGap", new Property("TargetGap", 4, 1, 5, 1));
        LogicPropTable.Add("DayGap", new Property("DayGap", 3.5, 5, 12, 1));
        LogicPropTable.Add("LongGap", new Property("LongGap", 2, 0, 3, 0.5));
        LogicPropTable.Add("ShortGap", new Property("ShortGap", 2, 0, 3, 0.5));
        LogicPropTable.Add("PercentGap", new Property("PercentGap", 61.5, 0, 100, 1));
        double endtimetrade = TimeSpan.Parse("16:54:30").TotalSeconds;
        double openstarttimetrade = TimeSpan.Parse("11:20:00").TotalSeconds;//15:40 best
        double openendtimetrade = TimeSpan.Parse("16:05:00").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", -1.5, -8, -2, 1));
        LogicPropTable.Add("StopGain", new Property("StopGain", 2.8, 2, 5, 0.5));
        LogicPropTable.Add("StopProfitLoss", new Property("StopProfitLoss", 0, -8, -2, 1));
        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 1, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -1, -500, -40, 50));
        LogicPropTable.Add("MaxLongOnhandVol", new Property("MaxLongOnhandVol", 1, 1, 10, 1));
        LogicPropTable.Add("MaxShortOnhandVol", new Property("MaxShortOnhandVol", -1, 1, 10, 1));
        LogicPropTable.Add("MaxOnhandCost", new Property("MaxOnhandCost", 1000000, 100000, 1000000, 100000));


        LogicPropTable.Add("TimeLookBack", new Property("TimeLookBack", 1200, 100, 100, 100));
        double targettimetrade = TimeSpan.Parse("16:00:00").TotalSeconds;
        LogicPropTable.Add("TargetTimeTrade", new Property("TargetTimeTrade", targettimetrade, targettimetrade, targettimetrade, 0));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 1, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 1, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 3, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 3, 1, 30, 1)); 


        LogicPropTable.Add("OpenSignalGap", new Property("OpenSignalGap", 0, 1, 10, 1));


        //double openstarttimetrade = TimeSpan.Parse("14:20:00").TotalSeconds;
        //double openendtimetrade = TimeSpan.Parse("16:25:00").TotalSeconds;
        //double endtimetrade = TimeSpan.Parse("16:54:00").TotalSeconds;

        LogicPropTable.Add("TotalLongVolPercent", new Property("TotalLongVolPercent", 35, -8, -2, 1));
        LogicPropTable.Add("TotalShortVolPercent", new Property("TotalShortVolPercent", 35, -8, -2, 1));
        //new trade property


        double nexttradetime = TimeSpan.Parse("00:01:00").TotalSeconds;
        LogicPropTable.Add("NextTradeTime", new Property("NextTradeTime", nexttradetime, nexttradetime, nexttradetime, 0));

        this.GlobalVarTable.Clear();
        this.InitVarTable();
    }
    public override void AddFeeder(Feeder _feed)
    {

    }
    public override List<string> GetAdjustedPropertyNamesList()
    {
        List<string> propList = new List<string>();

        propList.Add("TargetGap");
        //propList.Add("DayGap");
        //propList.Add("TotalLongVolume");
        //propList.Add("TotalShortVolume");
        return propList;
    }


    public void InitVarTable()
    {
        this.GlobalVarTable.Add("Lower High-Gap", "0");
        this.GlobalVarTable.Add("BreakHigh", "0");
        this.GlobalVarTable.Add("Higher Low+Gap", "0");
        this.GlobalVarTable.Add("BreakLow", "0");
        this.GlobalVarTable.Add("PrevProfit", "0");
        this.GlobalVarTable.Add("OpenAfternoon", "0");
        this.GlobalVarTable.Add("ReverseLong", "0");
        this.GlobalVarTable.Add("ReverseShort", "0");
        this.GlobalVarTable.Add("StopTrade", "0");
    }
    public void ClearVarTable()
    {
        this.GlobalVarTable["Lower High-Gap"] = "0";
        this.GlobalVarTable["BreakHigh"] = "0";
        this.GlobalVarTable["Higher Low+Gap"] = "0";
        this.GlobalVarTable["BreakLow"] = "0";
        this.GlobalVarTable["OpenAfternoon"] = "0";
        this.GlobalVarTable["ReverseLong"] = "0";
        this.GlobalVarTable["ReverseShort"] = "0";
        this.GlobalVarTable["StopTrade"] = "0";

    }
    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[7];

        col[0] = new DataColumn("datetime", typeof(System.DateTime)); //eg. 9:45 
        col[1] = new DataColumn("status", typeof(System.String));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", typeof(System.Double)); //price
        col[3] = new DataColumn("openshort", typeof(System.Double));//price
        col[4] = new DataColumn("closelong", typeof(System.Double));//price
        col[5] = new DataColumn("closeshort", typeof(System.Double));//price
        col[6] = new DataColumn("tradevol", typeof(System.Double));//price
        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }

    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
        sigrow["tradevol"] = this.TradeVol;
        switch (status)
        {
            case "none":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "dup":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openlong":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closelong":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = price;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closeshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = price;
                    break;
                }
            default:
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
        }
        return sigrow;
    }



    private double CloseStopLoss(double onhandcost, string positionside)
    {
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stoploss = this.LogicPropTable["StopLoss"].Value;
        double stoplossprice = 0;
        double close = 0;
        double profit = 0;
        if (stoploss != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
            {
                profit = close - onhandcost;
            }
            else if (positionside == "openshort")
            {
                profit = onhandcost - close;
            }
            if (profit < stoploss)
            {

                stoplossprice = close;


            }
        }
        return stoplossprice;
    }
    private double CloseStopGain(double onhandcost, string positionside)
    {
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopgain = this.LogicPropTable["StopGain"].Value;
        double stopgainprice = 0;
        double close = 0;
        double profit = 0;
        if (stopgain != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit > stopgain)
            {

                stopgainprice = close;
            }
        }
        return stopgainprice;
    }
    private double CloseStopProfitLoss(double onhandcost, string positionside)
    {
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopprofitloss = this.LogicPropTable["StopProfitLoss"].Value;
        double prevprofit = Double.Parse(this.GlobalVarTable["PrevProfit"]);
        double stopprofitlossprice = 0;
        double close = 0;
        double profit = 0;
        double profitdiff = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
        if (positionside == "openlong")
            profit = close - onhandcost;
        else if (positionside == "openshort")
            profit = onhandcost - close;
        if (prevprofit < profit)
            this.GlobalVarTable["PrevProfit"] = profit.ToString();//Update previous profit

        if ((stopprofitloss != 0) && (profit > 0) && (prevprofit > 0))
        {
            profitdiff = profit - prevprofit;
            if (profitdiff < stopprofitloss)
                stopprofitlossprice = close;
        }
        return stopprofitlossprice;

    }


    public override DataRow OpenLongStatus(Portfolio port)
    {
        double onhandcost = port.CostOnhand(symbol);
        double price = 0;//not hit any condition

        //Start default trade vol = 1
        this.TradeVol = 1;

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        TimeSpan targettimetrade = TimeSpan.FromSeconds((Double)this.LogicPropTable["TargetTimeTrade"].Value);
        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double high = (Double)this.HistoricalTable.Rows[counthistrow - 1]["High"];
        double low = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Low"];
        double vol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Volume"];
        double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];



        double targetgap = this.LogicPropTable["TargetGap"].Value;
        double targetlongvolpercent = this.LogicPropTable["TotalLongVolPercent"].Value;
        double targetshortvolpercent = this.LogicPropTable["TotalShortVolPercent"].Value;
        double ltimesec = this.LogicPropTable["TimeLookBack"].Value;

        double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
        double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
        double tlongtrans = this.LogicPropTable["TotalLongTrans"].Value;
        double tshorttrans = this.LogicPropTable["TotalShortTrans"].Value;
        double tlonggap = this.LogicPropTable["LongGap"].Value;
        double tshortgap = this.LogicPropTable["ShortGap"].Value;
        double tswinggap = this.LogicPropTable["SwingGap"].Value;
        double tdaygap = this.LogicPropTable["DayGap"].Value;
        double tpercentgap = this.LogicPropTable["PercentGap"].Value;

        double lastbiglotslongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
        double lastbiglotsshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];

        /*
        if (time == (TimeSpan.Parse("14:30:00")))
            this.VarTable["OpenAfternoon"] = open.ToString();
        */
        if ((port.VolOnhand(symbol) < 1) && (counthistrow > monitor) && (time >= openstarttime) && (time <= openendtime) && (time != (TimeSpan.Parse("14:15:00")) && (time != (TimeSpan.Parse("09:45:00")))))
        {

            found = 0;

            //Check gap high low lookback unitil expect x seconds
            double tmpvol = 0;
            double tmpnewshorthigh = 0;
            double tmpnewlonglow = 0;
            double tmpcount = 0;
            double tmpclose = (Double)this.HistoricalTable.Rows[counthistrow - 2]["Close"];

            double tmphigh = 0;
            double tmplow = 0;
            double tmphl = 0;
            TimeSpan gaptime = TimeSpan.FromSeconds(0);
            double tmpdayhigh = (Double)this.HistoricalTable.Rows[counthistrow - 2]["DayHigh"];
            double tmpdaylow = (Double)this.HistoricalTable.Rows[counthistrow - 2]["DayLow"];


            for (int m = 2; m < monitor; m++)
            {
                double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                double daylowprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                double biglotclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Close"];
                double totallongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalLongVolume"];
                double totalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"];
                double percentlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVolume%"];
                double percentshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVolume%"];

                double biglotgaphl = dayhighprice - daylowprice;
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];

                /*
                if ((biglotgaphl > tdaygap) && (biglotclose == daylowprice ))
                {
                    //if ((close-daylowprice > tlonggap))
                    if((close+tlonggap >= dayhighprice))
                        found++;
                }
                */
                if (biglotgaphl<=tdaygap)
                {
                    if(close > dayhighprice)
                        found++;

                }



            }

            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = close;
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("openlong", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    public override DataRow OpenShortStatus(Portfolio port)
    {
        double onhandcost = port.CostOnhand(symbol);
        double price = 0;//not hit any condition
                         //Start Trade vol = 1
        this.TradeVol = 1;

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        TimeSpan targettimetrade = TimeSpan.FromSeconds((Double)this.LogicPropTable["TargetTimeTrade"].Value);
        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double high = (Double)this.HistoricalTable.Rows[counthistrow - 1]["High"];
        double low = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Low"];
        double vol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Volume"];
        double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];

        double targetgap = this.LogicPropTable["TargetGap"].Value;
        double targetlongvolpercent = this.LogicPropTable["TotalLongVolPercent"].Value;
        double targetshortvolpercent = this.LogicPropTable["TotalShortVolPercent"].Value;
        double ltimesec = this.LogicPropTable["TimeLookBack"].Value;

        double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
        double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
        double tlongtrans = this.LogicPropTable["TotalLongTrans"].Value;
        double tshorttrans = this.LogicPropTable["TotalShortTrans"].Value;
        double tlonggap = this.LogicPropTable["LongGap"].Value;
        double tshortgap = this.LogicPropTable["ShortGap"].Value;
        double tswinggap = this.LogicPropTable["SwingGap"].Value;
        double tdaygap = this.LogicPropTable["DayGap"].Value;
        double tpercentgap = this.LogicPropTable["PercentGap"].Value;

        double lastbiglotslongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
        double lastbiglotsshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];

        if ((port.VolOnhand(symbol) > -1) && (counthistrow > monitor) && (time >= openstarttime) && (time <= openendtime) && (time != (TimeSpan.Parse("14:15:00")) && (time != (TimeSpan.Parse("09:45:00")))))
        {
            found = 0;

            //Check gap high low lookback unitil expect x seconds
            double tmpvol = 0;
            double tmpnewshorthigh = 0;
            double tmpnewlonglow = 0;
            double tmpcount = 0;
            double tmpclose = (Double)this.HistoricalTable.Rows[counthistrow - 2]["Close"];


            for (int m = 2; m < monitor; m++)
            {
                double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                double daylowprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                double biglotclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Close"];
                double totallongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalLongVolume"];
                double totalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"];
                double percentlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVolume%"];
                double percentshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVolume%"];

                double biglotgaphl = dayhighprice - daylowprice;
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];


                /*
                if ((biglotgaphl > tdaygap) && (biglotclose == dayhighprice))
                {
                    //if ((dayhighprice-close > tlonggap))
                    if ((close-tshortgap < daylowprice))
                        found++;
                }
                */
                if (biglotgaphl <= tdaygap)
                {
                    if (close < daylowprice)
                        found++;

                }



            }
            if ((found >= this.LogicPropTable["SignalShort"].Value))
            {
                price = open;
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("openshort", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    public override DataRow CloseLongStatus(Portfolio port)
    {
        double onhandcost = port.AvgOnhandPrice(symbol);
        double price = 0;
        double stoplossprice = this.CloseStopLoss(onhandcost, "openlong");
        double stopgainprice = this.CloseStopGain(onhandcost, "openlong");
        double stopprofitlossprice = this.CloseStopProfitLoss(onhandcost, "openlong");
        if (stoplossprice > 0)
            price = stoplossprice; //Stop loss
        else if (stopgainprice > 0)
            price = stopgainprice;//Stop gain
        else if (stopprofitlossprice > 0)//Stop profit loss
            price = stopprofitlossprice;

        else
        {

            //price = this.CloseLongCheckGap();
            //price = this.OpenShortStatus(onhandcost);
            int counthistrow = this.HistoricalTable.Rows.Count;
            double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];
            double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
            double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
            /*
            if (biglotssumshortvol < tshortvol)
                price = close;
            */
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);

            if (time >= endtime)
                price = close;
        }

        if (price > 0)
        {
            //Update tradevol, close all onhandvol
            this.TradeVol = this.CurrentOnhandVol;
            this.ClearVarTable();

            if (stoplossprice > 0)
            {
                GlobalVarTable["ReverseShort"] = "1";
                GlobalVarTable["StopTrade"] = "1";
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("closelong", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    public override DataRow CloseShortStatus(Portfolio port)
    {
        double onhandcost = port.AvgOnhandPrice(symbol);
        double price = 0;
        double stoplossprice = this.CloseStopLoss(onhandcost, "openshort");
        double stopgainprice = this.CloseStopGain(onhandcost, "openshort");
        double stopprofitlossprice = this.CloseStopProfitLoss(onhandcost, "openshort");
        if (stoplossprice > 0)
            price = stoplossprice; //Stop loss
        else if (stopgainprice > 0)
            price = stopgainprice;//Stop gain
        else if (stopprofitlossprice > 0)
            price = stopprofitlossprice;//Stop loss profit
        else
        {
            //price = this.CloseLongCheckGap();
            //price = this.OpenShortStatus(onhandcost);

            int counthistrow = this.HistoricalTable.Rows.Count;
            double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVol Biglots"];
            double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            double tlongvol = this.LogicPropTable["TotalLongVolume"].Value;
            double tshortvol = this.LogicPropTable["TotalShortVolume"].Value;
            /*
            if (biglotssumlongvol > tlongvol)
                price = close;
            */
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);

            if (time >= endtime)
                price = close;
        }
        if (price > 0)
        {
            //Update tradevol, close all onhandvol
            this.TradeVol = this.CurrentOnhandVol;
            this.ClearVarTable();
            if (stoplossprice > 0)
            {
                GlobalVarTable["ReverseLong"] = "1";
                GlobalVarTable["StopTrade"] = "1";
            }
        }
        DataRow signalrow;
        if (price > 0)
            signalrow = GetSingnal("closeshort", price, 1);
        else signalrow = signalTradeNone;
        return signalrow;
    }
    private double CloseStopLoss(Portfolio port, string positionside)
    {
        double onhandcost = port.CostOnhand(symbol) / port.VolOnhand(symbol);
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stoploss = this.LogicPropTable["StopLoss"].Value;
        double stoplossprice = 0;
        double close = 0;
        double profit = 0;
        if (stoploss != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
            {
                profit = close - onhandcost;
            }
            else if (positionside == "openshort")
            {
                profit = onhandcost - close;
            }
            if (profit < stoploss)
            {

                stoplossprice = close;


            }
        }
        return stoplossprice;
    }
    private double CloseStopGain(Portfolio port, string positionside)
    {
        double onhandcost = port.CostOnhand(symbol) / port.VolOnhand(symbol);
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopgain = this.LogicPropTable["StopGain"].Value;
        double stopgainprice = 0;
        double close = 0;
        double profit = 0;
        if (stopgain != 0)
        {
            if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
                close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            if (positionside == "openlong")
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit > stopgain)
            {

                stopgainprice = close;
            }
        }
        return stopgainprice;
    }
    private double CloseStopProfitLoss(Portfolio port, string positionside)
    {
        double onhandcost = port.CostOnhand(symbol);
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stopprofitloss = this.LogicPropTable["StopProfitLoss"].Value;
        double prevprofit = Double.Parse(this.GlobalVarTable["PrevProfit"]);
        double stopprofitlossprice = 0;
        double close = 0;
        double profit = 0;
        double profitdiff = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
        if (positionside == "openlong")
            profit = close - onhandcost;
        else if (positionside == "openshort")
            profit = onhandcost - close;
        if (prevprofit < profit)
            this.GlobalVarTable["PrevProfit"] = profit.ToString();//Update previous profit

        if ((stopprofitloss != 0) && (profit > 0) && (prevprofit > 0))
        {
            profitdiff = profit - prevprofit;
            if (profitdiff < stopprofitloss)
                stopprofitlossprice = close;
        }
        return stopprofitlossprice;

    }

    public override DataRow OnData(string symbol, DataRow resamplingtick, ref Portfolio port)
    {
        double onhandvol = port.VolOnhand(symbol);
        //first init signalTradeNone for reduce CPU time
        if (signalTradeNone == null)
            signalTradeNone = GetSingnal("null", 0, 0);

        DataRow tradesignal = signalTradeNone;//GetSignal("none",0,0);

        DateTime tradedatetime = ((DateTime)resamplingtick[0]).Add((TimeSpan)resamplingtick[1]);
        //DateTime endtradedatetime = ((DateTime)resamplingtick[0]).Add(TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value));

        if (tradedatetime >= NextTradeDateTime)
        {
            if (port.IsLong(symbol))// hold long
            {

                //Check OpenLong
                if ((port.VolOnhand(symbol) < (double)LogicPropTable["MaxLongOnhandVol"].Value))
                {
                    tradesignal = this.OpenLongStatus(port);

                }
                if ((double)tradesignal["tradevol"] == 0)//if no openlong check close status
                {
                    tradesignal = this.CloseLongStatus(port);
                }

            }
            else if (port.IsShort(symbol))// hold short
            {

                //Check Openshort
                if ((port.VolOnhand(symbol) > (double)LogicPropTable["MaxShortOnhandVol"].Value))
                {
                    tradesignal = this.OpenShortStatus(port);

                }
                if ((double)tradesignal["tradevol"] == 0)//if no openlong check close status
                {
                    tradesignal = this.CloseShortStatus(port);
                }
            }
            else//none hold
            {
                //check openlong first if not found check openshort
                tradesignal = this.OpenLongStatus(port);
                if ((double)tradesignal["tradevol"] == 0)//if no openlong check close status
                {
                    tradesignal = this.OpenShortStatus(port);
                }
            }
            //Check trade 
            if ((double)tradesignal["tradevol"] != 0)
            {
                //NextTradeTime = (TimeSpan)resamplingtick[1] + TimeSpan.FromSeconds((Double)this.LogicPropTable["NextTradeTime"].Value);
                NextTradeDateTime = ((DateTime)resamplingtick[0]).Add((TimeSpan)resamplingtick[1] + TimeSpan.FromSeconds((Double)this.LogicPropTable["NextTradeTime"].Value));
            }
        }
        this.SignalTable.Rows.Add(tradesignal.ItemArray);
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }

    private DataRow GetSingnal(string status, double price, double volume)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
        sigrow["tradevol"] = volume;
        switch (status)
        {
            case "none":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "dup":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openlong":
                {
                    sigrow["openlong"] = price;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "openshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = price;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closelong":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = price;
                    sigrow["closeshort"] = 0;
                    break;
                }
            case "closeshort":
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = price;
                    break;
                }
            default:
                {
                    sigrow["openlong"] = 0;
                    sigrow["openshort"] = 0;
                    sigrow["closelong"] = 0;
                    sigrow["closeshort"] = 0;
                    break;
                }
        }
        return sigrow;
    }

}
