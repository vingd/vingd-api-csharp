<%@ Page Language="C#" Inherits="VideoMonetizationWebExample.Default" %>
<!doctype html>
<html>
<head>
    <title>Video Monetization Example</title>
    <link rel="stylesheet" type="text/css" href="res/demo.css"/>
</head>
<body>
    <div class="v-video">
        <div class="v-video-purchase">
            <a href="<%= v.GetVideoOrderURL(1) %>" class="v-video-purchase-link">Open video 1 (authorize with VINGD <%= String.Format("{0:f2}", vingdVideoPrice) %>)</a>
        </div>
    </div>

    <div class="v-video">
        <div class="v-video-purchase">
            <a href="<%= v.GetVideoOrderURL(2) %>" class="v-video-purchase-link">Open video 2 (authorize with VINGD <%= String.Format("{0:f2}", vingdVideoPrice) %>)</a>
        </div>
    </div>

    <script type="text/javascript" src="http://apps.vingd.com/cdn/vingd-popup/v0.8/build/main.min.js"></script>
    <script type="text/javascript" src="http://ajax.googleapis.com/ajax/libs/jquery/1.9.1/jquery.min.js"></script>
    <script type="text/javascript">
        var siteURL = "<%= baseURL %>";
        var objectURL = "<%= videoBaseURL %>";
        var frontendURL = "<%= vingdFrontendURL %>";
    </script>
    <script type="text/javascript" src="res/demo.js"></script>
</body>
</html>
