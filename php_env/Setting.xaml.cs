using MahApps.Metro.Controls;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace php_env
{
    /// <summary>
    /// Setting.xaml 的交互逻辑
    /// </summary>
    public partial class Setting : MetroWindow
    {
        public int taskCount = 0;

        public Setting(MainWindow mainWin)
        {
            this.Owner = mainWin;
            this.Resources.Add("statusConverter", new InstallResultConverter());
            this.Resources.Add("btnConverter", new InstallButtonConverter());
            InitializeComponent();
            this.phpList.DataContext = mainWin.phpList;
            this.nginxList.DataContext = mainWin.nginxList;
            this.vcList.DataContext = mainWin.vcList;
        }

        private void showCommonStatus(Label textLabel, MetroProgressBar progressBar)
        {
            textLabel.Content = "无任务";
            progressBar.Visibility = Visibility.Hidden;
        }

        private void showPendingStatus(Label textLabel, MetroProgressBar progressBar, string text)
        {
            textLabel.Content = text;
            progressBar.Visibility = Visibility.Visible;
            progressBar.IsIndeterminate = true;
        }

        private void showProcessStatus(Label textLabel, MetroProgressBar progressBar, string text, double processed, double total)
        {
            textLabel.Content = text;
            progressBar.Visibility = Visibility.Visible;
            progressBar.IsIndeterminate = false;
            if (total != progressBar.Maximum)
            {
                progressBar.Minimum = 0;
                progressBar.Maximum = total;
            }
            progressBar.Value = processed;
        }

        /// <summary>
        /// 任务失败时执行
        /// </summary>
        /// <param name="message">错误消息</param>
        private void onItemTaskFailed(AppItem appItem, string message, string title = "出错了")
        {
            MainWindow mainWin = this.Owner as MainWindow;
            //
            DataGrid dataGrid = null;
            Label statusText = null;
            MetroProgressBar progressBar = null;
            //组件
            if (appItem.type == AppType.php)
            {
                dataGrid = this.phpList;
                statusText = this.phpStatusText;
                progressBar = this.phpStatus;
            }
            else if (appItem.type == AppType.nginx)
            {
                dataGrid = this.nginxList;
                statusText = this.nginxStatusText;
                progressBar = this.nginxStatus;
            }
            else
            {
            }
            dataGrid.IsEnabled = true;
            this.taskCount--;
            this.showCommonStatus(statusText, progressBar);
            mainWin.showErrorMessage(message);
        }

        private void onItemTaskFailed(AppItem appItem, TaskResult result, string title = "出错了")
        {
            this.onItemTaskFailed(appItem, result.message, title);
        }

        /// <summary>
        /// 处理安装或者卸载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void mainAction(object sender, RoutedEventArgs e)
        {
            this.taskCount++;
            AppItem appItem = ((Button)sender).DataContext as AppItem;
            MainWindow mainWin = this.Owner as MainWindow;
            string appPath = mainWin.getAppPath(appItem);
            DirectoryInfo appPathInfo = new DirectoryInfo(appPath);
            string appName = Enum.GetName(typeof(AppType), appItem.type) + appItem.version;
            //
            DataGrid dataGrid = null;
            Label statusText = null;
            MetroProgressBar progressBar = null;
            //组件
            if (appItem.type == AppType.php)
            {
                dataGrid = this.phpList;
                statusText = this.phpStatusText;
                progressBar = this.phpStatus;
            }
            else if (appItem.type == AppType.nginx)
            {
                dataGrid = this.nginxList;
                statusText = this.nginxStatusText;
                progressBar = this.nginxStatus;
            }
            else
            {
            }
            //表格禁用
            dataGrid.IsEnabled = false;
            TaskResult result;
            if (appItem.installed)
            {
                /*卸载*/
                this.showPendingStatus(statusText, progressBar, "正在卸载" + appName);
                result = await this.deleteDir(appPathInfo);
                this.showCommonStatus(statusText, progressBar);
                if (!result.success)
                {
                    this.onItemTaskFailed(appItem, result);
                    return;
                }
                appItem.installed = false;
            }
            else
            {
                /*安装*/
                string zipPath = mainWin.getZipPath(appItem, false);
                string zipTmpPath = mainWin.getZipPath(appItem);
                FileInfo zipPathInfo = new FileInfo(zipPath);

                if (!zipPathInfo.Exists)
                {
                    //下载文件
                    this.showPendingStatus(statusText, progressBar, "下载" + appName);
                    result = await this.downloadFileAsync(appItem.downloadUrl, zipTmpPath, new Action<long, long>((long processed, long total) =>
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            if (total != -1)
                            {
                                this.showProcessStatus(statusText, progressBar, "下载" + appName, processed, total);
                            }
                        });
                    }));
                    if (!result.success)
                    {
                        //下载文件出错
                        this.onItemTaskFailed(appItem, result, "下载文件出错");
                        return;
                    }
                    try
                    {
                        //copy临时文件
                        FileInfo zipTmpPathInfo = new FileInfo(zipTmpPath);
                        zipTmpPathInfo.CopyTo(zipPath, true);
                        //删除临时文件
                        zipTmpPathInfo.Delete();
                    }
                    catch (Exception e1)
                    {
                        this.onItemTaskFailed(appItem, e1.Message, "移动临时文件出错");
                        return;
                    }
                }
                //创建目录
                if (!appPathInfo.Exists)
                {
                    this.showPendingStatus(statusText, progressBar, "正在创建目录");
                    try
                    {
                        appPathInfo.Create();
                    }
                    catch (Exception e1)
                    {
                        this.onItemTaskFailed(appItem, e1.Message, "创建应用目录失败");
                        return;
                    }
                }
                this.showPendingStatus(statusText, progressBar, "正在解压" + appName);
                //解压
                result = await this.extractFileAsync(zipPath, appPath);
                if (!result.success)
                {
                    //解压文件出错
                    this.onItemTaskFailed(appItem, result, "解压文件出错");
                    return;
                }
                //处理文件
                this.showPendingStatus(statusText, progressBar, "正在安装" + appName);
                if (appItem.type == AppType.php)
                {
                    FileInfo phpIniFile = new FileInfo(appPath + @"\php.ini-development");
                    if (phpIniFile.Exists)
                    {
                        try
                        {
                            phpIniFile.CopyTo(appPath + @"\php.ini", false);
                        }
                        catch (Exception e1)
                        {
                            //copy文件出错
                            this.onItemTaskFailed(appItem, e1.Message, "处理php.ini出错");
                            return;
                        }
                    }
                }
                else if (appItem.type == AppType.nginx)
                {
                    DirectoryInfo[] dirs = appPathInfo.GetDirectories();
                    result = await this.copyFiles(dirs[0], appPathInfo);
                    if (!result.success)
                    {
                        //移动文件出错
                        this.onItemTaskFailed(appItem, result, "移动文件出错");
                        return;
                    }
                    result = await this.deleteDir(dirs[0]);
                    if (!result.success)
                    {
                        //删除临时文件夹出错
                        this.onItemTaskFailed(appItem, result, "删除临时文件夹出错");
                        return;
                    }
                    //复制配置文件
                    result = await this.copyFiles(new DirectoryInfo(mainWin.getDefaultConfigPath(appItem)), new DirectoryInfo(appPath + @"\conf"));
                    if (!result.success)
                    {
                        this.onItemTaskFailed(appItem, result, "复制配置文件出错");
                        return;
                    }
                }
                //任务完成
                this.showCommonStatus(statusText, progressBar);
                appItem.installed = true;
            }
            dataGrid.IsEnabled = true;
            this.taskCount--;
        }

        /// <summary>
        /// 删除目录
        /// </summary>
        /// <param name="srcPath"></param>
        /// <returns></returns>
        public Task<TaskResult> deleteDir(DirectoryInfo path)
        {
            return Task<TaskResult>.Run(() =>
            {

                try
                {

                    if (path.Exists)
                    {
                        path.Delete(true);
                    }
                }
                catch (Exception e1)
                {
                    return new TaskResult(e1);
                }
                return new TaskResult();
            });
        }

        /// <summary>
        /// 复制文件
        /// </summary>
        /// <param name="srcPath">源文件夹目录</param>
        /// <param name="distPath">目标文件夹目录</param>
        /// <returns></returns>
        private Task<TaskResult> copyFiles(DirectoryInfo srcPath, DirectoryInfo distPath)
        {
            return Task<TaskResult>.Run(async () =>
            {
                try
                {
                    if (!distPath.Exists)
                    {
                        distPath.Create();
                    }
                    //复制文件
                    FileInfo[] files = srcPath.GetFiles();
                    int i;
                    for (i = 0; i < files.Length; i++)
                    {
                        files[i].CopyTo(distPath.FullName + @"\" + files[i].Name, true);
                    }
                    //复制目录
                    DirectoryInfo[] dirs = srcPath.GetDirectories();
                    TaskResult result;
                    for (i = 0; i < dirs.Length; i++)
                    {
                        result = await this.copyFiles(dirs[i], new DirectoryInfo(distPath.FullName + @"\" + dirs[i].Name));
                        if (!result.success)
                        {
                            return result;
                        }
                    }
                }
                catch (Exception e)
                {
                    return new TaskResult(e);
                }
                return new TaskResult();
            });
        }

        private Task<TaskResult> extractFileAsync(string zipPath, string savePath)
        {
            return Task<TaskResult>.Run(() =>
            {
                try
                {
                    ZipFile.ExtractToDirectory(zipPath, savePath);
                }
                catch (Exception e)
                {
                    return new TaskResult(e);
                }
                return new TaskResult();
            });
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="url">文件的URL地址</param>
        /// <param name="savePath">文件的保存路径</param>
        /// <param name="onFileSizeChange">当文件大小发生改变时执行</param>
        private Task<TaskResult> downloadFileAsync(string url, string savePath, Action<long, long> onFileSizeChange)
        {
            return Task<TaskResult>.Run(() =>
            {
                try
                {
                    //如果文件存在则删除文件
                    FileInfo savePathInfo = new FileInfo(savePath);
                    if (savePathInfo.Exists)
                    {
                        savePathInfo.Delete();
                    }
                    else
                    {
                        DirectoryInfo saveDir = savePathInfo.Directory;
                        if (!saveDir.Exists)
                        {
                            saveDir.Create();
                        }
                    }
                    //
                    HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                    using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                    {
                        long processed = 0;
                        long total = response.ContentLength;
                        using (Stream stream = response.GetResponseStream())
                        {
                            using (FileStream fs = new FileStream(savePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                            {
                                byte[] bArr = new byte[1024];
                                int size = stream.Read(bArr, 0, bArr.Length);
                                while (size > 0)
                                {
                                    fs.Write(bArr, 0, size);
                                    //更新下载数据的界面
                                    processed += (long)size;
                                    onFileSizeChange(processed, total);
                                    //
                                    size = stream.Read(bArr, 0, bArr.Length);
                                }
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                    return new TaskResult(e);
                }
                return new TaskResult();
            });
        }

        private void viewAction(object sender, RoutedEventArgs e)
        {
            AppItem appItem = ((Button)sender).DataContext as AppItem;
            MainWindow mainWin = this.Owner as MainWindow;
            System.Diagnostics.Process.Start(@"explorer.exe ", mainWin.getAppPath(appItem));
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this.taskCount > 0)
            {
                e.Cancel = true;
                MessageBox.Show("还有" + this.taskCount + "个任务正在进行中", "任务进行中", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        /// <summary>
        /// 超链接处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void vcHyperlinkColumn_Click(object sender, RoutedEventArgs e)
        {
            AppItem appItem = ((TextBlock)sender).DataContext as AppItem;
            System.Diagnostics.Process.Start(appItem.downloadUrl);
        }
    }
}
