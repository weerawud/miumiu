using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

class SearchLogic
{

    Dictionary<DateTime, DataTable> ResamplingTableDict;
    Dictionary<DateTime, DataTable> TradeReturnDict;
    Dictionary<string, Property> LogicPropTable;

    int _resamplingInterval;
    public SearchLogic()
    {
        
    }

    public void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("ExpectReturn", new Property("ExpectReturn", 5, 5, 20, 1));
        /*
        double nexttradetime = TimeSpan.Parse("00:15:00").TotalSeconds;
        LogicPropTable.Add("NextTradeTime", new Property("NextTradeTime", nexttradetime, nexttradetime, nexttradetime, 0));
        LogicPropTable.Add("TimeLookBack", new Property("TimeLookBack", 240, 100, 100, 100));
        double targettimetrade = TimeSpan.Parse("16:00:00").TotalSeconds;
        LogicPropTable.Add("TargetTimeTrade", new Property("TargetTimeTrade", targettimetrade, targettimetrade, targettimetrade, 0));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 1, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 1, 1, 5, 1));
        */
    }

    public void SearchTradeReturnTable(DataTable resamplingTable)
    {

        //Store resamplingTable to Dict, remove if already exist
        DateTime tradeDate = (DateTime)resamplingTable.Rows[0]["Date"];
        if (ResamplingTableDict.ContainsKey(tradeDate))
            ResamplingTableDict.Remove(tradeDate);
        ResamplingTableDict.Add(tradeDate, resamplingTable);

        //Check existing Trade Return dict
        if (TradeReturnDict.ContainsKey(tradeDate))
            TradeReturnDict.Remove(tradeDate);
        TradeReturnDict.Add(tradeDate, NewSignalTable());

        var tmplongReturn = NewSignalTable();
        var tmpshortReturn = NewSignalTable();
        //Loop for get all trade pair
        foreach(DataRow dr in resamplingTable.Rows)
        {
            //Get return trade pair add to
            tmplongReturn = TradeLongReturnTable(dr);
            tmpshortReturn = TradeShortReturnTable(dr);
            if (tmplongReturn.Rows.Count > 0)
            {
                
                foreach (DataRow tradeRow in tmplongReturn.Rows)
                {
                    TradeReturnDict[tradeDate].Rows.Add(tradeRow.ItemArray);
                }
                
            }
            if (tmpshortReturn.Rows.Count > 0)
            {

                foreach (DataRow tradeRow in tmpshortReturn.Rows)
                {
                    TradeReturnDict[tradeDate].Rows.Add(tradeRow.ItemArray);
                }

            }

        }
        //If none trade pair then remove
        if (TradeReturnDict[tradeDate].Rows.Count == 0)
        {
            TradeReturnDict.Remove(tradeDate);
            ResamplingTableDict.Remove(tradeDate);
        }

    }
    private DataTable TradeLongReturnTable(DataRow openrow)
    {
        DataTable dt = NewSignalTable();
        DataTable resamplingTable = ResamplingTableDict[(DateTime)openrow["Date"]];
        TimeSpan opentime = (TimeSpan)openrow["Time"];
        double openprice = (double)openrow["Open"];
        double expectreturn = (double)LogicPropTable["ExpectReturn"].Value;
        double tradeloss = 0;
        foreach (DataRow dr in resamplingTable.Rows)
        {
            //loop for get all return trade which more than expect return
            double closeprice,tradereturn;
            if ((TimeSpan)dr["Time"] > opentime)
            {
                closeprice = (double)dr["Open"];
                tradereturn = closeprice-openprice;
                if ((tradeloss == 0) || (tradereturn < tradeloss))
                    tradeloss = tradereturn;
                if (tradereturn > expectreturn)
                {
                    DataRow newTradeSignalRow = dt.NewRow();
                    newTradeSignalRow["datetime"] = (DateTime)dr["Date"];
                    newTradeSignalRow["status"] = "openlong"; 
                    newTradeSignalRow["opentime"] = opentime;
                    newTradeSignalRow["openlong"] = openprice;
                    newTradeSignalRow["closetime"] = (TimeSpan)dr["Time"];
                    newTradeSignalRow["closelong"] = closeprice;
                    newTradeSignalRow["return"] = tradereturn;
                    newTradeSignalRow["lowestreturn"] = tradeloss;
                    dt.Rows.Add(newTradeSignalRow);
                }
            }
        }
        return dt;
    }
    private DataTable TradeShortReturnTable(DataRow openrow)
    {
        DataTable dt = NewSignalTable();
        DataTable resamplingTable = ResamplingTableDict[(DateTime)openrow["Date"]];
        TimeSpan opentime = (TimeSpan)openrow["Time"];
        double openprice = (double)openrow["Open"];
        double expectreturn = (double)LogicPropTable["ExpectReturn"].Value;
        double tradeloss = 0;
        foreach (DataRow dr in resamplingTable.Rows)
        {
            //loop for get all return trade which more than expect return
            double closeprice, tradereturn;
            if ((TimeSpan)dr["Time"] > opentime)
            {
                closeprice = (double)dr["Open"];
                tradereturn =  openprice- closeprice;
                if ((tradeloss==0)||(tradereturn < tradeloss))
                    tradeloss = tradereturn;
                if (tradereturn > expectreturn)
                {
                    DataRow newTradeSignalRow = dt.NewRow();
                    newTradeSignalRow["datetime"] = (DateTime)dr["Date"];
                    newTradeSignalRow["status"] = "openshort";
                    newTradeSignalRow["opentime"] = opentime;
                    newTradeSignalRow["openshort"] = openprice;
                    newTradeSignalRow["closetime"] = (TimeSpan)dr["Time"];
                    newTradeSignalRow["closeshort"] = closeprice;
                    newTradeSignalRow["return"] = tradereturn;
                    newTradeSignalRow["lowestreturn"] = tradeloss;
                    dt.Rows.Add(newTradeSignalRow);
                }
            }
        }
        return dt;
    }
    public DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[11];
        col[0] = new DataColumn("datetime", typeof(System.DateTime)); //eg. 9:45 
        col[1] = new DataColumn("status", typeof(System.String));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("opentime", typeof(System.TimeSpan)); //time
        col[3] = new DataColumn("openlong", typeof(System.Double)); //price
        col[4] = new DataColumn("openshort", typeof(System.Double));//price
        col[5] = new DataColumn("closetime", typeof(System.TimeSpan)); //time
        col[6] = new DataColumn("closelong", typeof(System.Double));//price
        col[7] = new DataColumn("closeshort", typeof(System.Double));//price
        col[8] = new DataColumn("return", typeof(System.Double));//price
        col[9] = new DataColumn("highloss", typeof(System.Double));//price
        dt.Columns.AddRange(col);
        return dt;
    }
}


