﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.DotNet.UpgradeAssistant;
using Microsoft.DotNet.UpgradeAssistant.Extensions;

namespace Schleupen.CS.PI.Paket
{
    public class PaketExtensionServiceProvider : IExtensionServiceProvider
    {
        public void AddServices(IExtensionServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            //services.Services.AddUpgradeStep<PaketAliasFixStep>();
            services.Services.AddUpgradeStep<GacFixStep>();
            services.Services.AddUpgradeStep<AddDefaultConfigFilesStep>();
            services.Services.AddUpgradeStep<AddPaketAsToolStep>();
            services.Services.AddUpgradeStep<FixWorkflowStep>();
            services.AddExtensionOption<SchleupenOptions>("Schleupen");
        }
    }
}