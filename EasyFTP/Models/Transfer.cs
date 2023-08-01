using FTPClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyFTP.Models
{
    public enum TransferType
    {
        Upload,
        Download
    }

    public enum TransferStatus
    {
        Started,
        Completed
    }

    public class Transfer
    {
        public int Id { get; set; }
        public string File { get; set; }
        public TransferType Type { get; set; }
        public TransferStatus Status { get; set; }
        //public long FileSize { get; set; } // Add this line to store file size
        public DateTime StartTime { get; set; } // Add this line to store transfer time
        public DateTime EndTime { get; set; } // Add this line to store transfer time
    }

    public class TransferDTO
    {
        public string File { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
    }

}