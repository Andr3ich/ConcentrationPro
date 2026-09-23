using System;
using System.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ConcentrationTracker.Tests
{
    [TestClass]
    public class AssemblyInitializer
    {
        [AssemblyInitialize]
        public static void Initialize(TestContext context)
        {
            if (Application.Current == null)
            {
                new Application();
            }

            AddDictionary("/ConcentrationTracker;component/Resources/Localization/StringsEn.xaml");
            AddDictionary("/ConcentrationTracker;component/Resources/Localization/TrayStringsEn.xaml");
        }

        private static void AddDictionary(string path)
        {
            ResourceDictionary dictionary = new ResourceDictionary
            {
                Source = new Uri(path, UriKind.RelativeOrAbsolute)
            };

            Application.Current.Resources.MergedDictionaries.Add(dictionary);
        }
    }
}
