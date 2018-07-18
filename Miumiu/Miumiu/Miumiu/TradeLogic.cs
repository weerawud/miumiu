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
    public DataTable SignalTable;
    public Dictionary<String,Property> LogicPropTable;
    public Dictionary<String, string> GlobalVarTable;
    
   
    abstract public void UpdateHistoricalTable(DataTable dt);
    abstract public DataTable NewSignalTable();
    abstract public void InitLogicPropTable(int interval);
    //abstract public List<string> GetAdjustedPropertyNamesList();
    abstract public DataRow FeedTickTrade(DataRow tick,string portstatus,double onhandcost,double onhandvol);
    abstract public Double OpenLongStatus(double onhandcost);
    abstract public Double OpenShortStatus(double onhandcost);
    abstract public Double CloseLongStatus(double onhandcost);
    abstract public Double CloseShortStatus(double onhandcost);
    public string GetLogicName()
    {
        return this.GetType().Name;
    }
    public void ChangeLogicPropTable(string propname, double val)
    {
        LogicPropTable[propname].Value = val;
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
        col[0] = new DataColumn("id", Type.GetType("System.String"));
        col[1] = new DataColumn("propertyname", Type.GetType("System.String"));
        col[2] = new DataColumn("value", Type.GetType("System.Double"));
        col[3] = new DataColumn("min", Type.GetType("System.Double"));
        col[4] = new DataColumn("max", Type.GetType("System.Double"));
        col[5] = new DataColumn("step", Type.GetType("System.Double"));

        dt.Columns.AddRange(col);
        return dt;
    }


}
/*
class GapTradeLogic : TradeLogic
{
    public GapTradeLogic(DataTable newResamplingTable,int interval)
    {
        //Update historical table
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.InitLogicPropTable(interval);

    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume", 200, 100, 1000, 100));
        LogicPropTable.Add("TotalShortVolume", new Property("TotalLongVolume", 200, 100, 1000, 100));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 1, 1,5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 1, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 6, 1, 30,1));
        LogicPropTable.Add("OpenGap", new Property("OpenGap", 2, 1, 4, 1));
        LogicPropTable.Add("GapHighCurrent", new Property("GapHighCurrent", 2, 2, 5, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 3, 1, 30, 1));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", -3, -8, -2, 1));
        
    }

   
    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[6];
        col[0] = new DataColumn("datetime", Type.GetType("System.DateTime")); //eg. 9:45 
        col[1] = new DataColumn("status", Type.GetType("System.String"));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", Type.GetType("System.Double")); //price
        col[3] = new DataColumn("openshort", Type.GetType("System.Double"));//price
        col[4] = new DataColumn("closelong", Type.GetType("System.Double"));//price
        col[5] = new DataColumn("closeshort", Type.GetType("System.Double"));//price
        
        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }
    public override DataRow FeedTickTrade(DataRow resamplingtick,string portstatus,double onhandcost,double onhandvol)
    {

        //Add a resampling tick to HistoricalTable
        //HistoricalTable.Rows.Add(resamplingtick.ItemArray);
        
        if (portstatus == "none")
        {
            double longprice = this.OpenLongStatus(onhandcost);
            double shortprice = this.OpenShortStatus(onhandcost);
            //Not hit any condition
            if ((longprice == 0)&&(shortprice ==0))
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if ((longprice > 0) && (shortprice > 0))
                this.SignalTable.Rows.Add(this.GetSingnal("dup", longprice).ItemArray);//longprice = shortprice
            else if (longprice > 0) //if Long
                this.SignalTable.Rows.Add(this.GetSingnal("openlong", longprice).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("openshort", shortprice).ItemArray);   
        }
        else if (portstatus == "closelong")
        {
            double shortprice = this.CloseLongStatus(onhandcost);
            //Not hit any condition
            if (shortprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closelong", shortprice).ItemArray);
        }
        else if (portstatus == "closeshort")
        {
            double longprice = this.CloseShortStatus(onhandcost);
            //Not hit any condition
            if (longprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (longprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closeshort", longprice).ItemArray);
        }
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }
    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
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
    
    public override double OpenLongStatus(double onhandcost)
    {
        
        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;

        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        double opengap = this.LogicPropTable["GapHighCurrent"].Value;
        //double opengap = 0;
        double close = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];

        
        double diff = 0;
        double high = 0;
        double tlongvol = 0;
        double price = 0;//not hit any condition
        if (counthistrow >= monitor)
        {
            found = 0;
            //Check total long volume > x
            for (int k = 1; k < monitor; k++)
            {
                tlongvol = 0;
                if (!HistoricalTable.Rows[counthistrow - k].IsNull("TotalLongVolume"))
                    tlongvol = (Double)this.HistoricalTable.Rows[counthistrow - k]["TotalLongVolume"];
                if (tlongvol >= this.LogicPropTable["TotalLongVolume"].Value)
                    found++;
                
                
                double tmphigh = (Double)this.HistoricalTable.Rows[counthistrow - k]["High"];
                if (tmphigh > high)
                {
                    high = tmphigh;
                }

            }
            //check open price must lower than monitor period high
            //Check how many time hit condition
            //diff = high - close;
            diff = opengap + 1;
            if ((found >= this.LogicPropTable["SignalLong"].Value)&&(diff > opengap))
            {
                price = close;
            }
        }
        return price;
    }
    public override double OpenShortStatus(double onhandcost)
    {
        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        
        int found = 0;

        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        double opengap = this.LogicPropTable["OpenGap"].Value;
        //double opengap = 0;
        double close = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];

        double diff = 0;
        double low = 0;
        double tshortvol = 0;
        
        double price = 0;//not hit any condition
        if (counthistrow >= monitor)
        {
            //Check total short volume > x
            found = 0;
            for (int k = 1; k < monitor; k++)
            {
                tshortvol = 0;
                if (!HistoricalTable.Rows[counthistrow - k].IsNull("TotalShortVolume"))
                    tshortvol = -1 * (Double)this.HistoricalTable.Rows[counthistrow - k]["TotalShortVolume"];//totalshortvolume is negative
                if (tshortvol >= this.LogicPropTable["TotalShortVolume"].Value)
                    found++;
                
                //check monitor period low
                double tmplow = (Double)this.HistoricalTable.Rows[counthistrow - k]["Low"];
                if (tmplow > low)
                {
                    low = tmplow;
                }
            }
            //check open price must lower than monitor period high
            //Check how many time hit condition
            //diff = close - low;
            diff = opengap + 1;
            if ((found >= this.LogicPropTable["SignalShort"].Value) && (diff > opengap))
            {
                price = close;
            }
        }
        return price;
    }
    public override double CloseLongStatus(double onhandcost)
    {
        double price = 0;
        double stoplossprice = this.CloseStopLoss(onhandcost, "openlong");
        if (stoplossprice == 0)
        {
            //price = this.CloseLongCheckGap();
            price = this.OpenShortStatus(onhandcost);
        }
        else price = stoplossprice; //Stop loss
        return price;
    }
    
    public override double CloseShortStatus(double onhandcost)
    {
        double price = 0;
        double stoplossprice = this.CloseStopLoss(onhandcost, "openshort");
        if (stoplossprice == 0)
        {
            //price = this.CloseShortCheckGap();
            price = this.OpenLongStatus(onhandcost);
        }
        else price = stoplossprice; //Stop loss
        return price;    
    }
    private double CloseStopLoss(double onhandcost, string positionside)
    {
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stoploss = this.LogicPropTable["StopLoss"].Value;
        double stoplossprice = 0;
        double close = 0;
        double profit = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
        if (positionside == "openlong")
            profit = close - onhandcost;
        else if (positionside == "openshort")
            profit = onhandcost - close;
        if (profit < stoploss)
            stoplossprice = close;
        return stoplossprice;
    }
    private double CloseLongCheckGap()
    {
        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        double monitor = this.LogicPropTable["CloseMonitorPeriod"].Value;
        double close = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];

        double highprice = 0;

        double price = 0;//not hit any condition
        if (counthistrow >= monitor)
        {
            //Check gap between High and current close price
            for (int k = 1; k < monitor; k++)
            {
                highprice = Double.Parse(HistoricalTable.Rows[counthistrow - k]["High"].ToString());
                double gap = highprice - close;
                if (gap > this.LogicPropTable["GapHighCurrent"].Value)
                {
                    price = close;
                    break;
                }
            }
        }
        return price;
    }
    private double CloseShortCheckGap()
    {
        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        double monitor = this.LogicPropTable["CloseMonitorPeriod"].Value;
        double close = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];

        double lowprice = 0;

        double price = 0;//not hit any condition
        if (counthistrow >= monitor)
        {
            //Check gap between High and current close price
            for (int k = 1; k < monitor; k++)
            {
                lowprice = Double.Parse(HistoricalTable.Rows[counthistrow - k]["Low"].ToString());
                double gap = close - lowprice;
                if (gap > this.LogicPropTable["GapHighCurrent"].Value)
                {
                    price = close;
                    break;
                }
            }
        }
        return price;
    }

}
*/


class HighVolumeTradeLogic : TradeLogic
{
    public HighVolumeTradeLogic(DataTable newResamplingTable,int interval)
    {
        //Update historical table
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.InitLogicPropTable(interval);

    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume", 700, 100, 1000, 100));
        LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", -700, -1000, 100, 100));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 1, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 1, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("OpenGap", new Property("OpenGap", 2, 1, 4, 1));
        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 20, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -20, -500,-40, 50));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", -1.8, -8, -2, 1));
        double openstarttimetrade = TimeSpan.Parse("16:05:00").TotalSeconds;
        double openendtimetrade = TimeSpan.Parse("16:30:00").TotalSeconds;
        double endtimetrade = TimeSpan.Parse("16:54:50").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));
        
    }
    

    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[6];
        col[0] = new DataColumn("datetime", Type.GetType("System.DateTime")); //eg. 9:45 
        col[1] = new DataColumn("status", Type.GetType("System.String"));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", Type.GetType("System.Double")); //price
        col[3] = new DataColumn("openshort", Type.GetType("System.Double"));//price
        col[4] = new DataColumn("closelong", Type.GetType("System.Double"));//price
        col[5] = new DataColumn("closeshort", Type.GetType("System.Double"));//price

        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }
    public override DataRow FeedTickTrade(DataRow resamplingtick, string portstatus, double onhandcost, double onhandvol)
    {

        //Add a resampling tick to HistoricalTable
        //HistoricalTable.Rows.Add(resamplingtick.ItemArray);

        if (portstatus == "none")
        {
            double longprice = this.OpenLongStatus(onhandcost);
            double shortprice = this.OpenShortStatus(onhandcost);
            //Not hit any condition
            if ((longprice == 0) && (shortprice == 0))
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if ((longprice > 0) && (shortprice > 0))
                this.SignalTable.Rows.Add(this.GetSingnal("dup", longprice).ItemArray);//longprice = shortprice
            else if (longprice > 0) //if Long
                this.SignalTable.Rows.Add(this.GetSingnal("openlong", longprice).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("openshort", shortprice).ItemArray);
        }
        else if (portstatus == "closelong")
        {
            double shortprice = this.CloseLongStatus(onhandcost);
            //Not hit any condition
            if (shortprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closelong", shortprice).ItemArray);
        }
        else if (portstatus == "closeshort")
        {
            double longprice = this.CloseShortStatus(onhandcost);
            //Not hit any condition
            if (longprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (longprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closeshort", longprice).ItemArray);
        }
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }
    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
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

    public override double OpenLongStatus(double onhandcost)
    {

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;

        

        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        double close = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];

        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        double tlongvol = 0;
        double maxlongvol = 0;
        double price = 0;//not hit any condition


        
        if ((counthistrow >= monitor)&&(time>=openstarttime)&&(time<=openendtime))
        {
            found = 0;
            //Check total long volume > x
            for (int k = 2; k <= monitor+1; k++)
            {
                tlongvol = 0;
                maxlongvol = 0;
                if (!HistoricalTable.Rows[counthistrow- k].IsNull("TotalLongVolume"))
                    tlongvol = (Double)this.HistoricalTable.Rows[counthistrow- k]["TotalLongVolume"];
                if (!HistoricalTable.Rows[counthistrow - k].IsNull("MaxLongVolume"))
                    maxlongvol = (Double)this.HistoricalTable.Rows[counthistrow - k]["MaxLongVolume"];
                if ((tlongvol > this.LogicPropTable["TotalLongVolume"].Value) && (maxlongvol > this.LogicPropTable["MaxLongVolume"].Value))
                    found++;

            }
            //Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = close;
            }
        }
        return price;
    }
    public override double OpenShortStatus(double onhandcost)
    {
        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        double close = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];

        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        int found = 0;
        double tshortvol = 0;
        double maxshortvol = 0;
        double price = 0;//not hit any condition

        if ((counthistrow >= monitor) && (time >= openstarttime) && (time <= openendtime))
        {
            //Check total short volume > x
            found = 0;
            for (int k = 2; k <= monitor+1; k++)
            {
                tshortvol = 0;
                if (!HistoricalTable.Rows[counthistrow - k].IsNull("TotalShortVolume"))
                    tshortvol = (Double)this.HistoricalTable.Rows[counthistrow - k]["TotalShortVolume"];
                if (!HistoricalTable.Rows[counthistrow - k].IsNull("MaxShortVolume"))
                    maxshortvol = (Double)this.HistoricalTable.Rows[counthistrow - k]["MaxShortVolume"];
                if ((tshortvol < this.LogicPropTable["TotalShortVolume"].Value) && (maxshortvol < this.LogicPropTable["MaxShortVolume"].Value))
                    found++;
            }
            //Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalShort"].Value))
            {
                price = close;
            }
        }
        return price;
    }

    public override double CloseLongStatus(double onhandcost)
    {
        double price = 0;
        double stoplossprice = this.CloseStopLoss(onhandcost, "openlong");
        if (stoplossprice == 0)
        {
            //price = this.CloseLongCheckGap();
            //price = this.OpenShortStatus(onhandcost);
            int counthistrow = this.HistoricalTable.Rows.Count;
            double close=(Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow-1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }
        else price = stoplossprice; //Stop loss
        return price;
    }

    public override double CloseShortStatus(double onhandcost)
    {
        double price = 0;
        double stoplossprice = this.CloseStopLoss(onhandcost, "openshort");
        if (stoplossprice == 0)
        {
            int counthistrow = this.HistoricalTable.Rows.Count;
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }
        else price = stoplossprice; //Stop loss
        return price;
    }
    private double CloseStopLoss(double onhandcost, string positionside)
    {
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stoploss = this.LogicPropTable["StopLoss"].Value;
        double stoplossprice = 0;
        double close = 0;
        double profit = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
        if (positionside == "openlong")
            profit = close - onhandcost;
        else if (positionside == "openshort")
            profit = onhandcost - close;
        if (profit < stoploss)
            stoplossprice = close;
        return stoplossprice;
    }
}

