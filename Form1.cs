using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using WebView2.DevTools.Dom;
using WebView2.DevTools.Dom.Input;
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

            webView21.NavigationStarting += WebView21_NavigationStarting;
            webView21.NavigationCompleted += WebView21_NavigationCompleted;
            webView21.CoreWebView2InitializationCompleted += WebView21_CoreWebView2InitializationCompleted;
            webView21.EnsureCoreWebView2Async();
          

            adblock = System.IO.File.ReadAllLines(".\\hosts");
            
            
           // webView21.CoreWebView2.Settings.UserAgent = "Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)";
        }

        private void WebView21_CoreWebView2InitializationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2InitializationCompletedEventArgs e)
        {
            webView21.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
         
                webView21.CoreWebView2.AddWebResourceRequestedFilter("*", 0);
       
        }

        private void CoreWebView2_WebResourceRequested(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebResourceRequestedEventArgs e)
        {
            String[] parts = new Uri(e.Request.Uri).Host.Split('.');
            if(parts.Length>1)
                if (adblock.Contains(parts[parts.Length - 2] + "." + parts[parts.Length - 1]))
                    e.Request.Uri = "about:blank";
        }

        String[] adblock;

        String[] toReplace = new string[]
        {
            
           // "on",
            "onmouseenter",
"onmouseleave",   
"onmousemove",
"onmouseout",
"onmouseover",
"onmouseup",        
"mouseleave",
"mousemove",
"mouseout",
"mouseover",
"mouseup",
"hover",
"onhover",
"onwheel",
"wheel",
"scroll",
"onscroll",
"scrolled",
"onscrolled"
        };

        async void WebView21_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            WebView2DevToolsContext devToolsContext;
            devToolsContext = await webView21.CoreWebView2.CreateDevToolsContextAsync();
            String text = await devToolsContext.GetContentAsync();
            WebView2.DevTools.Dom.HtmlElement[] scripts = await devToolsContext.QuerySelectorAllAsync("script");



            foreach (WebView2.DevTools.Dom.HtmlElement script in scripts)
            {

                String sTextOrig = await script.GetInnerTextAsync();
                String sText = sTextOrig;
                bool found = true;

                foreach (String s in toReplace)
                {
                    sText = sText.Replace("\""+s+"\"", "");
                    sText = sText.Replace("'" + s + "'", "");
                    sText = sText.Replace("\"on\"+", "\"no\"+");
                    sText = sText.Replace("\"on\" +", "\"no\" +");
                    sText = sText.Replace("'on'+", "\"no\"+");
                    sText = sText.Replace("'on' +", "\"no\" +");

                }




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

                if (sText != "")
                    text = text.Replace(sTextOrig, sText);
            }
         
            
            await devToolsContext.SetContentAsync(text);

        }

        private void WebView21_NavigationStarting(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs e)
        {
       
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
           
        }
    }
}
