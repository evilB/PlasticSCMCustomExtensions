using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Codice.Client.IssueTracker.YouTrackExtension
{
	class YouTrackExtensionConfiguration
	{
		#region key definitions

		private const string HostKey = "Host";
		private const string HostDefault = "myYoutrackHost/youtrack";
		private const IssueTrackerConfigurationParameterType HostType = IssueTrackerConfigurationParameterType.Host;
		private const string UseSslKey = "Use SSL";
		private const string UseSslDefault = "False";
		private const IssueTrackerConfigurationParameterType UseSslType = IssueTrackerConfigurationParameterType.Boolean;
		private const string PortNumberKey = "Port";
		private const string PortNumberDefault = "";
		private const IssueTrackerConfigurationParameterType PortNumberType = IssueTrackerConfigurationParameterType.Text;
		private const string UserKey = "User ID";
		private const string UserDefault = "username";
		private const IssueTrackerConfigurationParameterType UserType = IssueTrackerConfigurationParameterType.User;
		private const string PasswordKey = "Password";
		private const string PasswordDefault = "password";
		private const IssueTrackerConfigurationParameterType PasswordType = IssueTrackerConfigurationParameterType.Text;
		private const string BranchPrefixKey = "Branch prefix";
		private const string BranchPrefixDefault = "yt-";
		private const IssueTrackerConfigurationParameterType BranchPrefixType = IssueTrackerConfigurationParameterType.Text;
		private const string IssueTypeKey = "Branch issue type filter";
		private const string IssueTypeDefault = "Feature Bug Task Cosmetics {Meta Issue} {Performance Problem} {Usability Problem}";
		private const IssueTrackerConfigurationParameterType IssueTypeType = IssueTrackerConfigurationParameterType.Text;
		private const string PropagateCommentsKey = "Add checkin comments to youtrack task";
		private const string PropagateCommentsDefault = "True";
		private const IssueTrackerConfigurationParameterType PropagateCommentsType = IssueTrackerConfigurationParameterType.Boolean;
		private const string CommandsViaCommentsKey = "Regex selector for youtrack (match 1 will be used for selection)";
		private const string CommandsViaCommentsDefault = "{{(.*)}}";
		private const IssueTrackerConfigurationParameterType CommandsViaCommentsType = IssueTrackerConfigurationParameterType.Text;

		#endregion

		private IssueTrackerConfiguration storedConfiguration;

		#region accessors

		public string User
		{
			get
			{
				return GetValidParameterValue(storedConfiguration, UserKey, UserDefault);
			}
		}

		private bool UseSSL
		{
			get
			{
				return GetValidParameterValue(storedConfiguration, UseSslKey,
					UseSslDefault) != UseSslDefault;
			}
		}

		private int Port
		{
			get
			{
				var portString = GetValidParameterValue(storedConfiguration, PortNumberKey,
					PortNumberDefault);
				int portInt;
				var hasPortInt = Int32.TryParse(portString, out portInt);
				if (hasPortInt)
					return portInt;
				return UseSSL ? 443 : 80;
			}
		}

		private string Protocol
		{
			get { return UseSSL ? "https" : "http"; }
		}

		private string Host
		{
			get
			{
				return GetValidParameterValue(storedConfiguration, HostKey, HostDefault).Split('/').First().Split(':').First();
			}
		}

		private string Subfolder
		{
			get
			{
				var pathParts = GetValidParameterValue(storedConfiguration, HostKey, HostDefault).Split('/').ToList();
				pathParts.RemoveAt(0);
				if (pathParts.Count > 0)
					return "/" + String.Join("/", pathParts);
				return "";
			}

		}

		public string BaseURL
		{
			get
			{
				return Protocol + "://" + Host + ":" + Port + Subfolder;
			}
		}

		public string Password
		{
			get
			{
				return GetValidParameterValue(storedConfiguration, PasswordKey, PasswordDefault);
			}
		}

		public string BranchPrefix
		{
			get
			{
				return GetValidParameterValue(storedConfiguration, BranchPrefixKey, BranchPrefixDefault);
			}
		}

		public bool PropagateComments
		{
			get
			{
				return GetValidParameterValue(storedConfiguration, PropagateCommentsKey,
					PropagateCommentsDefault) == PropagateCommentsDefault;
			}
		}

		private List<string> issueTypes;
		public List<string> IssueTypes
		{
			get
			{
				if (issueTypes == null)
				{
					issueTypes = new List<string>();
					
					var pattern =  @"(?:{(.*?)}|(\S+))";
					var rest = Regex.Matches(GetValidParameterValue(storedConfiguration, IssueTypeKey, IssueTypeDefault),pattern);
					for (var i =0;i<rest.Count;i++)
					{
						issueTypes.Add(rest[i].Value);	
					}

				}
				return issueTypes;
			}
		}

		private Regex commandSelector;
		public Regex CommandsSelector
		{
			get
			{
				if (commandSelector == null)
				{
					var regexString = GetValidParameterValue(storedConfiguration, CommandsViaCommentsKey, CommandsViaCommentsDefault);
					commandSelector = new Regex(regexString);
				}
				return commandSelector;
			}
		}

		public bool ShowIssueStateInBranchTitle = true;
		public string IgnoreIssueStateForBranchTitle = "";

		#endregion

		public YouTrackExtensionConfiguration(IssueTrackerConfiguration config)
		{
			storedConfiguration = config;
		}

		public IssueTrackerConfiguration GetConfiguration()
		{
			List<IssueTrackerConfigurationParameter> parameters
				= new List<IssueTrackerConfigurationParameter>();

			ExtensionWorkingMode workingMode = GetWorkingMode(storedConfiguration);

			IssueTrackerConfigurationParameter hostParam =
				new IssueTrackerConfigurationParameter()
				{
					Name = HostKey,
					Value = GetValidParameterValue(storedConfiguration, HostKey, HostDefault),
					Type = HostType,
					IsGlobal = false
				};

			IssueTrackerConfigurationParameter portParam =
				new IssueTrackerConfigurationParameter()
				{
					Name = PortNumberKey,
					Value = GetValidParameterValue(storedConfiguration, PortNumberKey, PortNumberDefault),
					Type = PortNumberType,
					IsGlobal = false
				};
			IssueTrackerConfigurationParameter sslParam =
				new IssueTrackerConfigurationParameter()
				{
					Name = UseSslKey,
					Value = GetValidParameterValue(storedConfiguration, UseSslKey, UseSslDefault),
					Type = UseSslType,
					IsGlobal = false
				};
			IssueTrackerConfigurationParameter userIdParam =
				new IssueTrackerConfigurationParameter()
				{
					Name = UserKey,
					Value = GetValidParameterValue(storedConfiguration, UserKey, UserDefault),
					Type = UserType,
					IsGlobal = false
				};

			IssueTrackerConfigurationParameter passwordParam =
				new IssueTrackerConfigurationParameter()
				{
					Name = PasswordKey,
					Value = GetValidParameterValue(storedConfiguration, PasswordKey, PasswordDefault),
					Type = PasswordType,
					IsGlobal = false
				};


			IssueTrackerConfigurationParameter branchPrefixParam =
				new IssueTrackerConfigurationParameter()
				{
					Name = BranchPrefixKey,
					Value = GetValidParameterValue(storedConfiguration, BranchPrefixKey, BranchPrefixDefault),
					Type = BranchPrefixType,
					IsGlobal = false
				};

			IssueTrackerConfigurationParameter propagateCommentsParam = new IssueTrackerConfigurationParameter()
			{
				Name = PropagateCommentsKey,
				Value = GetValidParameterValue(storedConfiguration, PropagateCommentsKey, PropagateCommentsDefault),
				Type = PropagateCommentsType,
				IsGlobal = false
			};

			IssueTrackerConfigurationParameter IssueTypeSelectionParam = new IssueTrackerConfigurationParameter()
			{
				Name = IssueTypeKey,
				Value = GetValidParameterValue(storedConfiguration, IssueTypeKey, IssueTypeDefault),
				Type = IssueTypeType,
				IsGlobal = false
			};

			IssueTrackerConfigurationParameter RegexSelectorParam = new IssueTrackerConfigurationParameter()
			{
				Name = CommandsViaCommentsKey,
				Value = GetValidParameterValue(storedConfiguration, CommandsViaCommentsKey, CommandsViaCommentsDefault),
				Type = CommandsViaCommentsType,
				IsGlobal = false
			};

			parameters.Add(hostParam);
			parameters.Add(portParam);
			parameters.Add(userIdParam);
			parameters.Add(passwordParam);
			parameters.Add(branchPrefixParam);
			parameters.Add(propagateCommentsParam);
			parameters.Add(sslParam);
			parameters.Add(IssueTypeSelectionParam);
			parameters.Add(RegexSelectorParam);

			return new IssueTrackerConfiguration(workingMode, parameters);

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
			if (String.IsNullOrEmpty(configValue))
				return defaultValue;
			return configValue;
		}
	}
}
