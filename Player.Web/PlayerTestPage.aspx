﻿<%@ Page Language="C#" AutoEventWireup="true" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Player</title>
    <style type="text/css">
    html, body {
	    height: 100%;
	    overflow: auto;
    }
    body {
	    padding: 0;
	    margin: 0;
    }
    #silverlightControlHost {
	    height: 100%;
	    text-align:center;
    }
    </style>
    <script type="text/javascript" src="Silverlight.js"></script>
    <script type="text/javascript">
        function onSilverlightError(sender, args) {
            var appSource = "";
            if (sender != null && sender != 0) {
              appSource = sender.getHost().Source;
            }
            
            var errorType = args.ErrorType;
            var iErrorCode = args.ErrorCode;

            if (errorType == "ImageError" || errorType == "MediaError") {
              return;
            }

            var errMsg = "Unhandled Error in Silverlight Application " +  appSource + "\n" ;

            errMsg += "Code: "+ iErrorCode + "    \n";
            errMsg += "Category: " + errorType + "       \n";
            errMsg += "Message: " + args.ErrorMessage + "     \n";

            if (errorType == "ParserError") {
                errMsg += "File: " + args.xamlFile + "     \n";
                errMsg += "Line: " + args.lineNumber + "     \n";
                errMsg += "Position: " + args.charPosition + "     \n";
            }
            else if (errorType == "RuntimeError") {           
                if (args.lineNumber != 0) {
                    errMsg += "Line: " + args.lineNumber + "     \n";
                    errMsg += "Position: " +  args.charPosition + "     \n";
                }
                errMsg += "MethodName: " + args.methodName + "     \n";
            }

            throw new Error(errMsg);
        }
        var slCtl = null;
        function pluginLoaded(sender, args) {
            slCtl = sender.getHost();
            slCtl.Content.MediaElementJS.addJsListener("playerPlayed", "playing");
           // slCtl.Content.MediaElementJS.addJsListener("flavorsListChanged", "listing");
           // slCtl.Content.MediaElementJS.addJsListener("audioTracksReceived", "audios");
           // slCtl.Content.MediaElementJS.addJsListener("audioTrackSelected", "audioSelected");
           // slCtl.Content.MediaElementJS.addJsListener("textTracksReceived", "audios");
            slCtl.Content.MediaElementJS.addJsListener("textTrackSelected", "onCaption");
            slCtl.Content.MediaElementJS.addJsListener("loadEmbeddedCaptions", "onCaption");
            

        }
        function ready(playerId) {
            debugger;
            var player = document.getElementById(playerId);
            
        }
        function listing(data) {
            alert(data);
        }
        function audioSelected(data) {
            alert("selected: " + data);
        }

        function audios(data) {
            alert(data);
        }
        function playing() {
           // alert("xxx");
        }

        function changeIndex() {
            slCtl.Content.MediaElementJS.selectTrack(6);
        }
        function play() {
            slCtl.Content.MediaElementJS.playMedia();
        }
        function changeAudio0() {
            slCtl.Content.MediaElementJS.selectAudioTrack(0);
        }
       
        function changeAudio1() {
            slCtl.Content.MediaElementJS.selectAudioTrack(1);
        }
        function changeAudio2() {
            slCtl.Content.MediaElementJS.selectAudioTrack(2);
        }

        function changeText0() {
            slCtl.Content.MediaElementJS.selectTextTrack(0);
        }

        function changeText1() {
            slCtl.Content.MediaElementJS.selectTextTrack(1);
        }

        function onCaption(data) {
            document.getElementById("captions").value += "\n" + parseHtmlEnteties(decodeURIComponent(JSON.parse(data).ttml));
        }

        function parseHtmlEnteties(str) {
            return str.replace(/&#([0-9]{1,3});/gi, function (match, numStr) {
                var num = parseInt(numStr, 10); // read num as normal number
                return String.fromCharCode(num);
            });
        }
    </script>