class PriceNewHighLowTradeLogic : TradeLogic
{
    public PriceNewHighLowTradeLogic(DataTable newResamplingTable, int interval)
    {
        //Update historical table
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.InitLogicPropTable(interval);

    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("ClosePriceGap", new Property("ClosePriceGap", 0, 1, 20, 1));
        LogicPropTable.Add("OpenPriceGap", new Property("OpenePriceGap", 0, 1, 20, 1));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume", 700, -1000, 100, 100));
        LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", -700, -1000, 100, 100));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 1, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 1, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 30, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -30, -500, -40, 50));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", -1.8, -8, -2, 1));
        double openstarttimetrade = TimeSpan.Parse("16:05:00").TotalSeconds;
        double openendtimetrade = TimeSpan.Parse("16:30:00").TotalSeconds;
        double endtimetrade = TimeSpan.Parse("16:54:45").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));

    }


    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[6];
        col[0] = new DataColumn("datetime", Type.GetType("System.DateTime")); //eg. 9:45 
        col[1] = new DataColumn("status", Type.GetType("System.String"));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", Type.GetType("System.Double")); //price
        col[3] = new DataColumn("openshort", Type.GetType("System.Double"));//price
        col[4] = new DataColumn("closelong", Type.GetType("System.Double"));//price
        col[5] = new DataColumn("closeshort", Type.GetType("System.Double"));//price

        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }
    public override DataRow FeedTickTrade(DataRow resamplingtick, string portstatus, double onhandcost, double onhandvol)
    {

        //Add a resampling tick to HistoricalTable
        //HistoricalTable.Rows.Add(resamplingtick.ItemArray);

        if (portstatus == "none")
        {
            double longprice = this.OpenLongStatus(onhandcost);
            double shortprice = this.OpenShortStatus(onhandcost);
            //Not hit any condition
            if ((longprice == 0) && (shortprice == 0))
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if ((longprice > 0) && (shortprice > 0))
                this.SignalTable.Rows.Add(this.GetSingnal("dup", longprice).ItemArray);//longprice = shortprice
            else if (longprice > 0) //if Long
                this.SignalTable.Rows.Add(this.GetSingnal("openlong", longprice).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("openshort", shortprice).ItemArray);
        }
        else if (portstatus == "closelong")
        {
            double shortprice = this.CloseLongStatus(onhandcost);
            //Not hit any condition
            if (shortprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closelong", shortprice).ItemArray);
        }
        else if (portstatus == "closeshort")
        {
            double longprice = this.CloseShortStatus(onhandcost);
            //Not hit any condition
            if (longprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (longprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closeshort", longprice).ItemArray);
        }
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }
    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
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

    public override double OpenLongStatus(double onhandcost)
    {

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;



        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        double close = 0;
        double newhigh = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
        {
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            newhigh = (Double)this.HistoricalTable.Rows[counthistrow - 1]["DayHigh"];
        }

        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        double tlongvol = 0;
        double tshortvol = 0;
        double maxlongvol = 0;
        double price = 0;//not hit any condition
        double opengap = (Double)this.LogicPropTable["OpenPriceGap"].Value;

        if ((close>0)&&(close >= newhigh-opengap)) 
        {
            if ((counthistrow >= monitor) && (time >= openstarttime) && (time <= openendtime))
            {
                found = 0;
                //Check total long volume > x
                for (int k = 2; k <= monitor + 1; k++)
                {
                    tlongvol = 0;
                    tshortvol = 0;
                    maxlongvol = 0;
                    if (!HistoricalTable.Rows[counthistrow - k].IsNull("TotalLongVolume"))
                    {
                        tshortvol = (Double)this.HistoricalTable.Rows[counthistrow - k]["TotalShortVolume"];
                        tlongvol = (Double)this.HistoricalTable.Rows[counthistrow - k]["TotalLongVolume"];
                    }
                    if (!HistoricalTable.Rows[counthistrow - k].IsNull("MaxLongVolume"))
                        maxlongvol = (Double)this.HistoricalTable.Rows[counthistrow - k]["MaxLongVolume"];
                    //Check Total Long Volume and New High/Low
                    if ((tlongvol > this.LogicPropTable["TotalLongVolume"].Value) && (maxlongvol > this.LogicPropTable["MaxLongVolume"].Value) )
                        found++;

                }
                //Check how many time hit condition
                if ((found >= this.LogicPropTable["SignalLong"].Value))
                {
                    price = close;
                }
            }
        }
        return price;
    }
    public override double OpenShortStatus(double onhandcost)
    {
        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        double close = 0;
        double newlow = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
        {
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            newlow = (Double)this.HistoricalTable.Rows[counthistrow - 1]["DayLow"];
        }

        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        int found = 0;
        double tshortvol = 0;
        double tlongvol = 0;
        double maxshortvol = 0;
        double price = 0;//not hit any condition
        double opengap = (Double)this.LogicPropTable["OpenPriceGap"].Value;

        if ((close > 0) && (close <= newlow+opengap))
        {
            if ((counthistrow >= monitor) && (time >= openstarttime) && (time <= openendtime))
            {
                //Check total short volume > x
                found = 0;
                for (int k = 2; k <= monitor + 1; k++)
                {
                    tshortvol = 0;
                    tlongvol = 0;
                    maxshortvol = 0;
                    if (!HistoricalTable.Rows[counthistrow - k].IsNull("TotalShortVolume"))
                    {
                        tshortvol = (Double)this.HistoricalTable.Rows[counthistrow - k]["TotalShortVolume"];
                        tlongvol = (Double)this.HistoricalTable.Rows[counthistrow - k]["TotalLongVolume"];
                    }
                    if (!HistoricalTable.Rows[counthistrow - k].IsNull("MaxShortVolume"))
                        maxshortvol = (Double)this.HistoricalTable.Rows[counthistrow - k]["MaxShortVolume"];
                    if ((tshortvol < this.LogicPropTable["TotalShortVolume"].Value) && (maxshortvol < this.LogicPropTable["MaxShortVolume"].Value))
                        found++;
                }
                //Check how many time hit condition
                if ((found >= this.LogicPropTable["SignalShort"].Value))
                {
                    price = close;
                }
            }
        }
        return price;
    }



    public override double CloseLongStatus(double onhandcost)
    {
        double price = 0;
        double stoplossprice = this.CloseStopLoss(onhandcost, "openlong");
        if (stoplossprice == 0)
        {
            //price = this.CloseLongCheckGap();
            //price = this.OpenShortStatus(onhandcost);
            int counthistrow = this.HistoricalTable.Rows.Count;
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            double dayhigh = (Double)this.HistoricalTable.Rows[counthistrow - 1]["DayHigh"];
            double closepricegap = (Double)this.LogicPropTable["ClosePriceGap"].Value;
            //double pricegap = dayhigh - close;
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            
            
            if ((time >= endtime))
                price = close;
        }
        else price = stoplossprice; //Stop loss
        return price;
    }

    public override double CloseShortStatus(double onhandcost)
    {
        double price = 0;
        double stoplossprice = this.CloseStopLoss(onhandcost, "openshort");
        if (stoplossprice == 0)
        {
            int counthistrow = this.HistoricalTable.Rows.Count;
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            double daylow = (Double)this.HistoricalTable.Rows[counthistrow - 1]["DayLow"];
            double closepricegap = (Double)this.LogicPropTable["ClosePriceGap"].Value;
            //double pricegap = close-daylow;
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if ((time >= endtime))
                price = close;
        }
        else price = stoplossprice; //Stop loss
        return price;
    }

    private double CloseStopLoss(double onhandcost, string positionside)
    {
        int counthistrow = this.HistoricalTable.Rows.Count;
        double stoploss = this.LogicPropTable["StopLoss"].Value;
        double stoplossprice = 0;
        double close = 0;
        double profit = 0;
        if (!HistoricalTable.Rows[counthistrow - 1].IsNull("Close"))
            close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
        if (positionside == "openlong")
            profit = close - onhandcost;
        else if (positionside == "openshort")
            profit = onhandcost - close;
        if (profit < stoploss)
            stoplossprice = close;
        return stoplossprice;
    }
}

class ReboundHighLowTradeLogic : TradeLogic
{
    public ReboundHighLowTradeLogic(DataTable newResamplingTable, int interval)
    {
        //Update historical table
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.GlobalVarTable = new Dictionary<String, String>();
        this.InitLogicPropTable(interval);
        

    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume", 700, 100, 1000, 100));
        LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", -700, -1000, 100, 100));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 1, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 1, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("HighLowGap", new Property("HighLowGap", 2.5, 1, 10, 1));
        LogicPropTable.Add("OpenSignalGap", new Property("OpenSignalGap", 0, 1, 10, 1));
        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 20, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -20, -500, -40, 50));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", 0, -8, -2, 1));
        LogicPropTable.Add("StopGain", new Property("StopGain", 0, -8, -2, 1));
        LogicPropTable.Add("StopProfitLoss", new Property("StopProfitLoss", 0, -8, -2, 1));
        double openstarttimetrade = TimeSpan.Parse("14:35:00").TotalSeconds;
        double openendtimetrade = TimeSpan.Parse("16:05:00").TotalSeconds;
        double endtimetrade = TimeSpan.Parse("16:54:30").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));
        this.GlobalVarTable.Clear();
        this.InitVarTable();
    }
    public void InitVarTable()
    {
        this.GlobalVarTable.Add("Lower High-Gap","0");
        this.GlobalVarTable.Add("BreakHigh", "0");
        this.GlobalVarTable.Add("Higher Low+Gap", "0");
        this.GlobalVarTable.Add("BreakLow", "0");
        this.GlobalVarTable.Add("PrevProfit", "0");
    }
    public void ClearVarTable()
    {
        this.GlobalVarTable["Lower High-Gap"]="0";
        this.GlobalVarTable["BreakHigh"]="0";
        this.GlobalVarTable["Higher Low+Gap"]="0";
        this.GlobalVarTable["BreakLow"]="0";
    }
    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[6];
        col[0] = new DataColumn("datetime", Type.GetType("System.DateTime")); //eg. 9:45 
        col[1] = new DataColumn("status", Type.GetType("System.String"));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", Type.GetType("System.Double")); //price
        col[3] = new DataColumn("openshort", Type.GetType("System.Double"));//price
        col[4] = new DataColumn("closelong", Type.GetType("System.Double"));//price
        col[5] = new DataColumn("closeshort", Type.GetType("System.Double"));//price

        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }
    public override DataRow FeedTickTrade(DataRow resamplingtick, string portstatus, double onhandcost, double onhandvol)
    {

        //Add a resampling tick to HistoricalTable
        //HistoricalTable.Rows.Add(resamplingtick.ItemArray);

        if (portstatus == "none")
        {
            double longprice = this.OpenLongStatus(onhandcost);
            double shortprice = this.OpenShortStatus(onhandcost);
            //Not hit any condition
            if ((longprice == 0) && (shortprice == 0))
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if ((longprice > 0) && (shortprice > 0))
                this.SignalTable.Rows.Add(this.GetSingnal("dup", longprice).ItemArray);//longprice = shortprice
            else if (longprice > 0) //if Long
                this.SignalTable.Rows.Add(this.GetSingnal("openlong", longprice).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("openshort", shortprice).ItemArray);
        }
        else if (portstatus == "closelong")
        {
            double shortprice = this.CloseLongStatus(onhandcost);
            //Not hit any condition
            if (shortprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closelong", shortprice).ItemArray);
        }
        else if (portstatus == "closeshort")
        {
            double longprice = this.CloseShortStatus(onhandcost);
            //Not hit any condition
            if (longprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (longprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closeshort", longprice).ItemArray);
        }
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }
    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
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

    public override double OpenShortStatus(double onhandcost)
    {

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);
        

        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
        
        double dayhigh = 0;
        double periodlow = 0;
        double hlgap = this.LogicPropTable["HighLowGap"].Value; ;
        double opensingalgap = this.LogicPropTable["OpenSignalGap"].Value;
        double lowfromhighgap = Double.Parse(this.GlobalVarTable["Lower High-Gap"]);
        double breakhigh = Double.Parse(this.GlobalVarTable["BreakHigh"]);

        if ((counthistrow >= monitor) && (time >= openstarttime) && (time <= openendtime))
        {
            found = 0;
            //Check total long volume > x
            for (int k = 2; k <= monitor + 1; k++)
            {
                if (!HistoricalTable.Rows[counthistrow - k].IsNull("DayHigh"))
                    dayhigh = (Double)this.HistoricalTable.Rows[counthistrow - k]["DayHigh"];
                if (!HistoricalTable.Rows[counthistrow - k].IsNull("Low"))
                    periodlow = (Double)this.HistoricalTable.Rows[counthistrow - k]["Low"];

                //Singal 1 found new Low from High-Gap
                if ((lowfromhighgap == 0) && (periodlow < (dayhigh - hlgap)))//fisrt lower
                    this.GlobalVarTable["Lower High-Gap"] = periodlow.ToString();
                else if ((lowfromhighgap > 0) && (periodlow < lowfromhighgap))//new lower
                    this.GlobalVarTable["Lower High-Gap"] = periodlow.ToString();
                
                //Singal 2 Rebound almost same dayhigh (lowerfromhighgap >0)
                if (lowfromhighgap > 0)
                {
                    if (close >= (dayhigh - opensingalgap))
                    {
                        this.GlobalVarTable["BreakHigh"] = dayhigh.ToString();
                        //found++;
                    }
                }
                //Signal 3 Fall after breakhigh
                if (breakhigh > 0)
                {
                    if(close <= lowfromhighgap)
                        found++;
                }
            }
            //Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = close;
            }
        }
        return price;
    }
    public override double OpenLongStatus(double onhandcost)
    {
        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);


        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];

        double daylow = 0;
        double periodhigh = 0;
        double hlgap = this.LogicPropTable["HighLowGap"].Value; ;
        double opensingalgap = this.LogicPropTable["OpenSignalGap"].Value;
        double highfromlowgap = Double.Parse(this.GlobalVarTable["Higher Low+Gap"]);
        double breaklow = Double.Parse(this.GlobalVarTable["BreakLow"]);

        if ((counthistrow >= monitor) && (time >= openstarttime) && (time <= openendtime))
        {
            //Check total short volume > x
            found = 0;
            for (int k = 2; k <= monitor + 1; k++)
            {
                if (!HistoricalTable.Rows[counthistrow - k].IsNull("DayLow"))
                    daylow = (Double)this.HistoricalTable.Rows[counthistrow - k]["DayLow"];
                if (!HistoricalTable.Rows[counthistrow - k].IsNull("High"))
                    periodhigh = (Double)this.HistoricalTable.Rows[counthistrow - k]["High"];

                //Singal 1 found new Low from High-Gap
                if ((highfromlowgap == 0) && (periodhigh > (daylow + hlgap)))//fisrt higher
                    this.GlobalVarTable["Higher Low+Gap"] = periodhigh.ToString();
                else if ((highfromlowgap > 0) && (periodhigh > highfromlowgap))//new higher
                    this.GlobalVarTable["Higher Low+Gap"] = periodhigh.ToString();

                //Singal 2 Rebound almost same dayhigh (lowerfromhighgap >0)
                if (highfromlowgap > 0)
                {
                    if (close <= (daylow + opensingalgap))
                    {
                        //found++;
                        this.GlobalVarTable["BreakLow"] = daylow.ToString();
                    }
                }
                //Signal 3 Fall after breakhigh
                if (breaklow > 0)
                {
                    if (close >= highfromlowgap)
                        found++;
                }
            }
            //Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalShort"].Value))
            {
                price = close;
            }
        }
        return price;
    }

    public override double CloseLongStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }
        
        if(price>0)
            this.ClearVarTable();
        
        return price;
    }
    public override double CloseShortStatus(double onhandcost)
    {
        double price = 0;
        double stoplossprice = this.CloseStopLoss(onhandcost, "openshort");
        double stopgainprice = this.CloseStopGain(onhandcost, "openshort");
        double stopprofitlossprice =this.CloseStopProfitLoss(onhandcost, "openshort");
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }
        if(price>0)
            this.ClearVarTable();
        return price;
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
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit < stoploss)
                stoplossprice = close;
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
                stopgainprice = close;
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

        if ((stopprofitloss != 0)&&(profit>0)&&(prevprofit>0))
        {
            profitdiff = profit - prevprofit;
            if (profitdiff < stopprofitloss)
                stopprofitlossprice = close;
        }
        return stopprofitlossprice;

    }
}

