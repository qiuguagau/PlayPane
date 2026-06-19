using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;

namespace PlayPane
{
    internal static class WebView2Runtime
    {
        private const string BrowserArguments =
            "--disable-renderer-backgrounding " +
            "--disable-backgrounding-occluded-windows " +
            "--disable-background-timer-throttling";

        private static readonly Task<CoreWebView2Environment> EnvironmentTask = CreateEnvironmentAsync();

        public static Task<CoreWebView2Environment> GetEnvironmentAsync()
        {
            return EnvironmentTask;
        }

        private static Task<CoreWebView2Environment> CreateEnvironmentAsync()
        {
            var options = new CoreWebView2EnvironmentOptions(BrowserArguments);
            return CoreWebView2Environment.CreateAsync(null, null, options);
        }
    }
}
