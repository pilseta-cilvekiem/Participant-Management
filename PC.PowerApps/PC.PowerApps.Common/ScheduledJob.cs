using Newtonsoft.Json;
using PC.PowerApps.Common.Entities.Dataverse;
using PC.PowerApps.Common.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PC.PowerApps.Common
{
    public abstract class ScheduledJob
    {
        [JsonIgnore]
        public Context Context { get; set; }

        public abstract Task Execute();

        public void Schedule(bool allowDuplicates)
        {
            string name = GetType().Name;
            string parameters = JsonConvert.SerializeObject(this);
            pc_ScheduledJob scheduledJob;

            if (!allowDuplicates)
            {
                DateTime utcNow = DateTime.UtcNow;
                scheduledJob = Context.ServiceContext.pc_ScheduledJobSet
                    .Where(sj =>
                        sj.pc_Name == name &&
                        sj.pc_Parameters == parameters &&
                        (sj.StatusCode == pc_ScheduledJob_StatusCode.Pending || sj.StatusCode == pc_ScheduledJob_StatusCode.Failed) &&
                        sj.pc_ExecuteOn <= utcNow &&
                        (sj.pc_PostponeUntil == null || sj.pc_PostponeUntil <= utcNow))
                    .FirstOrDefault();

                if (scheduledJob != null)
                {
                    return;
                }
            }

            scheduledJob = new pc_ScheduledJob
            {
                pc_Name = name,
                pc_Parameters = parameters,
            };
            Context.OrganizationService.CreateWithoutNulls(scheduledJob);
        }
    }
}
