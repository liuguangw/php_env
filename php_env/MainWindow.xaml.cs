using MahApps.Metro.Controls;
using php_env.items;
using php_env.service;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace php_env
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        /// <summary>
        /// 标记为重启时退出操作
        /// </summary>
        public bool isWinAppRestart = false;

        /// <summary>
        /// 设置窗体
        /// </summary>
        public Setting settingWin = null;
        public XmlResource xmlResource = null;

        public AppServerItem appServerItem;
        //已安装列表
        public ObservableCollection<AppItem> installedPhpList;
        public ObservableCollection<AppItem> installedNginxList;

        public MainWindow()
        {

            //初始化应用状态
            this.appServerItem = new AppServerItem();
            this.Resources["appServerItem"] = this.appServerItem;
            InitializeComponent();
            //初始化下拉列表资源
            this.phpSelector.DataContext = this.installedPhpList = new ObservableCollection<AppItem>();
            this.nginxSelector.DataContext = this.installedNginxList = new ObservableCollection<AppItem>();
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                this.xmlResource = new XmlResource(DirectoryHelper.getXmlResourcePath());
            }
            catch (Exception e1)
            {
                this.showErrorMessage(e1.Message, "加载xml资源文件失败");
            }
            //已安装下拉列表默认选择第一项
            this.installedPhpList.CollectionChanged += InstalledPhpList_CollectionChanged;
            this.installedNginxList.CollectionChanged += InstalledNginxList_CollectionChanged;
            foreach (AppItem tmpItem in this.xmlResource.phpList)
            {
                if (tmpItem.isInstalled)
                {
                    this.installedPhpList.Add(tmpItem);
                }
                //当安装状态变化时,自动更新已安装列表
                tmpItem.PropertyChanged += appItem_PropertyChanged;
            }
            foreach (AppItem tmpItem in this.xmlResource.nginxList)
            {
                if (tmpItem.isInstalled)
                {
                    this.installedNginxList.Add(tmpItem);
                }
                //当安装状态变化时,自动更新已安装列表
                tmpItem.PropertyChanged += appItem_PropertyChanged;
            }
        }

        /// <summary>
        /// php下拉框默认选择第一项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InstalledPhpList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (this.installedPhpList.Count > 0)
            {
                if (this.phpSelector.SelectedIndex == -1)
                {
                    this.phpSelector.SelectedIndex = 0;
                }
                if (this.settingWin != null)
                {
                    this.settingWin.setComposerPhpList();
                }
            }
        }

        /// <summary>
        /// nginx下拉框默认选择第一项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InstalledNginxList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (this.installedNginxList.Count > 0)
            {
                if (this.nginxSelector.SelectedIndex == -1)
                {
                    this.nginxSelector.SelectedIndex = 0;
                }
            }
        }

        /// <summary>
        /// 自动更新已安装列表
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void appItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            AppItem appItem = sender as AppItem;
            ObservableCollection<AppItem> installedList;
            if (appItem.type == AppType.PHP)
            {
                installedList = this.installedPhpList;
            }
            else
            {
                installedList = this.installedNginxList;
            }
            if (e.PropertyName == "isInstalled")
            {
                if (appItem.isInstalled)
                {
                    installedList.Add(appItem);
                }
                else
                {
                    installedList.Remove(appItem);
                }
            }
        }

        public Task closeAllApp()
        {
            return Task.Run(() =>
            {
                this.Dispatcher.Invoke(async () =>
                {
                    //关闭设置窗口
                    if (this.settingWin != null)
                    {
                        this.settingWin.Close();
                    }
                    //关闭服务器
                    try
                    {
                        await this.appServerItem.closeAllApp();
                    }
                    catch (Exception)
                    {
                    }
                });
            });
        }

        /// <summary>
        /// 关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //重启操作时不提示
            if (this.isWinAppRestart)
            {
                return;
            }
            if ((!this.appServerItem.canSelectPhp) || (!this.appServerItem.canSelectNginx))
            {
                if (MessageBoxResult.Yes == MessageBox.Show("服务器正在运行,退出时服务器也会停止,你确定要退出吗?", "退出提示", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning))
                {
                    await this.closeAllApp();
                }
                else
                {
                    e.Cancel = true;
                }
                return;
            }
            if (MessageBoxResult.Yes != MessageBox.Show("你确定要退出程序吗?(退出时,后台任务也会停止!)", "退出提示", MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// 启动或者停止服务器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void appBtn_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            AppType appType;
            if (btn.Name == "phpBtn")
            {
                appType = AppType.PHP;
                this.appServerItem.phpItem = this.phpSelector.SelectedItem as AppItem;
            }
            else
            {
                appType = AppType.NGINX;
                this.appServerItem.nginxItem = this.nginxSelector.SelectedItem as AppItem;
            }
            try
            {
                await this.appServerItem.onCommand(appType);
            }
            catch (Exception e1)
            {
                this.showErrorMessage(e1.Message);
            }
        }

        /// <summary>
        /// 显示错误消息
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        /// <param name="image"></param>
        public void showErrorMessage(string message, string title = "出错了", MessageBoxImage image = MessageBoxImage.Error)
        {
            this.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, image);
            });
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

            }
            if (this.settingWin.Visibility != Visibility.Visible)
            {
                this.settingWin.Show();//显示
                if (this.settingWin.WindowState != WindowState.Normal)
                {
                    this.settingWin.WindowState = WindowState.Normal;
                }
            }
            //已经是显示状态则执行最小化再隐藏
            else if (this.settingWin.WindowState == WindowState.Normal)
            {
                this.settingWin.WindowState = WindowState.Minimized;
                this.settingWin.Hide();
            }
        }


    }
}
