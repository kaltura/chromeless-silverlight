using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Player
{
    public class FlavorObject
    {
        public FlavorObject(String type, int assetid, int bandwidth, int height)
        {
            this.type = type;
            this.assetid = assetid;
            this.bandwidth = bandwidth;
            this.height = height;
        }

        public String type { get; set; }
        public int assetid { get; set; }
        public int bandwidth { get; set; }
        public int height { get; set; }
    }
}
