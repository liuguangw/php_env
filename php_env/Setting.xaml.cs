using MahApps.Metro.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
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
        public Setting(MainWindow mainWin)
        {
            this.Owner = mainWin;
            InitializeComponent();
            this.phpList.DataContext = mainWin.phpList;
            this.nginxList.DataContext = mainWin.nginxList;
            this.vcList.DataContext = mainWin.vcList;
        }

        /// <summary>
        /// 任务失败时执行
        /// </summary>
        /// <param name="message">错误消息</param>
        private void onItemTaskFailed(Button senderBtn, string message, string title = "出错了")
        {
            MainWindow mainWin = this.Owner as MainWindow;
            AppItem appItem = senderBtn.DataContext as AppItem;
            appItem.resetProgress();
            mainWin.showErrorMessage(message, title);
        }

        private void onItemTaskFailed(Button senderBtn, TaskResult result, string title = "出错了")
        {
            this.onItemTaskFailed(senderBtn, result.message, title);
        }

        private async Task<TaskResult> processInstallAsync(Button senderBtn)
        {
            AppItem appItem = senderBtn.DataContext as AppItem;
            MainWindow mainWin = this.Owner as MainWindow;
            string appPath = mainWin.getAppPath(appItem);
            DirectoryInfo appPathInfo = new DirectoryInfo(appPath);
            TaskResult result;
            /*安装*/
            string zipPath = mainWin.getZipPath(appItem, false);
            string zipTmpPath = mainWin.getZipPath(appItem);
            FileInfo zipPathInfo = new FileInfo(zipPath);
            appItem.setPendingProgress();
            if (!zipPathInfo.Exists)
            {
                //下载文件
                result = await this.downloadFileAsync(appItem.downloadUrl, zipTmpPath, (long processed, long total) =>
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        if (total != -1)
                        {
                            appItem.updateProgress(processed, total);
                        }
                    });
                });
                if (!result.success)
                {
                    return result;
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
                    return new TaskResult(e1);
                }
            }
            //创建目录
            if (!appPathInfo.Exists)
            {
                try
                {
                    appPathInfo.Create();
                }
                catch (Exception e1)
                {
                    return new TaskResult(e1);
                }
            }
            //解压
            result = await this.extractFileAsync(zipPath, appPath);
            if (!result.success)
            {
                //解压文件出错
                return result;
            }
            //处理文件
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
                        return new TaskResult(e1);
                    }
                }
                //初始化配置文件
                result = await this.initAppConfig(appItem);
                if (!result.success)
                {
                    return result;
                }
            }
            else if (appItem.type == AppType.nginx)
            {
                DirectoryInfo[] dirs = appPathInfo.GetDirectories();
                result = await this.copyFiles(dirs[0], appPathInfo);
                if (!result.success)
                {
                    //移动文件出错
                    return result;
                }
                result = await this.deleteDir(dirs[0]);
                if (!result.success)
                {
                    //删除临时文件夹出错
                    return result;
                }
                //复制配置文件
                result = await this.copyFiles(new DirectoryInfo(mainWin.getDefaultConfigPath(appItem)), new DirectoryInfo(appPath + @"\conf"));
                if (!result.success)
                {
                    return result;
                }
                //初始化配置文件
                result = await this.initAppConfig(appItem);
                if (!result.success)
                {
                    return result;
                }
            }
            //任务完成
            appItem.resetProgress();
            appItem.installed = true;
            return new TaskResult();
        }

        /// <summary>
        /// 处理安装或者卸载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void mainAction(object sender, RoutedEventArgs e)
        {
            Button senderBtn = sender as Button;
            AppItem appItem = senderBtn.DataContext as AppItem;
            string appName = Enum.GetName(typeof(AppType), appItem.type) + appItem.version;
            MainWindow mainWin = this.Owner as MainWindow;
            string appPath = mainWin.getAppPath(appItem);
            DirectoryInfo appPathInfo = new DirectoryInfo(appPath);
            //
            TaskResult result;
            if (appItem.installed)
            {
                /*卸载*/
                if (appItem.isRunning)
                {
                    this.onItemTaskFailed(senderBtn, appName + "正在运行中无法卸载");
                    return;
                }
                //进度等待
                appItem.setPendingProgress();
                result = await this.deleteDir(appPathInfo);
                appItem.resetProgress();
                if (!result.success)
                {
                    this.onItemTaskFailed(senderBtn, result, "卸载" + appName + "出错");
                }
                else
                {
                    appItem.installed = false;
                }
            }
            else
            {
                //进度等待
                appItem.setPendingProgress();
                result = await this.processInstallAsync(senderBtn);
                appItem.resetProgress();
                if (!result.success)
                {
                    this.onItemTaskFailed(senderBtn, result, "安装" + appName + "出错");
                }
                else
                {
                    appItem.installed = true;
                }
            }
        }

        /// <summary>
        /// 初始化配置文件
        /// </summary>
        /// <param name="appItem">应用对象</param>
        /// <returns></returns>
        private Task<TaskResult> initAppConfig(AppItem appItem)
        {
            MainWindow mainWin = this.Owner as MainWindow;
            string configPath = mainWin.getDefaultAppConfPath(appItem);
            return Task<TaskResult>.Run(() =>
            {

                try
                {

                    UTF8Encoding encoding = new UTF8Encoding();
                    string fileContent = File.ReadAllText(configPath, encoding);
                    if (appItem.type == AppType.php)
                    {
                        fileContent = fileContent.Replace(";cgi.fix_pathinfo=1", "cgi.fix_pathinfo=0")
                        .Replace("; extension_dir = \"ext\"", "extension_dir = \"ext\"")
                        .Replace("upload_max_filesize = 2M", "upload_max_filesize = " + mainWin.phpUploadMaxFilesize);
                        foreach (string extName in mainWin.phpExtensions)
                        {
                            fileContent = fileContent.Replace(";extension=php_" + extName, "extension=php_" + extName);//老版本
                            fileContent = fileContent.Replace(";extension=" + extName, "extension=" + extName);//新版本
                        }
                    }
                    else if (appItem.type == AppType.nginx)
                    {
                        //{{path}}替换为web目录
                        string webPath = mainWin.getDefaultWebPath();
                        fileContent = fileContent.Replace("{{path}}", webPath.Replace("\\", "/"));
                    }
                    File.WriteAllText(configPath, fileContent, encoding);
                }
                catch (Exception e1)
                {
                    return new TaskResult(e1);
                }
                return new TaskResult();
            });
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

        private Task<TaskResult> getFileMd5Async(string fileName)
        {
            return Task<TaskResult>.Run(() =>
            {
                try
                {
                    byte[] retVal;
                    System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                    using (FileStream file = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                    {
                        retVal = md5.ComputeHash(file);
                    }

                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < retVal.Length; i++)
                    {
                        sb.Append(retVal[i].ToString("x2"));
                    }
                    return new TaskResult(true, sb.ToString());
                }
                catch (Exception e)
                {
                    return new TaskResult(e);
                }
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
                    //添加浏览器头信息
                    request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36";
                    request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8";
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
            Process.Start(@"explorer.exe", mainWin.getAppPath(appItem));
        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.WindowState = WindowState.Minimized;
            this.Hide();
        }

        /// <summary>
        /// 超链接处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void vcHyperlinkColumn_Click(object sender, RoutedEventArgs e)
        {
            AppItem appItem = ((TextBlock)sender).DataContext as AppItem;
            Process.Start(appItem.downloadUrl);
        }

        /// <summary>
        /// 项目主页跳转
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(e.Uri.AbsoluteUri);
        }

        private async void updateResource(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            MainWindow mainWin = this.Owner as MainWindow;
            string resourceXmlPath = mainWin.getResourceXmlPath();
            string resourceXmlTmpPath = mainWin.getResourceXmlPath(true);
            btn.IsEnabled = false;
            this.updateProgressBar.Visibility = Visibility.Visible;
            TaskResult result = await this.downloadFileAsync("https://github.com/liuguangw/php_env/raw/master/php_env/resource.xml", resourceXmlTmpPath, (long i1, long i2) => { });
            if (!result.success)
            {
                //下载配置文件出错
                btn.IsEnabled = true;
                this.updateProgressBar.Visibility = Visibility.Hidden;
                mainWin.showErrorMessage(result.message);
                return;
            }
            //获取两者的md5
            string localMd5 = "";
            string tmpMd5 = "";
            result = await this.getFileMd5Async(resourceXmlPath);
            if (!result.success)
            {
                btn.IsEnabled = true;
                this.updateProgressBar.Visibility = Visibility.Hidden;
                mainWin.showErrorMessage(result.message, "计算本地资源md5值出错");
                return;
            }
            else
            {
                localMd5 = result.message;
            }
            result = await this.getFileMd5Async(resourceXmlTmpPath);
            if (!result.success)
            {
                btn.IsEnabled = true;
                this.updateProgressBar.Visibility = Visibility.Hidden;
                mainWin.showErrorMessage(result.message, "计算临时资源md5值出错");
                return;
            }
            else
            {
                tmpMd5 = result.message;
            }
            //判断是否要覆盖
            FileInfo tmpFileInfo = new FileInfo(resourceXmlTmpPath);
            if (localMd5 != tmpMd5)
            {
                //copy
                try
                {
                    tmpFileInfo.CopyTo(resourceXmlPath, true);
                }
                catch (Exception e1)
                {
                    btn.IsEnabled = true;
                    this.updateProgressBar.Visibility = Visibility.Hidden;
                    mainWin.showErrorMessage(e1.Message, "覆盖本地资源文件出错");
                    return;
                }
            }
            //删除临时文件
            try
            {
                tmpFileInfo.Delete();
            }
            catch (Exception e1)
            {
                btn.IsEnabled = true;
                this.updateProgressBar.Visibility = Visibility.Hidden;
                mainWin.showErrorMessage(e1.Message, "删除临时资源文件出错");
                return;
            }
            //
            btn.IsEnabled = true;
            this.updateProgressBar.Visibility = Visibility.Hidden;
            if (localMd5 != tmpMd5)
            {
                if (MessageBox.Show("更新资源文件成功,重启本程序生效,确定要重启程序吗", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    //重启应用
                    mainWin.closeAllApp();
                    mainWin.isWinAppRestart = true;
                    Process.Start(Process.GetCurrentProcess().MainModule.FileName);
                    Application.Current.Shutdown();
                }
            }
            else
            {
                MessageBox.Show("资源文件已经是最新版", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