class DoubleHighLowTradeLogic : TradeLogic
{
    public DoubleHighLowTradeLogic(DataTable newResamplingTable, int interval)
    {
        //Update historical table
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.GlobalVarTable = new Dictionary<String, String>();
        this.InitLogicPropTable(interval);


    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume", 700, 100, 1000, 100));
        LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", -700, -1000, 100, 100));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 2, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 2, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("HighLowGap", new Property("HighLowGap", 4, 1, 10, 1));
        LogicPropTable.Add("OpenSignalGap", new Property("OpenSignalGap", 0, 1, 10, 1));
        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 20, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -20, -500, -40, 50));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", 0, -8, -2, 1));
        LogicPropTable.Add("StopGain", new Property("StopGain", 0, -8, -2, 1));
        LogicPropTable.Add("StopProfitLoss", new Property("StopProfitLoss", 0, -8, -2, 1));
        double openstarttimetrade = TimeSpan.Parse("09:50:00").TotalSeconds;
        double openendtimetrade = TimeSpan.Parse("16:05:00").TotalSeconds;
        double endtimetrade = TimeSpan.Parse("16:52:00").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));
        this.GlobalVarTable.Clear();
        this.InitVarTable();
    }
    public void InitVarTable()
    {
        this.GlobalVarTable.Add("Lower High-Gap", "0");
        this.GlobalVarTable.Add("BreakHigh", "0");
        this.GlobalVarTable.Add("Higher Low+Gap", "0");
        this.GlobalVarTable.Add("BreakLow", "0");
        this.GlobalVarTable.Add("PrevProfit", "0");
    }
    public void ClearVarTable()
    {
        this.GlobalVarTable["Lower High-Gap"] = "0";
        this.GlobalVarTable["BreakHigh"] = "0";
        this.GlobalVarTable["Higher Low+Gap"] = "0";
        this.GlobalVarTable["BreakLow"] = "0";
    }
    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[6];
        col[0] = new DataColumn("datetime", Type.GetType("System.DateTime")); //eg. 9:45 
        col[1] = new DataColumn("status", Type.GetType("System.String"));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", Type.GetType("System.Double")); //price
        col[3] = new DataColumn("openshort", Type.GetType("System.Double"));//price
        col[4] = new DataColumn("closelong", Type.GetType("System.Double"));//price
        col[5] = new DataColumn("closeshort", Type.GetType("System.Double"));//price

        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }
    public override DataRow FeedTickTrade(DataRow resamplingtick, string portstatus, double onhandcost, double onhandvol)
    {

        //Add a resampling tick to HistoricalTable
        //HistoricalTable.Rows.Add(resamplingtick.ItemArray);

        if (portstatus == "none")
        {
            double longprice = this.OpenLongStatus(onhandcost);
            double shortprice = this.OpenShortStatus(onhandcost);
            //Not hit any condition
            if ((longprice == 0) && (shortprice == 0))
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if ((longprice > 0) && (shortprice > 0))
                this.SignalTable.Rows.Add(this.GetSingnal("dup", longprice).ItemArray);//longprice = shortprice
            else if (longprice > 0) //if Long
                this.SignalTable.Rows.Add(this.GetSingnal("openlong", longprice).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("openshort", shortprice).ItemArray);
        }
        else if (portstatus == "closelong")
        {
            double shortprice = this.CloseLongStatus(onhandcost);
            //Not hit any condition
            if (shortprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closelong", shortprice).ItemArray);
        }
        else if (portstatus == "closeshort")
        {
            double longprice = this.CloseShortStatus(onhandcost);
            //Not hit any condition
            if (longprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (longprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closeshort", longprice).ItemArray);
        }
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }
    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
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

    public override double OpenLongStatus(double onhandcost)
    {

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);


        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];

        double dayhigh = 0;
        double periodlow = 0;
        double hlgap = this.LogicPropTable["HighLowGap"].Value; ;
        double opensingalgap = this.LogicPropTable["OpenSignalGap"].Value;
        double lowfromhighgap = Double.Parse(this.GlobalVarTable["Lower High-Gap"]);
        double breakhigh = Double.Parse(this.GlobalVarTable["BreakHigh"]);

        if ((counthistrow >= monitor) && (time >= openstarttime) && (time <= openendtime))
        {
            found = 0;
            //Check total long volume > x
            for (int k = 2; k <= monitor + 1; k++)
            {
                if (!HistoricalTable.Rows[counthistrow - k].IsNull("DayHigh"))
                    dayhigh = (Double)this.HistoricalTable.Rows[counthistrow - k]["DayHigh"];
                if (!HistoricalTable.Rows[counthistrow - k].IsNull("Low"))
                    periodlow = (Double)this.HistoricalTable.Rows[counthistrow - k]["Low"];

                //Singal 1 found new Low from High-Gap
                if ((lowfromhighgap == 0) && (periodlow < (dayhigh - hlgap)))//fisrt lower
                    this.GlobalVarTable["Lower High-Gap"] = periodlow.ToString();
                else if ((lowfromhighgap > 0) && (periodlow < lowfromhighgap))//new lower
                    this.GlobalVarTable["Lower High-Gap"] = periodlow.ToString();

                //Singal 2 Rebound almost same dayhigh (lowerfromhighgap >0)
                if (lowfromhighgap > 0)
                {
                    if (close >= (dayhigh - opensingalgap))
                    {
                        this.GlobalVarTable["BreakHigh"] = dayhigh.ToString();
                        //found++;
                    }
                }
                //Signal 3 Fall after breakhigh
                if (breakhigh > 0)
                {
                    found++;
                    if ((close < dayhigh)&&(periodlow>lowfromhighgap))//condition for reset break high and wait for another break
                    {
                        this.GlobalVarTable["BreakHigh"] = "0";
                    }
                    //if ((periodlow > lowfromhighgap))
                    //    found++;
                }
            }
            //Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = close;
            }
        }
        return price;
    }
    public override double OpenShortStatus(double onhandcost)
    {
        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);


        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];

        double daylow = 0;
        double periodhigh = 0;
        double hlgap = this.LogicPropTable["HighLowGap"].Value; ;
        double opensingalgap = this.LogicPropTable["OpenSignalGap"].Value;
        double highfromlowgap = Double.Parse(this.GlobalVarTable["Higher Low+Gap"]);
        double breaklow = Double.Parse(this.GlobalVarTable["BreakLow"]);

        if ((counthistrow >= monitor) && (time >= openstarttime) && (time <= openendtime))
        {
            //Check total short volume > x
            found = 0;
            for (int k = 2; k <= monitor + 1; k++)
            {
                if (!HistoricalTable.Rows[counthistrow - k].IsNull("DayLow"))
                    daylow = (Double)this.HistoricalTable.Rows[counthistrow - k]["DayLow"];
                if (!HistoricalTable.Rows[counthistrow - k].IsNull("High"))
                    periodhigh = (Double)this.HistoricalTable.Rows[counthistrow - k]["High"];

                //Singal 1 found new Low from High-Gap
                if ((highfromlowgap == 0) && (periodhigh > (daylow + hlgap)))//fisrt higher
                    this.GlobalVarTable["Higher Low+Gap"] = periodhigh.ToString();
                else if ((highfromlowgap > 0) && (periodhigh > highfromlowgap))//new higher
                    this.GlobalVarTable["Higher Low+Gap"] = periodhigh.ToString();

                //Singal 2 Rebound almost same dayhigh (lowerfromhighgap >0)
                if (highfromlowgap > 0)
                {
                    if (close <= (daylow + opensingalgap))
                    {
                        //found++;
                        this.GlobalVarTable["BreakLow"] = daylow.ToString();
                    }
                }
                //Signal 3 Fall after breakhigh
                if (breaklow > 0)
                {
                    found++;
                    if ((close > daylow) && (periodhigh < highfromlowgap))//condition for reset break high and wait for another break
                    {
                        this.GlobalVarTable["BreakLow"] = "0";
                    }
                    
                    //if ((periodhigh <highfromlowgap))
                    //    found++;
                }
            }
            //Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalShort"].Value))
            {
                price = close;
            }
        }
        return price;
    }

    public override double CloseLongStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }

        if (price > 0)
            this.ClearVarTable();

        return price;
    }
    public override double CloseShortStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }
        if (price > 0)
            this.ClearVarTable();
        return price;
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
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit < stoploss)
                stoplossprice = close;
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
                stopgainprice = close;
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
}

class OpenAfternoonTradeLogic : TradeLogic
{
    public OpenAfternoonTradeLogic(DataTable newResamplingTable, int interval)
    {
        //Update historical table
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.GlobalVarTable = new Dictionary<String, String>();
        this.InitLogicPropTable(interval);


    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume", 700, 100, 1000, 100));
        LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", -700, -1000, 100, 100));
        double targettimetrade = TimeSpan.Parse("14:15:00").TotalSeconds;
        LogicPropTable.Add("TargetTimeTrade", new Property("TargetTimeTrade", targettimetrade, targettimetrade, targettimetrade, 0));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 1, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 1, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("TargetGap", new Property("TargetGap", 3.5, 1, 10, 1));
        LogicPropTable.Add("OpenSignalGap", new Property("OpenSignalGap", 0, 1, 10, 1));
        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 20, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -20, -500, -40, 50));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", -2, -8, -2, 1));
        LogicPropTable.Add("StopGain", new Property("StopGain", 0, -8, -2, 1));
        LogicPropTable.Add("StopProfitLoss", new Property("StopProfitLoss", 0, -8, -2, 1));
        double openstarttimetrade = TimeSpan.Parse("12:00:00").TotalSeconds;
        double openendtimetrade = TimeSpan.Parse("14:30:00").TotalSeconds;
        double endtimetrade = TimeSpan.Parse("16:05:30").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));
        this.GlobalVarTable.Clear();
        this.InitVarTable();
    }
    public void InitVarTable()
    {
        this.GlobalVarTable.Add("Lower High-Gap", "0");
        this.GlobalVarTable.Add("BreakHigh", "0");
        this.GlobalVarTable.Add("Higher Low+Gap", "0");
        this.GlobalVarTable.Add("BreakLow", "0");
        this.GlobalVarTable.Add("PrevProfit", "0");
    }
    public void ClearVarTable()
    {
        this.GlobalVarTable["Lower High-Gap"] = "0";
        this.GlobalVarTable["BreakHigh"] = "0";
        this.GlobalVarTable["Higher Low+Gap"] = "0";
        this.GlobalVarTable["BreakLow"] = "0";
    }
    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[6];
        col[0] = new DataColumn("datetime", Type.GetType("System.DateTime")); //eg. 9:45 
        col[1] = new DataColumn("status", Type.GetType("System.String"));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", Type.GetType("System.Double")); //price
        col[3] = new DataColumn("openshort", Type.GetType("System.Double"));//price
        col[4] = new DataColumn("closelong", Type.GetType("System.Double"));//price
        col[5] = new DataColumn("closeshort", Type.GetType("System.Double"));//price

        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }
    public override DataRow FeedTickTrade(DataRow resamplingtick, string portstatus, double onhandcost, double onhandvol)
    {

        //Add a resampling tick to HistoricalTable
        //HistoricalTable.Rows.Add(resamplingtick.ItemArray);

        if (portstatus == "none")
        {
            double longprice = this.OpenLongStatus(onhandcost);
            double shortprice = this.OpenShortStatus(onhandcost);
            //Not hit any condition
            if ((longprice == 0) && (shortprice == 0))
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if ((longprice > 0) && (shortprice > 0))
                this.SignalTable.Rows.Add(this.GetSingnal("dup", longprice).ItemArray);//longprice = shortprice
            else if (longprice > 0) //if Long
                this.SignalTable.Rows.Add(this.GetSingnal("openlong", longprice).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("openshort", shortprice).ItemArray);
        }
        else if (portstatus == "closelong")
        {
            double shortprice = this.CloseLongStatus(onhandcost);
            //Not hit any condition
            if (shortprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closelong", shortprice).ItemArray);
        }
        else if (portstatus == "closeshort")
        {
            double longprice = this.CloseShortStatus(onhandcost);
            //Not hit any condition
            if (longprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (longprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closeshort", longprice).ItemArray);
        }
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }
    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
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

    public override double OpenLongStatus(double onhandcost)
    {

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        TimeSpan targettimetrade = TimeSpan.FromSeconds((Double)this.LogicPropTable["TargetTimeTrade"].Value);
        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double high = (Double)this.HistoricalTable.Rows[counthistrow - 1]["High"];
        double low = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Low"];
        double vol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Volume"];
        double tgap = this.LogicPropTable["TargetGap"].Value; ;
       

        if ((counthistrow >= monitor) && (time >= openstarttime) && (time <= openendtime))
        {
            found = 0;
            //Check total long volume > x
            
            for (int k = 2; k <= monitor + 1; k++)
            {
                if (time == targettimetrade)
                {
                    double prevclose = (Double)this.HistoricalTable.Rows[counthistrow - 2]["Close"];
                    double afternoongap = open - prevclose;
                    if (afternoongap > tgap)
                        found++;
                }
                
            }
            //Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = open;
            }
        }
        return price;
    }
    public override double OpenShortStatus(double onhandcost)
    {
        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        TimeSpan targettimetrade = TimeSpan.FromSeconds((Double)this.LogicPropTable["TargetTimeTrade"].Value);
        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double high = (Double)this.HistoricalTable.Rows[counthistrow - 1]["High"];
        double low = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Low"];
        double vol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Volume"];
        double tgap = this.LogicPropTable["TargetGap"].Value; ;

        if ((counthistrow >= monitor) && (time >= openstarttime) && (time <= openendtime))
        {
            //Check total short volume > x
            found = 0;
            for (int k = 2; k <= monitor + 1; k++)
            {
                if (time == targettimetrade)
                {
                    double prevclose = (Double)this.HistoricalTable.Rows[counthistrow - 2]["Close"];
                    double afternoongap = open - prevclose;
                    if (afternoongap < -tgap)
                        found++;
                }
            }
            //Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalShort"].Value))
            {
                price = open;
            }
        }
        return price;
    }

    public override double CloseLongStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }

        if (price > 0)
            this.ClearVarTable();

        return price;
    }
    public override double CloseShortStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }
        if (price > 0)
            this.ClearVarTable();
        return price;
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
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit < stoploss)
                stoplossprice = close;
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
                stopgainprice = close;
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
}

class LongWhenHighVolShortPercentTradeLogic : TradeLogic
{
    public LongWhenHighVolShortPercentTradeLogic(DataTable newResamplingTable, int interval)
    {
        //Update historical table
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.GlobalVarTable = new Dictionary<String, String>();
        this.InitLogicPropTable(interval);


    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume", 700, 100, 1000, 100));
        LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", -700, -1000, 100, 100));
        LogicPropTable.Add("TargetShortVolPercent", new Property("TargetShortVolPercent", 64, 50, 100, 10));
        LogicPropTable.Add("TargetLongVolPercent", new Property("TargetLongVolPercent", 0, 50, 100, 10));
        LogicPropTable.Add("OpenVolPercent", new Property("OpenVolPercent", 62, 0, 100, 10));
        LogicPropTable.Add("MinSumTotalVol", new Property("MinSumTotalVol", 10000, 10000, 50000, 5000));
        //double targettimetrade = TimeSpan.Parse("14:15:00").TotalSeconds;
        //LogicPropTable.Add("TargetTimeTrade", new Property("TargetTimeTrade", targettimetrade, targettimetrade, targettimetrade, 0));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 1, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 1, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("TargetGap", new Property("TargetGap", 3.5, 1, 10, 1));
        
        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 20, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -20, -500, -40, 50));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", 0, -8, -2, 1));
        LogicPropTable.Add("StopGain", new Property("StopGain", 0, -8, -2, 1));
        LogicPropTable.Add("StopProfitLoss", new Property("StopProfitLoss", -2, -8, -2, 1));
        double openstarttimetrade = TimeSpan.Parse("10:15:00").TotalSeconds;
        double openendtimetrade = TimeSpan.Parse("10:55:00").TotalSeconds;
        double endtimetrade = TimeSpan.Parse("16:30:00").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));
        this.GlobalVarTable.Clear();
        this.InitVarTable();
    }
    public void InitVarTable()
    {
        this.GlobalVarTable.Add("BreakTargetPercent", "0");
        this.GlobalVarTable.Add("PrevProfit", "0");
    }
    public void ClearVarTable()
    {
        this.GlobalVarTable["BreakTargetPercent"] = "0";
        this.GlobalVarTable["PrevProfit"]="0";
    }
    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[6];
        col[0] = new DataColumn("datetime", Type.GetType("System.DateTime")); //eg. 9:45 
        col[1] = new DataColumn("status", Type.GetType("System.String"));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", Type.GetType("System.Double")); //price
        col[3] = new DataColumn("openshort", Type.GetType("System.Double"));//price
        col[4] = new DataColumn("closelong", Type.GetType("System.Double"));//price
        col[5] = new DataColumn("closeshort", Type.GetType("System.Double"));//price

        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }
    public override DataRow FeedTickTrade(DataRow resamplingtick, string portstatus, double onhandcost, double onhandvol)
    {

        //Add a resampling tick to HistoricalTable
        //HistoricalTable.Rows.Add(resamplingtick.ItemArray);

        if (portstatus == "none")
        {
            double longprice = this.OpenLongStatus(onhandcost);
            double shortprice = this.OpenShortStatus(onhandcost);
            //Not hit any condition
            if ((longprice == 0) && (shortprice == 0))
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if ((longprice > 0) && (shortprice > 0))
                this.SignalTable.Rows.Add(this.GetSingnal("dup", longprice).ItemArray);//longprice = shortprice
            else if (longprice > 0) //if Long
                this.SignalTable.Rows.Add(this.GetSingnal("openlong", longprice).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("openshort", shortprice).ItemArray);
        }
        else if (portstatus == "closelong")
        {
            double shortprice = this.CloseLongStatus(onhandcost);
            //Not hit any condition
            if (shortprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closelong", shortprice).ItemArray);
        }
        else if (portstatus == "closeshort")
        {
            double longprice = this.CloseShortStatus(onhandcost);
            //Not hit any condition
            if (longprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (longprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closeshort", longprice).ItemArray);
        }
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }
    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
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

    public override double OpenLongStatus(double onhandcost)
    {

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double stvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumTotalVolume"];
        double slvolpercent = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVolume%"];
        double tlongvol = this.LogicPropTable["TargetLongVolPercent"].Value;
        double minsumvol = this.LogicPropTable["MinSumTotalVol"].Value;
        double openpercent = this.LogicPropTable["OpenVolPercent"].Value;


        if ((counthistrow >= monitor) && (time >= openstarttime) && (time <= openendtime))
        {
            found = 0;
            //Check total long volume > x

            for (int k = 2; k <= monitor + 1; k++)
            {
                if ((tlongvol>0)&&(stvol > minsumvol))
                {
                    if ((slvolpercent >= tlongvol)&&this.GlobalVarTable["BreakTargetPercent"] == "0")
                        this.GlobalVarTable["BreakTargetPercent"] = "1";
                    else if ((slvolpercent <= openpercent) && this.GlobalVarTable["BreakTargetPercent"] == "1")
                        found++;
                }
            }
            
            //Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = open;
            }
        }
        return price;
    }
    public override double OpenShortStatus(double onhandcost)
    {
        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double stvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumTotalVolume"];
        double ssvolpercent = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumShortVolume%"];
        double tshortvol = this.LogicPropTable["TargetShortVolPercent"].Value;
        double minsumvol = this.LogicPropTable["MinSumTotalVol"].Value;
        double openpercent = this.LogicPropTable["OpenVolPercent"].Value;

        if ((counthistrow >= monitor) && (time >= openstarttime) && (time <= openendtime))
        {
            //Check total short volume > x
            found = 0;
            for (int k = 2; k <= monitor + 1; k++)
            {
                if ((tshortvol>0)&&(stvol > minsumvol))
                {
                    //Condition for break first target percent
                    if ((ssvolpercent >= tshortvol)&&this.GlobalVarTable["BreakTargetPercent"] == "0")
                        this.GlobalVarTable["BreakTargetPercent"] = "1";
                    //Condition for rebound from first target percent
                    else if ((ssvolpercent <= openpercent) && this.GlobalVarTable["BreakTargetPercent"] == "1")
                        found++;
                }
            }
            //Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalShort"].Value))
            {
                price = open;
            }
        }
        return price;
    }

    public override double CloseLongStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }

        if (price > 0)
            this.ClearVarTable();

        return price;
    }
    public override double CloseShortStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }
        if (price > 0)
            this.ClearVarTable();
        return price;
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
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit < stoploss)
                stoplossprice = close;
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
                stopgainprice = close;
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
}

