using MahApps.Metro.Controls;
using php_env.items;
using php_env.service;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            Button senderBtn = sender as Button;
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
                //卸载确认
                string msg = "你确定要卸载" + appItem.appName + "吗?";
                if (appItem.type == AppType.PHP)
                {
                    //判断目录下是否安装了composer
                    FileInfo composerInfo = new FileInfo(appItem.getAppPath() + @"\composer.bat");
                    if (composerInfo.Exists)
                    {
                        msg += "(目录下安装的composer也会一起移除)";
                    }
                }
                if (MessageBoxResult.Yes != MessageBox.Show(msg, boxTitle, MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
                {
                    return;
                }
                //
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
        private async void updateResource(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            MainWindow mainWin = this.Owner as MainWindow;
            string boxTitle = "资源更新";
            //状态等待
            btn.IsEnabled = false;
            this.updateProgressBar.IsIndeterminate = true;
            this.updateProgressBar.Visibility = Visibility.Visible;
            try
            {
                ResourceUpdate updateService = new ResourceUpdate(this);
                bool hasUpdate = await updateService.updateAsync(DirectoryHelper.getXmlResourcePath(), DirectoryHelper.getXmlResourcePath(true));
                //状态还原
                btn.IsEnabled = true;
                this.updateProgressBar.IsIndeterminate = true;
                this.updateProgressBar.Visibility = Visibility.Hidden;
                if (hasUpdate)
                {
                    //更新成功
                    if (MessageBox.Show("更新资源文件成功,重启本程序生效,确定要重启程序吗", "", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        //重启应用
                        await mainWin.closeAllApp();
                        mainWin.isWinAppRestart = true;
                        Process.Start(Process.GetCurrentProcess().MainModule.FileName);
                        Application.Current.Shutdown();
                    }
                }
                else
                {
                    //已经是最新
                    mainWin.showErrorMessage("本地资源文件已经是最新版", "资源更新", MessageBoxImage.Information);
                }
            }
            catch (Exception e1)
            {
                //状态还原
                btn.IsEnabled = true;
                this.updateProgressBar.IsIndeterminate = true;
                this.updateProgressBar.Visibility = Visibility.Hidden;
                //
                mainWin.showErrorMessage(e1.Message, boxTitle);
            }
        }

        /// <summary>
        /// 安装composer
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void installComposer(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            AppItem appItem = this.phpSelector.SelectedItem as AppItem;
            MainWindow mainWin = this.Owner as MainWindow;
            string boxTitle = "安装composer";
            if (appItem == null)
            {
                mainWin.showErrorMessage("需要先安装php", boxTitle);
            }
            else
            {
                ComposerInstall installService = new ComposerInstall(this);
                string appPath = appItem.getAppPath();
                List<string> toRemoveDirs = null;
                //等待状态
                btn.IsEnabled = false;
                this.phpSelector.IsEnabled = false;
                this.composerProgressBar.IsIndeterminate = true;
                this.composerProgressBar.Visibility = Visibility.Visible;
                try
                {
                    //从path环境变量中,获取已经安装了composer的目录
                    List<string> installedDirs = await installService.getInstalledDirsAsync();
                    if (installedDirs.Contains(appPath))
                    {
                        installedDirs.Remove(appPath);
                    }
                    if (installedDirs.Count > 0)
                    {
                        string tipMessage = "检测到以下目录已经安装了composer,继续安装composer可能无法生效,是否删除下方目录中安装的composer?\r\n";
                        if (MessageBoxResult.Yes == MessageBox.Show(tipMessage + String.Join(" , ", installedDirs), boxTitle, MessageBoxButton.YesNoCancel, MessageBoxImage.Question))
                        {
                            toRemoveDirs = installedDirs;
                        }
                    }
                    //执行安装操作
                    await installService.installAsync(appPath, toRemoveDirs);
                    //状态还原
                    btn.IsEnabled = true;
                    this.phpSelector.IsEnabled = true;
                    this.composerProgressBar.IsIndeterminate = true;
                    this.composerProgressBar.Visibility = Visibility.Hidden;
                    //composer -V
                    string composerInfo = await installService.getComposerInfoAsync(appPath);
                    mainWin.showErrorMessage("--composer版本信息如下--\r\n" + composerInfo, "composer安装成功", MessageBoxImage.Information);
                }
                catch (Exception e1)
                {
                    //状态还原
                    btn.IsEnabled = true;
                    this.phpSelector.IsEnabled = true;
                    this.composerProgressBar.IsIndeterminate = true;
                    this.composerProgressBar.Visibility = Visibility.Hidden;
                    //
                    mainWin.showErrorMessage(e1.Message, boxTitle);
                }
            }
        }

        /// <summary>
        /// 浏览目录
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void viewAction(object sender, RoutedEventArgs e)
        {
            AppItem appItem = ((Button)sender).DataContext as AppItem;
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
    }
}
