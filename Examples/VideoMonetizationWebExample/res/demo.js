$(function() {
    // popup purchase
    var orderOpener = new vingd.popupOpener({
        popupURL: siteURL+"/res/popup.html",
        frontendURL: frontendURL,
        siteURL: siteURL,
        lang: 'en',
        onSuccess: function(hwnd, args) {
            window.location = vingd.buildURL(objectURL, {token: args.token});
        }
    });
    
    $(".v-video-purchase-link").click(orderOpener);
});
