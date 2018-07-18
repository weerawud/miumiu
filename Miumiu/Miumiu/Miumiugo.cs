using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Win32;
using System.IO;
using Accord.MachineLearning;
using Accord.Math;
using AForge.Math.Metrics;
using static Accord.MachineLearning.KMeansClusterCollection;
//using Encog.App.Quant.Normalize;


class Miumiugo
{


    //DataHandlerMSSQL sqlDB;
    private string _constring;
    Dictionary<string, DataTable> AssetDataTableList;
    FileInfo rawFile, normFile;
    private int inputNeurons, outputNeurons;
    Logs _eventlogs;
    PatternAnalyzer PA;
    Dictionary<string, List<KMeansCluster>> KMeansClusterDict;
    Dictionary<string, ChartPatternRepo> ChartRepoDict;
    Dictionary<string, List<double[]>> DWTDataDict;
    Dictionary<string, List<string>> SequentialChartPatternDict;
    public Miumiugo(Logs logs)
    {
        //Initial var for DataHandle
        _constring = @"server=" + System.Environment.MachineName + @"\WiiSQL;Initial Catalog=AssetDB;User ID=sa;Password=wee@dcs.c0m";
        //sqlDB = new DataHandlerMSSQL(constring);
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
        KMeansClusterDict = new Dictionary<string, List<KMeansCluster>>();
        DWTDataDict = new Dictionary<string, List<double[]>>();
        SequentialChartPatternDict = new Dictionary<string, List<string>>();
        //Init ChartRepo
        ChartRepoDict = new Dictionary<string, ChartPatternRepo>();
    }
    public void NewChartRepo()//When complete extract
    {
         
    }
    public void ExtractChart(string symbol, string field, List<string> datelist, int interval, int slidewindows,double threshold)//Create Raw chart KMCluster
    {

        Parallel.ForEach(datelist, new ParallelOptions { MaxDegreeOfParallelism = 6 },
            eachldate =>{

                DataHandlerMSSQL sqlDB = new DataHandlerMSSQL(_constring);
                //DataTable dt = sqlDB.GetResamplingBarsTable(symbol, eachldate, eachldate, interval);
                DataTable dt = sqlDB.GetResamplingBarsTableFillEmpty(symbol, eachldate, eachldate, interval);
                if (!KMeansClusterDict.ContainsKey(field))
                    KMeansClusterDict.Add(field, new List<KMeansCluster>());
                //store in DataTable Dict
                this.AssetDataTableList.Add(symbol + "@" + eachldate, dt);
                double[] tmpdat = new double[dt.Rows.Count];
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    tmpdat[i] = (double)dt.Rows[i][field];
                }
                WriteLog("Start Graph Extractor " + eachldate);
                string label = symbol + "@" + eachldate;
                List<KMeansCluster> tmpKMList = PA.Extractor(label, tmpdat, slidewindows,threshold);
                if (PA.DWTransformDataDict[label].Count > 0)
                {
                    DWTDataDict.Add(label, PA.DWTransformDataDict[label]);
                }

                WriteLog("Finish Graph Extractor " + eachldate);
                WriteLog("Updating KMeanClusterDict " + eachldate);
                UpdateKMeansClusterDict(field, tmpKMList);
                //this.UpdateKMeansClusterDict(field,tmpKMList);
                WriteLog("Updating completed " + eachldate);
            }
            );
        WriteLog("Optimizing KMClustersDict: "+KMeansClusterDict[field].Count);
        OptimizeKMeanClusterDict(field,threshold);

        WriteLog("Optimizing completed: " + KMeansClusterDict[field].Count);

        WriteLog("Adding new ChartPattern to Repo ");
        //New ChartRepo
        if (!ChartRepoDict.ContainsKey(symbol))
        {
            ChartRepoDict.Add(symbol, new ChartPatternRepo(symbol));
        }
        ChartRepoDict[symbol].AddChartPattern(field, interval, slidewindows, threshold, KMeansClusterDict);
        WriteLog("Adding new ChartPattern to Repo Completed ");
        WriteLog("Creating SequentailChart");
        //Create SequentialChart
        SequentialChartPatternDict.Clear();
        Parallel.ForEach(DWTDataDict.Keys, new ParallelOptions { MaxDegreeOfParallelism = 6 },
            eachlabel => {

                SequentialChartPatternDict.Add(eachlabel, ChartRepoDict[symbol].GetSequentialChartPattern(eachlabel, interval, slidewindows, threshold, field, DWTDataDict[eachlabel],false));
            }
            );

        string[] l = _eventlogs.Eventlogs.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
        TimeSpan stime = TimeSpan.Parse(l[l.Length - 2].Split('@')[0]);//start time log
        TimeSpan etime = TimeSpan.Parse(l[0].Split('@')[0]);//end time log
        TimeSpan totaltime = (etime - stime);
        WriteLog("Total Simulate Trade Time = " + totaltime.ToString());

        

    }
    public void InitSequentailChartPattern(string symbol,string field, double threshold)
    {
        
    }
    
    public void OptimizeKMeanClusterDict(string field,double threshold)//remove some cluster which correlate >= threshold 
    {
        //KMeans tmpKM = new KMeans(1, Distance.Correlation);
        PearsonCorrelation pc = new PearsonCorrelation();
        List<KMeansCluster> tmpClusterList = new List<KMeansCluster>();
        

        
        foreach (KMeansCluster existingkm in this.KMeansClusterDict[field])
        {
            bool isfound = false;
            foreach (KMeansCluster newcluster in tmpClusterList)
            {
                if (pc.GetSimilarityScore(newcluster.Centroid, existingkm.Centroid) > threshold)
                {
                    isfound = true;
                    break;
                }
            }
            if (!isfound)
                tmpClusterList.Add(existingkm);
        }
        this.KMeansClusterDict.Remove(field);
        this.KMeansClusterDict.Add(field, tmpClusterList);
    }
    public void UpdateKMeansClusterDict(string field, List<KMeansCluster> clusterlist)
    {
        if (!this.KMeansClusterDict.ContainsKey(field))
            this.KMeansClusterDict.Add(field, new List<KMeansCluster>());
        foreach (KMeansCluster km in clusterlist)
            this.KMeansClusterDict[field].Add(km);
    }
    /*private void UpdateKMeansClusterDict(string field,List<KMeansCluster> newClusterList)
    {
        KMeans tmpKM = new KMeans(1, Distance.Correlation);
        foreach (KMeansCluster newkm in newClusterList)
        {
            bool isfound = false;
            foreach (KMeansCluster existingkm in this.KMeansClusterDict[field])
            {
                if (tmpKM.Distance(newkm.Mean, existingkm.Mean) > 1.9)
                    isfound = true;    
            }
            if (!isfound)
                KMeansClusterDict[field].Add(newkm);
        }
    }*/
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
