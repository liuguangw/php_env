using System;
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
        public TaskResult(bool success,string message)
        {
            this.success = success;
            this.message = message;
        }

        /// <summary>
        /// 任务执行成功
        /// </summary>
        public TaskResult():this(true,"")
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
        public bool isRunning { get; set; }
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
                    this.Changed("selectionName");
                    this.Changed("statusText");
                    this.Changed("commandText");
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
                return this.version + (this.installed ? "" : "[未安装]");
            }
        }

        /// <summary>
        /// 安装状态文本
        /// </summary>
        public string statusText
        {
            get
            {
                return this.installed ? ("已安装"+(this.isRunning?"[运行中]":"")) : "未安装";
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
            set
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
        public string commandName {
            get {
                return this._isRunning ? "停止" : "启动";
            }
        }

        /// <summary>
        /// 绑定是否可以切换App版本
        /// </summary>
        public bool canSelect {
            get { return !this.isRunning; }
        }

        /// <summary>
        /// 正在运行的对象
        /// </summary>
        public AppItem appItem;
        private Process _process;

        /// <summary>
        /// 正在运行的进程
        /// </summary>
        public Process process {
            get { return this._process; }
            set {
                if (this._process != value) {
                    if (this._process != null) {
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
            this.isRunning = false;
            this.appItem.isRunning = false;
        }

        public AppStatus(bool isRunning = false)
        {
            this.isRunning = isRunning;
            this.handler= new EventHandler(myProcess_Exited);
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
}
