using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace MinerControl.Utility
{
    public static class WebUtil
    {
        public static void DownloadJson(string url, Action<object> jsonProcessor)
        {
            try
            {
                using (var client = new WebClient())
                {
                    var uri = new Uri(url);
                    client.Encoding = Encoding.UTF8;
                    client.DownloadStringCompleted += DownloadJsonComplete;
                    client.DownloadStringAsync(uri, jsonProcessor);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex);
            }
        }

        private static void DownloadJsonComplete(object sender, DownloadStringCompletedEventArgs e)
        {
            try
            {
                var pageString = e.Result;
                if (pageString == null) return;
                var jsonProcessor = e.UserState as Action<object>;
                var serializer = new JavaScriptSerializer();
                var data = serializer.DeserializeObject(pageString);

                jsonProcessor(data);
            }
            catch (Exception ex)
            {
                ErrorLogger.Log(ex);
            }
        }
    }
}