class ReversalTradeLogic : TradeLogic
{
    public ReversalTradeLogic(DataTable newResamplingTable, int interval)
    {
        //Update historical table
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.GlobalVarTable = new Dictionary<String, String>();
        this.InitLogicPropTable(interval);


    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        //LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume", 700, 100, 1000, 100));
        //LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", -700, -1000, 100, 100));
        LogicPropTable.Add("TargetLongVolume", new Property("TargetLongVolume", 300, 50, 100, 10));
        LogicPropTable.Add("TargetShortVolume", new Property("TargetShortVolume",-300, 50, 100, 10));
        LogicPropTable.Add("RangePrice", new Property("RangePrice", 0, 0, 100, 10));
        //double targettimetrade = TimeSpan.Parse("14:15:00").TotalSeconds;
        //LogicPropTable.Add("TargetTimeTrade", new Property("TargetTimeTrade", targettimetrade, targettimetrade, targettimetrade, 0));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 1, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 1, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("TargetGap", new Property("TargetGap", 1.5, 1, 10, 1));

        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 20, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -20, -500, -40, 50));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", -3, -8, -2, 1));
        LogicPropTable.Add("StopGain", new Property("StopGain", 0, -8, -2, 1));
        LogicPropTable.Add("StopProfitLoss", new Property("StopProfitLoss", -2, -8, -2, 1));
        double openstarttimetrade = TimeSpan.Parse("10:15:00").TotalSeconds;
        double openendtimetrade = TimeSpan.Parse("16:05:00").TotalSeconds;
        double endtimetrade = TimeSpan.Parse("16:54:30").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));
        this.GlobalVarTable.Clear();
        this.InitVarTable();
    }
    public void InitVarTable()
    {
        this.GlobalVarTable.Add("BreakTargetPercent", "0");
        this.GlobalVarTable.Add("FoundRow", "0");
        this.GlobalVarTable.Add("PrevProfit", "0");
    }
    public void ClearVarTable()
    {
        this.GlobalVarTable["BreakTargetPercent"] = "0";
        this.GlobalVarTable["PrevProfit"] = "0";
        this.GlobalVarTable["FoundRow"] = "0";
    }
    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[6];
        col[0] = new DataColumn("datetime", Type.GetType("System.DateTime")); //eg. 9:45 
        col[1] = new DataColumn("status", Type.GetType("System.String"));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", Type.GetType("System.Double")); //price
        col[3] = new DataColumn("openshort", Type.GetType("System.Double"));//price
        col[4] = new DataColumn("closelong", Type.GetType("System.Double"));//price
        col[5] = new DataColumn("closeshort", Type.GetType("System.Double"));//price

        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }
    public override DataRow FeedTickTrade(DataRow resamplingtick, string portstatus, double onhandcost, double onhandvol)
    {

        //Add a resampling tick to HistoricalTable
        //HistoricalTable.Rows.Add(resamplingtick.ItemArray);

        if (portstatus == "none")
        {
            double longprice = this.OpenLongStatus(onhandcost);
            double shortprice = this.OpenShortStatus(onhandcost);
            //Not hit any condition
            if ((longprice == 0) && (shortprice == 0))
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if ((longprice > 0) && (shortprice > 0))
                this.SignalTable.Rows.Add(this.GetSingnal("dup", longprice).ItemArray);//longprice = shortprice
            else if (longprice > 0) //if Long
                this.SignalTable.Rows.Add(this.GetSingnal("openlong", longprice).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("openshort", shortprice).ItemArray);
        }
        else if (portstatus == "closelong")
        {
            double shortprice = this.CloseLongStatus(onhandcost);
            //Not hit any condition
            if (shortprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closelong", shortprice).ItemArray);
        }
        else if (portstatus == "closeshort")
        {
            double longprice = this.CloseShortStatus(onhandcost);
            //Not hit any condition
            if (longprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (longprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closeshort", longprice).ItemArray);
        }
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }
    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
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

    public override double OpenLongStatus(double onhandcost)
    {

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition

        //Basic Value
        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);
        
        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double high = (Double)this.HistoricalTable.Rows[counthistrow - 1]["High"];
        double low = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Low"];
        double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];

        //Additional Value
        
        double targetgap = this.LogicPropTable["TargetGap"].Value;  //gap between Day low and close high
        double targetlongvol = this.LogicPropTable["TargetLongVolume"].Value;
        double targetshortvol = this.LogicPropTable["TargetShortVolume"].Value;
        double rangeprice = this.LogicPropTable["RangePrice"].Value;

        
        
        
        if ((counthistrow >= monitor) && (time >= openstarttime) && (time <= openendtime))
        {
            found = 0;
            for (int k = 2; k <= monitor + 1; k++)
            {
                double foundrow = Double.Parse(this.GlobalVarTable["FoundRow"]);
                if (counthistrow-k>foundrow)
                {
                    double prevtotallongvol = (Double)this.HistoricalTable.Rows[counthistrow - k]["TotalLongVolume"];
                    double prevtotalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - k]["TotalShortVolume"];
                    double prevdayhigh = (Double)this.HistoricalTable.Rows[counthistrow - k]["DayHigh"];
                    double prevdaylow = (Double)this.HistoricalTable.Rows[counthistrow - k]["DayLow"];
                    double prevhigh = (Double)this.HistoricalTable.Rows[counthistrow - k]["High"];
                    double prevlow = (Double)this.HistoricalTable.Rows[counthistrow - k]["Low"];
                    double prevclose = (Double)this.HistoricalTable.Rows[counthistrow - k]["Close"];

                    //Calculate Value
                    double lowgap = prevclose - prevdaylow;
                    double highgap = prevdayhigh - prevclose;

                    //First Signal
                    if ((prevdaylow == prevlow))
                    {
                        //declare range price
                        //double upbound = prevhigh + rangeprice;
                        double lowbound = prevhigh - rangeprice;
                        if ((prevclose >= lowbound))
                        {
                            if ((lowgap >= targetgap) && (prevtotallongvol >= targetlongvol))
                            {
                                //Found first lower
                                this.GlobalVarTable["FoundRow"] = (counthistrow-k).ToString();
                                found++;
                            }
                        }
                    }
                }
                        
            }

            //Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = open;
                this.ClearVarTable();
            }
        }
        return price;
    }
    public override double OpenShortStatus(double onhandcost)
    {
        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition

        //Basic Value
        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double high = (Double)this.HistoricalTable.Rows[counthistrow - 1]["High"];
        double low = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Low"];
        double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];

        //Additional Value

        double targetgap = this.LogicPropTable["TargetGap"].Value;  //gap between Day low and close high
        double targetlongvol = this.LogicPropTable["TargetLongVolume"].Value;
        double targetshortvol = this.LogicPropTable["TargetShortVolume"].Value;
        double rangeprice = this.LogicPropTable["RangePrice"].Value;

        if ((counthistrow >= monitor) && (time >= openstarttime) && (time <= openendtime))
        {
            //Check total short volume > x
            found = 0;
            for (int k = 2; k <= monitor + 1; k++)
            {
                double foundrow = Double.Parse(this.GlobalVarTable["FoundRow"]);
                if (counthistrow - k > foundrow)
                {
                    double prevtotallongvol = (Double)this.HistoricalTable.Rows[counthistrow - k]["TotalLongVolume"];
                    double prevtotalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - k]["TotalShortVolume"];
                    double prevdayhigh = (Double)this.HistoricalTable.Rows[counthistrow - k]["DayHigh"];
                    double prevdaylow = (Double)this.HistoricalTable.Rows[counthistrow - k]["DayLow"];
                    double prevhigh = (Double)this.HistoricalTable.Rows[counthistrow - k]["High"];
                    double prevlow = (Double)this.HistoricalTable.Rows[counthistrow - k]["Low"];
                    double prevclose = (Double)this.HistoricalTable.Rows[counthistrow - k]["Close"];

                    //Calculate Value
                    double lowgap = prevclose - prevdaylow;
                    double highgap = prevdayhigh - prevclose;

                    //First Signal
                    if ((prevdayhigh == prevhigh))
                    {
                        //declare range price
                        double upbound = prevlow + rangeprice;
                        //double lowbound = prevhigh - rangeprice;
                        if ((prevclose <= upbound))
                        {
                            if ((highgap >= targetgap) && (prevtotalshortvol <= targetshortvol))
                            {
                                //Found first lower
                                this.GlobalVarTable["FoundRow"] = (counthistrow - k).ToString();
                                found++;
                            }
                        }
                    }
                }
            }
            //Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalShort"].Value))
            {
                price = open;
            }
        }
        return price;
    }

    public override double CloseLongStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }

        if (price > 0)
            this.ClearVarTable();

        return price;
    }
    public override double CloseShortStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }
        if (price > 0)
            this.ClearVarTable();
        return price;
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
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit < stoploss)
                stoplossprice = close;
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
                stopgainprice = close;
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
}

class Open4PMTradeLogic : TradeLogic
{
    public Open4PMTradeLogic(DataTable newResamplingTable, int interval)
    {
        //Update historical table
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.GlobalVarTable = new Dictionary<String, String>();
        this.InitLogicPropTable(interval);


    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume", 700, 100, 1000, 100));
        LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", -700, -1000, 100, 100));
        double targettimetrade = TimeSpan.Parse("16:00:00").TotalSeconds;
        LogicPropTable.Add("TargetTimeTrade", new Property("TargetTimeTrade", targettimetrade, targettimetrade, targettimetrade, 0));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 1, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 1, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("TargetGap", new Property("TargetGap", 0, 1, 10, 1));
        LogicPropTable.Add("OpenSignalGap", new Property("OpenSignalGap", 0, 1, 10, 1));
        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 20, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -20, -500, -40, 50));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", 0, -8, -2, 1));
        LogicPropTable.Add("StopGain", new Property("StopGain", 0, -8, -2, 1));
        LogicPropTable.Add("StopProfitLoss", new Property("StopProfitLoss", 0, -8, -2, 1));
        double openstarttimetrade = TimeSpan.Parse("15:00:00").TotalSeconds;
        double openendtimetrade = TimeSpan.Parse("16:15:00").TotalSeconds;
        double endtimetrade = TimeSpan.Parse("16:54:30").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));
        this.GlobalVarTable.Clear();
        this.InitVarTable();
    }
    public void InitVarTable()
    {
        this.GlobalVarTable.Add("Lower High-Gap", "0");
        this.GlobalVarTable.Add("BreakHigh", "0");
        this.GlobalVarTable.Add("Higher Low+Gap", "0");
        this.GlobalVarTable.Add("BreakLow", "0");
        this.GlobalVarTable.Add("PrevProfit", "0");
        this.GlobalVarTable.Add("OpenAfternoon","0");
    }
    public void ClearVarTable()
    {
        this.GlobalVarTable["Lower High-Gap"] = "0";
        this.GlobalVarTable["BreakHigh"] = "0";
        this.GlobalVarTable["Higher Low+Gap"] = "0";
        this.GlobalVarTable["BreakLow"] = "0";
        this.GlobalVarTable["OpenAfternoon"] = "0";
        
    }
    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[6];
        col[0] = new DataColumn("datetime", Type.GetType("System.DateTime")); //eg. 9:45 
        col[1] = new DataColumn("status", Type.GetType("System.String"));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", Type.GetType("System.Double")); //price
        col[3] = new DataColumn("openshort", Type.GetType("System.Double"));//price
        col[4] = new DataColumn("closelong", Type.GetType("System.Double"));//price
        col[5] = new DataColumn("closeshort", Type.GetType("System.Double"));//price

        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }
    public override DataRow FeedTickTrade(DataRow resamplingtick, string portstatus, double onhandcost, double onhandvol)
    {

        //Add a resampling tick to HistoricalTable
        //HistoricalTable.Rows.Add(resamplingtick.ItemArray);

        if (portstatus == "none")
        {
            double longprice = this.OpenLongStatus(onhandcost);
            double shortprice = this.OpenShortStatus(onhandcost);
            //Not hit any condition
            if ((longprice == 0) && (shortprice == 0))
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if ((longprice > 0) && (shortprice > 0))
                this.SignalTable.Rows.Add(this.GetSingnal("dup", longprice).ItemArray);//longprice = shortprice
            else if (longprice > 0) //if Long
                this.SignalTable.Rows.Add(this.GetSingnal("openlong", longprice).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("openshort", shortprice).ItemArray);
        }
        else if (portstatus == "closelong")
        {
            double shortprice = this.CloseLongStatus(onhandcost);
            //Not hit any condition
            if (shortprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closelong", shortprice).ItemArray);
        }
        else if (portstatus == "closeshort")
        {
            double longprice = this.CloseShortStatus(onhandcost);
            //Not hit any condition
            if (longprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (longprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closeshort", longprice).ItemArray);
        }
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }
    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
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

    public override double OpenLongStatus(double onhandcost)
    {

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        TimeSpan targettimetrade = TimeSpan.FromSeconds((Double)this.LogicPropTable["TargetTimeTrade"].Value);
        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double high = (Double)this.HistoricalTable.Rows[counthistrow - 1]["High"];
        double low = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Low"];
        double vol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Volume"];
        double tgap = this.LogicPropTable["TargetGap"].Value; ;

        if(time==(TimeSpan.Parse("14:30:00")))
            this.GlobalVarTable["OpenAfternoon"] = open.ToString();

        if ((counthistrow >= monitor) && (time <= openstarttime) && (time >= openendtime))
        {
            found = 0;
            //Check total long volume > x

            for (int k = 2; k <= monitor + 1; k++)
            {
                if (time == targettimetrade)
                {
                    double prevclose = (Double)this.HistoricalTable.Rows[counthistrow - 2]["Close"];
                    //double afternoongap = open - prevclose;
                    //if (afternoongap > tgap)
                    double open2 = Double.Parse(this.GlobalVarTable["OpenAfternoon"]);
                    if(prevclose>open2)
                        found++;
                }

            }
            //Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = open;
            }
        }
        return price;
    }
    public override double OpenShortStatus(double onhandcost)
    {
        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        TimeSpan targettimetrade = TimeSpan.FromSeconds((Double)this.LogicPropTable["TargetTimeTrade"].Value);
        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double high = (Double)this.HistoricalTable.Rows[counthistrow - 1]["High"];
        double low = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Low"];
        double vol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Volume"];
        double tgap = this.LogicPropTable["TargetGap"].Value; ;

        if (time == (TimeSpan.Parse("14:30:00")))
            this.GlobalVarTable["OpenAfternoon"] = open.ToString();

        if ((counthistrow >= monitor) && (time >= openstarttime) && (time <= openendtime))
        {
            //Check total short volume > x
            found = 0;
            for (int k = 2; k <= monitor + 1; k++)
            {
                if (time == targettimetrade)
                {
                    double open2 = Double.Parse(this.GlobalVarTable["OpenAfternoon"]);
                    
                    double prevclose = (Double)this.HistoricalTable.Rows[counthistrow - 2]["Close"];
                    //if(prevclose<=open2)
                    //double afternoongap = open - prevclose;
                    //if (afternoongap < -tgap)
                        found++;
                }
            }
            //Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalShort"].Value))
            {
                price = open;
            }
        }
        return price;
    }

    public override double CloseLongStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }

        if (price > 0)
            this.ClearVarTable();

        return price;
    }
    public override double CloseShortStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }
        if (price > 0)
            this.ClearVarTable();
        return price;
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
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit < stoploss)
                stoplossprice = close;
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
                stopgainprice = close;
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
}

