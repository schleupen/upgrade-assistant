﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace MSBuild.Conversion.Package
{
    public class PackagesConfigPackage
    {
        /// <summary>
        /// Gets or sets name of the package.
        /// </summary>
        public string? ID { get; set; }

        /// <summary>
        /// Gets or sets exact version of the package depended upon.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets optional TFM that the package dependency applies to.
        /// </summary>
        public string? TargetFramework { get; set; }

        /// <summary>
        /// Gets or sets optional string of allowed versions that follow the NuGet spec for syntax.
        /// </summary>
        public string? AllowedVersions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether optional flag for use only in development; the package will not be included when a consuming package is created.
        /// </summary>
        public bool DevelopmentDependency { get; set; } = false;
    }
}
