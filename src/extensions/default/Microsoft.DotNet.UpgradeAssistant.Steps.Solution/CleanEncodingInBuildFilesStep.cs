// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.DotNet.UpgradeAssistant.Steps.Solution
{
    internal class CleanEncodingInBuildFilesStep : UpgradeStep
    {
        private IEnumerable<FileInfo> _filesToClean;

        public CleanEncodingInBuildFilesStep(ILogger<CleanEncodingInBuildFilesStep> logger)
            : base(logger)
        {
        }

        public override string Title => "Clean Encoding from Build Files";

        public override string Description => "Removing unsupported encodings from build files";

        public override string Id => WellKnownStepIds.CleanEncodingInBuildFilesStepId;

        

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.TryConvertProjectConverterStepId
        };

        protected override Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (_filesToClean == null)
            {
                _filesToClean = new List<FileInfo>();
            }

            foreach (var fileToClean in _filesToClean)
            {
                var contents = File.ReadAllLines(fileToClean.FullName);
                if (contents.Any())
                {
                    if (contents[0].Contains("encoding=\"iso-8859-15\""))
                    {
                        contents[0] = contents[0].Replace("encoding=\"iso-8859-15\"", string.Empty);
                        File.WriteAllLines(fileToClean.FullName, contents);
                        Logger.LogInformation($"updated {fileToClean.FullName}");
                    }
                }
            }

            return Task.FromResult(new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "Updated all build files"));
        }

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            var solutionFolder = new FileInfo(context.InputPath).Directory;
            var targets = solutionFolder.EnumerateFiles("*.targets", SearchOption.AllDirectories);
            var props = solutionFolder.EnumerateFiles("*.props", SearchOption.AllDirectories);
            _filesToClean = targets.Concat(props);
            if (_filesToClean.Any())
            {
                return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, $"found {_filesToClean.Count()} files to check", BuildBreakRisk.Low));
            }

            return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Skipped, $"no files to check found", BuildBreakRisk.Low));
        }

        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token)
        {
            return Task.FromResult(context.InputIsSolution);
        }
    }
}
