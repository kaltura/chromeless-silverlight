﻿using MediaStreamSrc.Classes;
using Microsoft.Web.Media.SmoothStreaming;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Browser;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Globalization;
using System.Threading;

namespace Player
{
    [ScriptableType]
    public partial class MainPage : UserControl
    {
        private DispatcherTimer _timer;
        private bool _autoplay;
        private string _mediaUrl;
        private bool _debug;
        private string _preload;
        private int _width;
        private int _height;
        private int _timerRate;
        private Double _volume = 1;
        private string _readyCallBack;
        private bool _externalInterfaceDisabled = false;
        private bool _disableOnScreenClick = false;
        private bool _isLoading;
        private bool _firedCanPlay;
        private bool _isAttemptingToPlay;
        private bool _isEnded;
        private bool _isPaused;
        private string _playerId;
        private double _lastDuration = 0;
        private int _defaultAudioTrack = -1;
        private int _waitForDefaultAudioTrackTimeout = 2000;
        private bool playAfterDefaultAudioChange = false;

        private Dictionary<String, List<String>> mapJSBindings = new Dictionary<string, List<String>>();
        private double _bufferedTime;
        private double _bufferedBytes;
        private string _licenseURL;
        private string _challengeCustomData;
        private bool _enableSmoothStreamPlayer;
        private bool _enableMultiCastPlayer;
        private IDictionary<string, string> _initParams;
        private Logger logger;

        private bool _isLive = false;
        private bool _isDVR = false;
        private bool _shouldReload = false;
        private bool _isRetry = false;
        private Uri LastSmoothStreamingSource { get; set; }
        private TimeSpan LastPosition { get; set; }

        private IMediaElement media = null;
        private string _ip;
        private int m_mediaFailureRetryCount = 0;
        private const int maxMediaFailureRetries = 5;


        public MainPage(IDictionary<string, string> initParams)
        {
            InitializeComponent();

            //Make sure that all data is passed as en-US culture, so it will prevent decimal seperator inconsistency
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            HandleInitParams(initParams);

            InitPlayer();

            HtmlPage.RegisterScriptableObject("MediaElementJS", this);
            if (initParams.ContainsKey("onLoaded"))
            {
                HtmlPage.Window.Invoke(initParams["onLoaded"]);
            }

            InitDebug();

            InitTimer();

            if (!String.IsNullOrEmpty(_readyCallBack))
            {
                try
                {
                    HtmlPage.Window.Eval(String.Format("{0}('{1}')", _readyCallBack, _playerId));
                }
                catch (Exception e)
                {
                    logger.info("Error occur while trying to call readyCallBack function:" + e.Message);
                }
            }

            this.Unloaded += MainPage_Unloaded;
        }

        void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            cleanup();
        }

        static Random idGen = new Random(Environment.TickCount);

        private void InitPlayer()
        {
            ChoosePlayer();
            InitMedia();


        }

        private void ChoosePlayer()
        {
            progressive_media.Visibility = System.Windows.Visibility.Collapsed;
            SmoothStream_media.Visibility = System.Windows.Visibility.Collapsed;
            if (_enableSmoothStreamPlayer)
            {
                SmoothStream_media.Visibility = System.Windows.Visibility.Visible;
                media = new SmoothStreamingElement(SmoothStream_media, logger);
                logger.info("ChoosePlayer : SmoothStream player");
                return;
            }
            if (_enableMultiCastPlayer)
            {
                progressive_media.Visibility = System.Windows.Visibility.Visible;
                media = new MulticastPlayer(progressive_media, _initParams, logger);
                logger.info("ChoosePlayer : MultiCast player");
                return;
            }

            //default
            progressive_media.Visibility = System.Windows.Visibility.Visible;
            media = new ProgressiveMediaElement(progressive_media, logger);

            logger.info("ChoosePlayer : Progressive download player");
        }

        /// <summary>
        /// Set debug text
        /// </summary>
        private void InitDebug()
        {
            tb_debug.Visibility = (_debug) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
            tb_debug.IsEnabled = false;
        }

        /// <summary>
        /// Init the media we should play
        /// </summary>
        private void InitMedia()
        {
            if (_defaultAudioTrack != -1)
            {
                //Keep autoplay value to resume playback after audio switch
                this.playAfterDefaultAudioChange = this._autoplay;
                //Load media to be able to get manifest info and change track
                this._preload = "auto";
            }
            else {
                media.AutoPlay = _autoplay;
            }
            media.Volume = _volume;
            if (!String.IsNullOrEmpty(_mediaUrl))
            {
                if (_autoplay || _preload != "none")
                    loadMedia();
            }
            RegisterMediaEvents();
        }

