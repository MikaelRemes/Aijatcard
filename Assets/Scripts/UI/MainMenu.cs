using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class MainMenu : MonoBehaviour
{
    public GameObject menuScreen;
    private bool checkLocalPlayerConnect = true;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GameObject networker = GameObject.FindGameObjectWithTag("NetworkManager");
            menuScreen.SetActive(true);
            if(networker!=null)networker.GetComponent<NetworkManagerHUD>().enabled = true;
        }
        if (checkLocalPlayerConnect)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                GameObject networker = GameObject.FindGameObjectWithTag("NetworkManager");
                LocalPlayerConnected(networker);
                checkLocalPlayerConnect = false;
            }
        }
        else
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                checkLocalPlayerConnect = true;
            }
        }
    }

    void LocalPlayerConnected(GameObject networker)
    {
        menuScreen.SetActive(false);
        if (networker != null) networker.GetComponent<NetworkManagerHUD>().enabled = false;
    }
}
