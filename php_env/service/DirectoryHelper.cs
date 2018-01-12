using php_env.items;
using System.IO;

namespace php_env.service
{
    /// <summary>
    /// 文件目录计算工具
    /// </summary>
    public static class DirectoryHelper
    {
        private static string _dataPath = null;
        private static string dataPath
        {
            get
            {
                if (_dataPath == null)
                {
                    string path = System.AppDomain.CurrentDomain.BaseDirectory;
#if (RELEASE)
#else

                    DirectoryInfo di = new DirectoryInfo(path);
                    path = di.Parent.Parent.FullName + @"\";
#endif
                    _dataPath = path + "data";
                }
                return _dataPath;
            }
        }

        public static string getAppPath(AppType appType,string appVersion)
        {
            return dataPath + @"\app\" + System.Enum.GetName(typeof(AppType), appType).ToLower() + "\\" + appVersion;
        }

        public static string getAppPath(this AppItem appItem)
        {
            return getAppPath(appItem.type, appItem.version);
        }

        /// <summary>
        /// 获取对应的压缩包保存路径
        /// </summary>
        /// <param name="appItem"></param>
        /// <param name="isTmpPath"></param>
        /// <returns></returns>
        public static string getAppZipPath(this AppItem appItem,bool isTmpPath=false)
        {
            string path= dataPath + @"\download\" + System.Enum.GetName(typeof(AppType), appItem.type).ToLower() + "\\" + appItem.version+".zip";
            if (isTmpPath) {
                path += ".tmp";
            }
            return path;
        }

        public static string getXmlResourcePath(bool isTmpPath = false) {
            string path = dataPath + @"\resource.xml";
            if (isTmpPath) {
                path += ".tmp";
            }
            return path;
        }

        public static string getNginxDefaultConfigPath() {
            return dataPath + @"\default_config\nginx";
        }

        public static string getDefaultWebsitePath()
        {
            return dataPath + @"\websites\localhost\public_html";
        }

    }
}
