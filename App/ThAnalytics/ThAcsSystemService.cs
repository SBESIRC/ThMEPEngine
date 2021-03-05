using AcsCommon;
using AcsConnector;

namespace ThAnalytics
{
    public class ThAcsSystemService
    {
        //==============SINGLETON============
        //fourth version from:
        //http://csharpindepth.com/Articles/General/Singleton.aspx
        private static readonly ThAcsSystemService instance = new ThAcsSystemService();
        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit    
        static ThAcsSystemService() { }
        internal ThAcsSystemService() { }
        public static ThAcsSystemService Instance { get { return instance; } }
        //-------------SINGLETON-----------------

        /// <summary>
        /// 连接器
        /// </summary>
        private AppConnect AcsConnector { get; set; }

        /// <summary>
        /// 返回结果
        /// </summary>
        private NameValueString Response { get; set; }

        public void Initialize()
        {
            AcsConnector = new AppConnect();
            Response = ACSQM_GETUSER();
        }

        public void UnInitialize()
        {
            //
        }

        /// <summary>
        /// 用户ID
        /// </summary>
        public string UserId
        {
            get
            {
                if (Response.ContainsName("UserId"))
                {
                    return Response["UserId"];
                }
                return null;
            }
        }

        /// <summary>
        /// 项目ID
        /// </summary>
        public string ProjectId
        {
            get
            {
                if (Response.ContainsName("PrjId"))
                {
                    return Response["PrjId"];
                }
                return null;
            }
        }

        /// <summary>
        /// 项目编号
        /// </summary>
        public string ProjectNumber
        {
            get
            {
                if (Response.ContainsName("PrjNo"))
                {
                    return Response["PrjNo"];
                }
                return null;
            }
        }

        private NameValueString ACSQM_GETUSER()
        {
            var runArgs = new NameValueString();
            runArgs.Set("IsWait", "True");
            runArgs.Set("OnlyUserName", "False");
            runArgs.Set("FilePath", "");
            return RunCmdFunc("ACSQM_GETUSER", runArgs);
        }

        private NameValueString RunCmdFunc(string cmdFuncName, NameValueString runArgs)
        {
            NameValueString rtnArgs = new NameValueString();

            if (AcsConnector == null)
                throw new System.Exception("AcsConnect is null.");

            var app = AcsConnector.GetCurrentApp(AppInfoType.ACS.ToString(), out string errMsg);
            if (app == null)
            {
                rtnArgs.Set("Succ", "False");
                rtnArgs.Set("Error", errMsg);
                return rtnArgs;
            }

            try
            {
                runArgs.Set("RunFuncName", cmdFuncName);

                if (!runArgs.ContainsName("IsWait"))
                    runArgs.Set("IsWait", "False");

                var rtnString = AcsConnector.Invoke(app, runArgs.ToString());
                if (rtnString.StartsWith("Error:"))
                {
                    rtnArgs.Set("Succ", "False");
                    rtnArgs.Set("Error", rtnString);
                }
                else
                {
                    rtnArgs = new NameValueString(rtnString);
                    if (rtnArgs.ContainsName("Error") && !string.IsNullOrEmpty(rtnArgs["Error"]))
                    {
                        rtnArgs.Set("Succ", "False");
                        rtnArgs.Set("Error", rtnArgs["Error"]);
                    }
                }

                rtnArgs.Set("Succ", "True");
                rtnArgs.Set("Error", "");
            }
            catch (System.Exception ex)
            {
                rtnArgs.Set("Succ", "False");
                rtnArgs.Set("Error", ex.Message);
            }

            return rtnArgs;
        }
    }
}
