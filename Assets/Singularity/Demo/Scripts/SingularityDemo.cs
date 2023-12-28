using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.CognitoIdentityProvider;
using Amazon.Extensions.CognitoAuthentication;
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

    public TMP_InputField emailInputField;
    public TMP_InputField passwordInputField;

    // the AWS region of where your services live
    public static Amazon.RegionEndpoint Region = Amazon.RegionEndpoint.APSouth1;

    // In production, should probably keep these in a config file
    const string IdentityPool = "ap-south-1_Kos4NB6dc"; //insert your Cognito User Pool ID, found under General Settings
    const string AppClientID = "348bbcqas087cp1gdcfc9ddib3"; //insert App client ID, found under App Client Settings
    const string userPoolId = "ap-south-1_Kos4NB6dc";

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
        Dictionary<string, string> txData = new Dictionary<string, string>();
        txData["value"] = "0.001";
        txData["to"] = "0xCA4511435F99dcbf3Ab7cba04C8A16721eB7b894";

        string jsonString = JsonConvert.SerializeObject(txData);
        var res = await singularityUnitySdk.SignTransactionAsync(jsonString);

        AppendToLog("testSignTransactionFlow result: " + res);
    }

    async public void testSignAndSendTransactionFlow()
    {
        Dictionary<string, string> txData = new Dictionary<string, string>();
        txData["value"] = "0.001";
        txData["to"] = "0xCA4511435F99dcbf3Ab7cba04C8A16721eB7b894";

        string jsonString = JsonConvert.SerializeObject(txData);

        var res = await singularityUnitySdk.SignAndSendTransactionAsync(jsonString);
        AppendToLog("testSignAndSendTransactionFlow result: " + res);

    }

    async public void testSignPersonalMessageFlow()
    {
        var res = await singularityUnitySdk.SignPersonalMessageAsync("Personal Message Signature Test");
        AppendToLog("testSignPersonalMessageFlow result: " + res);

    }

    public void testPaymentsFlow()
    {
        Dictionary<string, object> txData = new Dictionary<string, object>();
        txData["clientReferenceId"] = "My_REF_ID";
        txData["singularityTransactionType"] = "RECEIVE";
        txData["transactionLabel"] = "Demo unity label";
        txData["transactionDescription"] = "Description";
        txData["transactionIconLink"] = "https://singularity-icon-assets.s3.ap-south-1.amazonaws.com/currency/lode.svg";

        Dictionary<string, string> clientReceiveObject = new Dictionary<string, string>();
        clientReceiveObject["clientRequestedAssetQuantity"] = "0.001";
        clientReceiveObject["clientRequestedAssetId"] = "800011";

        txData["clientReceiveObject"] = clientReceiveObject;

        string jsonString = JsonConvert.SerializeObject(txData);
        singularityUnitySdk.TransactionFlow(jsonString);
    }

    public async void testSendingNonNativeToken()
    {
        Dictionary<string, object> txData = new Dictionary<string, object>();
        txData["recipient"] = "0x236B0bDC580d1Dd1bC1C182c925719b025aD239B";
        txData["tokenAddress"] = "0x0FA8781a83E46826621b3BC094Ea2A0212e71B23";
        txData["amount"] = "0.001";
      
        string jsonString = JsonConvert.SerializeObject(txData);
        var res = await singularityUnitySdk.SendNonNativeTokenAsync(jsonString);

        AppendToLog("testSendingNonNativeToken result: " + res);
    }

    public async void testSendingNft()
    {
        Dictionary<string, object> txData = new Dictionary<string, object>();
        txData["nftType"] = "ERC1155";
        txData["nftId"] = "0";
        txData["contractAddress"] = "0x572954a0db4bda484cebbd6e50dba519d35230bc";
        txData["recipient"] = "0x17F547ae02a94a0339c4CFE034102423907c4592";
        txData["quantity"] = "1";

        string jsonString = JsonConvert.SerializeObject(txData);

        var res = await singularityUnitySdk.SendNftAsync(jsonString);

        AppendToLog("testSendingNft result: " + res);
    }

    public async void testRequestTypedSignature()
    {
        String domainString = "{'name':'GamePay','version':'1','chainId':97,'verifyingContract':'0xED975dB5192aB41713f0080E7306E08188e53E7f'}";
        String typesString = "{'bid':[{'name':'bidder','type':'address'},{'name':'collectableId','type':'uint256'},{'name':'amount','type':'uint256'},{'name':'nounce','type':'uint'}]}";
        String messageString = "{'bidder':'0xAa81f641d4b3546F05260F49DEc69Eb0314c47De','collectableId':1,'amount':100,'nounce':1}";
        String primaryType = "bid";

        Dictionary<string, object> domainDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(domainString);
        string domainJsonString = JsonConvert.SerializeObject(domainDictionary);

        Dictionary<string, object> typesDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(typesString);
        string typesJsonString = JsonConvert.SerializeObject(typesDictionary);


        Dictionary<string, object> messageDictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(messageString);
        string messageJsonString = JsonConvert.SerializeObject(messageDictionary);
  
        var res = await singularityUnitySdk.RequestTypedSignatureAsync(domainJsonString, typesJsonString, messageJsonString, primaryType);
        AppendToLog("testRequestTypedSignature result: " + res);
    }

    public void logoutUser()
    {
        singularityUnitySdk.LogoutUser();
    }

    public async void getUserInfo()
    {
        var res = await singularityUnitySdk.GetConnectedUserInfoAsync();
        AppendToLog("getUserInfo result: "+res);
    }

    public async void testCognitoLoginAsync()
    {
        string email = emailInputField.text;
        string password = passwordInputField.text;

        AmazonCognitoIdentityProviderClient provider =
            new AmazonCognitoIdentityProviderClient(new Amazon.Runtime.AnonymousAWSCredentials());
        CognitoUserPool userPool = new CognitoUserPool("ap-south-1_Kos4NB6dc", "348bbcqas087cp1gdcfc9ddib3", provider);
        CognitoUser user = new CognitoUser(email, "348bbcqas087cp1gdcfc9ddib3", userPool, provider);
        InitiateSrpAuthRequest authRequest = new InitiateSrpAuthRequest()
        {
            Password = password
        };

        AuthFlowResponse authResponse = await user.StartWithSrpAuthAsync(authRequest).ConfigureAwait(false);
        var accessToken = authResponse.AuthenticationResult.AccessToken;
        var idToken = authResponse.AuthenticationResult.IdToken;

        Dictionary<string, string> dataObj = new Dictionary<string, string>();
        dataObj["idToken"] = idToken;
        dataObj["accessToken"] = accessToken;
        string jsonString = JsonConvert.SerializeObject(dataObj);
        singularityUnitySdk.CustomAuth("COGNITO", jsonString);
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
