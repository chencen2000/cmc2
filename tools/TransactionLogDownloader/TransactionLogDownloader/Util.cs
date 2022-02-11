using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace TransactionLogDownloader
{
    public class Util
    {
        static string username = "fdus";
        static string password = "392potrero";
        static string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));

        public static void test()
        {
            WebClient wc = new();
            NameValueCollection values = new NameValueCollection();
            values.Add("pcname", "Chris-PC");
            values.Add("macaddress", "020000000000");
            values.Add("filetype", "PCFiles");
            values.Add("filename", "HydraV5_CMCInstaller_v1.46.exe");
            wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            wc.Headers[HttpRequestHeader.Authorization] = $"Basic {credentials}";
            byte[] res = wc.UploadValues("https://mc.futuredial.com/getfileinfo/", "POST", values);
            string s = System.Text.Encoding.UTF8.GetString(res);
        }
        public static JsonNode[] getFileList(string machineName, string fileType= "TransactionLog")
        {
            WebClient wc = new();
            List<JsonNode> ret = new();
            try
            {
                bool done = false;
                string nextcontinuationtoken = "";
                while (!done)
                {
                    NameValueCollection values = new NameValueCollection();
                    values.Add("pcname", machineName);
                    values.Add("filetype", fileType);
                    if (!string.IsNullOrEmpty(nextcontinuationtoken))
                        values.Add("nextcontinuationtoken", nextcontinuationtoken);
                    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    wc.Headers[HttpRequestHeader.Authorization] = $"Basic {credentials}";
                    byte[] res = wc.UploadValues("https://mc.futuredial.com/getfiles/", "POST", values);
                    JsonNode data = JsonNode.Parse(res);
                    if ((int)data["status"] == 0)
                    {
                        done = !(bool)data["istruncated"];
                        nextcontinuationtoken = (string) data["nextcontinuationtoken"];
                        if (data["files"] != null && data["files"].GetType() == typeof(System.Text.Json.Nodes.JsonArray))
                        {
                            //JsonObject[] l = (JsonObject[])((JsonArray)data["files"]).ToArray<JsonObject[]>();
                            //ret.AddRange((JsonArray)data["files"]);
                            var l = ((JsonArray)data["files"]).ToArray();
                            ret.AddRange((JsonNode[])l);
                        }
                    }
                    else
                    {
                        done = true;
                    }
                }
            }
            catch (Exception) { }
            return ret.ToArray();
        }
    }
}
