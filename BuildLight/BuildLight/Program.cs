using BuildLight.Properties;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
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

    public class BuildLightApplicationContext : ApplicationContext
    {
        private const string BuildsMainPageUrl = "/api/json";
        private const string BuildDetailsUrl = "{0}/api/json";

        private NotifyIcon trayIcon;
        Blink1 blink1;

        public int currentBuildNumber;

        public BuildLightApplicationContext()
        {
            currentBuildNumber = 0;

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

        private void pingJenkinsAndParseResponse()
        {
            if(!isCurrentBuildLatest())
            {
                updateLight(getLatestBuildStatus());
            }
        }

        private bool isCurrentBuildLatest()
        {
            WebRequest buildRequest = WebRequest.Create(BuildsMainPageUrl);
            buildRequest.ContentType = "application/json";
            return isCurrentBuildLatest(buildRequest.GetResponse().ToString());
        }

        public bool isCurrentBuildLatest(string jsonString)
        {
            JObject jsonResponse = JObject.Parse(jsonString);

            var topBuild = jsonResponse["builds"].First;
            int buildNumber = (int)topBuild["number"];
            if (buildNumber > currentBuildNumber)
            {
                currentBuildNumber = buildNumber;
                return false;
            }

            return true;
        }

        private string getLatestBuildStatus()
        {
            WebRequest buildDetailsRequest = WebRequest.Create(String.Format(BuildDetailsUrl, currentBuildNumber));
            buildDetailsRequest.ContentType = "application/json";
            return getLatestBuildStatusFromJson(buildDetailsRequest.GetResponse().ToString());
        }

        public string getLatestBuildStatusFromJson(string jsonString)
        {
            JObject buildDetailsJson = JObject.Parse(jsonString);
            bool currentlyBuilding = (bool)buildDetailsJson["building"];
            if (currentlyBuilding)
            {
                return BuildStatusConstants.BUILDING;
            }

            string buildResult = (string)buildDetailsJson["result"];
            
            return buildResult;
        }

        private void updateLight(string buildStatus)
        {
            switch (buildStatus)
            {
                case BuildStatusConstants.BUILDING:
                    blink1.SetColor(new HtmlHexadecimal(HtmlColorName.Yellow));
                    break;
                case BuildStatusConstants.FAILURE:
                    blink1.SetColor(new HtmlHexadecimal(HtmlColorName.Red));
                    break;
                case BuildStatusConstants.SUCCESS:
                    blink1.SetColor(new HtmlHexadecimal(HtmlColorName.Green));
                    break;
                default:
                    blink1.SetColor(new HtmlHexadecimal(HtmlColorName.SlateGray));
                    break;
            }
        }
        
    }

    public static class BuildStatusConstants
    {
        public const string SUCCESS = "SUCCESS";
        public const string FAILURE = "FAILURE";
        public const string BUILDING = "BUILDING";
        public const string UNKNOWN = "UNKNOWN";
    }
}
