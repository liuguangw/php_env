using php_env.items;
using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace php_env.service
{
    public class DownloadHelper
    {
        public Task downloadFileAsync(Uri uri, string savePath, Action<string> onPercentageChange, Action completedHandler)
        {
            return this.doDownload(uri, savePath, new DownloadProgressChangedEventHandler((object sender, DownloadProgressChangedEventArgs e) =>
             {
                 if (e.TotalBytesToReceive > 0)
                 {
                     onPercentageChange(e.ProgressPercentage.ToString());
                 }
                 else
                 {
                     onPercentageChange("");
                 }
             }), completedHandler);
        }

        public Task downloadFileAsync(Uri uri, string savePath, Action<long, long> onPercentageChange, Action completedHandler)
        {
            return this.doDownload(uri, savePath, new DownloadProgressChangedEventHandler((object sender, DownloadProgressChangedEventArgs e) =>
            {
                if (e.TotalBytesToReceive > 0)
                {
                    onPercentageChange(e.BytesReceived, e.TotalBytesToReceive);
                }
            }), completedHandler);
        }

        private Task doDownload(Uri uri, string savePath, DownloadProgressChangedEventHandler progressChangedHandler, Action completedHandler)
        {
            //如果目录不存在则自动创建
            FileInfo savePathInfo = new FileInfo(savePath);
            DirectoryInfo saveDir = savePathInfo.Directory;
            if (!saveDir.Exists)
            {
                saveDir.Create();
            }
            ///
            WebClient client = new WebClient();
            client.DownloadProgressChanged += progressChangedHandler;
            client.DownloadFileCompleted += new AsyncCompletedEventHandler((object sender, AsyncCompletedEventArgs e) => {
                if (e.Error == null)
                {
                    //下载成功后才继续执行
                    completedHandler.Invoke();
                }
            });
            client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36");
            client.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            return client.DownloadFileTaskAsync(uri, savePath);
        }
    }
}
