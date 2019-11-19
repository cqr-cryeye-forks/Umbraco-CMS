﻿using Moq;
using NUnit.Framework;
using Umbraco.Core.Composing;
using Umbraco.Core.Configuration;
using Umbraco.Tests.TestHelpers;

namespace Umbraco.Tests.Configurations
{

    [TestFixture]
    public class GlobalSettingsTests : BaseWebTest
    {
        private string _root;

        public override void SetUp()
        {
            base.SetUp();
            _root = Current.IOHelper.Root;
        }

        public override void TearDown()
        {
            base.TearDown();
            Current.IOHelper.Root = _root;
        }

        [Ignore("fixme - ignored test")]
        [Test]
        public void Is_Version_From_Assembly_Correct()
        {
            Assert.That(Current.UmbracoVersion.SemanticVersion, Is.EqualTo("6.0.0"));
        }

        [TestCase("~/umbraco", "/", "umbraco")]
        [TestCase("~/umbraco", "/MyVirtualDir", "umbraco")]
        [TestCase("~/customPath", "/MyVirtualDir/", "custompath")]
        [TestCase("~/some-wacky/nestedPath", "/MyVirtualDir", "some-wacky-nestedpath")]
        [TestCase("~/some-wacky/nestedPath", "/MyVirtualDir/NestedVDir/", "some-wacky-nestedpath")]
        public void Umbraco_Mvc_Area(string path, string rootPath, string outcome)
        {
            var globalSettings = SettingsForTests.GenerateMockGlobalSettings();

            var globalSettingsMock = Mock.Get(globalSettings);
            globalSettingsMock.Setup(x => x.Path).Returns(() => Current.IOHelper.ResolveUrl(path));

            Current.IOHelper.Root = rootPath;
            Assert.AreEqual(outcome, globalSettings.GetUmbracoMvcAreaNoCache(IOHelper));
        }





    }
}
