using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Vuplex.WebView;
using Newtonsoft.Json;
using UnityEngine.UI;

public class SingularityUnitySdk
{
    CanvasWebViewPrefab googleLoginWebView;
    CanvasWebViewPrefab mainWebViewPrefab;
    long time1 = 0L;
    long time2 = 0L;
    long time3 = 0L;

    private GameObject borderImage;
    private GameObject closeButton;
    private TMPro.TextMeshProUGUI pleaseWaitText;

    GameObject canvas;

    private ISingularityUnityListener _listener;
    private ISingularityInitListener _initListener;

    TaskCompletionSource<string> userInfoCompletionSource;
    TaskCompletionSource<string> signTransactionCompletionSource;
    TaskCompletionSource<string> signAndSendTransactionCompletionSource;
    TaskCompletionSource<string> signPersonalMessageCompletionSource;
    TaskCompletionSource<string> sendNonNativeTokenCompletionSource;
    TaskCompletionSource<string> sendNftCompletionSource;
    TaskCompletionSource<string> requestTypedSignatureCompletionSource;


    private int CALL_TIMEOUT_SECONDS = 10;

    public SingularityUnitySdk(ISingularityUnityListener listener)
    {
        _listener = listener;
    }

    public SingularityUnitySdk()
    {
    
    }

    public void SetSingularityListener(ISingularityUnityListener listener)
    {
        _listener = listener;
    }

    public long GetCurrentTimeInMilliseconds()
    {
        TimeSpan timeSpan = DateTime.UtcNow - new DateTime(1970, 1, 1);
        return (long)timeSpan.TotalMilliseconds;
    }

    public async void InitializeSingularity(string apiKey, GameObject canvas, ISingularityInitListener initListener)
    {
        Web.SetUserAgent("UnityDesktop");

        time1 = GetCurrentTimeInMilliseconds();

        _initListener = initListener;

        this.canvas = canvas;

        mainWebViewPrefab = CanvasWebViewPrefab.Instantiate();
        Canvas canvasComponent = canvas.GetComponent<Canvas>();
        canvasComponent.enabled = false;

        mainWebViewPrefab.Resolution = 1f;
        mainWebViewPrefab.PixelDensity = 2;
        mainWebViewPrefab.Native2DModeEnabled = true;
        mainWebViewPrefab.transform.SetParent(canvas.transform, false);

        var rectTransform = mainWebViewPrefab.transform as RectTransform;
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.anchorMin = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        CanvasScaler canvasScaler = canvas.GetComponent<CanvasScaler>();
        float screenHeight = Screen.height;
        if (canvasScaler != null)
        {
            Vector2 referenceResolution = canvasScaler.referenceResolution;
            screenHeight = referenceResolution.y;
        }

        Vector2 position = rectTransform.anchoredPosition;
        position.x = 400;
        position.y = -(screenHeight / 2);
        rectTransform.anchoredPosition = position;

        mainWebViewPrefab.transform.localScale = Vector3.one;

        if (rectTransform.sizeDelta == Vector2.zero)
        {
            rectTransform.sizeDelta = new Vector2(400, screenHeight); // default size
        }

        await mainWebViewPrefab.WaitUntilInitialized();

        time2 = GetCurrentTimeInMilliseconds();

        int width = Screen.width; // for example, 800 pixels width
        int height = Screen.height; // for example, 600 pixels height
        bool isFullScreen = Screen.fullScreen; // set to true for fullscreen


        // Change the resolution
        Screen.SetResolution(width + 1, height + 1, isFullScreen);

        mainWebViewPrefab.WebView.SetDefaultBackgroundEnabled(false);
        canvasComponent.enabled = true;

        mainWebViewPrefab.WebView.MessageEmitted += Controls_MessageEmitted;
        mainWebViewPrefab.WebView.LoadUrl("https://mobile-sdk.s9y.gg?api_key="+apiKey+ "&env_key=1");
    }

