using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Core.DevToolsProtocolExtension;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Policy;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml.Linq;
using WebView2.DevTools.Dom;
using WebView2.DevTools.Dom.Input;
using static Microsoft.Web.WebView2.Core.DevToolsProtocolExtension.CSS;
using static Microsoft.Web.WebView2.Core.DevToolsProtocolExtension.Network;
using static Secuvox_2._0.Form1.CustomTabControl;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace Secuvox_2._0
{
    public partial class Form1 : Form
    {
        CustomTabControl tabControl;
        public static Form1 instance;
        public Form1()
        {
            InitializeComponent();

            if (!System.IO.Directory.Exists(".\\cache"))
                System.IO.Directory.CreateDirectory(".\\cache");

            instance= this; 


            adblock = System.IO.File.ReadAllLines(".\\hosts");

            

        }



     
        public static string CurrentUri = "";

        public class WebClientWithTimeout : WebClient
        {
            //10 secs default
            public int Timeout { get; set; } = 500;

            //for sync requests
            protected override WebRequest GetWebRequest(Uri uri)
            {
                try
                {
                    var w = base.GetWebRequest(uri);
                    w.Timeout = Timeout; //10 seconds timeout
                    return w;
                } catch (Exception ex) { return null; }
            }

            //the above will not work for async requests :(
            //let's create a workaround by hiding the method
            //and creating our own version of DownloadStringTaskAsync
            public new async Task<string> DownloadStringTaskAsync(Uri address)
            {
                var t = base.DownloadStringTaskAsync(address);
                if (await Task.WhenAny(t, Task.Delay(Timeout)) != t) //time out!
                {
                    CancelAsync();
                }
                return await t;
            }
        }


  



        public static String[] adblock;

        WebView2DevToolsContext devToolsContext;



        public static bool doScroll=false;
        public static bool doHover = false;
        public static bool doGeneric = false;


        public class CustomTabControl : System.Windows.Forms.TabControl
        {


            /// <summary>
            /// override to draw the close button
            /// </summary>
            /// <param name="e"></param>
            protected override void OnDrawItem(DrawItemEventArgs e)
            {
                RectangleF tabTextArea = RectangleF.Empty;
                for (int nIndex = 0; nIndex < this.TabCount; nIndex++)
                {
                    if (nIndex != this.SelectedIndex)
                    {
                        /*if not active draw ,inactive close button*/
                        tabTextArea = (RectangleF)this.GetTabRect(nIndex);
                        using (Bitmap bmp = new Bitmap(GetContentFromResource("cross.png")))
                        {
                            e.Graphics.DrawImage(bmp,
                                 tabTextArea.X + tabTextArea.Width - 20, (tabTextArea.Height / 2) - 7, 18, 18);
                        }
                    }
                    else
                    {
                        tabTextArea = (RectangleF)this.GetTabRect(nIndex);
                       // LinearGradientBrush br = new LinearGradientBrush(tabTextArea, SystemColors.ControlLightLight, SystemColors.Control, LinearGradientMode.Vertical);
                        //e.Graphics.FillRectangle(br, tabTextArea);

                        /*if active draw ,inactive close button*/
                        using (Bitmap bmp = new Bitmap(GetContentFromResource("cross.png")))
                        {
                            e.Graphics.DrawImage(bmp,
                                tabTextArea.X + tabTextArea.Width - 20, (tabTextArea.Height/2)-7, 18, 18);
                        }
                     //   br.Dispose();
                    }
                    
                    string str = this.TabPages[nIndex].Text;
                    StringFormat stringFormat = new StringFormat();
                    stringFormat.Alignment = StringAlignment.Center;
                    stringFormat.LineAlignment = StringAlignment.Center;
                    using (SolidBrush brush = new SolidBrush(this.TabPages[nIndex].ForeColor))
                    {
                        if (str.Length > 22)
                        {
                            str = str.Substring(0, 22);
                            str += "...";
                        }
                        e.Graphics.DrawString(str,this.Font, brush,
                        tabTextArea,stringFormat);
                    }
                }
            
            }

            protected override void OnMouseDown(MouseEventArgs e)
            {
                for (int idx =0;idx<this.TabPages.Count;idx++) 
                {
                    RectangleF tabTextArea = (RectangleF)this.GetTabRect(idx);
                    tabTextArea =
                        new RectangleF(tabTextArea.X + tabTextArea.Width - 18, (tabTextArea.Height / 2) - 9, 18, 18);
                    System.Drawing.Point pt = new System.Drawing.Point(e.X, e.Y);
                    if (tabTextArea.Contains(pt))
                    {

                        this.TabPages.RemoveAt(idx);
                    }
                }
            }

            private Stream GetContentFromResource(string filename)
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                Stream stream = asm.GetManifestResourceStream(
                    "Secuvox_2._0." + filename);
                return stream;
            }


            public class CustomTabPage : TabPage
            {
                public long id = 0;
                private static long maxId = 0;
                public Microsoft.Web.WebView2.WinForms.WebView2 webView2;
                WebView2DevToolsContext devToolsContext;
                public CustomTabPage()
                {
                    this.Text = "https://google.com";
                    maxId++;
                    this.id = maxId;
                    webView2 = new Microsoft.Web.WebView2.WinForms.WebView2();


                    this.Controls.Add(webView2);
                    ((System.ComponentModel.ISupportInitialize)(webView2)).BeginInit();
                    this.webView2.AllowExternalDrop = true;
                    this.webView2.CreationProperties = null;
                    this.webView2.DefaultBackgroundColor = System.Drawing.Color.White;
                    this.webView2.Dock = System.Windows.Forms.DockStyle.Fill;
                    this.webView2.Location = new System.Drawing.Point(3, 3);
                    this.webView2.Name = "webView21";
                    this.webView2.Size = new System.Drawing.Size(2110, 1288);
                    this.webView2.TabIndex = 4;
                    this.webView2.ZoomFactor = 1D;
                    ((System.ComponentModel.ISupportInitialize)(webView2)).EndInit();
                    webView2.CoreWebView2InitializationCompleted += WebView21_CoreWebView2InitializationCompleted;
                    var op = new CoreWebView2EnvironmentOptions("--disable-web-security");
                    var env = CoreWebView2Environment.CreateAsync(null, null, op);
                    
                    webView2.EnsureCoreWebView2Async(env.Result);

                }



                String[] toReplaceHover = new string[]
       {
            
           // "on",
            "onmouseenter",
"onmouseleave",
"onmousemove",
"onmouseout",
"onmouseover",
"mouseleave",
"mousemove",
"mouseout",
"mouseover",
"hover",
"onhover"
       };

                String[] toReplaceScroll = new string[]
                     {
            
           // "on",
"onwheel",
"wheel",
"mousewheel",
"onmousewheel",
"scroll",
"onscroll",
"scrolled",
"onscrolled"
                     };
                private async Task<CoreWebView2WebResourceResponse> ConvertResponseAsync(HttpResponseMessage aResponse, String content, String charset)
                {
                    CoreWebView2WebResourceResponse cwv2Response;

                    var statCode = (int)aResponse.StatusCode;



                    HttpHeaders headers = aResponse.Headers;


                    MemoryStream stream = new MemoryStream();
                    { //Default is what I would normally expect.
                        if (charset == "UTF-8")
                            stream = new MemoryStream(Encoding.UTF8.GetBytes(content ?? ""));
                        else
                            stream = new MemoryStream(Encoding.ASCII.GetBytes(content ?? ""));
                        cwv2Response = this.webView2.CoreWebView2.Environment.CreateWebResourceResponse(stream, statCode, aResponse.ReasonPhrase, headers.ToString());
                    }
                    //  heads.AppendHeader(@"Content-Disposition", @"attachment");




                    foreach (var header2 in cwv2Response.Headers)
                    {
                        System.Diagnostics.Debug.WriteLine(String.Concat("Key: ", header2.Key, "  Value: ", header2.Value));
                    }


                    return cwv2Response;
                }

                private HttpRequestMessage ConvertRequest(CoreWebView2WebResourceRequest request)
                {
                    HttpRequestMessage req = new HttpRequestMessage((HttpMethod.Get), request.Uri);

                    foreach (var header in request.Headers)
                    {
                        req.Headers.Add(header.Key, header.Value);
                    }
                    return req;
                }


                private void WebView21_NavigationStarting(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs e)
                {
                    CurrentUri = e.Uri.ToString();
                    //pictureBox1.Visible = false;
                    // webView21.Visible = false;
                    // pictureBox1.Visible = true;


                }

                async void CoreWebView2_WebResourceRequested(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs e)
                {
                    var d = e.GetDeferral();
                    if (e.Request.Headers.Contains("HTTP_REFERER"))
                        e.Request.Headers.RemoveHeader("HTTP_REFERER");
                    if (e.Request.Headers.Contains("Referer"))
                        e.Request.Headers.RemoveHeader("Referer");
                    try
                    {
                        String[] parts = new Uri(e.Request.Uri).Host.Split('.');
                        if (parts.Length > 1)
                            if (Form1.adblock.Contains(new Uri(e.Request.Uri).Host))
                            {
                                e.Request.Uri = "about:blank";
                                return;
                            }
                    }
                    catch { }



                    //  if (!new Uri(e.Request.Uri).Host.Contains("google."))
                    {




                        try
                        {
                            if (e.Request.Uri.StartsWith("http"))
                            {

                                HttpRequestMessage httpreq = ConvertRequest(e.Request);
                                var client = new HttpClient();
                                var response = await client.SendAsync(httpreq);

                                Stream strmText = await response.Content.ReadAsStreamAsync();
                                Ude.CharsetDetector cdet = new Ude.CharsetDetector();
                                cdet.Feed(strmText);
                                cdet.DataEnd();
                                if (cdet.Charset == "UTF-8" || cdet.Charset == "ASCII")
                                {

                                    String sText = await response.Content.ReadAsStringAsync();

                                    if (!sText.StartsWith("<svg "))
                                    {
                                        bool found = false;
                                        foreach (String s in toReplaceHover)
                                            if (sText.Contains(s))
                                                found = true;

                                        foreach (String s in toReplaceScroll)
                                            if (sText.Contains(s))
                                                found = true;

                                        if (sText.Contains("\"on\""))
                                            found = true;
                                        if (sText.Contains("'on'"))
                                            found = true;

                                        if (sText.Contains("scrollTop"))
                                            found = true;

                                        if (sText.Contains("pageYOffset"))
                                            found = true;

                                        if (sText.Contains("scrollArea"))
                                            found = true;

                                        if (sText.Contains("getBoundingClientRect().top"))
                                            found = true;

                                        if (sText.Contains("offsetTop"))
                                            found = true;



                                        if (found)
                                        {

                                            if (!Form1.doHover)
                                            {
                                                foreach (String s in toReplaceHover)
                                                {
                                                    sText = sText.Replace("\"" + s + "\"", "");
                                                    sText = sText.Replace("'" + s + "'", "");
                                                    sText = sText.Replace(s + "=", "");
                                                    sText = sText.Replace(s + " =", "");
                                                    sText = sText.Replace("." + s, "");
                                                }
                                            }

                                            if (!Form1.doScroll)
                                            {
                                                foreach (String s in toReplaceScroll)
                                                {
                                                    sText = sText.Replace("\"" + s + "\"", "");
                                                    sText = sText.Replace("." + s, "");
                                                    sText = sText.Replace(s + "=", "");
                                                    sText = sText.Replace(s + " =", "");
                                                    sText = sText.Replace("." + s, "");
                                                    sText = sText.Replace("scrollTop", "a");
                                                    sText = sText.Replace("pageYOffset", "a");
                                                    sText = sText.Replace("scrollArea", "a");
                                                    sText = sText.Replace("getBoundingClientRect().top", "a");
                                                    sText = sText.Replace("offsetTop", "a");
                                                }

                                            }

                                            if (!Form1.doGeneric)
                                            {
                                                sText = sText.Replace("\"on\"+", "\"no\"+");
                                                sText = sText.Replace("\"on\" +", "\"no\" +");
                                                sText = sText.Replace("'on'+", "\"no\"+");
                                                sText = sText.Replace("'on' +", "\"no\" +");
                                            }






                                            System.Net.HttpStatusCode sc = response.StatusCode;
                                            var totalBytes = response.Content.Headers.ContentLength;

                                            System.Diagnostics.Debug.Print("HttpStatusCode: " + sc.ToString());
                                            System.Diagnostics.Debug.Print("Content.Headers.ContentLength: " + totalBytes.ToString());


                                            response.EnsureSuccessStatusCode();



                                            foreach (var header2 in response.Headers)
                                            {
                                                string headerContent = string.Join(",", header2.Value.ToArray()); ;
                                                System.Diagnostics.Debug.WriteLine(String.Concat("Key: ", header2.Key, "  Value: ", headerContent));
                                            }

                                            sText = sText.Replace("<html", "<HTML");
                                            sText = sText.Substring(sText.IndexOf("<HTML"));

                                            e.Response = await ConvertResponseAsync(response, sText, cdet.Charset);
                                        }
                                    }

                                }


                            }


                        }
                        catch (Exception ex) { }
                    }

                    d.Complete();




                }

                async void WebView21_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
                {
                    devToolsContext = await ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.CreateDevToolsContextAsync();
                    await ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.CallDevToolsProtocolMethodAsync("Network.enable", "{}");
                    await ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.CallDevToolsProtocolMethodAsync("Fetch.enable", "{\"patterns\":[{\"urlPattern\":\"*\"}]}");
                    
                    
                    ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.GetDevToolsProtocolEventReceiver("Fetch.requestPaused").DevToolsProtocolEventReceived += WebView2_FetchRequestPaused;

                    //((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
                    ((Microsoft.Web.WebView2.WinForms.WebView2)sender).NavigationStarting += WebView21_NavigationStarting;
                    ((Microsoft.Web.WebView2.WinForms.WebView2)sender).NavigationCompleted += CustomTabPage_NavigationCompleted;

                    //((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.AddWebResourceRequestedFilter("*", 0);
                    //((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";
                    ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:124.0) Gecko/20100101 Firefox/124.0";
                    ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
                    ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
                    ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.Profile.PreferredTrackingPreventionLevel = CoreWebView2TrackingPreventionLevel.Strict;




                    
                    


                    ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.Navigate("https://google.com");
                    ((CustomTabPage)((Microsoft.Web.WebView2.WinForms.WebView2)sender).Parent).webView2.Focus();



                    //webView21.CoreWebView2.Profile.ClearBrowsingDataAsync();
                }

                async void CustomTabPage_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
                {
                    if(Form1.instance.tabControl.SelectedTab==((CustomTabControl.CustomTabPage) ((Microsoft.Web.WebView2.WinForms.WebView2)sender).Parent))
                    {
                        Form1.instance.toolStripTextBox1.Text = ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.Source;
                    
                    }
                    ((CustomTabControl.CustomTabPage)((Microsoft.Web.WebView2.WinForms.WebView2)sender).Parent).Text = ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.DocumentTitle;
                    ((CustomTabControl)((CustomTabControl.CustomTabPage)((Microsoft.Web.WebView2.WinForms.WebView2)sender).Parent).Parent).Refresh();

                    await((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.ExecuteScriptAsync("document.body.addEventListener(\"keydown\",event => { if (event.keyCode === 87 && event.ctrlKey)  window.chrome.webview.postMessage(\"PressedW\"); if (event.keyCode === 84 && event.ctrlKey)  window.chrome.webview.postMessage(\"PressedT\"); });"); ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.WebMessageReceived += (_, args) => {
                        if (args.WebMessageAsJson.Contains("PressedW"))
                        {
                            CustomTabControl.CustomTabPage tabPage = (CustomTabControl.CustomTabPage)((Microsoft.Web.WebView2.WinForms.WebView2)sender).Parent;
                            ((CustomTabControl)Form1.instance.tabControl).TabPages.Remove(tabPage);
                            tabPage.Dispose();
                        }
                        if (args.WebMessageAsJson.Contains("PressedT"))
                        {
                            CustomTabControl.CustomTabPage tabPage = new CustomTabPage();
                            ((CustomTabControl)Form1.instance.tabControl).TabPages.Add(tabPage);
                            ((CustomTabControl)Form1.instance.tabControl).SelectedTab = tabPage;
                        }
                    };

                }

                async void WebView2_FetchRequestPaused(object sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
                {
                    //if (e.ParameterObjectAsJson.Contains("responseStatusCode"))
                    {
                        var doc = JsonDocument.Parse(e.ParameterObjectAsJson);
                        var id = doc.RootElement.GetProperty("requestId").ToString();
                        string type = doc.RootElement.GetProperty("resourceType").ToString();
                        string url = doc.RootElement.GetProperty("request").GetProperty("url").ToString();
                        string method = doc.RootElement.GetProperty("request").GetProperty("method").ToString();
                        string payload = "{\"requestId\":\"" + id + "\"}";

                        

                        //string code = doc.RootElement.GetProperty("responseStatusCode").ToString();

                        if (type!="Image" /* && !string.IsNullOrWhiteSpace(code) && code == "200"*/)
                        {

                            try
                            {
                                byte[] bytes= { };
                                if (method == "GET")
                                {
                                    WebClient wc=new WebClient();
                                    foreach (JsonProperty header in doc.RootElement.GetProperty("request").GetProperty("headers").EnumerateObject())
                                    {

                                        String name = header.Name;
                                        String value = header.Value.ToString();
                                        wc.Headers.Add(name, value);

                                    }
                                    bytes = wc.DownloadData(url);
                                }
                                else if (method == "POST")
                                {
                                    string myParameters = doc.RootElement.GetProperty("request").GetProperty("postData").ToString();
                                    string URI = url;


                                    WebClient wc = new WebClient();
                                    foreach (JsonProperty header in doc.RootElement.GetProperty("request").GetProperty("headers").EnumerateObject())
                                    {

                                        String name = header.Name;
                                        String value = header.Value.ToString();
                                        wc.Headers.Add(name, value);

                                    }
                                    //wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                                        
                                        string HtmlResult = wc.UploadString(URI, myParameters);
                                        bytes=Encoding.ASCII.GetBytes(HtmlResult);
                                    
                                    
                                }

                                
                                MemoryStream stream = new MemoryStream(bytes);

                               
                                    //String bodyResponse = 





                                    try
                                {
                                    String[] parts = new Uri(url).Host.Split('.');
                                    if (parts.Length > 1)
                                        if (Form1.adblock.Contains(new Uri(url).Host))
                                        {
                                           url = "about:blank";
                                            await webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Fetch.failRequest", payload);
                                            return;
                                        }
                                }
                                catch { }

                                String sText = "";
                                if (url.Contains("jquery"))
                                {
                                    sText = System.IO.File.ReadAllText(".\\jquery.js");
                                    sText = Convert.ToBase64String(Encoding.ASCII.GetBytes(sText));
                                }

                                //  if (!new Uri(e.Request.Uri).Host.Contains("google."))
                                {




                                    try
                                    {
                                        if (url.StartsWith("http") && !url.Contains("jquery"))
                                        {


                                           
                                            Ude.CharsetDetector cdet = new Ude.CharsetDetector();
                                            cdet.Feed(stream);
                                            cdet.DataEnd();
                                            if (cdet.Charset == "UTF-8" || cdet.Charset == "ASCII")
                                            {
                                             
                                                if (cdet.Charset=="UTF-8")
                                                    sText= Encoding.UTF8.GetString(stream.ToArray());
                                                else
                                                    sText = Encoding.ASCII.GetString(stream.ToArray());
                                                if (!sText.StartsWith("<svg "))
                                            {
                                                bool found = false;
                                                foreach (String s in toReplaceHover)
                                                    if (sText.Contains(s))
                                                        found = true;

                                                foreach (String s in toReplaceScroll)
                                                    if (sText.Contains(s))
                                                        found = true;

                                                if (sText.Contains("\"on\""))
                                                    found = true;
                                                if (sText.Contains("'on'"))
                                                    found = true;

                                                if (sText.Contains("scrollTop"))
                                                    found = true;

                                                if (sText.Contains("pageYOffset"))
                                                    found = true;

                                                if (sText.Contains("scrollArea"))
                                                    found = true;

                                                if (sText.Contains("getBoundingClientRect().top"))
                                                    found = true;

                                                if (sText.Contains("offsetTop"))
                                                    found = true;



                                                    if (found)
                                                    {

                                                        if (!Form1.doHover)
                                                        {
                                                            foreach (String s in toReplaceHover)
                                                            {
                                                                sText = sText.Replace("\"" + s + "\"", "\"onload\"");
                                                                sText = sText.Replace("'" + s + "'", "'onload'");
                                                                sText = sText.Replace(s + "=", "onload=");
                                                                sText = sText.Replace(s + " =", "onload=");
                                                                sText = sText.Replace("." + s, ".onload");
                                                            }
                                                        }

                                                        if (!Form1.doScroll)
                                                        {
                                                            foreach (String s in toReplaceScroll)
                                                            {
                                                                sText = sText.Replace("\"" + s + "\"", "\"onload\"");                                                
                                                                sText = sText.Replace(s + "=", "onload=");
                                                                sText = sText.Replace(s + " =", "onload=");
                                                                sText = sText.Replace("." + s, ".onload");
                                                                sText = sText.Replace("scrollTop", "top");
                                                                sText = sText.Replace("pageYOffset", "top");
                                                                sText = sText.Replace("scrollArea", "getBoundingClientRect()");
                                                                //sText = sText.Replace("getBoundingClientRect().top", "");
                                                                sText = sText.Replace("offsetTop", "top");
                                                                sText = sText.Replace("scroll\"+", "on\"+");
                                                                sText = sText.Replace("scroll\" +", "on\"+");
                                                            }

                                                        }

                                                        if (!Form1.doGeneric)
                                                        {
                                                            sText = sText.Replace("\"on\"+", "\"no\"+");
                                                            sText = sText.Replace("\"on\" +", "\"no\" +");
                                                            sText = sText.Replace("'on'+", "\"no\"+");
                                                            sText = sText.Replace("'on' +", "\"no\" +");
                                                        }




                                                    }


                                                }
                                               if (cdet.Charset == "UTF-8")
                                                   sText = Convert.ToBase64String(Encoding.UTF8.GetBytes(sText));
                                                else
                                                    sText = Convert.ToBase64String(Encoding.ASCII.GetBytes(sText));
                                           
                                            }
                                            else
                                            {
                                                await webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Fetch.continueRequest", payload);
                                                return;
                                                //sText = Convert.ToBase64String(stream.ToArray());
                                            }

                                        }

                                    }
                                    catch { }
                                }


                                String headers = "[";
                                    foreach(JsonProperty header in doc.RootElement.GetProperty("request").GetProperty("headers").EnumerateObject())
                                    {

                                        String name=header.Name;
                                        String value=header.Value.ToString();                                       
                                        headers=headers+"{\"name\":\""+name+"\",\"value\":\""+value.Replace("\"","\\\"")+"\"},";
                                        
                                    }
                                    headers = headers + "{\"Access-Control-Allow-Origin\":\"" + "*" + "\",\"value\":\"" + "TRUE" + "\"},";
                                    headers = headers.Substring(0,headers.Length - 1);
                                    headers = headers + "]";
                                    

                                    // string payload1 = "{\"requestId\":\"" + id.Split('-')[id.Split('-').Length-1] + "\",\"responseCode\":200,\"responseHeaders\":" + header + ",\"body\":\"" + bodyResponse + "\"}";
                                    string payload1 = "{\"requestId\":\"" +id+ "\",\"responseCode\":200,\"body\":\""+ sText + "\",\"headers\":"+headers+ "}";


                                    //String payload2 = "{\"requestId\":\"" + id + "\",\"headers\":" + headers + ",\"url\":\"" + url + "\",\"method\":\"GET\",\"interceptResponse\":false}";
                                     await webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Fetch.fulfillRequest", payload1);
                                   
                                 

                                
                            }catch { }
                           


                        }
                       else
                        {
                            try
                            {
                                await webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Fetch.continueRequest", payload);
                            }
                            catch  { }
                        }
                    }
                }
            }
            
            public CustomTabControl()
            {
                Dock = DockStyle.Fill;
                SizeMode=TabSizeMode.Fixed;
                ItemSize = new System.Drawing.Size(200, 25);
                CustomTabPage customTabPage = new CustomTabPage();
                customTabPage.Text = "<none>";
                //SetStyle(ControlStyles.UserPaint, true);
                this.DrawMode = TabDrawMode.OwnerDrawFixed;
                TabPages.Add(customTabPage);
            }
        }


        public void startNavigate()
        {
            if (toolStripTextBox1.Text == "")
                toolStripTextBox1.Text = "https://google.com";
            if (!toolStripTextBox1.Text.StartsWith("http"))
                toolStripTextBox1.Text = "https://" + toolStripTextBox1.Text;
            try
            {
                if (new Uri(toolStripTextBox1.Text).Host.Split('.').Length > 1)
                {
                    ((CustomTabControl.CustomTabPage)tabControl.SelectedTab).webView2.CoreWebView2.Navigate(toolStripTextBox1.Text);                    
                }
                else
                {
                    ((CustomTabControl.CustomTabPage)tabControl.SelectedTab).webView2.CoreWebView2.Navigate("https://google.com/?q=" + toolStripTextBox1.Text);
                }
                Form1.instance.tabControl.SelectedTab.Text= toolStripTextBox1.Text;
                Form1.instance.tabControl.Refresh();
                ((CustomTabControl.CustomTabPage)tabControl.SelectedTab).webView2.Focus();
            } catch { ((CustomTabControl.CustomTabPage)tabControl.SelectedTab).webView2.CoreWebView2.Navigate("https://google.com/?q=" + toolStripTextBox1.Text); }
        }

        private String genBodyTag(String bodyTag,  string javascript)
        {
            bodyTag = bodyTag + "<script type='text/javascript'>"+javascript+"</script>";
            return bodyTag;
        }

  

        private string ReplaceBlockedFunctions(string javascript)
        {
            return javascript;
        }

        private string findSubStrings(string javascript)
        {
            return "";
        }

        private void webView21_Click(object sender, EventArgs e)
        {

        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            startNavigate();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            toolStripTextBox1.Width = this.Width - 200;
            tabControl = new CustomTabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Visible = true;
            panel1.Controls.Add(tabControl);
           
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            toolStripTextBox1.Width = this.Width - 200;
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            ((CustomTabControl.CustomTabPage)tabControl.SelectedTab).webView2.GoBack();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            ((CustomTabControl.CustomTabPage)tabControl.SelectedTab).webView2.GoForward();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            ((CustomTabControl.CustomTabPage)tabControl.SelectedTab).webView2.Reload();
        }

        private void toolStripTextBox1_Click(object sender, EventArgs e)
        {

        }

        private void toolStripTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {

            if (e.KeyChar == 13)
            {
                if (((CustomTabControl)tabControl).TabPages.Count == 0)
                {
                    CustomTabControl.CustomTabPage tabPage = new CustomTabControl.CustomTabPage();
                    ((CustomTabControl)tabControl).TabPages.Add(tabPage);
                }
                else
                {
                    ((CustomTabControl.CustomTabPage)tabControl.SelectedTab).webView2.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.CacheStorage);
                    ((CustomTabControl.CustomTabPage)tabControl.SelectedTab).webView2.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.WebSql);
                    ((CustomTabControl.CustomTabPage)tabControl.SelectedTab).webView2.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.DiskCache);
                    ((CustomTabControl.CustomTabPage)tabControl.SelectedTab).webView2.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.LocalStorage);
                    ((CustomTabControl.CustomTabPage)tabControl.SelectedTab).webView2.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.ServiceWorkers);
                    ((CustomTabControl.CustomTabPage)tabControl.SelectedTab).webView2.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.IndexedDb);
                    startNavigate();
                }
            
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void featuresHover_Click(object sender, EventArgs e)
        {
            doHover=!featuresHover.Checked;
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
       
        }

        private void featuresScroll_Click(object sender, EventArgs e)
        {
            doScroll = !featuresScroll.Checked;
        }

        private void featuresGeneric_Click(object sender, EventArgs e)
        {
            doGeneric = !featuresGeneric.Checked;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.T)
            {
                CustomTabControl.CustomTabPage tabPage = new CustomTabPage();
                ((CustomTabControl)Form1.instance.tabControl).TabPages.Add(tabPage);
                ((CustomTabControl)Form1.instance.tabControl).SelectedTab = tabPage;
                ((CustomTabControl.CustomTabPage)tabControl.SelectedTab).webView2.Focus();
            }
            else if (e.Control && e.KeyCode == Keys.W)
            {
                CustomTabControl.CustomTabPage tabPage = (CustomTabControl.CustomTabPage)((Microsoft.Web.WebView2.WinForms.WebView2)sender).Parent;
                ((CustomTabControl)Form1.instance.tabControl).TabPages.Remove(tabPage);
                tabPage.Dispose();
            }
         
        }
    }
}
