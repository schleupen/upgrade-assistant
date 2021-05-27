﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.DotNet.UpgradeAssistant.Extensions.Default.Analyzers;

namespace Microsoft.DotNet.UpgradeAssistant.Extensions.Default.CodeFixes
{
    [ApplicableComponents(ProjectComponents.AspNetCore)]
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = "UA0003 CodeFix Provider")]
    public class ResultTypeCodeFixer : IdentifierUpgradeCodeFixer
    {
        public override string CodeFixTitle => CodeFixResources.ResultTypeTitle;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ResultTypeAnalyzer.DiagnosticId);
    }
}