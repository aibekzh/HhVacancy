using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using hh_vacancies.Objects;
using Npgsql;

namespace hh_vacancies
{
    public class Proxy
    {
        public static readonly List<string> list = new List<string>();
        public static readonly List<string> proxyList = new List<string>();
        public static async Task AddProxies()
        {

            foreach (var var  in proxyList)
            {
                 list.Add(var);
            }
        }

        public static async Task GetProxies()
        {
            using (var conn = new NpgsqlConnection(Configuration.ConnectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText =
                        "select address from monitoring.proxies where status is true";
                    //cmd.CommandText = "select name ,comp_id from hh_parsing.enbek_companies order by id";
                    var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        var address = reader[0].ToString();
                        proxyList.Add(address);
                    }
                }
            }
        }
        
        public static async Task<WebProxy> GetProxy()
        {
            Random rnd = new Random();
            int i;
           
            i = rnd.Next(0, list.Count-1);

            WebProxy a = new WebProxy($"{list[i]}:65233", true)
                {Credentials = new NetworkCredential {UserName = "21046", Password = "V1r4TjI"}};
            //list.RemoveAt(i);
            return  a;
           
        }
    }
}