        /// <summary>
        /// init the interval timer that will sample the progress
        /// </summary>
        private void InitTimer()
        {
            if (_timerRate == 0)
                _timerRate = 250;

            // timer
            _timer = new System.Windows.Threading.DispatcherTimer();
            _timer.Interval = new TimeSpan(0, 0, 0, 0, _timerRate); // 200 Milliseconds 
            _timer.Tick += _timer_Tick;
            _timer.Stop();

        }

        /// <summary>
        /// Handle all the external params in order to load the player
        /// </summary>
        /// <param name="initParams"></param>
        private void HandleInitParams(IDictionary<string, string> initParams)
        {
            _initParams = new Dictionary<string, string>(initParams);

            if (initParams.ContainsKey("licenseURL"))
                _licenseURL = HttpUtility.UrlDecode(initParams["licenseURL"]);

            if (initParams.ContainsKey("challengeCustomData"))
                _challengeCustomData = initParams["challengeCustomData"];

            if (initParams.ContainsKey("playerId"))
                _playerId = initParams["playerId"];

            if (initParams.ContainsKey("entryURL"))
                _mediaUrl = HttpUtility.UrlDecode(initParams["entryURL"]);

            if (initParams.ContainsKey("autoplay") && initParams["autoplay"] == "true")
                _autoplay = true;

            if (initParams.ContainsKey("debug") && initParams["debug"] == "true")
            {
                _debug = true;

                MediaStreamSrc.Model.WMSLoggerFactory.getLogger(null).DebugEnabled = true;
            }

            if (initParams.ContainsKey("preload"))
                _preload = initParams["preload"].ToLower();
            else
                _preload = "";

            if (!(new string[] { "none", "metadata", "auto" }).Contains(_preload))
            {
                _preload = "none";
            }

            if (initParams.ContainsKey("jsCallBackReadyFunc"))
                _readyCallBack = initParams["jsCallBackReadyFunc"];

            if (initParams.ContainsKey("externalInterfaceDisabled") && initParams["externalInterfaceDisabled"] == "true")
                _externalInterfaceDisabled = true;

            if (initParams.ContainsKey("disableOnScreenClick") && initParams["disableOnScreenClick"] == "true")
                _disableOnScreenClick = true;


            if (initParams.ContainsKey("width"))
                Int32.TryParse(initParams["width"], out _width);

            if (initParams.ContainsKey("height"))
                Int32.TryParse(initParams["height"], out _height);


            if (initParams.ContainsKey("timerate"))
                Int32.TryParse(initParams["timerrate"], out _timerRate);

            if (initParams.ContainsKey("startvolume"))
                Double.TryParse(initParams["startvolume"], out _volume);

            if (initParams.ContainsKey("smoothStreamPlayer"))
            {
                _enableSmoothStreamPlayer = true;
            }

            if (initParams.ContainsKey("defaultAudioTrack"))
            {
                Int32.TryParse(initParams["defaultAudioTrack"], out _defaultAudioTrack);
            }

            if (initParams.ContainsKey("waitForDefaultAudioTrackTimeout"))
            {
                Int32.TryParse(initParams["waitForDefaultAudioTrackTimeout"], out _waitForDefaultAudioTrackTimeout);
            }            

            if (initParams.ContainsKey("multicastPlayer"))
            {
                _enableMultiCastPlayer = true;
                if (initParams.ContainsKey("streamAddress"))
                {
                    _ip = initParams["streamAddress"];
                }
            }
            this.logger = new Logger(string.Format("{0}-{1}", idGen.Next() % long.MaxValue, _ip));

            if (initParams.ContainsKey("isLive") && initParams["isLive"] == "true")
                _isLive = true;

            if (initParams.ContainsKey("isDVR") && initParams["isDVR"] == "true")
                _isDVR = true;

        }

