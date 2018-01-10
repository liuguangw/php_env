//定义是否为代码调试模式
#define APP_DEBUG 
using MahApps.Metro.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        public Setting settingWin = null;

        /// <summary>
        /// 默认开启的PHP扩展
        /// </summary>
        public List<string> phpExtensions;
        /// <summary>
        /// php默认的上传文件大小限制
        /// </summary>
        public string phpUploadMaxFilesize = "8M";
        /// <summary>
        /// 标记为重启时退出操作
        /// </summary>
        public bool isWinAppRestart = false;

        public MainWindow()
        {
            this.phpList = new ObservableCollection<AppItem>();
            this.nginxList = new ObservableCollection<AppItem>();
            this.vcList = new ObservableCollection<AppItem>();
            ObservableCollection<AppItem> list0 = new ObservableCollection<AppItem>();
            ObservableCollection<AppItem> list1 = new ObservableCollection<AppItem>();
            this.Resources["phpList"] = list0;
            this.Resources["nginxList"] = list1;
            this.phpExtensions = new List<string>();
            this.Resources["phpStatus"] = new AppStatus();
            this.Resources["nginxStatus"] = new AppStatus();
            InitializeComponent();
            //处理安装/卸载导致的下拉框元素数量的变化
            this.phpList.CollectionChanged += list_CollectionChanged;
            this.nginxList.CollectionChanged += list_CollectionChanged;
            //下拉框变化时默认选中第一项
            list0.CollectionChanged += selection_CollectionChanged;
            list1.CollectionChanged += selection_CollectionChanged;
        }

        private void selection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ObservableCollection<AppItem> list = sender as ObservableCollection<AppItem>;
            if (list.Count > 0) {
                AppItem firstItem = list[0];
                ComboBox comboBox;
                if (firstItem.type == AppType.php)
                {
                    comboBox = this.phpSelector;
                }
                else {
                    comboBox = this.nginxSelector;
                }
                if (comboBox.SelectedIndex == -1) {
                    comboBox.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// 首次加载时,自动添加元素,并绑定属性修改事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void list_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IList newItems = e.NewItems;
            if (newItems.Count > 0)
            {
                AppItem appItem = newItems[0] as AppItem;
                ObservableCollection<AppItem> rList;
                if (appItem.type == AppType.php)
                {
                    rList = this.Resources["phpList"] as ObservableCollection<AppItem>;
                }
                else
                {
                    rList = this.Resources["nginxList"] as ObservableCollection<AppItem>;
                }
                foreach (AppItem tmpItem in newItems)
                {
                    tmpItem.PropertyChanged += tmpItem_PropertyChanged;
                    if (tmpItem.installed)
                    {
                        rList.Add(tmpItem);
                    }
                }
            }
        }

        /// <summary>
        /// 当有应用被安装时,下拉框资源添加元素，反之则移除元素
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tmpItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            AppItem tmpItem = sender as AppItem;
            if (e.PropertyName == "installed")
            {
                ObservableCollection<AppItem> rList;
                if (tmpItem.type == AppType.php)
                {
                    rList = this.Resources["phpList"] as ObservableCollection<AppItem>;
                }
                else
                {
                    rList = this.Resources["nginxList"] as ObservableCollection<AppItem>;
                }
                if (tmpItem.installed)
                {
                    rList.Add(tmpItem);
                }
                else
                {
                    rList.Remove(tmpItem);
                }
            }
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
            if (this.settingWin == null)
            {
                this.settingWin = new Setting(this);
                this.settingWin.Owner = this;
            }
            if (this.settingWin.Visibility != Visibility.Visible)
            {
                this.settingWin.Show();//显示
                if (this.settingWin.WindowState != WindowState.Normal)
                {
                    this.settingWin.WindowState = WindowState.Normal;
                }
            }
            //已经是显示状态则执行最小化/还原切换
            else if (this.settingWin.WindowState == WindowState.Normal)
            {
                this.settingWin.WindowState = WindowState.Minimized;
            }
            else if (this.settingWin.WindowState == WindowState.Minimized)
            {
                this.settingWin.WindowState = WindowState.Normal;
            }

        }

        public async void closeAllApp()
        {

            AppStatus phpStatus = this.Resources["phpStatus"] as AppStatus;
            AppStatus nginxStatus = this.Resources["nginxStatus"] as AppStatus;

            if (phpStatus.isRunning)
            {
                await this.stopApp(phpStatus.appItem);
            }
            if (nginxStatus.isRunning)
            {
                await this.stopApp(nginxStatus.appItem);
            }

        }

        /// <summary>
        ///  关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //重启操作时不提示
            if (this.isWinAppRestart)
            {
                return;
            }
            AppStatus phpStatus = this.Resources["phpStatus"] as AppStatus;
            AppStatus nginxStatus = this.Resources["nginxStatus"] as AppStatus;
            if (phpStatus.isRunning || nginxStatus.isRunning)
            {
                if (MessageBoxResult.Yes == MessageBox.Show("服务器正在运行,退出时服务器也会停止,你确定要退出吗?", "退出提示", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning))
                {
                    //关闭设置窗口
                    this.settingWin.Close();
                    //关闭服务器
                    this.closeAllApp();
                }
                else
                {
                    e.Cancel = true;
                }
                return;
            }
            string tip = "你确定要退出程序吗?";
            if (this.settingWin.taskCount > 0)
            {
                tip += "(退出时,后台任务也会停止!)";
            }
            if (MessageBoxResult.Yes != MessageBox.Show(tip, "退出提示", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
            {
                e.Cancel = true;
            }
        }

        public void showErrorMessage(string err, string title = "出错了")
        {
            MessageBox.Show(err, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public string getResourceXmlPath(bool isTmpPath = false)
        {
            if (isTmpPath)
            {
                return basePath + "resource.xml.tmp";
            }
            return basePath + "resource.xml";
        }

        private void loadXmlData()
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(this.getResourceXmlPath());
                //
                XmlNodeList phpListXml = doc.DocumentElement["php"].GetElementsByTagName("item");
                XmlNodeList nginxListXml = doc.DocumentElement["nginx"].GetElementsByTagName("item");
                XmlNodeList vcListXml = doc.DocumentElement["vc"].GetElementsByTagName("item");
                //
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
                        appStatus.process = myProcess;//附加进程对象,用于停止服务时调用
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
                        //进程结束时,appStatus会自动更新运行状态属性
                        //appItem.isRunning = false;
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
