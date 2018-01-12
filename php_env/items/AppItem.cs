using System.Collections.Generic;
using System.ComponentModel;

namespace php_env.items
{
    /// <summary>
    /// 应用对象
    /// </summary>
    public class AppItem : INotifyPropertyChanged
    {
        public string version { get; }
        public string vcVersion { get; }
        public string downloadUrl { get; }
        public AppType type { get; }

        private AppItemStatus _status;
        public AppItemStatus status
        {
            set
            {
                if (this._status != value)
                {
                    this._status = value;
                    this.Changed("statusText");
                    //计算安装状态
                    List<AppItemStatus> notInstallStatus = new List<AppItemStatus>();
                    notInstallStatus.Add(AppItemStatus.NOT_INSTALL);
                    notInstallStatus.Add(AppItemStatus.UNDER_INSTALL);
                    this.isInstalled = !notInstallStatus.Contains(value);
                    //计算运行状态
                    this.isRunning = (value == AppItemStatus.UNDER_RUNNING);
                    //判断安装/卸载按钮是否应该激活
                    List<AppItemStatus> pendingStatus = new List<AppItemStatus>();
                    pendingStatus.Add(AppItemStatus.UNDER_INSTALL);
                    pendingStatus.Add(AppItemStatus.UNDER_UNISTALL);
                    pendingStatus.Add(AppItemStatus.UNDER_RUNNING);
                    this.canModify = !pendingStatus.Contains(value);
                }
            }
        }

        private bool _isInstalled = false;
        public bool isInstalled
        {
            get => this._isInstalled;
            private set
            {
                if (this._isInstalled != value)
                {
                    this._isInstalled = value;
                    this.Changed("isInstalled");
                    this.Changed("commandName");
                }
            }
        }

        private bool _isRunning = false;
        /// <summary>
        /// 是否正在运行中
        /// </summary>
        public bool isRunning
        {
            get => this._isRunning;
            private set
            {
                if (this._isRunning != value)
                {
                    this._isRunning = value;
                    this.Changed("isRunning");
                }
            }
        }

        public bool _canModify = true;
        /// <summary>
        /// 标识能否执行修改操作:安装或者卸载
        /// </summary>
        public bool canModify
        {
            get
            {
                return this._canModify;
            }
            private set
            {
                if (this._canModify != value)
                {
                    this._canModify = value;
                    this.Changed("canModify");
                }
            }
        }

        private string _progressPercentage = "";
        /// <summary>
        /// 进度状态
        /// </summary>
        public string progressPercentage
        {
            get
            {
                return this._progressPercentage;
            }
            set
            {
                if (this._progressPercentage != value)
                {
                    this._progressPercentage = value;
                    this.Changed("statusText");
                }
            }
        }

        /// <summary>
        /// 应用名称
        /// </summary>
        public string appName
        {
            get
            {
                return System.Enum.GetName(typeof(AppType), this.type).ToLower() + this.version;
            }
        }

        /// <summary>
        /// 安装/卸载按钮文本
        /// </summary>
        public string commandName
        {
            get
            {
                return this.isInstalled ? "卸载" : "安装";
            }
        }

        public string statusText
        {
            get
            {
                string text = "未知状态";
                switch (this._status)
                {
                    case AppItemStatus.NOT_INSTALL:
                        text = "未安装";
                        break;
                    case AppItemStatus.UNDER_INSTALL:
                        text = "正在安装";
                        break;
                    case AppItemStatus.INSTALLED:
                        text = "已安装";
                        break;
                    case AppItemStatus.UNDER_RUNNING:
                        text = "运行中";
                        break;
                    case AppItemStatus.UNDER_UNISTALL:
                        text = "卸载中";
                        break;
                }
                if (this.progressPercentage != "")
                {
                    text += ("(" + this.progressPercentage + "%)");
                }
                return text;
            }
        }

        //属性修改事件
        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public AppItem(string version, string vcVersion, string downloadUrl, AppType type, AppItemStatus status)
        {
            this.version = version;
            this.vcVersion = vcVersion;
            this.downloadUrl = downloadUrl;
            this.type = type;
            this.status = status;
        }

        public AppItem(string version, string downloadUrl, AppType type, AppItemStatus status) : this(version, "", downloadUrl, type, status)
        { }

        public AppItem(string version, string downloadUrl, AppType type) : this(version, downloadUrl, type, AppItemStatus.NOT_INSTALL)
        { }

    }
}