class HQTradeLogic : TradeLogic
{
    public HQTradeLogic(DataTable newResamplingTable, int interval)
    {
        //Update historical table
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.GlobalVarTable = new Dictionary<String, String>();
        this.InitLogicPropTable(interval);


    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume",500, 100, 1000, 100));
        LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", -500, -1000, 100, 100));
        double targettimetrade = TimeSpan.Parse("16:00:00").TotalSeconds;
        LogicPropTable.Add("TimeLookBack", new Property("TimeLookBack", 300, 100, 100, 100));
        LogicPropTable.Add("TargetTimeTrade", new Property("TargetTimeTrade", targettimetrade, targettimetrade, targettimetrade, 0));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 1, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 1, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("TargetGap", new Property("TargetGap", 10, 1, 10, 1));
        LogicPropTable.Add("OpenSignalGap", new Property("OpenSignalGap", 0, 1, 10, 1));
        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 30, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -30, -500, -40, 50));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", 0, -8, -2, 1));
        LogicPropTable.Add("StopGain", new Property("StopGain", 0, -8, -2, 1));
        LogicPropTable.Add("StopProfitLoss", new Property("StopProfitLoss", 0, -8, -2, 1));
        double openstarttimetrade = TimeSpan.Parse("10:30:00").TotalSeconds;
        double openendtimetrade = TimeSpan.Parse("12:20:00").TotalSeconds;
        double endtimetrade = TimeSpan.Parse("12:25:45").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));
        LogicPropTable.Add("TotalLongVolPercent", new Property("TotalLongVolPercent", 35, -8, -2, 1));
        LogicPropTable.Add("TotalShortVolPercent", new Property("TotalShortVolPercent", 35, -8, -2, 1));
        this.GlobalVarTable.Clear();
        this.InitVarTable();
    }
    public void InitVarTable()
    {
        this.GlobalVarTable.Add("Lower High-Gap", "0");
        this.GlobalVarTable.Add("BreakHigh", "0");
        this.GlobalVarTable.Add("Higher Low+Gap", "0");
        this.GlobalVarTable.Add("BreakLow", "0");
        this.GlobalVarTable.Add("PrevProfit", "0");
        this.GlobalVarTable.Add("OpenAfternoon", "0");
    }
    public void ClearVarTable()
    {
        this.GlobalVarTable["Lower High-Gap"] = "0";
        this.GlobalVarTable["BreakHigh"] = "0";
        this.GlobalVarTable["Higher Low+Gap"] = "0";
        this.GlobalVarTable["BreakLow"] = "0";
        this.GlobalVarTable["OpenAfternoon"] = "0";

    }
    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[6];
        col[0] = new DataColumn("datetime", Type.GetType("System.DateTime")); //eg. 9:45 
        col[1] = new DataColumn("status", Type.GetType("System.String"));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", Type.GetType("System.Double")); //price
        col[3] = new DataColumn("openshort", Type.GetType("System.Double"));//price
        col[4] = new DataColumn("closelong", Type.GetType("System.Double"));//price
        col[5] = new DataColumn("closeshort", Type.GetType("System.Double"));//price

        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }
    public override DataRow FeedTickTrade(DataRow resamplingtick, string portstatus, double onhandcost, double onhandvol)
    {

        //Add a resampling tick to HistoricalTable
        //HistoricalTable.Rows.Add(resamplingtick.ItemArray);

        if (portstatus == "none")
        {
            double longprice = this.OpenLongStatus(onhandcost);
            double shortprice = this.OpenShortStatus(onhandcost);
            //Not hit any condition
            if ((longprice == 0) && (shortprice == 0))
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if ((longprice > 0) && (shortprice > 0))
                this.SignalTable.Rows.Add(this.GetSingnal("dup", longprice).ItemArray);//longprice = shortprice
            else if (longprice > 0) //if Long
                this.SignalTable.Rows.Add(this.GetSingnal("openlong", longprice).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("openshort", shortprice).ItemArray);
        }
        else if (portstatus == "closelong")
        {
            double shortprice = this.CloseLongStatus(onhandcost);
            //Not hit any condition
            if (shortprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closelong", shortprice).ItemArray);
        }
        else if (portstatus == "closeshort")
        {
            double longprice = this.CloseShortStatus(onhandcost);
            //Not hit any condition
            if (longprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (longprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closeshort", longprice).ItemArray);
        }
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }
    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
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

    public override double OpenLongStatus(double onhandcost)
    {

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        TimeSpan targettimetrade = TimeSpan.FromSeconds((Double)this.LogicPropTable["TargetTimeTrade"].Value);
        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double high = (Double)this.HistoricalTable.Rows[counthistrow - 1]["High"];
        double low = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Low"];
        double vol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Volume"];
        
        double targetgap = this.LogicPropTable["TargetGap"].Value;
        double targetlongvolpercent = this.LogicPropTable["TotalLongVolPercent"].Value;
        double targetshortvolpercent = this.LogicPropTable["TotalShortVolPercent"].Value;
        double ltimesec = this.LogicPropTable["TimeLookBack"].Value;

        double tlongvol, maxlongvol;

        if (time == (TimeSpan.Parse("14:30:00")))
            this.GlobalVarTable["OpenAfternoon"] = open.ToString();

        if ((counthistrow > monitor) && (time >= openstarttime) && (time <= openendtime)&&(time!=(TimeSpan.Parse("14:15:00"))))
        {
            found = 0;
            
            //Check gap high low lookback unitil expect x seconds
            double thighclose=0;
            double tlowclose=0;
            double tgapclose = 0;
            for (int m = 1; m < counthistrow; m++)
            {
                
                TimeSpan lookbacktime = (TimeSpan)this.HistoricalTable.Rows[counthistrow - m]["Time"];
                double diffsec = (time - lookbacktime).TotalSeconds;
                if (diffsec <= ltimesec)
                {
                    
                    if((thighclose == 0)||((Double)this.HistoricalTable.Rows[counthistrow - m]["High"] > thighclose))
                        thighclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["High"];
                    if ((tlowclose == 0)||((Double)this.HistoricalTable.Rows[counthistrow - m]["Low"]<tlowclose))
                        tlowclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Low"];
                }
            }
            tgapclose = thighclose - tlowclose;
            //End Check high low gap
            if (tgapclose < targetgap)
            {
                for (int k = 2; k <= monitor + 1; k++)
                {

                    tlongvol = 0;
                    maxlongvol = 0;
                    double close = (Double)this.HistoricalTable.Rows[counthistrow - k]["Close"];
                    double currgap = close - tlowclose;
                    double sumlongvolpercent = (Double)this.HistoricalTable.Rows[counthistrow - k]["SumLongVolume%"];
                    if (!HistoricalTable.Rows[counthistrow - k].IsNull("TotalLongVolume"))
                        tlongvol = (Double)this.HistoricalTable.Rows[counthistrow - k]["TotalLongVolume"];
                    if ((tlongvol > this.LogicPropTable["TotalLongVolume"].Value) &&(sumlongvolpercent<targetlongvolpercent))
                        found++;

                }
            }
            //Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = open;
            }
        }
        return price;
    }
    public override double OpenShortStatus(double onhandcost)
    {
        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


        double monitor = this.LogicPropTable["OpenMonitorPeriod"].Value;
        TimeSpan openendtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenEndTimeTrade"].Value);
        TimeSpan openstarttime = TimeSpan.FromSeconds((Double)this.LogicPropTable["OpenStartTimeTrade"].Value);

        TimeSpan targettimetrade = TimeSpan.FromSeconds((Double)this.LogicPropTable["TargetTimeTrade"].Value);
        TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
        double open = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Open"];
        double high = (Double)this.HistoricalTable.Rows[counthistrow - 1]["High"];
        double low = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Low"];
        double vol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Volume"];
        double tgap = this.LogicPropTable["TargetGap"].Value; ;

        double tshortvol, maxshortvol;

        if (time == (TimeSpan.Parse("14:30:00")))
            this.GlobalVarTable["OpenAfternoon"] = open.ToString();

        if ((counthistrow > monitor) && (time <= openstarttime) && (time >= openendtime))
        {
            //Check total short volume > x
            found = 0;
            for (int k = 2; k <= monitor + 1; k++)
            {
                tshortvol = 0;
                maxshortvol = 0;
                if (!HistoricalTable.Rows[counthistrow - k].IsNull("TotalShortVolume"))
                    tshortvol = (Double)this.HistoricalTable.Rows[counthistrow - k]["TotalShortVolume"];
                if (!HistoricalTable.Rows[counthistrow - k].IsNull("MaxShortVolume"))
                    maxshortvol = (Double)this.HistoricalTable.Rows[counthistrow - k]["MaxShortVolume"];
                if ((tshortvol < this.LogicPropTable["TotalShortVolume"].Value) && (maxshortvol < this.LogicPropTable["MaxShortVolume"].Value))
                    found++;
            }
            //Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalShort"].Value))
            {
                price = open;
            }
        }
        return price;
    }

    public override double CloseLongStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }

        if (price > 0)
            this.ClearVarTable();

        return price;
    }
    public override double CloseShortStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }
        if (price > 0)
            this.ClearVarTable();
        return price;
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
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit < stoploss)
                stoplossprice = close;
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
                stopgainprice = close;
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
}

class PriceVolReversalTradeLogic : TradeLogic
{
    public PriceVolReversalTradeLogic(DataTable newResamplingTable, int interval)
    {
        //Update historical table
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.GlobalVarTable = new Dictionary<String, String>();
        this.InitLogicPropTable(interval);


    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume", 0, 100, 1000, 100));
        LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", 0, -1000, 100, 100));
        double targettimetrade = TimeSpan.Parse("16:00:00").TotalSeconds;
        LogicPropTable.Add("TimeLookBack", new Property("TimeLookBack", 240, 100, 100, 100));
        LogicPropTable.Add("TargetTimeTrade", new Property("TargetTimeTrade", targettimetrade, targettimetrade, targettimetrade, 0));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 1, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 1, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("TargetGap", new Property("TargetGap", 10, 1, 10, 1));
        LogicPropTable.Add("OpenSignalGap", new Property("OpenSignalGap", 0, 1, 10, 1));
        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 30, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -30, -500, -40, 50));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", 0, -8, -2, 1));
        LogicPropTable.Add("StopGain", new Property("StopGain",0, -8, -2, 1));
        LogicPropTable.Add("StopProfitLoss", new Property("StopProfitLoss", 0, -8, -2, 1));
        double openstarttimetrade = TimeSpan.Parse("09:50:00").TotalSeconds;
        double openendtimetrade = TimeSpan.Parse("16:15:00").TotalSeconds;
        double endtimetrade = TimeSpan.Parse("16:54:45").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));
        LogicPropTable.Add("TotalLongVolPercent", new Property("TotalLongVolPercent", 35, -8, -2, 1));
        LogicPropTable.Add("TotalShortVolPercent", new Property("TotalShortVolPercent", 35, -8, -2, 1));
        this.GlobalVarTable.Clear();
        this.InitVarTable();
    }
    public void InitVarTable()
    {
        this.GlobalVarTable.Add("Lower High-Gap", "0");
        this.GlobalVarTable.Add("BreakHigh", "0");
        this.GlobalVarTable.Add("Higher Low+Gap", "0");
        this.GlobalVarTable.Add("BreakLow", "0");
        this.GlobalVarTable.Add("PrevProfit", "0");
        this.GlobalVarTable.Add("OpenAfternoon", "0");
    }
    public void ClearVarTable()
    {
        this.GlobalVarTable["Lower High-Gap"] = "0";
        this.GlobalVarTable["BreakHigh"] = "0";
        this.GlobalVarTable["Higher Low+Gap"] = "0";
        this.GlobalVarTable["BreakLow"] = "0";
        this.GlobalVarTable["OpenAfternoon"] = "0";

    }
    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[6];
        col[0] = new DataColumn("datetime", Type.GetType("System.DateTime")); //eg. 9:45 
        col[1] = new DataColumn("status", Type.GetType("System.String"));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", Type.GetType("System.Double")); //price
        col[3] = new DataColumn("openshort", Type.GetType("System.Double"));//price
        col[4] = new DataColumn("closelong", Type.GetType("System.Double"));//price
        col[5] = new DataColumn("closeshort", Type.GetType("System.Double"));//price

        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }
    public override DataRow FeedTickTrade(DataRow resamplingtick, string portstatus, double onhandcost, double onhandvol)
    {

        //Add a resampling tick to HistoricalTable
        //HistoricalTable.Rows.Add(resamplingtick.ItemArray);

        if (portstatus == "none")
        {
            double longprice = this.OpenLongStatus(onhandcost);
            double shortprice = this.OpenShortStatus(onhandcost);
            //Not hit any condition
            if ((longprice == 0) && (shortprice == 0))
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if ((longprice > 0) && (shortprice > 0))
                this.SignalTable.Rows.Add(this.GetSingnal("dup", longprice).ItemArray);//longprice = shortprice
            else if (longprice > 0) //if Long
                this.SignalTable.Rows.Add(this.GetSingnal("openlong", longprice).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("openshort", shortprice).ItemArray);
        }
        else if (portstatus == "closelong")
        {
            double shortprice = this.CloseLongStatus(onhandcost);
            //Not hit any condition
            if (shortprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closelong", shortprice).ItemArray);
        }
        else if (portstatus == "closeshort")
        {
            double longprice = this.CloseShortStatus(onhandcost);
            //Not hit any condition
            if (longprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (longprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closeshort", longprice).ItemArray);
        }
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }
    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
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

    public override double OpenLongStatus(double onhandcost)
    {

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


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
        double isreversal = 0;
        double ishighvol = 0;
        
        if (time == (TimeSpan.Parse("14:30:00")))
            this.GlobalVarTable["OpenAfternoon"] = open.ToString();

        if ((counthistrow > monitor) && (time <= openstarttime) && (time >= openendtime) && (time != (TimeSpan.Parse("14:15:00"))))
        {
            found = 0;

            //Check gap high low lookback unitil expect x seconds
            double tmphighprice = 0;
            double tmpvol = 0;
            isreversal = 1;
            ishighvol = 0;
            for (int m = 2; m < counthistrow; m++)
            {

                TimeSpan lookbacktime = (TimeSpan)this.HistoricalTable.Rows[counthistrow - m]["Time"];
                double diffsec = (time - lookbacktime).TotalSeconds;
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];
                tmphighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["High"];
                if (diffsec <= ltimesec)
                {

                    if (( tmphighprice < close)&&(tmpvol > vol))
                        isreversal = isreversal * 1;
                    else isreversal = isreversal * 0;
                    
                    if ((Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"] < tshortvol)
                        ishighvol = 1;
                }
                
            }

            //End Check reversal
            if ((isreversal == 1) && (ishighvol ==1))
            {
                for (int k = 2; k <= monitor + 1; k++)
                {
                    found++;
                }
            }
            //Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = open;
            }
        }
        return price;
    }
    public override double OpenShortStatus(double onhandcost)
    {
        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


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
        double isreversal = 0;
        double ishighvol = 0;

        if (time == (TimeSpan.Parse("14:30:00")))
            this.GlobalVarTable["OpenAfternoon"] = open.ToString();

        if ((counthistrow > monitor) && (time >= openstarttime) && (time <= openendtime) && (time != (TimeSpan.Parse("14:15:00"))))
        {
            found = 0;

            //Check gap high low lookback unitil expect x seconds
            double tmplowprice = 0;
            double tmpvol = 0;
            isreversal = 1;
            ishighvol = 0;
            for (int m = 2; m < counthistrow; m++)
            {

                TimeSpan lookbacktime = (TimeSpan)this.HistoricalTable.Rows[counthistrow - m]["Time"];
                double diffsec = (time - lookbacktime).TotalSeconds;
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];
                tmplowprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["Low"];
                if (diffsec <= ltimesec)
                {

                    if ((tmplowprice > close) && (tmpvol < vol))
                        isreversal = isreversal * 1;
                    else isreversal = isreversal * 0;

                    if ((Double)this.HistoricalTable.Rows[counthistrow - m]["TotalLongVolume"] > tlongvol)
                        ishighvol = 1;
                }

            }

            //End Check reversal
            if ((isreversal == 1) && (ishighvol == 1))
            {
                for (int k = 2; k <= monitor + 1; k++)
                {
                    found++;

                }
            }
            //Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalShort"].Value))
            {
                price = open;
            }
        }
        return price;
    }

    public override double CloseLongStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }

        if (price > 0)
            this.ClearVarTable();

        return price;
    }
    public override double CloseShortStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }
        if (price > 0)
            this.ClearVarTable();
        return price;
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
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit < stoploss)
                stoplossprice = close;
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
                stopgainprice = close;
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
}


