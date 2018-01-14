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

        public Task removeAppAsync(AppItem appItem)
        {
            return Task.Run(() =>
            {
                string appPath = appItem.getAppPath();
                //删除文件夹
                DirectoryInfo appPathInfo = new DirectoryInfo(appPath);
                if (appPathInfo.Exists)
                {
                    appPathInfo.Delete(true);
                }
                if (appItem.type == AppType.PHP)
                {
                    //用户Path环境变量中删除
                    this.removeUserPath(appPath);
                }
            });
        }

        /// <summary>
        /// 用户Path环境变量中删除
        /// </summary>
        /// <param name="appPath"></param>
        private void removeUserPath(string appPath)
        {
            System.Collections.Generic.List<string> userPathList = PathEnvironment.getPathList(EnvironmentVariableTarget.User);
            if (userPathList.Contains(appPath))
            {
                userPathList.Remove(appPath);
                PathEnvironment.setPathList(userPathList, EnvironmentVariableTarget.User);
            }
        }
    }
}
