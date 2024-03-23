using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml.Linq;
using WebView2.DevTools.Dom;
using WebView2.DevTools.Dom.Input;
using static Microsoft.Web.WebView2.Core.DevToolsProtocolExtension.CSS;
using static System.Net.Mime.MediaTypeNames;

namespace Secuvox_2._0
{
    public partial class Form1 : Form
    {
       public Form1()
        {
            InitializeComponent();

            if (!System.IO.Directory.Exists(".\\cache"))
                System.IO.Directory.CreateDirectory(".\\cache");




            webView21.CoreWebView2InitializationCompleted += WebView21_CoreWebView2InitializationCompleted;
            webView21.EnsureCoreWebView2Async();
          

            adblock = System.IO.File.ReadAllLines(".\\hosts");
            
            
            
        }

    

        async void WebView21_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            devToolsContext = await webView21.CoreWebView2.CreateDevToolsContextAsync();
            webView21.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;

            webView21.NavigationStarting += WebView21_NavigationStarting;
            webView21.NavigationCompleted += WebView21_NavigationCompleted;
            webView21.CoreWebView2.AddWebResourceRequestedFilter("*", 0);
            //webView21.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";
            webView21.CoreWebView2.Settings.IsGeneralAutofillEnabled = false;
            webView21.CoreWebView2.Settings.IsPasswordAutosaveEnabled = false;
            
