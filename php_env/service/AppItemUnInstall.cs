using System;
using System.IO;
using System.Threading.Tasks;
using php_env.items;

namespace php_env.service
{
    public class AppItemUnInstall
    {
        private Setting setting;

        public AppItemUnInstall(Setting setting)
        {
            this.setting = setting;
        }

        public Task<TaskResult> removeAppAsync(AppItem appItem)
        {
            if (appItem.type == AppType.PHP)
            {
                return this.removePhpAsync(appItem);
            }
            else
            {
                return this.removeNginxAsync(appItem);
            }
        }

        private Task<TaskResult> removePhpAsync(AppItem appItem)
        {
            return Task<TaskResult>.Run(() =>
            {
                this.setting.Dispatcher.Invoke(() =>
                {
                    appItem.status = AppItemStatus.UNDER_UNISTALL;
                });
                string appPath = appItem.getAppPath();
                try
                {
                    //删除文件夹
                    DirectoryInfo appPathInfo = new DirectoryInfo(appPath);
                    if (appPathInfo.Exists)
                    {
                        appPathInfo.Delete(true);
                    }
                    //从用户Path变量中删除
                    System.Collections.Generic.List<string> userPathList = PathEnvironment.getPathList(EnvironmentVariableTarget.User);
                    if (userPathList.Contains(appPath)) {
                        userPathList.Remove(appPath);
                        PathEnvironment.setPathList(userPathList, EnvironmentVariableTarget.User);
                    }
                    this.setting.Dispatcher.Invoke(() =>
                    {
                        appItem.status = AppItemStatus.NOT_INSTALL;
                    });
                }
                catch (Exception e)
                {
                    return this.onTaskFailed(appItem, e);
                }
                return new TaskResult();
            });
        }

        private Task<TaskResult> removeNginxAsync(AppItem appItem)
        {
            return Task<TaskResult>.Run(() =>
            {
                this.setting.Dispatcher.Invoke(() =>
                {
                    appItem.status = AppItemStatus.UNDER_UNISTALL;
                });
                string appPath = appItem.getAppPath();
                try
                {
                    //删除文件夹
                    DirectoryInfo appPathInfo = new DirectoryInfo(appPath);
                    if (appPathInfo.Exists)
                    {
                        appPathInfo.Delete(true);
                    }
                    this.setting.Dispatcher.Invoke(() =>
                    {
                        appItem.status = AppItemStatus.NOT_INSTALL;
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
        /// 卸载失败
        /// </summary>
        /// <param name="appItem"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private TaskResult onTaskFailed(AppItem appItem, TaskResult result)
        {
            this.setting.Dispatcher.Invoke(() =>
            {
                appItem.status = AppItemStatus.INSTALLED;
            });
            return result;
        }

        /// <summary>
        /// 卸载失败
        /// </summary>
        /// <param name="appItem"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private TaskResult onTaskFailed(AppItem appItem, string message)
        {
            return this.onTaskFailed(appItem, new TaskResult(message));
        }

        /// <summary>
        /// 卸载失败
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
