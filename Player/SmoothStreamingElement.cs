﻿using Microsoft.Web.Media.SmoothStreaming;
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
using System.Linq;
using MediaStreamSrc.Classes;
using System.Text;
using System.Windows.Threading;
using System.Xml.Linq;

namespace Player
{
    public class SmoothStreamingElement: IMediaElement    , IDisposable       
    {
        public SmoothStreamingMediaElement element { get; set; }

        private StreamInfo playingStream;
        private List<TrackInfo> tracks;
        private List<StreamInfo> audioTracks;
        protected Logger logger; 
        private List<StreamInfo> textTracks;
        private List<ChunkInfo> textChunks;
        private StreamInfo currentTextTrack = null;
        private DispatcherTimer _capt_timer;
        private Boolean textTrackLoaded = false;
        private const int CAPT_FRAGMENT_COUNT = 20;
        private const int CAPT_TIMER_INTERVAL = 10; // seconds

        public SmoothStreamingElement(SmoothStreamingMediaElement element, Logger logger)
        {
            this.logger = logger;
            this.element = element;
            this.element.CurrentStateChanged += element_CurrentStateChanged;
            this.element.BufferingProgressChanged += element_BufferingProgressChanged;
            this.element.DownloadProgressChanged += element_DownloadProgressChanged;

            this.element.MediaEnded += element_MediaEnded;
            this.element.MediaFailed += element_MediaFailed;
            this.element.MediaOpened += element_MediaOpened;
            this.element.MouseLeftButtonUp += element_MouseLeftButtonUp;
            this.element.MarkerReached += element_MarkerReached;

            this.element.PlaybackTrackChanged += element_PlaybackTrackChanged;
            this.element.ManifestReady += element_ManifestReady;

        }

        public void selectTrack(int trackIndex)
        {
            if (this.playingStream!=null)
            {
                IList<TrackInfo> selectedList = new List<TrackInfo>();
                bool flushBuffer = false;
                if (trackIndex == -1)
                {
                    selectedList = this.tracks;
                } else
                {
                    TrackInfo selected = this.tracks[trackIndex];                    
                    selectedList.Add(selected);
                    flushBuffer = true;
                }
                this.playingStream.SelectTracks(selectedList, flushBuffer);
            }
        }

        public void selectAudioTrack(int trackIndex)
        {
            if (audioTracks != null && audioTracks.Count > trackIndex)
            {
                var newAudioStream = audioTracks[trackIndex];
                var segment = element.ManifestInfo.Segments[element.CurrentSegmentIndex.Value];
                var newStreams = new List<StreamInfo>();
                // use current video streams
                var selectedVideoStreams = segment.SelectedStreams.Where(i => i.Type != MediaStreamType.Audio).ToList();
                newStreams.AddRange(selectedVideoStreams);
                // add a new audio stream
                newStreams.Add(newAudioStream);
                // replace old streams by new ones
                
                this.element.ManifestInfo.SelectStreamsCompleted += ManifestInfo_SelectStreamsCompleted;
                segment.SelectStreamsAsync(newStreams);
                 this.element.FlushBuffers(new TimeSpan(0), true, true);
            } 
        }

        private void ManifestInfo_SelectStreamsCompleted(object sender, StreamUpdatedListEventArgs e)
        {
            this.element.ManifestInfo.SelectStreamsCompleted -= ManifestInfo_SelectStreamsCompleted;
            this.element.FlushBuffers(new TimeSpan(0), true, true);            
            if (CurrentAudioStreamChanged != null)
            {
                CurrentAudioStreamChanged(sender, e);
            }
        }

