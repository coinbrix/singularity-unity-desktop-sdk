using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Vuplex.WebView;
using Newtonsoft.Json;

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

    private int CALL_TIMEOUT_SECONDS = 10;

    public SingularityUnitySdk(ISingularityUnityListener listener)
    {
        _listener = listener;
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
        Web.EnableRemoteDebugging();
        Web.SetUserAgent("UnityDesktop");

        time1 = GetCurrentTimeInMilliseconds();

        _initListener = initListener;

        this.canvas = canvas;

        // todo - should be at the right and be hidden
        //Web.SetUserAgent("UnityDesktop");
        mainWebViewPrefab = CanvasWebViewPrefab.Instantiate();

        //todo - to be hidden at the begining
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
            Debug.Log("Reference Resolution: " + referenceResolution.x + "x" + referenceResolution.y);
            screenHeight = referenceResolution.y;
        }

        Vector2 position = rectTransform.anchoredPosition;
        position.x = -200;
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

        // TODO - to be removed at the end
        mainWebViewPrefab.LogConsoleMessages = true;

        mainWebViewPrefab.WebView.SetDefaultBackgroundEnabled(false);
        canvasComponent.enabled = true;

        mainWebViewPrefab.WebView.MessageEmitted += Controls_MessageEmitted;
        //mainWebViewPrefab.WebView.LoadUrl("http://localhost:9094/?api_key=53935&env_key=0");
        mainWebViewPrefab.WebView.LoadUrl("https://mobile-sdk.s9y.gg?api_key="+apiKey+ "&env_key=1");
    }

    private void Controls_MessageEmitted(object sender, EventArgs<string> eventArgs)
    {
        try
        {
            Debug.Log("Controls_MessageEmitted.inside value: " + eventArgs.Value);
            var message = eventArgs.Value;

            Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(eventArgs.Value);

            Debug.Log("Controls_MessageEmitted.inside deserialized");
            Debug.Log(dictionary);

            if (dictionary.TryGetValue("type", out object typeValue))
            {
                Debug.Log("Got type: " + typeValue);
                if ((string)typeValue == "SOCIAL_LOGIN_BUTTON_CLICKED")
                {
                    Debug.Log("----- SOCIAL_LOGIN_BUTTON_CLICKED.inside -----");
                    var loginMethod = dictionary["loginMethod"];
                    Debug.Log("----- SOCIAL_LOGIN_BUTTON_CLICKED.inside loginMethod-----" + loginMethod);
                    openNewWebViewAndAuth0Auth((string)loginMethod);
                }

                if ((string)typeValue == "SOCIAL_LOGOUT_EVENT")
                {
                    Debug.Log("----- SOCIAL_LOGOUT_EVENT.inside -----");
                    _listener.onUserLogout();
                }

                if ((string)typeValue == "SOCIAL_LOGIN_EVENT")
                {
                    Debug.Log("----- SOCIAL_LOGIN_EVENT.inside -----");
                    var data = dictionary["data"];
                    Debug.Log("data");
                    Debug.Log(data);
                    _listener.onUserLogIn((string)data);
                }

                if ((string)typeValue == "DRAWER_OPEN_EVENT")
                {
                    Debug.Log("----- DRAWER_OPEN_EVENT.inside -----");
                    _listener.onDrawerOpen();
                }

                if ((string)typeValue == "DRAWER_CLOSE_EVENT")
                {
                    Debug.Log("----- DRAWER_CLOSE_EVENT.inside -----");
                    _listener.onDrawerClose();
                }

                if ((string)typeValue == "ON_TRANSACTION_APPROVAL_EVENT")
                {
                    Debug.Log("----- ON_TRANSACTION_APPROVAL_EVENT.inside -----");
                    var data = dictionary["data"];
                    Debug.Log("----- ON_TRANSACTION_APPROVAL_EVENT.inside data-----" + data);
                    _listener.onTransactionApprove((string)data);
                }

                if ((string)typeValue == "ON_TRANSACTION_SUCCESS_EVENT")
                {
                    Debug.Log("----- ON_TRANSACTION_SUCCESS_EVENT.inside -----");
                    var data = dictionary["data"];
                    Debug.Log("----- ON_TRANSACTION_SUCCESS_EVENT.inside data-----" + data);
                    _listener.onTransactionSuccess((string)data);
                }

                if ((string)typeValue == "ON_TRANSACTION_FAILURE_EVENT")
                {
                    Debug.Log("----- ON_TRANSACTION_FAILURE_EVENT.inside -----");
                    var data = dictionary["data"];
                    Debug.Log("----- ON_TRANSACTION_FAILURE_EVENT.inside data-----" + data);
                    _listener.onTransactionFailure((string)data);
                }

                if ((string)typeValue == "INIT_SUCCESS_EVENT")
                {
                    Debug.Log("----- INIT_SUCCESS_EVENT.inside -----");
                    time3 = GetCurrentTimeInMilliseconds();
                    Debug.Log("Time1: " + time1);
                    Debug.Log("Time2: " + time2);
                    Debug.Log("Time3: " + time3);

                    Debug.Log("WebView init time: " + (time2-time1));
                    Debug.Log("Url Loading time: " + (time3 - time2));
                    Debug.Log("Total time: " + (time3 - time1)); 

                    _initListener.onInitSuccess();
                }

            }

        }
        catch (Exception e)
        {

        }
    }

    public Task<string> GetConnectedUserInfoAsync()
    {
        userInfoCompletionSource = new TaskCompletionSource<string>();

        System.EventHandler<EventArgs<string>> messageHandler = null;
        messageHandler = (sender, eventArgs) => {
            Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(eventArgs.Value);
            Debug.Log("messageHandler.inside deserialized");
            Debug.Log(dictionary);
            if (dictionary.TryGetValue("type", out object typeValue))
            {
                Debug.Log("Got type: " + typeValue);
                if ((string)typeValue == "USER_INFO_RESULT")
                {
                    Debug.Log("----- USER_INFO_RESULT.inside -----");
                    var userData = dictionary["data"];
                    Debug.Log("----- USER_INFO_RESULT.inside userData-----" + userData);
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
        signTransactionCompletionSource = new TaskCompletionSource<string>();

        EventHandler<EventArgs<string>> messageHandler = null;
        messageHandler = (sender, eventArgs) => {
            Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(eventArgs.Value);
            Debug.Log("SignTransactionAsync messageHandler.inside deserialized");
            Debug.Log(dictionary);
            if (dictionary.TryGetValue("type", out object typeValue))
            {
                Debug.Log("Got type: " + typeValue);
                if ((string)typeValue == "SIGN_TRANSACTION_RESULT")
                {
                    Debug.Log("----- SIGN_TRANSACTION_RESULT.inside -----");
                    var signTxnResult = dictionary["data"];
                    Debug.Log("----- SIGN_TRANSACTION_RESULT.inside userData-----" + signTxnResult);
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
        signPersonalMessageCompletionSource = new TaskCompletionSource<string>();

        EventHandler<EventArgs<string>> messageHandler = null;
        messageHandler = (sender, eventArgs) => {
            Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(eventArgs.Value);
            Debug.Log("SignPersonalMessageAsync messageHandler.inside deserialized");
            Debug.Log(dictionary);
            if (dictionary.TryGetValue("type", out object typeValue))
            {
                Debug.Log("Got type: " + typeValue);
                if ((string)typeValue == "SIGN_PERSONAL_MESSAGE_RESULT")
                {
                    Debug.Log("----- SIGN_PERSONAL_MESSAGE_RESULT.inside -----");
                    var signTxnResult = dictionary["data"];
                    Debug.Log("----- SIGN_PERSONAL_MESSAGE_RESULT.inside userData-----" + signTxnResult);
                    signPersonalMessageCompletionSource.TrySetResult((string)signTxnResult);
                    mainWebViewPrefab.WebView.MessageEmitted -= messageHandler;
                }
            }
        };

        mainWebViewPrefab.WebView.MessageEmitted += messageHandler;

        Debug.Log("Before execute js");
        string jsCode = $"window.SingularityEvent.requestPersonalSignature('{message}');";
        Debug.Log(jsCode);
        mainWebViewPrefab.WebView.ExecuteJavaScript(jsCode);

        return signPersonalMessageCompletionSource.Task;
    }

    public Task<string> SignAndSendTransactionAsync(string txData)
    {
        signAndSendTransactionCompletionSource = new TaskCompletionSource<string>();

        EventHandler<EventArgs<string>> messageHandler = null;
        messageHandler = (sender, eventArgs) => {
            Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(eventArgs.Value);
            Debug.Log("SignAndSendTransactionAsync messageHandler.inside deserialized");
            Debug.Log(dictionary);
            if (dictionary.TryGetValue("type", out object typeValue))
            {
                Debug.Log("Got type: " + typeValue);
                if ((string)typeValue == "SIGN_AND_SEND_TRANSACTION_RESULT")
                {
                    Debug.Log("----- SIGN_AND_SEND_TRANSACTION_RESULT.inside -----");
                    var signTxnResult = dictionary["data"];
                    Debug.Log("----- SIGN_AND_SEND_TRANSACTION_RESULT.inside userData-----" + signTxnResult);
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

    public void TransactionFlow(string txData)
    {
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
        string jsCode = $"window.SingularityEvent.open();";
        mainWebViewPrefab.WebView.ExecuteJavaScript(jsCode);
    }

    public void CloseDrawer()
    {
        string jsCode = $"window.SingularityEvent.close();";
        mainWebViewPrefab.WebView.ExecuteJavaScript(jsCode);
    }

    public void LogoutUser()
    {
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




        Debug.Log("openNewWebViewAndStartGoogleAuth.inside creating new webview" + loginMethod);
        // Instantiate a second webview above the first to show a UI that
        // displays the current URL and provides back / forward navigation buttons.
        //googleLoginWebView = WebViewPrefab.Instantiate(0.6f, 0.05f);
        //googleLoginWebView.KeyboardEnabled = true;
        //googleLoginWebView.transform.parent = mainWebViewPrefab.transform;
        //googleLoginWebView.transform.localPosition = new Vector3(0, 0.06f, 0);
        //googleLoginWebView.transform.localEulerAngles = Vector3.zero;

        //googleLoginWebView.LogConsoleMessages = true;

        //await Task.WhenAll(new Task[] {
        //   googleLoginWebView.WaitUntilInitialized(),
        //});


        googleLoginWebView = CanvasWebViewPrefab.Instantiate();

        //todo - to be hidden at the begining
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
        //if (canvasScaler != null)
        //{
        //    Vector2 referenceResolution = canvasScaler.referenceResolution;
        //    Debug.Log("Reference Resolution: " + referenceResolution.x + "x" + referenceResolution.y);
        //    screenHeight = referenceResolution.y;
        //}
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


        // Change the resolution
        //Screen.SetResolution(width + 1, height + 1, isFullScreen);

        // TODO - to be removed at the end
        googleLoginWebView.LogConsoleMessages = true;

        googleLoginWebView.WebView.SetDefaultBackgroundEnabled(false);
        //canvasComponent.enabled = true;



        googleLoginWebView.WebView.UrlChanged += async (sender, eventArgs) => {
            Debug.Log("googleLoginWebView url changed:" + eventArgs.Url);
            if (eventArgs.Url.Contains("neobrix://unity"))
            {
                Debug.Log("googleLoginWebView url changed contains callback");
                var queryParams = ParseQueryString(eventArgs.Url);
                Debug.Log("googleLoginWebView queryParams");
                Debug.Log(queryParams);
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

                Debug.Log("googleLoginWebView accessTkn:" + accessTkn);
                Debug.Log("googleLoginWebView idTkn:" + idTkn);

                string jsCode = $"window.SingularityMobile.onAuthTokenReceived('{accessToken}', '{idToken}');";
                await mainWebViewPrefab.WebView.ExecuteJavaScript(jsCode);


                CloseGoogleWebView();

            }

        };

        googleLoginWebView.WebView.LoadUrl("https://auth0.s9y.gg/?loginMethod=" + loginMethod + "&platform=android&appId=unity");
    }

    public void CustomAuth(string method, string data)
    {
        string jsCode = $"window.SingularityEvent.customAuth('{method}', '{data}');";
        mainWebViewPrefab.WebView.ExecuteJavaScript(jsCode);
    }


}