class SearchDailyLogic : SearchLogic
{

    Dictionary<DateTime, DataTable> ResamplingTableDict;
    Dictionary<DateTime, DataTable> TradeReturnDict;
    Dictionary<string, Property> LogicPropTable;

    int _resamplingInterval;
    public SearchDailyLogic(int interval)
    {
        TradeReturnDict = new Dictionary<DateTime, DataTable>();
        ResamplingTableDict = new Dictionary<DateTime, DataTable>();
        LogicPropTable = new Dictionary<string, Property>();
        //Store value in global private
        _resamplingInterval = interval;

        //InitLogicPropTable
        this.InitLogicPropTable(_resamplingInterval);
    }

    public void InitLogicPropTable(int interval)
    {

        LogicPropTable.Clear();
        LogicPropTable.Add("Interval", new Property("Interval", interval, 15, 3600, 15));
        LogicPropTable.Add("ExpectReturn", new Property("ExpectReturn", 5, 5, 20, 1));
        /*
        double nexttradetime = TimeSpan.Parse("00:15:00").TotalSeconds;
        LogicPropTable.Add("NextTradeTime", new Property("NextTradeTime", nexttradetime, nexttradetime, nexttradetime, 0));
        LogicPropTable.Add("TimeLookBack", new Property("TimeLookBack", 240, 100, 100, 100));
        double targettimetrade = TimeSpan.Parse("16:00:00").TotalSeconds;
        LogicPropTable.Add("TargetTimeTrade", new Property("TargetTimeTrade", targettimetrade, targettimetrade, targettimetrade, 0));
        LogicPropTable.Add("SignalLong", new Property("SignalLong", 1, 1, 5, 1));
        LogicPropTable.Add("SignalShort", new Property("SignalShort", 1, 1, 5, 1));
        */
    }

