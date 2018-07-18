using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Threading;


class Logs : INotifyPropertyChanged
{
    private string _eventlogs="";
    private SynchronizationContext context;
    public Logs(SynchronizationContext context)
    {
        this.context = context ?? new SynchronizationContext();
    }
    public string Eventlogs
    {
        get { return _eventlogs; }
        set
        {
            _eventlogs = value;
            
            context.Send(s => InvokePropertyChanged(new PropertyChangedEventArgs("EventLogs")), null);
            //InvokePropertyChanged(new PropertyChangedEventArgs("EventLogs"));
        }
    }

    #region Implementation of INotifyPropertyChanged

    public event PropertyChangedEventHandler PropertyChanged;
    

    public void InvokePropertyChanged(PropertyChangedEventArgs e)
    {
        PropertyChangedEventHandler handler = PropertyChanged;
        if (handler != null) handler(this, e);
    }

    #endregion
}

