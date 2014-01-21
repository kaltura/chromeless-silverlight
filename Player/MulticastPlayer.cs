﻿using MediaStreamSrc.Classes;
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
using System.Collections.Generic;

namespace Player
{
    public class MulticastPlayer : ProgressiveMediaElement
    {
        private MulticastReceiver receiver;
        public MulticastPlayer(MediaElement element, string ip = null)
            : base(element)
        {
            this.element = element;
            this.receiver = new MulticastReceiver();
            this.receiver.BeginJoinGroup += receiver_BeginJoinGroup;
            this.receiver.EndJoinGroup += receiver_EndJoinGroup;
            this.receiver.ReceivedFirstPacket += receiver_ReceivedFirstPacket;
            this.receiver.setMediaPlayer(this.element);
            Dictionary<string, string> param = new Dictionary<string, string>();
            if (!String.IsNullOrEmpty(ip))
            {
                param.Add("streamAddress", ip);
            }
            this.receiver.init(param);
        }

        void receiver_ReceivedFirstPacket()
        {
        }

        void receiver_EndJoinGroup(string streamAddress, int streamPort, bool isSuccessful, string errorStr)
        {
        }

        void receiver_BeginJoinGroup(string streamAddress, int streamPort)
        {
        }


    }
}
