using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace php_env
{
    /// <summary>
    /// 任务结果封装
    /// </summary>
    public class TaskResult
    {
        /// <summary>
        /// 任务是否完成
        /// </summary>
        public bool success { get; }
        /// <summary>
        /// 任务失败原因
        /// </summary>
        public string message { get; }

        /// <summary>
        /// 任务执行结果
        /// </summary>
        /// <param name="success"></param>
        /// <param name="message"></param>
        public TaskResult(bool success, string message)
        {
            this.success = success;
            this.message = message;
        }

        /// <summary>
        /// 任务执行成功
        /// </summary>
        public TaskResult() : this(true, "")
        {
        }

        /// <summary>
        /// 任务执行失败
        /// </summary>
        /// <param name="message">错误消息</param>
        public TaskResult(string message) : this(false, message)
        {
        }

        /// <summary>
        /// 任务执行失败
        /// </summary>
        /// <param name="e">异常</param>
        public TaskResult(Exception e) : this(e.Message)
        {
        }
    }
    /// <summary>
    /// 应用类型
    /// </summary>
    public enum AppType
    {
        php, nginx, vc
    }

    public class AppItem : INotifyPropertyChanged
    {
        public string version { get; }
        public string vcVersion { get; }
        public string downloadUrl { get; }
        public AppType type { get; }
        private bool _isRunning;
        public bool isRunning
        {
            get
            {
                return this._isRunning;
            }
            set
            {
                if (this._isRunning != value)
                {
                    this._isRunning = value;
                    this.Changed("isRunning");
                    this.Changed("statusText");
                }
            }
        }
        private bool _installed;
        public bool installed
        {
            get
            {
                return this._installed;
            }
            set
            {
                if (this._installed != value)
                {
                    this._installed = value;
                    this.Changed("installed");
                    this.Changed("statusText");
                    this.Changed("commandText");
                }
            }
        }

        private string _progressPercentage = "-";

        private string progressPercentage {
            get {
                return this._progressPercentage;
            }
            set {
                if (this._progressPercentage != value) {
                    this._progressPercentage = value;
                    this.Changed("progressPercentage");
                    this.Changed("statusText");
                }
            }

        }

        /// <summary>
        /// 更新任务进度
        /// </summary>
        /// <param name="bytesReceived"></param>
        /// <param name="totalBytesToReceive"></param>
        /// <param name="progressPercentage"></param>
        public void updateProgress(long bytesReceived, long totalBytesToReceive, int progressPercentage)
        {
            this.progressMaximum = totalBytesToReceive;
            this.progressPercentage = progressPercentage.ToString();
        }

        /// <summary>
        /// 重置进度
        /// </summary>
        public void resetProgress()
        {
            this.progressMaximum = 0;
        }

        /// <summary>
        /// 显示不可预知的进度
        /// </summary>
        public void setPendingProgress()
        {
            this.progressMaximum = -1;
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

        private double _progressMaximum = 0;

        /// <summary>
        /// 进度最大值 0表示已停止 -1表示进度无法显示 其他值为有效值
        /// </summary>
        private double progressMaximum
        {
            get
            {
                return this._progressMaximum;
            }
            set
            {
                if (this._progressMaximum != value)
                {
                    this._progressMaximum = value;
                    this.canModify = (value == 0);
                    this.Changed("progressMaximum");
                    this.Changed("statusText");
                }
            }
        }

        /// <summary>
        /// 下拉框文本
        /// </summary>
        public string selectionName
        {
            get
            {
                return Enum.GetName(typeof(AppType), this.type) + this.version;
            }
        }

        /// <summary>
        /// 安装状态文本
        /// </summary>
        public string statusText
        {
            get
            {
                if (this._progressMaximum == -1)
                {
                    return this.installed ? "卸载中" : "安装中";
                }
                else if (this._progressMaximum > 0)
                {
                    return "下载中[" + this.progressPercentage + "%]";
                }
                return this.installed ? ("已安装" + (this.isRunning ? "[运行中]" : "")) : "未安装";
            }
        }

        /// <summary>
        /// 对应的按钮文本
        /// </summary>
        public string commandText
        {
            get
            {
                return this.installed ? "卸载" : "安装";
            }
        }

        public AppItem(string version, string vcVersion, string downloadUrl, AppType type, bool installed, bool isRunning = false)
        {
            this.version = version;
            this.vcVersion = vcVersion;
            this.downloadUrl = downloadUrl;
            this.type = type;
            this.installed = installed;
            this.isRunning = isRunning;
        }

        public AppItem(string version, string downloadUrl, AppType type, bool installed, bool isRunning = false) : this(version, "", downloadUrl, type, installed, isRunning)
        { }


        public AppItem(string version, string downloadUrl, AppType type) : this(version, downloadUrl, type, false)
        { }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Changed(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
    public class AppStatus : INotifyPropertyChanged
    {
        private bool _isRunning;
        public bool isRunning
        {
            get
            {
                return this._isRunning;
            }
            //只允许内部修改状态
            private set
            {
                if (this._isRunning != value)
                {
                    this._isRunning = value;
                    this.Changed("isRunning");
                    this.Changed("commandName");
                    this.Changed("canSelect");
                }
            }
        }

        /// <summary>
        /// 应用对应的命令文本
        /// </summary>
        public string commandName
        {
            get
            {
                return this._isRunning ? "停止" : "启动";
            }
        }

        /// <summary>
        /// 绑定是否可以切换App版本
        /// </summary>
        public bool canSelect
        {
            get { return !this._isRunning; }
        }

        private AppItem _appItem;
        /// <summary>
        /// 正在运行的对象
        /// </summary>
        public AppItem appItem
        {
            get
            {
                return this._appItem;
            }
            set
            {
                if (this._appItem != value)
                {
                    //移除原数据绑定
                    if (this._appItem != null)
                    {
                        this._appItem.PropertyChanged -= this.AppItem_PropertyChanged;
                    }
                    //添加新数据绑定
                    if (value != null)
                    {
                        this._appItem = value;
                        value.PropertyChanged += this.AppItem_PropertyChanged;
                        this.isRunning = value.isRunning;
                    }
                    else
                    {
                        this.isRunning = false;
                    }
                }
            }
        }

        /// <summary>
        /// 运行状态依赖于appItem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.isRunning = this._appItem.isRunning;
        }

        private Process _process;

        /// <summary>
        /// 正在运行的进程
        /// </summary>
        public Process process
        {
            get { return this._process; }
            set
            {
                if (this._process != value)
                {
                    if (this._process != null)
                    {
                        this.process.Exited -= this.handler;
                    }
                    value.EnableRaisingEvents = true;
                    value.Exited += this.handler;
                    this._process = value;
                }
            }
        }
        private EventHandler handler;

        private void myProcess_Exited(object sender, EventArgs e)
        {
            this.appItem.isRunning = false;
        }

        public AppStatus(bool isRunning = false)
        {
            this.isRunning = isRunning;
            this.handler = new EventHandler(myProcess_Exited);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(String propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
    /// <summary>
    /// 获取composer安装路径的任务结果
    /// </summary>
    public class ComposerPathTaskResult {

        /// <summary>
        /// 任务是否完成
        /// </summary>
        public bool success { get; }

        /// <summary>
        /// 任务失败原因
        /// </summary>
        public string message { get; }

        /// <summary>
        /// 文件夹路径列表
        /// </summary>
        public List<string> pathList;

        /// <summary>
        /// 任务执行结果
        /// </summary>
        /// <param name="success"></param>
        /// <param name="message"></param>
        public ComposerPathTaskResult(bool success, string message, List<string> pathList)
        {
            this.success = success;
            this.message = message;
            this.pathList = pathList;
        }

        public ComposerPathTaskResult(string message) : this(false, message, null) {
        }

        public ComposerPathTaskResult(Exception e) : this(e.Message)
        {
        }

        public ComposerPathTaskResult(List<string> pathList) : this(true,"", pathList)
        {
        }
    }
}
