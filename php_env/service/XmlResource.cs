using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml;
using php_env.items;

namespace php_env.service
{
    public class XmlResource
    {
        public ObservableCollection<AppItem> phpList;
        public ObservableCollection<AppItem> nginxList;
        public ObservableCollection<AppItem> vcList;

        /// <summary>
        /// 默认开启的PHP扩展
        /// </summary>
        public List<string> phpExtensions;

        /// <summary>
        /// php默认的上传文件大小限制
        /// </summary>
        public string phpUploadMaxFilesize = "8M";

        /// <summary>
        /// composer下载地址
        /// </summary>
        public string composerUrl = "";

        /// <summary>
        /// packagist镜像地址
        /// </summary>
        public string composerMirror = "";

        /// <summary>
        /// xml文件路径
        /// </summary>
        /// <param name="xmlPath"></param>
        public XmlResource(string xmlPath)
        {
            //初始化数据
            this.phpList = new ObservableCollection<AppItem>();
            this.nginxList = new ObservableCollection<AppItem>();
            this.vcList = new ObservableCollection<AppItem>();
            this.phpExtensions = new List<string>();
            //
            XmlDocument doc = new XmlDocument();
            doc.Load(xmlPath);
            XmlNodeList phpListXml = doc.DocumentElement["php"].GetElementsByTagName("item");
            XmlNodeList nginxListXml = doc.DocumentElement["nginx"].GetElementsByTagName("item");
            XmlNodeList vcListXml = doc.DocumentElement["vc"].GetElementsByTagName("item");
            //
            foreach (XmlElement tmp in phpListXml)
            {
                DirectoryInfo d = new DirectoryInfo(DirectoryHelper.getAppPath(AppType.PHP, tmp.GetAttribute("version")));
                AppItem tmp1 = new AppItem(tmp.GetAttribute("version"), tmp.GetAttribute("vc"), tmp.InnerText, AppType.PHP, d.Exists ? AppItemStatus.INSTALLED : AppItemStatus.NOT_INSTALL);
                this.phpList.Add(tmp1);
            }
            foreach (XmlElement tmp in nginxListXml)
            {
                DirectoryInfo d = new DirectoryInfo(DirectoryHelper.getAppPath(AppType.NGINX, tmp.GetAttribute("version")));
                AppItem tmp1 = new AppItem(tmp.GetAttribute("version"), tmp.InnerText, AppType.NGINX, d.Exists ? AppItemStatus.INSTALLED : AppItemStatus.NOT_INSTALL);
                this.nginxList.Add(tmp1);
            }
            foreach (XmlElement tmp in vcListXml)
            {
                AppItem tmp1 = new AppItem(tmp.GetAttribute("version"), tmp.InnerText, AppType.VC_LIB);
                this.vcList.Add(tmp1);
            }
            //php相关配置初始化
            string uploadMaxFilesize = doc.DocumentElement["php"].GetAttribute("upload_max_filesize");
            if (uploadMaxFilesize != null)
            {
                this.phpUploadMaxFilesize = uploadMaxFilesize;
            }
            XmlNodeList extensionListXml = doc.DocumentElement["php_extension"].GetElementsByTagName("item");
            foreach (XmlElement tmp in extensionListXml)
            {
                this.phpExtensions.Add(tmp.InnerText);
            }
            //composer配置初始化
            XmlElement composer = doc.DocumentElement["composer"];
            this.composerUrl = composer.InnerText;
            string mirrorUrl = composer.GetAttribute("mirror");
            if (mirrorUrl != null)
            {
                this.composerMirror = mirrorUrl;
            }
        }
    }
}
