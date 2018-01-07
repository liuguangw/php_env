using MahApps.Metro.Controls;
using System;
using System.IO;
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
        public Setting(MainWindow mainWin)
        {
            this.Owner = mainWin;
            this.Resources.Add("statusConverter", new InstallResultConverter());
            this.Resources.Add("btnConverter", new InstallButtonConverter());
            InitializeComponent();
            phpList.DataContext = mainWin.phpList;
            nginxList.DataContext = mainWin.nginxList;
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

        private async void phpAction(object sender, RoutedEventArgs e)
        {
            PhpItem s = ((Button)sender).DataContext as PhpItem;
            MainWindow mainWin = this.Owner as MainWindow;
            this.phpList.IsEnabled = false;
            if (s.installed)
            {
                this.showPendingStatus(this.phpStatusText, this.phpStatus, "正在卸载php" + s.version);
                string err = "";
                bool result = await Task.Run(new Func<bool>(() =>
                  {
                      DirectoryInfo d = new DirectoryInfo(mainWin.getAppPath(AppType.php, s.version));
                      try
                      {

                          if (d.Exists)
                          {
                              d.Delete(true);
                          }
                      }
                      catch (Exception e1)
                      {
                          err = e1.Message;
                          return false;
                      }
                      return true;
                  }));
                //
                this.showCommonStatus(this.phpStatusText, this.phpStatus);
                if (result)
                {
                    s.installed = false;
                }
                else
                {
                    mainWin.showErrorMessage(err);
                }
            }
            else
            {
                string zipPath = mainWin.getZipPath(AppType.php, s.version, false);
                string zipTmpPath = mainWin.getZipPath(AppType.php, s.version);
                FileInfo zipPathInfo = new FileInfo(zipPath);
                string err = "";
                bool result;
                if (!zipPathInfo.Exists)
                {
                    //下载文件
                    this.showPendingStatus(this.phpStatusText, this.phpStatus, "下载php" + s.version);
                    result = await Task.Run(new Func<bool>(() =>
                    {
                        try
                        {
                            this.downloadFile(s.downloadUrl, zipTmpPath, new Action<long, long>((long processed, long total) =>
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                    if (total != -1)
                                    {
                                        this.showProcessStatus(this.phpStatusText, this.phpStatus,"下载php" + s.version,processed, total);
                                    }
                                });
                            }));
                            //重命名
                            FileInfo zipTmpPathInfo = new FileInfo(zipTmpPath);
                            zipTmpPathInfo.CopyTo(zipTmpPath, true);
                        }
                        catch (Exception e1)
                        {
                            err = e1.Message;
                            return false;
                        }
                        return true;
                    }));
                }
                //解压
                //...
                //
                s.installed = true;
            }
            this.phpList.IsEnabled = true;
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="url">文件的URL地址</param>
        /// <param name="savePath">文件的保存路径</param>
        /// <param name="onFileSizeChange">当文件大小发生改变时执行</param>
        private void downloadFile(string url, string savePath, Action<long, long> onFileSizeChange)
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
                throw e;
            }
        }

        private void nginxAction(object sender, RoutedEventArgs e)
        {
            NginxItem s = ((Button)sender).DataContext as NginxItem;
            if (s.installed)
            {
                MessageBox.Show("执行卸载");
                s.installed = false;
            }
            else
            {
                MessageBox.Show(s.downloadUrl);
                s.installed = true;
            }
        }

        private void viewPhpPath(object sender, RoutedEventArgs e)
        {
            MainWindow mainWin = this.Owner as MainWindow;
            PhpItem s = ((Button)sender).DataContext as PhpItem;
            System.Diagnostics.Process.Start(@"explorer.exe ", mainWin.getAppPath(AppType.php, s.version));
        }

        private void viewNginxPath(object sender, RoutedEventArgs e)
        {
            MainWindow mainWin = this.Owner as MainWindow;
            NginxItem s = ((Button)sender).DataContext as NginxItem;
            System.Diagnostics.Process.Start(@"explorer.exe ", mainWin.getAppPath(AppType.nginx, s.version));
        }
    }
}
