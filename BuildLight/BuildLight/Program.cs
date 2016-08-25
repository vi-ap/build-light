using BuildLight.Properties;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Principal;
using System.Text;
using System.Threading;
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
            BuildLightApplicationContext buildApplicationContext = new BuildLightApplicationContext();
            Application.Run(buildApplicationContext);
            buildApplicationContext.pingJenkinsAndHandleResponse();
        }
    }

    public class BuildLightApplicationContext : ApplicationContext
    {
        private const string buildsMainPageUrl = "/api/json";
        private const string buildDetailsUrl = "/{0}/api/json";

        private NotifyIcon trayIcon;
        Blink1 blink1;
        public int currentBuildNumber;

        public BuildLightApplicationContext()
        {
            currentBuildNumber = 0;

            blink1 = new Blink1();
            blink1.Open();
            blink1.SetColor(new HtmlHexadecimal(HtmlColorName.Gray));

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
            close();
            Application.Exit();
        }

        public void close()
        {
            blink1.Close();
            trayIcon.Visible = false;
        }

        public void pingJenkinsAndHandleResponse()
        {
            if(!isCurrentBuildLatest())
            {
                updateLight(getLatestBuildStatus());
            }

            Thread.Sleep(15000);
            pingJenkinsAndHandleResponse();
        }

        private string getResponseFromJenkins(WebRequest webRequest)
        {
            string username = "";
            string apiToken = "";
            string basicAuthToken = Convert.ToBase64String(Encoding.Default.GetBytes(username + ":" + apiToken));
            webRequest.Headers["Authorization"] = "Basic " + basicAuthToken;
            webRequest.PreAuthenticate = true;
            WebResponse response = webRequest.GetResponse();
            string responseString;
            using (StreamReader streamReader = new StreamReader(response.GetResponseStream()))
            {
                responseString = streamReader.ReadToEnd();
            }

            return responseString;
        }

        private bool isCurrentBuildLatest()
        {
            WebRequest buildRequest = WebRequest.Create(buildsMainPageUrl);
            buildRequest.ContentType = "application/json";
            return isCurrentBuildLatest(getResponseFromJenkins(buildRequest));
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

        public string getLatestBuildStatus()
        {
            WebRequest buildDetailsRequest = WebRequest.Create(String.Format(buildDetailsUrl, currentBuildNumber));
            buildDetailsRequest.ContentType = "application/json";
            return getLatestBuildStatusFromJson(getResponseFromJenkins(buildDetailsRequest));
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