    private void Controls_MessageEmitted(object sender, EventArgs<string> eventArgs)
    {
        try
        {
            var message = eventArgs.Value;

            Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(eventArgs.Value);

            if (dictionary.TryGetValue("type", out object typeValue))
            {
                if ((string)typeValue == "SOCIAL_LOGIN_BUTTON_CLICKED")
                {
                    var loginMethod = dictionary["loginMethod"];
                    openNewWebViewAndAuth0Auth((string)loginMethod);
                }

                if ((string)typeValue == "SOCIAL_LOGOUT_EVENT")
                {
                    if (_listener == null) return; 
                    _listener.onUserLogout();
                }

                if ((string)typeValue == "SOCIAL_LOGIN_EVENT")
                {
                    var data = dictionary["data"];
                    if (_listener == null) return;
                    _listener.onUserLogIn((string)data);
                }

                if ((string)typeValue == "DRAWER_OPEN_EVENT")
                {
                    OpenDrawer();
                    if (_listener == null) return;
                    _listener.onDrawerOpen();
                }

                if ((string)typeValue == "DRAWER_CLOSE_EVENT")
                {
                    CloseDrawer();
                    if (_listener == null) return;
                    _listener.onDrawerClose();
                }

                if ((string)typeValue == "ON_TRANSACTION_APPROVAL_EVENT")
                {
                    var data = dictionary["data"];
                    if (_listener == null) return;
                    _listener.onTransactionApprove((string)data);
                }

                if ((string)typeValue == "ON_TRANSACTION_SUCCESS_EVENT")
                {
                    var data = dictionary["data"];
                    if (_listener == null) return;
                    _listener.onTransactionSuccess((string)data);
                }

                if ((string)typeValue == "ON_TRANSACTION_FAILURE_EVENT")
                {
                    var data = dictionary["data"];
                    if (_listener == null) return;
                    _listener.onTransactionFailure((string)data);
                }

                if ((string)typeValue == "INIT_SUCCESS_EVENT")
                {
                    time3 = GetCurrentTimeInMilliseconds();                 
                    if (_initListener == null) return;
                    _initListener.onInitSuccess();
                }

                if ((string)typeValue == "OPEN_URL_EVENT")
                {
                    var url = (string)dictionary["data"];
                    Application.OpenURL(url);
                }

            }

        }
        catch (Exception e)
        {

        }
    }

    public Task<string> GetConnectedUserInfoAsync()
    {
        if (mainWebViewPrefab == null) return Task.FromResult("Singularity not initialized");

        userInfoCompletionSource = new TaskCompletionSource<string>();

        System.EventHandler<EventArgs<string>> messageHandler = null;
        messageHandler = (sender, eventArgs) => {
            Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(eventArgs.Value);
            if (dictionary.TryGetValue("type", out object typeValue))
            {
                if ((string)typeValue == "USER_INFO_RESULT")
                {
                    var userData = dictionary["data"];
                    userInfoCompletionSource.TrySetResult((string)userData);
                    mainWebViewPrefab.WebView.MessageEmitted -= messageHandler;
                }
            }
        };

        mainWebViewPrefab.WebView.MessageEmitted += messageHandler;


        var cancellationToken = new CancellationTokenSource(CALL_TIMEOUT_SECONDS * 1000); // Timeout in milliseconds
        cancellationToken.Token.Register(() => {
            userInfoCompletionSource.TrySetCanceled();
            mainWebViewPrefab.WebView.MessageEmitted -= messageHandler; // Unsubscribe when the task is canceled
        }, useSynchronizationContext: false);

        string jsCode = "window.SingularityEvent.getConnectUserInfo();";
        mainWebViewPrefab.WebView.ExecuteJavaScript(jsCode);

        return userInfoCompletionSource.Task;
    }

    public Task<string> SignTransactionAsync(string txData)
    {
        if (mainWebViewPrefab == null) return Task.FromResult("Singularity not initialized");

        signTransactionCompletionSource = new TaskCompletionSource<string>();

        EventHandler<EventArgs<string>> messageHandler = null;
        messageHandler = (sender, eventArgs) => {
            Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(eventArgs.Value);
            if (dictionary.TryGetValue("type", out object typeValue))
            {
                if ((string)typeValue == "SIGN_TRANSACTION_RESULT")
                {
                    var signTxnResult = dictionary["data"];
                    signTransactionCompletionSource.TrySetResult((string)signTxnResult);
                    mainWebViewPrefab.WebView.MessageEmitted -= messageHandler;
                }
            }
        };

        mainWebViewPrefab.WebView.MessageEmitted += messageHandler;

        string jsCode = $"window.SingularityEvent.signTransaction('{txData}');";
        mainWebViewPrefab.WebView.ExecuteJavaScript(jsCode);

        return signTransactionCompletionSource.Task;
    }

