using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace PlayPane.Tests
{
    internal static class OverlayTransparencyTests
    {
        public static void OverlayWindowAllowsDesktopTransparency()
        {
            XDocument document = XDocument.Load(FindSourceFile("src", "PlayPane", "OverlayWindow.xaml"));
            XElement window = document.Root;
            XElement outerBorder = document.Descendants()
                .FirstOrDefault(element => element.Name.LocalName == "Border" && AttributeValue(element, "Name") == "OuterBorder");

            TestAssert.Equal("True", AttributeValue(window, "AllowsTransparency"), "Overlay window must enable desktop transparency instead of staying opaque.");
            TestAssert.Equal("Transparent", AttributeValue(outerBorder, "Background"), "Overlay content background must not block the semi-transparent mirror.");
        }

        public static void WebRtcViewerUsesTransparentBackingSurfaces()
        {
            string overlayCode = File.ReadAllText(FindSourceFile("src", "PlayPane", "OverlayWindow.xaml.cs"));
            string viewerPageCode = File.ReadAllText(FindSourceFile("src", "PlayPane", "WebRtcViewerPage.cs"));

            TestAssert.True(overlayCode.Contains("WebRtcView.DefaultBackgroundColor = System.Drawing.Color.Transparent;"), "WebView2 must use a transparent default background.");
            TestAssert.True(viewerPageCode.Contains("background: transparent;"), "Viewer page must use transparent HTML and video backgrounds.");
            TestAssert.True(viewerPageCode.Contains("body style=\\\"margin:0;background:transparent\\\""), "Blank viewer page must remain transparent between navigations.");
        }

        private static string FindSourceFile(params string[] relativeParts)
        {
            DirectoryInfo directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null)
            {
                string candidate = Path.Combine(new[] { directory.FullName }.Concat(relativeParts).ToArray());
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException("Could not find " + Path.Combine(relativeParts) + " from the current test directory.");
        }

        private static string AttributeValue(XElement element, string localName)
        {
            if (element == null)
            {
                return string.Empty;
            }

            XAttribute attribute = element.Attributes().FirstOrDefault(candidate => candidate.Name.LocalName == localName);
            return attribute == null ? string.Empty : attribute.Value;
        }
    }
}
