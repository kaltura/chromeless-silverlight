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
using System.IO;
using System.Threading;


namespace Player
{
    public class MulticastInitFailedArgs : EventArgs
    {
        public string Description
        {
            private set;
            get;
        }
        public MulticastInitFailedArgs(string description)
        {
            Description = description;
        }
    }
    public class MulticastPlayer : ProgressiveMediaElement, IMediaElement
    {
        private MulticastReceiver receiver;
        IDictionary<string, string> initParams;
        string m_streamUrl; 
        
        private System.Windows.Threading.DispatcherTimer m_keepAliveTimer = new System.Windows.Threading.DispatcherTimer();
        private bool m_bDisposed=false;

        static TimeSpan KeepAliveInterval = TimeSpan.FromSeconds(10);


        public event EventHandler<MulticastInitFailedArgs> MulticastInitFailed; 

        public MulticastPlayer(MediaElement element, IDictionary<string, string> initParams, Logger logger)
            : base(element,logger.clone("McastPlayer"))
        {
            logger.info("c-tor");

            this.initParams = initParams;
            this.element = element;
            this.receiver = new MulticastReceiver(logger);
            this.receiver.BeginJoinGroup += receiver_BeginJoinGroup;
            this.receiver.EndJoinGroup += receiver_EndJoinGroup;
            this.receiver.ReceivedFirstPacket += receiver_ReceivedFirstPacket;
            this.receiver.setMediaPlayer(this.element);
            this.element.Volume = 1.0;

            m_keepAliveTimer.Tick += KeepAlive;
            m_keepAliveTimer.Interval = KeepAliveInterval;
        }



        public void Init()
        {
            m_streamUrl = this.initParams["streamAddress"];
            if (m_streamUrl.StartsWith("http"))
            {
                KeepAlive(null, EventArgs.Empty);
            }
            else
            {
                m_streamInfo = MulticastStreamInfo.FromDictionary(this.initParams);
                this.receiver.init(m_streamInfo);
            }
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
            if (!isSuccessful)
            {
                try
                {
                    MulticastInitFailed(this, new MulticastInitFailedArgs(errorStr));
                }
                catch
                {

                }
            }
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

        public TimeSpan TimeOffset
        {
            get
            {
                try
                {
                    var streamSource = receiver.getMediaSteramSource();
                    if (streamSource != null)
                    {
                        return (streamSource as MediaStreamSourceMulticast).TimecodeOffset;
                    }
                }
                catch (Exception e)
                {
                    logger.warn("Exception in timeoffset " + e);
                }
                return TimeSpan.Zero;
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
                    m_keepAliveTimer.Stop();
                    m_bDisposed = true;
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

        private MulticastStreamInfo m_streamInfo = null;
        private void KeepAlive(object state, EventArgs args)
        {
            if (m_bDisposed)
            {
                return ;
            }
            m_keepAliveTimer.Stop();
            var request = HttpWebRequest.CreateHttp(m_streamUrl);
            request.BeginGetResponse(result =>
            {
                if (m_bDisposed)
                {
                    return;
                }
                MulticastStreamInfo newStreamInfo = null;
                try
                {
                    var response = request.EndGetResponse(result);

                    Stream dataStream = response.GetResponseStream();
                    // Open the stream using a StreamReader for easy access.
                    StreamReader reader = new StreamReader(dataStream);
                    // Read the content.
                    string responseFromServer = reader.ReadToEnd();

                    newStreamInfo = MulticastStreamInfo.FromJson(responseFromServer);
                }
                catch (Exception e)
                {
                    logger.info("Exception in keep alive {0}", e);
                }

                Deployment.Current.Dispatcher.BeginInvoke(() =>
                {
                    try
                    {
                        if (m_bDisposed)
                        {
                            return;
                        }


                        if (m_streamInfo == null)
                        {
                            m_streamInfo = newStreamInfo;
                            this.receiver.init(newStreamInfo);
                        }
                    }
                    catch (Exception e)
                    {
                        logger.info("Exception in keep alive {0}", e);
                    }
                    finally
                    {
                        m_keepAliveTimer.Start();
                    }
                });
            }, null);
        }
    }
}
