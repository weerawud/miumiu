using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using ICSharpCode.SharpZipLib.Zip;



namespace Miumiu
{
    public partial class FormMain : Form
    {
        private DataHandlerMSSQL dmsql;
        private int interval, period;
        private string startdate;
        private string enddate;
        private string symbol;
        private List<string> listdate;
        private Logs eventlogs;
        //Bot bot;
        //private Thread runThread;

        public FormMain()
        {
            InitializeComponent();

            this.UpdateInit("PTT");
            
            
            //Set Technical Datagridview



        }
        private void UpdateInit(string ticker)
        {
            //private variable
            period = 180;
            interval = 5;
            symbol = ticker;
            eventlogs = new Logs(SynchronizationContext.Current);
            //Binding
            this.textBoxEventLog.DataBindings.Add("Text", eventlogs, "EventLogs");
            
            string constring = @"server="+System.Environment.MachineName+@"\WiiSQL;Initial Catalog=AssetDB;User ID=sa;Password=wee@dcs.c0m";
            dmsql = new DataHandlerMSSQL(constring);
            //Set Start & End Date
            listdate = new List<string>();
            DataTable dt = dmsql.GetDatesBarsTable(symbol);
            if (dt.Rows.Count > 0)
            {

                foreach (DataRow r in dt.Rows)
                {
                    listdate.Add(r[0].ToString());
                    comboBoxStartDate.Items.Add(r[0]);
                    comboBoxEndDate.Items.Add(r[0]);

                }
                comboBoxStartDate.SelectedIndex = comboBoxStartDate.Items.Count - 1;
                startdate = comboBoxStartDate.Text;
                comboBoxEndDate.SelectedIndex = comboBoxEndDate.Items.Count - 1;
                enddate = comboBoxEndDate.Text;
            }
            //Set Interval Timeframe
            comboBoxTimeframeTimeframeCal.SelectedIndex = 1;
            //Set start symbol
            dt = dmsql.CheckExistingTable();
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow r in dt.Rows)
                {
                    comboBoxSymbols.Items.Add(r["TABLE_NAME"].ToString().Replace("Intraday", ""));
                }
                comboBoxSymbols.Sorted = true;
                comboBoxSymbols.Text = symbol;
            }
            //Set Technical Period Combobox
            DataTable techdt = new DataTable();
            DataColumn[] col = new DataColumn[2];
            col[0] = new DataColumn("Name", typeof(System.String));
            col[1] = new DataColumn("Period", typeof(System.Double));
            techdt.Columns.AddRange(col);
            string[] indic = { "PeriodHigh", "PeriodLow", "PeriodGapHighLow", "TotalLongVolumePerTime", "TotalShortVolumePerTime", "TotalVolumePerTime", "Change%", "Volatility", "SumLongVolume%", "SumShortVolume%", "GapHighLow", "DayHigh", "DayLow" };
            for (int i = 0; i < indic.Length; i++)
            {

                techdt.Rows.Add(techdt.NewRow());
                techdt.Rows[i]["Name"] = indic[i];
                techdt.Rows[i]["Period"] = period;
            }
            dataGridViewTechnicalCal.DataSource = techdt;

