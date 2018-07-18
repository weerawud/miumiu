using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

 
    class Program
    {
        static void Main(string[] args)
        {
            string file = @"c:\test.csv";
            string sup = "2";
            if (args.Length > 0) {
                file = args[0];
                
            }
            if (args.Length== 2)
            {
                sup= args[1];

            }


            double support = double.Parse(sup);

            CSVReader cr = new CSVReader();
            ItemSet data = cr.Read(file);


            
            Program p = new Program();
            ItemSet a= p.apriori( data, support);
            for (int i = 0; i < a.Count;i++ )
            {
                ItemSet cur = (ItemSet)a[i];
                for (int j = 0; j < cur.Count; j++) {
                    ItemSet now = (ItemSet)cur[j];
                    foreach (DataItem item in now)
                    {

                        Console.Write("编号" + item.Id + ":" + item.ItemName+"  ");

                        
                    }
                    Console.WriteLine("  支持度:"+now.ICount);
                }

            }
            Console.Read();
        }

        private ItemSet FindOneColSet(ItemSet data, double support)
        {
            ItemSet cur=null;
            ItemSet result = new ItemSet();

            ItemSet set=null;
            ItemSet newset=null;
            DataItem cd=null;
             DataItem td=null;
             bool flag = true;

            for (int i = 0; i < data.Count; i++) {
                cur = (ItemSet)data[i];
                for (int j = 0; j < cur.Count; j++) {
                    cd = (DataItem)cur[j];
                    
                    for (int n = 0; n < result.Count; n++) {
                        set = (ItemSet)result[n];
                        td= (DataItem)set[0];
                        if (cd.Id == td.Id)
                        {
                            set.ICount++;
                            flag = false;
                            break;
                                                              
                        }
                        flag=true;
                    }
                    if (flag) {
                        newset = new ItemSet();
                        newset.Add(cd);
                        result.Add(newset);
                        newset.ICount = 1;
                    
                    
                    }

                   
                    
                }


                
            }
            ItemSet finalResult = new ItemSet();
            for (int i = 0; i < result.Count; i++)
            {
                ItemSet con = (ItemSet)result[i];
                if (con.ICount >= support)
                {

                    finalResult.Add(con);
                }


            }
            //finalResult.Sort();
            return finalResult;
  
        
        }


        private ItemSet apriori(  ItemSet data, double support)
        {

            ItemSet result = new ItemSet();
            ItemSet li = new ItemSet();
            ItemSet conList = new ItemSet();
            ItemSet subConList = new ItemSet();
            ItemSet subDataList = new ItemSet();
            int k = 2;
            li.Add( new ItemSet());
            li.Add(this.FindOneColSet(data,support));
            
            while (((ItemSet)li[k-1]).Count != 0)
            {

                conList = AprioriGenerate((ItemSet)li[k - 1],k-1, support);
                for (int i = 0; i < data.Count; i++)
                {
                    subDataList = SubSet((ItemSet)data[i], k);
                    for (int j = 0; j < subDataList.Count; j++)
                    {
                        for (int n = 0; n < conList.Count; n++)
                        {
                            ((ItemSet)subDataList[j]).Sort();
                            ((ItemSet)conList[n]).Sort();
                            if (((ItemSet)subDataList[j]).Equals(conList[n]))
                            {
                                ((ItemSet)conList[n]).ICount++;

                            }
                        }
                      
                    }

                }

                li.Add(new ItemSet());
                for (int i = 0; i < conList.Count; i++)
                {
                    ItemSet con = (ItemSet)conList[i];
                    if (con.ICount >= support)
                    {
                         
                        ((ItemSet)li[k]).Add(con);
                    }


                }
              
                k++;
            }
            for (int i = 0; i < li.Count; i++)
            {
                
                result.Add(li[i]);

                  

            }
            return result;



        }

        private ItemSet AprioriGenerate(ItemSet li,int k, double support)
        {

            ItemSet curList = null;
            ItemSet durList = null;
            ItemSet candi = null;
            ItemSet result = new ItemSet();
            for (int i = 0; i < li.Count; i++)
            {
                for (int j = 0; j < li.Count; j++)
                {
                    bool flag = true ;
                    curList = (ItemSet)li[i];
                    durList = (ItemSet)li[j];
                    for (int n = 2; n < k; n++)
                    {

                        if (((DataItem)curList[n - 2]).Id == ((DataItem)durList[n - 2]).Id)
                        {

                            flag = true;

                        }
                        else {
                            break;
                            flag = false;
                           
                           
                        }
                      

                    }

                    if (flag && ((DataItem)curList[k - 1] ).Id< ((DataItem)durList[k - 1]).Id)
                    {

                        flag = true;
                    }
                    else {
                        flag = false;
                    }
                    if (flag)
                    {
                        candi = new ItemSet();
                        

                        for(int m=0;m<k;m++){
                            candi.Add(durList[m]);
                        
                        }
                        candi.Add(curList[k-1]);





                        if (HasInFrequentSubset(candi, li,k))
                        {
                            candi.Clear();

                        }
                        else
                        {
                            result.Add(candi);
                        }
                    }

                }
            }
            return result;

        }


        
        private bool HasInFrequentSubset(ItemSet candidate, ItemSet li,int k)
        {
            ItemSet subSet = SubSet(candidate,k);
            ItemSet curList = null;
            ItemSet liCurList = null;
        
            for (int i = 0; i < subSet.Count; i++)
            {
                curList = (ItemSet)subSet[i];
                for (int j = 0; j < li.Count; j++)
                {

                    liCurList = (ItemSet)li[j];
                    if (liCurList.Equals(curList))
                    {
                       return false;

                    }
                   
                }
            }
            return true;;
        }
        //划分子集
        private ItemSet SubSet(ItemSet set)
        {
            ItemSet subSet = new ItemSet();

            ItemSet itemSet = new ItemSet();
            //移位求2n次访
            int num = 1 << set.Count;

            int bit;
            int mask = 0; ;
            for (int i = 0; i < num; i++)
            {
                itemSet = new ItemSet();
                for (int j = 0; j < set.Count; j++)
                {
                    //mask与i可以得出某位是否为零
                    mask = 1 << j;
                    bit = i & mask;
                    if (bit > 0)
                    {

                        itemSet.Add(set[j]);

                    }
                }
                if (itemSet.Count > 0)
                {
                    subSet.Add(itemSet);
                }


            }



            return subSet;
        }



        //划分子集
        private ItemSet SubSet(ItemSet set, int t)
        {
            ItemSet subSet = new ItemSet();

            ItemSet itemSet = new ItemSet();
            //移位求2n次访
            int num = 1 << set.Count;

            int bit;
            int mask = 0; ;
            for (int i = 0; i < num; i++)
            {
                itemSet = new ItemSet();
                for (int j = 0; j < set.Count; j++)
                {
                    //mask与i可以得出某位是否为零
                    mask = 1 << j;
                    bit = i & mask;
                    if (bit > 0)
                    {

                        itemSet.Add(set[j]);

                    }
                }
                if (itemSet.Count == t)
                {
                    subSet.Add(itemSet);
                }


            }



            return subSet;
        }
    }
 