        public void selectTextTrack(int trackIndex)
        {            
            if (textTracks != null && textTracks.Count > trackIndex)
            {
                currentTextTrack = textTracks[trackIndex];
                var segment = element.ManifestInfo.Segments[element.CurrentSegmentIndex.Value];
                var newStreams = new List<StreamInfo>();
                // use current video streams
                var selectedVideoStreams = segment.SelectedStreams.Where(i => i.Type != MediaStreamType.Script).ToList();
                newStreams.AddRange(selectedVideoStreams);
                // add a new text stream
                newStreams.Add(currentTextTrack);
                // replace old streams by new ones
                segment.SelectStreamsAsync(newStreams);

                textChunks = currentTextTrack.ChunkList.ToList<ChunkInfo>();
                //clear previous language markers
                this.element.Markers.Clear();
                textTrackLoaded = false;
                getNextTextChunks(null, null);
                if (_capt_timer == null)
                {
                    _capt_timer = new DispatcherTimer();
                    _capt_timer.Interval = new TimeSpan(0, 0, 0, CAPT_TIMER_INTERVAL, 0); // 10 seconds 
                    _capt_timer.Tick += getNextTextChunks;
                }


                if (element.CurrentState == SmoothStreamingMediaElementState.Playing)
                {
                    _capt_timer.Start();
                }
                else
                {
                    _capt_timer.Stop();
                }                    
            }            
        }

        private void getNextTextChunks(object sender, EventArgs e)
        {
            if (currentTextTrack != null && textChunks!= null)
            {
                //get upcoming text chunks
                List<ChunkInfo> chunks = textChunks.Where(i => i.TimeStamp >= this.Position).ToList();
                //read max 20 at a time
                int size = Math.Min(CAPT_FRAGMENT_COUNT, chunks.Count);
                TrackInfo trackInfo = currentTextTrack.SelectedTracks[0];
                for (int i = 0; i < size; i++)
                {
                    IAsyncResult ar =
                               trackInfo.BeginGetChunk(
                               chunks[i].TimeStamp, new AsyncCallback(AddMarkers), currentTextTrack.UniqueId);
                    //data was retrieved, remove from original list
                    textChunks.Remove(chunks[i]);
                }

                if (textChunks.Count == 0 && _capt_timer != null)
                {
                    _capt_timer.Stop();
                    _capt_timer = null;
                }
            }
        }

        public int getCurrentAudioIndex()
        {
            if ( audioTracks!=null && audioTracks.Count > 1 ) {
                var segment = element.ManifestInfo.Segments[element.CurrentSegmentIndex.Value];
                var currentAudioStream = segment.SelectedStreams.Where(i => i.Type == MediaStreamType.Audio).FirstOrDefault();
                for (int i = 0; i < audioTracks.Count; i++)
                {
                    if (audioTracks[i].Equals(currentAudioStream))
                        return i;
                }
            }            
             return -1;
        }

        public int getCurrentTextIndex()
        {
            if (textTracks != null && textTracks.Count > 0)
            {
                var segment = element.ManifestInfo.Segments[element.CurrentSegmentIndex.Value];
                var currentText = segment.SelectedStreams.Where(i => i.Type == MediaStreamType.Script).FirstOrDefault();
                for (int i = 0; i < textTracks.Count; i++)
                {
                    if (textTracks[i].Equals(currentText))
                        return i;
                }
            }
            return -1;
        }


        void element_PlaybackTrackChanged(object sender, TrackChangedEventArgs e)
        {
            if (this.tracks != null)	
            {
                SourceEventArgs args;		
                for (int i = 0; i < this.tracks.Count; i++)
                {
                    if (this.tracks[i].Bitrate == e.NewTrack.Bitrate)
                    {
                        args = new SourceEventArgs(i);
                        SourceChanged(this, args);
                        break;
                    }
                }
            }  
        }

        #region IMediaElement implementation

        public event EventHandler<RoutedEventArgs> CurrentStateChanged;

        public event EventHandler<RoutedEventArgs> BufferingProgressChanged;

        public event EventHandler<RoutedEventArgs> DownloadProgressChanged;

        public event EventHandler<RoutedEventArgs> MediaEnded;

