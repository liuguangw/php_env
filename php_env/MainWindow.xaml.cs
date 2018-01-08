//定义是否为代码调试模式
#define APP_DEBUG 
using MahApps.Metro.Controls;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
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

        public MainWindow()
        {
            InitializeComponent();
        }

        public string getAppPath(AppType appType, string appVersion)
        {
            return this.basePath + @"app\" + Enum.GetName(typeof(AppType), appType) + @"\" + appVersion;
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
        private void showSetting(object sender, System.Windows.RoutedEventArgs e)
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
        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
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
                this.phpList = new ObservableCollection<AppItem>();
                for (i = 0; i < phpListXml.Count; i++)
                {
                    XmlElement tmp = phpListXml.Item(i) as XmlElement;
                    DirectoryInfo d = new DirectoryInfo(this.getAppPath(AppType.php, tmp.GetAttribute("version")));
                    AppItem tmp1 = new AppItem(tmp.GetAttribute("version"), tmp.GetAttribute("vc"), tmp.InnerText, AppType.php, d.Exists);
                    this.phpList.Add(tmp1);
                }
                this.nginxList = new ObservableCollection<AppItem>();
                for (i = 0; i < nginxListXml.Count; i++)
                {
                    XmlElement tmp = nginxListXml.Item(i) as XmlElement;
                    DirectoryInfo d = new DirectoryInfo(this.getAppPath(AppType.nginx, tmp.GetAttribute("version")));
                    AppItem tmp1 = new AppItem(tmp.GetAttribute("version"), tmp.InnerText, AppType.nginx, d.Exists);
                    this.nginxList.Add(tmp1);
                }
                this.vcList = new ObservableCollection<AppItem>();
                for (i = 0; i < vcListXml.Count; i++)
                {
                    XmlElement tmp = vcListXml.Item(i) as XmlElement;
                    AppItem tmp1 = new AppItem(tmp.GetAttribute("version"), tmp.InnerText, AppType.vc);
                    this.vcList.Add(tmp1);
                }
            }
            catch (FileNotFoundException e1)
            {
                this.showErrorMessage(e1.Message);
            }
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.basePath = System.AppDomain.CurrentDomain.BaseDirectory;
#if (APP_DEBUG)

            DirectoryInfo di = new DirectoryInfo(basePath);
            basePath = di.Parent.Parent.FullName + @"\";
#endif
            this.loadXmlData();
        }
    }
}
