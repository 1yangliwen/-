using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FTPClient
{
    public class File
    {
        public int FileID { get; set; }
        public int ServerID { get; set; }
        public string FileName { get; set; }
        public string RemoteFilePath { get; set; }
        public string LocalFilePath { get; set; }
        //public string Action { get; set; }
        public DateTime Time { get; set; }
    }
}
