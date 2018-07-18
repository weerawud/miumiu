using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

class UnitTest
{
    
    public UnitTest(TradeLogic logic)
    {
        Dictionary<string, Property> logicprop = logic.LogicPropTable;
        Dictionary<string, double[]> logicproprange = new Dictionary<string, double[]>();
        foreach (KeyValuePair<string, Property> kv in logicprop)
        {
            logicproprange.Add(kv.Key,kv.Value.GetValueRange());
        }
    }
}
class TradeUnit
{
    public Portfolio port;
    public TradeLogic tradelogic;
    public Order tradeorder;
    DataRow resampdr;
    public string symbol;
    DataTable UnitTestSummaryTable;
    double _maxonhandvol;
    
    public TradeUnit(TradeLogic logic,string accountname,string symbol,double maxonhandvol)
    {
        UnitTestSummaryTable = new DataTable();
        port = new Portfolio(accountname);
        tradelogic = logic;
        tradeorder = new Order();
        this.symbol = symbol;
        _maxonhandvol = 1;
        if (maxonhandvol > 1)
        {
            _maxonhandvol = maxonhandvol;
            tradelogic.MaxOnhandVol = _maxonhandvol;
        }
        resampdr= Feeder.NewResamplingDataTable().NewRow();
    }
    public TradeUnit(TradeLogic _logic, string _symbol, Portfolio _port)
    {
        UnitTestSummaryTable = new DataTable();
        port = _port;
        tradelogic = _logic;
        tradeorder = new Order();
        this.symbol = _symbol;
        resampdr = Feeder.NewResamplingDataTable().NewRow();
    }
    public TradeUnit(TradeLogic _logic, string _symbol,ref Portfolio _port)
    {
        UnitTestSummaryTable = new DataTable();
        port = _port;
        tradelogic = _logic;
        tradeorder = new Order();
        this.symbol = _symbol;
        resampdr = Feeder.NewResamplingDataTable().NewRow();
    }
    public double MaxOnHandVol
    {
        get
        {
            return this._maxonhandvol;
        }
        set
        {
            tradelogic.MaxOnhandVol = value;
            this._maxonhandvol = value;
        }
    }
    /*
    public void Trade(DataRow tick,Feeder feed)
    {
        DataRow dr = tick;
        tradelogic.UpdateHistoricalTable(feed.ResamplingDataTable);
        if (tradelogic.HistoricalTable.Rows.Count > 1)
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
                signaldr = tradelogic.FeedTickTrade(resampdr, "closelong", onhandcost, onhandvol);

            }
            else if (shortstatus == "openshort")
            {
                double onhandcost = port.GetOnhandCost("openshort", symbol);
                double onhandvol = port.GetOnhandVolume("openshort", symbol);
                signaldr = tradelogic.FeedTickTrade(resampdr, "closeshort", onhandcost, onhandvol);

            }
            else signaldr = tradelogic.FeedTickTrade(resampdr, "none", 0, 0);
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
    */
    public void OnData(DataRow tick, Feeder feed)
    {
        DataRow dr = tick;
        tradelogic.UpdateHistoricalTable(feed.ResamplingDataTable);
        if (tradelogic.HistoricalTable.Rows.Count > 1)
        {
            

            //)2 check port status by input close price
            resampdr.ItemArray = feed.GetResamplingBar(dr["<DATE>"].ToString(), dr["<TIME>"].ToString()).ItemArray;
            //3) check return status from tradelogic
            string s;
            if (port.VolOnhand(symbol) > 0)
                s = "0";
            DataRow signaldr = tradelogic.OnData(symbol,resampdr, ref port);
            

            string signalstatus = signaldr["status"].ToString();

            if (signalstatus == "openlong" || signalstatus == "openshort" || signalstatus == "closelong" || signalstatus == "closeshort")
            {

                DataRow transdr = tradeorder.GetTransaction(dr["<DATE>"].ToString(), dr["<TIME>"].ToString(), "TFEX", symbol, signaldr, port.PortfolioTable.NewRow());
                //port.UpdatePortfolioTable(transdr, symbol);
                port.UpdatePortfolioTradeTransaction(transdr, symbol);
                //dt.Rows.Add(transdr.ItemArray);

            }
        }
        else
            port.GetPortfolioSummary(symbol); //first time summary port when load first tick
    }

    public DataTable GetPortSummaryDetails()
    {
        DataTable dt = Portfolio.NewPortfolioTradeDetails(port.GetAccountName());
        foreach (DataRow r in port.GetPortfolioTradeDetails(symbol).Rows)
            dt.Rows.Add(r.ItemArray);
        return dt;
    }
}
