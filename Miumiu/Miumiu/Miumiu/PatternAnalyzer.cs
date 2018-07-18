using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Accord.MachineLearning;
using Accord.Math;

class PatternAnalyzer
{
    Dictionary <string,double[]> RawDataDict;
    Dictionary<string, List<double[]>> SlideDataDict;
    Dictionary<string, List<double[]>> DWTransformDataDict;
    Logs _eventlogs;
    public PatternAnalyzer(Logs logs)
    {
        RawDataDict = new Dictionary<string, double[]>();
        SlideDataDict = new Dictionary<string, List<double[]>>();
        DWTransformDataDict = new Dictionary<string, List<double[]>>();
        _eventlogs = logs;
        
    }
    public List<double[]> Extractor(string label, double[] rawdat,int slidewindow)
    {
        
        RawDataDict.Add(label,rawdat);
        //Prepare slide data n windows
        HaarWavelet Haar = new HaarWavelet();
        List<double[]> tmphaar = new List<double[]>();
        List<double[]> tmpslideList = SlideData(rawdat, slidewindow);
        SlideDataDict.Add(label, tmpslideList);
        foreach (double[] sdata in tmpslideList )
        {
            tmphaar.Add(Haar.FWT(Haar.FWT(sdata,true),true));//Two time Haar DWT
        }
        DWTransformDataDict.Add(label, tmphaar);
        
        double[][] dwt = new double[tmphaar.Count][];
        for(int i=0;i<tmphaar.Count;i++)
        {
            dwt[i] = new double[tmphaar[0].Length];
            for (int j = 0; j < tmphaar[0].Length; j++)
                dwt[i][j] = tmphaar[i][j];
            
        }
        WriteLog("Start K-Mean, "+dwt.Length+" sampling");
        KMeans KM = new KMeans(tmphaar.Count/5, Distance.Correlation);
        int[] labels = KM.Compute(dwt);
        Dictionary<int,double> DistanceDict = new Dictionary<int,double>();
        for(int i = 0;i<labels.Length;i++)
            DistanceDict.Add(i,KM.Distance(tmphaar[i], KM.Clusters[labels[i]].Mean));
        //var d = KM.Distance(tmphaar[0], KM.Clusters[0].Mean);
        //var dd = KM.Distance(tmphaar[0], KM.Clusters[9].Mean);
        var info = KM.Clusters;

        WriteLog("Finish K-Mean Cluster = " + info.Count+" clusters");
        return tmphaar;
    }
    private List<double[]> SlideData(double[] rawdat,int slidewindow)
    {
        List<double[]> SlidingDataList = new List<double[]>();
        if (rawdat.Length > 0)
        {
            for(int i=0;i<rawdat.Length;i++)
                if ((i + slidewindow) <= rawdat.Length)
                {
                    double[] tmpdat = new double[slidewindow];
                    for (int j = 0; j < slidewindow; j++)
                    {
                        tmpdat[j] = rawdat[i + j];  
                    }
                    SlidingDataList.Add(tmpdat);
                }    
        }
        return SlidingDataList;
    }
    public void WriteLog(string log)
    {
        //Thread.Sleep(TimeSpan.FromSeconds(5));
        string logline = String.Format("{0}@{2}: {1} ", DateTime.Now.ToString("HH:mm:ss"), log, DateTime.Now.ToShortDateString());
        string textlog = logline + Environment.NewLine + _eventlogs.Eventlogs;
        _eventlogs.Eventlogs = textlog;
    }
}
