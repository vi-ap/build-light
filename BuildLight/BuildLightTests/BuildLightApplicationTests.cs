using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace BuildLight
{
    [TestClass]
    public class BuildLightApplicationTests
    {
        private string jsonWithStatusBuilding = JsonConvert.SerializeObject(new { building = true, result = "" });

        private string jsonWithStatusFailure = JsonConvert.SerializeObject(new { building = false, result = "FAILURE" });

        private string jsonWithStatusSuccess = JsonConvert.SerializeObject(new { building = false, result = "SUCCESS" });

        private string jsonWithMainBuildPageInfo = JsonConvert.SerializeObject(new { builds = new object[] { new { number = 10, url = "some/url" }, new { number = 9, url = "some/url" } } });

        private BuildLightApplicationContext application;

        [TestCleanup]
        public void Cleanup()
        {
            application.close();
        }

        [TestMethod]
        public void TestJsonParsingWithStatusBuilding()
        {
            application = new BuildLightApplicationContext();
            string result = application.getLatestBuildStatusFromJson(jsonWithStatusBuilding);
            Assert.AreEqual(result, BuildStatusConstants.BUILDING);
        }

        [TestMethod]
        public void TestJsonParsingWithStatusFailure()
        {
            application = new BuildLightApplicationContext();
            string result = application.getLatestBuildStatusFromJson(jsonWithStatusFailure);
            Assert.AreEqual(result, BuildStatusConstants.FAILURE);
        }

        [TestMethod]
        public void TestJsonParsingWithStatusSuccess()
        {
            application = new BuildLightApplicationContext();
            string result = application.getLatestBuildStatusFromJson(jsonWithStatusSuccess);
            Assert.AreEqual(result, BuildStatusConstants.SUCCESS);
        }

        [TestMethod]
        public void TestJsonParsingWithCurrentBuildLatest()
        {
            application = new BuildLightApplicationContext();
            application.currentBuildNumber = 10;
            bool result = application.isCurrentBuildLatest(jsonWithMainBuildPageInfo);
            Assert.IsTrue(result);
            Assert.AreEqual(application.currentBuildNumber, 10);
        }

        [TestMethod]
        public void TestJsonParsingWithCurrentBuildNotLatest()
        {
            application = new BuildLightApplicationContext();
            application.currentBuildNumber = 8;
            bool result = application.isCurrentBuildLatest(jsonWithMainBuildPageInfo);
            Assert.IsFalse(result);
            Assert.AreEqual(application.currentBuildNumber, 10);
        }
    }
}
