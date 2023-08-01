using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasyFTP.Models;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace FTPClient
{
    public class DatabaseHandler : DbContext
    {
        private MyDbContext dbContext;

        public DatabaseHandler(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MyDbContext>();
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 33));  //这个最好改为自己数据库的版本

            // If connectionString is null or empty, use the default connection string
            if (string.IsNullOrEmpty(connectionString))
            {
                connectionString = "server=localhost;database = ftp;Uid=root;Pwd=xyw200431";
            }

            optionsBuilder.UseMySql(connectionString, serverVersion);
            dbContext = new MyDbContext(optionsBuilder.Options);
            dbContext.Database.EnsureCreated();

            // Add this code to test your DbContext
            try
            {
                dbContext.Database.CanConnect();
                Console.WriteLine("Successfully connected to the database.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect to the database: {ex.Message}");
            }
        }
        /* public void AddFtpServer(FtpServer ftpServer)
         {
             dbContext.FtpServers.Add(ftpServer);
             dbContext.SaveChanges();
         }*/

        public void AddFile(File file)
        {
            dbContext.Files.Add(file);
            dbContext.SaveChanges();
        }

        public File GetFile(string remoteFilePath, string localFilePath)
        {
            // Try to get the File object from the database
            File file = dbContext.Files.FirstOrDefault(f => f.RemoteFilePath == remoteFilePath && f.LocalFilePath == localFilePath);

            // If the File object does not exist, create a new one
            if (file == null)
            {
                file = new File
                {
                    RemoteFilePath = remoteFilePath,
                    LocalFilePath = localFilePath,
                    // Other properties like Size and LastModified can be set later
                };
            }

            return file;
        }

        public void AddTransfer(Transfer transfer)
        {
            dbContext.Transfers.Add(transfer);
            dbContext.SaveChanges();
        }

        public void UpdateTransfer(Transfer transfer)
        {
            var existingTransfer = dbContext.Transfers
                .FirstOrDefault(t => t.File == transfer.File && t.Type == transfer.Type && t.StartTime == transfer.StartTime);

            if (existingTransfer != null)
            {
                existingTransfer.EndTime = DateTime.Now;
                existingTransfer.Status = TransferStatus.Completed;
                dbContext.SaveChanges();
            }
            else
            {
                Console.WriteLine("No matching transfer found to update.");
            }
        }


        public List<File> GetAllFiles()
        {
            return dbContext.Files.ToList();
        }



        public List<Transfer> GetAllTransfers()
        {
            return dbContext.Transfers.ToList();
        }
        public List<TransferDTO> GetAllUploadTransfersDTO()
        {
            return dbContext.Transfers
                .Where(t => t.Type == TransferType.Upload)
                .Select(t => new TransferDTO
                {
                    File = t.File,
                    Type = t.Type.ToString(),
                    Status = t.Status.ToString(),
                    StartTime = t.StartTime.ToString(),
                    EndTime = t.EndTime.ToString()
                })
                .ToList();
        }

        public List<TransferDTO> GetAllDownloadTransfersDTO()
        {
            return dbContext.Transfers
                .Where(t => t.Type == TransferType.Download)
                .Select(t => new TransferDTO
                {
                    File = t.File,
                    Type = t.Type.ToString(),
                    Status = t.Status.ToString(),
                    StartTime = t.StartTime.ToString(),
                    EndTime = t.EndTime.ToString()
                })
                .ToList();
        }
    }

    public class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
        {
            this.Database.EnsureCreated();
        }
        // Overriding OnConfiguring method
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Make sure to call base method to apply configurations from constructor
            base.OnConfiguring(optionsBuilder);

            // You can add more configurations here if needed
        }
        //public DbSet<FtpServer> FtpServers { get; set; }
        public DbSet<File> Files { get; set; }
        public DbSet<Transfer> Transfers { get; set; }
    }

}