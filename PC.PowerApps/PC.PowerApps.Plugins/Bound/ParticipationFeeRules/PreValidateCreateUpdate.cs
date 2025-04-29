using PC.PowerApps.Common;
using PC.PowerApps.Common.Entities.Dataverse;
using PC.PowerApps.Common.Extensions;
using PC.PowerApps.Common.Repositories;
using PC.PowerApps.Plugins.Contexts;
using PC.PowerApps.Plugins.Enumerations;
using System;

namespace PC.PowerApps.Plugins.Bound.ParticipationFeeRules
{
    public class PreValidateCreateUpdate : PluginBase
    {
        protected override void ExecuteInternal(IServiceProvider serviceProvider)
        {
            PreCreateUpdatePluginContext<pc_ParticipationFeeRule> context = new(serviceProvider, User.System, User.User);

            if (context.IsValidationDisabled)
            {
                return;
            }

            context.EnsureAttributesNotModified(pfr => pfr.pc_Name);

            pc_ParticipationFeeRule participationFeeRule = context.PostImage;

            if (participationFeeRule.StatusCode != pc_ParticipationFeeRule_StatusCode.Active)
            {
                context.EnsureAttributesNotModified(pfr => pfr.StatusCode);
            }

            context.EnsureCreatedOrUpdatedAttributesNotEmpty(pfr => new { pfr.pc_Amount, pfr.pc_ApplyToFirstMonth, pfr.pc_From });

            if (context.GetIsAnyAttributeModified(pfr => pfr.pc_From) && participationFeeRule.pc_From.Value.Day != 1)
            {
                throw context.CreateException(nameof(Resource.ParticipationFeeRuleFromNotFirstDayOfMonth));
            }

            if (context.GetIsAnyAttributeModified(pfr => pfr.pc_Till) && participationFeeRule.pc_Till != null && !participationFeeRule.pc_Till.Value.IsLastDayOfMonth())
            {
                throw context.CreateException(nameof(Resource.ParticipationFeeRuleTillNotLastDayOfMonth));
            }

            if (context.GetIsAnyAttributeModified(pfr => new { pfr.pc_From, pfr.pc_Till }) && participationFeeRule.pc_From > participationFeeRule.pc_Till)
            {
                throw context.CreateException(nameof(Resource.ParticipationFeeRuleFromGreaterThanTill));
            }

            if (context.GetIsAnyAttributeModified(pfr => new { pfr.pc_From, pfr.pc_Till }))
            {
                pc_ParticipationFeeRule otherParticipationFeeRule = ParticipationFeeRuleRepository.GetParticipationFeeRuleWithinSamePeriod(context, participationFeeRule);

                if (otherParticipationFeeRule != null)
                {
                    throw context.CreateException(nameof(Resource.AnotherParticipationFeeRule));
                }
            }
        }
    }
}
