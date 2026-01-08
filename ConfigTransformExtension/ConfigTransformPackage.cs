using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace ConfigTransformExtension
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class ConfigTransformPackage : AsyncPackage
    {
        public const string PackageGuidString = "8a2e8b7f-9f3d-4c1e-b8a2-3d4f5e6a7b8c";

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await Commands.ApplyTransformCommand.InitializeAsync(this);
        }
    }
}
