using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Haley.Abstractions;
using Haley.Enums;
using Haley.Events;
using Haley.Models;
using Haley.Rest;
using Haley.Utils;
using System.Text;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Net;
using System.Threading;

using System.Net.Http;
namespace RestCallTests
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e) {
            var title = AssemblyUtils.GetInfo(AssemblyInfo.Title);
            var des1 = AssemblyUtils.GetInfo(AssemblyInfo.Description);
            var title2 = AssemblyUtils.GetInfo(AssemblyInfo.Product);
            var title3 = AssemblyUtils.GetInfo(AssemblyInfo.Version);
            var title4 = AssemblyUtils.GetInfo(AssemblyInfo.Company);
            var title5 = AssemblyUtils.GetInfo(AssemblyInfo.Trademark);
            var title6 = AssemblyUtils.GetInfo(AssemblyInfo.Copyright);

            ClientStore.AddClient("betaclient", new FluentClient($@"https://daep.withbc.com"));
            var window = new MainWindow();
            window.Show();
            //Task.Run(() => APIService.InitiateSelfHostNetCore());
        }

        private void Application_Exit(object sender, ExitEventArgs e) {

        }
    }
}