    public Task<string> SignPersonalMessageAsync(string message)
    {
        if (mainWebViewPrefab == null) return Task.FromResult("Singularity not initialized");

        signPersonalMessageCompletionSource = new TaskCompletionSource<string>();

        EventHandler<EventArgs<string>> messageHandler = null;
        messageHandler = (sender, eventArgs) => {
            Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(eventArgs.Value);
            if (dictionary.TryGetValue("type", out object typeValue))
            {
                if ((string)typeValue == "SIGN_PERSONAL_MESSAGE_RESULT")
                {
                    var signTxnResult = dictionary["data"];
                    signPersonalMessageCompletionSource.TrySetResult((string)signTxnResult);
                    mainWebViewPrefab.WebView.MessageEmitted -= messageHandler;
                }
            }
        };

        mainWebViewPrefab.WebView.MessageEmitted += messageHandler;

        string jsCode = $"window.SingularityEvent.requestPersonalSignature('{message}');";
        mainWebViewPrefab.WebView.ExecuteJavaScript(jsCode);

        return signPersonalMessageCompletionSource.Task;
    }

    public Task<string> SignAndSendTransactionAsync(string txData)
    {
        if (mainWebViewPrefab == null) return Task.FromResult("Singularity not initialized");

        signAndSendTransactionCompletionSource = new TaskCompletionSource<string>();

        EventHandler<EventArgs<string>> messageHandler = null;
        messageHandler = (sender, eventArgs) => {
            Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(eventArgs.Value);
            if (dictionary.TryGetValue("type", out object typeValue))
            {
                if ((string)typeValue == "SIGN_AND_SEND_TRANSACTION_RESULT")
                {
                    var signTxnResult = dictionary["data"];
                    signAndSendTransactionCompletionSource.TrySetResult((string)signTxnResult);
                    mainWebViewPrefab.WebView.MessageEmitted -= messageHandler;
                }
            }
        };

        mainWebViewPrefab.WebView.MessageEmitted += messageHandler;

        string jsCode = $"window.SingularityEvent.signAndSendTransaction('{txData}');";
        mainWebViewPrefab.WebView.ExecuteJavaScript(jsCode);

        return signAndSendTransactionCompletionSource.Task;
    }

    public Task<string> SendNonNativeTokenAsync(string txData)
    {
        if (mainWebViewPrefab == null) return Task.FromResult("Singularity not initialized");

        sendNonNativeTokenCompletionSource = new TaskCompletionSource<string>();

        EventHandler<EventArgs<string>> messageHandler = null;
        messageHandler = (sender, eventArgs) => {
            Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(eventArgs.Value);
            if (dictionary.TryGetValue("type", out object typeValue))
            {
                if ((string)typeValue == "SEND_NON_NATIVE_TOKEN_RESULT")
                {
                    var sendNonNativeTokenResult = dictionary["data"];
                    sendNonNativeTokenCompletionSource.TrySetResult((string)sendNonNativeTokenResult);
                    mainWebViewPrefab.WebView.MessageEmitted -= messageHandler;
                }
            }
        };

        mainWebViewPrefab.WebView.MessageEmitted += messageHandler;

        string jsCode = $"window.SingularityEvent.sendNonNativeToken('{txData}');";
        mainWebViewPrefab.WebView.ExecuteJavaScript(jsCode);

        return sendNonNativeTokenCompletionSource.Task;
    }

