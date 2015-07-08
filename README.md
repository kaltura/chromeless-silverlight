chromeless-silverlight
======================

chromeless silverlight player

- in order to test smoothstreaming with mime type video/ism please add build line:

echo adding tracingconfig.xml" to Player.xap
"c:\Program Files\7-Zip\7z.exe" a $(TargetDir)Player.xap $(ProjectDir)TracingConfig.xml

where TracingConfig.xml looks like:

<?xml version="1.0" encoding="utf-8" ?>
<TracingConfiguration
          enabled="true"
          includeClassName="true"
          includeDate="false"
          includeMethodName="true"
          includeThreadId="true"
          includeTime="true"
          includeTraceLevel="false"
          includeMediaElementId="false">
  <TraceAreas baseSet="all">
    <!--<remove area="HttpWebRequest" />
    <remove area="HttpWebResponse" />-->
    <!--<remove area="Test" />-->
    <remove area="MediaSampleTrickPlay"></remove>
  </TraceAreas>
  <TraceDestinations>
    <add destination="Debug" />
 <add destination="Console" />
  </TraceDestinations>
  <TraceLevels baseSet="all">
    <remove level="Verbose" />
    <remove level="FunctionEntry" />
    <remove level="FunctionExit" />
  </TraceLevels>
</TracingConfiguration>

https://social.msdn.microsoft.com/Forums/windowsapps/en-US/7e1fa914-c062-41de-8712-b50eca31b2d9/cannot-find-file-tracingconfigxml-in-the-application-xap-package?forum=wpdevelop
