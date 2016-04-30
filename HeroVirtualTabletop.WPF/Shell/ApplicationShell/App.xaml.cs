using ApplicationShell.Properties;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ApplicationShell
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>   Gets or sets the application bootstrapper. </summary>
        /// <value> The application bootstrapper. </value>
        public static Bootstrapper appBootstrapper { get; set; }
        /// <summary>   
        /// Application startup event handler. This is part of the WPF framework that we hook into to
        /// startup our prims based bootstrapper. The prism bootstraper is the entry point for this
        /// application and is standard across WPF and Silverlight. 
        /// </summary>
        /// <param name="e">    Event information to send to registered event handlers. </param>
        protected override void OnStartup(StartupEventArgs e)
        {
            StartApplication();
        }

        private void StartApplication()
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture =
                System.Threading.Thread.CurrentThread.CurrentCulture;

            appBootstrapper = new Bootstrapper();
            appBootstrapper.Run();
        }
    }
}
