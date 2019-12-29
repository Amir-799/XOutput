﻿using System;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using XOutput.Logging;
using XOutput.Tools;
using XOutput.UI.Windows;

namespace XOutput
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static readonly ILogger logger = LoggerFactory.GetLogger(typeof(App));

        private MainWindowViewModel mainWindowViewModel;
        private MainWindow mainWindow;

        public App()
        {
            Dispatcher.UnhandledException += (object sender, DispatcherUnhandledExceptionEventArgs e) => UnhandledException(e.Exception, LogLevel.Error);
            AppDomain.CurrentDomain.FirstChanceException += (object sender, FirstChanceExceptionEventArgs e) => UnhandledException(e.Exception, LogLevel.Info);
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) => UnhandledException(e.ExceptionObject as Exception, LogLevel.Error);
            TaskScheduler.UnobservedTaskException += (object sender, UnobservedTaskExceptionEventArgs e) => UnhandledException(e.Exception, LogLevel.Error);
            DependencyEmbedder dependencyEmbedder = new DependencyEmbedder();
            dependencyEmbedder.AddPackage("Newtonsoft.Json");
            dependencyEmbedder.AddPackage("SharpDX.DirectInput");
            dependencyEmbedder.AddPackage("SharpDX");
            dependencyEmbedder.AddPackage("Hardcodet.Wpf.TaskbarNotification");
            dependencyEmbedder.AddPackage("Nefarius.ViGEm.Client");
            dependencyEmbedder.Initialize();
            string exePath = Assembly.GetExecutingAssembly().Location;
            string cwd = Path.GetDirectoryName(exePath);
            Directory.SetCurrentDirectory(cwd);
        }

        public void UnhandledException(Exception exceptionObject, LogLevel level)
        {
            logger.Log(exceptionObject.ToString(), level);
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            ApplicationContext globalContext = ApplicationContext.Global;
            globalContext.Resolvers.Add(Resolver.CreateSingleton(Dispatcher));
            globalContext.AddFromConfiguration(typeof(ApplicationConfiguration));
            globalContext.AddFromConfiguration(typeof(UI.UIConfiguration));

            var singleInstanceProvider = globalContext.Resolve<SingleInstanceProvider>();
            var argumentParser = globalContext.Resolve<ArgumentParser>();
            if (singleInstanceProvider.TryGetLock())
            {
                singleInstanceProvider.StartNamedPipe();
                try {
                    mainWindow = ApplicationContext.Global.Resolve<MainWindow>();
                    mainWindowViewModel = mainWindow.ViewModel;
                    MainWindow = mainWindow;
                    singleInstanceProvider.ShowEvent += mainWindow.ForceShow;
                    if (!argumentParser.Minimized)
                    {
                        mainWindow.Show();
                    }
#if !DEBUG
                    ApplicationContext.Global.Resolve<Devices.Input.Mouse.MouseHook>().StartHook();              
#endif
                } catch (Exception ex) {
                    logger.Error(ex);
                    Application.Current.Shutdown();
                }
            }
            else
            {
                singleInstanceProvider.Notify();
                Application.Current.Shutdown();
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            mainWindow.CleanUp();
            ApplicationContext.Global.Close();
        }
    }
}
