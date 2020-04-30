using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

//attach to main menu root
public class MMUserInterface : MonoBehaviour
{
    NetworkManager manager;
    public GameObject menuScreen;
    public Button EnableMMButton;
    public Button CreateLobbyButton;
    public Button JoinLobbyButton;
    public Button DisableMMButton;


    void Start()
    {
        GameObject networker = GameObject.FindGameObjectWithTag("NetworkManager");
        manager = networker.GetComponent<NetworkManager>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            menuScreen.SetActive(true);
        }
    }

    public void EnableMatchMaker()
    {
        //bool noConnection = (manager.client == null || manager.client.connection == null ||
        //        manager.client.connection.connectionId == -1);
        bool noConnection = true;
        if (!NetworkServer.active && !manager.IsClientConnected() && noConnection)
        {
            if (manager.matchMaker == null)
            {
                manager.StartMatchMaker();
                CreateLobbyButton.gameObject.SetActive(true);
                JoinLobbyButton.gameObject.SetActive(true);
                manager.matchMaker.ListMatches(0, 20, "", false, 0, 0, manager.OnMatchList);

                EnableMMButton.gameObject.SetActive(false);
                DisableMMButton.gameObject.SetActive(true);
            }
        }
    }

    public void CreateInternetLobby()
    {
        if (manager.matchInfo == null)
        {
            manager.matchName = "testlobby"; //TODO: textbox
            manager.matchMaker.CreateMatch(manager.matchName, manager.matchSize, true, "", "", "", 0, 0, manager.OnMatchCreate);
            CreateLobbyButton.gameObject.SetActive(false);
            JoinLobbyButton.gameObject.SetActive(false);
            menuScreen.SetActive(false);
        }
    }
    
    public void JoinInternetLobby()
    {
        if (manager.matchInfo == null)
        {
            if (!(manager.matches == null))
            {
                if (manager.matches.Count > 0)
                {
                    var match = manager.matches[0];
                    manager.matchName = "testlobby";
                    manager.matchMaker.JoinMatch(match.networkId, "", "", "", 0, 0, manager.OnMatchJoined);
                    menuScreen.SetActive(false);
                }
                CreateLobbyButton.gameObject.SetActive(false);
                JoinLobbyButton.gameObject.SetActive(false);
                menuScreen.SetActive(false);
            }
        }
    }

    public void DisableMatchMaker()
    {
        if (manager.IsClientConnected())
        {
            manager.StopMatchMaker();
            manager.matches = null;
            manager.StopHost();
        }
        else
        {
            manager.StopMatchMaker();
            manager.matches = null;
            manager.StopServer();
        }
        EnableMMButton.gameObject.SetActive(true);
        CreateLobbyButton.gameObject.SetActive(false);
        JoinLobbyButton.gameObject.SetActive(false);
        DisableMMButton.gameObject.SetActive(false);
    }


}
