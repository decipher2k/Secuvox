using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
using System.Web.UI;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Markup;
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

        public static Settings pageSettings=new Settings();

        [Serializable]
        public class Settings
        {
            public Dictionary<String,PerPageSettings> settings = new Dictionary<String,PerPageSettings>();
            [Serializable]
            public class PerPageSettings
            {
                public bool doScroll = false;
                public bool doHover = false;
                public bool doGeneric = false;
                public bool googleBot = true;
                public bool blockCSS = true;
                public bool ExtraAdblock = true;
            }
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
                    
                    ((System.ComponentModel.ISupportInitialize)(webView2)).EndInit();
                    webView2.CoreWebView2InitializationCompleted += WebView21_CoreWebView2InitializationCompleted;

                                                          

                    var op = new CoreWebView2EnvironmentOptions("--disable-web-security");
                    op.AreBrowserExtensionsEnabled = true;
                    
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


                public async void WebView21_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
                {
                    devToolsContext = await ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.CreateDevToolsContextAsync();
                    await ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.CallDevToolsProtocolMethodAsync("Network.enable", "{}");
                    await ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.CallDevToolsProtocolMethodAsync("Fetch.enable", "{\"patterns\":[{\"urlPattern\":\"*\"}]}");
                    
                    
                    ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.GetDevToolsProtocolEventReceiver("Fetch.requestPaused").DevToolsProtocolEventReceived += WebView2_FetchRequestPaused;

                    //((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
                    ((Microsoft.Web.WebView2.WinForms.WebView2)sender).NavigationStarting += WebView21_NavigationStarting;
                    ((Microsoft.Web.WebView2.WinForms.WebView2)sender).NavigationCompleted += CustomTabPage_NavigationCompleted;

                    //((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.AddWebResourceRequestedFilter("*", 0);
                  
                    if(Form1.instance.fakeGoogleBotToolStripMenuItem.Checked)
                        ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";

                    //((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:124.0) Gecko/20100101 Firefox/124.0";
                    ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
                    ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
                    ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.Profile.PreferredTrackingPreventionLevel = CoreWebView2TrackingPreventionLevel.Balanced;
                    

                    await ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.Profile.AddBrowserExtensionAsync(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)+ ".\\ublockOrigin\\1.56.0_0\\");
                    await ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.Profile.AddBrowserExtensionAsync(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + ".\\NinjaCookies\\0.7.0_0\\");

               //     if (Form1.instance.clearBrowsingDataToolStripMenuItem.Checked)
                 //       await ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.AllSite);








                    if(Form1.instance.toolStripTextBox1.Text=="")
                        ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.Navigate("https://google.com");
                    else
                        ((Microsoft.Web.WebView2.WinForms.WebView2)sender).CoreWebView2.Navigate(Form1.instance.toolStripTextBox1.Text);
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
                            Form1.instance.toolStripTextBox1.Text = "https://google.com";
                            CustomTabControl.CustomTabPage tabPage = new CustomTabPage();
                            ((CustomTabControl)Form1.instance.tabControl).TabPages.Add(tabPage);
                            ((CustomTabControl)Form1.instance.tabControl).SelectedTab = tabPage;
                        }
                    };

                }
                public static string HtmlEncode(string text)
                {
                    string result;
                    using (StringWriter sw = new StringWriter())
                    {
                        var x = new HtmlTextWriter(sw);
                        x.WriteEncodedText(text);
                        result = sw.ToString();
                    }
                    return result;

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

                        url = url.Replace("http://", "https://");

                        //string code = doc.RootElement.GetProperty("responseStatusCode").ToString();
                        string HtmlResult = "";
                        if (!url.Contains(".png") && !url.Contains(".jpg") && !url.Contains(".jpeg") && !url.Contains(".gif"))/* && !string.IsNullOrWhiteSpace(code) && code == "200"*/
                        {

                            try
                            {
                                byte[] bytes = { };
                                /*        if (method == "GET")
                                        {
                                            WebClient wc = new WebClient();
                                            foreach (JsonProperty header in doc.RootElement.GetProperty("request").GetProperty("headers").EnumerateObject())
                                            {

                                                String name = header.Name;
                                                String value = header.Value.ToString();
                                                wc.Headers.Add(name, value);

                                            }
                                            HtmlResult = wc.DownloadString(url);
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

                                            HtmlResult = wc.UploadString(URI, myParameters);
                                            bytes = wc.UploadData(URI, Encoding.ASCII.GetBytes(myParameters));


                                        }*/


                                String sText = "";

                                Stream strmText=null;
                                if (method == "GET")
                                {
                                    HttpRequestMessage httpreq = new HttpRequestMessage(HttpMethod.Get, url);
                                    
                                    foreach (JsonProperty header in doc.RootElement.GetProperty("request").GetProperty("headers").EnumerateObject())
                                    {

                                        String name = header.Name;
                                        String value = header.Value.ToString();
                                        if(name!="Referer")
                                            httpreq.Headers.Add(name, value);

                                    }
                                    httpreq.Headers.Add("DNT", "1");
                                    var client = new HttpClient();
                                    var response = await client.SendAsync(httpreq);

                                    sText = await response.Content.ReadAsStringAsync();
                                    strmText = await response.Content.ReadAsStreamAsync();
                                } else if (method == "POST")
                                {
                                    HttpRequestMessage httpreq = new HttpRequestMessage(HttpMethod.Post, url);
                                    httpreq.Content = new StringContent(doc.RootElement.GetProperty("request").GetProperty("postData").ToString());
                                    foreach (JsonProperty header in doc.RootElement.GetProperty("request").GetProperty("headers").EnumerateObject())
                                    {

                                        String name = header.Name;
                                        String value = header.Value.ToString();
                                        if (name != "Referer")
                                            httpreq.Headers.Add(name, value);

                                    }
                                    httpreq.Headers.Add("DNT", "1");

                                    var client = new HttpClient();
                                    var response = await client.SendAsync(httpreq);

                                   sText = await response.Content.ReadAsStringAsync();
                                    strmText= await response.Content.ReadAsStreamAsync();
                                }
                                else
                                {
                                    await webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Fetch.continueRequest", payload);
                                }

                                


                                //String bodyResponse = 





                                try
                                {
                                    if (!Form1.pageSettings.settings.ContainsKey(new Uri(Form1.instance.toolStripTextBox1.Text).Host) ||
                                        Form1.pageSettings.settings[new Uri(Form1.instance.toolStripTextBox1.Text).Host].ExtraAdblock)
                                    {
                                        String[] parts = new Uri(url).Host.Split('.');
                                        if (parts.Length > 1)
                                            if (Form1.instance.adblockerToolStripMenuItem.Checked)
                                            {
                                                if (Form1.adblock.Contains(new Uri(url).Host))
                                                {
                                                    url = "about:blank";
                                                    await webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Fetch.failRequest", payload);
                                                    return;
                                                }
                                            }
                                    }
                                }
                                catch { }
                                Ude.CharsetDetector cdet = new Ude.CharsetDetector();
                                cdet.Feed(strmText);
                                cdet.DataEnd();

                                if (url.Contains("jquery"))
                                {
                                    sText = System.IO.File.ReadAllText(".\\jquery.js");
                                    sText = Convert.ToBase64String(Encoding.ASCII.GetBytes(sText)); 
                                    
                                }
                              
                          /*      byte[] buffer = new byte[strmText.Length];
                                using (MemoryStream ms = new MemoryStream())
                                {
                                    int read;
                                    while ((read = strmText.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        ms.Write(buffer, 0, read);
                                    }
                                   bytes= ms.ToArray();
                                }
                                sText = Encoding.UTF8.GetString(bytes);
                                }*/
                          
                                try
                                {
                                    if (url.StartsWith("http")&&!url.Contains("jquery"))
                                    {



                              
                                        
                                        if (cdet.Charset!=null&& type!="Image" && !url.Contains(".png") && !url.Contains(".jpg") && !url.Contains(".jpeg") && !url.Contains(".gif"))
                                       
                                        {                                            

                                           

                                            if (!sText.StartsWith("<svg "))
                                            {
                                                sText.Replace("_blank", "_self");
                                                sText.Replace("_blank", "_top");
                                                sText.Replace("<a ", "<a rel=\"noreferrer\" referrerpolicy=\"no-referrer\"");

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
                                                if (sText.Contains("scrollY"))
                                                    found = true;


                                                if (sText.Contains("scrollArea"))
                                                    found = true;

                                                if (sText.Contains("getBoundingClientRect"))
                                                    found = true;

                                                if (sText.Contains("offsetTop"))
                                                    found = true;


                                                if (sText.Contains("scroll\"+"))
                                                    found= true;
                                                if (sText.Contains("scroll\" +"))
                                                found = true;
                                                if (sText.Contains("scrollHeight")) 
                                                    found = true;
                                                if (sText.Contains("clientHeight"))
                                                    found = true;
                                                
                                                {

                                                    if (!Form1.pageSettings.settings.ContainsKey(new Uri(Form1.instance.toolStripTextBox1.Text).Host) ||
                                                        !Form1.pageSettings.settings[new Uri(Form1.instance.toolStripTextBox1.Text).Host].doHover)
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

                                                    if (!Form1.pageSettings.settings.ContainsKey(new Uri(Form1.instance.toolStripTextBox1.Text).Host) ||
                                                        !Form1.pageSettings.settings[new Uri(Form1.instance.toolStripTextBox1.Text).Host].doScroll)
                                                    {
                                                        foreach (String s in toReplaceScroll)
                                                        {
                                                            sText = sText.Replace("\"" + s + "\"", "\"onload\"");
                                                            sText = sText.Replace("'" + s + "'", "\"onload\"");
                                                            sText = sText.Replace(s + "=", "onload=");
                                                            sText = sText.Replace(s + " =", "onload=");
                                                            sText = sText.Replace("." + s, ".onload");

                                                            sText = sText.Replace("scrollTop", "top");
                                                            sText = sText.Replace("pageYOffset", "top");
                                                            sText = sText.Replace("scrollArea", "");
                                                            sText = sText.Replace("getBoundingClientRect()", "getPosition()");
                                                            sText = sText.Replace("getClientRects()", "getPosition()");
                                                            
                                                            sText = sText.Replace("offsetTop", "top");
                                                            sText = sText.Replace("scrollY", "top");
                                                            sText = sText.Replace("scroll\"+", "on\"+");
                                                            sText = sText.Replace("scroll\" +", "on\"+");
                                                            sText = sText.Replace("scrollHeight", "top");
                                                            sText = sText.Replace("clientHeight", "top");
                                                            sText = sText.Replace("scrollTop", "top");
                                                            sText = sText.Replace("#scrollArea", "");
                                                            if (!Form1.pageSettings.settings.ContainsKey(new Uri(Form1.instance.toolStripTextBox1.Text).Host) ||
                                                                Form1.pageSettings.settings[new Uri(Form1.instance.toolStripTextBox1.Text).Host].blockCSS)
                                                            {
                                                                sText = sText.Replace("sticky", "");
                                                                sText = sText.Replace("calc(", "(");
                                                                sText = sText.Replace("data-scroll", "");
                                                                sText = sText.Replace(".observe", "");
                                                                sText = sText.Replace("scroll-", "");
                                                                sText = sText.Replace("scrollPosition", "");
                                                            }

                                                        }

                                                    }

                                                    if (!Form1.pageSettings.settings.ContainsKey(new Uri(Form1.instance.toolStripTextBox1.Text).Host) ||
                                                        Form1.pageSettings.settings[new Uri(Form1.instance.toolStripTextBox1.Text).Host].doGeneric)
                                                    {
                                                        sText = sText.Replace("\"on\"+", "\"no\"+");
                                                        sText = sText.Replace("\"on\" +", "\"no\" +");
                                                        sText = sText.Replace("'on'+", "\"no\"+");
                                                        sText = sText.Replace("'on' +", "\"no\" +");
                                                    }




                                                }
                                                sText = sText.Replace("crossorigin", "anonymous");
                                                if (cdet.Charset == "UTF-8")
                                                {
                                                    sText = Convert.ToBase64String(Encoding.UTF8.GetBytes(sText));
                                               }
                                                else if (cdet.Charset == "windows-1252")
                                                {
                                                    Encoding wind1252 = Encoding.GetEncoding(1252);
                                                    Encoding utf8 = Encoding.UTF8;
                                                    byte[] wind1252Bytes = wind1252.GetBytes(sText);
                                                    byte[] utf8Bytes = Encoding.Convert(wind1252, utf8, wind1252Bytes);
                                                    sText = Convert.ToBase64String(utf8Bytes);


                                                }
                                                else
                                                {
                                                    sText = Convert.ToBase64String(Encoding.ASCII.GetBytes(sText));
                                                }
                                            }
                                          

                                        }                                        
                                        else
                                        {
                                            await webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Fetch.continueRequest", payload);
                                            return;
                                            //Text = Convert.ToBase64String(stream.ToArray());
                                        }
                                        //sText = Base64UrlEncoder.Encode(sText);
                               /*        if(sText.ToLower().Contains("<html"))
                                            sText = HtmlEncode(sText);
                               */
                                        // sText =  Convert.ToBase64String(Encoding.UTF8.GetBytes(sText));

                                }
                                    else if(!url.Contains("jquery"))
                                    {
                                        await webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Fetch.continueRequest", payload);
                                        return;
                                    }


                                    
                                }
                                catch
                                {
                                    await webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Fetch.continueRequest", payload);
                                    return;
                                }

                             /* {
                                    byte[] ret = new byte[sText.Length];
                                    for (int i = 0; i < sText.Length; i++)
                                        ret[i] = (byte)sText[i];

                                    sText = Convert.ToBase64String(ret);
                                }*/
                                String headers = "[";
                                foreach (JsonProperty header in doc.RootElement.GetProperty("request").GetProperty("headers").EnumerateObject())
                                {

                                    String name = header.Name;
                                    String value = header.Value.ToString();
                                    headers = headers + "{\"name\":\"" + name + "\",\"value\":\"" + value.Replace("\"", "\\\"") + "\"},";

                                }
                                headers = headers + "{\"Access-Control-Allow-Origin\":\"" + "*" + "\",\"value\":\"" + "TRUE" + "\"},";
                                headers = headers.Substring(0, headers.Length - 1);
                                headers = headers + "]";


                                // string payload1 = "{\"requestId\":\"" + id.Split('-')[id.Split('-').Length-1] + "\",\"responseCode\":200,\"responseHeaders\":" + header + ",\"body\":\"" + bodyResponse + "\"}";
                                string payload1 = "{\"requestId\":\"" + id + "\",\"responseCode\":200,\"body\":\"" + sText + "\",\"headers\":" + headers + "}";


                                //String payload2 = "{\"requestId\":\"" + id + "\",\"headers\":" + headers + ",\"url\":\"" + url + "\",\"method\":\"GET\",\"interceptResponse\":false}";
                                await webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Fetch.fulfillRequest", payload1);
                                return;
                            }
                            catch
                            {
                                try
                                {
                                    await webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Fetch.continueRequest", payload);
                                }
                                catch { }
                                return;
                            }


                        }
                        else
                        {
                            try
                            {
                                await webView2.CoreWebView2.CallDevToolsProtocolMethodAsync("Fetch.continueRequest", payload);
                            }catch { }
                            return;
                            //sText = Convert.ToBase64String(stream.ToArray());
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
                    //if (Form1.pageSettings.settings.ContainsKey(new Uri(toolStripTextBox1.Text).Host))
                    if (Form1.pageSettings.settings.ContainsKey(new Uri(toolStripTextBox1.Text).Host))
                    {
                        adblockerToolStripMenuItem.Checked = Form1.pageSettings.settings[new Uri(toolStripTextBox1.Text).Host].ExtraAdblock;
                        featuresGeneric.Checked= !Form1.pageSettings.settings[new Uri(toolStripTextBox1.Text).Host].doGeneric;
                        featuresHover.Checked = !Form1.pageSettings.settings[new Uri(toolStripTextBox1.Text).Host].doHover;
                        featuresScroll.Checked = !Form1.pageSettings.settings[new Uri(toolStripTextBox1.Text).Host].doScroll;                        
                        paranoidToolStripMenuItem.Checked = Form1.pageSettings.settings[new Uri(toolStripTextBox1.Text).Host].blockCSS;
                    }
                    else
                    {
                        adblockerToolStripMenuItem.Checked = true;
                        featuresGeneric.Checked = true;
                        featuresHover.Checked = true;
                        featuresScroll.Checked = true;                        
                        paranoidToolStripMenuItem.Checked = true;
                    }
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
            toolStripTextBox1.Width = this.Width - 300;

        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
          
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            toolStripTextBox1.Width = this.Width - 300;
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
                    startNavigate();
                }
            
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void featuresHover_Click(object sender, EventArgs e)
        {
            setSettings();

        }

        private void setSettings()
        {
            if (!Form1.pageSettings.settings.ContainsKey(new Uri(toolStripTextBox1.Text).Host))
            {
                pageSettings.settings.Add(new Uri(toolStripTextBox1.Text).Host, new Settings.PerPageSettings());
            }
            pageSettings.settings[new Uri(toolStripTextBox1.Text).Host].doHover = !featuresHover.Checked;
            pageSettings.settings[new Uri(toolStripTextBox1.Text).Host].doGeneric = !featuresGeneric.Checked;
            pageSettings.settings[new Uri(toolStripTextBox1.Text).Host].doScroll = !featuresScroll.Checked;
            pageSettings.settings[new Uri(toolStripTextBox1.Text).Host].blockCSS = paranoidToolStripMenuItem.Checked;
            pageSettings.settings[new Uri(toolStripTextBox1.Text).Host].ExtraAdblock = adblockerToolStripMenuItem.Checked;
            //pageSettings.settings[new Uri(toolStripTextBox1.Text).Host].googleBot = fakeGoogleBotToolStripMenuItem.Checked;

        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
       
        }

        private void featuresScroll_Click(object sender, EventArgs e)
        {
            setSettings();
        }

        private void featuresGeneric_Click(object sender, EventArgs e)
        {
            setSettings();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.T)
            {
                toolStripTextBox1.Text = "https://google.com";
                CustomTabControl.CustomTabPage tabPage = new CustomTabPage();
                ((CustomTabControl)Form1.instance.tabControl).TabPages.Add(tabPage);
                ((CustomTabControl)Form1.instance.tabControl).SelectedTab = tabPage;
                ((CustomTabControl.CustomTabPage)tabControl.SelectedTab).webView2.Focus();
            }
            else if (e.Control && e.KeyCode == Keys.W)
            {
                CustomTabControl.CustomTabPage tabPage = (CustomTabControl.CustomTabPage)tabControl.SelectedTab;
                if (tabPage != null)
                {
                    ((CustomTabControl)Form1.instance.tabControl).TabPages.Remove(tabPage);
                    tabPage.Dispose();
                }
            }
         
        }

        private void clearBrowsingDataToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ((CustomTabControl.CustomTabPage)tabControl.SelectedTab).webView2.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.AllSite);
        }

        private void adblockerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setSettings();
        }

        private void Form1_MaximumSizeChanged(object sender, EventArgs e)
        {
            toolStripTextBox1.Width = this.Width - 300;
        }

        private void Form1_MaximizedBoundsChanged(object sender, EventArgs e)
        {
            toolStripTextBox1.Width = this.Width - 300;
        }

        private void Form1_LocationChanged(object sender, EventArgs e)
        {
            toolStripTextBox1.Width = this.Width - 300;
        }

        private void paranoidToolStripMenuItem_Click(object sender, EventArgs e)
        {
            setSettings();
        }

        private void fakeGoogleBotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CustomTabControl.CustomTabPage page=(CustomTabControl.CustomTabPage)this.tabControl.SelectedTab;

            page.webView2.Dispose();
            page.webView2 = new Microsoft.Web.WebView2.WinForms.WebView2();


            page.Controls.Add(page.webView2);
            ((System.ComponentModel.ISupportInitialize)(page.webView2)).BeginInit();
            page.webView2.AllowExternalDrop = true;
            page.webView2.CreationProperties = null;
            page.webView2.DefaultBackgroundColor = System.Drawing.Color.White;
            page.webView2.Dock = System.Windows.Forms.DockStyle.Fill;
            page.webView2.Location = new System.Drawing.Point(3, 3);
            page.webView2.Name = "webView21";
            page.webView2.Size = new System.Drawing.Size(2110, 1288);
            page.webView2.TabIndex = 4;

            ((System.ComponentModel.ISupportInitialize)(page.webView2)).EndInit();
            page.webView2.CoreWebView2InitializationCompleted += page.WebView21_CoreWebView2InitializationCompleted;



            var op = new CoreWebView2EnvironmentOptions("--disable-web-security");
            op.AreBrowserExtensionsEnabled = true;

            var env = CoreWebView2Environment.CreateAsync(null, null, op);

            page.webView2.EnsureCoreWebView2Async(env.Result);
        }
    }
}
