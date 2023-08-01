using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EasyFTP.Models;
using FTPClient;
using Microsoft.EntityFrameworkCore;

namespace EasyFTP
{
    public class FtpClient
    {
        private string server;
        private string user;
        private string password;
        private int port;
        private TcpClient client;
        private DatabaseHandler dbHandler;  // Add a DatabaseHandler member

        public FtpClient(string server, string user, string password, int port = 21, string dbConnectionString = null)
        {
            this.server = server;
            this.user = user;
            this.password = password;
            this.port = port;
            this.client = new TcpClient();
            this.dbHandler = new DatabaseHandler(dbConnectionString);  // Initialize the DatabaseHandler
        }

        public void Connect()
        {
            try
            {
                this.client.Connect(this.server, this.port);
                this.ReadResponse();

                this.SendCommand($"USER {this.user}");
                this.ReadResponse();

                this.SendCommand($"PASS {this.password}");
                this.ReadResponse();
            }
            catch (Exception ex)
            {
                // Handle any errors that might have occurred.
                Console.WriteLine($"An error occurred while connecting to the server: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            if (this.client.Connected)
            {
                this.SendCommand("QUIT");
                this.client.Close();
            }
        }



        public void DownloadFile(string remoteFilePath, string localFilePath)
        {
            try
            {
                this.SendCommand("TYPE I"); // Set transfer mode to binary
                string response = this.ReadResponse(); // Server should respond with "200 Type set to I"
                                                       //Console.WriteLine("Response to TYPE I: " + response); // Print the response

                this.SendCommand($"PASV"); // Request passive mode
                response = this.ReadResponse(); // Response should be in the format "227 Entering Passive Mode (h1,h2,h3,h4,p1,p2)"
                                                //Console.WriteLine("Response to PASV: " + response); // Print the response

                // Extract the IP and port from the response
                string[] parts = response.Split('(', ')')[1].Split(',');
                string ip = string.Join('.', parts, 0, 4);
                int port = (int.Parse(parts[4]) << 8) + int.Parse(parts[5]);

                TcpClient dataClient = null;
                try
                {
                    // Connect to the data port
                    dataClient = new TcpClient(ip, port);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error connecting to data port: {ex.Message}");
                    return;
                }

                long resumePosition = 0;
                if (System.IO.File.Exists(localFilePath))
                {
                    // If the local file exists, get its size and use it as the resume position
                    resumePosition = new FileInfo(localFilePath).Length;

                    // Send a REST command to start from the resume position
                    this.SendCommand($"REST {resumePosition}");
                    response = this.ReadResponse(); // Server should respond with "350 Restarting at {resumePosition}"
                }

                this.SendCommand($"RETR {remoteFilePath}");
                response = this.ReadResponse(); // Get response to RETR
                                                //Console.WriteLine("Response to RETR: " + response); // Print the response
                                                // Create a FtpServer object and add it to the database

                // Get or create the FtpServer object
                //FtpServer ftpServer = dbHandler.GetFtpServer(this.server, this.user, this.password, this.port);
                string FileName = Path.GetFileName(remoteFilePath);
                // Create a File object and add it to the database
                // Get or create the File object
                FTPClient.File file = dbHandler.GetFile(remoteFilePath, localFilePath);

                // Create a Transfer object
                Transfer transfer = new Transfer
                {
                    //ftpServer = ftpServer,
                    File = FileName,
                    Type = TransferType.Download,
                    Status = TransferStatus.Started,
                    StartTime = DateTime.Now  // Record the start time of the transfer
                };
                dbHandler.AddTransfer(transfer);
                using (FileStream fs = new FileStream(localFilePath, FileMode.Append))
                {
                    // Now receive the file data on the data connection, starting from the resume position
                    byte[] buffer = new byte[1024];
                    int bytesRead = 0;
                    while ((bytesRead = dataClient.GetStream().Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fs.Write(buffer, 0, bytesRead);
                    }
                }


                response = this.ReadResponse(); // Server should respond with "226 Transfer complete"
                                                //Console.WriteLine("Final response: " + response); // Print the final response
                dataClient.Close();
                dbHandler.UpdateTransfer(transfer);
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"An error occurred while updating the database: {ex.Message}");
                Console.WriteLine($"Detailed error: {ex.InnerException?.Message}");

                foreach (var entity in ex.Entries)
                {
                    Console.WriteLine($"Entity that caused the error: {entity.Entity}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while downloading the file: {ex.Message}");
            }
        }



        public void UploadFile(string localFilePath)
        {
            try
            {
                this.SendCommand("TYPE I"); // Set transfer mode to binary
                string response = this.ReadResponse(); // Server should respond with "200 Type set to I"

                this.SendCommand($"PASV"); // Request passive mode
                response = this.ReadResponse(); // Response should be in the format "227 Entering Passive Mode (h1,h2,h3,h4,p1,p2)"

                // Extract the IP and port from the response
                string[] parts = response.Split('(', ')')[1].Split(',');
                string ip = string.Join('.', parts, 0, 4);
                int port = (int.Parse(parts[4]) << 8) + int.Parse(parts[5]);

                // Connect to the data port
                TcpClient dataClient = new TcpClient(ip, port);
                //var ftpServer = dbHandler.GetFtpServer(this.server, this.user, this.password, this.port);
                string fileName = Path.GetFileName(localFilePath);
                long resumePosition = 0;

                // Check if the remote file already exists
                if (FileExistsOnServer(fileName))
                {
                    // Get the size of the remote file
                    long remoteFileSize = GetRemoteFileSize(fileName);

                    // Check if the local file size matches the remote file size
                    FileInfo fileInfo = new FileInfo(localFilePath);
                    if (fileInfo.Length == remoteFileSize)
                    {
                        Console.WriteLine("The file already exists on the server with the same size. No need to upload.");
                        dataClient.Close();
                        return;
                    }

                    // Set the resume position to the remote file size
                    resumePosition = remoteFileSize;

                    // Send a REST command to start from the remote file size
                    this.SendCommand($"REST {resumePosition}");
                    response = this.ReadResponse(); // Server should respond with "350 Restarting at {resumePosition}"
                }
                // Create a Transfer object and add it to the database
                Transfer transfer = new Transfer
                {
                    //ftpServer = ftpServer,
                    File = fileName,
                    Type = TransferType.Upload,
                    Status = TransferStatus.Started,
                    StartTime = DateTime.Now  // Record the start time of the transfer
                };
                dbHandler.AddTransfer(transfer);
                // Start the upload from the resume position
                this.SendCommand($"STOR {fileName}");
                response = this.ReadResponse(); // Server should respond with "150 Opening BINARY mode data connection"

                // Now send the file data on the data connection, starting from the resume position
                using (FileStream fs = new FileStream(localFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    fs.Seek(resumePosition, SeekOrigin.Begin);
                    byte[] buffer = new byte[1024];
                    int bytesRead = 0;
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        dataClient.GetStream().Write(buffer, 0, bytesRead);
                    }
                }
                //response = this.ReadResponse(); // Server should respond with "226 Transfer complete"
                dataClient.Close();
                dbHandler.UpdateTransfer(transfer);
            }
            catch (DbUpdateException ex)
            {
                Console.WriteLine($"An error occurred while saving the entity changes. Details: {ex.InnerException?.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while uploading the file: {ex.Message}");
            }
        }


        private bool FileExistsOnServer(string fileName)
        {
            this.SendCommand($"SIZE {fileName}");
            string response = this.ReadResponse();
            return response.StartsWith("213 ");
        }

        private long GetRemoteFileSize(string fileName)
        {
            this.SendCommand($"SIZE {fileName}");
            string response = this.ReadResponse();
            if (response.StartsWith("213 "))
            {
                long size;
                if (long.TryParse(response.Substring(4), out size))
                {
                    return size;
                }
            }
            return 0;
        }

        private void SendCommand(string command)
        {
            if (this.client.Connected)
            {
                var writer = new StreamWriter(this.client.GetStream()) { AutoFlush = true };
                writer.WriteLine(command);
                writer.Flush();  // Flush the writer to make sure the command is sent immediately
            }
        }



        private string ReadResponse()
        {
            if (this.client.Connected)
            {
                var reader = new StreamReader(this.client.GetStream(), Encoding.ASCII);
                string response = reader.ReadLine();
                Console.WriteLine(response);
                return response;
            }
            return null;
        }



        public string[] ListDirectory(string remoteDirectory)
        {
            try
            {
                this.SendCommand("TYPE A"); // Set transfer mode to ASCII
                string response = this.ReadResponse(); // Server should respond with "200 Type set to A"

                this.SendCommand($"PASV"); // Request passive mode
                response = this.ReadResponse(); // Response should be in the format "227 Entering Passive Mode (h1,h2,h3,h4,p1,p2)"

                // Extract the IP and port from the response
                string[] splitResponse = response.Split('(', ')');
                if (splitResponse.Length < 2)
                {
                    throw new Exception("Invalid server response: " + response);
                }
                string[] parts = splitResponse[1].Split(',');

                string ip = string.Join('.', parts, 0, 4);
                int port = (int.Parse(parts[4]) << 8) + int.Parse(parts[5]);

                // Connect to the data port
                TcpClient dataClient = new TcpClient(ip, port);

                this.SendCommand($"LIST {remoteDirectory}");

                // Now read the directory listing from the data connection
                var reader = new StreamReader(dataClient.GetStream(), Encoding.ASCII);
                string listing = reader.ReadToEnd();
                dataClient.Close();

                this.ReadResponse(); // Server should respond with "226 Transfer complete"

                // Extract the filename from each line and return it in an array
                return Regex.Split(listing, @"\r\n|\r|\n")
                    .Select(s => s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    .Where(parts => parts.Length > 8)
                    .Select(parts => string.Join(" ", parts.Skip(8)))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while listing the directory: {ex.Message}");
                return null;
            }
        }


        public string[] ListRemoteDirectory()
        {
            try
            {
                // Set transfer mode to ASCII
                this.SendCommand("TYPE A");
                string response = this.ReadResponse();

                // Request passive mode
                this.SendCommand("PASV");
                response = this.ReadResponse();

                // Extract the IP and port from the response
                string[] splitResponse = response.Split('(', ')');
                if (splitResponse.Length < 2)
                {
                    throw new Exception("Invalid server response: " + response);
                }
                string[] parts = splitResponse[1].Split(',');

                string ip = string.Join('.', parts, 0, 4);
                int port = (int.Parse(parts[4]) << 8) + int.Parse(parts[5]);

                // Connect to the data port
                TcpClient dataClient = new TcpClient(ip, port);

                // Send the LIST command to retrieve the directory listing
                this.SendCommand("LIST");

                // Now read the directory listing from the data connection
                var reader = new StreamReader(dataClient.GetStream(), Encoding.ASCII);
                string listing = reader.ReadToEnd();
                dataClient.Close();

                this.ReadResponse(); // Server should respond with "226 Transfer complete"

                // Extract the filename from each line and return it in an array
                return Regex.Split(listing, @"\r\n|\r|\n")
                            .Select(s => s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                            .Where(parts => parts.Length > 8)
                            .Select(parts => string.Join(" ", parts.Skip(8)))
                            .Where(s => !string.IsNullOrWhiteSpace(s))
                            .ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while listing the remote directory: {ex.Message}");
                return null;
            }
        }
    }
}