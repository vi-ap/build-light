using BuildLight.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using ThingM.Blink1;
using ThingM.Blink1.ColorProcessor;

namespace BuildLight
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new BuildLightApplicationContext());
        }
    }

    internal class BuildLightApplicationContext : ApplicationContext
    {
        private const string jenkinsApiUrl = "";

        private NotifyIcon trayIcon;
        Blink1 blink1;
        int currentBuildNumber;
        BuildStatus currentBuildStatus;

        internal BuildLightApplicationContext()
        {
            currentBuildNumber = 0;
            currentBuildStatus = BuildStatus.Unknown;

            blink1 = new Blink1();
            blink1.Open();
            blink1.SetColor(new HtmlHexadecimal(HtmlColorName.SlateGray));

            trayIcon = new NotifyIcon()
            {
                Icon = Resources.TrayIcon,
                ContextMenu = new ContextMenu(new MenuItem[]
                {
                    new MenuItem("Exit", Exit)
                }),
                Text = "Build Light",
                Visible = true
            };
        }
        
        private void Exit(object sender, EventArgs evArgs)
        {
            blink1.Close();
            trayIcon.Visible = false;
            Application.Exit();
        }

        private void pingJenkins()
        {

        }

        private void updateLight(BuildStatus buildStatus)
        {
            switch (buildStatus)
            {
                case BuildStatus.Building:
                    blink1.SetColor(new HtmlHexadecimal(HtmlColorName.Yellow));
                    break;
                case BuildStatus.Failure:
                    blink1.SetColor(new HtmlHexadecimal(HtmlColorName.Red));
                    break;
                case BuildStatus.Success:
                    blink1.SetColor(new HtmlHexadecimal(HtmlColorName.Green));
                    break;
                default:
                    blink1.SetColor(new HtmlHexadecimal(HtmlColorName.SlateGray));
                    break;
            }
        }

        private enum BuildStatus
        {
            Success = 1,
            Failure = 2,
            Building = 3,
            Unknown = 4
        }
        
    }
}
