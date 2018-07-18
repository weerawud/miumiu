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

            this.UpdateInit("S50M16");
            
            
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
            col[0] = new DataColumn("Name", Type.GetType("System.String"));
            col[1] = new DataColumn("Period", Type.GetType("System.Double"));
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
            
            saveFileDialogExport.Filter = "Intraday files (*.xls}|*.xls";

            saveFileDialogExport.FileName = DateTime.Now.ToString("yyyyMMdd") + "-" + "MiuExport" + ".xls";
            if (saveFileDialogExport.ShowDialog() == DialogResult.OK)
            {
                this.ToExcel(saveFileDialogExport.FileName, dataGridViewDataResult1);


            }
        }
        private void ToExcel(string filename, DataGridView dGV)
        {
            string stOutput = "";
            // Export titles:
            string sHeaders = "";

            for (int j = 0; j < dGV.Columns.Count; j++)
                sHeaders = sHeaders.ToString() + Convert.ToString(dGV.Columns[j].HeaderText) + "\t";
            stOutput += sHeaders + "\r\n";
            // Export data.
            for (int i = 0; i < dGV.RowCount - 1; i++)
            {
                string stLine = "";
                for (int j = 0; j < dGV.Rows[i].Cells.Count; j++)
                    stLine = stLine.ToString() + Convert.ToString(dGV.Rows[i].Cells[j].Value) + "\t";
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

            BackgroundWorker bg5 = new BackgroundWorker();
            //bg.DoWork += new DoWorkEventHandler(bg_DoWork);
            bg5.DoWork += new DoWorkEventHandler(bg_DoWork5);
            bg5.RunWorkerAsync();

            BackgroundWorker bg6 = new BackgroundWorker();
            //bg.DoWork += new DoWorkEventHandler(bg_DoWork);
            bg6.DoWork += new DoWorkEventHandler(bg_DoWork6);
            bg6.RunWorkerAsync();
            */
            
            
        }
        void bg_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            //this.SimTrade(15,"16:05","16:55"); best
            this.SimTrade(interval, "09:45", "16:55");
        }
        void bg_DoWork1(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            //this.SimTrade(15,"16:05","16:55"); best
            this.SimTrade(15, "14:15", "16:55","S50M16");
        }
        void bg_DoWork2(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            //this.SimTrade(15,"16:05","16:55"); best
            this.SimTrade(15, "14:15", "16:55", "S50H16");
        }
        void bg_DoWork3(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            //this.SimTrade(15,"16:05","16:55"); best
            this.SimTrade(15, "14:15", "16:55", "S50Z15");
        }
        void bg_DoWork4(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            //this.SimTrade(15,"16:05","16:55"); best
            this.SimTrade(15, "14:15", "16:55", "S50U15");
        }
        void bg_DoWork5(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            //this.SimTrade(15,"16:05","16:55"); best
            this.SimTrade(15, "14:15", "16:55", "S50M15");
        }
        void bg_DoWork6(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            //this.SimTrade(15,"16:05","16:55"); best
            this.SimTrade(15, "14:15", "16:55", "S50H15");
        }
        
        void SimTrade(int interval, string sdate, string edate)
        {
            List<string> ldate = new List<string>();
            //int interval = 15;
            foreach (string s in comboBoxStartDate.Items)
                ldate.Add(s);

            

            //ldate.Add("20151214");
            //ldate.Add("20151215");

            //Bot bot = new Bot("reboundhighlowtrade", eventlogs, new ReboundHighLowTradeLogic(Feeder.NewResamplingDataTable(), interval), dmsql);
            //Bot bot = new Bot("doublehighlowtrade", eventlogs, new DoubleHighLowTradeLogic(Feeder.NewResamplingDataTable(), interval), dmsql);
            //Bot bot = new Bot("lwhighvol", eventlogs, new LongWhenHighVolShortPercentTradeLogic(Feeder.NewResamplingDataTable(), interval), dmsql);

            
            Bot bot = new Bot("hqt", eventlogs, new HQTradeLogic(Feeder.NewResamplingDataTable(), interval), dmsql);
            bot.SimulateTradeUnit(symbol, ldate, interval, sdate, edate);
            //dataGridViewDataResult1.BeginInvoke((MethodInvoker)delegate() { dataGridViewDataResult1.DataSource = bot.simulatetestSummary; ;});
            
        }

        void SimTrade(int interval, string sdate, string edate,string testsymbol)
        {
            //List<string> ldate = new List<string>();
            //int interval = 15;
            //foreach (string s in comboBoxStartDate.Items)
            //    ldate.Add(s);


            List<string> ldate = new List<string>();
            DataTable dt = dmsql.GetDatesBarsTable(testsymbol);
            if (dt.Rows.Count > 0)
            {

                foreach (DataRow r in dt.Rows)
                {
                    ldate.Add(r[0].ToString());
                }
            }
            //interval = 30;
            //Bot bot = new Bot("highvoltrade", eventlogs, new HighVolumeTradeLogic(Feeder.NewResamplingDataTable(), interval), dmsql);
            //Bot bot = new Bot("lwhighvolshort", eventlogs, new LongWhenHighVolShortPercentTradeLogic(Feeder.NewResamplingDataTable(), interval), dmsql);
            Bot bot = new Bot("biglotstrade2", eventlogs, new BigLotsHFTTradeLogic2(Feeder.NewResamplingDataTable(), interval), dmsql);
            bot.SimulateTradeUnit(testsymbol, ldate, interval, sdate, edate);
            dataGridViewDataResult1.BeginInvoke((MethodInvoker)delegate() { dataGridViewDataResult1.DataSource = bot.simulatetestSummary; ;});
        }

        
        private void button1_Click_1(object sender, EventArgs e)
        {
            /*Property p = new Property("test", 5, 5, 100, 5);
            double[] x = p.GetValueRange();*/
            Miumiugo mm = new Miumiugo(eventlogs);
            mm.ExtractChart("S50M16", "20160405", "20160405", 10,120);
            //mm.SetPathListToRegistry(@"D:\Miumiugo\raw\","raw");
            //mm.SetPathListToRegistry(@"D:\Miumiugo\norm\", "norm");
            //string rawpath = mm.GetPathListFromRegistry("raw");
            //string normpth = mm.GetPathListFromRegistry("norm");
            //mm.NormalizeData("S50M16", "20160405", "20160405", 300);
        }

        private void textBoxEventLog_TextChanged(object sender, EventArgs e)
        {
            this.textBoxEventLog.Update();
        }
    }
}
