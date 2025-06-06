﻿using Microsoft.Xrm.Sdk;
using PC.PowerApps.Common.Extensions;
using PC.PowerApps.Plugins.Enumerations;
using System;
using System.Linq;

namespace PC.PowerApps.Plugins.Contexts
{
    internal class DeletePluginContext<TEntity> : EntityPluginContext<TEntity> where TEntity : Entity
    {
        public EntityReference Target => (EntityReference)PluginExecutionContext.InputParameters
            .Where(ip => ip.Key == PluginConstants.TargetAttributeName)
            .TakeSingle()
            .Value;

        public DeletePluginContext(IServiceProvider serviceProvider, User organizationServiceUser, User user) : base(serviceProvider, organizationServiceUser, user)
        {
        }
    }
}
