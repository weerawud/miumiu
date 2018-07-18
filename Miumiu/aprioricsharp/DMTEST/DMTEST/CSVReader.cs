using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
 
    class CSVReader
    {
        public CSVReader() { 
        
        
        }
        public ItemSet Read(string fileName) {

            ItemSet rowSet = new ItemSet();
            ItemSet colSet = null;
            string col="";
            string[] head=null;
            if (File.Exists(fileName))
            {
                // Create a file to write to.

                StreamReader sr = new StreamReader(File.OpenRead(fileName), Encoding.Default, true);
                string row = "";
                int k = 0;
                while(!sr.EndOfStream){
                    k++;
                    row=sr.ReadLine();
                    
                    string[] cols=row.Split(",".ToCharArray());
                    
                    if (k == 1)
                    {
                        head=cols;

                    }
                    else {

                        colSet = new ItemSet();

                        for(int i=1;i<cols.Length+1;i++)
                        {
                            col=cols[i-1];
                            if (col.Equals("1"))
                            {
                                colSet.Add(new DataItem(i,head[i-1]));
                            }


                        }

                        rowSet.Add(colSet);
                    }


                }
              
             
               
                sr.Close();
               


            }
            return rowSet;
        }
    }
 
