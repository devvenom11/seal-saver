﻿using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using Firebase.Auth;

public class Login : MonoBehaviour {

    public InputField Username;
    public InputField Password;
    public Text errorTextSignIn;
    public static bool loggedIn;
    public static string user;
    public static string userID;
    public static string email;
    public string password;
    public GameObject loggedInMessage;
    public Button loggedInMessageButton;
    public GameObject loadingScreen;
    public static bool subscribed;
    public static bool newUser;
    public FirebaseAuth auth;

    void Start()
    {
        // Prefill fields with saved datas
        //LoadPlayerPrefs();
        loggedInMessage.SetActive(false);
        string deviceModel = SystemInfo.deviceModel.ToLower();
        //Amazon Device check
        if (!deviceModel.Contains("amazon"))
        {
            //Debug.Log("NOT AMAZON");
            InitializeFirebase();
        }
        if (PlayerPrefs.HasKey("Username") && PlayerPrefs.HasKey("Password"))
        {
            loadingScreen.SetActive(true);
            Username.text = PlayerPrefs.GetString("Username");
            Password.text = PlayerPrefs.GetString("Password");
            Launch();
        }
    }

    void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
    }

    private void Update()
    {
        if (Register.registered == true && loggedIn == false)
        {
            //Deactivate flag first
            Register.registered = false;
            //Obtain valus from Playerprefs, which was saved while registering
            if (PlayerPrefs.HasKey("Username") && PlayerPrefs.HasKey("Password"))
            {
                Username.text = PlayerPrefs.GetString("Username");
                Password.text = PlayerPrefs.GetString("Password");
                newUser = true;
                Launch();
            }
        }
    }

    public void LoadPlayerPrefs()
    {
        // Prefill fields with saved datas
        if (PlayerPrefs.HasKey("Username") && PlayerPrefs.HasKey("Password"))
        {
            Username.text = PlayerPrefs.GetString("Username");
            Password.text = PlayerPrefs.GetString("Password");
        }
    }

    public void Launch()
    {
        loadingScreen.SetActive(true);
        errorTextSignIn.text = "";
        if (Username.text == "" || Password.text == "")
        {
            errorTextSignIn.text = "Please complete all fields";
            loadingScreen.SetActive(false);
            return;
        }
        //Debug.Log("Launch Login");
        email = Username.text.ToLower();
        password = Password.text;
        string deviceModel = SystemInfo.deviceModel.ToLower();
        //Amazon Device check
        if (!deviceModel.Contains("amazon"))
        {
            auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWith(task => {
                if (task.IsCanceled)
                {
                    Debug.LogError("SignInWithEmailAndPasswordAsync was canceled.");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("SignInWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                    // Show the error
                    if (SyncTables.internetLogin == false)
                    {
                        errorTextSignIn.text = "Not connected to Internet";
                    }
                    else
                    {
                        errorTextSignIn.text = "Invalid Email/Password";
                    }
                    loadingScreen.SetActive(false);
                    loggedIn = false;
                    return;
                }
                Firebase.Auth.FirebaseUser newUser = task.Result;
                //Debug.LogFormat("User signed in successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);
                SyncTables.firebaseUID = newUser.UserId;
                user = email;
                PlayerPrefs.SetString("Username", email);
                PlayerPrefs.SetString("Password", password);
                loggedIn = true;
                StartCoroutine(GetUID());
            });
        }
        else
        {
            StartCoroutine(SendDetails());
        }
    }

    IEnumerator SendDetails()
    {
        //Debug.Log("SEND DETAILS");
        string loginUserURL = "https://edplus.net/loginUser";
        var request = new UnityWebRequest(loginUserURL, "POST");
        SendAuthDetailsJSON sendAuthDetailsJSON = new SendAuthDetailsJSON()
        {
            UserID = email,
            Password = password
        };
        string json = JsonUtility.ToJson(sendAuthDetailsJSON);
        byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(json);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        while (!request.isDone)
        {
            yield return null;
        }
        //Debug.Log("Response: " + request.downloadHandler.text);
        SendAuthDetailsJSONResponse sendAuthDetailsJSONResponse = JsonUtility.FromJson<SendAuthDetailsJSONResponse>(request.downloadHandler.text);
        if (sendAuthDetailsJSONResponse.status != "success")
        {
            //Debug.Log(sendAuthDetailsJSONResponse.data);
            if (sendAuthDetailsJSONResponse.data == "EMAIL_NOT_FOUND" || sendAuthDetailsJSONResponse.data == "INVALID_PASSWORD")
            {
                errorTextSignIn.text = "Invalid Email/Password";
                yield return null;
            }
            else if (SyncTables.internetLogin == false)
            {
                errorTextSignIn.text = "Not connected to Internet";
            }
            else
            {
                errorTextSignIn.text = "PLease try again";
                yield return null;
            }
        }
        else
        {
            //Debug.Log(sendAuthDetailsJSONResponse.data);
            SyncTables.firebaseUID = sendAuthDetailsJSONResponse.data;
            user = email;
            PlayerPrefs.SetString("Username", email);
            PlayerPrefs.SetString("Password", password);
            loggedIn = true;
            StartCoroutine(GetUID());
        }
    }

    IEnumerator GetUID()
    {
        //Debug.Log("Getting UID");
        string findUIDURL = "https://edplus.net/findUID";
        var request = new UnityWebRequest(findUIDURL, "POST");
        //Debug.Log(SyncTables.firebaseUID);
        FindUIDJSON findUIDJSON = new FindUIDJSON()
        {
            FirebaseUID = SyncTables.firebaseUID
        };
        string json = JsonUtility.ToJson(findUIDJSON);
        byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(json);
        request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();
        while (!request.isDone)
        {
            yield return null;
        }
        //Debug.Log("Response: " + request.downloadHandler.text);
        FindUIDJSONResponse findUIDJSONResponse = JsonUtility.FromJson<FindUIDJSONResponse>(request.downloadHandler.text);
        if (findUIDJSONResponse.status != "success")
        {
            Debug.Log(findUIDJSONResponse.data);
        }
        else
        {
            var cols = findUIDJSONResponse.data.Split('&');
            userID = cols[0];
            if (cols[1] == "1")
            {
                subscribed = true;
            }
            else
            {
                subscribed = false;
            }
            //Debug.Log(userID + " " + subscribed);
        }
        ShowLoggedInMessage();
        if (newUser)
        {
            newUser = false;
            SyncTables.playerData.Clear();
            SyncTables.playerCoins.Clear();
        }
        else
        {
            SyncTables.getStarsAndLevels = true;
        }
        loadingScreen.SetActive(false);
        loggedInMessageButton.onClick.Invoke();
    }

    public void ShowLoggedInMessage()
    {
        loggedInMessage.SetActive(true);
    }
}
