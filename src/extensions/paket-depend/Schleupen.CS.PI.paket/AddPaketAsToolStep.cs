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
    internal class AddPaketAsToolStep : UpgradeStep
    {
        private readonly bool _isProjectRoot;
        private readonly string _paketVersion;
        private readonly IProcessRunner _runner;


        public AddPaketAsToolStep(ILogger<AddPaketAsToolStep> logger, IOptions<SchleupenOptions> options, IProcessRunner runner)
            : base(logger)
        {
            _isProjectRoot = options.Value.IsProjectRoot;
            _paketVersion = options.Value.PaketVersion;
            _runner = runner;
        }

        public override IEnumerable<string> DependsOn { get; } = new[]
        {
            WellKnownStepIds.AddDefaultConfigFilesStepId,
        };

        public override IEnumerable<string> DependencyOf { get; } = new[]
        {
            WellKnownStepIds.FinalizeSolutionStepId,
        };

        public override string Title => "Adds paket as dotnet tool";
        public override string Id => WellKnownStepIds.AddPaketAsToolStepId;

        public override string Description => "Adds paket as dotnet tool";

        protected override Task<bool> IsApplicableImplAsync(IUpgradeContext context, CancellationToken token) => Task.FromResult(context?.CurrentProject is null && context?.InputIsSolution == true);

        protected override Task<UpgradeStepInitializeResult> InitializeImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            return Task.FromResult(new UpgradeStepInitializeResult(UpgradeStepStatus.Incomplete, "Paket tool must be installed", BuildBreakRisk.Low));
        }

        protected override async Task<UpgradeStepApplyResult> ApplyImplAsync(IUpgradeContext context, CancellationToken token)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var solutionDirectory = new FileInfo(context.InputPath).Directory;

            if (_isProjectRoot)
            {
                await _runner.RunProcessAsync(new ProcessInfo
                {
                    WorkingDirectory = solutionDirectory.FullName,
                    Command = "dotnet",
                    Arguments = $"new tool-manifest",
                    EnvironmentVariables = context.GlobalProperties,
                    Name = "dotnet new tool-manifest",

                    GetMessageLogLevel = (_, message) => LogLevel.Information,
                }, token).ConfigureAwait(false);

                var versionArgument = !string.IsNullOrWhiteSpace(_paketVersion) ? $"--version {_paketVersion}" : string.Empty;
                await _runner.RunProcessAsync(new ProcessInfo
                {
                    WorkingDirectory = solutionDirectory.FullName,
                    Command = "dotnet",
                    Arguments = $"tool install paket {versionArgument}",
                    EnvironmentVariables = context.GlobalProperties,
                    Name = "dotnet add schleupen paket version",

                    GetMessageLogLevel = (_, message) => LogLevel.Information,
                }, token).ConfigureAwait(false);

                await _runner.RunProcessAsync(new ProcessInfo
                {
                    WorkingDirectory = solutionDirectory.FullName,
                    Command = "dotnet",
                    Arguments = $"tool restore",
                    EnvironmentVariables = context.GlobalProperties,
                    Name = "dotnet tool restore",

                    GetMessageLogLevel = (_, message) => LogLevel.Information,
                }, token).ConfigureAwait(false);

                await _runner.RunProcessAsync(new ProcessInfo
                {
                    WorkingDirectory = solutionDirectory.FullName,
                    Command = "dotnet",
                    Arguments = $"paket install",
                    EnvironmentVariables = context.GlobalProperties,
                    Name = "dotnet paket install",

                    GetMessageLogLevel = (_, message) => LogLevel.Information,
                }, token).ConfigureAwait(false);
            }

            await _runner.RunProcessAsync(new ProcessInfo
            {
                WorkingDirectory = solutionDirectory.FullName,
                Command = "dotnet",
                Arguments = $"paket restore",
                EnvironmentVariables = context.GlobalProperties,
                Name = "dotnet paket restore",

                GetMessageLogLevel = (_, message) => LogLevel.Information,
            }, token).ConfigureAwait(false);

            await _runner.RunProcessAsync(new ProcessInfo
            {
                WorkingDirectory = solutionDirectory.FullName,
                Command = "dotnet",
                Arguments = $"restore {context.InputPath}",
                EnvironmentVariables = context.GlobalProperties,
                Name = "dotnet restore solution",

                GetMessageLogLevel = (_, message) => LogLevel.Information,
            }, token).ConfigureAwait(false);



            return new UpgradeStepApplyResult(UpgradeStepStatus.Complete, "paket restore ausgeführt");
        }
    }
}
