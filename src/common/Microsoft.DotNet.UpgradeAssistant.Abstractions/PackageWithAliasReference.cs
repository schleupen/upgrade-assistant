﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.DotNet.UpgradeAssistant
{
    public record PackageWithAliasReference(string Name, string Alias)
    {
        public override string ToString()
        {
            return $"{Name}, Alias={Alias}";
        }
    }
}
