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

    public partial class MainService : ServiceBase
    {
        public bool stopbit;
        public static StConfig st;
        public struct StConfig
        {
            public string WSRNG, WSTrain, frequencyMiliSeconds, loginRNG, passRNG, loginTrain, passTrain,log;

            public StConfig(string p1, string p2, string IP2, string lRNG,string pRNG,
                                string logTrain,string pTrain, string log1)
            {
                WSRNG = p1;
                WSTrain = p2;
                frequencyMiliSeconds = IP2;
                loginRNG = lRNG;
                passRNG = pRNG;
                loginTrain = logTrain;
                passTrain = pTrain;
                log = log1;
            }
        }

        public MainService(string[] args)
        {
            st = readconfig(args[0], "configRest", "WSRNG");
            InitializeComponent();


        }

        public MainService()
        {
            st = readconfig("config.xml", "configRest", "WSRNG");
            InitializeComponent();


        }

        protected override void OnStart(string[] args)
        {
            stopbit = false;
            while (true)
            {
                if (stopbit == true)
                {
                    break;
                }

                Data data = new Data();
                //Reqdata reqdata = new Reqdata();
                //reqdata.eventstr = "GetNewRequest";
                //var json = JsonConvert.SerializeObject(reqdata);
                try
                {


                    string responseToString;
                    responseToString = "";
                    var response = PostMethod("{\"event\":\"GetNewRequest\"}", st.WSTrain, "application/json");
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
                        n.UseDefaultCredentials = false;
                        n.Credentials = new NetworkCredential(st.loginRNG, st.passRNG);
                        result = n.ВыполнитьАлгоритмИПолучитьРезультат("ПаровозикиФоновыеЗадания", responseToString,
                            false,
                            false, true);
                        DateTime localDate = DateTime.Now;
                        StreamWriter file = Getlog(st.log);
                        file.WriteLine(localDate.ToString(new CultureInfo("ru-RU")) + "----на передано в RNG запросов " + data.data.Count + ". Результат - " + Convert.ToString(result));
                        file.Close();

                    }
                }
                catch (Exception ex)
                {
                    DateTime localDate = DateTime.Now;
                    StreamWriter file = Getlog(st.log);
                    file.WriteLine(localDate.ToString(new CultureInfo("ru-RU")) + "----на передачу в RNG" + data.data.Count +".Ошибка - "+ ex.ToString());
                    file.Close();
                }

                Thread.Sleep(Convert.ToInt32(st.frequencyMiliSeconds));
            }

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

        public static HttpWebResponse PostMethod(string postedData, string postUrl, string contentType)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(postUrl);
            request.Method = "POST";
            request.Credentials = CredentialCache.DefaultCredentials;

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

        protected override void OnStop()
        {
            stopbit = false;
        }

        protected override void OnPause()
        {
            stopbit = false;
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
                }



            }
            return cf;
        }
    
    }

}
