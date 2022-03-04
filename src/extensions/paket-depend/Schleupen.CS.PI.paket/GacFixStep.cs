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

namespace Schleupen.CS.PI.paket
{
    internal class GacFixStep : UpgradeStep
    {
        private IEnumerable<UpgradeStep>? _substeps;
        public GacFixStep(ILogger<GacFixStep> logger)
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

        public override string Title => "Moves references from gac to paket";

        public override string Description => "Moves assembly references from gac to paket";

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
                    .Select(p => new ProjectGacFixStep(p, Logger))
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

        private class ProjectGacFixStep : UpgradeStep
        {
            private readonly IProject _project;
            private string[] _referenceLines;

            public ProjectGacFixStep(IProject project, ILogger logger)
                : base(logger)
            {
                _project = project;
            }

            public override string Title => $"Moves references from gac to paket";

            public override string Description => $"Moves references from gac to paket in {_project}";

            protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
            {
                var projectDir = _project.FileInfo.Directory.FullName;
                var paketReferencFile = new FileInfo(Path.Combine(projectDir, "paket.references"));
                if (!paketReferencFile.Exists)
                {
                    return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Skipped, "paket.reference not found", BuildBreakRisk.Low));
                }

                if(_project.References.Any(r => r.Name.Equals("System.Management.Automation", StringComparison.OrdinalIgnoreCase)))
                {
                    return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"found reference to System.Management.Automation", BuildBreakRisk.Low));
                }

                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Skipped, "no reference to System.Management.Automation found", BuildBreakRisk.Low));
            }

            protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
            {
                var projectDir = _project.FileInfo.Directory.FullName;
                var paketReferencFile = new FileInfo(Path.Combine(projectDir, "paket.references"));

                var reference = _project.References.Where(r => r.Name.Equals("System.Management.Automation", StringComparison.OrdinalIgnoreCase));

                if (reference.Any())
                {
                    _project.GetFile().RemoveReferences(reference);

                    var paketContents = (await File.ReadAllLinesAsync(paketReferencFile.FullName)).ToList();
                    if (!paketContents.Any(x => x.Equals("System.Management.Automation", StringComparison.OrdinalIgnoreCase)))
                    {
                        paketContents.Add("System.Management.Automation");
                        await File.WriteAllLinesAsync(paketReferencFile.FullName, paketContents);
                        await _project.GetFile().SaveAsync(token);
                        return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "reference moved");
                    }
                    
                }

                return new UpgradeStepApplyResult(UpgradeStepStatus.Skipped, "no reference moved");
            }

            protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
                => Task.FromResult(true);
        }
    }
}
