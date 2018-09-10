using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;

namespace nuget2bazel
{
    public class DeleteCommand
    {
        public async Task Do(string package, string rootPath)
        {
            var providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());  // Add v3 API support

            var settings = Settings.LoadDefaultSettings(rootPath, null, new MachineWideSettings());
            var project = new ProjectBazel(rootPath);
            var sourceRepositoryProvider = new SourceRepositoryProvider(settings, providers);
            var solutionManager = new BazelSolutionManager(project, rootPath);
            var deleteManager = new DeleteOnRestart();
            var packageManager = new NuGetPackageManager(sourceRepositoryProvider, settings, solutionManager, deleteManager)
            {
                PackagesFolderNuGetProject = project,
            };

            var projectContext = new ProjectContext();
            var uninstallContext = new UninstallationContext(true, true);

            await packageManager.UninstallPackageAsync(packageManager.PackagesFolderNuGetProject,
                package, uninstallContext, projectContext, 
                CancellationToken.None);
        }
    }
}
