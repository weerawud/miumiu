using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Win32;
using System.IO;


//using Encog.App.Quant.Normalize;
using Encog.App.Analyst;
using Encog.App.Analyst.Wizard;
using Encog.App.Analyst.CSV.Normalize;
using Encog.Util.CSV;
using Encog.Util.Arrayutil;


class Miumiugo
{

        
        DataHandlerMSSQL sqlDB;
        Dictionary<string, DataTable> AssetDataTableList;
        FileInfo rawFile, normFile;
        private int inputNeurons, outputNeurons;
        Logs _eventlogs;
        PatternAnalyzer PA;
        public Miumiugo(Logs logs)
        {
            //Initial var for DataHandle
            string constring = @"server=" + System.Environment.MachineName + @"\WiiSQL;Initial Catalog=AssetDB;User ID=sa;Password=wee@dcs.c0m";
            sqlDB = new DataHandlerMSSQL(constring);
            _eventlogs = logs;
        //Set,Get filepath
            this.SetPathListToRegistry(@"C:\Data\Miumiugo\norm", "norm");
            this.SetPathListToRegistry(@"C:\Data\Miumiugo\raw", "raw");
            rawFile = new FileInfo(this.GetPathListFromRegistry("raw"));
            normFile = new FileInfo(this.GetPathListFromRegistry("norm"));

            //Init AssetDataTableList
            AssetDataTableList = new Dictionary<string, DataTable>();
            
            //Init encog parameters
            inputNeurons = 0;
            outputNeurons = 0;

            //Init Pattern Analyzer
            PA = new PatternAnalyzer(_eventlogs);
        }
        public double[] ExtractChart(string symbol, string startdate, string enddate, int interval,int slidewindow)
        {
            string currdate = DateTime.Now.ToString("yyyyMMdd");
            string rfile = rawFile.FullName + String.Format("{4}-{0}-{1}-{2}-{3}s.csv", symbol, startdate, enddate, interval.ToString(), currdate);
            string nfile = normFile.FullName + String.Format("{4}-{0}-{1}-{2}-{3}s-norm.csv", symbol, startdate, enddate, interval.ToString(), currdate);
            string efile = normFile.FullName + String.Format("{4}-{0}-{1}-{2}-{3}s-norm.ega", symbol, startdate, enddate, interval.ToString(), currdate);
            DataTable dt = sqlDB.SaveResamplingBarsTableToCSV(symbol, startdate, enddate, interval, rfile);
            //store in DataTable Dict
            this.AssetDataTableList.Add(symbol, dt);
            double[] tmpdat = new double[dt.Rows.Count];
            for(int i=0;i<dt.Rows.Count;i++){
                tmpdat[i] = (Double)dt.Rows[i]["Close"];
            }
            WriteLog("Start Graph Extractor");
            this.PA.Extractor(symbol, tmpdat, slidewindow);
            WriteLog("Finish Graph Extractor");
            string[] l = _eventlogs.Eventlogs.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            TimeSpan stime = TimeSpan.Parse(l[l.Length - 2].Split('@')[0]);//start time log
            TimeSpan etime = TimeSpan.Parse(l[0].Split('@')[0]);//end time log
            TimeSpan totaltime = (etime - stime);
            WriteLog("Total Simulate Trade Time = " + totaltime.ToString());
            return tmpdat;
        }
        public void WriteLog(string log)
        {
            //Thread.Sleep(TimeSpan.FromSeconds(5));
            string logline = String.Format("{0}@{2}: {1} ", DateTime.Now.ToString("HH:mm:ss"), log, DateTime.Now.ToShortDateString());
            string textlog = logline + Environment.NewLine + _eventlogs.Eventlogs;
            _eventlogs.Eventlogs = textlog;
        }
    /*
        public void NormalizeData(string symbol,string startdate,string enddate,int interval)
        {
            string currdate = DateTime.Now.ToString("yyyyMMdd");
            string rfile = rawFile.FullName + String.Format("{4}-{0}-{1}-{2}-{3}s.csv", symbol, startdate, enddate, interval.ToString(),currdate);
            string nfile = normFile.FullName + String.Format("{4}-{0}-{1}-{2}-{3}s-norm.csv", symbol, startdate, enddate, interval.ToString(),currdate);
            string efile = normFile.FullName + String.Format("{4}-{0}-{1}-{2}-{3}s-norm.ega", symbol, startdate, enddate, interval.ToString(),currdate);
            DataTable dt = sqlDB.SaveResamplingBarsTableToCSV(symbol, startdate, enddate,interval,rfile);
            //store in DataTable Dict
            this.AssetDataTableList.Add(symbol, dt);
            FileInfo sourceFile = new FileInfo(rfile);
            FileInfo targetFile = new FileInfo(nfile);
            FileInfo encogFile = new FileInfo(efile);
            var encogAnalyst = new EncogAnalyst();
            var encogWizard = new AnalystWizard(encogAnalyst);
            encogWizard.Wizard(sourceFile, true, AnalystFileFormat.DecpntComma);
            
            var norm = new AnalystNormalizeCSV();
            norm.Analyze(sourceFile,true,CSVFormat.English,encogAnalyst);
            norm.ProduceOutputHeaders = true;
            /*
            for (int i = 0; i < encogAnalyst.Script.Normalize.NormalizedFields.Count; i++)
            {
                if (i > 1)
                    encogAnalyst.Script.Normalize.NormalizedFields[i].Action = NormalizationAction.Normalize;
                else encogAnalyst.Script.Normalize.NormalizedFields[i].Action = NormalizationAction.PassThrough;//passthrough whenn date and time
                
            }
            norm.Normalize(targetFile);
            encogAnalyst.Save(encogFile);
        }
*/
    public string GetPathListFromRegistry(string rkey)
        {
            string path = "Miumiugo"; //กำหนด path ที่ต้องการ ซึ่ง path นี้จะอยู่ใน HKEY_CURRENT_USER
            string listpath = "";
            RegistryKey r = Registry.CurrentUser.OpenSubKey(path); //เรียกใช้งาน OpenSubKey เพื่อทำให้สามารถเข้าถึง regiskey นี้ได้
            if (r == null)
            {
                r = Registry.CurrentUser.CreateSubKey(path);
                r.SetValue(rkey, "");
            }
            listpath = (string)r.GetValue(rkey);
            return listpath;
        }
    public void SetPathListToRegistry(string filepath,string rkey)
        {
            string path = "Miumiugo"; //กำหนด path ที่ต้องการ ซึ่ง path นี้จะอยู่ใน HKEY_CURRENT_USER
            RegistryKey r = Registry.CurrentUser.OpenSubKey(path, true); //เรียกใช้งาน OpenSubKey เพื่อทำให้สามารถเข้าถึง regiskey นี้ได้
            if (r == null)
            {
                r = Registry.CurrentUser.CreateSubKey(path);
            }
            else r.SetValue(rkey, filepath);
            
        }
        
        
}
