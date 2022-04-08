// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Schleupen.CS.PI.Paket
{
    internal class AddDefaultConfigFilesStep : UpgradeStep
    {
        #region fileContent

        private const string DirectoryBuildPropsContent = @"<Project>
    <PropertyGroup>
      <DisableTransitiveProjectReferences>true</DisableTransitiveProjectReferences>
    </PropertyGroup>
    <ItemDefinitionGroup>
        <PackageReference>
            <PrivateAssets>all</PrivateAssets>
        </PackageReference>
        <ProjectReference>
            <PrivateAssets>all</PrivateAssets>
        </ProjectReference>
    </ItemDefinitionGroup>
</Project>";

        private const string NugetConfigPlatformContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <solution>
    <add key=""disableSourceControlIntegration"" value=""true"" />
  </solution>
  <packageSources>
    <clear />
    <add key=""nuget"" value=""http://nexus.schleupen-ag.de/repository/nuget/"" />
    <add key=""schleupen"" value=""http://nexus.schleupen-ag.de/repository/SchleupenNugetPlatformDev/"" />
  </packageSources>
  <packageRestore>
     <add key=""enabled"" value=""False"" />
  </packageRestore>
</configuration>";

        private const string NugetConfigContent = @"<?xml version=""1.0"" encoding=""utf-8""?>
<configuration>
  <solution>
    <add key=""disableSourceControlIntegration"" value=""true"" />
  </solution>
  <packageSources>
    <clear />
    <add key=""nuget"" value=""http://nexus.schleupen-ag.de/repository/nuget/"" />
    <add key=""schleupen"" value=""http://nexus.schleupen-ag.de/repository/SchleupenNugetDev/"" />
  </packageSources>
  <packageRestore>
     <add key=""enabled"" value=""False"" />
  </packageRestore>
</configuration>";
        private bool _isProjectRoot;


        #endregion
        public AddDefaultConfigFilesStep(ILogger<AddDefaultConfigFilesStep> logger, IOptions<SchleupenOptions> options)
            : base(logger)
        {
            _isProjectRoot = options.Value.IsProjectRoot;
        }

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            WellKnownStepIds.TryConvertProjectConverterStepId,
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.FinalizeSolutionStepId,
        };

        public override string Title => "Fix Paket dependencies with alias";

        public override string Id => WellKnownStepIds.AddDefaultConfigFilesStepId;

        public override string Description => "Adds a PacketReference to the project";

        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token) => Task.FromResult(context?.CurrentProject is null && context?.InputIsSolution == true && _isProjectRoot);

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, "Config Files musst be added", BuildBreakRisk.Low));
        }

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var solutionFile = new FileInfo(context.InputPath);
            var solutionDirectory = solutionFile.Directory;
            File.WriteAllText(Path.Combine(solutionDirectory.FullName, "Directory.Build.props"), DirectoryBuildPropsContent);
            File.WriteAllText(Path.Combine(solutionDirectory.FullName, "NuGet.config"), NugetConfigPlatformContent);

            return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Config Files added");
        }
    }
}
