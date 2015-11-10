using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using System.Web;
using log4net;
using Newtonsoft.Json;

using Codice.Client.IssueTracker.SampleExtension.Model;


namespace Codice.Client.IssueTracker.YouTrackExtension
{
	public class YoutrackExtension : IPlasticIssueTrackerExtension
	{
		private int _authRetryCount = 0;
		private string _authData;

		YouTrackExtensionConfiguration mConfig;

		private readonly ILog mLog = LogManager.GetLogger("youtrackextension");

		internal YoutrackExtension(IssueTrackerConfiguration config)
		{
			mConfig = new YouTrackExtensionConfiguration(config);

			mLog.Info("Youtrack issue tracker is initialized");
		}

		public void Connect()
		{
			Authenticate(mConfig.BaseURL, mConfig.User, mConfig.Password);
		}

		private void Authenticate(string baseURL, string user, string password)
		{
			_authRetryCount++;

			using (var client = new WebClient())
			{
				var requestURL = string.Format
					("{0}/rest/user/login?login={1}&password={2}", baseURL, user, password);
				try
				{
					var result = client.UploadString(requestURL, "POST", "");
					if (result == @"<login>ok</login>")
					{
						_authData = client.ResponseHeaders.Get("Set-Cookie");
						mLog.DebugFormat("YouTrackHandler: Successfully authenticated in {0} attempt(s).", _authRetryCount);
						_authRetryCount = 0;
					}
				}
				catch (WebException exWeb)
				{
					mLog.Error(string.Format("YouTrackHandler: Failed to authenticate using request '{0}'.", requestURL), exWeb);
				}
			}
		}

		public void Disconnect()
		{
			// No action needed
		}

		public string GetExtensionName()
		{
			return "YouTrack Extension";
		}

		public List<PlasticTask> GetPendingTasks(string assignee)
		{
			return GetUnresolvedIssues(true);
		}

		public List<PlasticTask> GetPendingTasks()
		{
			return GetUnresolvedIssues(false);
		}

		List<PlasticTask> GetUnresolvedIssues(bool userOnly)
		{
			string userFilter = userOnly?"for:me+":"";

			string stateFilter = "Unresolved";

			string projectFilter =  mConfig.BranchPrefix.Substring(0, mConfig.BranchPrefix.Length - 1);


			var filters = mConfig.IssueTypes.Select(type => "filter=" + string.Format("{0}%23{1}+%23{2}+%23{3}", userFilter, type,stateFilter , projectFilter)).ToList();

			int maxIssues = 10000;
			
			var url =string.Format("{0}/rest/issue?{1}&max={2}", mConfig.BaseURL,  string.Join("&",filters), maxIssues);

			var xml = ConnectToYoutrack(url);
			return BuildTasksFromXML(xml);
		}

		/// <summary>
		/// Function is called everytime a changeset is created. 
		/// - The commands are split using the separator regex (default {{(.*)}})
		/// - Comments are uploaded to youtrack if preferences are set
		/// </summary>
		/// <param name="changeset"></param>
		/// <param name="tasks"></param>
		public void LogCheckinResult(PlasticChangeset changeset, List<PlasticTask> tasks)
		{
			var changeComment = changeset.Comment;
			var comment = "";
			var command = "";
			// detect & handle commands
			var match = mConfig.CommandsSelector.Match(changeComment);
			if (match.Success)
			{
				changeComment = changeComment.Replace(match.Groups[0].Value, "");
				command = match.Groups[1].Value;
			}
			// add comments
			if (changeComment.Length > 0 && mConfig.PropagateComments)
			{
				comment = "Via PlasticSCM: " + changeComment;
			}

			foreach (var task in tasks)
			{
				ExecuteOnYoutrack(task.Id,command,comment);
			}
		}

		//done
		public PlasticTask GetTaskForBranch(string fullBranchName)
		{
			return LoadSingleTask(GetTaskIdFromBranchName(GetBranchName(fullBranchName)));
		}