class BigLotsTradeLogic : TradeLogic
{
    public BigLotsTradeLogic(DataTable newResamplingTable, int interval)
    {
        //Update historical table
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.GlobalVarTable = new Dictionary<String, String>();
        this.InitLogicPropTable(interval);


    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume", 400, 100, 1000, 100));
        LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", -400, -1000, 100, 100));
        LogicPropTable.Add("TotalLongTrans", new Property("TotalLongTrans", 90, 100, 1000, 100));
        LogicPropTable.Add("TotalShortTrans", new Property("TotalShortTrans",90, -1000, 100, 100));
        LogicPropTable.Add("Gap", new Property("Gap", 0, 0, 10, 1));
        double targettimetrade = TimeSpan.Parse("16:00:00").TotalSeconds;
        LogicPropTable.Add("TimeLookBack", new Property("TimeLookBack", 240, 100, 100, 100));
        LogicPropTable.Add("TargetTimeTrade", new Property("TargetTimeTrade", targettimetrade, targettimetrade, targettimetrade, 0));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 2, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 2, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 4, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("TargetGap", new Property("TargetGap", 10, 1, 10, 1));
        LogicPropTable.Add("OpenSignalGap", new Property("OpenSignalGap", 0, 1, 10, 1));
        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 30, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -30, -500, -40, 50));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", 0, -8, -2, 1));
        LogicPropTable.Add("StopGain", new Property("StopGain", 0, -8, -2, 1));
        LogicPropTable.Add("StopProfitLoss", new Property("StopProfitLoss", 0, -8, -2, 1));
        double openstarttimetrade = TimeSpan.Parse("09:50:00").TotalSeconds;
        double openendtimetrade = TimeSpan.Parse("16:05:00").TotalSeconds;
        double endtimetrade = TimeSpan.Parse("16:54:00").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));
        LogicPropTable.Add("TotalLongVolPercent", new Property("TotalLongVolPercent", 35, -8, -2, 1));
        LogicPropTable.Add("TotalShortVolPercent", new Property("TotalShortVolPercent", 35, -8, -2, 1));
        this.GlobalVarTable.Clear();
        this.InitVarTable();
    }
    public void InitVarTable()
    {
        this.GlobalVarTable.Add("Lower High-Gap", "0");
        this.GlobalVarTable.Add("BreakHigh", "0");
        this.GlobalVarTable.Add("Higher Low+Gap", "0");
        this.GlobalVarTable.Add("BreakLow", "0");
        this.GlobalVarTable.Add("PrevProfit", "0");
        this.GlobalVarTable.Add("OpenAfternoon", "0");
    }
    public void ClearVarTable()
    {
        this.GlobalVarTable["Lower High-Gap"] = "0";
        this.GlobalVarTable["BreakHigh"] = "0";
        this.GlobalVarTable["Higher Low+Gap"] = "0";
        this.GlobalVarTable["BreakLow"] = "0";
        this.GlobalVarTable["OpenAfternoon"] = "0";

    }
    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[6];
        col[0] = new DataColumn("datetime", Type.GetType("System.DateTime")); //eg. 9:45 
        col[1] = new DataColumn("status", Type.GetType("System.String"));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", Type.GetType("System.Double")); //price
        col[3] = new DataColumn("openshort", Type.GetType("System.Double"));//price
        col[4] = new DataColumn("closelong", Type.GetType("System.Double"));//price
        col[5] = new DataColumn("closeshort", Type.GetType("System.Double"));//price

        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }
    public override DataRow FeedTickTrade(DataRow resamplingtick, string portstatus, double onhandcost, double onhandvol)
    {

        //Add a resampling tick to HistoricalTable
        //HistoricalTable.Rows.Add(resamplingtick.ItemArray);

        if (portstatus == "none")
        {
            double longprice = this.OpenLongStatus(onhandcost);
            double shortprice = this.OpenShortStatus(onhandcost);
            //Not hit any condition
            if ((longprice == 0) && (shortprice == 0))
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if ((longprice > 0) && (shortprice > 0))
                this.SignalTable.Rows.Add(this.GetSingnal("dup", longprice).ItemArray);//longprice = shortprice
            else if (longprice > 0) //if Long
                this.SignalTable.Rows.Add(this.GetSingnal("openlong", longprice).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("openshort", shortprice).ItemArray);
        }
        else if (portstatus == "closelong")
        {
            double shortprice = this.CloseLongStatus(onhandcost);
            //Not hit any condition
            if (shortprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closelong", shortprice).ItemArray);
        }
        else if (portstatus == "closeshort")
        {
            double longprice = this.CloseShortStatus(onhandcost);
            //Not hit any condition
            if (longprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (longprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closeshort", longprice).ItemArray);
        }
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }
    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
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

    public override double OpenLongStatus(double onhandcost)
    {

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


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
        double tgap = this.LogicPropTable["Gap"].Value;

        double isreversal = 0;
        double ishighvol = 0;
        /*
        if (time == (TimeSpan.Parse("14:30:00")))
            this.VarTable["OpenAfternoon"] = open.ToString();
        */
        if ((counthistrow > monitor) && (time >= openstarttime) && (time <= openendtime) && (time != (TimeSpan.Parse("14:15:00"))&& (time != (TimeSpan.Parse("09:45:00")))))
        {
            found = 0;

            //Check gap high low lookback unitil expect x seconds
            double tmphighprice = 0;
            double tmpvol = 0;
            isreversal = 1;
            ishighvol = 0;
            for (int m = 2; m < counthistrow; m++)
            {
                double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                double daylogwprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];

                if ((close + tgap) < dayhighprice)
                {
                    if ((biglotssumlongvol > tlongvol) && (biglotssumlongtrans > tlongtrans))
                        found++;
                }

                /*
                TimeSpan lookbacktime = (TimeSpan)this.HistoricalTable.Rows[counthistrow - m]["Time"];
                double diffsec = (time - lookbacktime).TotalSeconds;
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];
                tmphighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["High"];
                if (diffsec <= ltimesec)
                {

                    if ((tmphighprice < close) && (tmpvol > vol))
                        isreversal = isreversal * 1;
                    else isreversal = isreversal * 0;

                    if ((Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"] < tshortvol)
                        ishighvol = 1;
                }
                */

            }

            //End Check reversal
            /*
            if ((isreversal == 1) && (ishighvol == 1))
            {
                for (int k = 2; k <= monitor + 1; k++)
                {
                    found++;
                }
            }*/
            //Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = open;
            }
        }
        return price;
    }
    public override double OpenShortStatus(double onhandcost)
    {
        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


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
        double tgap = this.LogicPropTable["Gap"].Value;

        double isreversal = 0;
        double ishighvol = 0;
        /*
        if (time == (TimeSpan.Parse("14:30:00")))
            this.VarTable["OpenAfternoon"] = open.ToString();
        */
        if ((counthistrow > monitor) && (time >= openstarttime) && (time <= openendtime) && (time != (TimeSpan.Parse("14:15:00")) && (time != (TimeSpan.Parse("09:45:00")))))
        {
            found = 0;

            //Check gap high low lookback unitil expect x seconds
            double tmplowprice = 0;
            double tmpvol = 0;
            isreversal = 1;
            ishighvol = 0;
            for (int m = 2; m < counthistrow; m++)
            {

                double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                double daylowprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];

                if ((close - tgap) > daylowprice)
                {
                    if ((biglotssumshortvol < tshortvol) && (biglotssumshorttrans > tshorttrans))
                        found++;
                }

            }

            //Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalShort"].Value))
            {
                price = open;
            }
        }
        return price;
    }

    public override double CloseLongStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }

        if (price > 0)
            this.ClearVarTable();

        return price;
    }
    public override double CloseShortStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }
        if (price > 0)
            this.ClearVarTable();
        return price;
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
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit < stoploss)
                stoplossprice = close;
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
                stopgainprice = close;
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
}


class BigLotsSensitiveTradeLogic : TradeLogic
{
    public BigLotsSensitiveTradeLogic(DataTable newResamplingTable, int interval)
    {
        //Update historical table
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.GlobalVarTable = new Dictionary<String, string>();
        this.InitLogicPropTable(interval);


    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume", 400, 100, 1000, 100));
        LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", -400, -1000, 100, 100));
        LogicPropTable.Add("TotalLongTrans", new Property("TotalLongTrans", 90, 100, 1000, 100));
        LogicPropTable.Add("TotalShortTrans", new Property("TotalShortTrans", 90, -1000, 100, 100));
        LogicPropTable.Add("Gap", new Property("Gap", 0, 0, 10, 1));
        double targettimetrade = TimeSpan.Parse("16:00:00").TotalSeconds;
        LogicPropTable.Add("TimeLookBack", new Property("TimeLookBack", 240, 100, 100, 100));
        LogicPropTable.Add("TargetTimeTrade", new Property("TargetTimeTrade", targettimetrade, targettimetrade, targettimetrade, 0));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 2, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 2, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 4, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("TargetGap", new Property("TargetGap", 10, 1, 10, 1));
        LogicPropTable.Add("OpenSignalGap", new Property("OpenSignalGap", 0, 1, 10, 1));
        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 30, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -30, -500, -40, 50));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", 0, -8, -2, 1));
        LogicPropTable.Add("StopGain", new Property("StopGain", 0, -8, -2, 1));
        LogicPropTable.Add("StopProfitLoss", new Property("StopProfitLoss", 0, -8, -2, 1));
        double openstarttimetrade = TimeSpan.Parse("09:50:00").TotalSeconds;
        double openendtimetrade = TimeSpan.Parse("16:05:00").TotalSeconds;
        double endtimetrade = TimeSpan.Parse("16:54:00").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));
        LogicPropTable.Add("TotalLongVolPercent", new Property("TotalLongVolPercent", 35, -8, -2, 1));
        LogicPropTable.Add("TotalShortVolPercent", new Property("TotalShortVolPercent", 35, -8, -2, 1));
        this.GlobalVarTable.Clear();
        this.InitVarTable();
    }
    
    public void InitVarTable()
    {
        this.GlobalVarTable.Add("Lower High-Gap", "0");
        this.GlobalVarTable.Add("BreakHigh", "0");
        this.GlobalVarTable.Add("Higher Low+Gap", "0");
        this.GlobalVarTable.Add("BreakLow", "0");
        this.GlobalVarTable.Add("PrevProfit", "0");
        this.GlobalVarTable.Add("OpenAfternoon", "0");
    }
    public void ClearVarTable()
    {
        this.GlobalVarTable["Lower High-Gap"] = "0";
        this.GlobalVarTable["BreakHigh"] = "0";
        this.GlobalVarTable["Higher Low+Gap"] = "0";
        this.GlobalVarTable["BreakLow"] = "0";
        this.GlobalVarTable["OpenAfternoon"] = "0";

    }
    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[6];
        col[0] = new DataColumn("datetime", Type.GetType("System.DateTime")); //eg. 9:45 
        col[1] = new DataColumn("status", Type.GetType("System.String"));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", Type.GetType("System.Double")); //price
        col[3] = new DataColumn("openshort", Type.GetType("System.Double"));//price
        col[4] = new DataColumn("closelong", Type.GetType("System.Double"));//price
        col[5] = new DataColumn("closeshort", Type.GetType("System.Double"));//price

        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }
    public override DataRow FeedTickTrade(DataRow resamplingtick, string portstatus, double onhandcost, double onhandvol)
    {

        //Add a resampling tick to HistoricalTable
        //HistoricalTable.Rows.Add(resamplingtick.ItemArray);

        if (portstatus == "none")
        {
            double longprice = this.OpenLongStatus(onhandcost);
            double shortprice = this.OpenShortStatus(onhandcost);
            //Not hit any condition
            if ((longprice == 0) && (shortprice == 0))
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if ((longprice > 0) && (shortprice > 0))
                this.SignalTable.Rows.Add(this.GetSingnal("dup", longprice).ItemArray);//longprice = shortprice
            else if (longprice > 0) //if Long
                this.SignalTable.Rows.Add(this.GetSingnal("openlong", longprice).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("openshort", shortprice).ItemArray);
        }
        else if (portstatus == "closelong")
        {
            double shortprice = this.CloseLongStatus(onhandcost);
            //Not hit any condition
            if (shortprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closelong", shortprice).ItemArray);
        }
        else if (portstatus == "closeshort")
        {
            double longprice = this.CloseShortStatus(onhandcost);
            //Not hit any condition
            if (longprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (longprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closeshort", longprice).ItemArray);
        }
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }
    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
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

    public override double OpenLongStatus(double onhandcost)
    {

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


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
        double tgap = this.LogicPropTable["Gap"].Value;

       
        /*
        if (time == (TimeSpan.Parse("14:30:00")))
            this.VarTable["OpenAfternoon"] = open.ToString();
        */
        if ((counthistrow > monitor) && (time >= openstarttime) && (time <= openendtime) && (time != (TimeSpan.Parse("14:15:00")) && (time != (TimeSpan.Parse("09:45:00")))))
        {
            found = 0;

            //Check gap high low lookback unitil expect x seconds
            double tmpvol = 0;
            for (int m = 2; m < counthistrow; m++)
            {
                double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                double daylogwprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                double biglotclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Close"];
                double biglothigh = (Double)this.HistoricalTable.Rows[counthistrow - m]["High"];
                double biglotlow = (Double)this.HistoricalTable.Rows[counthistrow - m]["Low"];
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];

                if ((biglotssumlongvol > tlongvol) && (biglotssumlongtrans > tlongtrans))
                {
                    if ((biglotclose - biglotlow) > tgap)
                        found++;
                }

            }//Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = open;
            }
        }
        return price;
    }
    public override double OpenShortStatus(double onhandcost)
    {
        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


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
        double tgap = this.LogicPropTable["Gap"].Value;

        double isreversal = 0;
        double ishighvol = 0;
        /*
        if (time == (TimeSpan.Parse("14:30:00")))
            this.VarTable["OpenAfternoon"] = open.ToString();
        */
        if ((counthistrow > monitor) && (time <= openstarttime) && (time >= openendtime) && (time != (TimeSpan.Parse("14:15:00")) && (time != (TimeSpan.Parse("09:45:00")))))
        {
            found = 0;

            //Check gap high low lookback unitil expect x seconds
            double tmplowprice = 0;
            double tmpvol = 0;
            isreversal = 1;
            ishighvol = 0;
            for (int m = 2; m < counthistrow; m++)
            {

                double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                double daylowprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];

                if ((close - tgap) > daylowprice)
                {
                    if ((biglotssumshortvol < tshortvol) && (biglotssumshorttrans > tshorttrans))
                        found++;
                }

            }

            //Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalShort"].Value))
            {
                price = open;
            }
        }
        return price;
    }

    public override double CloseLongStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }

        if (price > 0)
            this.ClearVarTable();

        return price;
    }
    public override double CloseShortStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }
        if (price > 0)
            this.ClearVarTable();
        return price;
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
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit < stoploss)
                stoplossprice = close;
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
                stopgainprice = close;
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
}


