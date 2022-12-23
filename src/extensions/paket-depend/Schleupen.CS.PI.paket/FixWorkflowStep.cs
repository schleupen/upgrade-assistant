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
    internal class FixWorkflowStep : UpgradeStep
    {
        private readonly IProcessRunner _runner;

        private IEnumerable<UpgradeStep>? _substeps;


        public FixWorkflowStep(ILogger<FixWorkflowStep> logger, IOptions<SchleupenOptions> options, IProcessRunner runner)
            : base(logger)
        {
            _runner = runner;
        }

        public override IEnumerable<UpgradeStep> SubSteps => _substeps ?? Enumerable.Empty<UpgradeStep>();

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            WellKnownStepIds.TryConvertProjectConverterStepId,
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.FinalizeSolutionStepId,
        };

        public override string Title => "Fixes Workflow Projects in Solution";
        public override string Id => WellKnownStepIds.FixWorkflowProjectStepId;

        public override string Description => "Fixes Workflow Projects";

        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token) => Task.FromResult(context?.CurrentProject is null);

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
                    .Select(p => new ProjectFixWorkflowStep(p, Logger))
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
            Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "All Workflow projects fixed"));

        private class ProjectFixWorkflowStep : UpgradeStep
        {
            private readonly IProject _project;

            public ProjectFixWorkflowStep(IProject project, ILogger logger)
                : base(logger)
            {
                _project = project;
            }

            public override string Title => $"Fixes Workflow Project";

            public override string Description => $"Fixes Workflow Project {_project}";

            protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
            {
                var projectDir = _project?.FileInfo?.Directory;
                if (projectDir == null)
                {
                    return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Skipped, "no reference to System.Management.Automation found", BuildBreakRisk.Low));
                }

                var hasWorkflowFolder = projectDir.EnumerateDirectories("WfP*", SearchOption.AllDirectories).Any();
                var hasWorkflowReferences = _project.GetFile().References.Any(r => r.HintPath?.Contains("WfP", StringComparison.OrdinalIgnoreCase) == true);

                if (hasWorkflowFolder && hasWorkflowReferences)
                {
                    return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, "Workflow Project found. Lets fix it.", BuildBreakRisk.Low));
                }

                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Complete, "Not a Workflow Project. Good luck next time", BuildBreakRisk.Low));
            }

            protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
            {
                var projectDir = _project.FileInfo?.Directory?.FullName;
                if (projectDir == null)
                {
                    return new UpgradeStepApplyResult(UpgradeStepStatus.Skipped, "nothing to do");
                }

                var paketReferencFile = new FileInfo(Path.Combine(projectDir, "paket.references"));

                var paketReferencContent = paketReferencFile.Exists ?
                    File.ReadLines(paketReferencFile.FullName).ToList() :
                    new List<string>();

                if (!paketReferencContent.Contains("Moq"))
                {
                    paketReferencContent.Add("Moq");
                }

                if (!paketReferencContent.Contains("NUnit"))
                {
                    paketReferencContent.Add("NUnit");
                }

                if (!paketReferencContent.Contains("Schleupen.CS.PI.Framework.Testing"))
                {
                    paketReferencContent.Add("Schleupen.CS.PI.Framework.Testing");
                }

                await File.WriteAllLinesAsync(paketReferencFile.FullName, paketReferencContent, token).ConfigureAwait(false);

                _project?.GetFile().AddDefaultItemExcludes("**/WfP*/*");

                await _project.GetFile().SaveAsync(token).ConfigureAwait(false);

                return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Workflow project fixed");
            }

            protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
                => Task.FromResult(true);
        }
    }
}
