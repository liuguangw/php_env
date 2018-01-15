﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using php_env.items;

namespace php_env.service
{
    public class ComposerInstall
    {
        private Setting setting;

        public ComposerInstall(Setting setting)
        {
            this.setting = setting;
        }

        /// <summary>
        /// 获取Path环境变量中已经安装过composer的目录
        /// </summary>
        /// <returns></returns>
        public Task<List<string>> getInstalledDirsAsync()
        {
            return Task.Run(() =>
            {
                List<string> result = new List<string>();
                List<string> pathList = PathEnvironment.getPathList(EnvironmentVariableTarget.Machine);
                pathList.AddRange(PathEnvironment.getPathList(EnvironmentVariableTarget.User));
                FileInfo tmpFileinfo;
                foreach (string tmpPath in pathList)
                {
                    tmpFileinfo = new FileInfo(tmpPath + @"\composer.bat");
                    if (tmpFileinfo.Exists)
                    {
                        result.Add(tmpPath);
                    }
                }
                return result;
            });
        }

        public Task installAsync(string appPath, List<string> toRemoveDirs)
        {
            return Task.Run(async () =>
            {
                List<string> userPathList = PathEnvironment.getPathList(EnvironmentVariableTarget.User);
                //删除已安装的composer:文件+用户Path环境变量
                if (toRemoveDirs != null)
                {
                    bool needDelEnv = false;
                    foreach (string tmpPath in toRemoveDirs)
                    {
                        FileInfo batFile = new FileInfo(tmpPath + @"\composer.bat");
                        if (batFile.Exists)
                        {
                            batFile.Delete();
                        }
                        FileInfo pharFile = new FileInfo(tmpPath + @"\composer.phar");
                        if (pharFile.Exists)
                        {
                            pharFile.Delete();
                        }
                        if (userPathList.Contains(tmpPath))
                        {
                            needDelEnv = true;
                            userPathList.Remove(tmpPath);
                        }
                    }
                    if (needDelEnv)
                    {
                        PathEnvironment.setPathList(userPathList, EnvironmentVariableTarget.User);
                    }
                }
                //获取url
                string composerUrl = null;
                this.setting.Dispatcher.Invoke(() =>
                {
                    MainWindow mainWin = this.setting.Owner as MainWindow;
                    composerUrl = mainWin.xmlResource.composerUrl;
                });
                //下载
                string savePath = appPath + @"\composer.phar";
                DownloadHelper downloadHelper = new DownloadHelper();
                await downloadHelper.downloadFileAsync(new Uri(composerUrl), savePath, (long processed, long total) =>
                {
                    this.setting.Dispatcher.Invoke(() =>
                    {
                        this.setting.composerProgressBar.IsIndeterminate = false;
                        this.setting.composerProgressBar.Value = processed;
                        this.setting.composerProgressBar.Maximum = total;
                    });
                });
                this.setting.Dispatcher.Invoke(() =>
                {
                    this.setting.composerProgressBar.IsIndeterminate = true;
                });
                //生成批处理文件
                File.WriteAllText(appPath + @"\composer.bat", "@php \"%~dp0composer.phar\" %*", System.Text.Encoding.Default);
                //判断Path环境变量中是否有当前目录
                List<string> pathList = PathEnvironment.getPathList(EnvironmentVariableTarget.Machine);
                pathList.AddRange(PathEnvironment.getPathList(EnvironmentVariableTarget.User));
                if (!pathList.Contains(appPath))
                {
                    userPathList.Add(appPath);
                    PathEnvironment.setPathList(userPathList, EnvironmentVariableTarget.User);
                }
                //
            });
        }

        public Task<string> getComposerInfoAsync(string appPath)
        {
            return Task<string>.Run(() =>
            {
                string result = "";
                using (Process myProcess = new Process())
                {
                    myProcess.StartInfo.UseShellExecute = false;
                    myProcess.StartInfo.RedirectStandardOutput = true;
                    myProcess.StartInfo.WorkingDirectory = appPath;
                    myProcess.StartInfo.FileName = "php.exe";
                    myProcess.StartInfo.Arguments = "composer.phar -v";
                    myProcess.StartInfo.CreateNoWindow = true;
                    myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;//隐藏
                    myProcess.Start();
                    StreamReader reader = myProcess.StandardOutput;
                    result = reader.ReadToEnd();
                    myProcess.WaitForExit();
                }
                return result;
            });
        }
    }
}
