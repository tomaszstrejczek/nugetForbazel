using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace nuget2bazel.test
{
    [TestClass]
    public class ParserTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            var toparse = @"
nuget_package(
    name = ""xunit"",
    package = ""xunit"",
    version = ""2.4.0"",
    sha256 = ""123456"",
    core_lib = ""lib/netstabdard2.0/Npgsql.dll"",
    net_lib = ""lib/net451/Npgsql.dll"",
    mono_lib = ""lib/net451/Npgsql.dll"",
    deps = [
    ],
    core_files = [
        ""lib/netstabdard2.0/Npgsql.xml"",
        ""ala.doc"",
    ],
    net_files = [
        ""lib/netstabdard2.0/Npgsql.xml"",
        ""ala.doc"",
    ],
    mono_files = [
        ""lib/netstabdard2.0/Npgsql.xml"",
        ""ala.doc"",
    ],
)
";

            var parser = new WorkspaceParser(toparse);
            var entries = parser.Parse().ToList();
            Assert.AreEqual(1, entries.Count);

            var entry = entries.First();

            Assert.AreEqual("xunit", entry.PackageIdentity.Id);
            Assert.AreEqual("2.4.0", entry.PackageIdentity.Version.ToString());
            Assert.AreEqual("123456", entry.Sha256);
            Assert.AreEqual("lib/netstabdard2.0/Npgsql.dll", entry.CoreLib);
            Assert.AreEqual("lib/net451/Npgsql.dll", entry.NetLib);
            Assert.AreEqual("lib/net451/Npgsql.dll", entry.MonoLib);
        }
    }
}
