using BuildLight.Properties;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Principal;
using System.Text;
using System.Threading;
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

    public class BuildLightApplicationContext : ApplicationContext
    {
        private const string buildsMainPageUrl = "/api/json";
        private const string buildDetailsUrl = "/{0}/api/json";

        private NotifyIcon trayIcon;
        Blink1 blink1;
        private int currentBuildNumber;
        private string currentBuildStatus;

        public BuildLightApplicationContext()
        {
            currentBuildNumber = 0;
            currentBuildStatus = BuildStatusConstants.UNKNOWN;

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

            pingJenkinsAndHandleResponse();
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

        private void pingJenkinsAndHandleResponse()
        {
            var delayTask = Task.Run(async () =>
            {
                setLatestBuildNumber();
                if(isCurrentBuildStatusOutdated())
                {
                    updateLight(currentBuildStatus);
                }
                
                await Task.Delay(15000);
            });

            delayTask.Wait();
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

        public void setLatestBuildNumber()
        {
            WebRequest buildRequest = WebRequest.Create(buildsMainPageUrl);
            buildRequest.ContentType = "application/json";
            int buildNumber = getLatestBuildNumber(getResponseFromJenkins(buildRequest));
            if (currentBuildNumber != buildNumber)
            {
                Console.WriteLine("Found new build. Updating current build number to: " + buildNumber);
                currentBuildNumber = buildNumber;
            }
        }

        public int getLatestBuildNumber(string jsonString)
        {
            JObject jsonResponse = JObject.Parse(jsonString);
            var topBuild = jsonResponse["builds"].First;
            return (int)topBuild["number"];
        }

        public bool isCurrentBuildStatusOutdated()
        {
            WebRequest buildDetailsRequest = WebRequest.Create(String.Format(buildDetailsUrl, currentBuildNumber));
            buildDetailsRequest.ContentType = "application/json";
            string buildStatus = getLatestBuildStatusFromJson(getResponseFromJenkins(buildDetailsRequest));
            if (currentBuildStatus != buildStatus)
            {
                Console.WriteLine("Build status changed. Updating build latest to: " + buildStatus);
                currentBuildStatus = buildStatus;
                return true;
            }

            return false;
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
