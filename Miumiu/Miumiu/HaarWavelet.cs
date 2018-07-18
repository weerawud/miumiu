using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class HaarWavelet
{

    /// <summary>
    ///   Discrete Haar Wavelet Transform
    /// </summary>
    /// 
    private const double w0 = 0.5;
    private const double w1 = -0.5;
    private const double s0 = 0.5;
    private const double s1 = 0.5;
    public double[] FWT(double[] data)
    {
        double[] temp = new double[data.Length];

        int h = data.Length >> 1;
        for (int i = 0; i < h; i++)
        {
            int k = (i << 1);
            temp[i] = data[k] * s0 + data[k + 1] * s1;
            temp[i + h] = data[k] * w0 + data[k + 1] * w1;
        }
        
        //for (int i = 0; i < data.Length; i++)
        //    data[i] = temp[i];
        return temp;
    }

    /// <summary>
    ///   Discrete Haar Wavelet 2D Transform
    /// </summary>
    /// 
    public double[,] FWT(double[,] data, int iterations)
    {
        int rows = data.GetLength(0);
        int cols = data.GetLength(1);
        double[,] temp = new double[rows,cols];

        double[] row = new double[cols];
        double[] col = new double[rows];

        for (int k = 0; k < iterations; k++)
        {
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < row.Length; j++)
                    row[j] = data[i, j];

                row = FWT(row);

                for (int j = 0; j < row.Length; j++)
                    temp[i, j] = row[j];
            }

            for (int j = 0; j < cols; j++)
            {
                for (int i = 0; i < col.Length; i++)
                    col[i] = data[i, j];

                col = FWT(col);

                for (int i = 0; i < col.Length; i++)
                    temp[i, j] = col[i];
            }
        }
        return temp;
    }

    /// <summary>
    ///   Inverse Haar Wavelet Transform
    /// </summary>
    /// 
    public double[] IWT(double[] data)
    {
        double[] temp = new double[data.Length];

        int h = data.Length >> 1;
        for (int i = 0; i < h; i++)
        {
            int k = (i << 1);
            temp[k] = (data[i] * s0 + data[i + h] * w0) / w0;
            temp[k + 1] = (data[i] * s1 + data[i + h] * w1) / s0;
        }

        //for (int i = 0; i < data.Length; i++)
            //data[i] = temp[i];
        return temp;
    }

    /// <summary>
    ///   Inverse Haar Wavelet 2D Transform
    /// </summary>
    /// 
    public double[,] IWT(double[,] data, int iterations)
    {
        int rows = data.GetLength(0);
        int cols = data.GetLength(1);
        double[,] temp = new double[rows, cols];
        double[] col = new double[rows];
        double[] row = new double[cols];

        for (int l = 0; l < iterations; l++)
        {
            for (int j = 0; j < cols; j++)
            {
                for (int i = 0; i < row.Length; i++)
                    col[i] = data[i, j];

                col = IWT(col);

                for (int i = 0; i < col.Length; i++)
                    temp[i, j] = col[i];
            }

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < row.Length; j++)
                    row[j] = data[i, j];

                row = IWT(row);

                for (int j = 0; j < row.Length; j++)
                    temp[i, j] = row[j];
            }
        }
        return temp;
    }
    public double[] FWT(double[] data, Boolean LowPass)
    {
        double[] temp = FWT(data);

        int h = data.Length >> 1;
        double[] splittemp = new double[h];

        for (int i = 0; i < h; i++)
        {
            if (LowPass)
                splittemp[i] = temp[i];         //Get LowPass
            else splittemp[i] = temp[i + h];    //Get HighPass
        }
        return splittemp;
    }
}
