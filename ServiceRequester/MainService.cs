using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using Service.RNG;

namespace ServiceRequester
{

    class Reqdata
    {
        public string eventstr;

    }

    class Data
    {
        public bool isnext;
        public List<datareq> data;

    }

    public struct datareq
    {
        public string uid, method,paramsstr;
        public bool compress,debug,json;

        public datareq(string uid1, string method1, string paramsstr1, bool compress1, bool debug1, bool json1)
        {
            uid = uid1;
            method = method1;
            paramsstr = paramsstr1;
            compress = compress1;
            debug = debug1;
            json = json1;
        }
    }

    public class Requester
    {
        public static string WSRNG;
        public static string WSTrain;
        public static string frequencyMiliSeconds;
        public static string loginRNG;
        public static string passRNG;
        public static string loginTrain;
        public static string passTrain;
        public static string log;
        public static bool KeepLogging;
        public static int freq;

        public Requester(string WSRNGt, string WSTraint, string frequencyMiliSecondst, string loginRNGt, string passRNGt, string loginTraint, string passTraint, string logt, bool KeepLoggingt)
        {
            WSRNG = WSRNGt;
            WSTrain = WSTraint;
            frequencyMiliSeconds = frequencyMiliSecondst;
            loginRNG = loginRNGt;
            passRNG = passRNGt;
            loginTrain = loginTraint;
            passTrain = passTraint;
            log = logt;
            KeepLogging = KeepLoggingt;
    }

        public void threadproc()
        {
            Data data = new Data();
            //Reqdata reqdata = new Reqdata();
            //reqdata.eventstr = "GetNewRequest";
            //var json = JsonConvert.SerializeObject(reqdata);
            try
            {


                string responseToString;
                responseToString = "";
                var response = PostMethod("{\"event\":\"GetNewRequest\"}", WSTrain, "application/json", loginTrain, passTrain);
                if (response != null)
                {
                    var strreader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                    responseToString = strreader.ReadToEnd();
                }

                //responseToString = responseToString.Replace("params", "paramsstr");
                data = JsonConvert.DeserializeObject<Data>(responseToString);
                if (data.data.Count > 0)
                {
                    string result;
                    Service.RNG.DataExchange n = new DataExchange();
                    n.UseDefaultCredentials = true;
                    n.Credentials = new NetworkCredential(loginRNG, passRNG);
                    result = n.ВыполнитьАлгоритмИПолучитьРезультат("ПаровозикиФоновыеЗадания", responseToString,
                        false,
                        false, true);
                    if (KeepLogging == true)
                    {
                        DateTime localDate = DateTime.Now;
                        StreamWriter file = Getlog(log);
                        file.WriteLine(localDate.ToString(new CultureInfo("ru-RU")) + "----на передано в RNG запросов " + data.data.Count + ". Результат - " + Convert.ToString(result));
                        file.Close();
                    }

                    freq = Convert.ToInt32(frequencyMiliSeconds) / 2;
                }
                else
                {
                    freq = Convert.ToInt32(frequencyMiliSeconds);
                }
            }
            catch (Exception ex)
            {
                if (KeepLogging == true)
                {
                    DateTime localDate = DateTime.Now;
                    StreamWriter file = Getlog(log);
                    if (data.data != null)
                    {
                        file.WriteLine(localDate.ToString(new CultureInfo("ru-RU")) + "----на передачу в RNG" + data.data.Count + ".Ошибка - " + ex.ToString());
                    }
                    else
                    {
                        file.WriteLine(localDate.ToString(new CultureInfo("ru-RU")) + "----на передачу в RNG.Ошибка - " + ex.ToString());
                    }
                    file.Close();
                }
            }



        }

        public void RequestProc()
        {
            freq = Convert.ToInt32(frequencyMiliSeconds);
            while (true)
            {
                Thread subrequesterproc = new Thread(new ThreadStart(threadproc));
                subrequesterproc.Start();

                Thread.Sleep(freq);
            }

        }

        public static HttpWebResponse PostMethod(string postedData, string postUrl, string contentType, string login, string pass)
        {
            Uri myUri = new Uri(postUrl);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(myUri);
            request.Method = "POST";
            String encoded = System.Convert.ToBase64String(System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(login + ":" + pass));
            request.Headers.Add("Authorization", "Basic " + encoded);
            NetworkCredential myNetworkCredential = new NetworkCredential(login, pass);
            CredentialCache myCredentialCache = new CredentialCache();
            myCredentialCache.Add(myUri, "Basic", myNetworkCredential);
            CookieContainer myContainer = new CookieContainer();
            request.CookieContainer = myContainer;
            request.Credentials = myCredentialCache;
            request.PreAuthenticate = true;

            UTF8Encoding encoding = new UTF8Encoding();
            var bytes = encoding.GetBytes(postedData);

            request.ContentType = contentType;
            request.ContentLength = bytes.Length;

            using (var newStream = request.GetRequestStream())
            {
                newStream.Write(bytes, 0, bytes.Length);
                newStream.Close();
            }
            return (HttpWebResponse)request.GetResponse();
        }