        /// <summary>
        /// Register to media events
        /// </summary>
        private void RegisterMediaEvents()
        {
            // add events
            media.BufferingProgressChanged += media_BufferingProgressChanged;
            media.DownloadProgressChanged += media_DownloadProgressChanged;
            media.CurrentStateChanged += media_CurrentStateChanged;
            media.MediaEnded += media_MediaEnded;
            media.MediaFailed += media_MediaFailed;
            media.MediaOpened += media_MediaOpened;

            if (media is SmoothStreamingElement)
            {
                SmoothStreamingElement ssMedia = media as SmoothStreamingElement;
                ssMedia.BitratesReady += media_BitratesReady;
                ssMedia.AudioTracksReady += media_AudioTracksReady;
                ssMedia.TextTracksReady += media_TextTracksReady;
                ssMedia.SourceChanged += media_SourceChanged;
                ssMedia.MarkerReached += media_MarkerReached;
                ssMedia.TextTrackLoaded += media_TextTrackLoaded;
                ssMedia.CurrentAudioStreamChanged += media_CurrentAudioStreamChanged;
            }

            if (media is MulticastPlayer)
            {
                (media as MulticastPlayer).ReceivedID3Tag += MainPage_ReceivedID3Tag;
            }

            //  media.MouseLeftButtonDown += media_MouseLeftButtonDown;

            if (!_disableOnScreenClick)
            {
                media.MouseLeftButtonUp += media_MouseLeftButtonUp;
            }
        }

        private void media_CurrentAudioStreamChanged(object sender, StreamUpdatedListEventArgs e)
        {
            if (playAfterDefaultAudioChange)
            {
                //If autoplay was set and we had to set default language on start then 
                //continue play now and set the flag to false
                this.playAfterDefaultAudioChange = false;
                SmoothStream_media.Play();
            }
            SendEvent("audioTrackSelected", "{\"index\":" + (media as SmoothStreamingElement).getCurrentAudioIndex() + "}");
        }

        void MainPage_ReceivedID3Tag(string id3Tag)
        {
            System.Diagnostics.Debug.WriteLine("onId3Tag " + id3Tag);
            this.SendEvent("id3tag", id3Tag);
        }

        private void StartTimer()
        {
            _timer.Start();
        }

        private void StopTimer()
        {
            _timer.Stop();
        }

        private void WriteDebug(string text)
        {
            tb_debug.Text += text + "\n";

            MediaStreamSrc.Model.WMSLoggerFactory.getLogger(null).debug(text);
        }

        private void SendEvent(string eventName, string param = null)
        {
            /*
             * var bindEventMap = {
							'playerPaused' : 'onPause',
							'playerPlayed' : 'onPlay',
							'durationChange' : 'onDurationChange',
							'playerPlayEnd' : 'onClipDone',
							'playerUpdatePlayhead' : 'onUpdatePlayhead',
							'playerSeekEnd': 'onPlayerSeekEnd',
							'alert': 'onAlert',
							'mute': 'onMute',
							'unmute': 'onUnMute',
							'volumeChanged': 'onVolumeChanged'
						};
             */
            if (mapJSBindings.Keys.Contains(eventName))
            {
                logger.info("Trigger " + eventName + " with param " + param);
                for (int i = 0; i < mapJSBindings[eventName].Count; i++)
                {
                    try
                    {
                        HtmlPage.Window.Eval(String.Format("{0}('{1}')", mapJSBindings[eventName][i], param));
                    }
                    catch (Exception e)
                    {
                        logger.info("Error occur while trying to trig function:" + e.Message);
                    }
                }
            }
        }

        private void cleanup()
        {
            if (media is MulticastPlayer)
            {
                (media as MulticastPlayer).ReceivedID3Tag -= MainPage_ReceivedID3Tag;
            }
            if (media is IDisposable)
            {
                (media as IDisposable).Dispose();
                media = null;
            }
        }

        #region media events
        void media_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            {
                switch (media.CurrentState)
                {
                    case MediaElementState.Playing:
                        pauseMedia();
                        break;

                    case MediaElementState.Paused:
                        playMedia();
                        break;
                    case MediaElementState.Stopped:
                        playMedia();
                        break;
                    case MediaElementState.Buffering:
                        pauseMedia();
                        break;
                }
            }
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            double time = CurrentTimeInSeconds;
            double currentDuration = media.NaturalDuration.TimeSpan.TotalSeconds;

