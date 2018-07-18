using System;
using System.Collections.Generic;
using System.Text;


public class DataItem:IComparable
    {
       public DataItem()
       { 
       
       }
       public DataItem(int id)
       {
           this.Id = id;

       }
       public DataItem(int id, string itemName)
       {
           this.Id = id;
           this.ItemName = itemName;

       }
       private string itemName;

       public string ItemName
       {
           get { return itemName; }
           set { itemName = value; }
       }
       private int id;

       public int Id
       {
           get { return id; }
           set { id = value; }
       }

    public override bool Equals(object obj)
    {
        DataItem di = (DataItem)obj;
        return di.Id.Equals(this.Id);
    }

       #region IComparable Members

       public int CompareTo(object obj)
       {
           DataItem di = (DataItem)obj;
           return this.Id.CompareTo(di.Id);

       }

       #endregion
   }
    
 
