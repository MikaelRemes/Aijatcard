using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class ChangePlayerReset : MonoBehaviour
{
    GameObject localPlayer;

    public void changeReady(bool reset)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            if (p.GetComponent<NetworkIdentity>().isLocalPlayer) localPlayer = p;
        }
        localPlayer.GetComponent<Player>().CmdChangeReset(reset);
    }

    private void Update()
    {
        if (localPlayer != null)
        {
            if (localPlayer.GetComponent<Player>().reset) GetComponent<Image>().color = new Color(0f, 1f, 0f);
            else GetComponent<Image>().color = new Color(0f, 0.8220968f, 0.8301887f);
        }
    }
}