    public void SearchTradeReturnTable(DataTable resamplingTable)
    {
        /*
        //Store resamplingTable to Dict, remove if already exist
        DateTime tradeDate = (DateTime)resamplingTable.Rows[0]["<DATE>"];
        if (ResamplingTableDict.ContainsKey(tradeDate))
            ResamplingTableDict.Remove(tradeDate);
        ResamplingTableDict.Add(tradeDate, resamplingTable);

        //Check existing Trade Return dict
        if (TradeReturnDict.ContainsKey(tradeDate))
            TradeReturnDict.Remove(tradeDate);
        TradeReturnDict.Add(tradeDate, NewSignalTable());

        var tmplongReturn = NewSignalTable();
        var tmpshortReturn = NewSignalTable();
        //Loop for get all trade pair
        foreach (DataRow dr in resamplingTable.Rows)
        {
            //Get return trade pair add to
            tmplongReturn = TradeLongReturnTable(dr);
            tmpshortReturn = TradeShortReturnTable(dr);
            if (tmplongReturn.Rows.Count > 0)
            {

                foreach (DataRow tradeRow in tmplongReturn.Rows)
                {
                    TradeReturnDict[tradeDate].Rows.Add(tradeRow.ItemArray);
                }

            }
            if (tmpshortReturn.Rows.Count > 0)
            {

                foreach (DataRow tradeRow in tmpshortReturn.Rows)
                {
                    TradeReturnDict[tradeDate].Rows.Add(tradeRow.ItemArray);
                }

            }

        }
        //If none trade pair then remove
        if (TradeReturnDict[tradeDate].Rows.Count == 0)
        {
            TradeReturnDict.Remove(tradeDate);
            ResamplingTableDict.Remove(tradeDate);
        }
        */

    }
    private DataTable TradeLongReturnTable(DataRow openrow)
    {
        DataTable dt = NewSignalTable();
        DataTable resamplingTable = ResamplingTableDict[(DateTime)openrow["Date"]];
        TimeSpan opentime = (TimeSpan)openrow["Time"];
        double openprice = (double)openrow["Open"];
        double expectreturn = (double)LogicPropTable["ExpectReturn"].Value;
        double tradeloss = 0;
        foreach (DataRow dr in resamplingTable.Rows)
        {
            //loop for get all return trade which more than expect return
            double closeprice, tradereturn;
            if ((TimeSpan)dr["Time"] > opentime)
            {
                closeprice = (double)dr["Open"];
                tradereturn = closeprice - openprice;
                if ((tradeloss == 0) || (tradereturn < tradeloss))
                    tradeloss = tradereturn;
                if (tradereturn > expectreturn)
                {
                    DataRow newTradeSignalRow = dt.NewRow();
                    newTradeSignalRow["datetime"] = (DateTime)dr["Date"];
                    newTradeSignalRow["status"] = "openlong";
                    newTradeSignalRow["opentime"] = opentime;
                    newTradeSignalRow["openlong"] = openprice;
                    newTradeSignalRow["closetime"] = (TimeSpan)dr["Time"];
                    newTradeSignalRow["closelong"] = closeprice;
                    newTradeSignalRow["return"] = tradereturn;
                    newTradeSignalRow["lowestreturn"] = tradeloss;
                    dt.Rows.Add(newTradeSignalRow);
                }
            }
        }
        return dt;
    }
    private DataTable TradeShortReturnTable(DataRow openrow)
    {
        DataTable dt = NewSignalTable();
        DataTable resamplingTable = ResamplingTableDict[(DateTime)openrow["Date"]];
        TimeSpan opentime = (TimeSpan)openrow["Time"];
        double openprice = (double)openrow["Open"];
        double expectreturn = (double)LogicPropTable["ExpectReturn"].Value;
        double tradeloss = 0;
        foreach (DataRow dr in resamplingTable.Rows)
        {
            //loop for get all return trade which more than expect return
            double closeprice, tradereturn;
            if ((TimeSpan)dr["Time"] > opentime)
            {
                closeprice = (double)dr["Open"];
                tradereturn = openprice - closeprice;
                if ((tradeloss == 0) || (tradereturn < tradeloss))
                    tradeloss = tradereturn;
                if (tradereturn > expectreturn)
                {
                    DataRow newTradeSignalRow = dt.NewRow();
                    newTradeSignalRow["datetime"] = (DateTime)dr["Date"];
                    newTradeSignalRow["status"] = "openshort";
                    newTradeSignalRow["opentime"] = opentime;
                    newTradeSignalRow["openshort"] = openprice;
                    newTradeSignalRow["closetime"] = (TimeSpan)dr["Time"];
                    newTradeSignalRow["closeshort"] = closeprice;
                    newTradeSignalRow["return"] = tradereturn;
                    newTradeSignalRow["lowestreturn"] = tradeloss;
                    dt.Rows.Add(newTradeSignalRow);
                }
            }
        }
        return dt;
    }
    public DataTable NewSignalTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[11];
        col[0] = new DataColumn("datetime", typeof(System.DateTime)); //eg. 9:45 
        col[1] = new DataColumn("status", typeof(System.String));//none,openlong,openshort,closelong,closeshort
        col[2] = new DataColumn("opentime", typeof(System.TimeSpan)); //time
        col[3] = new DataColumn("openlong", typeof(System.Double)); //price
        col[4] = new DataColumn("openshort", typeof(System.Double));//price
        col[5] = new DataColumn("closetime", typeof(System.TimeSpan)); //time
        col[6] = new DataColumn("closelong", typeof(System.Double));//price
        col[7] = new DataColumn("closeshort", typeof(System.Double));//price
        col[8] = new DataColumn("return", typeof(System.Double));//price
        col[9] = new DataColumn("highloss", typeof(System.Double));//price
        dt.Columns.AddRange(col);
        return dt;
    }
}