using System;
using System.Collections;
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

        TimeSpan StartPosition { get; }

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

        void Play();

        void Pause();

        LicenseAcquirer LicenseAcquirer { get; set; }

        Uri Source { get; set; }

        void Stop();

        bool IsMuted { get; set; }

        Double Width { get; set; }

        Double Height { get; set; }

        IDictionary GetDiagnostics();
    }

    static internal class DiagnosticsConstants
    {
        public static readonly string RenderedFramesPerSecond = "RenderFps";
        public  static readonly string DroppedFramesPerSecond = "RenderDroppedFps";
        public static readonly string CurrentBitrate = "currentBitrate";
        public static readonly string MulticastAddress = "mcAddress";
        public static readonly string InputFrameRate = "InputFps";
        public static readonly string PacketLoss = "PacketLoss";
        public static readonly string PacketRate = "PacketRate";
    }
    
    static internal class DiagnosticsLiveConstants
    {
        public static readonly string IsLive = "IsLive";
        public static readonly string IsLivePosition = "IsLivePosition";
        public static readonly string LiveBackOff = "LiveBackOff";
        public static readonly string LivePlaybackOffset = "LivePlaybackOffset";
        public static readonly string LivePlaybackStartPosition = "LivePlaybackStartPosition";
        public static readonly string LivePosition = "LivePosition";        
    }
}