class BigLotsReversalTradeLogic : TradeLogic
{
    public BigLotsReversalTradeLogic(DataTable newResamplingTable, int interval)
    {
        //Update historical table
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.GlobalVarTable = new Dictionary<String, string>();
        this.InitLogicPropTable(interval);


    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume", 200, 100, 1000, 100));
        LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", -400, -1000, 100, 100));
        LogicPropTable.Add("TotalLongTrans", new Property("TotalLongTrans", 30, 100, 1000, 100));
        LogicPropTable.Add("TotalShortTrans", new Property("TotalShortTrans", 60, -1000, 100, 100));
        LogicPropTable.Add("Gap", new Property("Gap", 1.6, 0, 10, 1));
        LogicPropTable.Add("LongGap", new Property("LongGap", 1.6, 0, 10, 1));
        LogicPropTable.Add("ShortGap", new Property("ShortGap", 2.0, 0, 10, 1));
        double targettimetrade = TimeSpan.Parse("16:00:00").TotalSeconds;
        LogicPropTable.Add("TimeLookBack", new Property("TimeLookBack", 240, 100, 100, 100));
        LogicPropTable.Add("TargetTimeTrade", new Property("TargetTimeTrade", targettimetrade, targettimetrade, targettimetrade, 0));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 1, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 1, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 2, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("TargetGap", new Property("TargetGap", 10, 1, 10, 1));
        LogicPropTable.Add("OpenSignalGap", new Property("OpenSignalGap", 0, 1, 10, 1));
        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 30, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -30, -500, -40, 50));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", 0, -8, -2, 1));
        LogicPropTable.Add("StopGain", new Property("StopGain", 0, -8, -2, 1));
        LogicPropTable.Add("StopProfitLoss", new Property("StopProfitLoss", 0, -8, -2, 1));
        double openstarttimetrade = TimeSpan.Parse("15:30:00").TotalSeconds;
        double openendtimetrade = TimeSpan.Parse("16:05:00").TotalSeconds;
        double endtimetrade = TimeSpan.Parse("16:54:00").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));
        LogicPropTable.Add("TotalLongVolPercent", new Property("TotalLongVolPercent", 35, -8, -2, 1));
        LogicPropTable.Add("TotalShortVolPercent", new Property("TotalShortVolPercent", 35, -8, -2, 1));
        this.GlobalVarTable.Clear();
        this.InitVarTable();
    }

    public void InitVarTable()
    {
        this.GlobalVarTable.Add("Lower High-Gap", "0");
        this.GlobalVarTable.Add("BreakHigh", "0");
        this.GlobalVarTable.Add("Higher Low+Gap", "0");
        this.GlobalVarTable.Add("BreakLow", "0");
        this.GlobalVarTable.Add("PrevProfit", "0");
        this.GlobalVarTable.Add("OpenAfternoon", "0");
    }
    public void ClearVarTable()
    {
        this.GlobalVarTable["Lower High-Gap"] = "0";
        this.GlobalVarTable["BreakHigh"] = "0";
        this.GlobalVarTable["Higher Low+Gap"] = "0";
        this.GlobalVarTable["BreakLow"] = "0";
        this.GlobalVarTable["OpenAfternoon"] = "0";

    }
    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[6];
        col[0] = new DataColumn("datetime", Type.GetType("System.DateTime")); //eg. 9:45 
        col[1] = new DataColumn("status", Type.GetType("System.String"));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", Type.GetType("System.Double")); //price
        col[3] = new DataColumn("openshort", Type.GetType("System.Double"));//price
        col[4] = new DataColumn("closelong", Type.GetType("System.Double"));//price
        col[5] = new DataColumn("closeshort", Type.GetType("System.Double"));//price

        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }
    public override DataRow FeedTickTrade(DataRow resamplingtick, string portstatus, double onhandcost, double onhandvol)
    {

        //Add a resampling tick to HistoricalTable
        //HistoricalTable.Rows.Add(resamplingtick.ItemArray);

        if (portstatus == "none")
        {
            double longprice = this.OpenLongStatus(onhandcost);
            double shortprice = this.OpenShortStatus(onhandcost);
            //Not hit any condition
            if ((longprice == 0) && (shortprice == 0))
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if ((longprice > 0) && (shortprice > 0))
                this.SignalTable.Rows.Add(this.GetSingnal("dup", longprice).ItemArray);//longprice = shortprice
            else if (longprice > 0) //if Long
                this.SignalTable.Rows.Add(this.GetSingnal("openlong", longprice).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("openshort", shortprice).ItemArray);
        }
        else if (portstatus == "closelong")
        {
            double shortprice = this.CloseLongStatus(onhandcost);
            //Not hit any condition
            if (shortprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closelong", shortprice).ItemArray);
        }
        else if (portstatus == "closeshort")
        {
            double longprice = this.CloseShortStatus(onhandcost);
            //Not hit any condition
            if (longprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (longprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closeshort", longprice).ItemArray);
        }
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }
    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
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

    public override double OpenLongStatus(double onhandcost)
    {

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


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


        /*
        if (time == (TimeSpan.Parse("14:30:00")))
            this.VarTable["OpenAfternoon"] = open.ToString();
        */
        if ((counthistrow > monitor) && (time >= openstarttime) && (time <= openendtime) && (time != (TimeSpan.Parse("14:15:00")) && (time != (TimeSpan.Parse("09:45:00")))))
        {
            found = 0;

            //Check gap high low lookback unitil expect x seconds
            double tmpvol = 0;
            double tmpnewlow = 0;
            for (int m = 2; m < counthistrow; m++)
            {
                double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                double daylogwprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                double biglotclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Close"];
                double totallongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalLongVolume"];
                double totalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"];

                double biglothigh = (Double)this.HistoricalTable.Rows[counthistrow - m]["High"];
                double biglotlow = (Double)this.HistoricalTable.Rows[counthistrow - m]["Low"];
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];


                if ((biglotssumlongvol > tlongvol) &&(biglotssumlongtrans > tlongtrans)&& ((biglothigh-biglotlow)>tlonggap))
                {
                    found++;
                }
                

            }//Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = open;
            }
        }
        return price;
    }
    public override double OpenShortStatus(double onhandcost)
    {
        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


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
        double tshortgap = this.LogicPropTable["ShortGap"].Value;

        double isreversal = 0;
        double ishighvol = 0;
        /*
        if (time == (TimeSpan.Parse("14:30:00")))
            this.VarTable["OpenAfternoon"] = open.ToString();
        */
        if ((counthistrow > monitor) && (time <= openstarttime) && (time >= openendtime) && (time != (TimeSpan.Parse("14:15:00")) && (time != (TimeSpan.Parse("09:45:00")))))
        {
            found = 0;

            //Check gap high low lookback unitil expect x seconds
            double tmpvol = 0;
            double tmpnewlow = 0;
            for (int m = 2; m < counthistrow; m++)
            {
                double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                double daylogwprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                double biglotclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Close"];
                double totallongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalLongVolume"];
                double totalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"];

                double biglothigh = (Double)this.HistoricalTable.Rows[counthistrow - m]["High"];
                double biglotlow = (Double)this.HistoricalTable.Rows[counthistrow - m]["Low"];
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];


                if ((biglotssumshortvol < tshortvol) && (biglotssumshorttrans > tshorttrans) && ((biglothigh - biglotlow) > tshortgap))
                {
                    found++;
                }


            }//Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = open;
            }
        }
        return price;
    }

    public override double CloseLongStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }

        if (price > 0)
            this.ClearVarTable();

        return price;
    }
    public override double CloseShortStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }
        if (price > 0)
            this.ClearVarTable();
        return price;
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
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit < stoploss)
                stoplossprice = close;
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
                stopgainprice = close;
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
}


class BigLotsReversalTradeLogic2 : TradeLogic
{
    public BigLotsReversalTradeLogic2(DataTable newResamplingTable, int interval)
    {
        //Update historical table
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.GlobalVarTable = new Dictionary<String, string>();
        this.InitLogicPropTable(interval);


    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume",  400, 100, 1000, 100));
        LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", -200, -1000, 100, 100));
        LogicPropTable.Add("TotalLongTrans", new Property("TotalLongTrans", 100, 100, 1000, 100));
        LogicPropTable.Add("TotalShortTrans", new Property("TotalShortTrans", 10, -1000, 100, 100));
        LogicPropTable.Add("Gap", new Property("Gap", 1, 0, 10, 1));
        LogicPropTable.Add("LongGap", new Property("LongGap", 2.2, 0, 10, 1));
        LogicPropTable.Add("ShortGap", new Property("ShortGap", 2.0, 0, 10, 1));
        double targettimetrade = TimeSpan.Parse("16:00:00").TotalSeconds;
        LogicPropTable.Add("TimeLookBack", new Property("TimeLookBack", 240, 100, 100, 100));
        LogicPropTable.Add("TargetTimeTrade", new Property("TargetTimeTrade", targettimetrade, targettimetrade, targettimetrade, 0));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 1, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 1, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 20, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("TargetGap", new Property("TargetGap", 10, 1, 10, 1));
        LogicPropTable.Add("OpenSignalGap", new Property("OpenSignalGap", 0, 1, 10, 1));
        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 30, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -30, -500, -40, 50));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", 0, -8, -2, 1));
        LogicPropTable.Add("StopGain", new Property("StopGain", 0, -8, -2, 1));
        LogicPropTable.Add("StopProfitLoss", new Property("StopProfitLoss", 0, -8, -2, 1));
        double openstarttimetrade = TimeSpan.Parse("14:15:00").TotalSeconds;
        double openendtimetrade = TimeSpan.Parse("16:15:00").TotalSeconds;
        double endtimetrade = TimeSpan.Parse("16:54:00").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));
        LogicPropTable.Add("TotalLongVolPercent", new Property("TotalLongVolPercent", 35, -8, -2, 1));
        LogicPropTable.Add("TotalShortVolPercent", new Property("TotalShortVolPercent", 35, -8, -2, 1));
        this.GlobalVarTable.Clear();
        this.InitVarTable();
    }

    public void InitVarTable()
    {
        this.GlobalVarTable.Add("Lower High-Gap", "0");
        this.GlobalVarTable.Add("BreakHigh", "0");
        this.GlobalVarTable.Add("Higher Low+Gap", "0");
        this.GlobalVarTable.Add("BreakLow", "0");
        this.GlobalVarTable.Add("PrevProfit", "0");
        this.GlobalVarTable.Add("OpenAfternoon", "0");
    }
    public void ClearVarTable()
    {
        this.GlobalVarTable["Lower High-Gap"] = "0";
        this.GlobalVarTable["BreakHigh"] = "0";
        this.GlobalVarTable["Higher Low+Gap"] = "0";
        this.GlobalVarTable["BreakLow"] = "0";
        this.GlobalVarTable["OpenAfternoon"] = "0";

    }
    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[6];
        col[0] = new DataColumn("datetime", Type.GetType("System.DateTime")); //eg. 9:45 
        col[1] = new DataColumn("status", Type.GetType("System.String"));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", Type.GetType("System.Double")); //price
        col[3] = new DataColumn("openshort", Type.GetType("System.Double"));//price
        col[4] = new DataColumn("closelong", Type.GetType("System.Double"));//price
        col[5] = new DataColumn("closeshort", Type.GetType("System.Double"));//price

        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }
    public override DataRow FeedTickTrade(DataRow resamplingtick, string portstatus, double onhandcost, double onhandvol)
    {

        //Add a resampling tick to HistoricalTable
        //HistoricalTable.Rows.Add(resamplingtick.ItemArray);

        if (portstatus == "none")
        {
            double longprice = this.OpenLongStatus(onhandcost);
            double shortprice = this.OpenShortStatus(onhandcost);
            //Not hit any condition
            if ((longprice == 0) && (shortprice == 0))
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if ((longprice > 0) && (shortprice > 0))
                this.SignalTable.Rows.Add(this.GetSingnal("dup", longprice).ItemArray);//longprice = shortprice
            else if (longprice > 0) //if Long
                this.SignalTable.Rows.Add(this.GetSingnal("openlong", longprice).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("openshort", shortprice).ItemArray);
        }
        else if (portstatus == "closelong")
        {
            double shortprice = this.CloseLongStatus(onhandcost);
            //Not hit any condition
            if (shortprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closelong", shortprice).ItemArray);
        }
        else if (portstatus == "closeshort")
        {
            double longprice = this.CloseShortStatus(onhandcost);
            //Not hit any condition
            if (longprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (longprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closeshort", longprice).ItemArray);
        }
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }
    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
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

    public override double OpenLongStatus(double onhandcost)
    {

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


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

        double lastbiglotslongvol = (Double)this.HistoricalTable.Rows[counthistrow - 1]["SumLongVol Biglots"];
        if (lastbiglotslongvol > 0)
        { }
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
            double tmpnewlonglow= 0;
            double tmpcount = 0;
            double tmpclose = (Double)this.HistoricalTable.Rows[counthistrow - 2]["Close"];
            for (int m = 2; m < counthistrow; m++)
            {
                double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                double daylogwprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                double biglotclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Close"];
                double totallongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalLongVolume"];
                double totalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"];

                double biglothigh = (Double)this.HistoricalTable.Rows[counthistrow - m]["High"];
                double biglotlow = (Double)this.HistoricalTable.Rows[counthistrow - m]["Low"];
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];


                if (biglotssumlongtrans>70)
                {
                    tmpcount++;
                    if ((tmpnewlonglow == 0) || tmpnewlonglow > biglotlow)
                        tmpnewlonglow = biglotlow;
                }
                //if (biglotssumlongvol > tlongvol)
                //    tmpnewlonghigh = biglothigh;
                if ((tmpcount>=1)&&(tmpclose+tlonggap<tmpnewlonglow))
                    found++;

            }//Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = open;
            }
        }
        return price;
    }
    public override double OpenShortStatus(double onhandcost)
    {
        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


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
        double tshortgap = this.LogicPropTable["ShortGap"].Value;

        double isreversal = 0;
        double ishighvol = 0;
        /*
        if (time == (TimeSpan.Parse("14:30:00")))
            this.VarTable["OpenAfternoon"] = open.ToString();
        */
        if ((counthistrow > monitor) && (time <= openstarttime) && (time >= openendtime) && (time != (TimeSpan.Parse("14:15:00")) && (time != (TimeSpan.Parse("09:45:00")))))
        {
            found = 0;

            //Check gap high low lookback unitil expect x seconds
            double tmpvol = 0;
            double tmpnewlow = 0;
            for (int m = 2; m < counthistrow; m++)
            {
                double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                double daylogwprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                double biglotclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Close"];
                double totallongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalLongVolume"];
                double totalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"];

                double biglothigh = (Double)this.HistoricalTable.Rows[counthistrow - m]["High"];
                double biglotlow = (Double)this.HistoricalTable.Rows[counthistrow - m]["Low"];
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];


                if ((biglotssumshortvol < tshortvol) && (biglotssumshorttrans > tshorttrans) && ((biglothigh - biglotlow) > tshortgap))
                {
                    found++;
                }


            }//Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = open;
            }
        }
        return price;
    }

    public override double CloseLongStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }

        if (price > 0)
            this.ClearVarTable();

        return price;
    }
    public override double CloseShortStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }
        if (price > 0)
            this.ClearVarTable();
        return price;
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
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
            if (profit < stoploss)
                stoplossprice = close;
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
                stopgainprice = close;
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
}