            if (SmoothStream_media.Position != null && SmoothStream_media.Position.TotalSeconds > 0)
            {
                LastPosition = SmoothStream_media.Position;
            }
            if (_isLive && _isDVR && _lastDuration != currentDuration)
            {
                _lastDuration = currentDuration;
                SendEvent("durationChange", _lastDuration.ToString());
            }
            SendEvent("playerUpdatePlayhead", time.ToString());
        }

        void media_MediaOpened(object sender, RoutedEventArgs e)
        {
            if (this._isRetry)
            {
                this._isRetry = false;
                m_mediaFailureRetryCount = 0; //success

                // restore state of player
                if (SmoothStream_media.IsLive)
                {
                    // go to live if a live feed and don't have a last position,
                    // most likely this is the result of the first load attempt
                    SmoothStream_media.StartSeekToLive();
                }
                else
                {
                    // restore position		
                    SmoothStream_media.Position = LastPosition;
                }
            }
            else
            {
                this.LastSmoothStreamingSource = SmoothStream_media.SmoothStreamingSource;

            }
            if (_defaultAudioTrack != -1)
            {
                //If we have default audio track other then 0 then request change after media is opened
                selectAudioTrack(_defaultAudioTrack);
                if (_waitForDefaultAudioTrackTimeout > 0)
                {
                    Thread.Sleep(_waitForDefaultAudioTrackTimeout);
                }
            }
            _lastDuration = media.NaturalDuration.TimeSpan.TotalSeconds;
            SendEvent("durationChange", media.NaturalDuration.TimeSpan.TotalSeconds.ToString());
        }

        void media_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            if (e.ErrorException != null &&
                !String.IsNullOrEmpty(e.ErrorException.Message) &&
                e.ErrorException.Message.IndexOf("3050 Corrupt H264 data encountered") > -1)
            {
                if (m_mediaFailureRetryCount < maxMediaFailureRetries)
                {
                    logger.info("Encountered error 3050 Corrupt H264 data encountered, retrying playback");
                    m_mediaFailureRetryCount++;

                    SmoothStream_media.Source = null;
                    //If we try to recover stream and we have default audio other then 0
                    //then don't set autoplay just yet, we will change it on media opened 
                    //and continue playback after change
                    if (_defaultAudioTrack != -1)
                    {
                        this.playAfterDefaultAudioChange = true;
                    }
                    else {
                        SmoothStream_media.AutoPlay = true;                        
                    }
                    SmoothStream_media.SmoothStreamingSource = new Uri(LastSmoothStreamingSource.AbsoluteUri);
                    this._autoplay = true;
                    this._isRetry = true;
                    if (_defaultAudioTrack == -1) {
                        SmoothStream_media.Play();
                    }
                }
            }
            else
            {
                logger.info(e.ErrorException.Message);
                SendEvent("error", "{\"errorMessage\":\"" + e.ErrorException.Message + "\", \"stackTrace\":\"" + e.ErrorException.StackTrace + "\"}");
            }
        }

        void media_MediaEnded(object sender, RoutedEventArgs e)
        {
            SendEvent("playerPlayEnd");
        }

        void play_timer_tick(object sender, EventArgs e)
        {
            ((DispatcherTimer)sender).Stop();
            this.playMedia();
        }


        void media_CurrentStateChanged(object sender, RoutedEventArgs e)
        {

            logger.info("state:" + media.CurrentState.ToString());

            switch (media.CurrentState)
            {
                case MediaElementState.Opening:
                    SendEvent("loadstart");
                    break;
                case MediaElementState.Playing:
                    _isEnded = false;
                    _isPaused = false;
                    _isAttemptingToPlay = false;
                    StartTimer();


                    SendEvent("play");
                    SendEvent("playerPlayed");
                    break;

                case MediaElementState.Paused:
                    _isEnded = false;
                    _isPaused = true;

                    // special settings to allow play() to work
                    _isLoading = false;
                    StopTimer();
                    logger.info("paused event, " + _isAttemptingToPlay);
                    if (_isAttemptingToPlay)
                    {
                        System.Windows.Threading.DispatcherTimer playTimer = new System.Windows.Threading.DispatcherTimer();
                        playTimer.Interval = new TimeSpan(0, 0, 0, 0, 300);
                        playTimer.Tick += play_timer_tick;
                        playTimer.Start();
                    }
                    else
                    {
                        SendEvent("playerPaused");
                    }

                    break;
                case MediaElementState.Stopped:
                    _isEnded = false;
                    _isPaused = true;
                    StopTimer();
                    SendEvent("playerPaused");
                    break;
                case MediaElementState.Buffering:
                    SendEvent("buffering");
                    break;
                case MediaElementState.Individualizing:
                    SendEvent("individualizing");
                    break;
                case MediaElementState.AcquiringLicense:
                    SendEvent("acquiringLicense");
                    break;
            }
        }

        void media_DownloadProgressChanged(object sender, RoutedEventArgs e)
        {
            _bufferedTime = media.DownloadProgress * media.NaturalDuration.TimeSpan.TotalSeconds;
            _bufferedBytes = media.BufferingProgress;

            if (!_firedCanPlay)
            {
                SendEvent("loadeddata");
                SendEvent("canplay");
                _firedCanPlay = true;
            }

            SendEvent("progress");
        }

        void media_BufferingProgressChanged(object sender, RoutedEventArgs e)
        {
            _bufferedTime = media.DownloadProgress * media.NaturalDuration.TimeSpan.TotalSeconds;
            _bufferedBytes = media.BufferingProgress;

            SendEvent("progress");
        }

        void media_AudioTracksReady(object sender, ManifestEventArgs e)
        {
            SendEvent("audioTracksReceived", parseLanguages(e));
            //notify default audio index
            SendEvent("audioTrackSelected", "{\"index\":" + (media as SmoothStreamingElement).getCurrentAudioIndex() + "}");
        }

        void media_TextTracksReady(object sender, ManifestEventArgs e)
        {
            SendEvent("textTracksReceived", parseLanguages(e));
        }





        /**
* parse given args to languages string
* */
        private string parseLanguages(ManifestEventArgs e)
        {
            List<Object> tracks = e.Flavors;
            String languages = "";
            if (tracks != null)
            {
                string name = "";
                string langName = "";
                for (int i = 0; i < tracks.Count; i++)
                {
                    ((StreamInfo)tracks.ElementAt(i)).Attributes.TryGetValue("Name", out name);
                    ((StreamInfo)tracks.ElementAt(i)).Attributes.TryGetValue("Language", out langName);
                    languages += "{\"label\":\"" + name + "\",\"index\":" + i + ",\"language\":\"" + langName + "\"}";
                    if (i < tracks.Count - 1)
                    {
                        languages += ",";
                    }
                }
            }
            languages = "{\"languages\":[" + languages + "]}";

            return languages;
        }

        void media_BitratesReady(object sender, ManifestEventArgs e)
        {
            List<TrackInfo> tracks = e.Flavors.Cast<TrackInfo>().ToList();
            String bitrates = "";
            if (tracks != null)
            {
                foreach (TrackInfo ti in tracks)
                {
                    int height = 0;
                    if (ti.Attributes.ContainsKey("MaxHeight"))
                    {
                        Int32.TryParse(ti.Attributes["MaxHeight"], out height);
                    }
                    int width = 0;
                    if (ti.Attributes.ContainsKey("MaxWidth"))
                    {
                        Int32.TryParse(ti.Attributes["MaxWidth"], out width);
                    }
                    bitrates += "{\"type\":\"video/ism\",\"assetid\":" + "\"ism_" + ti.Index + "\"" + ",\"bandwidth\":" + ti.Bitrate + ",\"height\":" + height + ",\"width\":" + width + "}";
                    if (tracks.IndexOf(ti) < tracks.Count - 1)
                    {
                        bitrates += ",";
                    }
                }
            }
            bitrates = "{\"flavors\":[" + bitrates + "]}";
            SendEvent("flavorsListChanged", bitrates);
        }

        void media_SourceChanged(object sender, SourceEventArgs e)
        {
            SendEvent("switchingChangeComplete", "{\"newIndex\":" + e.NewIndex + "}");
        }

        void media_MarkerReached(object sender, TimelineMarkerRoutedEventArgs e)
        {
            SendEvent("loadEmbeddedCaptions", "{\"language\":\"" + e.Marker.Type + "\", \"ttml\":\"" + HttpUtility.HtmlEncode(Uri.EscapeUriString(e.Marker.Text)) + "\"}");
        }

        void media_TextTrackLoaded(object sender, SourceEventArgs e)
        {
            SendEvent("textTrackSelected", "{\"index\":" + e.NewIndex + ", \"ttml\":\"" + HttpUtility.HtmlEncode(Uri.EscapeUriString(e.Text)) + "\"}");
        }





        #endregion
        #region JS Interface
        [ScriptableMember]
        public void addJsListener(string bindName, string callback)
        {
            if (!mapJSBindings.ContainsKey(bindName))
            {
                mapJSBindings[bindName] = new List<String>();
            }
            mapJSBindings[bindName].Add(callback);
        }

        [ScriptableMember]
        public void removeJsListener(string bindName, string callback)
        {
            if (mapJSBindings.ContainsKey(bindName))
            {
                mapJSBindings[bindName].Remove(callback);
            }

        }

        [ScriptableMember]
        public void changeMulticastParams(string multicastGroup, string sourceAddress, bool multicastPolicyOverMulticastEnabled)
        {
            _initParams["streamAddress"] = multicastGroup;
            _initParams["sourceAddress"] = sourceAddress;
            _initParams["multicastPolicyOverMulticastEnabled"] = multicastPolicyOverMulticastEnabled.ToString();
            reloadMedia();
        }

        [ScriptableMember]
        public void playMedia()
        {
            logger.info("method:play " + media.CurrentState);

            // sometimes people forget to call load() first
            if (_shouldReload || (!_enableMultiCastPlayer && (media.CurrentState == MediaElementState.Closed || (_mediaUrl != "" && media.Source == null))))
            {
                _isAttemptingToPlay = true;
                _shouldReload = false;
                loadMedia();

            }
            // store and trigger with the state change above
            else if (media.CurrentState == MediaElementState.Closed && _isLoading)
            {
                logger.info("storing _isAttemptingToPlay ");
                _isAttemptingToPlay = true;
            }
            else
            {
                if (_enableMultiCastPlayer && media.CurrentState == MediaElementState.Stopped)
                {
                    reloadMedia();
                }
                //If we're still waiting for initial audio language to change then mask play 
                //reuest as it will happen once change is completed
                if (!playAfterDefaultAudioChange)
                {
                    media.Play();
                }
                _isEnded = false;
                _isPaused = false;
            }

        }

        [ScriptableMember]
        public void pauseMedia()
        {
            logger.info("method:pause " + media.CurrentState);

            _isEnded = false;
            _isPaused = true;

            if (_isLive && !_isDVR)
            {
                _shouldReload = true;
            }
            media.Pause();
            StopTimer();

        }

        [ScriptableMember]
        public void loadMedia()
        {
            _isLoading = true;
            _firedCanPlay = false;

            logger.info("method:load " + media.CurrentState);
            if (!String.IsNullOrEmpty(_mediaUrl))
            {
                logger.info(" - " + _mediaUrl.ToString());
            }
            if (!String.IsNullOrEmpty(_licenseURL))
            {
                media.LicenseAcquirer = new customLicenseAcquirer("media");
                media.LicenseAcquirer.AcquireLicenseCompleted += new EventHandler<AcquireLicenseCompletedEventArgs>(acquirer_Completed);






                // Set the License URI to proper License Server address.
                /*partnerId - mandatory
                ks - mandatory
                entryId  - optional
                referrer – optional*/
                if (!String.IsNullOrEmpty(_challengeCustomData))
                {
                    media.LicenseAcquirer.ChallengeCustomData = _challengeCustomData;
                }

                media.LicenseAcquirer.LicenseServerUriOverride = new Uri(_licenseURL, UriKind.Absolute);
            }
            media.Source = new Uri(_mediaUrl, UriKind.Absolute);
        }
        public void acquirer_Completed(object sender, AcquireLicenseCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                // take appropriate action.  Might be retrying for instance.
            }
            else if (e.Cancelled)
            {
                // take appropriate action.  Might be nothing.
            }
            else
            {
            }
        }

        [ScriptableMember]
        public void reloadMedia()
        {
            logger.info("method:reloadMedia " + media.CurrentState);
            if (_enableMultiCastPlayer)
            {
                cleanup();

                InitPlayer();
            }
        }

        [ScriptableMember]
        public void stopMedia()
        {
            logger.info("method:stop " + media.CurrentState);

            _isEnded = true;
            _isPaused = false;

            media.Stop();
            StopTimer();
        }

        [ScriptableMember]
        public void setVolume(Double volume)
        {
            logger.info("method:setvolume: " + volume.ToString());

            media.Volume = volume;

            SendEvent("volumechange", volume.ToString());
        }

        [ScriptableMember]
        public void setMuted(bool isMuted)
        {
            logger.info("method:setmuted: " + isMuted.ToString());

            media.IsMuted = isMuted;
            if (isMuted)
            {
                SendEvent("mute");
            }
            else
            {
                SendEvent("unmute");
            }


        }

        [ScriptableMember]
        public void setCurrentTime(Double position)
        {
            logger.info("method:setCurrentTime: " + position.ToString());
            int seconds = Convert.ToInt32(position);
            TimeSpan newPosition = TimeSpan.FromSeconds(seconds) + media.StartPosition;
            SendEvent("playerSeekStart", "0");
            media.Position = newPosition;
            //Send the event here so if we are paused the event will still be dispatched
            var time = CurrentTimeInSeconds;
            //WriteDebug("playerUpdatePlayhead " + TimeSpan.FromSeconds(time));
            SendEvent("playerUpdatePlayhead", time.ToString());
            SendEvent("playerSeekEnd", time.ToString());
        }

        [ScriptableMember]
        public void setSrc(string url)
        {
            _mediaUrl = url;
        }


        [ScriptableMember]
        public void setVideoSize(int width, int height)
        {
            this.Width = media.Width = width;
            this.Height = media.Height = height;
        }

        [ScriptableMember]
        public void selectTrack(int trackIndex)
        {
            if (media is SmoothStreamingElement)
            {
                (media as SmoothStreamingElement).selectTrack(trackIndex);
            }

        }

        [ScriptableMember]
        public void selectAudioTrack(int trackIndex)
        {
            if (media is SmoothStreamingElement)
            {
                (media as SmoothStreamingElement).selectAudioTrack(trackIndex);
            }
        }

        [ScriptableMember]
        public void selectTextTrack(int trackIndex)
        {
            if (media is SmoothStreamingElement)
            {
                (media as SmoothStreamingElement).selectTextTrack(trackIndex);
            }
        }

        [ScriptableMember]
        public bool StartSeekToLive
        {
            get
            {
                if (media is SmoothStreamingElement)
                {
                    (media as SmoothStreamingElement).element.StartSeekToLive();
                }
                return true;
            }
        }

        [ScriptableMember]
        public void stretchFill()
        {
            if (media is MulticastPlayer)
            {
                (media as MulticastPlayer).stretchFill();
            }

        }


        #endregion

        [ScriptableMember]
        public double MulticastAverageBitRate
        {
            get
            {
                if (media is MulticastPlayer)
                {
                    return (media as MulticastPlayer).AverageBitRate;
                }
                return 0;
            }
        }

        [ScriptableMember]
        public double CurrentTimeInSeconds
        {
            get
            {
                //In manifest that have start position other then 0 if we haven't started playing yet then return 0,
                //because before first play the curren tposition is 0, but start position is bigger than 0 and you'll get a negative number
                if (media.StartPosition.TotalSeconds > 0 && media.Position.TotalSeconds == 0)
                {
                    return 0;
                }
                else
                {
                    return media.Position.TotalSeconds - media.StartPosition.TotalSeconds;
                }
            }
        }

        [ScriptableMember]
        public IDictionary getDiagnostics()
        {
            if (media == null)
            {
                return null;
            }
            return media.GetDiagnostics();
        }
        [ScriptableMember]
        public string getDiagnosticsString()
        {
            if (media == null)
            {
                return null;
            }
            Dictionary<string, string> myDictionary = media.GetDiagnostics() as Dictionary<string, string>;
            string diagDataString = "{";
            foreach (KeyValuePair<string, string> diagData in myDictionary)
            {
                diagDataString += "\"" + diagData.Key + "\":\"" + diagData.Value + "\",";//,\"assetid\":" + "\"ism_" + ti.Index + "\"" + ",\"bandwidth\":" + ti.Bitrate + ",\"height\":" + height + ",\"width\":" + width + "}";

            }
            diagDataString = diagDataString.Remove(diagDataString.Length - 1);
            diagDataString += "}";
            return diagDataString;

        }

        [ScriptableMember]
        public string getCurrentBufferLength()
        {
            if (media == null)
            {
                return null;
            }
            Double bufferLength = 0;
            if (media.GetType().GetMethod("getCurrentBufferLength") != null)
            {
                bufferLength = media.getCurrentBufferLength();
            }            
            return bufferLength.ToString();

        }
    }
}
