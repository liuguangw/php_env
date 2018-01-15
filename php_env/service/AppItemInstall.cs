﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using php_env.items;

namespace php_env.service
{
    public class AppItemInstall
    {
        private Setting setting;

        public AppItemInstall(Setting setting)
        {
            this.setting = setting;
        }

        public Task installAppAsync(AppItem appItem)
        {
            return Task.Run(async () =>
            {
                string zipPath = appItem.getAppZipPath();
                FileInfo zipPathInfo = new FileInfo(zipPath);
                //如果压缩包不存在,则进行在线下载
                if (!zipPathInfo.Exists)
                {
                    string zipTmpPath = appItem.getAppZipPath(true);
                    await this.downloadAppAsync(appItem, zipPath, zipTmpPath, continueInstall);
                }
                else {
                    await this.continueInstall(appItem);
                }
            });
        }

        private Task continueInstall(AppItem appItem)
        {

            return Task.Run(async () =>
            {
                //创建目录
                string appPath = appItem.getAppPath();
                DirectoryInfo appPathInfo = new DirectoryInfo(appPath);
                if (!appPathInfo.Exists)
                {
                    appPathInfo.Create();
                }
                //解压
                string zipPath = appItem.getAppZipPath();
                System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, appPath);
                //执行后续步骤
                if (appItem.type == AppType.PHP)
                {
                    await this.installPhpAsync(appItem);
                }
                else if (appItem.type == AppType.NGINX)
                {
                    await this.installNginxAsync(appItem);
                }
            });
        }

        private Task downloadAppAsync(AppItem appItem, string zipPath, string zipTmpPath, Func<AppItem,Task> onDownloadSuccess)
        {
            return Task.Run(async () =>
            {
                DownloadHelper downloadHelper = new DownloadHelper();
                await downloadHelper.downloadFileAsync(new Uri(appItem.downloadUrl), zipTmpPath, (string progressPercentage) =>
                {
                    this.setting.Dispatcher.Invoke(() =>
                    {
                        appItem.progressPercentage = progressPercentage;
                    });
                }, async () =>
                {
                    this.setting.Dispatcher.Invoke(() =>
                    {
                        appItem.progressPercentage = "";
                    });
                    //copy文件
                    FileInfo zipTmpPathInfo = new FileInfo(zipTmpPath);
                    zipTmpPathInfo.CopyTo(zipPath, true);
                    //删除临时文件
                    zipTmpPathInfo.Delete();
                    //继续执行
                    await onDownloadSuccess.Invoke(appItem);
                });
            });
        }

        private Task installPhpAsync(AppItem appItem)
        {
            return Task.Run(() =>
            {
                string appPath = appItem.getAppPath();
                DirectoryInfo appPathInfo = new DirectoryInfo(appPath);
                //复制php.ini
                FileInfo phpIniFile = new FileInfo(appPath + @"\php.ini-development");
                string configPath = appPath + @"\php.ini";
                if (phpIniFile.Exists)
                {
                    phpIniFile.CopyTo(configPath, true);
                }
                //处理php.ini
                UTF8Encoding encoding = new UTF8Encoding();
                string fileContent = File.ReadAllText(configPath, encoding);
                XmlResource xmlResource = null;
                this.setting.Dispatcher.Invoke(() =>
                {
                    MainWindow mainWin = this.setting.Owner as MainWindow;
                    xmlResource = mainWin.xmlResource;
                });
                //基本配置替换
                fileContent = fileContent.Replace(";cgi.fix_pathinfo=1", "cgi.fix_pathinfo=0")
                .Replace("; extension_dir = \"ext\"", "extension_dir = \"ext\"")
                .Replace("upload_max_filesize = 2M", "upload_max_filesize = " + xmlResource.phpUploadMaxFilesize);
                //扩展
                foreach (string extName in xmlResource.phpExtensions)
                {
                    fileContent = fileContent.Replace(";extension=php_" + extName, "extension=php_" + extName);//老版本
                    fileContent = fileContent.Replace(";extension=" + extName, "extension=" + extName);//新版本
                }
                File.WriteAllText(configPath, fileContent, encoding);
            });
        }

        private Task installNginxAsync(AppItem appItem)
        {
            return Task.Run(async () =>
            {
                string appPath = appItem.getAppPath();
                DirectoryInfo appPathInfo = new DirectoryInfo(appPath);
                //从目录中移动到当前目录
                DirectoryInfo[] subDirs = appPathInfo.GetDirectories();
                await this.copyFiles(subDirs[0], appPathInfo);
                //删除目录
                subDirs[0].Delete(true);
                //复制默认配置文件
                DirectoryInfo defaultConfigPath = new DirectoryInfo(DirectoryHelper.getNginxDefaultConfigPath());
                await this.copyFiles(defaultConfigPath, new DirectoryInfo(appPath + @"\conf"));
                //修改默认网站路径
                string defaultWebsitePath = DirectoryHelper.getDefaultWebsitePath();
                string configPath = appPath + @"\conf\vhost\localhost.conf";
                UTF8Encoding encoding = new UTF8Encoding();
                string fileContent = File.ReadAllText(configPath, encoding);
                fileContent = fileContent.Replace("{{path}}", defaultWebsitePath.Replace("\\", "/"));
                File.WriteAllText(configPath, fileContent, encoding);
            });
        }

        /// <summary>
        /// 复制文件
        /// </summary>
        /// <param name="srcPath">源文件夹目录</param>
        /// <param name="distPath">目标文件夹目录</param>
        /// <returns></returns>
        private Task copyFiles(DirectoryInfo srcPath, DirectoryInfo distPath)
        {
            return Task.Run(async () =>
            {
                if (!distPath.Exists)
                {
                    distPath.Create();
                }
                //复制文件
                FileInfo[] files = srcPath.GetFiles();
                foreach (FileInfo tmpFile in files)
                {
                    tmpFile.CopyTo(distPath.FullName + @"\" + tmpFile.Name, true);
                }
                //复制目录
                DirectoryInfo[] dirs = srcPath.GetDirectories();
                foreach (DirectoryInfo tmpDir in dirs)
                {
                    await this.copyFiles(tmpDir, new DirectoryInfo(distPath.FullName + @"\" + tmpDir.Name));
                }
            });
        }
    }
}
