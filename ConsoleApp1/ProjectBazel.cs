using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.ProjectManagement.Projects;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace nuget2bazel
{
    public class ProjectBazel: FolderNuGetProject
    {
        public string JsonConfigPath { get; set; }
        public string WorkspacePath { get; set; }
        public IEnumerable<NuGetProjectAction> NuGetProjectActions { get; set; }

        public ProjectBazel(string root) : base(Path.Combine(root, "packages"))
        {
            JsonConfigPath = Path.Combine(root, "packages.json");
            WorkspacePath = Path.Combine(root, "WORKSPACE");
        }

        private async Task<JObject> GetJsonAsync()
        {
            if (!File.Exists(JsonConfigPath))
                using (var sw = new StreamWriter(JsonConfigPath))
                {
                    await sw.WriteAsync("{}");
                    return new JObject();
                }

            using (var reader = File.OpenText(JsonConfigPath))
            {
                return JObject.Parse(await reader.ReadToEndAsync());
            }
        }

        private async Task<string> GetWorkspaceAsync()
        {
            if (!File.Exists(WorkspacePath))
                using (var sw = new StreamWriter(WorkspacePath))
                {
                    await sw.WriteAsync("");
                    return "";
                }

            using (var reader = File.OpenText(WorkspacePath))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private async Task SaveJsonAsync(JObject json)
        {
            using (var writer = new StreamWriter(JsonConfigPath, false, Encoding.UTF8))
            {
                await writer.WriteAsync(json.ToString());
            }
        }

        private async Task SaveWorkspaceAsync(string workspace)
        {
            using (var writer = new StreamWriter(WorkspacePath, false, Encoding.UTF8))
            {
                await writer.WriteAsync(workspace);
            }
        }

        private string GetSha(Stream stream)
        {
            var hash = SHA256.Create();
            var bytes = hash.ComputeHash(stream);
            var builder = new StringBuilder();
            foreach (var t in bytes)
            {
                builder.Append(t.ToString("x2"));
            }
            return builder.ToString();
        }

        public override async Task<IEnumerable<PackageReference>> GetInstalledPackagesAsync(CancellationToken token)
        {
            var packages = new List<PackageReference>();

            //  Find all dependencies and convert them into packages.config style references
            foreach (var dependency in JsonConfigUtility.GetDependencies(await GetJsonAsync()))
            {
                // Use the minimum version of the range for the identity
                var identity = new PackageIdentity(dependency.Id, dependency.VersionRange.MinVersion);

                // Pass the actual version range as the allowed range
                packages.Add(new PackageReference(identity,
                    targetFramework: NuGetFramework.AnyFramework,
                    userInstalled: true,
                    developmentDependency: false,
                    requireReinstallation: false,
                    allowedVersions: dependency.VersionRange));
            }

            return packages;
        }

        public override async Task<bool> InstallPackageAsync(
            PackageIdentity packageIdentity,
            DownloadResourceResult downloadResourceResult,
            INuGetProjectContext nuGetProjectContext,
            CancellationToken token)
        {
            var dependency = new PackageDependency(packageIdentity.Id, new VersionRange(packageIdentity.Version));

            var json = await GetJsonAsync();
            JsonConfigUtility.AddDependency(json, dependency);
            await SaveJsonAsync(json);

            var packageReader = downloadResourceResult.PackageReader
                                ?? new PackageArchiveReader(downloadResourceResult.PackageStream, leaveStreamOpen: true);
            IAsyncPackageContentReader packageContentReader = packageReader;
            IAsyncPackageCoreReader packageCoreReader = packageReader;

            var libItemGroups = await packageContentReader.GetLibItemsAsync(token);
            var referenceItemGroups = await packageContentReader.GetReferenceItemsAsync(token);
            var frameworkReferenceGroups = await packageContentReader.GetFrameworkItemsAsync(token);
            var contentFileGroups = await packageContentReader.GetContentItemsAsync(token);
            var buildFileGroups = await packageContentReader.GetBuildItemsAsync(token);
            var toolItemGroups = await packageContentReader.GetToolItemsAsync(token);

            var deps = (PackageDependencyInfo) NuGetProjectActions.First(x => x.PackageIdentity.Equals(packageIdentity)).PackageIdentity;
            var entry = new WorkspaceEntry(packageIdentity, GetSha(downloadResourceResult.PackageStream),
                deps.Dependencies, libItemGroups, referenceItemGroups);

            var workspace = await GetWorkspaceAsync();
            var updater = new WorkspaceWriter();
            var updated = updater.AddEntry(workspace, entry);
            await SaveWorkspaceAsync(updated);

            return await base.InstallPackageAsync(packageIdentity, downloadResourceResult, nuGetProjectContext, token);
        }

        public override async Task<bool> UninstallPackageAsync(
            PackageIdentity packageIdentity,
            INuGetProjectContext nuGetProjectContext,
            CancellationToken token)
        {
            var json = await GetJsonAsync();
            JsonConfigUtility.RemoveDependency(json, packageIdentity.Id);
            await SaveJsonAsync(json);

            var workspace = await GetWorkspaceAsync();
            var updater = new WorkspaceWriter();
            var updated = updater.RemoveEntry(workspace, packageIdentity.Id);
            await SaveWorkspaceAsync(updated);

            return await base.UninstallPackageAsync(packageIdentity, nuGetProjectContext, token);
        }

    }

}
