//定义是否为代码调试模式
#define APP_DEBUG 
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml;

namespace php_env
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private string basePath;
        public ObservableCollection<AppItem> phpList;
        public ObservableCollection<AppItem> nginxList;
        public ObservableCollection<AppItem> vcList;

        /// <summary>
        /// 默认开启的PHP扩展
        /// </summary>
        public List<string> phpExtensions;
        /// <summary>
        /// php默认的上传文件大小限制
        /// </summary>
        public string phpUploadMaxFilesize = "8M";

        public MainWindow()
        {
            this.Resources["phpList"] = this.phpList = new ObservableCollection<AppItem>();
            this.Resources["nginxList"] = this.nginxList = new ObservableCollection<AppItem>();
            this.vcList = new ObservableCollection<AppItem>();
            this.phpExtensions = new List<string>();
            this.Resources["phpStatus"] = new AppStatus();
            this.Resources["nginxStatus"] = new AppStatus();
            InitializeComponent();
        }

        public string getAppPath(AppType appType, string appVersion)
        {
            return this.basePath + @"app\" + Enum.GetName(typeof(AppType), appType) + @"\" + appVersion;
        }

        /// <summary>
        /// 获取默认的网站目录
        /// </summary>
        /// <returns></returns>
        public string getDefaultWebPath()
        {
            return this.basePath + @"websites\localhost\public_html";
        }

        /// <summary>
        /// 获取nginx/php配置文件路径
        /// </summary>
        /// <returns></returns>
        public string getDefaultAppConfPath(AppItem appItem)
        {
            if (appItem.type == AppType.php)
            {
                return this.getAppPath(appItem) + @"\php.ini";
            }
            else
            {
                return this.getAppPath(appItem) + @"\conf\vhost\localhost.conf";
            }
        }

        /// <summary>
        /// 获取应用所在目录
        /// </summary>
        /// <param name="appItem">应用对象</param>
        /// <returns></returns>
        public string getAppPath(AppItem appItem)
        {
            return this.getAppPath(appItem.type, appItem.version);
        }

        /// <summary>
        /// 获取应用压缩包保存路径
        /// </summary>
        /// <param name="appItem">应用对象</param>
        /// <param name="isTmpPath">是否为临时路径</param>
        /// <returns></returns>
        public string getZipPath(AppItem appItem, bool isTmpPath = true)
        {
            string path = this.basePath + @"download\" + Enum.GetName(typeof(AppType), appItem.type) + @"\" + appItem.version;
            if (isTmpPath)
            {
                path += @".zip.tmp";
            }
            else
            {
                path += @".zip";
            }
            return path;
        }

        /// <summary>
        /// 获取默认的配置文件保存目录
        /// </summary>
        /// <param name="appItem">应用对象</param>
        /// <returns></returns>
        public string getDefaultConfigPath(AppItem appItem)
        {
            return this.basePath + @"default_config\" + Enum.GetName(typeof(AppType), appItem.type);
        }

        /// <summary>
        /// 显示设置窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void showSetting(object sender, RoutedEventArgs e)
        {
            Setting settingDialog = new Setting(this);
            settingDialog.Owner = this;
            settingDialog.ShowDialog();
        }

        /// <summary>
        ///  关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AppStatus phpStatus = this.Resources["phpStatus"] as AppStatus;
            AppStatus nginxStatus = this.Resources["nginxStatus"] as AppStatus;
            if (phpStatus.isRunning || nginxStatus.isRunning)
            {
                if (MessageBoxResult.Yes == MessageBox.Show("服务器正在运行,退出时服务器也会停止,你确定要退出吗?", "退出提示", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning))
                {
                    if (phpStatus.isRunning)
                    {
                        await this.stopApp(phpStatus.appItem);
                    }
                    if (nginxStatus.isRunning)
                    {
                        await this.stopApp(nginxStatus.appItem);
                    }
                    e.Cancel = false;
                }
                else
                {
                    e.Cancel = true;
                }
                return;
            }
            if (MessageBoxResult.Yes == MessageBox.Show("你确定要退出程序吗?", "退出提示", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
            {
                e.Cancel = false;
            }
            else
            {
                e.Cancel = true;
            }
        }

        public void showErrorMessage(string err, string title = "出错了")
        {
            MessageBox.Show(err, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void loadXmlData()
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(basePath + "resource.xml");
                //
                XmlNodeList phpListXml = doc.DocumentElement["php"].GetElementsByTagName("item");
                XmlNodeList nginxListXml = doc.DocumentElement["nginx"].GetElementsByTagName("item");
                XmlNodeList vcListXml = doc.DocumentElement["vc"].GetElementsByTagName("item");
                //
                int i;
                foreach (XmlElement tmp in phpListXml)
                {
                    DirectoryInfo d = new DirectoryInfo(this.getAppPath(AppType.php, tmp.GetAttribute("version")));
                    AppItem tmp1 = new AppItem(tmp.GetAttribute("version"), tmp.GetAttribute("vc"), tmp.InnerText, AppType.php, d.Exists);
                    this.phpList.Add(tmp1);
                }
                foreach (XmlElement tmp in nginxListXml)
                {
                    DirectoryInfo d = new DirectoryInfo(this.getAppPath(AppType.nginx, tmp.GetAttribute("version")));
                    AppItem tmp1 = new AppItem(tmp.GetAttribute("version"), tmp.InnerText, AppType.nginx, d.Exists);
                    this.nginxList.Add(tmp1);
                }
                foreach (XmlElement tmp in vcListXml)
                {
                    AppItem tmp1 = new AppItem(tmp.GetAttribute("version"), tmp.InnerText, AppType.vc);
                    this.vcList.Add(tmp1);
                }
                //php相关配置初始化
                string uploadMaxFilesize = doc.DocumentElement["php"].GetAttribute("upload_max_filesize");
                if (uploadMaxFilesize != null)
                {
                    this.phpUploadMaxFilesize = uploadMaxFilesize;
                }
                XmlNodeList extensionListXml = doc.DocumentElement["php_extension"].GetElementsByTagName("item");
                foreach (XmlElement tmp in extensionListXml)
                {
                    this.phpExtensions.Add(tmp.InnerText);
                }
            }
            catch (FileNotFoundException e1)
            {
                this.showErrorMessage(e1.Message);
            }
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.basePath = AppDomain.CurrentDomain.BaseDirectory;
#if (APP_DEBUG)

            DirectoryInfo di = new DirectoryInfo(basePath);
            basePath = di.Parent.Parent.FullName + @"\";
#endif
            this.loadXmlData();
        }

        private void selector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            AppItem orgItem = null;
            AppItem destItem = null;
            if (e.AddedItems.Count > 0)
            {
                destItem = e.AddedItems[0] as AppItem;
            }
            else
            {
                return;
            }
            if (e.RemovedItems.Count > 0)
            {
                orgItem = e.RemovedItems[0] as AppItem;
            }
            if (!destItem.installed)
            {
                combo.SelectedItem = orgItem;//还原选择
                this.showErrorMessage(destItem.version + "版本尚未安装");
            }
        }

        private Task<TaskResult> runApp(AppItem appItem)
        {
            return Task<TaskResult>.Run(() =>
            {
                try
                {
                    string appPath = this.getAppPath(appItem);
                    AppStatus appStatus;
                    Process myProcess = new Process();
                    myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;//隐藏
                    myProcess.StartInfo.WorkingDirectory = appPath;//工作目录
                    if (appItem.type == AppType.php)
                    {
                        appStatus = this.Resources["phpStatus"] as AppStatus;
                        appStatus.appItem = appItem;
                        appStatus.process = myProcess;
                        myProcess.StartInfo.FileName = @"php-cgi.exe";
                        myProcess.StartInfo.Arguments = "-b 127.0.0.1:6757";
                        myProcess.Start();
                    }
                    else
                    {
                        appStatus = this.Resources["nginxStatus"] as AppStatus;
                        appStatus.appItem = appItem;
                        myProcess.StartInfo.FileName = @"nginx.exe";
                        myProcess.Start();
                    }
                    appStatus.isRunning = true;
                    appItem.isRunning = true;
                }
                catch (Exception e)
                {
                    return new TaskResult(e);
                }
                return new TaskResult();
            });
        }

        private Task<TaskResult> stopApp(AppItem appItem)
        {
            return Task<TaskResult>.Run(() =>
            {
                try
                {

                    AppStatus appStatus;
                    if (appItem.type == AppType.php)
                    {
                        appStatus = this.Resources["phpStatus"] as AppStatus;
                        appStatus.process.Kill();
                    }
                    else
                    {
                        appStatus = this.Resources["nginxStatus"] as AppStatus;
                        //nginx -s stop
                        Process myProcess = new Process();
                        myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;//隐藏
                        myProcess.StartInfo.WorkingDirectory = this.getAppPath(appItem);//工作目录
                        myProcess.StartInfo.FileName = @"nginx.exe";
                        myProcess.StartInfo.Arguments = "-s stop";
                        myProcess.Start();
                        appStatus.isRunning = false;
                        appItem.isRunning = false;
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
        /// 启动或者停止应用
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void appBtn_Click(object sender, RoutedEventArgs e)
        {
            Button appButton = sender as Button;
            ComboBox combo;
            if (appButton.Name == "phpBtn")
            {
                combo = this.phpSelector;
            }
            else
            {
                combo = this.nginxSelector;
            }
            AppItem appItem = combo.SelectedItem as AppItem;
            if (appItem == null)
            {
                this.showErrorMessage("请先选择版本");
                return;
            }
            TaskResult result;
            if (appItem.isRunning)
            {
                //停止
                result = await this.stopApp(appItem);
            }
            else
            {
                //启动
                result = await this.runApp(appItem);
            }
            if (!result.success)
            {
                this.showErrorMessage(result.message);
                return;
            }
        }
    }
}
