using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sunny.UI;
using EasyFTP;
using FTPClient;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolBar;

namespace FTPWinForm
{
    // 继承自UIForm，表示一个FTP连接窗体
    public partial class Link : UIForm
    {
        // FTP客户端连接对象
        FtpClient link;

        // 本地文件路径
        string localPath = "E:\\FTPtest";// it's your local path

        // 远程文件路径
        string remotePath = "F:\\FTPWin";// it's the remote path

        // 数据库处理器，用于处理FTP上传和下载的历史记录
        DatabaseHandler databaseHandler = new DatabaseHandler("server=localhost;database=ftp;Uid=root;Pwd=xyw200431");

        public Link()
        {
            InitializeComponent();

            // 在初始化时，禁用上传、下载、断开连接按钮
            dcButton.Enabled = false;
            ulButton.Enabled = false;
            dlButton.Enabled = false;

            // 将数据库中的上传和下载历史记录绑定到DataGridView
            transferDTOBindingSource1.DataSource = databaseHandler.GetAllUploadTransfersDTO();
            transferDTOBindingSource2.DataSource = databaseHandler.GetAllDownloadTransfersDTO();

            // 不允许DataGridView添加新行
            uiDataGridView1.AllowUserToAddRows = false;
            uiDataGridView2.AllowUserToAddRows = false;

            // 显示本地目录文件结构到TreeView中
            PaintTreeView(treeViewFiles, localPath);
        }

        // 在TreeView中显示指定路径的文件结构
        private void PaintTreeView(UITreeView uiTreeView, string fullPath)
        {
            try
            {
                uiTreeView.Nodes.Clear(); // 清空TreeView

                // 获得指定路径的目录对象
                DirectoryInfo dirs = new DirectoryInfo(fullPath);
                // 获得目录下文件夹对象和文件对象
                DirectoryInfo[] dir = dirs.GetDirectories();
                FileInfo[] file = dirs.GetFiles();
                int dircount = dir.Count(); // 获得文件夹对象数量
                int filecount = file.Count(); // 获得文件对象数量

                // 循环添加文件夹节点
                for (int i = 0; i < dircount; i++)
                {
                    uiTreeView.Nodes.Add(dir[i].Name);
                    string pathNode = fullPath + "\\" + dir[i].Name;
                    GetMultiNode(uiTreeView.Nodes[i], pathNode);
                }

                // 循环添加文件节点
                for (int j = 0; j < filecount; j++)
                {
                    uiTreeView.Nodes.Add(file[j].Name);
                }
            }
            catch (Exception ex)
            {
                // 捕获并显示异常信息
                UIMessageBox.Show(ex.Message + "\r\n出错的位置为：Link.PaintTreeView()");
            }
        }

        // 递归获取多级目录节点
        private bool GetMultiNode(TreeNode treeNode, string path)
        {
            if (Directory.Exists(path) == false)
            {
                return false;
            }

            // 获得指定路径的目录对象
            DirectoryInfo dirs = new DirectoryInfo(path);
            // 获得目录下文件夹对象和文件对象
            DirectoryInfo[] dir = dirs.GetDirectories();
            FileInfo[] file = dirs.GetFiles();
            int dircount = dir.Count(); // 获得文件夹对象数量
            int filecount = file.Count(); // 获得文件对象数量
            int sumcount = dircount + filecount;

            if (sumcount == 0)
            {
                return false;
            }

            // 循环添加文件夹节点
            for (int j = 0; j < dircount; j++)
            {
                treeNode.Nodes.Add(dir[j].Name);
                string pathNodeB = path + "\\" + dir[j].Name;
                GetMultiNode(treeNode.Nodes[j], pathNodeB);
            }

            // 循环添加文件节点
            for (int j = 0; j < filecount; j++)
            {
                treeNode.Nodes.Add(file[j].Name);
            }

            return true;
        }

