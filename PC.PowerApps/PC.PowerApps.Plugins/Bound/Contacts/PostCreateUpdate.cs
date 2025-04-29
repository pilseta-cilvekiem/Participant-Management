using PC.PowerApps.Common.Entities.Dataverse;
using PC.PowerApps.Common.Repositories;
using PC.PowerApps.Plugins.Contexts;
using PC.PowerApps.Plugins.Enumerations;
using System;

namespace PC.PowerApps.Plugins.Bound.Contacts
{
    public class PostCreateUpdate : PluginBase
    {
        protected override void ExecuteInternal(IServiceProvider serviceProvider)
        {
            PostCreateUpdatePluginContext<Contact> context = new(serviceProvider, User.System, User.User);
            Contact contact = context.PostImage;

            string oldEmail = ContactRepository.GetEmail(context.PreImage);
            string newEmail = ContactRepository.GetEmail(contact);

            bool wasValidForGoogleMemberGroup = ContactRepository.IsValidForGoogleMemberGroup(context.PreImage);
            bool isValidForGoogleMemberGroup = ContactRepository.IsValidForGoogleMemberGroup(contact);
            bool synchronizeMembers =
                wasValidForGoogleMemberGroup != isValidForGoogleMemberGroup ||
                (isValidForGoogleMemberGroup && oldEmail != newEmail);

            bool wasValidForGoogleSupporterGroup = ContactRepository.IsValidForGoogleSupporterGroup(context.PreImage);
            bool isValidForGoogleSupporterGroup = ContactRepository.IsValidForGoogleSupporterGroup(contact);
            bool synchronizeSupporters =
                wasValidForGoogleSupporterGroup != isValidForGoogleSupporterGroup ||
                (isValidForGoogleSupporterGroup && oldEmail != newEmail);

            ContactRepository.ScheduleSynchronizeGoogleParticipantGroupMembers(context, synchronizeMembers, synchronizeSupporters);

            if (context.GetIsAnyAttributeModified(c => c.pc_ParticipationLevel))
            {
                ContactRepository.SendWelcomeEmail(context, contact);
            }

            if (context.GetIsAnyAttributeModified(c => c.StatusCode))
            {
                ContactRepository.ScheduleDeleteChangeHistory(context, contact);
            }
        }
    }
}
