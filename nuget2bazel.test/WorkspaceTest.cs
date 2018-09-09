using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace nuget2bazel.test
{
    [TestClass]
    public class WorkspaceTest
    {
        [TestMethod]
        public void EmptyWorkspace()
        {
            var workspaceWriter = new WorkspaceWriter();
            //var entry = new WorkspaceEntry();
            //var section = workspaceWriter.UpdateEntry("", entry);
        }
    }
}
