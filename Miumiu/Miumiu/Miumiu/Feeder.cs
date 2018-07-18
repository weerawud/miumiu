using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Globalization;
using Accord.Statistics;

//Input tick, Output resample
class Feeder
{
    private Dictionary<double, List<DataRow>> priceDict;
    public Dictionary<DateTime, DataRow> ResamplingDict;
    private Dictionary<DateTime, List<DataRow>> RawTickDict;
    public DataTable ResamplingDataTable;
    private int interval;
    private string date;
    private double _dayhigh;
    private double _daylow;
    private double _sumlongvol;
    private double _sumshortvol;
    public Dictionary<string, List<DataRow>> LongBigLotsDict;
    public Dictionary<string, List<DataRow>> ShortBigLotsDict;
    public Feeder(int second,string tradedate)
    {
        //Initial
        interval = second;
        date = tradedate;
        _dayhigh = 0;
        _daylow = 0;
        _sumlongvol = 0;
        _sumshortvol = 0;
        this.priceDict = new Dictionary<double, List<DataRow>>();
        this.RawTickDict = new Dictionary<DateTime, List<DataRow>>();
        this.ResamplingDataTable = Feeder.NewResamplingDataTable();
        
        //Check interval time and then create intraday Dict for store tick data
        if (second > 0)
        {
            this.ResamplingDict = this.IntradayTimeTradeDict(second,tradedate);
            
        }
        //Biglots
        LongBigLotsDict = new Dictionary<string, List<DataRow>>();
        ShortBigLotsDict = new Dictionary<string, List<DataRow>>();
    }
    //FullResamplingTick
    /*
    public DataRow ResamplingTick(DataRow tickdr)
    {
        
        
        //Prepare DataTable for create DataRow for tradetime Dict
        DataTable tmptradedt = new DataTable();
        DataColumn[] tradecol = new DataColumn[4];
        tradecol[0] = new DataColumn("Close", Type.GetType("System.Double"));
        tradecol[1] = new DataColumn("Volume", Type.GetType("System.Double"));
        tradecol[2] = new DataColumn("Value", Type.GetType("System.Double"));
        tradecol[3] = new DataColumn("Date", Type.GetType("System.DateTime"));
        tmptradedt.Columns.AddRange(tradecol);

        //TimeSpan tradetime = TimeSpan.Parse(r["<TIME>"].ToString());
        DateTime tradetime = this.ToDateTime(tickdr["<DATE>"].ToString(), "yyyyMMdd").Add(TimeSpan.Parse(tickdr["<TIME>"].ToString()));
        //DateTime tradedate = (DateTime)r["<DATE>"].
        DataRow tradedr = tmptradedt.NewRow();
        //Round up to most nearest interval
        //tradetime.TimeOfDay
        tradedr["Date"] = tradetime;
        tradetime = tradetime.Date + this.RoundTo(tradetime.TimeOfDay, interval);
        DataRow tmprow = ResamplingDict[tradetime];
        tmprow.Table.TableName = tickdr["<TICKER>"].ToString();
        double tmpprice = Double.Parse(tickdr["<CLOSE>"].ToString());
        double tmpvol = 0;
        if (tickdr["<VOL>"].ToString().TrimEnd(null).EndsWith("M"))
            tmpvol = 1000000 * Double.Parse(tickdr["<VOL>"].ToString().Remove(tickdr["<VOL>"].ToString().LastIndexOf('M'), 1));
        else tmpvol = Double.Parse(tickdr["<VOL>"].ToString());

        //negative volume in short side
        if (tickdr["<OPENINT>"].ToString().Trim() == "S")
            tmpvol = -1 * tmpvol;
        //Caculate value1
        double tmpval = tmpprice * tmpvol;

        tradedr["Close"] = tmpprice;
        tradedr["Volume"] = tmpvol;
        tradedr["Value"] = tmpval;

        //Add price to price dict
        if (!RawTickDict.ContainsKey(tradetime))
            RawTickDict.Add(tradetime, new List<DataRow>());
        RawTickDict[tradetime].Add(tradedr);

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

        //Add Date & Time
        tmprow["Date"] = tradetime.Date;
        tmprow["Time"] = tradetime.TimeOfDay;
        if (RawTickDict[tradetime].Count > 0)
        {


            //Loop for cal each transaction in one tradetime period
            List<Double> tradeClose = new List<double>();
            List<Double> tradeVolume = new List<double>();
            List<Double> tradeValue = new List<double>();
            Dictionary<string, List<DataRow>> LongBigLotsDict = new Dictionary<string, List<DataRow>>();
            Dictionary<string, List<DataRow>> ShortBigLotsDict = new Dictionary<string, List<DataRow>>();
            int tmplongtps = 0;
            int tmpshortps = 0;
            foreach (DataRow r in RawTickDict[tradetime])
            {
                
                double tmpvoltps = (Double)r["Volume"];
                if (tmpvoltps > 0)
                {
                    tmplongtps = tmplongtps + 1;
                    //for biglots
                    if (!LongBigLotsDict.ContainsKey(r["Date"].ToString()))
                        LongBigLotsDict.Add(r["Date"].ToString(), new List<DataRow>());
                    LongBigLotsDict[r["Date"].ToString()].Add(r);
                }
                else if (tmpvoltps < 0)
                {
                    tmpshortps = tmpshortps + 1;
                    //for biglots
                    if (!ShortBigLotsDict.ContainsKey(r["Date"].ToString()))
                        ShortBigLotsDict.Add(r["Date"].ToString(), new List<DataRow>());
                    ShortBigLotsDict[r["Date"].ToString()].Add(r);
                }
                tradeClose.Add((Double)r["Close"]);
                tradeVolume.Add(tmpvoltps);
                tradeValue.Add((Double)r["Value"]);
            }
            //Start Find Biglot > 100 contact per sec &&
            double totallongtmpbtvol = 0;
            double totallongtmpbttrans = 0;
            double totallongtmpbtval = 0;
            double totallongtmpbtcost = 0;
            foreach (KeyValuePair<string,List<DataRow>> kv in LongBigLotsDict)
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

            tmprow["SumShortVol Biglots"] = totalshorttmpbtvol;
            tmprow["SumLongVol Biglots"] = totallongtmpbtvol;
            tmprow["SumShortTrans Biglots"] = totalshorttmpbttrans;
            tmprow["SumLongTrans Biglots"] = totallongtmpbttrans;
            tmprow["AvgShortCost Biglots"] = totalshorttmpbtcost;
            tmprow["AvgLongCost Biglots"] = totallongtmpbtcost;
            //End BigLot

            //Add number transaction per interval
            tmprow["Long TPS"] = tmplongtps;
            tmprow["Short TPS"] = -1*tmpshortps;
            //Calculate Standard Deviation
            tmprow["SD"] = Math.Round(Tools.StandardDeviation(tradeClose.ToArray(), false), 3);

            //Calculate Max Long/Short Volume
            if (tradeVolume.Max() >= 0)
                tmprow["MaxLongVolume"] = tradeVolume.Max();
            else tmprow["MaxLongVolume"] = 0;
            if (tradeVolume.Min() < 0)
                tmprow["MaxShortVolume"] = tradeVolume.Min();
            else tmprow["MaxShortVolume"] = 0;

            //Calculate Max Long/Short Value
            if (tradeValue.Max() >= 0)
                tmprow["MaxLongValue"] = tradeValue.Max();
            else tmprow["MaxLongValue"] = 0;
            if (tradeValue.Min() < 0)
                tmprow["MaxShortValue"] = tradeValue.Min();
            else tmprow["MaxShortValue"] = 0;

            //Calculate Total Long/Short Volume
            //Clear previous for recalculate
            tmprow["TotalLongVolume"] = 0;
            tmprow["TotalShortVolume"] = 0;
            foreach (double vol in tradeVolume)
            {
                if (vol >= 0) //Long
                    tmprow["TotalLongVolume"] = (Double)tmprow["TotalLongVolume"] + vol;
                else //Short
                    tmprow["TotalShortVolume"] = (Double)tmprow["TotalShortVolume"] + vol;
            }

            //Calculate Total Long/Short Value
            //Clear previous for recalculate
            tmprow["TotalLongValue"] = 0;
            tmprow["TotalShortValue"] = 0;
            foreach (double val in tradeValue)
            {
                if (val >= 0) //Long
                    tmprow["TotalLongValue"] = (Double)tmprow["TotalLongValue"] + val;
                else //Short
                    tmprow["TotalShortValue"] = (Double)tmprow["TotalShortValue"] + val;
            }
            //Calculate %Volume Long/short
            double abstotalvol = (Double)tmprow["TotalLongVolume"] + Math.Abs((Double)tmprow["TotalShortVolume"]);
            tmprow["%Volume Long"] = Math.Round(100 * (Double)tmprow["TotalLongVolume"] / abstotalvol, 2);
            tmprow["%Volume Short"] = Math.Round(100 * Math.Abs((Double)tmprow["TotalShortVolume"] / abstotalvol), 2);

            //Calculate %Value Long/short
            double abstotalval = (Double)tmprow["TotalLongValue"] + Math.Abs((Double)tmprow["TotalShortValue"]);
            tmprow["%Value Long"] = Math.Round(100 * (Double)tmprow["TotalLongValue"] / abstotalval, 2);
            tmprow["%Value Short"] = Math.Round(100 * Math.Abs((Double)tmprow["TotalShortValue"] / abstotalval), 2);

            //Calculate Volume:TransactionFrequncy
            double vf = ((Double)tmprow["TotalLongVolume"] + Math.Abs((Double)tmprow["TotalShortVolume"])) / tradeVolume.Count();
            tmprow["VF"] = Math.Round(vf, 2);

            //prepare sum Long/Short Volume
            if(tmpvol>0)
                _sumlongvol = _sumlongvol + tmpvol;
            else if(tmpvol<0)
                _sumshortvol = _sumshortvol + Math.Abs(tmpvol);
            double tmpsumvol = _sumlongvol + _sumshortvol;
            tmprow["SumShortVolume%"] = Math.Round(100*_sumshortvol/tmpsumvol,2);
            tmprow["SumLongVolume%"] = Math.Round(100*_sumlongvol / tmpsumvol, 2);
            tmprow["SumTotalVolume"] = tmpsumvol;

            //Calculate DayHigh/Low
            double periodhigh = tradeClose.Max();
            double periodlow = tradeClose.Min();
            //Initial for first Resampling Row
            if (_dayhigh == 0)
                _dayhigh = periodhigh;
            if (_daylow == 0)
                _daylow = periodlow;
            
            if ((periodhigh > 0) && (periodhigh > _dayhigh))
            {
                tmprow["DayHigh"] = periodhigh;
                _dayhigh = periodhigh;
            }
            else tmprow["DayHigh"] = _dayhigh;

            if ((periodlow > 0) && (periodlow < _daylow))
            {
                tmprow["DayLow"] = periodlow;
                _daylow = periodlow;
            }
            else tmprow["DayLow"] = _daylow;
        }
        //reassign back
        this.ResamplingDict[tradetime] = tmprow;
        //
        int resampcount = ResamplingDataTable.Rows.Count;
        //Check if row count =0 add or if row > 0 del and add
        if (resampcount > 0)
            if ((TimeSpan)ResamplingDataTable.Rows[resampcount - 1]["Time"] == tradetime.TimeOfDay)
            {
                ResamplingDataTable.Rows.RemoveAt(resampcount - 1);

            }
        
        
        ResamplingDataTable.Rows.Add(tmprow.ItemArray);
        Feeder.AddLookBackData(ResamplingDataTable);
        //return ResamplingDataTable.Rows[ResamplingDataTable.Rows.Count - 1];
        return tmprow;
    }
    */
    //Full ResamplingTick

