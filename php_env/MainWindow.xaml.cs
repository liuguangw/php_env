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
        public string basePath;
        public ObservableCollection<PhpItem> phpList;
        public ObservableCollection<NginxItem> nginxList;
        public ObservableCollection<VcItem> vcList;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 获取应用所在目录
        /// </summary>
        /// <param name="appType">应用类型</param>
        /// <param name="appVersion">应用版本</param>
        /// <returns></returns>
        public string getAppPath(AppType appType, string appVersion)
        {
            return this.basePath + @"app\" + Enum.GetName(typeof(AppType), appType) + @"\" + appVersion;
        }

        public string getZipPath(AppType appType, string appVersion,bool isTmpPath=true)
        {
            string path=this.basePath + @"download\" + Enum.GetName(typeof(AppType), appType) + @"\" + appVersion;
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

        public void showErrorMessage(string err)
        {
            MessageBox.Show(err, "出错了", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void loadXmlData()
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(basePath + "resource.xml");
                XmlElement phpListXmlItem = doc.GetElementsByTagName("php_list").Item(0) as XmlElement;
                XmlElement nginxListXmlItem = doc.GetElementsByTagName("nginx_list").Item(0) as XmlElement;
                XmlElement vcListXmlItem = doc.GetElementsByTagName("vc_list").Item(0) as XmlElement;
                //
                XmlNodeList phpListXml = phpListXmlItem.GetElementsByTagName("php");
                XmlNodeList nginxListXml = nginxListXmlItem.GetElementsByTagName("nginx");
                XmlNodeList vcListXml = vcListXmlItem.GetElementsByTagName("vc");
                //
                int i;
                this.phpList = new ObservableCollection<PhpItem>();
                for (i = 0; i < phpListXml.Count; i++)
                {
                    XmlElement tmp = phpListXml.Item(i) as XmlElement;
                    DirectoryInfo d = new DirectoryInfo(this.getAppPath(AppType.php, tmp.GetAttribute("version")));
                    PhpItem tmp1 = new PhpItem(tmp.GetAttribute("version"), tmp.GetAttribute("vc"), tmp.InnerText, d.Exists);
                    this.phpList.Add(tmp1);
                }
                this.nginxList = new ObservableCollection<NginxItem>();
                for (i = 0; i < nginxListXml.Count; i++)
                {
                    XmlElement tmp = nginxListXml.Item(i) as XmlElement;
                    DirectoryInfo d = new DirectoryInfo(this.getAppPath(AppType.nginx, tmp.GetAttribute("version")));
                    NginxItem tmp1 = new NginxItem(tmp.GetAttribute("version"), tmp.InnerText, d.Exists);
                    this.nginxList.Add(tmp1);
                }
                this.vcList = new ObservableCollection<VcItem>();
                for (i = 0; i < vcListXml.Count; i++)
                {
                    XmlElement tmp = vcListXml.Item(i) as XmlElement;
                    VcItem tmp1 = new VcItem(tmp.GetAttribute("version"), tmp.InnerText);
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
