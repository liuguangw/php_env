using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace php_env.service
{
    public class ResourceUpdate
    {
        private Setting setting;

        public ResourceUpdate(Setting setting)
        {
            this.setting = setting;
        }

        public Task updateAsync(Button updateBtn,string xmlResourcePath, string xmlResourceTmpPath)
        {
            return Task.Run(async () =>
            {
                string updateUrl = @"https://github.com/liuguangw/php_env/raw/master/php_env/data/resource.xml";
                //下载
                DownloadHelper downloadHelper = new DownloadHelper();
                await downloadHelper.downloadFileAsync(new Uri(updateUrl), xmlResourceTmpPath, (long processed, long total) =>
                {
                    this.setting.Dispatcher.Invoke(() =>
                    {
                        this.setting.updateProgressBar.IsIndeterminate = false;
                        this.setting.updateProgressBar.Value = processed;
                        this.setting.updateProgressBar.Maximum = total;
                    });
                }, () => { this.continueUpdate(updateBtn,xmlResourcePath, xmlResourceTmpPath); });
            });
        }

        private async void continueUpdate(Button updateBtn, string xmlResourcePath, string xmlResourceTmpPath)
        {

            this.setting.Dispatcher.Invoke(() =>
            {
                this.setting.composerProgressBar.IsIndeterminate = true;
            });
            //获取md5
            Task<string> t1 = this.getFileMd5Async(xmlResourcePath);
            Task<string> t2 = this.getFileMd5Async(xmlResourceTmpPath);
            string[] result = await Task<string>.WhenAll(t1, t2);
            bool needUpdate = (result[0] != result[1]);
            FileInfo tmpFileInfo = new FileInfo(xmlResourceTmpPath);
            if (needUpdate)
            {
                tmpFileInfo.CopyTo(xmlResourcePath, true);
            }
            tmpFileInfo.Delete();
            this.setting.Dispatcher.Invoke(()=> {
                this.afterUpdate(needUpdate,updateBtn);
            });
        }

        private async void afterUpdate(bool hasUpdate, Button updateBtn) {
            MainWindow mainWin = this.setting.Owner as MainWindow;
            //状态还原
            updateBtn.IsEnabled = true;
            this.setting.updateProgressBar.IsIndeterminate = true;
            this.setting.updateProgressBar.Visibility = Visibility.Hidden;
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

        private Task<string> getFileMd5Async(string fileName)
        {
            return Task<string>.Run(() =>
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
                return sb.ToString();
            });
        }
    }
}
