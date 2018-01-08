using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

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
        /// 任务执行成功
        /// </summary>
        public TaskResult()
        {
            this.success = true;
            this.message = "";
        }

        /// <summary>
        /// 任务执行失败
        /// </summary>
        /// <param name="message">错误消息</param>
        public TaskResult(string message)
        {
            this.success = false;
            this.message = message;
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
                }
            }
        }

        public AppItem(string version, string vcVersion, string downloadUrl, AppType type, bool installed)
        {
            this.version = version;
            this.vcVersion = vcVersion;
            this.downloadUrl = downloadUrl;
            this.type = type;
            this.installed = installed;
        }

        public AppItem(string version, string downloadUrl, AppType type, bool installed) : this(version, "", downloadUrl, type, installed)
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

    /// <summary>
    /// 安装状态显示转换
    /// </summary>
    public class InstallResultConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return "已安装";
            }
            else
            {
                return "未安装";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string r = value as string;
            if (r == "已安装")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 安装按钮文本显示转换
    /// </summary>
    public class InstallButtonConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                return "卸载";
            }
            else
            {
                return "安装";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string r = value as string;
            if (r == "卸载")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
