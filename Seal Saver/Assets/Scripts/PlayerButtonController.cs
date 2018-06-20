﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerButtonController : MonoBehaviour
{
    public Text buttonText;
    public int playerIndex;
    //public static int currentPlayer;

    public void SetText(string text, int index)
    {
        buttonText.text = text;
        playerIndex = index;
    }

    public void OnClick()
    {
        //Debug.Log(playerIndex);
        SyncTables.currentPlayerIndex = playerIndex;
        //Debug.Log(playerIndex);
        var cols = SyncTables.playerCoins[playerIndex - 1];
        int gems;
        int.TryParse(cols, out gems);
        PlayerPrefs.SetString("CurrentPlayerName", buttonText.text);
        PlayerPrefs.SetInt("Gems", gems);
        PlayerPrefs.Save();
        SyncTables.isLoggingIn = true;
        //SyncTables.syncDownloadNow = true;
        SceneManager.LoadScene(3);
    }
}