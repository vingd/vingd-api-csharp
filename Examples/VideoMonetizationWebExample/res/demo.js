$(function() {
    // popup purchase
    var orderOpener = new vingd.popupOpener({
        popupURL: siteURL+"/res/popup.html",
        frontendURL: frontendURL,
        siteURL: siteURL,
        lang: 'en',
        onSuccess: function(hwnd, args) {
            window.location = vingd.buildURL(objectURL, {vid: args.context, token: args.token, context: args.context});
        }
    });
    
    $(".v-video-purchase-link").click(orderOpener);
});