    public Task<string> SendNftAsync(string txData)
    {
        if (mainWebViewPrefab == null) return Task.FromResult("Singularity not initialized");

        sendNftCompletionSource = new TaskCompletionSource<string>();

        EventHandler<EventArgs<string>> messageHandler = null;
        messageHandler = (sender, eventArgs) => {
            Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(eventArgs.Value);
            if (dictionary.TryGetValue("type", out object typeValue))
            {
                if ((string)typeValue == "SEND_NFT_RESULT")
                {
                    var sendNftResult = dictionary["data"];
                    sendNftCompletionSource.TrySetResult((string)sendNftResult);
                    mainWebViewPrefab.WebView.MessageEmitted -= messageHandler;
                }
            }
        };

        mainWebViewPrefab.WebView.MessageEmitted += messageHandler;

        string jsCode = $"window.SingularityEvent.sendNft('{txData}');";
        mainWebViewPrefab.WebView.ExecuteJavaScript(jsCode);

        return sendNftCompletionSource.Task;
    }

    public Task<string> RequestTypedSignatureAsync(string domain,string types, string message, string primaryType)
    {
        if (mainWebViewPrefab == null) return Task.FromResult("Singularity not initialized");

        requestTypedSignatureCompletionSource = new TaskCompletionSource<string>();

        EventHandler<EventArgs<string>> messageHandler = null;
        messageHandler = (sender, eventArgs) => {
            Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(eventArgs.Value);
            if (dictionary.TryGetValue("type", out object typeValue))
            {
                if ((string)typeValue == "TYPED_MESSAGE_SIGNATURE_RESULT")
                {
                    var typedMessageSignatureResult = dictionary["data"];
                    requestTypedSignatureCompletionSource.TrySetResult((string)typedMessageSignatureResult);
                    mainWebViewPrefab.WebView.MessageEmitted -= messageHandler;
                }
            }
        };

        mainWebViewPrefab.WebView.MessageEmitted += messageHandler;
        string jsCode = $"window.SingularityEvent.requestTypedSignature('{domain}', '{primaryType}', '{types}', '{message}' );";
        mainWebViewPrefab.WebView.ExecuteJavaScript(jsCode);

        return requestTypedSignatureCompletionSource.Task;
    }

    public void TransactionFlow(string txData)
    {
        if (mainWebViewPrefab == null) return;


        string jsCode = $"window.SingularityEvent.transactionFlow('{txData}');";
        mainWebViewPrefab.WebView.ExecuteJavaScript(jsCode);
    }

    private Dictionary<string, string> ParseQueryString(string uri)
    {
        var parameters = new Dictionary<string, string>();
        var parts = uri.Split('?');
        if (parts.Length >= 2)
        {
            var query = parts[1];
            foreach (var param in query.Split('&'))
            {
                var keyValue = param.Split('=');
                if (keyValue.Length == 2)
                {
                    parameters[keyValue[0]] = keyValue[1];
                }
            }
        }
        return parameters;
    }

    public void OpenDrawer()
    {
        if (mainWebViewPrefab == null) return;
        if (canvas == null) return;
        var rectTransform = mainWebViewPrefab.transform as RectTransform;
        
        Vector2 position = rectTransform.anchoredPosition;
        position.x = -200;
        
        rectTransform.anchoredPosition = position;
    }

    public void CloseDrawer()
    {
        if (mainWebViewPrefab == null) return;
        if (canvas == null) return;
        var rectTransform = mainWebViewPrefab.transform as RectTransform;

        Vector2 position = rectTransform.anchoredPosition;
        position.x = 400;

        rectTransform.anchoredPosition = position;
    }

    public void LogoutUser()
    {
        if (mainWebViewPrefab == null) return;
        string jsCode = $"window.SingularityEvent.logout();";
        mainWebViewPrefab.WebView.ExecuteJavaScript(jsCode);
    }

    void CreateBorder()
    {
        borderImage = new GameObject("WebViewBorder");
        var image = borderImage.AddComponent<Image>();
        image.color = Color.black; // Set border color

        borderImage.transform.SetParent(canvas.transform, false);

        var rectTransform = borderImage.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(700, 700);
        rectTransform.anchoredPosition = Vector2.zero;
    }

    void CloseGoogleWebView()
    {
        try
        {
            googleLoginWebView.Destroy();
        }
        catch (Exception e)
        {

        }

        try
        {
            UnityEngine.Object.Destroy(closeButton);
        }
        catch (Exception e)
        {

        }

        try
        {
            UnityEngine.Object.Destroy(borderImage);
        }
        catch (Exception e)
        {

        }

        try
        {
            UnityEngine.Object.Destroy(pleaseWaitText);
        }
        catch (Exception e)
        {

        }



    }

