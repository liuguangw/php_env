using php_env.service;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;

namespace php_env.items
{
    /// <summary>
    /// 选择的项目
    /// </summary>
    public class AppServerItem : INotifyPropertyChanged
    {
        private AppItem _phpItem = null;
        /// <summary>
        /// 当前选择的php
        /// </summary>
        public AppItem phpItem
        {
            get => this._phpItem;
            set
            {
                if (this._phpItem != value)
                {
                    if (this._phpItem != null)
                    {
                        this._phpItem.PropertyChanged -= appItem_PropertyChanged;
                    }
                    value.PropertyChanged += appItem_PropertyChanged;
                    this._phpItem = value;
                }
            }
        }

        private AppItem _nginxItem = null;
        /// <summary>
        /// 当前选择的nginx
        /// </summary>
        public AppItem nginxItem
        {
            get => this._nginxItem;
            set
            {
                if (this._nginxItem != value)
                {
                    if (this._nginxItem != null)
                    {
                        this._nginxItem.PropertyChanged -= appItem_PropertyChanged;
                    }
                    value.PropertyChanged += appItem_PropertyChanged;
                    this._nginxItem = value;
                }
            }
        }

        private void appItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "isRunning")
            {
                AppItem appItem = sender as AppItem;
                if (appItem.type == AppType.PHP)
                {
                    this.Changed("canSelectPhp");
                    this.Changed("phpCommandName");
                }
                else if (appItem.type == AppType.NGINX)
                {
                    this.Changed("canSelectNginx");
                    this.Changed("nginxCommandName");
                }
            }
        }

        /// <summary>
        /// 当前运行的php进程
        /// </summary>
        private Process phpItemProcess = null;

        /// <summary>
        /// 判断是否可以切换php版本
        /// </summary>
        public bool canSelectPhp
        {
            get
            {
                if (this._phpItem == null)
                {
                    return true;
                }
                else
                {
                    return !this._phpItem.isRunning;
                }
            }
        }

        /// <summary>
        /// 判断是否可以切换nginx版本
        /// </summary>
        public bool canSelectNginx
        {
            get
            {
                if (this._nginxItem == null)
                {
                    return true;
                }
                else
                {
                    return !this._nginxItem.isRunning;
                }
            }
        }

        public string phpCommandName
        {
            get
            {
                return this.canSelectPhp ? "启动" : "停止";
            }
        }

        public string nginxCommandName
        {
            get
            {
                return this.canSelectNginx ? "启动" : "停止";
            }
        }

        /// <summary>
        /// 点击了启动或者停止按钮时执行
        /// </summary>
        public Task onCommand(AppType appType)
        {
            AppItem appItem = null;
            if (appType == AppType.PHP)
            {
                appItem = this._phpItem;
            }
            else if (appType == AppType.NGINX)
            {
                appItem = this._nginxItem;
            }
            if (appItem == null)
            {
                throw new Exception("请先安装" + Enum.GetName(typeof(AppType), appType).ToLower());
            }
            if (!appItem.isRunning)
            {
                return this.runAppItem(appItem);
            }
            else
            {
                return this.stopAppItem(appItem);
            }
        }

        public Task stopAppItem(AppType appType)
        {
            return Task.Run(() =>
           {
               if (appType == AppType.PHP)
               {
                   this.phpItemProcess.Kill();
                   //进程结束时,会自动更新运行状态属性
               }
               else
               {
                   AppItem appItem = this.nginxItem;
                   //nginx -s stop
                   Process myProcess = new Process();
                   myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;//隐藏
                   myProcess.StartInfo.WorkingDirectory = appItem.getAppPath();//工作目录
                   myProcess.StartInfo.FileName = @"nginx.exe";
                   myProcess.StartInfo.Arguments = "-s stop";
                   myProcess.Start();
                   appItem.status = AppItemStatus.INSTALLED;
               }
           });
        }

        private Task stopAppItem(AppItem appItem)
        {
            return this.stopAppItem(appItem.type);
        }

        private Task runAppItem(AppItem appItem)
        {

            return Task.Run(() =>
            {
                Process myProcess = new Process();
                myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;//隐藏
                myProcess.StartInfo.WorkingDirectory = appItem.getAppPath();//工作目录
                if (appItem.type == AppType.PHP)
                {
                    myProcess.StartInfo.FileName = @"php-cgi.exe";
                    myProcess.StartInfo.Arguments = "-b 127.0.0.1:6757";
                    if (this.phpItemProcess != null)
                    {
                        this.phpItemProcess.Exited -= phpProcess_Exited;
                    }
                    //绑定退出事件
                    myProcess.EnableRaisingEvents = true;
                    myProcess.Exited += phpProcess_Exited;
                    this.phpItemProcess = myProcess;//附加进程对象,用于停止服务时调用
                    myProcess.Start();
                }
                else
                {
                    myProcess.StartInfo.FileName = @"nginx.exe";
                    myProcess.Start();
                }
                appItem.status = AppItemStatus.UNDER_RUNNING;
            });
        }

        public Task closeAllApp()
        {
            return Task.Run(async () =>
            {
                if (!this.canSelectPhp)
                {
                    //停止php
                    await this.stopAppItem(AppType.PHP);

                }
                if (!this.canSelectNginx)
                {
                    //停止nginx
                    await this.stopAppItem(AppType.NGINX);
                }
            });
        }

        private void phpProcess_Exited(object sender, EventArgs e)
        {
            this._phpItem.status = AppItemStatus.INSTALLED;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
