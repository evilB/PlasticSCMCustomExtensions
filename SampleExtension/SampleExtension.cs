using log4net;
using Newtonsoft.Json;
using SampleExtension.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace Codice.Client.Extension
{
    public class SampleExtension : BasePlasticExtension
    {
        protected BaseExtensionConfiguration mConfig;
        private static readonly ILog mLog = LogManager.GetLogger("extensions");

        public SampleExtension()
        {
            mConfig = new BaseExtensionConfiguration();
            mConfig.BranchPrefix = "sample";
            mConfig.SetDefaultAttributePrefix("sample");
            mConfig.WorkingMode = ExtensionWorkingMode.TaskOnChangeset;
            mBaseConfig = mConfig;

            mLog.Info("Extension initialized");
        }

        public override string GetName()
        {
            return "My awesome extension";
        }

        public override PlasticTask[] LoadTask(string[] id, string repName)
        {
            PlasticTask[] tasks = new PlasticTask[id.Length];

            for (int i = 0; i < id.Length; i++)
            {
                if (string.IsNullOrEmpty(id[i]))
                {
                    continue;
                }

                string uri = string.Format("http://jsonplaceholder.typicode.com/posts/{0}", id[i]);

                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(uri);
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                using (StreamReader reader = new StreamReader(resp.GetResponseStream()))
                {
                    string resultJson = reader.ReadToEnd();
                    var myServiceDataForTask = JsonConvert.DeserializeObject<MyServiceData>(resultJson);
                    tasks[i] = new PlasticTask()
                    {
                        Id = myServiceDataForTask.Id.ToString(),
                        Owner = myServiceDataForTask.UserId.ToString(),
                        RepName = repName,
                        Title = myServiceDataForTask.Title,
                        Description = myServiceDataForTask.Body,
                        Status = "working"
                    };
                }

            }

            return tasks;
        }

        public override void OpenTask(string id, string repName)
        {
            Process.Start(string.Format("https://www.google.es/search?q={0}+{1}", id, repName));
        }

        public override PlasticTaskConfiguration[] GetTaskConfiguration(string task)
        {
            PlasticTaskConfiguration taskConf = new PlasticTaskConfiguration();
            taskConf.TaskId = long.Parse(task);
            taskConf.PlasticPrefix = "sample";
            return new PlasticTaskConfiguration[] { taskConf };
        }
    }
}
