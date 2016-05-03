using MediaStreamSrc.Classes;
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
using System.Reflection;
using System.Net.Sockets;
using Microsoft.Web.Media.SmoothStreaming;

namespace Player
{
    public class MulticastPlayer : ProgressiveMediaElement, IMediaElement
    {
        private MulticastReceiver receiver;

        public event MulticastReceiver.ReceivedID3TagDelegate ReceivedID3Tag
        {
            add
            {
                if (this.receiver != null)
                {
                    this.receiver.ReceivedID3Tag += value;
                }
            }
            // Remove the input delegate from the collection.
            remove
            {
                if (this.receiver != null)
                {
                    this.receiver.ReceivedID3Tag -= value;
                }
            }
        }

        public MulticastPlayer(MediaElement element, IDictionary<string, string> initParams, Logger logger)
            : base(element,logger.clone("McastPlayer"))
        {
            logger.info("c-tor");

            this.element = element;
            this.receiver = new MulticastReceiver(logger);
            this.receiver.BeginJoinGroup += receiver_BeginJoinGroup;
            this.receiver.EndJoinGroup += receiver_EndJoinGroup;
            this.receiver.ReceivedFirstPacket += receiver_ReceivedFirstPacket;
      
            this.receiver.setMediaPlayer(this.element);
            this.receiver.init(initParams);
            this.element.Volume = 1.0;
        }

    
        public void stretchFill()
        {
            this.element.Stretch = Stretch.Fill;
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

        public double AverageBitRate
        {
            get
            {
                try
                {
                    var streamSource = receiver.getMediaSteramSource();
                    if (streamSource != null)
                    {
                        return (streamSource as MediaStreamSourceMulticast).AverageBitRate;
                    }
                }
                catch (Exception e)
                {
                    this.logger.warn("Exception in timeoffset " + e);
                }
                return 0;
            }
        }


        public void Pause()
        {
            logger.info("Pause");

            base.Pause();
            receiver.pausePlayer();
        }


        public void Stop()
        {
            logger.info("Stop");

            receiver.stopPlayer();
            base.Stop();
        }

        #region IDisposable
        protected override void Dispose(bool disposing)
        {
            logger.info("Dispose: disposing={0}", disposing);

            if (disposing)
            {
                if (receiver != null)
                {
                    receiver.stopPlayer();
                    this.receiver.BeginJoinGroup -= receiver_BeginJoinGroup;
                    this.receiver.EndJoinGroup -= receiver_EndJoinGroup;
                    this.receiver.ReceivedFirstPacket -= receiver_ReceivedFirstPacket;
                    this.receiver.setMediaPlayer(null);
                    if (receiver is IDisposable)
                    {
                        (receiver as IDisposable).Dispose();
                    }
                    receiver = null;
                }
            }
        }
        #endregion
        
        System.Collections.IDictionary IMediaElement.GetDiagnostics()
        {
            Dictionary<string, string> diags = new Dictionary<string, string>();
            if (this.element != null)
            {
                try
                {
                    diags[DiagnosticsConstants.RenderedFramesPerSecond] = this.element.RenderedFramesPerSecond.ToString("N2");
                    diags[DiagnosticsConstants.DroppedFramesPerSecond] = this.element.DroppedFramesPerSecond.ToString("N2");
                    if (this.receiver != null)
                    {
                        DiagnosticsInfo info;
                        this.receiver.GetDiagnostics(out info);
                        diags[DiagnosticsConstants.InputFrameRate] = info.inputFrameRate.ToString("N2");
                        diags[DiagnosticsConstants.MulticastAddress] = info.streamAdress;
                        diags[DiagnosticsConstants.CurrentBitrate] = (info.currentBitRate / 1024).ToString("N2") + " Kbps";
                        
                        int droppedPackets = (int)(info.videoLostPackets + info.audioLostPackets),
                            receivedPackets = (int)(info.videoTotalPackets + info.audioTotalPackets);
                        double dropRate = receivedPackets > 0 ? droppedPackets / (double)receivedPackets : 0.0;
               
                        diags[DiagnosticsConstants.PacketLoss] = string.Format( "{0} ({1:N2} %)",droppedPackets,dropRate);
                        diags[DiagnosticsConstants.PacketRate] = receivedPackets.ToString() ;
                    }
                }
                catch(Exception e)
                {
                    this.logger.warn("diagnostics error: {0}", e);
                }
            }   
            return diags;
        }

        public new Double getCurrentBufferLength()
        {
            Dictionary<string, string> myDictionary = GetDiagnostics() as Dictionary<string, string>;
            var videoBuffer = myDictionary[DiagnosticsConstants.VideoBuffer];
            Double bufferLength;
            if (Double.TryParse(videoBuffer, out bufferLength))
            {
                return bufferLength;
            } else
            {
                return 0;
            }

        }
    }
}
