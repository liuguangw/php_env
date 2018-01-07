using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;

namespace php_env
{
    public enum AppType
    {
        php, nginx
    }
    public class PhpItem : INotifyPropertyChanged
    {
        public string version { get; }
        public string vcVersion { get; }
        public string downloadUrl { get; }
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
        public PhpItem(string version, string vcVersion, string downloadUrl, bool installed)
        {
            this.version = version;
            this.vcVersion = vcVersion;
            this.downloadUrl = downloadUrl;
            this.installed = installed;
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
    public class NginxItem : INotifyPropertyChanged
    {
        public string version { get; }
        public string downloadUrl { get; }
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
        public NginxItem(string version, string downloadUrl, bool installed)
        {
            this.version = version;
            this.downloadUrl = downloadUrl;
            this.installed = installed;
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
    public class VcItem
    {
        public string version { get; }
        public string downloadUrl { get; }
        public VcItem(string version, string downloadUrl)
        {
            this.version = version;
            this.downloadUrl = downloadUrl;
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
