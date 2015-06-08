using System;
using System.Collections.Generic;

namespace Codice.Client.IssueTracker.SampleExtension
{
    public class SampleExtensionFactory : IPlasticIssueTrackerExtensionFactory
    {
        public IssueTrackerConfiguration GetConfiguration(
            IssueTrackerConfiguration storedConfiguration)
        {
            List<IssueTrackerConfigurationParameter> parameters
                = new List<IssueTrackerConfigurationParameter>();

            ExtensionWorkingMode workingMode = storedConfiguration.WorkingMode;
            if (workingMode == ExtensionWorkingMode.None)
                workingMode = ExtensionWorkingMode.TaskOnBranch;

            string user = storedConfiguration.GetValue(SampleExtension.USER_KEY);
            if (string.IsNullOrEmpty(user))
                user = "1";

            string prefix = storedConfiguration.GetValue(SampleExtension.BRANCH_PREFIX_KEY);
            if (string.IsNullOrEmpty(prefix))
                prefix = "scm";

            IssueTrackerConfigurationParameter userIdParam =
                new IssueTrackerConfigurationParameter()
            {
                Name = SampleExtension.USER_KEY,
                Value = user,
                Type = IssueTrackerConfigurationParameterType.User,
                IsGlobal = false
            };
            
            IssueTrackerConfigurationParameter branchPrefixParam =
                new IssueTrackerConfigurationParameter()
            {
                Name = SampleExtension.USER_KEY,
                Value = storedConfiguration.GetValue(SampleExtension.USER_KEY),
                Type = IssueTrackerConfigurationParameterType.User,
                IsGlobal = false
            };

            return new IssueTrackerConfiguration(workingMode, parameters);
        }

        public IPlasticIssueTrackerExtension GetIssueTrackerExtension(
            IssueTrackerConfiguration configuration)
        {
            return new SampleExtension(configuration);
        }

        public string GetIssueTrackerName()
        {
            return "Sample extension";
        }
    }
}