            for (int i = 0; i < 3601; i++)
                comboBoxTimeframeTechCal.Items.Add(i.ToString());
            comboBoxTimeframeTechCal.Text = period.ToString();
        }
        private void WriteLog(string log)
        {
            //Thread.Sleep(TimeSpan.FromSeconds(5));
            string logline = String.Format("{0}: {1} ", DateTime.Now.ToLongTimeString(), log);
            string textlog = logline + Environment.NewLine + this.textBoxDataLogs.Text;
            //this.textBoxEventLog.Invoke(new MethodInvoker(() => this.textBoxEventLog.Text = textlog));
            this.textBoxDataLogs.Invoke(new MethodInvoker(() => textBoxDataLogs.Text = textlog));


        }
        private void WriteLog2(string log)
        {
            //Thread.Sleep(TimeSpan.FromSeconds(5));
            string logline = String.Format("{0}: {1} ", DateTime.Now.ToLongTimeString(), log);
            string textlog = logline + Environment.NewLine + this.textBoxEventLog.Text;
            //this.textBoxEventLog.Invoke(new MethodInvoker(() => this.textBoxEventLog.Text = textlog));
            this.textBoxEventLog.Invoke(new MethodInvoker(() => textBoxEventLog.Text = textlog));


        }

        private void button1_Click(object sender, EventArgs e)
        {

            //dmsql.GetBars("S50H15");
            //var S50M15=dmsql.GetBarsTable("S50M15","20150514");
            //interval = 5;
            WriteLog("Start Query: " + DateTime.Now.ToLongTimeString());
            //var PTT = dmsql.GetBarsTable("PTT", "20150604");
            var stock = dmsql.GetBarsTable(symbol, startdate, enddate);
            //var PTT = dmsql.GetBars("S50M15", "20150614");
            WriteLog("End Query: " + DateTime.Now.ToLongTimeString());
            WriteLog(stock.Rows.Count.ToString());
            
            
            //filter only require volume
            var filterstock = stock.Clone();
            foreach (DataRow r in stock.Rows)
            {
                if (Double.Parse(r["<VOL>"].ToString()) > Double.Parse(this.comboBoxVolFilter.Text))
                    filterstock.Rows.Add(r.ItemArray);
            }
            stock = filterstock;
            
            //end fileter require volume
            /*
            WriteLog("Start Get QueryTable: " + DateTime.Now.ToLongTimeString());
            
            DataTable dt = dmsql.GetQueryTable(PTT);
            WriteLog("End Get QueryTable: " + DateTime.Now.ToLongTimeString());
            var test = (from DataRow dr in dt.Rows
                        
                        select new
                        {
                            Price = Math.Round((Double)dr["Close"]),
                            Date = (DateTime)dr["Date"]
                        });
            */
            //this.dataGridViewDataResult1.DataSource = test.CopyToDataTable();
            if (stock.Rows.Count > 0)
            {
                WriteLog("Create Intraday Table: " + DateTime.Now.ToLongTimeString());
                var intstock = dmsql.GetResamplingBarsTable(interval, stock);
                WriteLog("Finish Intraday Table: " + DateTime.Now.ToLongTimeString());
                WriteLog(intstock.Rows.Count.ToString());
                this.dataGridViewDataResult1.DataSource = intstock;
                this.dataGridViewDataResult2.DataSource = QuantQuoteTickData(intstock);

                WriteLog("Calculate Technical Indic: " + DateTime.Now.ToLongTimeString());
                string[] indic = { "PeriodHigh", "PeriodLow", "PeriodGapHighLow", "TotalLongVolumePerTime", "TotalShortVolumePerTime", "TotalVolumePerTime", "Change%", "Volatility", "SumLongVolume%", "SumShortVolume%", "GapHighLow", "DayHigh", "DayLow" };
                var techStock = dmsql.CalculateTechnicalIndicator(intstock, indic, period);
                WriteLog("Finish Calculate Table: " + DateTime.Now.ToLongTimeString());
                WriteLog(intstock.Rows.Count.ToString());
                this.dataGridViewDataResult1.DataSource = techStock;
            }
            else
            {
                WriteLog("Non query data");
            }


            /*
            WriteLog("Calculate Hash: " + DateTime.Now.ToLongTimeString());
            this.dataGridViewDataResult1.DataSource = dmsql.GetClassifiedTable(techPTT);
            WriteLog("Finish Hash: " + DateTime.Now.ToLongTimeString());
            WriteLog(intstock.Rows.Count.ToString());
            */

        }
        private DataTable QuantQuoteTickData(DataTable dt)
        {
            DataTable QuantQuoteDT = new DataTable();
            DataColumn[] col = new DataColumn[7];
            col[0] = new DataColumn("TIME", typeof(System.Decimal));
            col[1] = new DataColumn("OPEN", typeof(System.Decimal));
            col[2] = new DataColumn("HIGH", typeof(System.Decimal));
            col[3] = new DataColumn("LOW", typeof(System.Decimal));
            col[4] = new DataColumn("CLOSE", typeof(System.Decimal));
            col[5] = new DataColumn("VOLUME", typeof(System.Decimal));
            QuantQuoteDT.Columns.AddRange(col);
            foreach(DataRow dr in dt.Rows)
            {
                DataRow tmpdr = QuantQuoteDT.NewRow();

                

                tmpdr["TIME"] = ((TimeSpan)dr["Time"]).TotalMilliseconds;
                tmpdr["OPEN"] = (Double)dr["Open"]*10000;
                tmpdr["HIGH"] = (Double)dr["High"]*10000;
                tmpdr["LOW"] = (Double)dr["Low"]*10000;
                tmpdr["CLOSE"] = (Double)dr["Close"]*10000;
                tmpdr["VOLUME"] = (Double)dr["TotalLongVolume"]+ Math.Abs((Double)dr["TotalLongVolume"]);
                QuantQuoteDT.Rows.Add(tmpdr);
            }
            return QuantQuoteDT;
        }

        private void button2_Click(object sender, EventArgs e)
        {

            DataTable dtResult1 = (DataTable)dataGridViewDataResult1.DataSource;
            DataTable dtResult2 = dmsql.GetBestTrade(dtResult1, 4, 1.5, interval);
            this.dataGridViewDataResult2.DataSource = dtResult2;
            DataTable dtResult3 = dmsql.GetPrevTrade(dtResult1, dtResult2, 5);
            this.dataGridViewDataResult3.DataSource = dtResult3;
        }

        private void buttonExportToExcel_Click(object sender, EventArgs e)
        {
            //dmsql.SaveResamplingBarsTableToCSV("S50H16", "20160315", "20160315", 15, @"C:\Users\Administrator\Desktop\test.csv");
            
            saveFileDialogExport.Filter = "Intraday files (*.csv}|*.csv";

            //saveFileDialogExport.FileName = symbol.ToUpper+"_SECOND"+ DateTime.Now.ToString("yyyyMMdd") + "-" + "MiuExport" + ".xls";
            saveFileDialogExport.FileName = symbol.ToUpper() + "_SECOND_TRADE.csv";
            if (saveFileDialogExport.ShowDialog() == DialogResult.OK)
            {
                if(tabControlDataResult.SelectedIndex==0)
                    this.ToExcel(saveFileDialogExport.FileName, dataGridViewDataResult1);
                else if(tabControlDataResult.SelectedIndex == 1)
                {
                    this.ToCSV(saveFileDialogExport.FileName, dataGridViewDataResult2);
                }


            }
        }
        private void ToExcel(string filename, DataGridView dGV)
        {
            string stOutput = "";
            // Export titles:
            string sHeaders = "";

            for (int j = 0; j < dGV.Columns.Count; j++)
                //sHeaders = sHeaders.ToString() + Convert.ToString(dGV.Columns[j].HeaderText) + "\t";
                sHeaders = sHeaders.ToString() + Convert.ToString(dGV.Columns[j].HeaderText) + ",";
            stOutput += sHeaders + "\r\n";
            // Export data.
            for (int i = 0; i < dGV.RowCount - 1; i++)
            {
                string stLine = "";
                for (int j = 0; j < dGV.Rows[i].Cells.Count; j++)
                    //stLine = stLine.ToString() + Convert.ToString(dGV.Rows[i].Cells[j].Value) + "\t";
                    stLine = stLine.ToString() + Convert.ToString(dGV.Rows[i].Cells[j].Value) + ",";
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
        private void ToCSV(string filename, DataGridView dGV)
        {
            string stOutput = "";
            // Export titles:
            string sHeaders = "";
            /*
            for (int j = 0; j < dGV.Columns.Count; j++)
                //sHeaders = sHeaders.ToString() + Convert.ToString(dGV.Columns[j].HeaderText) + "\t";
                sHeaders = sHeaders.ToString() + Convert.ToString(dGV.Columns[j].HeaderText) + ",";
            stOutput += sHeaders + "\r\n";
            */
            // Export data.
            for (int i = 0; i < dGV.RowCount - 1; i++)
            {
                string stLine = "";
                for (int j = 0; j < dGV.Rows[i].Cells.Count; j++)
                    //stLine = stLine.ToString() + Convert.ToString(dGV.Rows[i].Cells[j].Value) + "\t";
                    stLine = stLine.ToString() + Convert.ToString(dGV.Rows[i].Cells[j].Value) + ",";
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

        private void ToCSV(string filename, DataTable table)
        {
            string stOutput = "";
            // Export titles:
            string sHeaders = "";
            /*
            for (int j = 0; j < dGV.Columns.Count; j++)
                //sHeaders = sHeaders.ToString() + Convert.ToString(dGV.Columns[j].HeaderText) + "\t";
                sHeaders = sHeaders.ToString() + Convert.ToString(dGV.Columns[j].HeaderText) + ",";
            stOutput += sHeaders + "\r\n";
            */
            // Export data.
            for (int i = 0; i < table.Rows.Count - 1; i++)
            {
                string stLine = "";
                for (int j = 0; j < table.Columns.Count; j++)
                    //stLine = stLine.ToString() + Convert.ToString(dGV.Rows[i].Cells[j].Value) + "\t";
                    stLine = stLine.ToString() + Convert.ToString(table.Rows[i][j].ToString()) + ",";
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




        private void comboBoxTimeframeTimeframeCal_SelectedIndexChanged(object sender, EventArgs e)
        {
            interval = Int32.Parse(comboBoxTimeframeTimeframeCal.Text);
            double pval = period * interval;
            string sec = pval.ToString();
            string min = string.Format("{0:N2}", pval / 60);
            labelPeriod.Text = "Period (" + sec + " sec" + " | " + min + " min)";
        }

        private void comboBoxStartDate_SelectedIndexChanged(object sender, EventArgs e)
        {
            startdate = comboBoxStartDate.Text;
            comboBoxEndDate.Items.Clear();
            int sindex = listdate.IndexOf(startdate);
            for (int i = sindex; i < listdate.Count; i++)
                comboBoxEndDate.Items.Add(listdate[i]);
        }

        private void comboBoxEndDate_SelectedIndexChanged(object sender, EventArgs e)
        {
            enddate = comboBoxStartDate.Text;
            comboBoxStartDate.Items.Clear();
            int eindex = listdate.IndexOf(enddate);
            for (int i = 0; i <= eindex; i++)
                comboBoxStartDate.Items.Add(listdate[i]);
        }

        private void comboBoxSymbols_SelectedIndexChanged(object sender, EventArgs e)
        {
            symbol = comboBoxSymbols.Text;
            //Clear Start-EndDate
            comboBoxEndDate.Items.Clear();
            comboBoxStartDate.Items.Clear();
            //Set Start & End Date
            
            listdate = new List<string>();
            DataTable dt = dmsql.GetDatesBarsTable(symbol);
            if (dt.Rows.Count > 0)
            {

                foreach (DataRow r in dt.Rows)
                {
                    listdate.Add(r[0].ToString());
                    comboBoxStartDate.Items.Add(r[0]);
                    comboBoxEndDate.Items.Add(r[0]);

                }
                comboBoxStartDate.SelectedIndex = comboBoxStartDate.Items.Count - 1;
                startdate = comboBoxStartDate.Text;
                comboBoxEndDate.SelectedIndex = comboBoxEndDate.Items.Count - 1;
                enddate = comboBoxEndDate.Text;
            }
        }

        private void comboBoxTimeframeTechCal_SelectedIndexChanged(object sender, EventArgs e)
        {
            period = Int32.Parse(comboBoxTimeframeTechCal.Text);
            double pval = period * interval;
            string sec = pval.ToString();
            string min = string.Format("{0:N2}", pval / 60);
            labelPeriod.Text = "Period (" + sec + " sec" + " | " + min + " min)";
            //Change technical period
            DataTable dt = (DataTable)dataGridViewTechnicalCal.DataSource;
            foreach (DataRow r in dt.Rows)
                r["Period"] = period.ToString();
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            /*
            BackgroundWorker bg = new BackgroundWorker();
            //bg.DoWork += new DoWorkEventHandler(bg_DoWork);
            bg.DoWork += new DoWorkEventHandler(bg_DoWork);
            bg.RunWorkerAsync();
            */
            
            BackgroundWorker bg1 = new BackgroundWorker();
            //bg.DoWork += new DoWorkEventHandler(bg_DoWork);
            bg1.DoWork += new DoWorkEventHandler(bg_DoWork1);
            bg1.RunWorkerAsync();
            
            
            /*
            BackgroundWorker bg2 = new BackgroundWorker();
            //bg.DoWork += new DoWorkEventHandler(bg_DoWork);
            bg2.DoWork += new DoWorkEventHandler(bg_DoWork2);
            bg2.RunWorkerAsync();
            
            BackgroundWorker bg3 = new BackgroundWorker();
            //bg.DoWork += new DoWorkEventHandler(bg_DoWork);
            bg3.DoWork += new DoWorkEventHandler(bg_DoWork3);
            bg3.RunWorkerAsync();
            
            BackgroundWorker bg4 = new BackgroundWorker();
            //bg.DoWork += new DoWorkEventHandler(bg_DoWork);
            bg4.DoWork += new DoWorkEventHandler(bg_DoWork4);
            bg4.RunWorkerAsync();
            
            /*
            BackgroundWorker bg5 = new BackgroundWorker();
            //bg.DoWork += new DoWorkEventHandler(bg_DoWork);
            bg5.DoWork += new DoWorkEventHandler(bg_DoWork5);
            bg5.RunWorkerAsync();

            BackgroundWorker bg6 = new BackgroundWorker();
            //bg.DoWork += new DoWorkEventHandler(bg_DoWork);
            bg6.DoWork += new DoWorkEventHandler(bg_DoWork6);
            bg6.RunWorkerAsync();

            BackgroundWorker bg7 = new BackgroundWorker();
            //bg.DoWork += new DoWorkEventHandler(bg_DoWork);
            bg7.DoWork += new DoWorkEventHandler(bg_DoWork7);
            bg7.RunWorkerAsync();

            */
        }
        /*
        void bg_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            //this.SimTrade(15,"16:05","16:55"); best
            this.SimTrade(interval, "09:45", "16:55");
        }*/
        void bg_DoWork1(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            //this.SimTrade(15,"16:05","16:55"); best
            //this.SimTrade(30, "09:45", "16:55","S50U16");
            //this.SimTrade(1,"20160201","20160331", "10:30", "16:55", "S50H16

            //testsymbol.Add("S50M16");
            //this.SimTrade(1, "20160201", "20160331", "10:30", "16:55", testsymbol);
            List<string> testsymbol = new List<string>();
            testsymbol.Add("S50M17");
            this.SimTrade(10, "20170401", "20170631", "09:45", "16:55", testsymbol,1,0); //lookback = ignore this value
            //this.SimTrade(5, "20151001", "20151231", "10:00", "16:55", testsymbol, 1, 300);
        }
        
        void bg_DoWork2(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            //this.SimTrade(15,"16:05","16:55"); best
            //this.SimTrade(15, "14:15", "16:55", "S50M16");
            //this.SimTrade(1, "20160401", "20160631", "09:45", "16:55", "S50M16");
            List<string> testsymbol = new List<string>();
            testsymbol.Add("S50M16");
            this.SimTrade(0, "20160101", "20180331", "09:45", "12:30", testsymbol, 1, 300);
        }
        void bg_DoWork3(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            //this.SimTrade(15,"16:05","16:55"); best
            //this.SimTrade(15, "14:15", "16:55", "S50H16");
            //this.SimTrade(1, "20160701", "20160931", "09:45", "16:55", "S50U16");
            List<string> testsymbol = new List<string>();
            testsymbol.Add("S50U16");
            this.SimTrade(30, "20160101", "20180331", "09:45", "12:30", testsymbol, 1, 300);
        }
        void bg_DoWork4(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            //this.SimTrade(15,"16:05","16:55"); best
            //this.SimTrade(15, "14:15", "16:55", "S50H16");
            //this.SimTrade(1, "20161001", "20161231", "09:45", "16:55", "S50Z16");
            List<string> testsymbol = new List<string>();
            testsymbol.Add("S50H17");
            this.SimTrade(30, "20160101", "20180331", "09:45", "12:30", testsymbol, 1, 300);
        }
        /*


        void bg_DoWork5(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            //this.SimTrade(15,"16:05","16:55"); best
            this.SimTrade(15, "14:15", "16:55", "S50U15");
        }
        void bg_DoWork6(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            //this.SimTrade(15,"16:05","16:55"); best
            this.SimTrade(15, "14:15", "16:55", "S50M15");
        }
        void bg_DoWork7(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            //this.SimTrade(15,"16:05","16:55"); best
            this.SimTrade(15, "14:15", "16:55", "S50H15");
        }
        */
        
        
        void SimTrade(int interval, string sdate, string edate,string stime,string etime, List<string> testsymbol,double maxonhandvol,int lookbackperiod)
        {
            //List<string> ldate = new List<string>();
            //int interval = 15;
            //foreach (string s in comboBoxStartDate.Items)
            //    ldate.Add(s);


            List<string> ldate = new List<string>();
            foreach (string s in testsymbol)
            {
                DataTable dt = dmsql.GetDatesBarsTable(s);
                if (dt.Rows.Count > 0)
                {

                    foreach (DataRow r in dt.Rows)
                    {
                        if((Convert.ToInt32(r[0])>=Convert.ToInt32(sdate))&& (Convert.ToInt32(r[0]) <= Convert.ToInt32(edate)))
                            if (!ldate.Contains(r[0].ToString()))
                                ldate.Add(r[0].ToString());
                    }
                }
            }
            //interval = 30;
            //Bot bot = new Bot("highvoltrade", eventlogs, new HighVolumeTradeLogic(Feeder.NewResamplingDataTable(), interval), dmsql);
            //Bot bot = new Bot("lwhighvolshort", eventlogs, new LongWhenHighVolShortPercentTradeLogic(Feeder.NewResamplingDataTable(), interval), dmsql);
            Bot bot = new Bot("percentvol", eventlogs, new PercentVolTradeLogic(symbol, Feeder.NewResamplingDataTable(), interval), dmsql);
            bot.BindBackTestProgressBar(ref progressBarBackTest);
            //bot.SimulateTradeUnit(testsymbol, ldate, interval, sdate, edate,1);
            //bot.SimulateTradeUnitSequential(testsymbol, ldate,interval, stime, etime, maxonhandvol,lookbackperiod);
            bot.SimulateTradeUnitParallel(testsymbol, ldate, interval, stime, etime, maxonhandvol, lookbackperiod);
            //bot.SimulateBestTradeUnit(testsymbol, ldate, interval, sdate, edate,60,1);
            dataGridViewDataResult1.BeginInvoke((MethodInvoker)delegate () { dataGridViewDataResult1.DataSource = bot.simulatetestSummary; ; });
        }

        private void button1_Click_1(object sender, EventArgs e)
        {

            BackgroundWorker bg = new BackgroundWorker();
            //bg.DoWork += new DoWorkEventHandler(bg_DoWork);
            bg.DoWork += new DoWorkEventHandler(bg_DoWorkMT);
            bg.RunWorkerAsync();
            
            /*
            Parallel.ForEach(ldate, new ParallelOptions { MaxDegreeOfParallelism = 4 },
            eachldate =>
            {
                mm.ExtractChart("S50M16", "Close", eachldate, eachldate, 10, 30);
            }
            );*/


            //mm.SetPathListToRegistry(@"D:\Miumiugo\raw\","raw");
            //mm.SetPathListToRegistry(@"D:\Miumiugo\norm\", "norm");
            //string rawpath = mm.GetPathListFromRegistry("raw");
            //string normpth = mm.GetPathListFromRegistry("norm");
            //mm.NormalizeData("S50M16", "20160405", "20160405", 300);
        }
        void bg_DoWorkMT(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            //this.SimTrade(15,"16:05","16:55"); best
            this.DoExtractChart();
        }
        private void DoExtractChart()
        {
            /*Property p = new Property("test", 5, 5, 100, 5);
            double[] x = p.GetValueRange();*/
            List<string> ldate = new List<string>();
            DataTable dt = dmsql.GetDatesBarsTable("S50U16");
            if (dt.Rows.Count > 0)
            {

                foreach (DataRow r in dt.Rows)
                {
                    if(Convert.ToInt64(r[0]) > 20160600)
                        ldate.Add(r[0].ToString());
                }
            }
            Miumiugo mm = new Miumiugo(eventlogs);
            mm.ExtractChart("S50U16", "Close", ldate, 60, 30,0.9);
            
        }
        void bg_DoWorkSearch(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            //this.SimTrade(15,"16:05","16:55"); best
            this.DoSearch();
        }
        private void DoSearch()
        {
            List<string> ldate = new List<string>();
            DataTable dt = dmsql.GetDatesBarsTable("STA");
            
            if (dt.Rows.Count > 0)
            {

                foreach (DataRow r in dt.Rows)
                {
                    ldate.Add(r[0].ToString());
                }
            }
            
            //ldate.Add("20160826");
            Bot bot = new Bot("searchdaily", eventlogs, new SearchDailyLogic(interval), dmsql);
            bot.BindBackTestProgressBar(ref progressBarBackTest);
            //bot.SimulateTradeSeach("S50U16", ldate, 60, "09:45", "16:55");
            List<string> symbollist = new List<string>();
            //symbollist.Add("PTT");
            symbollist.Add("STA");
            //symbollist.Add("PT");
            //symbollist.Add("S50M17");
            //bot.SimulateDailyTradeSeach(symbollist, "09:45:00", "16:55:00");
            bot.SearchDailyParallel(symbollist, ldate, 1, "09:45:00", "16:55:00", 0, 0);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            BackgroundWorker bg = new BackgroundWorker();
            bg.DoWork += new DoWorkEventHandler(bg_DoWorkSearch);
            bg.RunWorkerAsync();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //
            BackgroundWorker bg1 = new BackgroundWorker();
            //bg.DoWork += new DoWorkEventHandler(bg_DoWork);
            bg1.DoWork += new DoWorkEventHandler(bg_DoExportTick);
            bg1.RunWorkerAsync();
        }

        void bg_DoExportTick(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            exportTick();
        }
        void exportTick()
        {
            
            //Create symbol date list
            Dictionary<string, List<string>> symbolListDateDict = new Dictionary<string, List<string>>();
            
            List<string> slist = new List<string>();
            foreach (object o in comboBoxSymbols.Items)
                slist.Add(o.ToString());
            Parallel.ForEach(slist, new ParallelOptions { MaxDegreeOfParallelism = 8 },
                    s =>
                    {

                        WriteLog2("Start create symbol-date dict : " + s);
                        List<string> tmplistdate = new List<string>();
                        DataTable dt = dmsql.GetDatesBarsTable(s);
                        if (dt.Rows.Count > 0)
                        {

                            foreach (DataRow r in dt.Rows)
                            {
                                tmplistdate.Add(r[0].ToString());

                            }
                        }
                        symbolListDateDict.Add(s, tmplistdate);
                        WriteLog2("Completed create symbol-date dict : " + s);
                    }
                    );
            //End Create symbol date list
            
            Parallel.ForEach(slist, new ParallelOptions { MaxDegreeOfParallelism = 4 },
                    s =>
                    {
                        foreach(string d in symbolListDateDict[s])
                        {
                            string tmpsymbol = s;
                            string tmpdate = d;

                            string rootpath = @"C:\Users\weerawud\Documents\Visual Studio 2015\Projects\Lean-master-prev\Data\equity\usa\second";
                            string symbolpath = rootpath + @"\" + tmpsymbol.ToLower();

                            if (!Directory.Exists(symbolpath))
                            {
                                Directory.CreateDirectory(symbolpath);
                            }
                            string symbolfilename = symbolpath + @"\" + tmpdate + "_" + tmpsymbol.ToLower() + "_trade_second.csv";
                            string zipfilename = symbolpath + @"\" + tmpdate + "_trade.zip";

                            WriteLog2("Start query get tick data : " + tmpsymbol);
                            var tmpTickDT = dmsql.GetBarsTable(tmpsymbol, tmpdate);
                            WriteLog2("End Query: " + DateTime.Now.ToLongTimeString());
                            WriteLog2(tmpTickDT.Rows.Count.ToString());

                            if (tmpTickDT.Rows.Count > 0)
                            {
                                WriteLog2("Create Intraday Table: " + DateTime.Now.ToLongTimeString());
                                var intstock = dmsql.GetResamplingBarsTable(interval, tmpTickDT);
                                WriteLog2("Finish Intraday Table: " + DateTime.Now.ToLongTimeString());
                                WriteLog2(intstock.Rows.Count.ToString());
                                DataTable tmpResampTickTable = QuantQuoteTickData(intstock);
                                ToCSV(symbolfilename, tmpResampTickTable);
                                WriteLog2("Completed export to CSV : " + symbolfilename);

                                WriteLog2("Start Zip : " + symbolfilename);
                                ZipData(symbolfilename, zipfilename);
                                if (File.Exists(symbolfilename))
                                {
                                    File.Delete(symbolfilename);
                                }
                                WriteLog2("Completed Zip : " + symbolfilename);
                            }
                        }
                    }
                    );
                          
            //Check and Create directory
        }

        private void textBoxEventLog_TextChanged(object sender, EventArgs e)
        {
            this.textBoxEventLog.Update();
        }
        public static bool ZipData(string inputfilename, string zipfilename)
        {
            try
            {
                
                //Create our output
                using (var stream = new ZipOutputStream(File.Create(zipfilename)))
                {
                    stream.SetLevel(9);
                    //Create the space in the zip file:
                    var entry = new ZipEntry(Path.GetFileName(inputfilename));
                    var data = inputfilename;
                    //var bytes = Encoding.Default.GetBytes(data);
                    stream.PutNextEntry(entry);

                    byte[] buffer = new byte[4096];
                    using (FileStream fs = File.OpenRead(data))
                    {
                        int sourceBytes;
                        do
                        {
                            sourceBytes = fs.Read(buffer, 0, buffer.Length);
                            stream.Write(buffer, 0, sourceBytes);
                        } while (sourceBytes > 0);

                    }
                
                    stream.CloseEntry();
                    //Close stream:
                    stream.Finish();
                    stream.Close();
                } // End Using
            }
            catch (Exception err)
            {
                return false;
            }
            return true;
        }
        private delegate void AddProgressValueDelegate();
        private void AddProgressValue()
        {
            progressBarBackTest.PerformStep();
        }

        private delegate void SetProgressMaximumDelegate(int max);
        private void SetProgressMaximum(int max)
        {
            progressBarBackTest.Value = 0;
            progressBarBackTest.Maximum = max;
            progressBarBackTest.Refresh();
        }

    }
}
