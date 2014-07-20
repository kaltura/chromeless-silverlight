using Microsoft.Web.Media.SmoothStreaming;
using System;
using System.Collections.Generic;
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
    public class ManifestEventArgs : EventArgs
    {
        public ManifestEventArgs(List<Object> flavors)
        {
            this.Flavors = flavors;
        }

        public List<Object> Flavors { get; private set; }
    }
}
