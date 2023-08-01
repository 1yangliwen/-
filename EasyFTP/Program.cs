namespace EasyFTP
{
    internal static class Program
    {
        static void Main()
        {
            string server = "127.0.0.1";
            string user = "test";
            string password = "123456";

            string localDirectory = @"E:\FTPtest";  // Set this to your local path
            string remoteDirectory = "";   // Set this to your remote path

            while (true)
            {
                Console.WriteLine("请选择一个操作:");
                Console.WriteLine("1. 列出本地目录");
                Console.WriteLine("2. 列出远程目录");
                Console.WriteLine("3. 上传文件");
                Console.WriteLine("4. 下载文件");
                Console.WriteLine("5. 退出");

                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        // List local directory
                        string[] localFiles = Directory.GetFiles(localDirectory);
                        for (int i = 0; i < localFiles.Length; i++)
                        {
                            Console.WriteLine($"{i + 1}. {Path.GetFileName(localFiles[i])}");
                        }
                        break;

                    case "2":
                        // List remote directory
                        FtpClient clientForList = new FtpClient(server, user, password);
                        clientForList.Connect();
                        string[] remoteFiles = clientForList.ListDirectory(remoteDirectory);
                        clientForList.Disconnect();
                        for (int i = 0; i < remoteFiles.Length; i++)
                        {
                            Console.WriteLine($"{i + 1}. {remoteFiles[i]}");
                        }
                        break;

                    case "3":
                        // Upload a file
                        localFiles = Directory.GetFiles(localDirectory);
                        for (int i = 0; i < localFiles.Length; i++)
                        {
                            Console.WriteLine($"{i + 1}. {Path.GetFileName(localFiles[i])}");
                        }
                        Console.WriteLine("请输入要上传的文件的编号，或输入0返回:");
                        int localFileIndex = int.Parse(Console.ReadLine()) - 1;
                        if (localFileIndex >= 0)
                        {
                            FtpClient clientForUpload = new FtpClient(server, user, password);
                            clientForUpload.Connect();
                            clientForUpload.UploadFile(localFiles[localFileIndex]);
                            clientForUpload.Disconnect();
                        }
                        break;

                    case "4":
                        // Download a file
                        FtpClient clientForListAgain = new FtpClient(server, user, password);
                        clientForListAgain.Connect();
                        remoteFiles = clientForListAgain.ListDirectory(remoteDirectory);
                        clientForListAgain.Disconnect();
                        for (int i = 0; i < remoteFiles.Length; i++)
                        {
                            Console.WriteLine($"{i + 1}. {remoteFiles[i]}");
                        }
                        Console.WriteLine("请输入要下载的文件的编号，或输入0返回:");
                        int remoteFileIndex = int.Parse(Console.ReadLine()) - 1;
                        if (remoteFileIndex >= 0)
                        {
                            FtpClient clientForDownload = new FtpClient(server, user, password);
                            clientForDownload.Connect();
                            clientForDownload.DownloadFile(remoteFiles[remoteFileIndex], Path.Combine(localDirectory, remoteFiles[remoteFileIndex]));
                            clientForDownload.Disconnect();
                        }
                        break;

                    case "5":
                        // Exit
                        return;

                    default:
                        Console.WriteLine("无效的选择，请重新选择.");
                        break;
                }
            }

        }
    }
}