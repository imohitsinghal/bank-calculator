using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    public class Environments
    {
		//thread static attribute to save tasks from deadlock add this to process task
		// [ThreadStatic]
        // static KeyValuePair<string, string> ActiveConnection;
        public static Dictionary<string, string> ConnectionStrings { get; set; }
        public static int MaxThreads { get; set; }
        public static bool development { get; set; }
        public static bool ExtractExcel { get; set; }


        public Environments(bool isDev, Dictionary<string, string> connections, string activeConnection, int maxThreads)
        {
            development = isDev;
            ConnectionStrings = connections;
            MaxThreads = maxThreads;
        }
        public void ProcessEnvironments()
        {
            //To be handled by windows schedular;
            //var lastMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            //while (true)
            //{
            //    if (DateTime.Now > lastMonth)
            //    {
            //        Console.WriteLine("Running");
            //        lastMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month + 1, 1);
            //        IterateEnvironment();
            //    }
            //    else
            //    {
            //        Console.WriteLine("Sleeping for the day..");
            //        DateTime startTime = DateTime.Now;
            //        DateTime endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 1);
            //        TimeSpan span = endTime.Subtract(startTime);
            //        Thread.Sleep(span);
            //    }
            //}
            IterateEnvironment();
        }

        private void IterateEnvironment()
        {
            var Targetconnection = ConnectionStrings.Where(i => i.Key == "Target").FirstOrDefault();
            using (SemaphoreSlim concurrencySemaphore = new SemaphoreSlim(20))
            {
                List<Task> tasks = new List<Task>();
                foreach (var connection in ConnectionStrings.Where(i => i.Key != "Target")) 
                {
                    concurrencySemaphore.Wait();
                    var t = Task.Factory.StartNew(() =>
                    {
                        try
                        {
                            var cons = new Dictionary<string, string>();
                            cons.Add(connection.Key, connection.Value);
                            cons.Add(Targetconnection.Key, Targetconnection.Value);
                            var active = new KeyValuePair<string, string>();
                            active = connection;
                            var process = new RenewalProcess(development, cons, active, MaxThreads);
                            lock (process)
                            {
                                process.ProcessTasks();
                            }
                        }
                        catch (Exception ex)
                        {
                            var util = new Utility();
                            util.LogError(ex, connection.Key);
                            Console.WriteLine(connection.Key + " Environment Thread failed :" + ex.Message);
                        }
                        finally
                        {
                            concurrencySemaphore.Release();
                        }
                    });
                    tasks.Add(t);
                }
                Task.WaitAll(tasks.ToArray());
            }
        }



    }
}