    public DataRow ResamplingTick(DataRow tickdr)
    {


        //Prepare DataTable for create DataRow for tradetime Dict
        DataTable tmptradedt = new DataTable();
        DataColumn[] tradecol = new DataColumn[4];
        tradecol[0] = new DataColumn("Close", Type.GetType("System.Double"));
        tradecol[1] = new DataColumn("Volume", Type.GetType("System.Double"));
        tradecol[2] = new DataColumn("Value", Type.GetType("System.Double"));
        tradecol[3] = new DataColumn("Date", Type.GetType("System.DateTime"));
        tmptradedt.Columns.AddRange(tradecol);

        //TimeSpan tradetime = TimeSpan.Parse(r["<TIME>"].ToString());
        DateTime tradetime = this.ToDateTime(tickdr["<DATE>"].ToString(), "yyyyMMdd").Add(TimeSpan.Parse(tickdr["<TIME>"].ToString()));
        //DateTime tradedate = (DateTime)r["<DATE>"].
        DataRow tradedr = tmptradedt.NewRow();
        //Round up to most nearest interval
        //tradetime.TimeOfDay
        tradedr["Date"] = tradetime;
        tradetime = tradetime.Date + this.RoundTo(tradetime.TimeOfDay, interval);
        DataRow tmprow = ResamplingDict[tradetime];
        tmprow.Table.TableName = tickdr["<TICKER>"].ToString();
        double tmpprice = Double.Parse(tickdr["<CLOSE>"].ToString());
        double tmpvol = 0;
        if (tickdr["<VOL>"].ToString().TrimEnd(null).EndsWith("M"))
            tmpvol = 1000000 * Double.Parse(tickdr["<VOL>"].ToString().Remove(tickdr["<VOL>"].ToString().LastIndexOf('M'), 1));
        else tmpvol = Double.Parse(tickdr["<VOL>"].ToString());

        //negative volume in short side
        if (tickdr["<OPENINT>"].ToString().Trim() == "S")
            tmpvol = -1 * tmpvol;
        //Caculate value1
        double tmpval = tmpprice * tmpvol;

        tradedr["Close"] = tmpprice;
        tradedr["Volume"] = tmpvol;
        tradedr["Value"] = tmpval;

        //Add price to price dict
        if (!RawTickDict.ContainsKey(tradetime))
            RawTickDict.Add(tradetime, new List<DataRow>());
        RawTickDict[tradetime].Add(tradedr);

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

        //Add Date & Time
        tmprow["Date"] = tradetime.Date;
        tmprow["Time"] = tradetime.TimeOfDay;
        
        if (RawTickDict[tradetime].Count > 0)
        {

            
            //Loop for cal each transaction in one tradetime period
            List<Double> tradeClose = new List<double>();
            List<Double> tradeVolume = new List<double>();
            List<Double> tradeValue = new List<double>();
            Dictionary<string, List<DataRow>> LongBigLotsDict = new Dictionary<string, List<DataRow>>();
            Dictionary<string, List<DataRow>> ShortBigLotsDict = new Dictionary<string, List<DataRow>>();
            int tmplongtps = 0;
            int tmpshortps = 0;
            foreach (DataRow r in RawTickDict[tradetime])
            {

                double tmpvoltps = (Double)r["Volume"];
                if (tmpvoltps > 0)
                {
                    tmplongtps = tmplongtps + 1;
                    //for biglots
                    if (!LongBigLotsDict.ContainsKey(r["Date"].ToString()))
                        LongBigLotsDict.Add(r["Date"].ToString(), new List<DataRow>());
                    LongBigLotsDict[r["Date"].ToString()].Add(r);
                }
                else if (tmpvoltps < 0)
                {
                    tmpshortps = tmpshortps + 1;
                    //for biglots
                    if (!ShortBigLotsDict.ContainsKey(r["Date"].ToString()))
                        ShortBigLotsDict.Add(r["Date"].ToString(), new List<DataRow>());
                    ShortBigLotsDict[r["Date"].ToString()].Add(r);
                }
                tradeClose.Add((Double)r["Close"]);
                tradeVolume.Add(tmpvoltps);
                tradeValue.Add((Double)r["Value"]);
            }
            //Start Find Biglot > 100 contact per sec &&
            double totallongtmpbtvol = 0;
            double totallongtmpbttrans = 0;
            double totallongtmpbtval = 0;
            double totallongtmpbtcost = 0;
            int totallongtmpbigtrans = 0;
            
            foreach (KeyValuePair<string, List<DataRow>> kv in LongBigLotsDict)
            {
                double tmpbtvol = 0;
                double tmpbtval = 0;
                foreach (DataRow r in kv.Value)
                {
                    tmpbtvol = tmpbtvol + (Double)r["Volume"];
                    tmpbtval = tmpbtval + (Double)r["Value"];
                    if ((Double)r["Volume"] >= 100)// Check one transaction higher than 100
                        totallongtmpbigtrans++; 
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
            int totalshorttmpbigtrans = 0;
            foreach (KeyValuePair<string, List<DataRow>> kv in ShortBigLotsDict)
            {
                double tmpbtvol = 0;
                double tmpbtval = 0;
                foreach (DataRow r in kv.Value)
                {
                    tmpbtvol = tmpbtvol + (Double)r["Volume"];
                    tmpbtval = tmpbtval + (Double)r["Value"];
                    if((Double)r["Volume"] <= -100)// Check one transaction higher than 100
                        totalshorttmpbigtrans++;
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

            tmprow["SumShortVol Biglots"] = totalshorttmpbtvol;
            tmprow["SumLongVol Biglots"] = totallongtmpbtvol;
            tmprow["SumShortTrans Biglots"] = totalshorttmpbttrans;
            tmprow["SumLongTrans Biglots"] = totallongtmpbttrans;
            tmprow["AvgShortCost Biglots"] = totalshorttmpbtcost;
            tmprow["AvgLongCost Biglots"] = totallongtmpbtcost;
            tmprow["SumShortBigTrans Biglots"] = totalshorttmpbigtrans;
            tmprow["SumLongBigTrans Biglots"] = totallongtmpbigtrans;
            //End BigLot



            //Calculate Max Long/Short Volume
            if (tradeVolume.Max() >= 0)
                tmprow["MaxLongVolume"] = tradeVolume.Max();
            else tmprow["MaxLongVolume"] = 0;
            if (tradeVolume.Min() < 0)
                tmprow["MaxShortVolume"] = tradeVolume.Min();
            else tmprow["MaxShortVolume"] = 0;

           
            
            //Calculate Total Long/Short Volume
            //Clear previous for recalculate
            tmprow["TotalLongVolume"] = 0;
            tmprow["TotalShortVolume"] = 0;
            foreach (double vol in tradeVolume)
            {
                if (vol >= 0) //Long
                    tmprow["TotalLongVolume"] = (Double)tmprow["TotalLongVolume"] + vol;
                else //Short
                    tmprow["TotalShortVolume"] = (Double)tmprow["TotalShortVolume"] + vol;
            }

            
            //Calculate %Volume Long/short
            double abstotalvol = (Double)tmprow["TotalLongVolume"] + Math.Abs((Double)tmprow["TotalShortVolume"]);
            tmprow["%Volume Long"] = Math.Round(100 * (Double)tmprow["TotalLongVolume"] / abstotalvol, 2);
            tmprow["%Volume Short"] = Math.Round(100 * Math.Abs((Double)tmprow["TotalShortVolume"] / abstotalvol), 2);

            //prepare sum Long/Short Volume
            if (tmpvol > 0)
                _sumlongvol = _sumlongvol + tmpvol;
            else if (tmpvol < 0)
                _sumshortvol = _sumshortvol + Math.Abs(tmpvol);
            double tmpsumvol = _sumlongvol + _sumshortvol;
            tmprow["SumShortVolume%"] = Math.Round(100 * _sumshortvol / tmpsumvol, 2);
            tmprow["SumLongVolume%"] = Math.Round(100 * _sumlongvol / tmpsumvol, 2);
            tmprow["SumTotalVolume"] = tmpsumvol;

            //Calculate DayHigh/Low
            double periodhigh = tradeClose.Max();
            double periodlow = tradeClose.Min();
            //Initial for first Resampling Row
            if (_dayhigh == 0)
                _dayhigh = periodhigh;
            if (_daylow == 0)
                _daylow = periodlow;

            if ((periodhigh > 0) && (periodhigh > _dayhigh))
            {
                tmprow["DayHigh"] = periodhigh;
                _dayhigh = periodhigh;
            }
            else tmprow["DayHigh"] = _dayhigh;

            if ((periodlow > 0) && (periodlow < _daylow))
            {
                tmprow["DayLow"] = periodlow;
                _daylow = periodlow;
            }
            else tmprow["DayLow"] = _daylow;
        }
        //reassign back
        this.ResamplingDict[tradetime] = tmprow;
        //
        int resampcount = ResamplingDataTable.Rows.Count;
        //Check if row count =0 add or if row > 0 del and add
        if (resampcount > 0)
            if ((TimeSpan)ResamplingDataTable.Rows[resampcount - 1]["Time"] == tradetime.TimeOfDay)
            {
                ResamplingDataTable.Rows.RemoveAt(resampcount - 1);

            }


        ResamplingDataTable.Rows.Add(tmprow.ItemArray);
        //** lookback
        //Feeder.AddLookBackData(ResamplingDataTable);
        //** lookback
        //return ResamplingDataTable.Rows[ResamplingDataTable.Rows.Count - 1];
        return tmprow;
    }

    public DataTable GetResamplingTable()
    {
        DataTable dt = Feeder.NewResamplingDataTable();

        
        foreach (KeyValuePair<DateTime, DataRow> kvp in this.ResamplingDict)
        {
            if (!kvp.Value.IsNull("Close"))
                dt.Rows.Add(kvp.Value.ItemArray);
            //else 
            //    break;
        }

        return dt;
    }
    public DataRow GetResamplingBar(string tickdate,string ticktime)
    {
        DateTime tradetime = this.ToDateTime(tickdate, "yyyyMMdd").Add(TimeSpan.Parse(ticktime));
        tradetime = tradetime.Date + this.RoundTo(tradetime.TimeOfDay, interval);
        //this.ToDateTime(tradetime, "yyyyMMdd");
        return this.ResamplingDict[tradetime];   
    }
    public DataRow GetLatestResamplingBar()
    {
        return this.ResamplingDict.Values.Last();
    }
    public static DataTable NewResamplingDataTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[37];
        col[0] = new DataColumn("Date", Type.GetType("System.DateTime"));
        col[1] = new DataColumn("Time", Type.GetType("System.TimeSpan"));
        col[2] = new DataColumn("Open", Type.GetType("System.Double"));
        col[3] = new DataColumn("High", Type.GetType("System.Double"));
        col[4] = new DataColumn("Low", Type.GetType("System.Double"));
        col[5] = new DataColumn("Close", Type.GetType("System.Double"));
        col[6] = new DataColumn("Volume", Type.GetType("System.Double"));
        col[7] = new DataColumn("Value", Type.GetType("System.Double"));
        col[8] = new DataColumn("MaxLongVolume", Type.GetType("System.Double"));
        col[9] = new DataColumn("MaxShortVolume", Type.GetType("System.Double"));
        col[10] = new DataColumn("MaxLongValue", Type.GetType("System.Double"));
        col[11] = new DataColumn("MaxShortValue", Type.GetType("System.Double"));
        col[12] = new DataColumn("TotalLongVolume", Type.GetType("System.Double"));
        col[13] = new DataColumn("TotalShortVolume", Type.GetType("System.Double"));
        col[14] = new DataColumn("TotalLongValue", Type.GetType("System.Double"));
        col[15] = new DataColumn("TotalShortValue", Type.GetType("System.Double"));
        col[16] = new DataColumn("%Volume Long", Type.GetType("System.Double"));
        col[17] = new DataColumn("%Volume Short", Type.GetType("System.Double"));
        col[18] = new DataColumn("%Value Long", Type.GetType("System.Double"));
        col[19] = new DataColumn("%Value Short", Type.GetType("System.Double"));
        col[20] = new DataColumn("SD", Type.GetType("System.Double"));
        col[21] = new DataColumn("VF", Type.GetType("System.Double"));
        col[22] = new DataColumn("DayHigh", Type.GetType("System.Double"));
        col[23] = new DataColumn("DayLow", Type.GetType("System.Double"));
        col[24] = new DataColumn("Long TPS", Type.GetType("System.Double"));
        col[25] = new DataColumn("Short TPS", Type.GetType("System.Double"));
        col[26] = new DataColumn("SumShortVolume%", Type.GetType("System.Double"));
        col[27] = new DataColumn("SumLongVolume%", Type.GetType("System.Double"));
        col[28] = new DataColumn("SumTotalVolume", Type.GetType("System.Double"));
        col[29] = new DataColumn("SumShortVol Biglots", Type.GetType("System.Double"));
        col[30] = new DataColumn("SumLongVol Biglots", Type.GetType("System.Double"));
        col[31] = new DataColumn("SumShortTrans Biglots", Type.GetType("System.Double"));
        col[32] = new DataColumn("SumLongTrans Biglots", Type.GetType("System.Double"));
        col[33] = new DataColumn("SumShortBigTrans Biglots", Type.GetType("System.Double"));
        col[34] = new DataColumn("SumLongBigTrans Biglots", Type.GetType("System.Double"));
        col[35] = new DataColumn("AvgShortCost Biglots", Type.GetType("System.Double"));
        col[36] = new DataColumn("AvgLongCost Biglots", Type.GetType("System.Double"));
        dt.Columns.AddRange(col);
        /*
        //Add Lookback Column
        //Create pair dict
        Dictionary<string, List<string>> lpairDict = new Dictionary<string, List<string>>();
        lpairDict.Add("Close", new List<string> { "High", "Low", "Open", "Close" });
        lpairDict.Add("TotalLongVolume", new List<string> { "TotalLongVolume" });
        lpairDict.Add("TotalShortVolume", new List<string> { "TotalShortVolume" });
        dt = Feeder.AddLookBackColumn(dt, lpairDict, 3);
        //End add
        */
        return dt;
    }
        public static DataTable AddLookBackData(DataTable dt)
    {
        DataTable LookBackDataTable = dt;
        foreach (DataColumn col in LookBackDataTable.Columns)
        {
            if (col.ColumnName.Contains("lb:"))
            {
                string[] splitcolname = col.ColumnName.Split(':');//0=lb, 1=source,2=target,3=nothing,4=backperiod
                double backperiod = Double.Parse(splitcolname[4]);
            }
        }
        return LookBackDataTable;
    }
    public static DataTable AddLookBackColumn(DataTable dt,Dictionary<string,List<string>> colPairDict,int LookbackPeriod)
    {
        DataTable lookbackDT = dt;
        //loop for column compare pair lookback eg. Close-Open to Create new column
        for (int n = 1; n <= LookbackPeriod; n++)
        {
            foreach (KeyValuePair<string, List<string>> cpair in colPairDict)
            {
                foreach (string valuepair in cpair.Value)
                {
                    string paircolname = "lb:"+cpair.Key + ":" + valuepair + ":p=:" + (-1 * n).ToString();
                    lookbackDT.Columns.Add(paircolname, typeof(double));
                }
            }
        }
        return lookbackDT;
    }
    
    #region private method
    private Dictionary<DateTime, DataRow> IntradayTimeTradeDict(int secondinterval, string tradedate)
    {
        Dictionary<DateTime, DataRow> intraDict = new Dictionary<DateTime, DataRow>();
        if (secondinterval > 0)
        {
            TimeSpan interval = new TimeSpan(0, 0, secondinterval);
            //Start trade time
            TimeSpan startTradeTimeMorning = new TimeSpan(9, 45, 0);
            //End trade time
            TimeSpan endTradeTimeMorning = new TimeSpan(12, 30, 0);

            TimeSpan startTradeTimeAfternoon = new TimeSpan(14, 15, 0);
            TimeSpan endTradeTimeAfternoon = new TimeSpan(16, 55, 0);
            DataTable dt = Feeder.NewResamplingDataTable();
            /*DataColumn[] col = new DataColumn[22];
            col[0] = new DataColumn("Date", Type.GetType("System.DateTime"));
            col[1] = new DataColumn("Time", Type.GetType("System.TimeSpan"));
            col[2] = new DataColumn("Open", Type.GetType("System.Double"));
            col[3] = new DataColumn("High", Type.GetType("System.Double"));
            col[4] = new DataColumn("Low", Type.GetType("System.Double"));
            col[5] = new DataColumn("Close", Type.GetType("System.Double"));
            col[6] = new DataColumn("Volume", Type.GetType("System.Double"));
            col[7] = new DataColumn("Value", Type.GetType("System.Double"));
            col[8] = new DataColumn("MaxLongVolume", Type.GetType("System.Double"));
            col[9] = new DataColumn("MaxShortVolume", Type.GetType("System.Double"));
            col[10] = new DataColumn("MaxLongValue", Type.GetType("System.Double"));
            col[11] = new DataColumn("MaxShortValue", Type.GetType("System.Double"));
            col[12] = new DataColumn("TotalLongVolume", Type.GetType("System.Double"));
            col[13] = new DataColumn("TotalShortVolume", Type.GetType("System.Double"));
            col[14] = new DataColumn("TotalLongValue", Type.GetType("System.Double"));
            col[15] = new DataColumn("TotalShortValue", Type.GetType("System.Double"));
            col[16] = new DataColumn("%Volume Long", Type.GetType("System.Double"));
            col[17] = new DataColumn("%Volume Short", Type.GetType("System.Double"));
            col[18] = new DataColumn("%Value Long", Type.GetType("System.Double"));
            col[19] = new DataColumn("%Value Short", Type.GetType("System.Double"));
            col[20] = new DataColumn("SD", Type.GetType("System.Double"));
            col[21] = new DataColumn("VF", Type.GetType("System.Double"));
            dt.Columns.AddRange(col);*/
            DateTime tmpdate = this.ToDateTime(tradedate.ToString(), "yyyyMMdd");
            //Create Morning interval Dict
            while (startTradeTimeMorning <= endTradeTimeMorning)
            {
                intraDict.Add(tmpdate.Date + startTradeTimeMorning, dt.NewRow());
                startTradeTimeMorning = startTradeTimeMorning.Add(interval);
            }
            //Create Morning interval Dict
            while (startTradeTimeAfternoon <= endTradeTimeAfternoon)
            {
                intraDict.Add(tmpdate.Date + startTradeTimeAfternoon, dt.NewRow());
                startTradeTimeAfternoon = startTradeTimeAfternoon.Add(interval);
            }
        }
        return intraDict;

    }
    
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
