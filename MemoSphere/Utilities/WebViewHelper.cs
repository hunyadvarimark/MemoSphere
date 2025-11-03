using Microsoft.Web.WebView2.Wpf;
using System;
using System.Text.Json;
using System.Windows;

namespace WPF.Utilities
{
    public static class WebViewHelper
    {
        public static readonly DependencyProperty HtmlSourceProperty =
            DependencyProperty.RegisterAttached(
                "HtmlSource",
                typeof(string),
                typeof(WebViewHelper),
                new PropertyMetadata(null, OnHtmlSourceChanged));

        public static string GetHtmlSource(DependencyObject obj)
        {
            return (string)obj.GetValue(HtmlSourceProperty);
        }

        public static void SetHtmlSource(DependencyObject obj, string value)
        {
            obj.SetValue(HtmlSourceProperty, value);
        }

        private static void OnHtmlSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is WebView2 webView && e.NewValue is string content)
            {
                if (webView.CoreWebView2 != null)
                {
                    NavigateToHtmlContent(webView, content);
                }
                else
                {
                    webView.CoreWebView2InitializationCompleted += (s, args) =>
                    {
                        if (args.IsSuccess)
                        {
                            NavigateToHtmlContent(webView, content);
                        }
                    };
                    _ = webView.EnsureCoreWebView2Async();
                }
            }
        }

        private static void NavigateToHtmlContent(WebView2 webView, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                webView.NavigateToString("<html><body><p style='color: gray; padding: 20px;'>Üres tartalom</p></body></html>");
                return;
            }

            string html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    
    <!-- Marked.js - Markdown renderelés -->
    <script src=""https://cdn.jsdelivr.net/npm/marked/marked.min.js""></script>
    
    <!-- MathJax - LaTeX renderelés -->
    <script>
        window.MathJax = {{
            tex: {{
                inlineMath: [['$', '$'], ['\\(', '\\)']],
                displayMath: [['$$', '$$'], ['\\[', '\\]']],
                processEscapes: true,
                processEnvironments: true
            }},
            options: {{
                skipHtmlTags: ['script', 'noscript', 'style', 'textarea', 'pre']
            }},
            startup: {{
                pageReady: () => {{
                    return MathJax.startup.defaultPageReady();
                }}
            }}
        }};
    </script>
    <script src=""https://cdn.jsdelivr.net/npm/mathjax@3/es5/tex-mml-chtml.js""></script>
    
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #2C3E50;
            padding: 20px;
            background-color: white;
            margin: 0;
        }}
        h1, h2, h3 {{ 
            color: #2C3E50; 
            margin-top: 1.5em; 
            margin-bottom: 0.5em; 
        }}
        h1 {{ 
            font-size: 1.8em; 
            border-bottom: 2px solid #3498db; 
            padding-bottom: 0.3em; 
        }}
        h2 {{ font-size: 1.5em; }}
        h3 {{ font-size: 1.2em; }}
        p {{ margin: 0.8em 0; }}
        ul, ol {{ margin: 0.8em 0; padding-left: 2em; }}
        li {{ margin: 0.4em 0; }}
        code {{
            background-color: #f4f4f4;
            padding: 2px 6px;
            border-radius: 3px;
            font-family: 'Courier New', monospace;
            font-size: 0.9em;
        }}
        pre {{
            background-color: #f4f4f4;
            padding: 15px;
            border-radius: 5px;
            overflow-x: auto;
        }}
        pre code {{
            background-color: transparent;
            padding: 0;
        }}
        blockquote {{
            border-left: 4px solid #3498db;
            margin: 1em 0;
            padding-left: 1em;
            color: #555;
        }}
        strong {{ color: #2C3E50; }}
        
        /* ✅ MathJax formázás */
        mjx-container {{
            display: inline-block;
            margin: 0.2em 0;
        }}
        mjx-container[display=""true""] {{
            display: block;
            text-align: center;
            margin: 1em 0;
        }}
    </style>
</head>
<body>
    <div id=""content""></div>
    
    <script>
        const markdownText = {JsonSerializer.Serialize(content)};
        
        // 1. Markdown → HTML (Marked.js)
        const htmlContent = marked.parse(markdownText);
        document.getElementById('content').innerHTML = htmlContent;
        
        // 2. MathJax renderelés (LaTeX képletek)
        if (window.MathJax) {{
            MathJax.typesetPromise([document.getElementById('content')])
                .then(() => {{
                    console.log('✅ MathJax rendering complete');
                }})
                .catch((err) => {{
                    console.error('❌ MathJax error:', err);
                }});
        }}
    </script>
</body>
</html>
";

            webView.NavigateToString(html);
        }
    }
}