        // 点击连接按钮时触发的事件
        private void linkButton_Click(object sender, EventArgs e)
        {
            // 获取用户输入的服务器地址、用户名、密码和端口号
            string server, user, password;
            int port;
            server = serverTextBox.Text;
            user = userTextBox.Text;
            password = pwTextBox.Text;
            port = Convert.ToInt32(portTextBox.Text);

            // 创建FTP客户端连接对象，并尝试连接服务器
            link = new FtpClient(server, user, password, port);
            link.Connect();
            UIMessageBox.Show("连接成功");

            // 连接成功后，启用上传、下载、断开连接按钮，并显示远程目录结构到remoteTreeView中
            dcButton.Enabled = true;
            ulButton.Enabled = true;
            dlButton.Enabled = true;
            // 这里需要得到server的根目录
            PaintTreeView(remoteTreeView, remotePath);
        }

        // 点击断开连接按钮时触发的事件
        private void dcButton_Click(object sender, EventArgs e)
        {
            // 断开连接，并清空remoteTreeView中的节点
            link.Disconnect();
            UIMessageBox.Show("断开连接");
            remoteTreeView.Nodes.Clear();
        }

        // 点击上传按钮时触发的事件
        private void ulButton_Click(object sender, EventArgs e)
        {
            uiProcessBar2.Value = 0;
            // 判断选择的是文件夹或者没有选择文件
            if (treeViewFiles.SelectedNode == null || (!treeViewFiles.SelectedNode.Text.Contains(".")))
            {
                UIMessageBox.Show("请选择一个文件");
                return;
            }

            // 获取选择文件的完整路径
            TreeNode tr1 = treeViewFiles.SelectedNode.Parent;
            string thisPath = localPath;
            while (tr1 != null)
            {
                thisPath += "\\";
                thisPath += tr1.Text;
                tr1 = tr1.Parent;
            }
            thisPath += $"\\{treeViewFiles.SelectedNode.Text}";
            uiTextBox5.Text = treeViewFiles.SelectedNode.Text;

            // 执行上传操作，并刷新上传历史记录和remoteTreeView中的节点
            link.UploadFile(thisPath);
            transferDTOBindingSource1.DataSource = databaseHandler.GetAllUploadTransfersDTO();
            PaintTreeView(remoteTreeView, remotePath);
        }

        // 点击下载按钮时触发的事件
        private void dlButton_Click(object sender, EventArgs e)
        {
            uiProcessBar2.Value = 0;
            // 判断选择的是文件夹或者没有选择文件
            if (remoteTreeView.SelectedNode == null || (!remoteTreeView.SelectedNode.Text.Contains(".")))
            {
                UIMessageBox.Show("请选择一个文件");
                return;
            }

            // 获取选择文件的完整路径
            uiTextBox6.Text = remoteTreeView.SelectedNode.Text;
            TreeNode tr1 = remoteTreeView.SelectedNode.Parent;
            string thisPath = remotePath;
            while (tr1 != null)
            {
                thisPath += "\\";
                thisPath += tr1.Text;
                tr1 = tr1.Parent;
            }
            thisPath += $"\\{remoteTreeView.SelectedNode.Text}";

            // 执行下载操作，并刷新下载历史记录和treeViewFiles中的节点
            link.DownloadFile(thisPath, localPath);
            uiProcessBar2.Value = 100;
            transferDTOBindingSource2.DataSource = databaseHandler.GetAllDownloadTransfersDTO();
            PaintTreeView(treeViewFiles, localPath);
        }

        // 点击停止上传按钮时触发的事件
        private void uiButton6_Click(object sender, EventArgs e)
        {
            // 断开FTP连接，并弹出消息框提示停止上传
            link.Disconnect();
            MessageBox.Show("停止上传");
        }

        // 点击恢复下载按钮时触发的事件
        private void uiButton7_Click(object sender, EventArgs e)
        {
            // 连接FTP服务器，执行下载操作，并刷新下载历史记录和treeViewFiles中的节点
            link.Connect();
            uiTextBox6.Text = remoteTreeView.SelectedNode.Text;
            TreeNode tr1 = remoteTreeView.SelectedNode.Parent;
            string thisPath = remotePath;
            while (tr1 != null)
            {
                thisPath += "\\";
                thisPath += tr1.Text;
                tr1 = tr1.Parent;
            }
            thisPath += $"\\{remoteTreeView.SelectedNode.Text}";
            link.DownloadFile(thisPath, localPath);
            transferDTOBindingSource2.DataSource = databaseHandler.GetAllDownloadTransfersDTO();
            PaintTreeView(treeViewFiles, localPath);
        }
    }
}