        public event EventHandler<ExceptionRoutedEventArgs> MediaFailed;

        public event EventHandler<RoutedEventArgs> MediaOpened;

        public event EventHandler<MouseButtonEventArgs> MouseLeftButtonUp;

        public event EventHandler<ManifestEventArgs> BitratesReady;

        public event EventHandler<ManifestEventArgs> AudioTracksReady;

        public event EventHandler<ManifestEventArgs> TextTracksReady;

        public event EventHandler<SourceEventArgs> TextTrackLoaded;
        
        public event EventHandler<SourceEventArgs> SourceChanged;

        public event EventHandler<StreamUpdatedListEventArgs> CurrentAudioStreamChanged;

        public event EventHandler<TimelineMarkerRoutedEventArgs> MarkerReached;
   

        void element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (MouseLeftButtonUp != null)
            {
                MouseLeftButtonUp(sender, e);
            }
        }

        void element_ManifestReady(object sender, EventArgs e)
        {   
        }

        void element_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (this.element.IsLive)
            {
                this.element.StartSeekToLive();                
            }
            
            foreach (SegmentInfo segment in this.element.ManifestInfo.Segments)
            {                
                audioTracks = new List<StreamInfo>();
                textTracks = new List<StreamInfo>();
                IList<StreamInfo> streamInfoList = segment.AvailableStreams;
                List<StreamInfo> selectStreams = segment.SelectedStreams.ToList<StreamInfo>();
                
                foreach (StreamInfo stream in streamInfoList)
                {
                    if (stream.Type == MediaStreamType.Video)
                    {
                        playingStream = stream;
                        tracks = stream.AvailableTracks.ToList<TrackInfo>();

                        ManifestEventArgs args = new ManifestEventArgs(tracks.ToList<Object>());
                        BitratesReady(this, args);
                    }
                    else if (stream.Type == MediaStreamType.Audio)
                    {
                        audioTracks.Add(stream);
                    }
                    //subtitles
                    else if (stream.Type == MediaStreamType.Script && stream.Subtype == "CAPT")
                    {
                        textTracks.Add(stream); 
                    }
                }
                                
                if (MediaOpened != null)
                {
                    MediaOpened(sender, e);
                }
                ManifestEventArgs audioArgs = new ManifestEventArgs(audioTracks.ToList<Object>());                
                AudioTracksReady(this, audioArgs);
                
                ManifestEventArgs textArgs = new ManifestEventArgs(textTracks.ToList<Object>());
                TextTracksReady(this, textArgs);
            }            
        }

        void element_MarkerReached(object sender, TimelineMarkerRoutedEventArgs e)
        {
            if (MarkerReached != null)
            {
                MarkerReached(sender, e);
            }
        }

        private void AddMarkers(IAsyncResult argAR)
        {
            try {
                if (!Deployment.Current.Dispatcher.CheckAccess())
                {
                    Deployment.Current.Dispatcher.BeginInvoke(() => AddMarkers(argAR));
                }

                foreach (SegmentInfo segmentInfo in this.element.ManifestInfo.Segments)
                {
                    foreach (StreamInfo streamInfo in segmentInfo.SelectedStreams)
                    {
                        if (streamInfo.UniqueId == ((string)argAR.AsyncState))
                        {
                            foreach (TrackInfo trackInfo in streamInfo.SelectedTracks)
                            {
                                ChunkResult chunkResult = trackInfo.EndGetChunk(argAR);

                                if (chunkResult.Result == ChunkResult.ChunkResultState.Succeeded && currentTextTrack != null)
                                {
                                    System.Text.Encoding enc = System.Text.Encoding.UTF8;
                                    int length = (int)chunkResult.ChunkData.Length;
                                    byte[] rawData = new byte[length];
                                    chunkResult.ChunkData.Read(rawData, 0, length);
                                    String text = enc.GetString(rawData, 0, rawData.Length);
                                    XElement xElem = XElement.Parse(text);
                                    XElement bodyElem = xElem.Elements().FirstOrDefault(e => e.Name.LocalName == "body");

                                    //first received chunk - notify js
                                    if (!textTrackLoaded)
                                    {
                                        XElement copyElement = new XElement(xElem);
                                        XElement copyBodyElem = copyElement.Elements().FirstOrDefault(e => e.Name.LocalName == "body");
                                        //we can't send the body elements, they are causing "Eval" exception
                                        copyBodyElem.RemoveAll();
                                        SourceEventArgs args = new SourceEventArgs(getCurrentTextIndex());
                                        args.Text = copyElement.ToString();
                                        TextTrackLoaded(this, args);
                                        textTrackLoaded = true;
                                    }                                    
                                    
                                    //Get the caption language
                                    string langName = "";
                                    currentTextTrack.Attributes.TryGetValue("Name", out langName);

                                    //Fix for TTML where div contains begin and end values instead of paragraph node
                                    int segId = 0;
                                    //Get all div under the body node
                                    IEnumerable<XElement> divElements =
                                            from div in bodyElem.Descendants()
                                            where div.Name.LocalName == "div"
                                            select div;
                                    //Fix alogorithem:
                                    //1. Iterate over each div.
                                    //2. Find each begin and end values.
                                    //3. Aggregate over each child of a div, concat it text value.
                                    //4. Create a new paragraph element with the div begin and end attributes.
                                    //5. Clear all the attribute and elements of the original div and replace with new paragraph.
                                    foreach (XElement divElem in divElements) {
                                        XAttribute begin = divElem.Attributes().FirstOrDefault(e => e.Name.LocalName == "begin");
                                        XAttribute end = divElem.Attributes().FirstOrDefault(e => e.Name.LocalName == "end");

                                        if (begin != null)
                                        {
                                            IEnumerable<string> textSegs =
                                                from seg in divElem.Descendants()
                                                where seg.Name.LocalName == "p"
                                                select (string)seg;

                                            string str = textSegs.Aggregate((x, y) => x + "<br/>" + y);
                                            
                                            XElement pElem = divElem.Elements().FirstOrDefault(e => e.Name.LocalName == "p");
                                            XAttribute region = pElem.Attributes().FirstOrDefault(e => e.Name.LocalName == "region");

                                            divElem.RemoveAll();

                                            XAttribute idAttr = new XAttribute("id", "p" + segId);
                                            XElement newP = new XElement("p", begin, end, region, idAttr);
                                            newP.Value = str;
                                            divElem.Add(newP);
                                            segId++;
                                        }
                                    }
                                    //Turn each caption line to a media marker
                                    foreach (XElement el in bodyElem.Elements())
                                    {                                        
                                        IEnumerable<XElement> pSegs =
                                                from p in el.Descendants()
                                                where p.Name.LocalName == "p"
                                                select p;

                                        foreach (XElement pSeg in pSegs)
                                        {
                                            TimelineMarker newMarker = new TimelineMarker();
                                            //Create discrete marker points for each segment
                                            pSeg.Add(new XAttribute("timestamp", chunkResult.Timestamp.TotalSeconds.ToString()));
                                            newMarker.Text = pSeg.ToString();
                                            newMarker.Type = langName;
                                            XAttribute begin = pSeg.Attribute("begin");
                                            if (begin.Value != null)
                                            {                                                
                                                newMarker.Time = chunkResult.Timestamp + TimeSpan.Parse(begin.Value);
                                                this.element.Markers.Add(newMarker);                                                
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }                
            }catch(Exception ex)
            {
                logger.info("Error occur while trying to add marker:" + ex.Message);
            }
        }

        void element_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (MediaFailed != null)
            {
                MediaFailed(sender, e);
            }
        }

        void element_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (MediaEnded != null)
            {
                MediaEnded(sender, e);
            }
        }

        void element_DownloadProgressChanged(object sender, RoutedEventArgs e)
        {
            if (DownloadProgressChanged != null)
            {
                DownloadProgressChanged(sender, e);
            }
        }

        void element_BufferingProgressChanged(object sender, RoutedEventArgs e)
        {
            if (BufferingProgressChanged != null)
            {
                BufferingProgressChanged(sender, e);
            }
        }

        void element_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            if (CurrentStateChanged != null)
            {
                CurrentStateChanged(sender, e);
            }

            if (_capt_timer != null)
            {
                if (element.CurrentState == SmoothStreamingMediaElementState.Playing)
                {
                    _capt_timer.Start();
                }
                else if (element.CurrentState == SmoothStreamingMediaElementState.Paused || element.CurrentState == SmoothStreamingMediaElementState.Stopped)
                {
                    _capt_timer.Stop();
                } 
            }
            
        }

        public bool AutoPlay
        {
            get
            {
                return element.AutoPlay;
            }
            set
            {
                element.AutoPlay = value;
            }
        }

        public double Volume
        {
            get
            {
                return element.Volume;
            }
            set
            {
                element.Volume = value;
            }
        }

        public void Play()
        {
            element.Play();
        }

        public void Pause()
        {
            element.Pause();
        }

        public LicenseAcquirer LicenseAcquirer
        {
            get
            {
                return element.LicenseAcquirer;
            }
            set
            {
                element.LicenseAcquirer = value;
            }
        }

        public Uri Source
        {
            get
            {
                return element.SmoothStreamingSource;
            }
            set
            {
                element.SmoothStreamingSource = value;
            }
        }

        public void Stop()
        {
            element.Stop();
        }

        public bool IsMuted
        {
            get
            {
                return element.IsMuted;
            }
            set
            {
                element.IsMuted = value;
            }
        }

        public Double Width
        {
            get
            {
                return element.Width;
            }
            set
            {
                element.Width = value;
            }
        }

        public Double Height
        {
            get
            {
                return element.Height;
            }
            set
            {
                element.Height = value;
            }
        }

        public MediaElementState CurrentState
        {
            get
            {
                return (MediaElementState)((int)element.CurrentState);
            }
            
        }

        public TimeSpan Position
        {
            get
            {
                return element.Position;
            }
            set
            {
                element.Position = value;
            }
        }

        public TimeSpan StartPosition
        {
            get
            {
                return element.StartPosition;
            }
        }

        public Duration NaturalDuration
        {
            get
            {
                return element.NaturalDuration;
            }
        }

        public double DownloadProgress
        {
            get
            {
                return element.DownloadProgress;
            }
        }

        public double BufferingProgress
        {
            get
            {
                return element.BufferingProgress;
            }
        }
        #endregion

        #region IDisposable
        ~SmoothStreamingElement()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
        }
        #endregion


        public System.Collections.IDictionary GetDiagnostics()
        {
            Dictionary<string, string> diags = new Dictionary<string, string>();
            if (this.element != null)
            {
                try
                {
                    diags[DiagnosticsLiveConstants.IsLive] = this.element.IsLive.ToString();
                    diags[DiagnosticsLiveConstants.IsLivePosition] = this.element.IsLivePosition.ToString();
                    diags[DiagnosticsLiveConstants.LiveBackOff] = this.element.LiveBackOff.ToString();
                    diags[DiagnosticsLiveConstants.LivePlaybackOffset] = this.element.LivePlaybackOffset.ToString();
                    diags[DiagnosticsLiveConstants.LivePlaybackStartPosition] = this.element.LivePlaybackStartPosition.ToString();
                    diags[DiagnosticsLiveConstants.LivePosition] = this.element.LivePosition.ToString();
                }
                catch (Exception e)
                {
                    this.logger.warn("diagnostics error: {0}", e);
                }
            }
            return diags;
        }

        public Double getCurrentBufferLength()
        {
            return 0;
        }
    }
}
