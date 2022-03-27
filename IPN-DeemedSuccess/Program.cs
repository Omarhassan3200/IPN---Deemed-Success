using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace IPN_DeemedSuccess
{
    class Program
    {
        
//*****************************************************************************************FUNCTIONS**************************************************************************************************************


//-------DT Structure---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        public static void DT_Structure(DataTable DT)
        {
            DT.Clear();

            DT.Columns.Add(new DataColumn("TransactionId", typeof(string)));
            DT.Columns.Add(new DataColumn("TransactionType", typeof(string)));
            DT.Columns.Add(new DataColumn("TransactionTimeStamp", typeof(string)));
            DT.Columns.Add(new DataColumn("TransactionAmount", typeof(string)));
            DT.Columns.Add(new DataColumn("Status", typeof(string)));
            DT.Columns.Add(new DataColumn("ResponseDescription", typeof(string)));

        }

//-------Read File---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        public static void Read_File(string Path,DataTable DT,string FileName)
        {
            // Read the file and display it line by line.  
            System.IO.StreamReader file = new System.IO.StreamReader(Path);

            try
            {


                // gets the first line from file.
                string line1 = File.ReadLines(Path).First();

                // Current Line
                string line = "";

                ArrayList TransactionId = new ArrayList();
                ArrayList TransactionType = new ArrayList();
                ArrayList TransactionTimeStamp = new ArrayList();
                ArrayList TransactionAmount = new ArrayList();

                while ((line = file.ReadLine()) != null)
                {
                    string current_line = line.ToString();

                    TransactionId.Add(current_line.Substring(0, 36));
                    TransactionType.Add(current_line.Substring(36, 32));
                    TransactionTimeStamp.Add(current_line.Substring(78, 23));
                    TransactionAmount.Add(current_line.Substring(221, 18));


                }

                for (int i = 0; i < TransactionId.Count; i++)
                {
                    DataRow dr = DT.NewRow();

                    dr["TransactionId"] = TransactionId[i].ToString();
                    dr["TransactionType"] = TransactionType[i].ToString();
                    dr["TransactionTimeStamp"] = TransactionTimeStamp[i].ToString();
                    dr["TransactionAmount"] = TransactionAmount[i].ToString();


                    DT.Rows.Add(dr);
                }
            }

            catch(Exception e)
            {

            }


            
            file.Dispose();
            file.Close();


        }

 //-----------------Call API------------------------------------------------------------------------------------------------------------------------------------------------------------
        public static async Task<HttpResponseMessage> CallApi(HttpClient client, string RequestMethod, string RequestPath, HttpContent content)
        {
            //var APIsSecretKey = EDApp.AES.Decrypt(SecretKey, MySettingsModel.KEY);
            //string RequestUrl = HttpUtility.HtmlEncode(RequestPath.ToLower());
            //string RequestUrl = RequestPath.ToLower();
            //string APPID = "3181892D-E0AC-4E93-8283-BE5C6A0F4F80";
            //string RequestTimestamp = Functions.GetTimeStamp();
            //string nonce = Guid.NewGuid().ToString("N");
            //string hashValue = string.Format("{0}{1}{2}{3}{4}", APPID, RequestMethod, RequestUrl, RequestTimestamp, nonce);
            //string RequestSignarureBase64 = Functions.HashMethodBase64(hashValue, APIsSecretKey);
            //string Signarure = string.Format("{0}:{1}:{2}:{3}", APPID, RequestSignarureBase64, nonce, RequestTimestamp);
            //string SignatureBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(Signarure));
            //client.DefaultRequestHeaders.Add("ApiKey", SignatureBase64);

            return await client.PostAsync(RequestPath, content);
        }
        //--------Get Token---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        bool error = false;
        public static async Task<string> GetTokenAsync()
        {
            try
            {
                string Request_Token_Path = "/hid/generatetoken";
                var handler = new HttpClientHandler();
                var client = new HttpClient(handler);
                string RequestMethod = "POST";

                int Counter = 0;
                string final_token = "";
                Rootobject root;

                client.BaseAddress = new Uri(ConfigurationManager.AppSettings["APILinkToken"]);

            repeat:
                {

                    HID_Token hID_Token = new HID_Token
                    {
                        user = ConfigurationManager.AppSettings["HIDUsername"],
                        password = ConfigurationManager.AppSettings["HIDPassword"],
                    };
                    var json_token = JsonConvert.SerializeObject(hID_Token, Formatting.Indented);
                    HttpContent content_token = new StringContent(json_token, Encoding.UTF8, "application/json");


                    HttpResponseMessage result_token = await CallApi(client, RequestMethod, Request_Token_Path, content_token);
                    string Temp = result_token.Content.ReadAsStringAsync().Result.ToString();


                    root = JsonConvert.DeserializeObject<Rootobject>(Temp);

                    final_token = root.tokens.accessToken;


                }
                if (root.tokens.error != null)
                {

                    if (root.tokens.error.ToString() == "Authentication failure" && Counter < 3)
                    {
                        Counter++;
                        goto repeat;

                    }
                }

                return final_token;
            }

            catch(Exception e)
            {
                return e.Message;
            }

        }

//--------Check For Transaction-------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        public static async Task CheckForTransactionsAsync(string token,string transactionid,string requesttype,DataTable DT,int counter)
        {

            try
            {
                var handler = new HttpClientHandler();
                var client = new HttpClient(handler);

                client.BaseAddress = new Uri(ConfigurationManager.AppSettings["APILink"]);


                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                HttpResponseMessage result = await client.GetAsync("/funds/" + transactionid + '/' + requesttype);

                string final_result = result.Content.ReadAsStringAsync().Result;

                API_Response api_response = null;
                api_response = JsonConvert.DeserializeObject<API_Response>(final_result);

                if (result.IsSuccessStatusCode)
                {
                    if (api_response.Status==true)
                    {
                        DT.Rows[counter][4] = "SUCCESS";
                    }
                    else
                    {
                        DT.Rows[counter][4] = "FAILURE";
                        DT.Rows[counter][5] = api_response.Message.ToString();
                    }


                }
                
            }

            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
     

        }

//-------Write File------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        public static void Write_File(string path, DataTable tbl)
        {
            try
            {
                System.IO.File.WriteAllText(path, string.Empty);
                StreamWriter stream = new StreamWriter(path, true);
                //stream.Write(Environment.NewLine);                                                                                                                     

               

                foreach (DataRow row in tbl.Rows)
                {
                   
                    stream.Write(normalize(row["TransactionId"].ToString(),36)+ normalize(row["Status"].ToString(),7)+normalize(" ",5)+ normalizeResultDescription(row["ResponseDescription"].ToString(),100) +"\n");

                }
             

                stream.Flush();
                stream.Close();
            }
            catch (Exception e)
            {
                //WriteLog(ConfigurationManager.AppSettings["Log"] + "Log" + file + ".txt", DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt "), "Error", e.Message);
              

            }

        }
//---------------Normalize-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        public static string normalize(string str, int reserve)
        {
            char[] strchar = new char[reserve];
            int strStart = reserve - str.Length;
            int j = 0;
            string finalstr = "";

            for (int i = 0; i < strStart; i++)
            {
                strchar[i] = ' ';

            }

            for (int i = strStart; i < strchar.Length; i++)
            {

                strchar[i] = str[j];

                j++;
            }

            for (int i = 0; i < strchar.Length; i++)
            {
                finalstr += strchar[i];
            }

            return finalstr;
        }
//------------Normalize Result Description--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

        public static string normalizeResultDescription(string str, int reserve)
        {
            char[] strchar = new char[reserve];
            int strStart = 0;
            int j = 0;
            string finalstr = "";
            for (int i = strStart; i < str.Length; i++)
            {

                strchar[i] = str[j];

                j++;
            }

            for (int i = j; i < reserve; i++)
            {
                strchar[i] = ' ';
            }

            for (int i = 0; i < strchar.Length; i++)
            {
                finalstr += strchar[i];
            }

            return finalstr;
        }

//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++


        static async Task Main(string[] args)
        {

            DataTable dt = new DataTable();
            DT_Structure(dt);

            string readfilepath = @"C:\Omar\IPN Deemed Success\Requests\1-20201206A-DmdReq.txt";
            string writefilepath = @"C:\Omar\IPN Deemed Success\Requests\Result.txt";

            Read_File(readfilepath,dt,"");

            dt.Rows[0][4] = "OK";

            

            for (int i=0;i<dt.Rows.Count;i++)
            {

                string token = " ";
                token = await GetTokenAsync();
                await CheckForTransactionsAsync(token,dt.Rows[i][0].ToString(), "DEBIT",dt,i);

                 
            }


            Write_File(writefilepath,dt);

            



        }
    }
}