            //webView21.CoreWebView2.Profile.ClearBrowsingDataAsync();
        }
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
                }catch (Exception ex) { return null; }
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
        async void CoreWebView2_WebResourceRequested(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs e)
        {
            var d=e.GetDeferral();
                if (e.Request.Headers.Contains("HTTP_REFERER"))
                    e.Request.Headers.RemoveHeader("HTTP_REFERER");
                if (e.Request.Headers.Contains("Referer"))
                    e.Request.Headers.RemoveHeader("Referer");
                try
                {
                    String[] parts = new Uri(e.Request.Uri).Host.Split('.');
                    if (parts.Length > 1)
                        if (adblock.Contains(new Uri(e.Request.Uri).Host))
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
                        if (cdet.Charset =="UTF-8" || cdet.Charset=="ASCII")
                        {

                            String sText = await response.Content.ReadAsStringAsync();


                            bool found = false;
                            foreach (String s in toReplaceHover)
                                if (sText.Contains(s))
                                    found = true;

                            foreach (String s in toReplaceScroll)
                                if (sText.Contains(s))
                                    found = true;

                            if(sText.Contains("\"on\""))
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
                          


                            if(!found)
                            { 

                            if (featuresHover.Checked)
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

                                if (featuresScroll.Checked)
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


                                    if (featuresGeneric.Checked)
                                    {
                                        sText = sText.Replace("\"on\"+", "\"no\"+");
                                        sText = sText.Replace("\"on\" +", "\"no\" +");
                                        sText = sText.Replace("'on'+", "\"no\"+");
                                        sText = sText.Replace("'on' +", "\"no\" +");
                                    }






                                    System.Net.HttpStatusCode sc = response.StatusCode;
                                    var totalBytes = response.Content.Headers.ContentLength;
                                    System.Diagnostics.Debug.Print("##############################   HTTPResponse STATUS and SIZE   ###############################################");
                                    System.Diagnostics.Debug.Print("HttpStatusCode: " + sc.ToString());
                                    System.Diagnostics.Debug.Print("Content.Headers.ContentLength: " + totalBytes.ToString());
                                    System.Diagnostics.Debug.Print("##############################   HTTPResponse STATUS and SIZE   ###############################################");

                                    response.EnsureSuccessStatusCode();

                                    System.Diagnostics.Debug.Print("************************************   HTTP RECIEVED RESPONSE HEADERS   *******************************************");

                                    foreach (var header2 in response.Headers)
                                    {
                                        string headerContent = string.Join(",", header2.Value.ToArray()); ;
                                        System.Diagnostics.Debug.WriteLine(String.Concat("Key: ", header2.Key, "  Value: ", headerContent));
                                    }
                                    System.Diagnostics.Debug.Print("************************************   HTTP RECIEVED RESPONSE HEADERS   *******************************************");

                                    e.Response = await ConvertResponseAsync(response, sText, cdet.Charset);
                                }
                            }

                        }
                        else
                        {

                        }
                     
                      

                    }
                }
                catch (Exception ex) { }
            }

            d.Complete();
          
            
          

        }
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
                cwv2Response = this.webView21.CoreWebView2.Environment.CreateWebResourceResponse(stream, statCode, aResponse.ReasonPhrase, headers.ToString());
            }
            //  heads.AppendHeader(@"Content-Disposition", @"attachment");


            System.Diagnostics.Debug.Print("************************************   NEW RESPONSE HEADERS   *******************************************");

            foreach (var header2 in cwv2Response.Headers)
            {
                System.Diagnostics.Debug.WriteLine(String.Concat("Key: ", header2.Key, "  Value: ", header2.Value));
            }
            System.Diagnostics.Debug.Print("************************************   NEW RESPONSE HEADERS   *******************************************");

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

    
    
    String[] adblock;

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
"scroll",
"onscroll",
"scrolled",
"onscrolled"
             };
        WebView2DevToolsContext devToolsContext;
        

        async void WebView21_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {


            /*      String text = await devToolsContext.GetContentAsync();
                  if (!(featuresGeneric.Checked == false && featuresScroll.Checked == false && featuresHover.Checked == false))
                  {
                      if (text != null)
                      {

                          int start = text.IndexOf("<head");
                          if (start < 0)
                              start = text.IndexOf("<HEAD");

                          text = text.Substring(start);
                          text = "<html>" + text;

                      //    text = text.Replace(" async ", "");

                          WebView2.DevTools.Dom.HtmlElement[] scripts = await devToolsContext.QuerySelectorAllAsync("script");

                          text = text.Replace("<a ", "<a rel=\"noreferrer\" ");

                          foreach (WebView2.DevTools.Dom.HtmlElement script in scripts)
                          {

                              String outerText = await script.GetAttributeAsync("src");
                              if (outerText != null)
                              {
                                  String url = "";
                                  if (outerText.Contains("http"))
                                  {
                                      url = outerText;
                                  }
                                  else
                                  {
                                      url = "https://" + new Uri(toolStripTextBox1.Text).Host + "/" + outerText;
                                  }
                                  url = url.Replace("\"", "");
                                  url = url.Replace("///", "/");

                                  // text = text.Replace(outerText, "about:blank");
                                  try
                                  {
                                      String[] host = new Uri(url).Host.Split('.');
                                      if (!adblock.Contains(host[host.Length - 2] + "." + host[host.Length - 1]))
                                      {
                                          try
                                          {
                                              String javascript = new WebClient().DownloadString(url);




                                              String sTextOrig = javascript;
                                              String sText = sTextOrig;
                                              bool found = true;

                                              if (featuresHover.Checked)
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

                                              if (featuresScroll.Checked)
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

                                              if (featuresGeneric.Checked)
                                              {
                                                  sText = sText.Replace("\"on\"+", "\"no\"+");
                                                  sText = sText.Replace("\"on\" +", "\"no\" +");
                                                  sText = sText.Replace("'on'+", "\"no\"+");
                                                  sText = sText.Replace("'on' +", "\"no\" +");
                                              }

                                              javascript = sText;

                                              javascript = javascript.Replace("</head", "");
                                              javascript = javascript.Replace("</HEAD", "");
                                              javascript = javascript.Replace("<BODY", "");
                                              javascript = javascript.Replace("</BODY", "");
                                              javascript = javascript.Replace("<body", "");
                                              javascript = javascript.Replace("</body", "");
                                              javascript = javascript.Replace("<script", "");
                                              javascript = javascript.Replace("</script", "");


                                              String tag = await script.GetOuterHtmlAsync();
                                              text = text.Replace("</head>", "<script>" + javascript + "</script></head>");

                                          }
                                          catch (Exception ex) { }//error text
                                      }

                                  }
                                  catch (Exception ex)
                                  { //error text
                                  }
                              }
                              else
                              {




                                  String sTextOrig = await script.GetInnerTextAsync();
                                  String sText = sTextOrig;
                                  bool found = true;
                                  try
                                  {
                                      if (featuresHover.Checked)
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

                                      if (featuresScroll.Checked)
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

                                      if (featuresGeneric.Checked)
                                      {
                                          sText = sText.Replace("\"on\"+", "\"no\"+");
                                          sText = sText.Replace("\"on\" +", "\"no\" +");
                                          sText = sText.Replace("'on'+", "\"no\"+");
                                          sText = sText.Replace("'on' +", "\"no\" +");
                                      }


                                      if (sText != "")
                                          text = text.Replace(sTextOrig, sText);
                                  }
                                  catch (Exception ex) { }
                              }
            */

            /* if ((sTextOrig.Contains("\"click\"") || sTextOrig.Contains("\"onclick\"") || sTextOrig.Contains("\"message\"") || sTextOrig.Contains("\"onmessage\"")))

             {
                 found = false;
                 int start = sTextOrig.IndexOf("attachEvent");

                 int end = start;
                 while (sTextOrig[end] != 59 && sTextOrig[end] != 38 && sTextOrig[end] != 124)
                 {
                     end++;
                 }

                 if (sTextOrig.Substring(start, end - start).Contains("\"click\"") || sTextOrig.Substring(start, end - start).Contains("\"onclick\"") || sTextOrig.Substring(start, end - start).Contains("\"message\"") || sTextOrig.Substring(start, end - start).Contains("\"onmessage\""))
                 {
                     sTextOrig = sTextOrig.Replace(sTextOrig.Substring(start, end - start), "");

                 }

             }
             found = true;
             while (sTextOrig.Contains("attachEventListener") && (sTextOrig.Contains("\"click\"") || sTextOrig.Contains("\"onclick\"") || sTextOrig.Contains("\"message\"") || sTextOrig.Contains("\"onmessage\"")))
             {
                 found = false;
                 int start = sTextOrig.IndexOf("attachEventListener");

                 int end = start;
                 while (sTextOrig[end] != 59 && sTextOrig[end] != 38 && sTextOrig[end] != 124)
                 {
                     end++;
                 }

                 if (sTextOrig.Substring(start, end - start).Contains("\"click\"") || sTextOrig.Substring(start, end - start).Contains("\"onclick\"") || sTextOrig.Substring(start, end - start).Contains("\"message\"") || sTextOrig.Substring(start, end - start).Contains("\"onmessage\""))
                 {
                     sTextOrig = sTextOrig.Replace(sTextOrig.Substring(start, end - start), "");

                 }

             }
             found = true;
             while (sTextOrig.Contains("addEventListener") &&  (sTextOrig.Contains("\"click\"") || sTextOrig.Contains("\"onclick\"") || sTextOrig.Contains("\"message\"") || sTextOrig.Contains("\"onmessage\"")))
             {
                 found = false;
                 int start = sTextOrig.IndexOf("addEventListener");

                 int end = start;
                 while (sTextOrig[end] != 59 && sTextOrig[end] != 38 && sTextOrig[end] != 124)
                 {
                     end++;
                 }

                 if (sTextOrig.Substring(start, end - start).Contains("\"click\"") || sTextOrig.Substring(start, end - start).Contains("\"onclick\"") || sTextOrig.Substring(start, end - start).Contains("\"message\"") || sTextOrig.Substring(start, end - start).Contains("\"onmessage\""))
                 {
                     sTextOrig = sTextOrig.Replace(sTextOrig.Substring(start, end - start), "");

                 }

             }

             found = true;
             while (sTextOrig.Contains("addEvent") && (sTextOrig.Contains("\"click\"") || sTextOrig.Contains("\"onclick\"") || sTextOrig.Contains("\"message\"") || sTextOrig.Contains("\"onmessage\"")))
             {
                 found = false;
                 int start = sTextOrig.IndexOf("addEvent");

                 int end = start;
                 while (sTextOrig[end] != 59 && sTextOrig[end] != 38 && sTextOrig[end] != 124)
                 {
                     end++;
                 }

                 if (sTextOrig.Substring(start, end - start).Contains("\"click\"") || sTextOrig.Substring(start, end - start).Contains("\"onclick\"") || sTextOrig.Substring(start, end - start).Contains("\"message\"") || sTextOrig.Substring(start, end - start).Contains("\"onmessage\""))
                 if (sTextOrig.Substring(start, end - start).Contains("\"click\"") || sTextOrig.Substring(start, end - start).Contains("\"onclick\"") || sTextOrig.Substring(start, end - start).Contains("\"message\"") || sTextOrig.Substring(start, end - start).Contains("\"onmessage\""))
                 {
                     sTextOrig = sTextOrig.Replace(sTextOrig.Substring(start, end - start), "");

                 }

             }*/

            /*         }


                     if (featuresHover.Checked)
                     {
                         foreach (String s in toReplaceHover)
                         {
                             text = text.Replace("\"" + s + "\"", "");
                             text = text.Replace("'" + s + "'", "");
                             text = text.Replace(s + "=", "");
                             text = text.Replace(s + " =", "");
                             text = text.Replace("." + s, "");
                         }
                     }

                     if (featuresScroll.Checked)
                     {
                         foreach (String s in toReplaceScroll)
                         {
                             text = text.Replace("\"" + s + "\"", "");
                             text = text.Replace("." + s, "");
                             text = text.Replace(s + "=", "");
                             text = text.Replace(s + " =", "");
                             text = text.Replace("." + s, "");
                         }
                     }

                     if (featuresGeneric.Checked)
                     {
                         text = text.Replace("\"on\"+", "\"no\"+");
                         text = text.Replace("\"on\" +", "\"no\" +");
                         text = text.Replace("'on'+", "\"no\"+");
                         text = text.Replace("'on' +", "\"no\" +");
                     }
                     await devToolsContext.SetContentAsync(text);
                 }


             }
             */
            pictureBox1.Visible = false;

        }

        private void WebView21_NavigationStarting(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs e)
        {
            toolStripTextBox1.Text =e.Uri.ToString();
            // webView21.Visible = false;
         //   pictureBox1.Visible = true;


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
            webView21.CoreWebView2.Navigate(toolStripTextBox1.Text);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            toolStripTextBox1.Width = this.Width - 200;
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
            webView21.GoBack();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            webView21.GoForward();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            webView21.Reload();
        }

        private void toolStripTextBox1_Click(object sender, EventArgs e)
        {

        }

        private void toolStripTextBox1_KeyPress(object sender, KeyPressEventArgs e)
        {

            if (e.KeyChar == 13)
            { 
                webView21.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.CacheStorage);
                webView21.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.WebSql);
                webView21.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.DiskCache);
                webView21.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.LocalStorage);
                webView21.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.ServiceWorkers);
                webView21.CoreWebView2.Profile.ClearBrowsingDataAsync(CoreWebView2BrowsingDataKinds.IndexedDb);

                if (toolStripTextBox1.Text.StartsWith("http"))
                    webView21.CoreWebView2.Navigate(toolStripTextBox1.Text);
                else
                    webView21.CoreWebView2.Navigate("https://"+toolStripTextBox1.Text);
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void featuresHover_Click(object sender, EventArgs e)
        {

        }
    }
}