		//done
		public Dictionary<string, PlasticTask> GetTasksForBranches(List<string> fullBranchNames)
		{
			Dictionary<string, PlasticTask> result = new Dictionary<string, PlasticTask>();
			foreach (string fullBranchName in fullBranchNames)
			{
				string taskId = GetTaskIdFromBranchName(GetBranchName(fullBranchName));
				result.Add(fullBranchName, LoadSingleTask(taskId));
			}
			return result;
		}

		//done
		public List<PlasticTask> LoadTasks(List<string> taskIds)
		{
			List<PlasticTask> result = new List<PlasticTask>();

			foreach (string taskId in taskIds)
			{
				PlasticTask loadedTask = LoadSingleTask(taskId);
				if (loadedTask == null)
					continue;
				result.Add(loadedTask);
			}

			return result;
		}

		public void MarkTaskAsOpen(string taskId, string assignee)
		{
			string command = string.Format("assignee {0} state {1}", assignee, "in progress");

			ExecuteOnYoutrack(taskId, command);
		}

		
		public void OpenTaskExternally(string taskId)
		{
			Process.Start(string.Format("{0}/issue/{1}", mConfig.BaseURL, taskId));
		}

		public bool TestConnection(IssueTrackerConfiguration configuration)
		{
			var testConfig = new YouTrackExtensionConfiguration(configuration);
			_authRetryCount = 0;
			Authenticate(testConfig.BaseURL, testConfig.User, testConfig.Password);

			var result = _authRetryCount == 0;
			//cleanup
			Authenticate(mConfig.BaseURL, mConfig.User, mConfig.Password);
			return result;
		}

		public void UpdateLinkedTasksToChangeset(PlasticChangeset changeset, List<string> tasks)
		{
			// Not supported
		}

		PlasticTask LoadSingleTask(string taskId)
		{
			if (string.IsNullOrEmpty(taskId))
				return null;

			return GetPlasticTaskFromTaskID(taskId);
		}

		string GetBranchName(string fullBranchName)
		{
			int lastSeparatorIndex = fullBranchName.LastIndexOf('/');

			if (lastSeparatorIndex < 0)
				return fullBranchName;

			if (lastSeparatorIndex == fullBranchName.Length - 1)
				return string.Empty;

			return fullBranchName.Substring(lastSeparatorIndex + 1);
		}

		string GetTaskIdFromBranchName(string branchName)
		{
			string prefix = mConfig.BranchPrefix;
			if (string.IsNullOrEmpty(prefix))
				return branchName;

			if (!branchName.StartsWith(prefix) || branchName == prefix)
				return string.Empty;

			return branchName.Split(' ').First();
		}

		//done 
		public PlasticTask GetPlasticTaskFromTaskID(string pTaskID)
		{
			mLog.DebugFormat("YouTrackHandler: GetPlasticTaskFromTaskID {0}", pTaskID);
			var result = new PlasticTask { Id = pTaskID };
			var requestURL = string.Format("{0}/rest/issue/{1}", mConfig.BaseURL, pTaskID);

			var xml = ConnectToYoutrack(requestURL);
			if (xml.Length > 0)
				result = BuildTaskFromXML(xml);

			return result;
		}


		private static string getTextFromXPathElement(XmlDocument pXMLDoc, string pFieldName)
		{
			var node = pXMLDoc.SelectSingleNode(string.Format("//field[@name='{0}']/value", pFieldName));
			return node != null ? node.InnerText : string.Empty;
		}

		private static string getTextFromXPathElement(XmlNode pXMLDoc, string pFieldName)
		{
			var node = pXMLDoc.SelectSingleNode(string.Format("//field[@name='{0}']/value", pFieldName));
			return node != null ? node.InnerText : string.Empty;
		}

