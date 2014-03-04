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
    public class SourceEventArgs : EventArgs
    {

        public SourceEventArgs(int newIndex)
        {
            this.NewIndex = newIndex;
        }

        public int NewIndex { get; private set; }
    }
}
