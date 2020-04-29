using System;
using System.Threading.Tasks;
using hh_resume.Objects;
using Microsoft.Extensions.Configuration;

namespace hh_resume
{
    class Program
    {
        static void Main(string[] args)
        {
            var GetConfiguration = new ConfigurationBuilder().AddJsonFile("Configuration.json").Build();

            Configuration.ConnectionString = GetConfiguration["ConnectionString"];
            Configuration.SchemaName = GetConfiguration["SchemaName"];
            Configuration.TableName = GetConfiguration["TableName"];
            
            Proxy.AddProxies();
            Task[] tasks = new Task[5];
            for (int i = 0; i < tasks.Length; i++)
            {
                int j = i;
                tasks[i] = new Task(() => { Parser.Execute(j); });
                tasks[i].Start();
            }

            Task.WaitAll(tasks);
        }
    }
}