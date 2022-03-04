// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant;
using Microsoft.Extensions.Logging;

namespace Schleupen.CS.PI.Paket
{
    internal class PaketAliasFixStep : UpgradeStep
    {
        private IEnumerable<UpgradeStep>? _substeps;
        public PaketAliasFixStep(ILogger<PaketAliasFixStep> logger)
            : base(logger)
        {

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

        public override string Description => "Adds a PacketReference to the project";

        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token) => Task.FromResult(context?.CurrentProject is null);

        public override IEnumerable<UpgradeStep> SubSteps => _substeps ?? Enumerable.Empty<UpgradeStep>();

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_substeps is null)
            {
                var all = context.Projects
                    .Where(p => p.GetFile().IsSdk)
                    .Select(p => new ProjectAliasFixStep(p, Logger))
                    .ToList();

                _substeps = all;

                if (!all.Any())
                {
                    return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "ALl projects are already SDK style.", BuildBreakRisk.None));
                }
                else
                {
                    return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, "Some projects need to be converted to SDK style.", BuildBreakRisk.High));
                }
            }
            else
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "Projects have already been converted to SDK style projects.", BuildBreakRisk.None));
            }
        }

        protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token) => 
            Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "All projects in solution has correct alias"));

        private class ProjectAliasFixStep : UpgradeStep
        {
            private readonly IProject _project;
            private string[] _referenceLines;

            public ProjectAliasFixStep(IProject project, ILogger logger)
                : base(logger)
            {
                _project = project;
            }

            public override string Title => $"Update paket alias";

            public override string Description => $"add paketreference to project file";

            protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
            {

                var aliasLines = _referenceLines.Where(x => x.Contains("alias "));
                    var packages = new List<PackageWithAliasReference>();
                    foreach (var aliasLine in aliasLines)
                    {
                        var alias = aliasLine.Split(new char[] {' '}, StringSplitOptions.RemoveEmptyEntries)[2];
                        var index = Array.IndexOf(_referenceLines, aliasLine);
                        var reference = _referenceLines[index - 1];
                        if (_project.PackageReferences.Any(x => x.Name == reference))
                        {
                            continue;
                        }

                        var package = new PackageWithAliasReference(reference, alias);
                        packages.Add(package);
                    }

                    _project.GetFile().AddPackagesWithAlias(packages);
                 await _project.GetFile().SaveAsync(token);

                return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Alias reference added");

            }

            protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
            {
                var projectDir = _project.FileInfo.Directory.FullName;
                var paketReferencFile = new FileInfo(Path.Combine(projectDir, "paket.references"));
                if (paketReferencFile.Exists)
                {
                    _referenceLines = File.ReadAllLines(paketReferencFile.FullName);
                    if(_referenceLines.Any(x => x.Contains("alias ")))
                    {
                        return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, "alias found", BuildBreakRisk.Low));
                    }
                    return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "alias not found", BuildBreakRisk.Low));
                }
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "paket.reference not found", BuildBreakRisk.Low));
            }

            protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
                => Task.FromResult(true);
        }
    }
}
