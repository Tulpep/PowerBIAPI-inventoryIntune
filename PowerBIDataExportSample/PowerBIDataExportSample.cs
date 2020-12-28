///===================================================================================
///   PowerBIServiceDataExportSample.cs
///===================================================================================
/// -- Author:       Jeff Pries (jeff@jpries.com)
/// -- Create date:  10/4/2019
/// -- Description:  Sample application to export data from the Power BI API using an interactive prompt for credentials
/// 

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Newtonsoft.Json;

namespace PowerBIDataExportSample
{
    class PowerBIDataExportSample
    {
        /// ----------------------------------------------------------------------------------------------------------------------------------------------------------------- ///
        ///
        /// Global Constants and Variables
        ///    

        // Constants
        const string HTTPHEADUSERAGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/68.0.3440.106 Safari/537.36";
        const string PBI_API_URLBASE = "https://api.powerbi.com/v1.0/myorg/";

        const string AuthorityURL = "https://login.windows.net/common/oauth2/authorize";  // use with 3.19.8

        const string ResourceURL = "https://analysis.windows.net/powerbi/api";
        const string RedirectURL = "https://login.microsoftonline.com/common/oauth2/nativeclient";
        const string ApplicationID = "ab133804-8555-42e5-b334-f96fe7981a45"; // Native Azure AD App ClientID  --  Put your Client ID here

        const string UserName = "jeff@contoso.com";  // Put your Active Directory / Power BI Username here (note this is not a secure place to store this!)
        const string Password = "mysecretpassword";  // Put your Active Directory / Power BI Password here (note this is not secure pace to store this!  this is a sample only)

        // Variables 
        private static HttpClient client = null;


        /// ----------------------------------------------------------------------------------------------------------------------------------------------------------------- ///
        ///
        /// Default Class constructor
        ///    
        public PowerBIDataExportSample()
        {
        }

        /// ----------------------------------------------------------------------------------------------------------------------------------------------------------------- ///
        ///
        /// Execute Method
        ///    
        public void Execute(string authType)
        {
            string authToken = "";

            // Get an authentication token
            authToken = GetAuthTokenUser(authType);  // Uses native AD auth
            Console.WriteLine("Token de autenticacion: " + authToken);

            // Initialize the client with the token
            if (!String.IsNullOrEmpty(authToken))
            {
                InitHttpClient(authToken);

                Console.WriteLine("Despues de la conexion");

                //GetWorkspaces();

                //CreateDataset(authToken);
                //GetDatasets();



                AddRows("efb13f2c-d94d-459f-83d2-e67af7f6391f", "ComputerSoftwareInventory");
                Console.WriteLine("");
                Console.WriteLine("- Done!");
            }
            Console.ReadKey();
        }

        /// ----------------------------------------------------------------------------------------------------------------------------------------------------------------- ///
        ///
        /// GetAuthUserLogin Method (Interactive)
        /// 
        public async Task<AuthenticationResult> GetAuthUserLoginInteractive()
        {
            AuthenticationResult authResult = null;

            PlatformParameters parameters = new PlatformParameters(PromptBehavior.Always);

            try
            {
                // Query Azure AD for an interactive login prompt and subsequent Power BI auth token
                AuthenticationContext authContext = new AuthenticationContext(AuthorityURL);
                authResult = await authContext.AcquireTokenAsync(ResourceURL, ApplicationID, new Uri(RedirectURL), parameters).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("   - Error acquiring token with interactive credentials.");
                Console.WriteLine("     Usually this is due to an invalid username or password.");
                Console.WriteLine("");
                Console.WriteLine("     Details: " + ex.Message);
                authResult = null;
            }

            return authResult;
        }

