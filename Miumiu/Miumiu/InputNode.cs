using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Accord.Statistics;



abstract class InputNode
{
    protected Dictionary<string, DataTable> _InputNodeDataTableDict;
    protected Dictionary<String, Property> _NodePropTable;
    protected Boolean _IsIntradayTrade;
    public string assetname { get; set; }
    public string nodename { get; set; }
    abstract public void InitLogicPropTable(int interval);

    

    public void InitVar()
    {
        _InputNodeDataTableDict = new Dictionary<string, DataTable>();
        _NodePropTable = new Dictionary<string, Property>();
    }
    public void ChangeLogicPropTable(string propname, double val)
    {
        _NodePropTable[propname].Value = val;
    }
    public void InitLogicPropTable(Dictionary<string, Property> propDict, int interval)
    {
        _NodePropTable = propDict;
        ChangeLogicPropTable("Interval", interval);
    }
    public DataTable GetPropertyTable(string id)
    {
        DataTable dt = this.NewLogicPropertyTable();
        dt.TableName = this.GetType().Name;
        foreach (KeyValuePair<string, Property> kv in _NodePropTable)
        {
            DataRow dr = dt.NewRow();
            dr["id"] = id;
            dr["propertyname"] = kv.Key;
            dr["value"] = kv.Value.Value;
            dr["min"] = kv.Value.Min;
            dr["max"] = kv.Value.Max;
            dr["step"] = kv.Value.Step;
            dt.Rows.Add(dr.ItemArray);
        }
        return dt;
    }
    public double[] GetLogicPropTableValueRange(string propname)
    {
        return _NodePropTable[propname].GetValueRange();
    }
    private DataTable NewLogicPropertyTable()
    {
        DataTable dt = new DataTable();
        DataColumn[] col = new DataColumn[6];
        col[0] = new DataColumn("id", typeof(System.String));
        col[1] = new DataColumn("propertyname", typeof(System.String));
        col[2] = new DataColumn("value", typeof(System.Double));
        col[3] = new DataColumn("min", typeof(System.Double));
        col[4] = new DataColumn("max", typeof(System.Double));
        col[5] = new DataColumn("step", typeof(System.Double));
        dt.Columns.AddRange(col);
        return dt;
    }
}

class MovingAvgInputNode : InputNode
{
    
    public MovingAvgInputNode(DataTable rawDataTable, string assetname, bool IsIntradayTrade)
    {
        _IsIntradayTrade = IsIntradayTrade;
        this.assetname = assetname;
        this.nodename = this.GetType().Name;
        InitVar();
    }
    public override void InitLogicPropTable(int interval)
    {
        throw new NotImplementedException();
    }
}

