﻿using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Firebase.Auth;
using UnityEngine.Networking;

public class Register : MonoBehaviour {
    
    public InputField Mail;
    public InputField Password;
    public Text errorTextSignUp;
    public static bool registered = false;
    public GameObject emailField;
    public GameObject passwordField;
    public GameObject registerButton;
    public GameObject nextButton;
    public GameObject trialText;
    public GameObject bulletPointsText;
    public GameObject facebookButton;
    public GameObject orText;
    public string email;
    public string password;
    public string deviceModel;
    public FirebaseAuth auth;
    public GameObject loadingScreen;
    public GameObject termsConditions;

    void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
    }

    private void Start()
    {
        nextButton.SetActive(true);
        trialText.SetActive(true); ;
        bulletPointsText.SetActive(true);
        emailField.SetActive(false);
        registerButton.SetActive(false);
        passwordField.SetActive(false);
        termsConditions.SetActive(false);
        facebookButton.SetActive(false);
        orText.SetActive(false);
        errorTextSignUp.text = "";
        deviceModel = SystemInfo.deviceModel.ToLower();
        //Amazon Device check
        if (!deviceModel.Contains("amazon"))
        {
            InitializeFirebase();
        }
    }

    public void GoSignUpPage2()
    {
        errorTextSignUp.text = "";
        emailField.SetActive(true);
        registerButton.SetActive(true);
        passwordField.SetActive(true);
        termsConditions.SetActive(true);
        orText.SetActive(true);
        nextButton.SetActive(false);
        trialText.SetActive(false);
        bulletPointsText.SetActive(false);
        //Amazon Device check
        if (!deviceModel.Contains("amazon"))
        {
            facebookButton.SetActive(true);
        }
    }

    public void Launch()
    {
        loadingScreen.SetActive(true);
        errorTextSignUp.text = "";
        //Check for errors
        if (Mail.text == "" || Password.text == "")
        {
            errorTextSignUp.text = "Please complete all fields";
            loadingScreen.SetActive(false);
            return;
        }
        else if (Mail.text.Length < 6 || !Mail.text.Contains("@") || Mail.text[0].Equals('@') || Mail.text[0].Equals('.'))
        {
            errorTextSignUp.text = "Invalid Email";
            loadingScreen.SetActive(false);
            return;
        }
        else if (Password.text.Length < 6)
        {
            errorTextSignUp.text = "Password should contain at least 6 characters";
            loadingScreen.SetActive(false);
            return;
        }
        email = Mail.text.ToLower();
        password = Password.text;
        errorTextSignUp.text = "";
        deviceModel = SystemInfo.deviceModel.ToLower();
        //Amazon Device check
        if (!deviceModel.Contains("amazon"))
        {
            auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWith(task => {
                if (task.IsCanceled)
                {
                    Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
                    loadingScreen.SetActive(false);
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError("CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                    loadingScreen.SetActive(false);
                    errorTextSignUp.text = "Oops - this is not a valid email address or it has already been used";
                    return;
                }

                //Firebase user has been created.
                FirebaseUser newUser = task.Result;
                //Debug.LogFormat("Firebase user created successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);
                SyncTables.firebaseUID = newUser.UserId;
                //Save information in playerPrefs
                PlayerPrefs.SetString("Username", email);
                PlayerPrefs.SetString("Mail", email);
                PlayerPrefs.SetString("Password", password);
                Debug.Log("Registration succeeded.");
                registered = true;
                StartCoroutine(SendFirebaseUID());
            });
        }
        else
        {
            StartCoroutine(SendDetails());
        }
    }

    IEnumerator SendFirebaseUID()
    {
        string sendUIDURL = "https://edplus.net/createUser";
        var request = new UnityWebRequest(sendUIDURL, "POST");
        SendFirebaseUIDJSON sendUIDJSON = new SendFirebaseUIDJSON()
        {
            FirebaseUID = SyncTables.firebaseUID,
            Mail = email
        };
        string json = JsonUtility.ToJson(sendUIDJSON);
        //Debug.Log(json);
        byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        yield return request.SendWebRequest();
        while (!request.isDone)
        {
            yield return null;
        }
        //Debug.Log("Response: " + request.downloadHandler.text);
    }

    IEnumerator SendDetails()
    {
        //Debug.Log("SEND DETAILS");
        string registerUserURL = "https://edplus.net/registerUser";
        var request = new UnityWebRequest(registerUserURL, "POST");
        SendAuthDetailsJSON sendAuthDetailsJSON = new SendAuthDetailsJSON()
        {
            UserID = email,
            Password = password
        };
        string json = JsonUtility.ToJson(sendAuthDetailsJSON);
        byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
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
            if(sendAuthDetailsJSONResponse.data == "EMAIL_EXISTS")
            {
                errorTextSignUp.text = "Email already exists";
                registered = false;
                loadingScreen.SetActive(false);
                yield return null;
            }
            else
            {
                errorTextSignUp.text = "Please try again";
                loadingScreen.SetActive(false);
                yield return null;
            }
        }
        else
        {
            SyncTables.firebaseUID = sendAuthDetailsJSONResponse.data;
            PlayerPrefs.SetString("Username", email);
            PlayerPrefs.SetString("Mail", email);
            PlayerPrefs.SetString("Password", password);
            //Debug.Log("Registration succeeded.");
            registered = true;
            StartCoroutine(SendFirebaseUID());
        }
    }
}