        /// ----------------------------------------------------------------------------------------------------------------------------------------------------------------- ///
        ///
        /// GetAuthUserLogin Method (Saved Credential)
        /// 
        public async Task<AuthenticationResult> GetAuthUserLoginSavedCredential()
        {
            AuthenticationResult authResult = null;

            PlatformParameters parameters = new PlatformParameters(PromptBehavior.Always);

            try
            {
                // Query Azure AD for an interactive login prompt and subsequent Power BI auth token
                AuthenticationContext authContext = new AuthenticationContext(AuthorityURL);

                UserPasswordCredential userPasswordCredential = new UserPasswordCredential(UserName, Password);
                authResult = await authContext.AcquireTokenAsync(ResourceURL, ApplicationID, userPasswordCredential).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine("   - Error acquiring token with saved credentials.");
                Console.WriteLine("     Usually this is due to an invalid username or password.");
                Console.WriteLine("");
                Console.WriteLine("     Details: " + ex.Message);
            }

            return authResult;
        }


        /// ----------------------------------------------------------------------------------------------------------------------------------------------------------------- ///
        ///
        /// GetAuthToken Method (Interactive and Saved Credential)
        /// 
        public string GetAuthTokenUser(string authType)
        {
            Task<AuthenticationResult> authResult = null;
            string authToken = "";

            Console.WriteLine("- Performing App authentication to request API access token...");
            if (authType == "SavedCredential")
            {
                authResult = GetAuthUserLoginSavedCredential();
                authResult.Wait();  // Wait for the authentication to be attempted
            }
            else
            {
                authResult = GetAuthUserLoginInteractive();
                authResult.Wait();  // Wait for the authentication to be attempted
            }

            // If authentication result received, get the token
            if (authResult != null)
            {
                if (authResult.Result != null)
                {
                    authToken = authResult.Result.CreateAuthorizationHeader();
                    if (authToken.Substring(0, 6) == "Bearer")
                    {
                        Console.WriteLine("   - API Authorization token received.");
                    }
                    else
                    {
                        Console.WriteLine("   - Unable to retrieve API Authorization token.");
                    }
                }
                else
                {
                    Console.WriteLine("   - Unable to retrieve API Authorization token.");
                }
            }
            else
            {
                Console.WriteLine("   - Unable to retrieve API Authorization token.");
            }

            return authToken;
        }

        /// ----------------------------------------------------------------------------------------------------------------------------------------------------------------- ///
        ///
        /// InitHttpClient Method
        ///    
        public void InitHttpClient(string authToken)
        {
            Console.WriteLine("");
            Console.WriteLine("- Initializing client with generated auth token...");

            // Create the web client connection
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(HTTPHEADUSERAGENT);
            client.DefaultRequestHeaders.Add("Authorization", authToken);
        }

