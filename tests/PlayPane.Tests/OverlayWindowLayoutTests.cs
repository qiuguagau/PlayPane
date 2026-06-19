using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace PlayPane.Tests
{
    internal static class OverlayWindowLayoutTests
    {
        public static void ToolbarUsesWrappingLayout()
        {
            XDocument document = XDocument.Load(FindOverlayWindowXaml());
            XElement toolbar = document.Descendants()
                .FirstOrDefault(element => element.Name.LocalName == "Border" && AttributeValue(element, "Name") == "Toolbar");

            TestAssert.True(toolbar != null, "Overlay toolbar should exist.");

            XElement wrapPanel = toolbar.Descendants()
                .FirstOrDefault(element => element.Name.LocalName == "WrapPanel");

            TestAssert.True(wrapPanel != null, "Overlay toolbar should wrap controls when the mirror window is narrow.");
            TestAssert.Equal("Horizontal", AttributeValue(wrapPanel, "Orientation"), "Toolbar controls should wrap horizontally.");
            TestAssert.True(wrapPanel.Descendants().Any(element => element.Name.LocalName == "Button" && AttributeValue(element, "Name") == "StopButton"), "Stop button should remain inside the wrapping toolbar controls.");
        }

        private static string FindOverlayWindowXaml()
        {
            DirectoryInfo directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, "src", "PlayPane", "OverlayWindow.xaml");
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new FileNotFoundException("Could not find OverlayWindow.xaml from the current test directory.");
        }

        private static string AttributeValue(XElement element, string localName)
        {
            XAttribute attribute = element.Attributes().FirstOrDefault(candidate => candidate.Name.LocalName == localName);
            return attribute == null ? string.Empty : attribute.Value;
        }
    }
}
