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

namespace Player
{
    public class ProgressiveMediaElement : IMediaElement, IDisposable
    {
        protected Logger logger; 

        public MediaElement element { get; set; }
        public ProgressiveMediaElement(MediaElement element,Logger logger)
        {
            this. logger = logger;
            this.element = element;
            this.element.CurrentStateChanged += element_CurrentStateChanged;
            this.element.BufferingProgressChanged += element_BufferingProgressChanged;
            this.element.DownloadProgressChanged += element_DownloadProgressChanged;

            this.element.MediaEnded += element_MediaEnded;
            this.element.MediaFailed += element_MediaFailed;
            this.element.MediaOpened += element_MediaOpened;
            this.element.MouseLeftButtonUp += element_MouseLeftButtonUp;

        }
        #region IMediaElement implementation

        public event EventHandler<RoutedEventArgs> CurrentStateChanged;

        public event EventHandler<RoutedEventArgs> BufferingProgressChanged;

        public event EventHandler<RoutedEventArgs> DownloadProgressChanged;

        public event EventHandler<RoutedEventArgs> MediaEnded;

        public event EventHandler<ExceptionRoutedEventArgs> MediaFailed;

        public event EventHandler<RoutedEventArgs> MediaOpened;

        public event EventHandler<MouseButtonEventArgs> MouseLeftButtonUp;



        void element_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.logger.debug("element_MouseLeftButtonUp"); 
            if (MouseLeftButtonUp != null)
            {
                MouseLeftButtonUp(sender, e);
            }
        }

        void element_MediaOpened(object sender, RoutedEventArgs e)
        {
            this.logger.info("element_MediaOpened"); 
   
            if (MediaOpened != null)
            {
                MediaOpened(sender, e);
            }
        }

        void element_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            this.logger.info("element_MediaFailed"); 
   
            if (MediaFailed != null)
            {
                MediaFailed(sender, e);
            }
        }

        void element_MediaEnded(object sender, RoutedEventArgs e)
        {
            this.logger.info("element_MediaEnded"); 
   
            if (MediaEnded != null)
            {
                MediaEnded(sender, e);
            }
        }

        void element_DownloadProgressChanged(object sender, RoutedEventArgs e)
        {
            this.logger.info("element_DownloadProgressChanged progress={0}", ((MediaElement)sender).DownloadProgress); 
   
            if (DownloadProgressChanged != null)
            {
                DownloadProgressChanged(sender, e);
            }
        }

        void element_BufferingProgressChanged(object sender, RoutedEventArgs e)
        {
            this.logger.info("element_BufferingProgressChanged progress={0}", ((MediaElement)sender).BufferingProgress); 
   
            if (BufferingProgressChanged != null)
            {
                BufferingProgressChanged(sender, e);
            }
        }

        void element_CurrentStateChanged(object sender, RoutedEventArgs e)
        {
            this.logger.info("element_CurrentStateChanged state={0}",((MediaElement)sender).CurrentState); 
   
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
                this.logger.info("AutoPlay value={0}",value); 
  
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
                this.logger.info("Volume value={0}", value); 
  
                element.Volume = value;
            }
        }

        public void Play()
        {
            this.logger.info("Play"); 
  
            element.Play();
        }

        public void Pause()
        {
            this.logger.info("Pause"); 
  
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
                return element.Source;
            }
            set
            {
                this.logger.info("Source={0}",value); 
  
                element.Source = value;
            }
        }

        public void Stop()
        {
            this.logger.info("Stop"); 
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
                this.logger.info("set_IsMuted={0}",value); 
   
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
                this.logger.info("set_Position={0}", value); 
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
        public event EventHandler<ManifestEventArgs> BitratesReady;

        public event EventHandler<ManifestEventArgs> AudioTracksReady;

        public event EventHandler<SourceEventArgs> SourceChanged;

        #region IDisposable
        ~ProgressiveMediaElement()
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
    }
}
