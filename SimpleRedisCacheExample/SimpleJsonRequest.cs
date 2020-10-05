using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Threading.Tasks;

namespace SimpleRedisCacheExample
{
    class SimpleJsonRequest
    {
        public static async Task<string> GetAsync(string url)
        {
            string cachedValue = CheckCache(url);
            if (!string.IsNullOrWhiteSpace(cachedValue))
            {
                Console.WriteLine(String.Format("using cached response for url:{0}", url));
                return cachedValue;
            }

            HttpRequestCachePolicy requestPolicy = new HttpRequestCachePolicy(HttpCacheAgeControl.MaxAge, TimeSpan.FromDays(1));
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.CachePolicy = requestPolicy;

            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                string result = await reader.ReadToEndAsync();
                Console.WriteLine(String.Format("Response achieved for url:{0} , direclty from server", url));
                SaveDataToCache(url, result);
                return result;
            }
        }

        private static string CheckCache(string url)
        {
            var cache = RedisConnectorHelper.Connection.GetDatabase();
            var value = cache.StringGet($"Device_Status:{url}");
            Console.WriteLine($"Valor={value}");

            return value;
        }

        private static void SaveDataToCache(string url, string data)
        {
            var cache = RedisConnectorHelper.Connection.GetDatabase();
            cache.StringSet($"Device_Status:{url}", data);
            TimeSpan keyTimeOut = new TimeSpan(1000000);

            cache.KeyExpire($"Device_Status:{url}", keyTimeOut, StackExchange.Redis.CommandFlags.None);
            Console.WriteLine(String.Format("Saved url:{0} ,response to cache, with a Time to expire {1}", url, keyTimeOut));
        }

        public static async Task<string> PostAsync(string url, Dictionary<string, string> data)
        {
            return await RequestAsync("POST", url, data);
        }

        public static async Task<string> PutAsync(string url, Dictionary<string, string> data)
        {
            return await RequestAsync("PUT", url, data);
        }

        public static async Task<string> PatchAsync(string url, Dictionary<string, string> data)
        {
            return await RequestAsync("PATCH", url, data);
        }

        public static async Task<string> DeleteAsync(string url, Dictionary<string, string> data)
        {
            return await RequestAsync("DELETE", url, data);
        }

        private static async Task<string> RequestAsync(string method, string url, Dictionary<string, string> data)
        {
            string dataString = JsonConvert.SerializeObject(data);
            byte[] dataBytes = Encoding.UTF8.GetBytes(dataString);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.ContentLength = dataBytes.Length;
            request.ContentType = "application/json";
            request.Method = method;

            using (Stream requestBody = request.GetRequestStream())
            {
                await requestBody.WriteAsync(dataBytes, 0, dataBytes.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}
