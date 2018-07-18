using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Globalization;
class Order
{
   
    private double _tradevol;
    private double _tfexcomm;
    private double _setcomm;
    public Order()
    {
        _tradevol = 1;
        _tfexcomm = 1500;
        _setcomm = 0.0015;
    }
    public Double TradeVolume
    {
        get
        {
            return _tradevol;
        }set
        {
            _tradevol = value;
        }
    }
    public DataRow GetTransaction(string tickdate,string ticktime,string market,string symbol,DataRow signaldr,DataRow portdr)
    {
        DateTime date = this.ToDateTime(tickdate,"yyyyMMdd");
        double price=0;

        portdr["datetime"] = date.Add(TimeSpan.Parse(ticktime));//"datetime"
        portdr["market"] = market;//"market"
        portdr["symbol"] = symbol;//"symbol"
        portdr["status"] = signaldr["status"].ToString();
        portdr["volume"] = signaldr["tradevol"];
        price = Double.Parse(signaldr[portdr["status"].ToString()].ToString()); //status must be openlong/short, closelong/short
        portdr["price"] = price;

        if(market == "TFEX")
            portdr["commision"] = _tfexcomm * _tradevol;
        else portdr["commision"] = _setcomm * _tradevol * price;
        return portdr;
    }
    private DateTime ToDateTime(string strdate, string format)
    {
        return DateTime.ParseExact(strdate, format, CultureInfo.InvariantCulture);
    }
    private string ToDateTime(DateTime dtDate, string format)
    {
        return dtDate.ToString(format, CultureInfo.InvariantCulture);
    }
}