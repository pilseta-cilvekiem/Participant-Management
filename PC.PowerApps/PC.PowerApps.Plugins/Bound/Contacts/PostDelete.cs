﻿using PC.PowerApps.Common.Entities.Dataverse;
using PC.PowerApps.Common.Repositories;
using PC.PowerApps.Plugins.Contexts;
using PC.PowerApps.Plugins.Enumerations;
using System;

namespace PC.PowerApps.Plugins.Bound.Contacts
{
    public class PostDelete : PluginBase
    {
        protected override void ExecuteInternal(IServiceProvider serviceProvider)
        {
            DeletePluginContext<Contact> context = new(serviceProvider, User.System, User.User);
            Contact contact = context.PreImage;

            bool synchronizeMembers = ContactRepository.IsValidForGoogleMemberGroup(contact);
            bool synchronizeSupporters = ContactRepository.IsValidForGoogleSupporterGroup(contact);
            ContactRepository.ScheduleSynchronizeGoogleParticipantGroupMembers(context, synchronizeMembers, synchronizeSupporters);
        }
    }
}
