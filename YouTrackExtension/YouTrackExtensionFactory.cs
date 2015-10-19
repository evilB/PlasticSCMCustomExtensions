using System;
using System.Collections.Generic;

namespace Codice.Client.IssueTracker.YouTrackExtension
{
	public class YouTrackExtensionFactory : IPlasticIssueTrackerExtensionFactory
	{
		public IssueTrackerConfiguration GetConfiguration(IssueTrackerConfiguration storedConfiguration)
		{
			var ytConfig = new YouTrackExtensionConfiguration(storedConfiguration);
			return ytConfig.GetConfiguration();
		}

		public IPlasticIssueTrackerExtension GetIssueTrackerExtension(
			IssueTrackerConfiguration configuration)
		{
			return new YoutrackExtension(configuration);
		}

		public string GetIssueTrackerName()
		{
			return "Sample Issue Tracker";
		}
	}
}
