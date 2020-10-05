using System;

namespace SimpleRedisCacheExample
{
    class Program
    {
        static async System.Threading.Tasks.Task Main()
        {
            string url = "https://jsonplaceholder.typicode.com/users";
            for (int i = 0; i < 3; i++)
            {
                var result = await SimpleJsonRequest.GetAsync(url);
            }
            System.Threading.Thread.Sleep(1000);

            var resultNonCache = await SimpleJsonRequest.GetAsync(url);
        }
    }
}
