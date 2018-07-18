using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Accord.MachineLearning;
using Accord.Math;
using System.Data;
using AForge.Math.Metrics;
using static Accord.MachineLearning.KMeansClusterCollection;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable]
class ChartPattern
{
    private double _interval, _slidewindows, _correlatethreshold;
    private string _assetname;
    Dictionary<string, List<KMeansCluster>> _KMeansClusterDict;
    private string _chartlabel;
    DateTime _startdate;
    DateTime _enddate;
    public ChartPattern(string assetname,double interval, double slidewindows, double correlatethreshold, Dictionary<string, List<KMeansCluster>> clusterDict)
    {
        AssetName = assetname;
        Interval = interval;
        SlideWindows = slidewindows;
        CorrelateThreshold = correlatethreshold;
        ClusterDict = clusterDict;
        ChartLabel = String.Format("{0}@{1}@{2}@{3}", AssetName, Interval.ToString(), SlideWindows.ToString(), CorrelateThreshold.ToString());

    }
    public DateTime StartDate
    {
        get { return _startdate; }
        set { _startdate = value; }
    }
    public DateTime EndDate
    {
        get { return _enddate; }
        set { _enddate = value; }
    }
    public Dictionary<string, List<KMeansCluster>> ClusterDict
    {
        get { return _KMeansClusterDict; }
        set { _KMeansClusterDict = value; }
    }
    public String AssetName
    {
        get { return _assetname; }
        set { _assetname = value; }
    }
    public String ChartLabel
    {
        get { return _chartlabel; }
        set { _chartlabel = value; }
    }
    public Double Interval
    {
        get { return _interval; }
        set { _interval = value; }
    }
    public Double SlideWindows
    {
        get { return _slidewindows; }
        set { _slidewindows = value; }
    }
    public Double CorrelateThreshold
    {
        get { return _correlatethreshold; }
        set { _correlatethreshold = value; }
    }
}

[Serializable]
class ChartPatternRepo
{

    private string _assetname;
    public Dictionary<string,ChartPattern> ChartClusterRepoDict;
    public ChartPatternRepo(string assetname)
    {
        ChartClusterRepoDict = new Dictionary<string, ChartPattern>();
        AssetName = assetname;
    }
    public void AddChartPattern(string field,double interval, double slidewindows, double correlatethreshold, Dictionary<string, List<KMeansCluster>> clusterDict)
    {
        /*
        if (!ChartClusterRepoDict.ContainsKey(field))
        {
            ChartClusterRepoDict.Add(field, new Dictionary < string,  ChartPattern >());
        }*/
        ChartPattern tmpCP = new ChartPattern(AssetName, interval, slidewindows, correlatethreshold, clusterDict);
        //if existing chartpattern then remove
        if (ChartClusterRepoDict.ContainsKey(tmpCP.ChartLabel))
            ChartClusterRepoDict.Remove(tmpCP.ChartLabel);
        //Add New
        ChartClusterRepoDict.Add(tmpCP.ChartLabel, tmpCP);
    }

    public List<string> GetSequentialChartPattern(string clusterlabel,double interval,double slidewindows, double correlatethreshold,string field,List<double[]> dwtdataList,bool isdup)
    {
        List<string> SequentialList = new List<string>();
        
        string chartlabel = String.Format("{0}@{1}@{2}@{3}", AssetName, interval, slidewindows.ToString(), correlatethreshold.ToString());
        
        List<KMeansCluster> clusterList = ChartClusterRepoDict[chartlabel].ClusterDict[field];

        //KMeans tmpKM = new KMeans(1, Distance.Correlation);
        PearsonCorrelation pc = new PearsonCorrelation();
        
        foreach (double[] data in dwtdataList)
        {
            int index = -1;
            
            foreach (KMeansCluster kmc in clusterList)
            {
                index++;
                if (pc.GetSimilarityScore(kmc.Centroid, data) >= correlatethreshold)
                {
                    break;
                }
                
            }
            //Check last sequence duplicate or new
            if ((isdup)||SequentialList.Count==0)
                SequentialList.Add(index.ToString());
            else {
                if (SequentialList[SequentialList.Count - 1] != index.ToString())
                    SequentialList.Add(index.ToString());
            }
        }



        

        return SequentialList;
    }
    public String AssetName
    {
        get { return _assetname; }
        set { _assetname = value; }
    }
}
