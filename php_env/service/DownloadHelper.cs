using php_env.items;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace php_env.service
{
    public class DownloadHelper
    {
        public Task downloadFileAsync(Uri uri, string savePath, Action<string> onPercentageChange)
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
             }));
        }

        public Task downloadFileAsync(Uri uri, string savePath, Action<long, long> onPercentageChange)
        {
            return this.doDownload(uri, savePath, new DownloadProgressChangedEventHandler((object sender, DownloadProgressChangedEventArgs e) =>
            {
                if (e.TotalBytesToReceive > 0)
                {
                    onPercentageChange(e.BytesReceived, e.TotalBytesToReceive);
                }
            }));
        }

        private Task doDownload(Uri uri, string savePath, DownloadProgressChangedEventHandler handler)
        {
            return Task.Run(async () =>
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
                client.DownloadProgressChanged += handler;
                client.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.132 Safari/537.36");
                client.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
                await client.DownloadFileTaskAsync(uri, savePath);
            });
        }
    }
}
