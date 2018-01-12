using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        private void onItemTaskFailed(AppItem appItem, string message, string title = "出错了")
        {
            MainWindow mainWin = this.Owner as MainWindow;
            appItem.resetProgress();
            mainWin.showErrorMessage(message, title);
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
            Button senderBtn = sender as Button;
            AppItem appItem = senderBtn.DataContext as AppItem;
            string appName = Enum.GetName(typeof(AppType), appItem.type) + appItem.version;
            MainWindow mainWin = this.Owner as MainWindow;
            string appPath = mainWin.getAppPath(appItem);
            DirectoryInfo appPathInfo = new DirectoryInfo(appPath);
            //
            if (appItem.installed)
            {
                //进度等待
                appItem.setPendingProgress();
                /*卸载*/
                if (appItem.isRunning)
                {
                    this.onItemTaskFailed(appItem, appName + "正在运行中无法卸载");
                    return;
                }
                string uninstallMessage = "你确定要卸载" + appName + "吗?";
                if (appItem.type == AppType.php)
                {
                    //判断composer安装情况
                    FileInfo composerInfo = new FileInfo(appPath + @"\composer.bat");
                    if (composerInfo.Exists)
                    {
                        uninstallMessage += "(目录下的composer也会一起移除)";
                    }
                }
                if (MessageBox.Show(uninstallMessage, "卸载提示", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    appItem.resetProgress();
                    return;
                }

                TaskResult result = await this.deleteDir(appPathInfo);
                if (!result.success)
                {
                    //删除目录出错
                    this.onItemTaskFailed(appItem, result, "卸载" + appName + "出错");
                    appItem.resetProgress();
                    return;
                }
                if (appItem.type == AppType.php)
                {
                    //删除Path变量
                    result = await this.removeUserPath(appPath);
                    if (!result.success)
                    {
                        //删除目录出错
                        this.onItemTaskFailed(appItem, result, "卸载" + appName + "出错");
                        appItem.resetProgress();
                        return;
                    }
                }
                appItem.installed = false;
                appItem.resetProgress();
            }
            else
            {
                appItem.setPendingProgress();
                this.processInstall(appItem);
            }
        }

        private void processInstall(AppItem appItem)
        {
            MainWindow mainWin = this.Owner as MainWindow;
            /*安装*/
            string zipPath = mainWin.getZipPath(appItem, false);
            string zipTmpPath = mainWin.getZipPath(appItem);
            FileInfo zipPathInfo = new FileInfo(zipPath);
            if (zipPathInfo.Exists)
            {
                //继续安装
                this.continueInstall(appItem, zipPath);
            }
            else
            {
                //下载压缩包
                this.downloadFileAsync(appItem.downloadUrl, zipTmpPath, (DownloadProgressChangedEventArgs e) =>
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        appItem.updateProgress(e.BytesReceived, e.TotalBytesToReceive, e.ProgressPercentage);
                    });
                }, () =>
                {
                    //下载成功
                    try
                    {
                        //copy临时文件
                        FileInfo zipTmpPathInfo = new FileInfo(zipTmpPath);
                        zipTmpPathInfo.CopyTo(zipPath, true);
                        //删除临时文件
                        zipTmpPathInfo.Delete();
                        //继续安装
                        this.continueInstall(appItem, zipPath);
                    }
                    catch (Exception e)
                    {
                        this.onItemTaskFailed(appItem, e.Message);
                    }
                }, (Exception e) =>
                {
                    this.onItemTaskFailed(appItem, e.Message);
                });
            }
        }

        private async void continueInstall(AppItem appItem, string zipPath)
        {
            MainWindow mainWin = this.Owner as MainWindow;
            string appPath = mainWin.getAppPath(appItem);
            DirectoryInfo appPathInfo = new DirectoryInfo(appPath);
            //创建目录
            if (!appPathInfo.Exists)
            {
                try
                {
                    appPathInfo.Create();
                }
                catch (Exception e)
                {
                    this.onItemTaskFailed(appItem, e.Message);
                    return;
                }
            }

            //解压
            TaskResult result = await this.extractFileAsync(zipPath, appPath);
            if (!result.success)
            {
                //解压文件出错
                this.onItemTaskFailed(appItem, result);
                return;
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
                        this.onItemTaskFailed(appItem, e1.Message);
                        return;
                    }
                }
                //初始化配置文件
                result = await this.initAppConfig(appItem);
                if (!result.success)
                {
                    this.onItemTaskFailed(appItem, result);
                    return;
                }
            }
            else if (appItem.type == AppType.nginx)
            {
                DirectoryInfo[] dirs = appPathInfo.GetDirectories();
                result = await this.copyFiles(dirs[0], appPathInfo);
                if (!result.success)
                {
                    //移动文件出错
                    this.onItemTaskFailed(appItem, result);
                    return;
                }
                result = await this.deleteDir(dirs[0]);
                if (!result.success)
                {
                    //删除临时文件夹出错
                    this.onItemTaskFailed(appItem, result);
                    return;
                }
                //复制配置文件
                result = await this.copyFiles(new DirectoryInfo(mainWin.getDefaultConfigPath(appItem)), new DirectoryInfo(appPath + @"\conf"));
                if (!result.success)
                {
                    this.onItemTaskFailed(appItem, result);
                    return;
                }
                //初始化配置文件
                result = await this.initAppConfig(appItem);
                if (!result.success)
                {
                    this.onItemTaskFailed(appItem, result);
                    return;
                }
            }
            //任务完成
            appItem.resetProgress();
            appItem.installed = true;
        }

        /// <summary>
        /// 从用户Path环境变量中删除指定目录
        /// </summary>
        /// <param name="appPath"></param>
        /// <returns></returns>
        private Task<TaskResult> removeUserPath(string appPath)
        {
            return Task<TaskResult>.Run(() =>
            {
                try
                {
                    List<string> pathList = this.parsePathString(EnvironmentVariableTarget.User);
                    if (pathList.Contains(appPath))
                    {
                        pathList.Remove(appPath);
                        //更新用户Path环境变量
                        Environment.SetEnvironmentVariable("Path", String.Join(";", pathList) + ";", EnvironmentVariableTarget.User);
                    }
                }
                catch (Exception e)
                {
                    return new TaskResult(e);
                }
                return new TaskResult();
            });
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
        /// 批量删除文件
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        public Task<TaskResult> deleteFiles(List<string> files)
        {
            return Task<TaskResult>.Run(() =>
            {

                try
                {
                    FileInfo fileInfo;
                    foreach (string filePath in files)
                    {
                        fileInfo = new FileInfo(filePath);
                        if (fileInfo.Exists)
                        {
                            fileInfo.Delete();
                        }
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
        /// <param name="url"></param>
        /// <param name="savePath"></param>
        /// <param name="onFileSizeChange"></param>
        /// <param name="downloadSuccess"></param>
        /// <param name="downloadFailed"></param>
        /// <returns></returns>
        private void downloadFileAsync(string url, string savePath, Action<DownloadProgressChangedEventArgs> onFileSizeChange, Action downloadSuccess, Action<Exception> downloadFailed)
        {
            try
            {
                //如果目录不存在则自动创建
                FileInfo savePathInfo = new FileInfo(savePath);
                DirectoryInfo saveDir = savePathInfo.Directory;
                if (!saveDir.Exists)
                {
                    saveDir.Create();
                }
                ///
                WebClient client = new WebClient();
                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler((object sender, DownloadProgressChangedEventArgs e) =>
                {
                    onFileSizeChange(e);
                });
                client.DownloadFileCompleted += new AsyncCompletedEventHandler((object sender, AsyncCompletedEventArgs e) =>
                {
                    downloadSuccess();
                });
                client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36");
                client.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
                client.DownloadFileAsync(new Uri(url), savePath);
            }
            catch (Exception e)
            {
                downloadFailed(e);
            }
        }

        private void viewAction(object sender, RoutedEventArgs e)
        {
            AppItem appItem = ((Button)sender).DataContext as AppItem;
            MainWindow mainWin = this.Owner as MainWindow;
            Process.Start(@"explorer.exe", mainWin.getAppPath(appItem));
        }

        private void MetroWindow_Closing(object sender, CancelEventArgs e)
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

        private void updateResource(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            MainWindow mainWin = this.Owner as MainWindow;
            string resourceXmlPath = mainWin.getResourceXmlPath();
            string resourceXmlTmpPath = mainWin.getResourceXmlPath(true);
            btn.IsEnabled = false;
            this.updateProgressBar.Visibility = Visibility.Visible;
            this.downloadFileAsync("https://github.com/liuguangw/php_env/raw/master/php_env/resource.xml", resourceXmlTmpPath, (DownloadProgressChangedEventArgs e1) => { },
                () =>
                {
                    //下载成功后执行
                    this.processUpdateResource(btn, resourceXmlTmpPath, resourceXmlPath);
                    //
                },
                (Exception e1) =>
                {
                    //下载配置文件出错
                    btn.IsEnabled = true;
                    this.updateProgressBar.Visibility = Visibility.Hidden;
                    mainWin.showErrorMessage(e1.Message);
                }
            );
            ////

        }

        private async void processUpdateResource(Button btn, string resourceXmlTmpPath, string resourceXmlPath)
        {
            MainWindow mainWin = this.Owner as MainWindow;
            //获取两者的md5
            string localMd5 = "";
            string tmpMd5 = "";
            //本地md5
            TaskResult result = await this.getFileMd5Async(resourceXmlPath);
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
            //下载得到的临时文件md5
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
            //md5不同则覆盖本地文件
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

        /// <summary>
        /// 解析Path环境变量
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private List<string> parsePathString(EnvironmentVariableTarget target)
        {
            List<string> pathList = new List<string>();
            string pathStr = Environment.GetEnvironmentVariable("Path", target);
            if (pathStr == null)
            {
                return pathList;
            }
            pathStr = pathStr.TrimEnd(';');
            if (pathStr.Length == 0)
            {
                return pathList;
            }
            string[] pathArray = pathStr.Split(';');
            foreach (string path in pathArray)
            {
                if (path.EndsWith("\\"))
                {
                    pathList.Add(path.TrimEnd('\\'));
                }
                else
                {
                    pathList.Add(path);
                }
            }
            return pathList;
        }

        private Task<ComposerPathTaskResult> getComposerPathAsync()
        {
            return Task.Run(() =>
            {
                List<string> resultList = new List<string>();
                try
                {
                    List<string> pathList = this.parsePathString(EnvironmentVariableTarget.Machine);
                    pathList.AddRange(this.parsePathString(EnvironmentVariableTarget.User));
                    FileInfo fileInfo;
                    foreach (string path in pathList)
                    {
                        fileInfo = new FileInfo(path + @"\composer.bat");
                        if (fileInfo.Exists)
                        {
                            resultList.Add(path);
                        }
                    }
                }
                catch (Exception e)
                {
                    return new ComposerPathTaskResult(e);
                }
                return new ComposerPathTaskResult(resultList);
            });

        }

        /// <summary>
        /// 安装
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void installComposer(object sender, RoutedEventArgs e)
        {
            AppItem appItem = this.phpSelector.SelectedItem as AppItem;
            Button btn = sender as Button;
            MainWindow mainWin = this.Owner as MainWindow;
            string boxTitle = "安装composer";
            if (appItem == null)
            {
                ObservableCollection<AppItem> installedPhpList = Application.Current.Resources["phpList"] as ObservableCollection<AppItem>;
                if (installedPhpList.Count == 0)
                {
                    mainWin.showErrorMessage("请先安装PHP再安装", boxTitle);
                    return;
                }
                else
                {
                    mainWin.showErrorMessage("请选择PHP版本", boxTitle);
                    return;
                }
            }
            //读取环境变量,获取系统已经安装了composer的路径
            btn.IsEnabled = false;
            this.composerProgressBar.Visibility = Visibility.Visible;
            ComposerPathTaskResult result = await this.getComposerPathAsync();
            if (!result.success)
            {
                btn.IsEnabled = true;
                this.composerProgressBar.Visibility = Visibility.Hidden;
                mainWin.showErrorMessage(result.message, boxTitle);
                return;
            }
            string appPath = mainWin.getAppPath(appItem);
            if (result.pathList.Contains(appPath))
            {
                //排除当前目录
                result.pathList.Remove(appPath);
            }
            string[] pathList = result.pathList.ToArray();
            if (pathList.Length > 0)
            {
                if (MessageBoxResult.Yes == MessageBox.Show("检测到以下目录已经安装了composer,继续安装composer可能无法生效,是否删除下方目录中安装的composer?\r\n" + String.Join(" , ", result.pathList.ToArray()), "操作提示", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
                {
                    //删除文件
                    List<string> toBeDeleteFiles = new List<string>();
                    foreach (string tmpPath in pathList)
                    {
                        toBeDeleteFiles.Add(tmpPath + @"\composer.bat");
                        toBeDeleteFiles.Add(tmpPath + @"\composer.phar");
                    }
                    TaskResult delResult = await this.deleteFiles(toBeDeleteFiles);
                    if (!delResult.success)
                    {
                        btn.IsEnabled = true;
                        this.composerProgressBar.Visibility = Visibility.Hidden;
                        mainWin.showErrorMessage(delResult.message, boxTitle);
                        return;
                    }
                    //删除用户Path变量
                    TaskResult delEnvResult;
                    foreach (string tmpPath in pathList)
                    {
                        delEnvResult = await this.removeUserPath(tmpPath);
                        if (!delEnvResult.success)
                        {
                            btn.IsEnabled = true;
                            this.composerProgressBar.Visibility = Visibility.Hidden;
                            mainWin.showErrorMessage(delEnvResult.message, boxTitle);
                            return;
                        }
                    }
                }
            }
            //下载composer文件
            this.downloadFileAsync(mainWin.composerUrl, appPath + @"\composer.phar", (DownloadProgressChangedEventArgs e1) =>
            {
                if (e1.TotalBytesToReceive > 0)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        this.composerProgressBar.IsIndeterminate = false;
                        this.composerProgressBar.Minimum = 0;
                        this.composerProgressBar.Maximum = e1.TotalBytesToReceive;
                        this.composerProgressBar.Value = e1.BytesReceived;
                    });
                }
            },
            () =>
            {
                //下载成功,继续安装composer
                this.continueInstallComposer(btn, appPath, boxTitle);
            }, (Exception e1) =>
            {
                //下载失败
                btn.IsEnabled = true;
                this.composerProgressBar.Visibility = Visibility.Hidden;
                mainWin.showErrorMessage(e1.Message, boxTitle);
            });
        }

        /// <summary>
        /// 继续composer安装
        /// </summary>
        /// <param name="btn"></param>
        /// <param name="appPath"></param>
        /// <param name="boxTitle"></param>
        private async void continueInstallComposer(Button btn, string appPath, string boxTitle)
        {
            MainWindow mainWin = this.Owner as MainWindow;
            this.composerProgressBar.IsIndeterminate = true;
            TaskResult taskResult = await this.initComposerAsync(appPath);
            if (!taskResult.success)
            {
                //composer初始化失败
                btn.IsEnabled = true;
                this.composerProgressBar.Visibility = Visibility.Hidden;
                mainWin.showErrorMessage(taskResult.message, boxTitle);
                return;
            }
            btn.IsEnabled = true;
            this.composerProgressBar.Visibility = Visibility.Hidden;
            MessageBox.Show("安装composer成功", boxTitle + "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private Task<TaskResult> initComposerAsync(string appPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    //生成bat文件
                    File.WriteAllText(appPath + @"\composer.bat", "@php \"%~dp0composer.phar\" %*", Encoding.Default);
                    //判断Path环境变量中是否有当前目录
                    List<string> pathList = this.parsePathString(EnvironmentVariableTarget.Machine);
                    pathList.AddRange(this.parsePathString(EnvironmentVariableTarget.User));
                    if (!pathList.Contains(appPath))
                    {
                        //设置用户变量
                        List<string> userPathList = this.parsePathString(EnvironmentVariableTarget.User);
                        userPathList.Add(appPath);
                        string userPath = String.Join(";", userPathList) + ";";
                        Environment.SetEnvironmentVariable("Path", userPath, EnvironmentVariableTarget.User);
                    }
                }
                catch (Exception e)
                {
                    return new TaskResult(e);
                }
                return new TaskResult();
            });
        }
    }
}
