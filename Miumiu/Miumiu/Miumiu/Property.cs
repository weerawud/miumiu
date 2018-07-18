using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Property
{
    private double _value,_min,_max,_step;
    private string _ownername;

    public Property(string ownername, double val,double min,double max,double step)
    {
        _ownername = ownername;
        _value = val;
        _min = min;
        _max = max;
        _step = step;
    }
    public double Value
    {
        get { return _value; }
        set { _value = value; }
    }
    public double Min
    {
        get { return _min; }
        set { _min = value; }
    }
    public double Max
    {
        get { return _max; }
        set { _max = value; }
    }
    public double Step
    {
        get { return _step; }
        set { _step = value; }
    }
    public double[] GetValueRange()
    {
        List<double> vals = new List<double>();
        for (double i = _min; i <= _max; i = i + _step)
            vals.Add(i);
        return vals.ToArray<double>();
    }
    
}