        /// ----------------------------------------------------------------------------------------------------------------------------------------------------------------- ///
        ///
        /// GetWorkspaces Method
        /// 
        public void GetWorkspaces()
        {
            HttpResponseMessage response = null;
            HttpContent responseContent = null;
            string strContent = "";

            PowerBIWorkspace rc = null;

            string serviceURL = PBI_API_URLBASE + "groups";

            try
            {
                Console.WriteLine("");
                Console.WriteLine("- Retrieving data from: " + serviceURL);

                response = client.GetAsync(serviceURL).Result;

                Console.WriteLine("   - Response code received: " + response.StatusCode);
                //Console.WriteLine(response);  // debug
                try
                {
                    responseContent = response.Content;
                    strContent = responseContent.ReadAsStringAsync().Result;

                    if (strContent.Length > 0)
                    {
                        Console.WriteLine("   - De-serializing Workspace Data...");

                        // Parse the JSON string into objects and store in DataTable
                        JavaScriptSerializer js = new JavaScriptSerializer();
                        js.MaxJsonLength = 2147483647;  // Set the maximum json document size to the max
                        rc = js.Deserialize<PowerBIWorkspace>(strContent);

                        if (rc != null)
                        {
                            if (rc.value != null)
                            {
                                Console.WriteLine("      - Workspaces received: " + rc.value.Count);
                                foreach (PowerBIWorkspaceValue item in rc.value)
                                {
                                    string workspaceID = "";
                                    string workspaceName = "";
                                    string workspaceDescription = "";
                                    string capacityID = "";
                                    string dataflowStorageID = "";
                                    bool isOnDedicatedCapacity = false;
                                    bool isReadOnly = false;
                                    bool isOrphaned = false;
                                    string workspaceState = "";
                                    string workspaceType = "";

                                    if (item.id != null)
                                    {
                                        workspaceID = item.id;
                                    }

                                    if (item.name != null)
                                    {
                                        workspaceName = item.name;
                                    }

                                    if (item.description != null)
                                    {
                                        workspaceDescription = item.description;
                                    }

                                    if (item.capacityId != null)
                                    {
                                        capacityID = item.capacityId;
                                    }

                                    if (item.dataflowStorageId != null)
                                    {
                                        dataflowStorageID = item.dataflowStorageId;
                                    }

                                    if (item.type != null)
                                    {
                                        workspaceType = item.type;
                                    }

                                    isOnDedicatedCapacity = item.isOnDedicatedCapacity;
                                    isReadOnly = item.isReadOnly;
                                    isOrphaned = item.isOrphaned;

                                    if (item.state != null)
                                    {
                                        workspaceState = item.state;
                                    }

                                    if (item.type != null)
                                    {
                                        workspaceState = item.type;
                                    }

                                    // Output the Workspace Data
                                    Console.WriteLine("");
                                    Console.WriteLine("----------------------------------------------------------------------------------");
                                    Console.WriteLine("");
                                    Console.WriteLine("Workspace ID: " + workspaceID);
                                    Console.WriteLine("Workspace Name: " + workspaceName);
                                    Console.WriteLine("Workspace Description: " + workspaceDescription);
                                    Console.WriteLine("Capacity ID: " + capacityID);
                                    Console.WriteLine("Dataflow Storage ID: " + dataflowStorageID);
                                    Console.WriteLine("On Dedicated Capacity: " + isOnDedicatedCapacity);
                                    Console.WriteLine("Read Only: " + isReadOnly);
                                    Console.WriteLine("Orphaned: " + isOrphaned);
                                    Console.WriteLine("WorkspaceState: " + workspaceState);
                                    Console.WriteLine("WorkspaceType: " + workspaceType);
                                    Console.ReadKey();
                                } // foreach
                            } // rc.value
                        } // rc

                    }
                    else
                    {
                        Console.WriteLine("   - No content received.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("   - API Access Error: " + ex.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("   - API Access Error: " + ex.ToString());
            }
        }

        public void GetDatasets()
        {
            HttpResponseMessage response = null;
            HttpContent responseContent = null;
            string strContent = "";

            PowerBIWorkspace rc = null;

            //string serviceURL = PBI_API_URLBASE +  "datasets";
            string serviceURL = "https://api.PowerBI.com/v1.0/myorg/groups/{ffa5684d-a93f-4430-93c2-0491f8ff7d37}/datasets";
            try
            {
                Console.WriteLine("");
                Console.WriteLine("- Retrieving data from: " + serviceURL);



                response = client.GetAsync(serviceURL).Result;
                
                Console.WriteLine("   - Response code received: " + response.StatusCode);
                //Console.WriteLine(response);  // debug
                try
                {
                    responseContent = response.Content;

                    strContent = responseContent.ReadAsStringAsync().Result;
                    Console.WriteLine(responseContent);
                    if (strContent.Length > 0)
                    {
                        Console.WriteLine("   - De-serializing Workspace Data...");

                        // Parse the JSON string into objects and store in DataTable
                        ////JavaScriptSerializer js = new JavaScriptSerializer();
                        ////js.MaxJsonLength = 2147483647;  // Set the maximum json document size to the max
                        ////rc = js.Deserialize<PowerBIWorkspace>(strContent);

                        string datasetId = string.Empty;
                        var results = JsonConvert.DeserializeObject<dynamic>(strContent);
                        Console.WriteLine(results);
                        //foreach (var result in results)
                        //{
                        //    datasetId = result["value"][0]["id"];
                        //    Console.WriteLine(String.Format("Dataset ID: {​​0}​​", result));
                        //}
                        
                        datasetId = results["value"][0]["id"];
                        Console.WriteLine(String.Format("Dataset ID: {​​0}​​", datasetId));
                        Console.ReadLine();
                        
                    

                    }
                    else
                    {
                        Console.WriteLine("   - No content received.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("   - API Access Error: " + ex.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("   - API Access Error: " + ex.ToString());
            }
        }

        #region Add rows to a Power BI table
        public static void AddRows(string datasetId, string tableName)
        {
            string powerBIApiAddRowsUrl = String.Format("https://api.powerbi.com/v1.0/myorg/datasets/{0}/tables/{1}/rows", datasetId, tableName);
            HttpResponseMessage response = null;
            string strContent = "";
            HttpContent responseContent = null;

            ////POST web request to add rows.
            ////To add rows to a dataset in a group, use the Groups uri: https://api.powerbi.com/v1.0/myorg/groups/{group_id}/datasets/{dataset_id}/tables/{table_name}/rows
            ////Change request method to "POST"
            //HttpWebRequest request = System.Net.WebRequest.Create(powerBIApiAddRowsUrl) as System.Net.HttpWebRequest;
            //request.KeepAlive = true;
            //request.Method = "POST";
            //request.ContentLength = 0;
            //request.ContentType = "application/json";

            ////Add token to the request header
            //request.Headers.Add("Authorization", String.Format("Bearer {0}"));

            ////JSON content for product row
            string rowsJson = "{\"rows\":" +
                "[{\"AppVersion\":1,\"ComputerName\":\"Adjustable Race\",\"DisplayName\":\"Components\",\"InstallDate\":\"07/30/2014\",\"InstallLocation\":\"07/30/2014\",\"IsDomainJoined\":\"true\",\"IsMSI\":\"true\",\"IsThereIntuneManagementExtension\":\"true\",\"Publisher\":\"true\",\"UninstallString\":\"ddfgdfghdfgh\"}," +
                //"[{\"AppVersion\":1,\"ComputerName\":\"Adjustable Race\",\"DisplayName\":\"Components\",\"InstallDate\":" + DateTime.Now.ToString("yyyyMMdd") + ",\"InstallLocation\":\"07/30/2014\",\"IsDomainJoined\":\"true\",\"IsMSI\":\"true\",\"IsThereIntuneManagementExtension\":\"true\",\"Publisher\":\"true\",\"UninstallString\":\"ddfgdfghdfgh\"}," +
                "{\"AppVersion\":2,\"ComputerName\":\"LL Crankarm\",\"DisplayName\":\"Components\",\"InstallDate\":\"07/30/2014\",\"InstallLocation\":\"07/30/2014\", \"IsDomainJoined\":\"false\",\"IsMSI\":\"false\",\"IsThereIntuneManagementExtension\":\"true\",\"Publisher\":\"true\",\"UninstallString\":\"dfghdfghdfghdh\"}]}";
            //"{\"AppVersion\":2,\"ComputerName\":\"LL Crankarm\",\"DisplayName\":\"Components\",\"InstallDate\":" + DateTime.Now.ToString("yyyyMMdd") + ",\"InstallLocation\":\"07/30/2014\", \"IsDomainJoined\":\"false\",\"IsMSI\":\"false\",\"IsThereIntuneManagementExtension\":\"true\",\"Publisher\":\"true\",\"UninstallString\":\"dfghdfghdfghdh\"}]}";

            ////POST web request
            //byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(rowsJson);
            //request.ContentLength = byteArray.Length;


            HttpContent c = new StringContent(rowsJson, Encoding.UTF8, "application/json");

            response = client.PostAsync(powerBIApiAddRowsUrl, c).Result;

            responseContent = response.Content;

            strContent = responseContent.ReadAsStringAsync().Result;
            Console.WriteLine(responseContent);
            if (strContent.Length > 0)
            {
                Console.WriteLine("   - De-serializing Workspace Data...");

                // Parse the JSON string into objects and store in DataTable
                ////JavaScriptSerializer js = new JavaScriptSerializer();
                ////js.MaxJsonLength = 2147483647;  // Set the maximum json document size to the max
                ////rc = js.Deserialize<PowerBIWorkspace>(strContent);

                var results = JsonConvert.DeserializeObject<dynamic>(strContent);
                Console.WriteLine(results);
                //foreach (var result in results)
                //{
                //    datasetId = result["value"][0]["id"];
                //    Console.WriteLine(String.Format("Dataset ID: {​​0}​​", result));
                //}
            }
            else
            {
                Console.WriteLine("   - No content received.");
            }




        }

        #endregion
        //public void PostRows()
        //{
        //    HttpResponseMessage response = null;
        //    HttpContent responseContent = null;
        //    string strContent = "";

        //    PowerBIWorkspace rc = null;

        //    //string serviceURL = PBI_API_URLBASE +  "datasets";
        //    string serviceURL = "https://api.PowerBI.com/v1.0/myorg/groups/{ffa5684d-a93f-4430-93c2-0491f8ff7d37}/datasets";
        //    try
        //    {
        //        Console.WriteLine("");
        //        Console.WriteLine("- Retrieving data from: " + serviceURL);



        //        response = client.PostAsync(serviceURL, ).Result;

        //        Console.WriteLine("   - Response code received: " + response.StatusCode);
        //        //Console.WriteLine(response);  // debug
        //        try
        //        {
        //            responseContent = response.Content;

        //            strContent = responseContent.ReadAsStringAsync().Result;
        //            Console.WriteLine(responseContent);
        //            if (strContent.Length > 0)
        //            {
        //                Console.WriteLine("   - De-serializing Workspace Data...");

        //                // Parse the JSON string into objects and store in DataTable
        //                ////JavaScriptSerializer js = new JavaScriptSerializer();
        //                ////js.MaxJsonLength = 2147483647;  // Set the maximum json document size to the max
        //                ////rc = js.Deserialize<PowerBIWorkspace>(strContent);

        //                string datasetId = string.Empty;
        //                var results = JsonConvert.DeserializeObject<dynamic>(strContent);
        //                Console.WriteLine(results);
        //                //foreach (var result in results)
        //                //{
        //                //    datasetId = result["value"][0]["id"];
        //                //    Console.WriteLine(String.Format("Dataset ID: {​​0}​​", result));
        //                //}

        //                datasetId = results["value"][0]["id"];
        //                Console.WriteLine(String.Format("Dataset ID: {​​0}​​", datasetId));
        //                Console.ReadLine();


        //            }
        //            else
        //            {
        //                Console.WriteLine("   - No content received.");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine("   - API Access Error: " + ex.ToString());
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("   - API Access Error: " + ex.ToString());
        //    }
        //}



        //private static string GetToken()
        //{
        //    // TODO: Install-Package Microsoft.IdentityModel.Clients.ActiveDirectory -Version 2.21.301221612
        //    // and add using Microsoft.IdentityModel.Clients.ActiveDirectory

        //    //The client id that Azure AD created when you registered your client app.
        //    string clientID = "{Client_ID}";

        //    //RedirectUri you used when you register your app.
        //    //For a client app, a redirect uri gives Azure AD more details on the application that it will authenticate.
        //    // You can use this redirect uri for your client app
        //    string redirectUri = "https://login.live.com/oauth20_desktop.srf";

        //    //Resource Uri for Power BI API
        //    string resourceUri = "https://analysis.windows.net/powerbi/api";

        //    //OAuth2 authority Uri
        //    string authorityUri = "https://login.microsoftonline.com/common/";

        //    //Get access token:
        //    // To call a Power BI REST operation, create an instance of AuthenticationContext and call AcquireToken
        //    // AuthenticationContext is part of the Active Directory Authentication Library NuGet package
        //    // To install the Active Directory Authentication Library NuGet package in Visual Studio,
        //    //  run "Install-Package Microsoft.IdentityModel.Clients.ActiveDirectory" from the nuget Package Manager Console.

        //    // AcquireToken will acquire an Azure access token
        //    // Call AcquireToken to get an Azure token from Azure Active Directory token issuance endpoint
        //    AuthenticationContext authContext = new AuthenticationContext(authorityUri);
        //    string token = authContext.AcquireToken(resourceUri, clientID, new Uri(redirectUri)).AccessToken;

        //    Console.WriteLine(token);
        //    Console.ReadLine();

        //    return token;
        //}

        private static string GetDataset(string token)
        {
            string powerBIDatasetsApiUrl = "https://api.powerbi.com/v1.0/myorg/datasets";
            //POST web request to create a dataset.
            //To create a Dataset in a group, use the Groups uri: https://api.PowerBI.com/v1.0/myorg/groups/{group_id}/datasets
            HttpWebRequest request = System.Net.WebRequest.Create(powerBIDatasetsApiUrl) as System.Net.HttpWebRequest;
            request.KeepAlive = true;
            request.Method = "GET";
            request.ContentLength = 0;
            request.ContentType = "application/json";

            //Add token to the request header
            request.Headers.Add("Authorization", String.Format("Bearer {0}", token));

            string datasetId = string.Empty;
            //Get HttpWebResponse from GET request
            using (HttpWebResponse httpResponse = request.GetResponse() as System.Net.HttpWebResponse)
            {
                //Get StreamReader that holds the response stream
                using (StreamReader reader = new System.IO.StreamReader(httpResponse.GetResponseStream()))
                {
                    string responseContent = reader.ReadToEnd();

                    //TODO: Install NuGet Newtonsoft.Json package: Install-Package Newtonsoft.Json
                    //and add using Newtonsoft.Json
                    var results = JsonConvert.DeserializeObject<dynamic>(responseContent);

                    //Get the first id
                    datasetId = results["value"][0]["id"];

                    Console.WriteLine(String.Format("Dataset ID: {0}", datasetId));
                    Console.ReadLine();

                    return datasetId;
                }
            }
        }

        private static void CreateDataset(string token)
        {
            //TODO: Add using System.Net and using System.IO
            string powerBIDatasetsApiUrl = "https://api.PowerBI.com/v1.0/myorg/groups/{f0945d09-afcd-412b-ba8f-22784c6f47f0}/datasets";
            //POST web request to create a dataset.f0945d09-afcd-412b-ba8f-22784c6f47f0 https://api.powerbi.com/v1.0/myorg/datasets
            //To create a Dataset in a group, use the Groups uri: https://api.PowerBI.com/v1.0/myorg/groups/{group_id}/datasets
            HttpWebRequest request = System.Net.WebRequest.Create(powerBIDatasetsApiUrl) as System.Net.HttpWebRequest;
            request.KeepAlive = true;
            request.Method = "POST";
            request.ContentLength = 0;
            request.ContentType = "application/json";

            //Add token to the request header
            request.Headers.Add("Authorization", String.Format("Bearer {0}", token));

            //Create dataset JSON for POST request
            string datasetJson = "{\"name\": \"SalesMarketing\", \"tables\": " +
                "[{\"name\": \"Product\", \"columns\": " +
                "[{ \"name\": \"ProductID\", \"dataType\": \"Int64\"}, " +
                "{ \"name\": \"Name\", \"dataType\": \"string\"}, " +
                "{ \"name\": \"Category\", \"dataType\": \"string\"}," +
                "{ \"name\": \"IsCompete\", \"dataType\": \"bool\"}," +
                "{ \"name\": \"ManufacturedOn\", \"dataType\": \"DateTime\"}" +
                "]}]}";

            //POST web request
            byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(datasetJson);
            request.ContentLength = byteArray.Length;

            //Write JSON byte[] into a Stream
            using (Stream writer = request.GetRequestStream())
            {
                writer.Write(byteArray, 0, byteArray.Length);

                var response = (HttpWebResponse)request.GetResponse();

                Console.WriteLine(string.Format("Dataset {0}", response.StatusCode.ToString()));

                Console.ReadLine();
            }
        }

        /// ----------------------------------------------------------------------------------------------------------------------------------------------------------------- ///
        ///
        /// Main Method
        ///    
        static void Main(string[] args)
        {
            PowerBIDataExportSample pbi = new PowerBIDataExportSample();
            string key = "";

            Console.WriteLine("Login method?");
            Console.WriteLine("[1] Interactive login prompt");
            Console.WriteLine("[2] Saved credential in program source file");
            Console.WriteLine("");

            key = Console.ReadKey().KeyChar.ToString();
            Console.WriteLine("");


            if (key == "1")
            {
                
                Console.WriteLine("Executing using an interactive prompt for credentials.");
                pbi.Execute("InteractiveLogin");
            }
            else if (key == "2")
            {
                Console.WriteLine("Executing using saved credential.");
                pbi.Execute("SavedCredential");
            }
            else
            {
                Console.WriteLine("Please choose 1 or 2");
            }
            
        }
    }
}