		private string getBranchTitle(string pIssueType, string pIssueState, string pIssueSummary)
		{
			//if feature is disabled, return ticket summary.
			if (!mConfig.ShowIssueStateInBranchTitle)
				return pIssueSummary;

			//if feature is enabled but no states are ignored, return default format.
			if (string.IsNullOrEmpty(mConfig.IgnoreIssueStateForBranchTitle.Trim()))
				return string.Format("[{0}] {1} [{2}]",pIssueType, pIssueSummary, pIssueState);

			//otherwise, consider the ignore list.
			var ignoreStates = new ArrayList(mConfig.IgnoreIssueStateForBranchTitle.Trim().Split(','));
			return ignoreStates.Contains(pIssueState)
				? pIssueSummary
				: string.Format("{0} [{1}]", pIssueSummary, pIssueState);
		}

		private PlasticTask BuildTaskFromXML(string xmlTask)
		{
			var xmlDoc = new XmlDocument();
			xmlDoc.LoadXml(xmlTask);
			var issueState = getTextFromXPathElement(xmlDoc, "State");
			var issueType = getTextFromXPathElement(xmlDoc, "Type");
			return new PlasticTask()
			{
				Owner = getTextFromXPathElement(xmlDoc, "Assignee"),
				Status = issueState,
				Title = getBranchTitle(issueType,issueState, getTextFromXPathElement(xmlDoc, "summary")),
				Description = getTextFromXPathElement(xmlDoc, "description"),
				Id = mConfig.BranchPrefix + getTextFromXPathElement(xmlDoc, "numberInProject")
			
			};
			
		}

		private List<PlasticTask> BuildTasksFromXML(string xml)
		{
			var xmlDoc = new XmlDocument();
			xmlDoc.LoadXml(xml);
			var issues = xmlDoc.SelectNodes("//issue");
			var result = new List<PlasticTask>();
			if (issues.Count > 0)
			{
				foreach (XmlNode issue in issues)
				{
					result.Add(BuildTaskFromXML(issue.OuterXml));
				}
			}
			return result;
		}


		#region connection helpers

		private void ExecuteOnYoutrack(string taskId, string command = "", string comment = "")
		{
			var executionStrings = new List<string>();
			if (command != string.Empty)
			{
				executionStrings.Add(string.Format("command={0}", Uri.EscapeDataString(command)));
			}
			if (comment != string.Empty)
			{
				executionStrings.Add(string.Format("comment={0}", Uri.EscapeDataString(comment)));
			}

			string uri = string.Format("{0}/rest/issue/{1}/execute?{2}",
				mConfig.BaseURL,
				taskId,
				string.Join("&", executionStrings.Select(s => s)));
			ConnectToYoutrack(uri, "POST");
		}
		
		private string ConnectToYoutrack(string requestURL, string method = "GET")
		{
			{
				if(_authData == null)
					Connect();

				using (var client = new WebClient())
				{
					client.Headers.Add("Cookie", _authData);
					try
					{
						string xml = "";
						switch (method)
						{
							case "GET":
								{
									xml = client.DownloadString(requestURL);
									break;
								}
							case "POST":
								{
									xml = client.UploadString(requestURL, "");
									break;
								}
						}
						return xml;
					}
					catch (WebException exWeb)
					{
						if (exWeb.Message.Contains("Unauthorized.") && _authRetryCount < 3)
						{
							mLog.WarnFormat
								("YouTrackHandler: Failed to fetch youtrack link '{0}' due to authentication error. Will retry after authentication again. Details: {1}",
									requestURL, exWeb);
							Authenticate(mConfig.BaseURL, mConfig.User, mConfig.Password);
							return ConnectToYoutrack(requestURL, method);
						}

						mLog.WarnFormat("YouTrackHandler: Failed to find youtrack link '{0}' due to {1}", requestURL, exWeb);
						return String.Empty;
					}
				}
			}

		}
		
		#endregion
	}



}
