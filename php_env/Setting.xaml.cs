using MahApps.Metro.Controls;
using php_env.items;
using php_env.service;
using System;
using System.Diagnostics;
using System.Windows;

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
            this.phpList.DataContext = mainWin.xmlResource.phpList;
            this.nginxList.DataContext = mainWin.xmlResource.nginxList;
            this.vcList.DataContext = mainWin.xmlResource.vcList;
            //
            this.phpSelector.DataContext = mainWin.installedPhpList;
            if (mainWin.installedPhpList.Count > 0)
            {
                this.setComposerPhpList();
            }
        }

        /// <summary>
        /// 设置面板composer处PHP列表
        /// </summary>
        public void setComposerPhpList()
        {
            if (this.phpSelector.SelectedIndex == -1)
            {
                this.phpSelector.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// 关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.WindowState = WindowState.Minimized;
            this.Hide();
        }

        /// <summary>
        /// 处理安装或者卸载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void mainAction(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button senderBtn = sender as System.Windows.Controls.Button;
            AppItem appItem = senderBtn.DataContext as AppItem;
            if (!appItem.isInstalled)
            {
                string boxTitle = "安装" + appItem.appName;
                AppItemInstall installService = new AppItemInstall(this);
                try
                {
                    appItem.status = AppItemStatus.UNDER_INSTALL;
                    await installService.installAppAsync(appItem);
                    appItem.status = AppItemStatus.INSTALLED;
                }
                catch (Exception e1)
                {
                    appItem.status = AppItemStatus.NOT_INSTALL;
                    appItem.progressPercentage = "";
                    MainWindow mainWin = this.Owner as MainWindow;
                    mainWin.showErrorMessage(e1.Message, boxTitle);
                }
            }
            else
            {
                string boxTitle = "卸载" + appItem.appName;
                //@todo 卸载确认
                AppItemUnInstall unInstallService = new AppItemUnInstall(this);
                try
                {
                    appItem.status = AppItemStatus.UNDER_UNISTALL;
                    await unInstallService.removeAppAsync(appItem);
                    appItem.status = AppItemStatus.NOT_INSTALL;
                }
                catch (Exception e1)
                {
                    appItem.status = AppItemStatus.INSTALLED;
                    appItem.progressPercentage = "";
                    MainWindow mainWin = this.Owner as MainWindow;
                    mainWin.showErrorMessage(e1.Message, boxTitle);
                }
            }
        }

        /// <summary>
        /// 处理资源文件更新
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void updateResource(object sender, RoutedEventArgs e)
        { }

        /// <summary>
        /// 安装composer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void installComposer(object sender, RoutedEventArgs e)
        { }

        /// <summary>
        /// 浏览目录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void viewAction(object sender, RoutedEventArgs e)
        {
            AppItem appItem = ((System.Windows.Controls.Button)sender).DataContext as AppItem;
            MainWindow mainWin = this.Owner as MainWindow;
            Process.Start(@"explorer.exe", appItem.getAppPath());
        }

        /// <summary>
        /// 超链接处理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void vcHyperlinkColumn_Click(object sender, RoutedEventArgs e)
        {
            AppItem appItem = ((System.Windows.Controls.TextBlock)sender).DataContext as AppItem;
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
    }
}
