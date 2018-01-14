using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace php_env.service
{
    public class ResourceUpdate
    {
        private Setting setting;

        public ResourceUpdate(Setting setting)
        {
            this.setting = setting;
        }

        public Task<bool> updateAsync(string xmlResourcePath, string xmlResourceTmpPath)
        {
            return Task<bool>.Run(async () =>
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
                });
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
                return needUpdate;
            });
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
