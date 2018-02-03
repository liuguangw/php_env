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
        //0=>php 1=>nginx

        /// <summary>
        /// 进程对象数组
        /// </summary>
        private Process[] processArr = new Process[] { null, null };

        /// <summary>
        /// 错误消息数组
        /// </summary>
        private string[] processError = new string[] { null, null };

        /// <summary>
        /// 是否强制结束PHP进程(用于区分异常退出)
        /// </summary>
        private bool forceKillPhp = false;

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

        private MainWindow mainWindow;

        public AppServerItem(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
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
                   this.forceKillPhp = true;
                   this.processArr[0].Kill();
                   //进程结束时,会自动更新运行状态属性
               }
               else
               {
                   AppItem appItem = this.nginxItem;
                   //nginx -s stop
                   Process myProcess = new Process();
                   myProcess.StartInfo.CreateNoWindow = true;//隐藏
                   myProcess.StartInfo.UseShellExecute = false;
                   myProcess.StartInfo.WorkingDirectory = appItem.getAppPath();//工作目录
                   myProcess.StartInfo.FileName = appItem.getAppPath() + @"\nginx.exe";
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

            return Task.Run((Action)(() =>
            {
                Process myProcess = new Process();
                myProcess.StartInfo.WorkingDirectory = appItem.getAppPath();//工作目录
                myProcess.StartInfo.CreateNoWindow = true;//隐藏
                myProcess.StartInfo.UseShellExecute = false;
                myProcess.StartInfo.RedirectStandardError = true;
                myProcess.EnableRaisingEvents = true;
                if (appItem.type == AppType.PHP)
                {
                    myProcess.StartInfo.FileName = appItem.getAppPath() + @"\php-cgi.exe";
                    myProcess.StartInfo.Arguments = "-b 127.0.0.1:6757";
                    if (this.processArr[0] != null)
                    {
                        this.processArr[0].Exited -= phpProcess_Exited;
                        this.processArr[0].ErrorDataReceived -= phpErrorHandler;
                    }
                    this.forceKillPhp = false;
                    //绑定退出事件、错误事件
                    myProcess.Exited += phpProcess_Exited;
                    myProcess.ErrorDataReceived += phpErrorHandler;
                    this.processArr[0] = myProcess;//附加进程对象,用于停止服务时调用
                    this.processError[0] = "";
                }
                else
                {
                    myProcess.StartInfo.FileName = appItem.getAppPath() + @"\nginx.exe";
                    if (this.processArr[1] != null)
                    {
                        this.processArr[1].Exited -= nginxProcess_Exited;
                        this.processArr[1].ErrorDataReceived -= nginxErrorHandler;
                    }
                    //绑定退出事件、错误事件
                    myProcess.Exited += nginxProcess_Exited;
                    myProcess.ErrorDataReceived += nginxErrorHandler;
                    this.processArr[1] = myProcess;//附加进程对象,用于停止服务时调用
                    this.processError[1] = "";
                }
                myProcess.Start();
                myProcess.BeginErrorReadLine();
                appItem.status = AppItemStatus.UNDER_RUNNING;
            }));
        }

        private void phpErrorHandler(object sender, DataReceivedEventArgs errLine)
        {
            if (!String.IsNullOrEmpty(errLine.Data))
            {
                this.processError[0] += errLine.Data + "\r\n";
            }
        }

        private void nginxErrorHandler(object sender, DataReceivedEventArgs errLine)
        {
            if (!String.IsNullOrEmpty(errLine.Data))
            {
                this.processError[1] += errLine.Data + "\r\n";
            }
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
            //异常退出
            if ((!this.forceKillPhp) && (this.processArr[0].ExitCode != 0))
            {
                string title = "php异常退出";
                string errMsg = this.processError[0].TrimEnd("\r\n".ToCharArray());
                if (String.IsNullOrEmpty(errMsg))
                {
                    this.mainWindow.showErrorMessage(title);
                }
                else
                {
                    this.mainWindow.showErrorMessage(errMsg, title);
                }
            }
        }

        private void nginxProcess_Exited(object sender, EventArgs e)
        {
            this._nginxItem.status = AppItemStatus.INSTALLED;
            //异常退出
            if (this.processArr[1].ExitCode != 0)
            {
                string title = "nginx异常退出";
                string errMsg = this.processError[1].TrimEnd("\r\n".ToCharArray());
                if (String.IsNullOrEmpty(errMsg))
                {
                    this.mainWindow.showErrorMessage(title);
                }
                else
                {
                    this.mainWindow.showErrorMessage(errMsg, title);
                }
            }
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
