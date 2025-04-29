using PC.PowerApps.Common;
using PC.PowerApps.Common.Entities.Dataverse;
using PC.PowerApps.Common.Extensions;
using PC.PowerApps.Common.Repositories;
using PC.PowerApps.Plugins.Contexts;
using PC.PowerApps.Plugins.Enumerations;
using System;

namespace PC.PowerApps.Plugins.Bound.ParticipationFeeRules
{
    public class PostCreateUpdate : PluginBase
    {
        protected override void ExecuteInternal(IServiceProvider serviceProvider)
        {
            PostCreateUpdatePluginContext<pc_ParticipationFeeRule> context = new(serviceProvider, User.System, User.User);
            pc_ParticipationFeeRule participationFeeRule = context.PostImage;

            if (context.GetIsAnyAttributeModified(pfr => new { pfr.pc_Amount, pfr.pc_ApplyToFirstMonth, pfr.pc_From, pfr.pc_Till }))
            {
                DateTime endOfLastMonth = context.GetCurrentOrganizationTime().GetFirstDayOfMonth().AddDays(-1);
                Period pastMonths = new(null, endOfLastMonth);
                Period prePeriodPastMonths = context.PreImage == null
                    ? null
                    : new Period(context.PreImage.pc_From, context.PreImage.pc_Till).Intersect(pastMonths);
                Period postPeriodPastMonths = new Period(participationFeeRule.pc_From, participationFeeRule.pc_Till).Intersect(pastMonths);

                if ((context.GetIsAnyAttributeModified(pfr => new { pfr.pc_Amount, pfr.pc_ApplyToFirstMonth }) && (prePeriodPastMonths != null || postPeriodPastMonths != null)) ||
                    prePeriodPastMonths != postPeriodPastMonths)
                {
                    ContactRepository.ScheduleRecalculate(context, participantTill: false, participationLevel: false, requiredParticipationFee: true);
                }
            }
        }
    }
}
