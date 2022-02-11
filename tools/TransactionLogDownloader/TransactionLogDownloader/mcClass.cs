using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TransactionLogDownloader
{
    public class mcClass
    {
        static string username = "fdus";
        static string password = "392potrero";
        static string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{username}:{password}"));

    }
    public class MCFileInfo
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }
        [JsonPropertyName("filename")]
        public String FileName { get; set; }
        [JsonPropertyName("filetype")]
        public String FileType { get; set; }
        [JsonPropertyName("md5")]
        public String MD5 { get; set; }
        [JsonPropertyName("size")]
        public int Size { get; set; }
        [JsonPropertyName("uploadtime")]
        public String UploadTime { get; set; }
        [JsonPropertyName("macaddress")]
        public String MacAddress { get; set; }
        [JsonPropertyName("pcname")]
        public String PCName { get; set; }
    }
    public class MCFileList
    {
        //[JsonPropertyName("status")]
        public int Status { get; set; }
        //[JsonPropertyName("istruncated")]
        public Boolean isTruncated { get; set; }
        //[JsonPropertyName("files")]
        public List<MCFileInfo> Files { get; set; }
    }
}