class BigLotsHFTTradeLogic : TradeLogic
{
    public BigLotsHFTTradeLogic(DataTable newResamplingTable, int interval)
    {
        //Update historical table
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.GlobalVarTable = new Dictionary<String, string>();
        this.InitLogicPropTable(interval);


    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume", 800, 100, 1000, 100));
        LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", -800, -1000, 100, 100));
        LogicPropTable.Add("TotalLongTrans", new Property("TotalLongTrans", 40, 100, 1000, 100));
        LogicPropTable.Add("TotalShortTrans", new Property("TotalShortTrans", 40, -1000, 100, 100));
        LogicPropTable.Add("Gap", new Property("Gap", 1, 0, 10, 1));
        LogicPropTable.Add("LongGap", new Property("LongGap", 1, 0, 10, 1));
        LogicPropTable.Add("ShortGap", new Property("ShortGap", 1, 0, 10, 1));
        double targettimetrade = TimeSpan.Parse("16:00:00").TotalSeconds;
        LogicPropTable.Add("TimeLookBack", new Property("TimeLookBack", 240, 100, 100, 100));
        LogicPropTable.Add("TargetTimeTrade", new Property("TargetTimeTrade", targettimetrade, targettimetrade, targettimetrade, 0));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 1, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 1, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("TargetGap", new Property("TargetGap", 10, 1, 10, 1));
        LogicPropTable.Add("OpenSignalGap", new Property("OpenSignalGap", 0, 1, 10, 1));
        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 30, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -30, -500, -40, 50));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", -2.5, -8, -2, 1));
        LogicPropTable.Add("StopGain", new Property("StopGain", 3.5, -8, -2, 1));
        LogicPropTable.Add("StopProfitLoss", new Property("StopProfitLoss", 0, -8, -2, 1));
        double openstarttimetrade = TimeSpan.Parse("14:20:00").TotalSeconds;
        double openendtimetrade = TimeSpan.Parse("16:20:00").TotalSeconds;
        double endtimetrade = TimeSpan.Parse("16:54:00").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));
        LogicPropTable.Add("TotalLongVolPercent", new Property("TotalLongVolPercent", 35, -8, -2, 1));
        LogicPropTable.Add("TotalShortVolPercent", new Property("TotalShortVolPercent", 35, -8, -2, 1));
        this.GlobalVarTable.Clear();
        this.InitVarTable();
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

    }
    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[6];
        col[0] = new DataColumn("datetime", Type.GetType("System.DateTime")); //eg. 9:45 
        col[1] = new DataColumn("status", Type.GetType("System.String"));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", Type.GetType("System.Double")); //price
        col[3] = new DataColumn("openshort", Type.GetType("System.Double"));//price
        col[4] = new DataColumn("closelong", Type.GetType("System.Double"));//price
        col[5] = new DataColumn("closeshort", Type.GetType("System.Double"));//price

        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }
    public override DataRow FeedTickTrade(DataRow resamplingtick, string portstatus, double onhandcost, double onhandvol)
    {

        //Add a resampling tick to HistoricalTable
        //HistoricalTable.Rows.Add(resamplingtick.ItemArray);

        if (portstatus == "none")
        {
            double longprice = this.OpenLongStatus(onhandcost);
            double shortprice = this.OpenShortStatus(onhandcost);
            //Not hit any condition
            if ((longprice == 0) && (shortprice == 0))
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if ((longprice > 0) && (shortprice > 0))
                this.SignalTable.Rows.Add(this.GetSingnal("dup", longprice).ItemArray);//longprice = shortprice
            else if (longprice > 0) //if Long
                this.SignalTable.Rows.Add(this.GetSingnal("openlong", longprice).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("openshort", shortprice).ItemArray);
        }
        else if (portstatus == "closelong")
        {
            double shortprice = this.CloseLongStatus(onhandcost);
            //Not hit any condition
            if (shortprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closelong", shortprice).ItemArray);
        }
        else if (portstatus == "closeshort")
        {
            double longprice = this.CloseShortStatus(onhandcost);
            //Not hit any condition
            if (longprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (longprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closeshort", longprice).ItemArray);
        }
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }
    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
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

    public override double OpenLongStatus(double onhandcost)
    {

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


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
            GlobalVarTable["ReverseShort"] = "0";
            if (GlobalVarTable["ReverseShort"] == "1")
                found = Convert.ToInt32(this.LogicPropTable["SignalLong"].Value);
            else
                for (int m = 2; m < counthistrow; m++)
                {
                    double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                    double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                    double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                    double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                    double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                    double daylogwprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                    double biglotclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Close"];
                    double totallongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalLongVolume"];
                    double totalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"];

                    double biglothigh = (Double)this.HistoricalTable.Rows[counthistrow - m]["High"];
                    double biglotlow = (Double)this.HistoricalTable.Rows[counthistrow - m]["Low"];
                    tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];


                    if ((biglotssumshortvol < tshortvol) && (biglotssumshorttrans > tshorttrans))
                    {
                        /*if ((tmpnewshorthigh == 0) || tmpnewshorthigh < biglothigh)
                        {
                            tmpnewshorthigh = biglothigh;
                        }*/
                        if (tmpclose - tshortgap > biglothigh)
                            found++;
                    }
                    



                }//Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = open;
            }
        }
        return price;
    }
    public override double OpenShortStatus(double onhandcost)
    {
        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


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
            GlobalVarTable["ReverseLong"] = "0";
            if (GlobalVarTable["ReverseLong"] == "1")
                found = Convert.ToInt32(this.LogicPropTable["SignalShort"].Value);
            else
            
                for (int m = 2; m < counthistrow; m++)
            {
                double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                double daylogwprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                double biglotclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Close"];
                double totallongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalLongVolume"];
                double totalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"];

                double biglothigh = (Double)this.HistoricalTable.Rows[counthistrow - m]["High"];
                double biglotlow = (Double)this.HistoricalTable.Rows[counthistrow - m]["Low"];
                tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];

                    if ((biglotssumlongvol > tlongvol) && (biglotssumlongtrans > tlongtrans))
                    {
                        /*tmpcount++;
                        if ((tmpnewlonglow == 0) || tmpnewlonglow > biglotlow)
                            tmpnewlonglow = biglotlow;*/
                        if (tmpclose + tlonggap < biglotlow)
                            found++;
                    }
                    
           
                    

            }//Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalShort"].Value))
            {
                price = open;
            }
        }
        return price;
    }

    public override double CloseLongStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }

        if (price > 0)
        {
            this.ClearVarTable();
            if (stoplossprice > 0)
                GlobalVarTable["ReverseShort"] = "1";
        }

        return price;
    }
    public override double CloseShortStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }
        if (price > 0)
        {
            this.ClearVarTable();
            if (stoplossprice > 0)
                GlobalVarTable["ReverseLong"] = "1";
        }
        return price;
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
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
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
                stopgainprice = close;
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
}
class BigLotsHFTTradeLogic2 : TradeLogic
{
    public BigLotsHFTTradeLogic2(DataTable newResamplingTable, int interval)
    {
        //Update historical table
        this.HistoricalTable = newResamplingTable.Clone();
        //Initial signaltable
        this.SignalTable = this.NewSignalTable();
        //Init LogicPropTable
        this.LogicPropTable = new Dictionary<string, Property>();
        this.GlobalVarTable = new Dictionary<String, string>();
        this.InitLogicPropTable(interval);


    }
    public override void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("TotalLongVolume", new Property("TotalLongVolume", 700, 100, 1000, 100));
        LogicPropTable.Add("TotalShortVolume", new Property("TotalShortVolume", -700, -1000, 100, 100));
        LogicPropTable.Add("TotalLongTrans", new Property("TotalLongTrans", 200, 100, 1000, 100));
        LogicPropTable.Add("TotalShortTrans", new Property("TotalShortTrans", 200, -1000, 100, 100));
        LogicPropTable.Add("Gap", new Property("Gap", 1, 0, 10, 1));
        LogicPropTable.Add("LongGap", new Property("LongGap", 2.5, 0, 10, 1));
        LogicPropTable.Add("ShortGap", new Property("ShortGap", 2.5, 0, 10, 1));
        double targettimetrade = TimeSpan.Parse("16:00:00").TotalSeconds;
        LogicPropTable.Add("TimeLookBack", new Property("TimeLookBack", 240, 100, 100, 100));
        LogicPropTable.Add("TargetTimeTrade", new Property("TargetTimeTrade", targettimetrade, targettimetrade, targettimetrade, 0));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 1, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 1, 1, 5, 1));
        LogicPropTable.Add("OpenMonitorPeriod", new Property("OpenMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("CloseMonitorPeriod", new Property("CloseMonitorPeriod", 1, 1, 30, 1));
        LogicPropTable.Add("TargetGap", new Property("TargetGap", 10, 1, 10, 1));
        LogicPropTable.Add("OpenSignalGap", new Property("OpenSignalGap", 0, 1, 10, 1));
        LogicPropTable.Add("MaxLongVolume", new Property("MaxLongVolume", 30, 40, 500, 50));
        LogicPropTable.Add("MaxShortVolume", new Property("MaxShortVolume", -30, -500, -40, 50));
        LogicPropTable.Add("StopLoss", new Property("StopLoss", -3, -8, -2, 1));
        LogicPropTable.Add("StopGain", new Property("StopGain", 3.5, -8, -2, 1));
        LogicPropTable.Add("StopProfitLoss", new Property("StopProfitLoss", 0, -8, -2, 1));
        double openstarttimetrade = TimeSpan.Parse("14:20:00").TotalSeconds;
        double openendtimetrade = TimeSpan.Parse("16:20:00").TotalSeconds;
        double endtimetrade = TimeSpan.Parse("16:54:00").TotalSeconds;
        LogicPropTable.Add("OpenStartTimeTrade", new Property("OpenStartTimeTrade", openstarttimetrade, openstarttimetrade, openstarttimetrade, 0));
        LogicPropTable.Add("OpenEndTimeTrade", new Property("OpenEndTimeTrade", openendtimetrade, openendtimetrade, openendtimetrade, 0));
        LogicPropTable.Add("EndTimeTrade", new Property("EndTimeTrade", endtimetrade, endtimetrade, endtimetrade, 0));
        LogicPropTable.Add("TotalLongVolPercent", new Property("TotalLongVolPercent", 35, -8, -2, 1));
        LogicPropTable.Add("TotalShortVolPercent", new Property("TotalShortVolPercent", 35, -8, -2, 1));
        this.GlobalVarTable.Clear();
        this.InitVarTable();
    }
    public List<string> GetAdjustedPropertiesList()
    {
        List < string > propList = new List<string>();
        //propList.Add("LongGap");
        //propList.Add("ShortGap");
        //propList.Add("StopLoss");
        propList.Add("StopGain");
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

    }
    public override DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[6];
        col[0] = new DataColumn("datetime", Type.GetType("System.DateTime")); //eg. 9:45 
        col[1] = new DataColumn("status", Type.GetType("System.String"));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("openlong", Type.GetType("System.Double")); //price
        col[3] = new DataColumn("openshort", Type.GetType("System.Double"));//price
        col[4] = new DataColumn("closelong", Type.GetType("System.Double"));//price
        col[5] = new DataColumn("closeshort", Type.GetType("System.Double"));//price

        dt.Columns.AddRange(col);
        return dt;
    }
    public override void UpdateHistoricalTable(DataTable dt)
    {
        HistoricalTable = dt;
    }
    public override DataRow FeedTickTrade(DataRow resamplingtick, string portstatus, double onhandcost, double onhandvol)
    {

        //Add a resampling tick to HistoricalTable
        //HistoricalTable.Rows.Add(resamplingtick.ItemArray);

        if (portstatus == "none")
        {
            double longprice = this.OpenLongStatus(onhandcost);
            double shortprice = this.OpenShortStatus(onhandcost);
            //Not hit any condition
            if ((longprice == 0) && (shortprice == 0))
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if ((longprice > 0) && (shortprice > 0))
                this.SignalTable.Rows.Add(this.GetSingnal("dup", longprice).ItemArray);//longprice = shortprice
            else if (longprice > 0) //if Long
                this.SignalTable.Rows.Add(this.GetSingnal("openlong", longprice).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("openshort", shortprice).ItemArray);
        }
        else if (portstatus == "closelong")
        {
            double shortprice = this.CloseLongStatus(onhandcost);
            //Not hit any condition
            if (shortprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (shortprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closelong", shortprice).ItemArray);
        }
        else if (portstatus == "closeshort")
        {
            double longprice = this.CloseShortStatus(onhandcost);
            //Not hit any condition
            if (longprice == 0)
                this.SignalTable.Rows.Add(this.GetSingnal("none", 0).ItemArray);
            else if (longprice > 0)//if Short
                this.SignalTable.Rows.Add(this.GetSingnal("closeshort", longprice).ItemArray);
        }
        return this.SignalTable.Rows[this.SignalTable.Rows.Count - 1];
    }
    private DataRow GetSingnal(string status, double price)
    {
        DataRow sigrow = this.NewSignalTable().NewRow();
        sigrow["status"] = status;
        sigrow["datetime"] = ((DateTime)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Date"]).Add((TimeSpan)this.HistoricalTable.Rows[this.HistoricalTable.Rows.Count - 1]["Time"]);
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

    public override double OpenLongStatus(double onhandcost)
    {

        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


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
            GlobalVarTable["ReverseShort"] = "0";
            if (GlobalVarTable["ReverseShort"] == "1")
                found = Convert.ToInt32(this.LogicPropTable["SignalLong"].Value);
            else
                for (int m = 2; m < counthistrow; m++)
                {
                    double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                    double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                    double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                    double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                    double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                    double daylogwprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                    double biglotclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Close"];
                    double totallongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalLongVolume"];
                    double totalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"];

                    double biglothigh = (Double)this.HistoricalTable.Rows[counthistrow - m]["High"];
                    double biglotlow = (Double)this.HistoricalTable.Rows[counthistrow - m]["Low"];
                    tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];


                    if ((biglotssumlongvol > tlongvol) && (biglotssumlongtrans > tlongtrans))
                    {
                        /*if ((tmpnewshorthigh == 0) || tmpnewshorthigh < biglothigh)
                        {
                            tmpnewshorthigh = biglothigh;
                        }*/
                        if (tmpclose - tlonggap > biglothigh)
                            found++;
                    }else if ((biglotssumshortvol < tshortvol) && (biglotssumshorttrans > tshorttrans))
                    {
                        /*if ((tmpnewshorthigh == 0) || tmpnewshorthigh < biglothigh)
                        {
                            tmpnewshorthigh = biglothigh;
                        }*/
                        if (tmpclose - tshortgap > biglothigh)
                            found++;
                    }




                }//Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalLong"].Value))
            {
                price = open;
            }
        }
        return price;
    }
    public override double OpenShortStatus(double onhandcost)
    {
        //Monitor n period if TotalLongVolume > x then open
        int counthistrow = this.HistoricalTable.Rows.Count;
        int found = 0;
        double price = 0;//not hit any condition


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
            GlobalVarTable["ReverseLong"] = "0";
            if (GlobalVarTable["ReverseLong"] == "1")
                found = Convert.ToInt32(this.LogicPropTable["SignalShort"].Value);
            else

                for (int m = 2; m < counthistrow; m++)
                {
                    double biglotssumshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortVol Biglots"];
                    double biglotssumlongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongVol Biglots"];
                    double biglotssumshorttrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumShortTrans Biglots"];
                    double biglotssumlongtrans = (Double)this.HistoricalTable.Rows[counthistrow - m]["SumLongTrans Biglots"];
                    double dayhighprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayHigh"];
                    double daylogwprice = (Double)this.HistoricalTable.Rows[counthistrow - m]["DayLow"];
                    double biglotclose = (Double)this.HistoricalTable.Rows[counthistrow - m]["Close"];
                    double totallongvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalLongVolume"];
                    double totalshortvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["TotalShortVolume"];

                    double biglothigh = (Double)this.HistoricalTable.Rows[counthistrow - m]["High"];
                    double biglotlow = (Double)this.HistoricalTable.Rows[counthistrow - m]["Low"];
                    tmpvol = (Double)this.HistoricalTable.Rows[counthistrow - m]["Volume"];

                    if ((biglotssumshortvol < tshortvol) && (biglotssumshorttrans > tlongtrans))
                    {
                        /*tmpcount++;
                        if ((tmpnewlonglow == 0) || tmpnewlonglow > biglotlow)
                            tmpnewlonglow = biglotlow;*/
                        if (tmpclose + tshortgap < biglotlow)
                            found++;
                    }else if ((biglotssumlongvol > tlongvol) && (biglotssumlongtrans > tlongtrans))
                    {
                        /*tmpcount++;
                        if ((tmpnewlonglow == 0) || tmpnewlonglow > biglotlow)
                            tmpnewlonglow = biglotlow;*/
                        if (tmpclose + tlonggap < biglotlow)
                            found++;
                    }




                }//Check how many time hit condition
            if ((found >= this.LogicPropTable["SignalShort"].Value))
            {
                price = open;
            }
        }
        return price;
    }

    public override double CloseLongStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }

        if (price > 0)
        {
            this.ClearVarTable();
            if (stoplossprice > 0)
                GlobalVarTable["ReverseShort"] = "1";
        }

        return price;
    }
    public override double CloseShortStatus(double onhandcost)
    {
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
            double close = (Double)this.HistoricalTable.Rows[counthistrow - 1]["Close"];
            TimeSpan time = (TimeSpan)this.HistoricalTable.Rows[counthistrow - 1]["Time"];
            TimeSpan endtime = TimeSpan.FromSeconds((Double)this.LogicPropTable["EndTimeTrade"].Value);
            if (time >= endtime)
                price = close;
        }
        if (price > 0)
        {
            this.ClearVarTable();
            if (stoplossprice > 0)
                GlobalVarTable["ReverseLong"] = "1";
        }
        return price;
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
                profit = close - onhandcost;
            else if (positionside == "openshort")
                profit = onhandcost - close;
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
                stopgainprice = close;
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
}