</head>
<body>
    <div><Button onClick="changeIndex()">set stream index</Button></div>
     <div><Button onClick="play()">Play</Button></div>
     <div><Button onClick="changeText0()">Change Text0</Button></div>
     <div><Button onClick="changeText1()">Change Text1</Button></div>
     <div><Button onClick="changeAudio0()">Change Audio0</Button></div>
     <div><Button onClick="changeAudio1()">Change Audio1</Button></div>
     <div><Button onClick="changeAudio2()">Change Audio2</Button></div>

    <form id="form1" runat="server" style="height:100%">
    <textarea id="captions" style="width:300px;height:280px;" value="test1" ></textarea>
    <div id="silverlightControlHost">

        <object id="kplayer" data="data:application/x-silverlight-2," type="application/x-silverlight-2" width="400" height="330">
		  <param name="source" value="ClientBin/Player.xap"/>
		  <param name="onError" value="onSilverlightError" />
		  <param name="background" value="white" />
		  <param name="minRuntimeVersion" value="5.0.61118.0" />
          <param name="onLoad" value="pluginLoaded" />
		  <param name="autoUpgrade" value="true" />
          <%--<param name="initParams" value="autoplay=true,smoothStreamPlayer=true,playerId=kplayer,entryURL=http://mediadl.microsoft.com/mediadl/iisnet/smoothmedia/Experience/BigBuckBunny_720p.ism/Manifest,startvolume=1" />--%>
          <%--<param name="initParams" value="autoplay=true,smoothStreamPlayer=true,playerId=kplayer,entryURL=http://playready.directtaps.net/smoothstreaming/TTLSS720VC1/To_The_Limit_720.ism/Manifest" />--%>

          <param name="initParams" value="startvolume=1,smoothStreamPlayer=true,preload=auto,entryURL=http://vod.toggletv.sg/vod/s/felucia/201504A/DI_HD_KING_ARTHUR_PC_SS.ism/Manifest,licenseURL=http%3A//udrm2-403043622.eu-west-1.elb.amazonaws.com//cenc/widevine/license%3Fcustom_data%3DeyJjYV9zeXN0ZW0iOiJPVFQiLCJ1c2VyX3Rva2VuIjoiMjIxNDg4MyIsImFjY291bnRfaWQiOjE0NywiY29udGVudF9pZCI6IlBSTU0xMDAwMDAzMDI5MDIzMTkwIiwiZmlsZXMiOiIiLCJ1ZGlkIjoiIn0%253D%26signature%3DeTYiuKqWLeQUFOdHeSZZwgDbktI%253D%26,autoPlay=true" />

		  <a href="http://go.microsoft.com/fwlink/?LinkID=149156&v=5.0.61118.0" style="text-decoration:none">
 			  <img src="http://go.microsoft.com/fwlink/?LinkId=161376" alt="Get Microsoft Silverlight" style="border-style:none"/>
		  </a>
	    </object><iframe id="_sl_historyFrame" style="visibility:hidden;height:0px;width:0px;border:0px"></iframe></div>
      
                <!--
                <object id="kplayer" data="data:application/x-silverlight-2," type="application/x-silverlight-2" width="400" height="330">
		  <param name="source" value="ClientBin/Player.xap"/>
		  <param name="onError" value="onSilverlightError" />
		  <param name="background" value="black" />
		  <param name="minRuntimeVersion" value="5.0.61118.0" />
          <param name="onLoad" value="pluginLoaded" />
		  <param name="autoUpgrade" value="true" />
          <param name="initParams" value="startvolume=1,entryURL=http://cdnapi.kaltura.com/p/524241/sp/52424100/playManifest/entryId/0_8zzalxul/flavorId/0_3ob6cr7c/format/url/protocol/http/a.mp4,autoplay=true,playerId=kplayer" />

		  <a href="http://go.microsoft.com/fwlink/?LinkID=149156&v=5.0.61118.0" style="text-decoration:none">
 			  <img src="http://go.microsoft.com/fwlink/?LinkId=161376" alt="Get Microsoft Silverlight" style="border-style:none"/>
		  </a>
	    </object><iframe id="_sl_historyFrame" style="visibility:hidden;height:0px;width:0px;border:0px"></iframe></div>
     
         <!--object id="kplayer" data="data:application/x-silverlight-2," type="application/x-silverlight-2" width="100%" height="100%">
		  <param name="source" value="ClientBin/Player.xap"/>
		  <param name="onError" value="onSilverlightError" />
		  <param name="background" value="white" />
		  <param name="minRuntimeVersion" value="5.0.61118.0" />
          <param name="onLoad" value="pluginLoaded" />
		  <param name="autoUpgrade" value="true" />
          <param name="initParams" value="multicastPlayer=true,streamAddress=239.1.1.1:10000,autoplay=true,playerId=kplayer,jsCallBackReadyFunc=ready" />

		  <a href="http://go.microsoft.com/fwlink/?LinkID=149156&v=5.0.61118.0" style="text-decoration:none">
 			  <img src="http://go.microsoft.com/fwlink/?LinkId=161376" alt="Get Microsoft Silverlight" style="border-style:none"/>
		  </a>
	    </object><iframe id="_sl_historyFrame" style="visibility:hidden;height:0px;width:0px;border:0px"></iframe></div-->
 
        
         </form>
</body>
</html>
