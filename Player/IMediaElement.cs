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
    public enum MediaElementState
    {
        Closed = 0,
        Opening = 1,
        Buffering = 2,
        Playing = 3,
        Paused = 4,
        Stopped = 5,
        Individualizing = 6,
        AcquiringLicense = 7,
        ClipPlaying = 100,
    }
    public interface IMediaElement
    {
         MediaElementState CurrentState { get;  }

         TimeSpan Position { get; set; }

         Duration NaturalDuration { get;  }

         Double DownloadProgress { get;  }

         Double BufferingProgress { get; }

        bool AutoPlay { get; set; }

        double Volume { get; set; }

         event  EventHandler<RoutedEventArgs> BufferingProgressChanged;

         event EventHandler<RoutedEventArgs> DownloadProgressChanged;

         event EventHandler<RoutedEventArgs> CurrentStateChanged;

         event EventHandler<RoutedEventArgs> MediaEnded;

         event EventHandler<ExceptionRoutedEventArgs> MediaFailed;

         event EventHandler<RoutedEventArgs> MediaOpened;

         event  EventHandler<MouseButtonEventArgs> MouseLeftButtonUp;

         event EventHandler<ManifestEventArgs> BitratesReady;

         event EventHandler<SourceEventArgs> SourceChanged;




        void Play();

        void Pause();

        LicenseAcquirer LicenseAcquirer { get; set; }

        Uri Source { get; set; }

        void Stop();

        bool IsMuted { get; set; }

        Double Width { get; set; }

        Double Height { get; set; }
    }
}
