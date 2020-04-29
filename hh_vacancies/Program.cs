using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using hh_vacancies.Objects;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using Npgsql;
using RestSharp;

namespace hh_vacancies
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var GetConfiguration = new ConfigurationBuilder().AddJsonFile("Configuration.json").Build();

            Configuration.ConnectionString = GetConfiguration["ConnectionString"];
            Configuration.SchemaName = GetConfiguration["SchemaName"];
            Configuration.TableName = GetConfiguration["TableName"];
            await Proxy.GetProxies();
            await Proxy.AddProxies();
            var relevance_date = DateTime.Now;
            /*Task[] tasks = new Task[4];
            for (int i = 0; i < tasks.Length; i++)
            {
                int j = i;
                tasks[i] = new Task(() => { Parser.Execute(j); });
                tasks[i].Start();
            }

            Task.WaitAll(tasks);*/
            await Parser.Execute();
            Parser.CheckForRelevance(relevance_date.ToString("yyyy-MM-dd HH:mm:ss"));
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine($"< Parser started at {relevance_date} finished at {DateTime.Now}");
            Console.ResetColor();
            //Parser.Execute();
            SetParsed("HeadHunter");

        }
        private static void SetParsed(string name)
        {
            using (var conn = new NpgsqlConnection(Configuration.ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = $"UPDATE monitoring.parser_monitoring SET parsed = TRUE,last_parsed = current_timestamp  WHERE name = @name";
                    cmd.Parameters.AddWithValue("name", name);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
