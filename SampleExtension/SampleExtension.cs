using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;

using log4net;
using Newtonsoft.Json;

using Codice.Client.IssueTracker.SampleExtension.Model;


namespace Codice.Client.IssueTracker.SampleExtension
{
    public class SampleExtension : IPlasticIssueTrackerExtension
    {
        internal const string BRANCH_PREFIX_KEY = "Branch prefix";
        internal const string USER_KEY = "User ID";

        const string POST_URL = "http://jsonplaceholder.typicode.com/posts/{0}";
        const string ALL_POSTS_URL = "http://jsonplaceholder.typicode.com/posts";
        const string POSTS_BY_USER_URL = "http://jsonplaceholder.typicode.com/posts?userId={0}";

        IssueTrackerConfiguration mConfig;

        static readonly ILog mLog = LogManager.GetLogger("sampleextension");

        internal SampleExtension(IssueTrackerConfiguration config)
        {
            mConfig = config;

            mLog.Info("Sample issue tracker is initialized");
        }

        public void Connect()
        {
            // No action needed
        }

        public void Disconnect()
        {
            // No action needed
        }

        public string GetExtensionName()
        {
            return "My awesome extension";
        }

        public List<PlasticTask> GetPendingTasks(string assignee)
        {
            int assigneeId;
            if (string.IsNullOrEmpty(assignee) || !int.TryParse(assignee, out assigneeId))
                return new List<PlasticTask>();

            return QueryServiceForTasks(string.Format(POSTS_BY_USER_URL, assignee));
        }

        public List<PlasticTask> GetPendingTasks()
        {
            return QueryServiceForTasks(ALL_POSTS_URL);
        }

        public PlasticTask GetTaskForBranch(string fullBranchName)
        {
            return LoadSingleTask(GetTaskIdFromBranchName(GetBranchName(fullBranchName)));
        }

        public Dictionary<string, PlasticTask> GetTasksForBranches(List<string> fullBranchNames)
        {
            Dictionary<string, PlasticTask> result = new Dictionary<string, PlasticTask>();
            foreach(string fullBranchName in fullBranchNames)
            {
                string taskId = GetTaskIdFromBranchName(GetBranchName(fullBranchName));
                result.Add(fullBranchName, LoadSingleTask(taskId));
            }
            return result;
        }

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

        public void LogCheckinResult(PlasticChangeset changeset, List<PlasticTask> tasks)
        {
            // Not supported
        }

        public void MarkTaskAsOpen(string taskId, string assignee)
        {
            // Not supported
        }

        public void OpenTaskExternally(string taskId)
        {
            Process.Start(string.Format(
                "https://www.google.es/search?q={0}", taskId));
        }

        public bool TestConnection(IssueTrackerConfiguration configuration)
        {
            return true;
        }

        public void UpdateLinkedTasksToChangeset(PlasticChangeset changeset, List<string> tasks)
        {
            // Not supported
        }

        PlasticTask LoadSingleTask(string taskId)
        {
            if (string.IsNullOrEmpty(taskId))
                return null;

            string uri = string.Format(POST_URL, taskId);
            string resultJson = PerformJsonRequest(uri);

            return BuildTaskFromJson(
                JsonConvert.DeserializeObject<MyServiceData>(resultJson));
        }

        string GetBranchName(string fullBranchName)
        {
            int lastSeparatorIndex = fullBranchName.LastIndexOf('/');

            if (lastSeparatorIndex < 0 )
                return fullBranchName;

            if (lastSeparatorIndex == fullBranchName.Length - 1)
                return string.Empty;

            return fullBranchName.Substring(lastSeparatorIndex + 1);
        }

        string GetTaskIdFromBranchName(string branchName)
        {
            string prefix = mConfig.GetValue(BRANCH_PREFIX_KEY);
            if (string.IsNullOrEmpty(prefix))
                return branchName;

            if (!branchName.StartsWith(branchName) || branchName == prefix)
                return string.Empty;

            return branchName.Substring(prefix.Length);
        }

        List<PlasticTask> QueryServiceForTasks(string uri)
        {
            List<PlasticTask> result = new List<PlasticTask>();
            string jsonResult = PerformJsonRequest(uri);

            MyServiceData[] deserializedServiceData =
                JsonConvert.DeserializeObject<MyServiceData[]>(jsonResult);

            foreach (MyServiceData serviceData in deserializedServiceData)
                result.Add(BuildTaskFromJson(serviceData));
            return result;
        }

        string PerformJsonRequest(string targetUri)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(targetUri);

            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                {
                    return reader.ReadToEnd();
                }
            }
            catch(Exception e)
            {
                mLog.ErrorFormat(
                    "Unable to perform request on URI {0}: {1}", targetUri, e.Message);
                mLog.DebugFormat(
                    "Stack trace:{0}{1}", Environment.NewLine, e.StackTrace);
                return string.Empty;
            }
        }

        PlasticTask BuildTaskFromJson(MyServiceData jsonData)
        {
            if (jsonData == null)
                return null;

            return new PlasticTask()
            {
                Id = jsonData.Id.ToString(),
                Owner = jsonData.UserId.ToString(),
                Title = jsonData.Title,
                Description = jsonData.Body,
                Status = "working"
            };
        }
    }
}