    void CreateCloseButton()
    {
        closeButton = new GameObject("CloseButton");
        var button = closeButton.AddComponent<Button>();
        var text = closeButton.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = "X";
        text.fontSize = 24;
        text.alignment = TMPro.TextAlignmentOptions.Center;

        button.onClick.AddListener(CloseGoogleWebView);

        closeButton.transform.SetParent(borderImage.transform, false);

        var rectTransform = closeButton.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(50, 50);
        rectTransform.anchoredPosition = new Vector2(325, 325); // Adjust as needed
    }

    private void CreatePleaseWaitText()
    {
        GameObject textObj = new GameObject("PleaseWaitText");
        pleaseWaitText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        textObj.transform.SetParent(borderImage.transform, false);

        pleaseWaitText.text = "Please Wait";
        pleaseWaitText.fontSize = 24;
        pleaseWaitText.alignment = TMPro.TextAlignmentOptions.Center;
        pleaseWaitText.color = Color.white;

        RectTransform rectTransform = textObj.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(200, 100);
        rectTransform.anchoredPosition = Vector2.zero; // Center of the parent
    }

    async void openNewWebViewAndAuth0Auth(string loginMethod)
    {
        CloseGoogleWebView();


        CreateBorder();
        CreateCloseButton();
        CreatePleaseWaitText();

        googleLoginWebView = CanvasWebViewPrefab.Instantiate();

        Canvas canvasComponent = canvas.GetComponent<Canvas>();


        googleLoginWebView.Resolution = 1f;
        googleLoginWebView.PixelDensity = 2;
        googleLoginWebView.Native2DModeEnabled = true;
        googleLoginWebView.transform.SetParent(canvas.transform, false);

        var rectTransform = googleLoginWebView.transform as RectTransform;
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        CanvasScaler canvasScaler = canvas.GetComponent<CanvasScaler>();
        float screenHeight = Screen.height;
        screenHeight = 600;

        Vector2 position = rectTransform.anchoredPosition;
        position.x = 0;
        position.y = 0;
        rectTransform.anchoredPosition = position;

        mainWebViewPrefab.transform.localScale = Vector3.one;

        if (rectTransform.sizeDelta == Vector2.zero)
        {
            rectTransform.sizeDelta = new Vector2(600, 600); // default size
        }

        await googleLoginWebView.WaitUntilInitialized();

        int width = Screen.width; // for example, 800 pixels width
        int height = Screen.height; // for example, 600 pixels height
        bool isFullScreen = Screen.fullScreen; // set to true for fullscreen

        googleLoginWebView.WebView.SetDefaultBackgroundEnabled(false);

        googleLoginWebView.WebView.UrlChanged += async (sender, eventArgs) => {
            if (eventArgs.Url.Contains("neobrix://unity"))
            {
                var queryParams = ParseQueryString(eventArgs.Url);
                var accessTkn = "";
                var idTkn = "";

                if (queryParams.TryGetValue("access_token", out string accessToken))
                {
                    Console.WriteLine("Access Token: " + accessToken);
                    accessTkn = accessToken;
                }

                if (queryParams.TryGetValue("id_token", out string idToken))
                {
                    Console.WriteLine("ID Token: " + idToken);
                    idTkn = idToken;
                }

                string jsCode = $"window.SingularityMobile.onAuthTokenReceived('{accessToken}', '{idToken}');";
                await mainWebViewPrefab.WebView.ExecuteJavaScript(jsCode);


                CloseGoogleWebView();

            }

        };

        googleLoginWebView.WebView.LoadUrl("https://auth0.s9y.gg/?loginMethod=" + loginMethod + "&platform=android&appId=unity");
    }

    public void CustomAuth(string method, string data)
    {
        if (mainWebViewPrefab == null) return;
        string jsCode = $"window.SingularityEvent.customAuth('{method}', '{data}');";
        mainWebViewPrefab.WebView.ExecuteJavaScript(jsCode);
    }


}