        private static StreamWriter Getlog(string fileName)
        {
            if (File.Exists(fileName))
            {
                StreamWriter sw = new StreamWriter(new FileStream(fileName, FileMode.Append, FileAccess.Write));
                return sw;
            }
            else
            {
                StreamWriter sw = new StreamWriter(new FileStream(fileName, FileMode.Create, FileAccess.Write));
                return sw;

            }
        }



    }

    public partial class MainService : ServiceBase
    {
        public bool stopbit;
        public static StConfig st;
        static Requester requester;
        Thread requesterproc;
        public struct StConfig
        {
            public string WSRNG, WSTrain, frequencyMiliSeconds, loginRNG, passRNG, loginTrain, passTrain, log;
            public bool KeepLogging;

            public StConfig(string p1, string p2, string IP2, string lRNG,string pRNG,
                                string logTrain,string pTrain, string plog, bool pKeepLogging)
            {
                WSRNG = p1;
                WSTrain = p2;
                frequencyMiliSeconds = IP2;
                loginRNG = lRNG;
                passRNG = pRNG;
                loginTrain = logTrain;
                passTrain = pTrain;
                log = plog;
                KeepLogging = pKeepLogging;
            }
        }

        public MainService(string[] args)
        {
            st = readconfig(args[0], "configRest", "WSRNG");
            InitializeComponent();
            requester = new Requester(st.WSRNG, st.WSTrain, st.frequencyMiliSeconds, st.loginRNG, st.passRNG, st.loginTrain, st.passTrain, st.log, st.KeepLogging);
            requesterproc = new Thread(new ThreadStart(requester.RequestProc));
        }

        public MainService()
        {
            st = readconfig("config.xml", "config", "WSRNG");
            InitializeComponent();
            requester = new Requester(st.WSRNG, st.WSTrain, st.frequencyMiliSeconds, st.loginRNG, st.passRNG, st.loginTrain, st.passTrain, st.log, st.KeepLogging);
            requesterproc = new Thread(new ThreadStart(requester.RequestProc));


        }

        protected override void OnStart(string[] args)
        {
            requesterproc.Start();
        }

        protected override void OnStop()
        {
            requesterproc.Abort();
        }

        protected override void OnPause()
        {
            requesterproc.Abort();
        }

        protected override void OnContinue()
        {
            string[] args = { "", "" };
            OnStart(args);
        }

    

        public static StConfig readconfig(string pathtoXML, string RootElement, string Node)
        {
            StConfig cf = new StConfig();
            // XElement el = new XElement();
            using (XmlReader reader = XmlReader.Create(pathtoXML))
            {
                while (reader.Read())
                {
                    if (reader.Name=="WSRNG")
                    {
                        reader.Read();
                        if(reader.Value.IndexOf('\n') ==-1)
                            cf.WSRNG = reader.Value;
                    }
                      else if (reader.Name=="WSTrain")
                        {
                            reader.Read();
                            if (reader.Value.IndexOf('\n') == -1)
                                cf.WSTrain = reader.Value;
                        }
                    else if (reader.Name == "frequencyMiliSeconds")
                    {
                        reader.Read();
                        if (reader.Value.IndexOf('\n') == -1)
                            cf.frequencyMiliSeconds = reader.Value;
                    }
                    else if (reader.Name == "loginRNG")
                    {
                        reader.Read();
                        if (reader.Value.IndexOf('\n') == -1)
                            cf.loginRNG = reader.Value;
                    }
                    else if (reader.Name == "passRNG")
                    {
                        reader.Read();
                        if (reader.Value.IndexOf('\n') == -1)
                            cf.passRNG = reader.Value;
                    }
                    else if (reader.Name == "loginTrain")
                    {
                        reader.Read();
                        if (reader.Value.IndexOf('\n') == -1)
                            cf.loginTrain = reader.Value;
                    }
                    else if (reader.Name == "passTrain")
                    {
                        reader.Read();
                        if (reader.Value.IndexOf('\n') == -1)
                            cf.passTrain = reader.Value;
                    }
                    else if (reader.Name == "log")
                    {
                        reader.Read();
                        if (reader.Value.IndexOf('\n') == -1)
                            cf.log = reader.Value;
                    }
                    else if (reader.Name == "KeepLogging")
                    {
                        reader.Read();
                        if (reader.Value.IndexOf('\n') == -1)
                            cf.KeepLogging = Convert.ToBoolean(reader.Value);
                    }
                }



            }
            return cf;
        }
    
    }

}
