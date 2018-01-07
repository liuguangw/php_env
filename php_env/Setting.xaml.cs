using MahApps.Metro.Controls;
using System.Windows;
using System.Windows.Controls;

namespace php_env
{
    /// <summary>
    /// Setting.xaml 的交互逻辑
    /// </summary>
    public partial class Setting : MetroWindow
    {
        public Setting(MainWindow mainWin)
        {
            this.Owner = mainWin;
            InitializeComponent();
            phpList.DataContext = mainWin.phpList;
        }

        private void phpAction(object sender, System.Windows.RoutedEventArgs e)
        {
            PhpItem s = ((Button)sender).DataContext as PhpItem;
            if (s.installed)
            {
                MessageBox.Show("执行卸载");
                s.installed = false;
            }
            else {
                MessageBox.Show(s.downloadUrl);
                s.installed = true;
            }
        }
    }
}
