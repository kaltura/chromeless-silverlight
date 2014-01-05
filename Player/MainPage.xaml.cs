﻿using System;
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
        private Double _volume;
        private string _readyCallBack;
        private bool _externalInterfaceDisabled = false;
        private bool _disableOnScreenClick = false;
        private bool _isLoading;
        private bool _firedCanPlay;
        private bool _isAttemptingToPlay;
        private bool _isEnded;
        private bool _isPaused;
        private string _playerId;

        private Dictionary<String, List<String>> mapJSBindings = new Dictionary<string, List<String>>();
        private double _bufferedTime;
        private double _bufferedBytes;
       
        public MainPage(IDictionary<string, string> initParams)
        {
            InitializeComponent();

            HandleInitParams(initParams);

            HtmlPage.RegisterScriptableObject("MediaElementJS", this);

            RegisterMediaEvents();

            InitDebug();

            InitTimer();

            InitMedia();

            try
            {
                HtmlPage.Window.Eval(String.Format("{0}('{1}')",_readyCallBack,_playerId));
            }
            catch(Exception e) 
            {
                WriteDebug("Error occur while trying to call readyCallBack function:" + e.Message);
            }
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
            media.AutoPlay = _autoplay;
            media.Volume = _volume;
            if (!String.IsNullOrEmpty(_mediaUrl))
            {
                if (_autoplay || _preload != "none")
                    loadMedia();
            }
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
            if (initParams.ContainsKey("playerId"))
                _playerId = initParams["playerId"];
            if (initParams.ContainsKey("entryURL"))
                _mediaUrl = initParams["entryURL"];
            if (initParams.ContainsKey("autoplay") && initParams["autoplay"] == "true")
                _autoplay = true;
            if (initParams.ContainsKey("debug") && initParams["debug"] == "true")
                _debug = true;
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
          //  media.MouseLeftButtonDown += media_MouseLeftButtonDown;

            if (!_disableOnScreenClick)
            {
                media.MouseLeftButtonUp += media_MouseLeftButtonUp;
            }
            

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
        }

        private void SendEvent(string eventName,string param = null)
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
                WriteDebug(String.Format("Trigger {0} with param {1}", eventName, param));
                for (int i = 0; i < mapJSBindings[eventName].Count; i++)
                {
                    try
                    {
                        HtmlPage.Window.Eval(String.Format("{0}('{1}')", mapJSBindings[eventName][i], param));
                    }
                    catch (Exception e)
                    {
                        WriteDebug("Error occur while trying to trig function:" + e.Message);
                    } 
                }
            }
        }
       
        #region media events
        void media_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
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

                    break;
                case MediaElementState.Buffering:
                    pauseMedia();
                    break;
            }
        }
        void _timer_Tick(object sender, EventArgs e)
        {
            SendEvent("playerUpdatePlayhead", media.Position.TotalSeconds.ToString());
        }
       
        void media_MediaOpened(object sender, RoutedEventArgs e)
        {
            SendEvent("durationChange", media.NaturalDuration.TimeSpan.TotalSeconds.ToString());
        }

        void media_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            SendEvent("alert", e.ErrorException.Message);
        }

        void media_MediaEnded(object sender, RoutedEventArgs e)
        {
            SendEvent("playerPlayEnd");
        }

        void media_CurrentStateChanged(object sender, RoutedEventArgs e)
        {

            WriteDebug("state:" + media.CurrentState.ToString());

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
                    WriteDebug("paused event, " + _isAttemptingToPlay);
                    if (_isAttemptingToPlay)
                    {
                        this.playMedia();
                    }

                    StopTimer();
                    SendEvent("playerPaused");
                    break;
                case MediaElementState.Stopped:
                    _isEnded = false;
                    _isPaused = true;
                    StopTimer();
                    SendEvent("playerPaused");
                    break;
                case MediaElementState.Buffering:
                    SendEvent("progress");
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
        #endregion

        #region JS Interface
        
        [ScriptableMember]
        public void addJsListener(string bindName,string callback)
        {
            if (!mapJSBindings.ContainsKey(bindName) )
            {
                mapJSBindings[bindName] = new List<String>();
            }
            mapJSBindings[bindName].Add(callback);
        }

        [ScriptableMember]
        public void removeJsListener(string bindName, string callback)
        {
            if (mapJSBindings.ContainsKey(bindName) != null )
            {
                mapJSBindings[bindName].Remove(callback);
            }
           
        }

       

        [ScriptableMember]
        public void playMedia()
        {
            WriteDebug("method:play " + media.CurrentState);

            // sometimes people forget to call load() first
            if (_mediaUrl != "" && media.Source == null)
            {
                _isAttemptingToPlay = true;
                loadMedia();
            }

            // store and trigger with the state change above
            if (media.CurrentState == MediaElementState.Closed && _isLoading)
            {
                WriteDebug("storing _isAttemptingToPlay ");
                _isAttemptingToPlay = true;
            }

            media.Play();
            _isEnded = false;
            _isPaused = false;
     

        }

        [ScriptableMember]
        public void pauseMedia()
        {
            WriteDebug("method:pause " + media.CurrentState);

            _isEnded = false;
            _isPaused = true;

            media.Pause();
            StopTimer();
          
        }

        [ScriptableMember]
        public void loadMedia()
        {
            _isLoading = true;
            _firedCanPlay = false;

            WriteDebug("method:load " + media.CurrentState);
            WriteDebug(" - " + _mediaUrl.ToString());

            media.Source = new Uri(_mediaUrl, UriKind.Absolute);
        }

        [ScriptableMember]
        public void stopMedia()
        {
            WriteDebug("method:stop " + media.CurrentState);

            _isEnded = true;
            _isPaused = false;

            media.Stop();
            StopTimer();
        }

        [ScriptableMember]
        public void setVolume(Double volume)
        {
            WriteDebug("method:setvolume: " + volume.ToString());

            media.Volume = volume;

            SendEvent("volumechange",volume.ToString());
        }

        [ScriptableMember]
        public void setMuted(bool isMuted)
        {
            WriteDebug("method:setmuted: " + isMuted.ToString());

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
            WriteDebug("method:setCurrentTime: " + position.ToString());

            int milliseconds = Convert.ToInt32(position * 1000);

            SendEvent("playerSeekStart","0");
            media.Position = new TimeSpan(0, 0, 0, 0, milliseconds);
            SendEvent("playerSeekEnd",media.Position.TotalSeconds.ToString());
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
        #endregion


    }
}