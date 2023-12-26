using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vuplex.WebView;

public class SingularityDemo : MonoBehaviour, ISingularityUnityListener, ISingularityInitListener
{
    SingularityUnitySdk singularityUnitySdk;

    public TMP_Text logText;
    private string currentLog = "";
    public ScrollRect scrollRect; // Reference to the ScrollRect
    private String API_KEY = "2";

    private void UpdateScroll()
    {
        // Set the vertical scrollbar value to 0 to scroll to the bottom
        scrollRect.verticalNormalizedPosition = 0f;
    }

    // Start is called before the first frame update
    async void Start()
    {
        singularityUnitySdk = new SingularityUnitySdk(this);
    }

    public void openDrawer()
    {
        singularityUnitySdk.OpenDrawer();
    }

    public void closeDrawer()
    {
        singularityUnitySdk.CloseDrawer();
    }

    void AppendToLog(string message)
    {
        currentLog += message + "\n\n";
        logText.text = currentLog;

        // Call this after a frame to ensure the content size is updated
        Invoke("UpdateScroll", 0.1f);
    }



    public void initializeSingularity()
    {
        AppendToLog("Initializing with Api key: " + API_KEY + ", Please wait...");
        var canvas = GameObject.Find("SingularitySdkCanvas");
        Canvas canvasComponent = canvas.GetComponent<Canvas>();
        singularityUnitySdk.InitializeSingularity(API_KEY, canvas, this);
    }

    async public void testSignTransactionFlow()
    {
        Debug.Log("testSignTransactionFlow.insde");

        Dictionary<string, string> txData = new Dictionary<string, string>();
        txData["value"] = "0.001";
        txData["to"] = "0xCA4511435F99dcbf3Ab7cba04C8A16721eB7b894";

        string jsonString = JsonConvert.SerializeObject(txData);
        Debug.Log("txDataJson:" + jsonString);

        Debug.Log("txData which wil be sent:" + jsonString);
        var res = await singularityUnitySdk.SignTransactionAsync(jsonString);
        Debug.Log("testSignTransactionFlow.result:" + res);

        AppendToLog("testSignTransactionFlow result: " + res);

    }

    async public void testSignAndSendTransactionFlow()
    {
        Debug.Log("testSignAndSendTransactionFlow.insde");

        Dictionary<string, string> txData = new Dictionary<string, string>();
        txData["value"] = "0.001";
        txData["to"] = "0xCA4511435F99dcbf3Ab7cba04C8A16721eB7b894";

        string jsonString = JsonConvert.SerializeObject(txData);
        Debug.Log("txDataJson:" + jsonString);

        var res = await singularityUnitySdk.SignAndSendTransactionAsync(jsonString);
        Debug.Log("testSignAndSendTransactionFlow.result:" + res);
        AppendToLog("testSignAndSendTransactionFlow result: " + res);

    }

    async public void testSignPersonalMessageFlow()
    {
        Debug.Log("testSignPersonalMessageFlow.insde");

        var res = await singularityUnitySdk.SignPersonalMessageAsync("Personal Message Signature Test");
        Debug.Log("testSignPersonalMessageFlow.result:" + res);
        AppendToLog("testSignPersonalMessageFlow result: " + res);

    }

    public void logoutUser()
    {
        singularityUnitySdk.LogoutUser();
    }

    public async void getUserInfo()
    {
        var res = await singularityUnitySdk.GetConnectedUserInfoAsync();
        Debug.Log("getUserInfo.result:" + res);
        AppendToLog("getUserInfo result: "+res);
    }


    // Update is called once per frame
    void Update()
    {
        
    }

    public void onUserLogout()
    {
        AppendToLog("Event Received: onUserLogout");
    }

    public void onUserLogIn(string userData)
    {
        AppendToLog("Event Received: onUserLogIn; Data: " + userData);
    }

    public void onDrawerOpen()
    {
        AppendToLog("Event Received: onDrawerOpen");
    }

    public void onDrawerClose()
    {
        AppendToLog("Event Received: onDrawerClose");
    }

    public void onTransactionApprove(string txData)
    {
        AppendToLog("Event Received: onTransactionApprove; Data: " + txData);
    }

    public void onTransactionSuccess(string txData)
    {
        AppendToLog("Event Received: onTransactionSuccess; Data: " + txData);
    }

    public void onTransactionFailure(string txData)
    {
        AppendToLog("Event Received: onTransactionFailure; Data: " + txData);
    }

    public void onInitSuccess()
    {
        AppendToLog("Event Received: onInitSuccess");
    }
}
