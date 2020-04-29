using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace hh_resume
{
    
    public class Proxy
    {
        public static readonly List<string> list = new List<string>();
        public static void AddProxies()
        {
            list.Add("185.120.76.68");
            list.Add("185.120.77.95");
            list.Add("185.120.78.125");
            list.Add("185.120.79.35");
            list.Add("185.120.76.66");
            list.Add("185.120.77.246");
            list.Add("185.120.76.151");
            list.Add("185.120.79.29");
            list.Add("185.120.78.135");
            list.Add("185.120.76.184");
            list.Add("185.120.76.59");
            list.Add("185.120.78.160");
            list.Add("185.120.76.40");
            list.Add("45.136.56.97");
            list.Add("185.120.78.39");
            list.Add("185.120.78.233");
            list.Add("185.120.79.40");
            list.Add("185.120.76.70");
            list.Add("45.152.84.81");
            list.Add("185.120.78.249");
        }
        
        public static IWebProxy GetProxy()
        {
            Random rnd = new Random();
            int i;
            try
            {
                i = rnd.Next(0, list.Count-1);
            }
            catch (Exception)
            {
                AddProxies();
                i = rnd.Next(0, list.Count-1);
            }


            IWebProxy a = new WebProxy($"{list[i]}:65233", true)
                {Credentials = new NetworkCredential {UserName = "21046", Password = "V1r4TjI"}};
            //list.RemoveAt(i);
            return a;
           
        }
            
           
    }
    
}