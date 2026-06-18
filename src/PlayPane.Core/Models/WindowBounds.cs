using System.Runtime.Serialization;

namespace PlayPane.Core.Models
{
    [DataContract]
    public sealed class WindowBounds
    {
        public WindowBounds()
        {
        }

        public WindowBounds(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        [DataMember(Order = 1)]
        public int X { get; set; }

        [DataMember(Order = 2)]
        public int Y { get; set; }

        [DataMember(Order = 3)]
        public int Width { get; set; }

        [DataMember(Order = 4)]
        public int Height { get; set; }

        public bool IsUsable
        {
            get { return Width >= 80 && Height >= 80; }
        }
    }
}
