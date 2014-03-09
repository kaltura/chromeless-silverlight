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

namespace Player
{
    public class SmoothStreamingElement: IMediaElement          
    {
        public SmoothStreamingMediaElement element { get; set; }

        private StreamInfo playingStream;
        private List<TrackInfo> tracks;
        public SmoothStreamingElement(SmoothStreamingMediaElement element)
        {
            this.element = element;
            this.element.CurrentStateChanged += element_CurrentStateChanged;
            this.element.BufferingProgressChanged += element_BufferingProgressChanged;
            this.element.DownloadProgressChanged += element_DownloadProgressChanged;

            this.element.MediaEnded += element_MediaEnded;
            this.element.MediaFailed += element_MediaFailed;
            this.element.MediaOpened += element_MediaOpened;
            this.element.MouseLeftButtonUp += element_MouseLeftButtonUp;

             this.element.PlaybackTrackChanged += element_PlaybackTrackChanged;
            this.element.ManifestReady += element_ManifestReady;

        }

        public void selectTrack(int trackIndex)
        {
            if (this.playingStream!=null)
            {
                TrackInfo selected = this.tracks[trackIndex];
                IList<TrackInfo> selectedList = new List<TrackInfo>();
                selectedList.Add(selected);
                this.playingStream.SelectTracks(selectedList, false);
            }
        }


        void element_PlaybackTrackChanged(object sender, TrackChangedEventArgs e)
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

        #region IMediaElement implementation

        public event EventHandler<RoutedEventArgs> CurrentStateChanged;

        public event EventHandler<RoutedEventArgs> BufferingProgressChanged;

        public event EventHandler<RoutedEventArgs> DownloadProgressChanged;

        public event EventHandler<RoutedEventArgs> MediaEnded;

        public event EventHandler<ExceptionRoutedEventArgs> MediaFailed;

        public event EventHandler<RoutedEventArgs> MediaOpened;

        public event EventHandler<MouseButtonEventArgs> MouseLeftButtonUp;

        public event EventHandler<ManifestEventArgs> BitratesReady;

        public event EventHandler<SourceEventArgs> SourceChanged;
   

        void element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (MouseLeftButtonUp != null)
            {
                MouseLeftButtonUp(sender, e);
            }
        }

        void element_ManifestReady(object sender, EventArgs e)
        {
            foreach (SegmentInfo segment in this.element.ManifestInfo.Segments)
            {
                IList<StreamInfo> streamInfoList = segment.AvailableStreams;
                foreach (StreamInfo stream in streamInfoList)
                {
                    if (stream.Type == MediaStreamType.Video)
                    {
                        this.playingStream = stream;
                        this.tracks = stream.AvailableTracks.ToList<TrackInfo>();

                        ManifestEventArgs args = new ManifestEventArgs( this.tracks );
                        BitratesReady(this, args);

                        
                        // Limit bit-rate to 866000.
                        /* ulong highRate = 866000 + 1;
                            List<TrackInfo> tracks = new List<TrackInfo>();

                            tracks = ;
                            IList<TrackInfo> allowedTracks = tracks.Where((ti) => ti.Bitrate < highRate).ToList();
                            stream.SelectTracks(allowedTracks, false);*/
                    }
                }
            }
        
        }

        void element_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (MediaOpened != null)
            {
                MediaOpened(sender, e);
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
    }
}