using ConcentrationTracker.Core.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcentrationTracker.Tests
{
    [TestClass]
    public class AppDisplayNameServiceTests
    {
        [TestMethod]
        public void GetDisplayName_KnownProcessName_ReturnsReadableApplicationName()
        {
            string result = AppDisplayNameService.GetDisplayName("devenv");

            Assert.AreEqual("Visual Studio", result);
        }

        [TestMethod]
        public void GetDisplayName_ProcessNameWithExeExtension_RemovesExtensionAndReturnsKnownName()
        {
            string result = AppDisplayNameService.GetDisplayName("chrome.exe");

            Assert.AreEqual("Google Chrome", result);
        }

        [TestMethod]
        public void GetDisplayName_UnknownProcessName_ReturnsHumanizedName()
        {
            string result = AppDisplayNameService.GetDisplayName("unknownapp");

            Assert.AreEqual("Unknownapp", result);
        }

        [TestMethod]
        public void GetDisplayName_EmptyProcessName_ReturnsUnknown()
        {
            string result = AppDisplayNameService.GetDisplayName("   ");

            Assert.AreEqual("Unknown", result);
        }
    }
}
