using System;
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

        public Task<TaskResult> installAppAsync(AppItem appItem)
        {
            if (appItem.type == AppType.PHP)
            {
                return this.installPhpAsync(appItem);
            }
            else
            {
                return this.installNginxAsync(appItem);
            }
        }

        private Task<TaskResult> installPhpAsync(AppItem appItem)
        {
            return Task<TaskResult>.Run(async () =>
            {
                this.setting.Dispatcher.Invoke(() =>
                {
                    appItem.status = AppItemStatus.UNDER_INSTALL;
                });
                string zipPath = appItem.getAppZipPath();
                FileInfo zipPathInfo = new FileInfo(zipPath);
                //如果压缩包不存在,则进行在线下载
                if (!zipPathInfo.Exists)
                {
                    string zipTmpPath = appItem.getAppZipPath(true);
                    DownloadHelper downloadHelper = new DownloadHelper();
                    TaskResult result = await downloadHelper.downloadFileAsync(new Uri(appItem.downloadUrl), zipTmpPath, (string progressPercentage) =>
                     {

                         this.setting.Dispatcher.Invoke(() =>
                         {
                             appItem.progressPercentage = progressPercentage;
                         });
                     });
                    this.setting.Dispatcher.Invoke(() =>
                    {
                        appItem.progressPercentage = "";
                    });
                    if (!result.success)
                    {
                        return this.onTaskFailed(appItem, result);
                    }
                    try
                    {
                        //copy文件
                        FileInfo zipTmpPathInfo = new FileInfo(zipTmpPath);
                        zipTmpPathInfo.CopyTo(zipPath, true);
                        //删除临时文件
                        zipTmpPathInfo.Delete();
                    }
                    catch (Exception e)
                    {
                        return this.onTaskFailed(appItem, e);
                    }
                }
                string appPath = appItem.getAppPath();
                DirectoryInfo appPathInfo = new DirectoryInfo(appPath);
                try
                {
                    //创建目录
                    if (!appPathInfo.Exists)
                    {
                        appPathInfo.Create();
                    }
                    //解压
                    System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, appPath);
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
                    this.setting.Dispatcher.Invoke(() =>
                    {
                        appItem.status = AppItemStatus.INSTALLED;
                    });
                }
                catch (Exception e)
                {
                    return this.onTaskFailed(appItem, e);
                }
                return new TaskResult();
            });
        }

        private Task<TaskResult> installNginxAsync(AppItem appItem)
        {
            return Task<TaskResult>.Run(async () =>
            {
                this.setting.Dispatcher.Invoke(() =>
                {
                    appItem.status = AppItemStatus.UNDER_INSTALL;
                });
                string zipPath = appItem.getAppZipPath();
                FileInfo zipPathInfo = new FileInfo(zipPath);
                //如果压缩包不存在,则进行在线下载
                if (!zipPathInfo.Exists)
                {
                    string zipTmpPath = appItem.getAppZipPath(true);
                    DownloadHelper downloadHelper = new DownloadHelper();
                    TaskResult result = await downloadHelper.downloadFileAsync(new Uri(appItem.downloadUrl), zipTmpPath, (string progressPercentage) =>
                    {

                        this.setting.Dispatcher.Invoke(() =>
                        {
                            appItem.progressPercentage = progressPercentage;
                        });
                    });
                    this.setting.Dispatcher.Invoke(() =>
                    {
                        appItem.progressPercentage = "";
                    });
                    if (!result.success)
                    {
                        return this.onTaskFailed(appItem, result);
                    }
                    try
                    {
                        //copy文件
                        FileInfo zipTmpPathInfo = new FileInfo(zipTmpPath);
                        zipTmpPathInfo.CopyTo(zipPath, true);
                        //删除临时文件
                        zipTmpPathInfo.Delete();
                    }
                    catch (Exception e)
                    {
                        return this.onTaskFailed(appItem, e);
                    }
                }
                string appPath = appItem.getAppPath();
                DirectoryInfo appPathInfo = new DirectoryInfo(appPath);
                try
                {
                    //创建目录
                    if (!appPathInfo.Exists)
                    {
                        appPathInfo.Create();
                    }
                    //解压
                    System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, appPath);
                    //从目录中移动到当前目录
                    DirectoryInfo[] subDirs = appPathInfo.GetDirectories();
                    TaskResult result = await this.copyFiles(subDirs[0], appPathInfo);
                    if (!result.success)
                    {
                        return this.onTaskFailed(appItem, result);
                    }
                    //删除目录
                    subDirs[0].Delete(true);
                    //复制默认配置文件
                    DirectoryInfo defaultConfigPath = new DirectoryInfo(DirectoryHelper.getNginxDefaultConfigPath());
                    result = await this.copyFiles(defaultConfigPath, new DirectoryInfo(appPath + @"\conf"));
                    if (!result.success)
                    {
                        return this.onTaskFailed(appItem, result);
                    }
                    //修改默认网站路径
                    string defaultWebsitePath = DirectoryHelper.getDefaultWebsitePath();
                    string confPath = appPath + @"\conf\vhost\localhost.conf";
                    UTF8Encoding encoding = new UTF8Encoding();
                    string fileContent = File.ReadAllText(confPath, encoding);
                    fileContent = fileContent.Replace("{{path}}", defaultWebsitePath.Replace("\\", "/"));
                    File.WriteAllText(confPath, fileContent, encoding);
                    //状态变为已安装
                    this.setting.Dispatcher.Invoke(() =>
                    {
                        appItem.status = AppItemStatus.INSTALLED;
                    });
                }
                catch (Exception e)
                {
                    return this.onTaskFailed(appItem, e);
                }
                return new TaskResult();
            });
        }

        /// <summary>
        /// 复制文件
        /// </summary>
        /// <param name="srcPath">源文件夹目录</param>
        /// <param name="distPath">目标文件夹目录</param>
        /// <returns></returns>
        private Task<TaskResult> copyFiles(DirectoryInfo srcPath, DirectoryInfo distPath)
        {
            return Task<TaskResult>.Run(async () =>
            {
                try
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
                    TaskResult result;
                    foreach (DirectoryInfo tmpDir in dirs)
                    {
                        result = await this.copyFiles(tmpDir, new DirectoryInfo(distPath.FullName + @"\" + tmpDir.Name));
                        if (!result.success)
                        {
                            return result;
                        }
                    }
                }
                catch (Exception e)
                {
                    return new TaskResult(e);
                }
                return new TaskResult();
            });
        }

        /// <summary>
        /// 安装失败
        /// </summary>
        /// <param name="appItem"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private TaskResult onTaskFailed(AppItem appItem, TaskResult result)
        {
            this.setting.Dispatcher.Invoke(() =>
            {
                appItem.status = AppItemStatus.NOT_INSTALL;
            });
            return result;
        }

        /// <summary>
        /// 安装失败
        /// </summary>
        /// <param name="appItem"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private TaskResult onTaskFailed(AppItem appItem, string message)
        {
            return this.onTaskFailed(appItem, new TaskResult(message));
        }

        /// <summary>
        /// 安装失败
        /// </summary>
        /// <param name="appItem"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private TaskResult onTaskFailed(AppItem appItem, Exception e)
        {
            return this.onTaskFailed(appItem, new TaskResult(e));
        }
    }
}
