using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Accord.MachineLearning;
using Accord.Math;
using AForge.Math.Metrics;
using static Accord.MachineLearning.KMeansClusterCollection;


class PatternAnalyzer
{
    //Dictionary <string,double[]> RawDataDict;
    public Dictionary<string, List<double[]>> SlideDataDict;
    public Dictionary<string, List<double[]>> DWTransformDataDict;
    Logs _eventlogs;
    public PatternAnalyzer(Logs logs)
    {
        //RawDataDict = new Dictionary<string, double[]>();
        SlideDataDict = new Dictionary<string, List<double[]>>();
        DWTransformDataDict = new Dictionary<string, List<double[]>>();
        _eventlogs = logs;

        //KMeans tmpKM = new KMeans(1, Distance.Correlation);
        /*
        PearsonCorrelation pc = new PearsonCorrelation();
        double[][] tmp1 = new double[2][];
        tmp1[0] = new double[7] { 900, 901, 902, 903, 904, 905, 906 };
        tmp1[1] = new double[7] { 900, 801, 802, 803, 804, 805, 806 };
        double tmpscore = pc.GetSimilarityScore(tmp1[0], tmp1[1]);
        */
    }
    public List<KMeansCluster> Extractor(string label, double[] rawdat,int slidewindow,double threshold)
    {
        
        //RawDataDict.Add(label,rawdat);
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
        List<KMeansCluster> PatternClusterList = this.FindCluster(tmphaar,threshold);//distance 2 is the exactly same
        /*
        //Change List of double array to double[][]
        double[][] dwt = new double[tmphaar.Count][];
        for(int i=0;i<tmphaar.Count;i++)
        {
            dwt[i] = new double[tmphaar[0].Length];
            for (int j = 0; j < tmphaar[0].Length; j++)
                dwt[i][j] = tmphaar[i][j];
            
        }
        WriteLog("Start K-Mean, "+dwt.Length+" sampling");
        KMeans KM = new KMeans(60, Distance.Correlation);
        int[] clusterlabels = KM.Compute(dwt);
        Dictionary<int,double> DistanceDict = new Dictionary<int,double>();
        
        //Cluster Dict

        for(int i = 0;i<clusterlabels.Length;i++)
            DistanceDict.Add(i,KM.Distance(tmphaar[i], KM.Clusters[clusterlabels[i]].Mean));
        //var d = KM.Distance(tmphaar[0], KM.Clusters[0].Mean);
        //var dd = KM.Distance(tmphaar[0], KM.Clusters[9].Mean);
        var info = KM.Clusters;
        */
        WriteLog("Finish K-Mean Cluster = " + PatternClusterList.Count + " clusters");
        //WriteLog("Finish K-Mean Cluster = " + info.Count+" clusters");
        return PatternClusterList;
    }

    private List<KMeansCluster> FindCluster(List<double[]> dataset, double threshold)
    {

        // Change List of double array to double[][]
        double[][] dwt = new double[dataset.Count][];
        for (int i = 0; i < dataset.Count; i++)
        {
            dwt[i] = new double[dataset[0].Length];
            for (int j = 0; j < dataset[0].Length; j++)
                dwt[i][j] = dataset[i][j];

        }

        //Init List Cluster
        List<KMeansCluster> ListCluster = new List<KMeansCluster>();

        //Define number of cluster which must less than number of dataset
        int tmpnumcluster = dwt.Length / 2;

        for (int i = 0; i < dwt.Length; i++)
        {

            KMeans tmpKM = new KMeans(1);
            PearsonCorrelation pc = new PearsonCorrelation();
            double distance = 0;
            //initial new tmp double [1][] array
            double[][] tmpdwt;
            tmpdwt = new double[1][];
            tmpdwt[0] = new double[dwt[i].Length];
            for (int j = 0; j < dwt[i].Length; j++)
            {

                tmpdwt[0][j] = dwt[i][j];

            }
            //Computer cluster

            if (ListCluster.Count > 0)
            {
                //Find the best cluster match with dataset

                for (int k = 0; k < ListCluster.Count; k++)
                {
                    //Check correlation distance from mean
                    double tmpdistance = pc.GetSimilarityScore(tmpdwt[0], ListCluster[k].Centroid);
                    if (tmpdistance > distance)
                        distance = tmpdistance;
                }
                //if none match dataset with existing cluster then add new cluster

            }
            if (distance < threshold)
            {
                //tmpKM.Compute(tmpdwt);
                tmpKM.Learn(tmpdwt);
                ListCluster.Add(tmpKM.Clusters[0]);
            }

        }
        return ListCluster;
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
