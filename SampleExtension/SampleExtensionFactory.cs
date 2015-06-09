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

            ExtensionWorkingMode workingMode = GetWorkingMode(storedConfiguration);

            string user = GetValidParameterValue(
                storedConfiguration, SampleExtension.USER_KEY, "1");

            string prefix = GetValidParameterValue(
                storedConfiguration, SampleExtension.BRANCH_PREFIX_KEY, "scm");

            IssueTrackerConfigurationParameter userIdParam =
                new IssueTrackerConfigurationParameter()
            {
                Name = SampleExtension.USER_KEY,
                Value = GetValidParameterValue(
                    storedConfiguration, SampleExtension.USER_KEY, "1"),
                Type = IssueTrackerConfigurationParameterType.User,
                IsGlobal = false
            };
            
            IssueTrackerConfigurationParameter branchPrefixParam =
                new IssueTrackerConfigurationParameter()
            {
                Name = SampleExtension.BRANCH_PREFIX_KEY,
                Value = GetValidParameterValue(
                    storedConfiguration, SampleExtension.BRANCH_PREFIX_KEY, "sample"),
                Type = IssueTrackerConfigurationParameterType.BranchPrefix,
                IsGlobal = true
            };

            parameters.Add(userIdParam);
            parameters.Add(branchPrefixParam);

            return new IssueTrackerConfiguration(workingMode, parameters);
        }

        public IPlasticIssueTrackerExtension GetIssueTrackerExtension(
            IssueTrackerConfiguration configuration)
        {
            return new SampleExtension(configuration);
        }

        public string GetIssueTrackerName()
        {
            return "Sample Issue Tracker";
        }

        ExtensionWorkingMode GetWorkingMode(IssueTrackerConfiguration config)
        {
            if (config == null)
                return ExtensionWorkingMode.TaskOnBranch;

            if (config.WorkingMode == ExtensionWorkingMode.None)
                return ExtensionWorkingMode.TaskOnBranch;

            return config.WorkingMode;
        }

        string GetValidParameterValue(
            IssueTrackerConfiguration config, string paramName, string defaultValue)
        {
            string configValue = (config != null) ? config.GetValue(paramName) : null;
            if (string.IsNullOrEmpty(configValue))
                return defaultValue;
            return configValue;
        }
    }
}
