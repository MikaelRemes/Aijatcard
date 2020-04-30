using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DrawCard : MonoBehaviour
{
    GameObject localPlayer;
    
    public void DrawACard()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            if (p.GetComponent<NetworkIdentity>().isLocalPlayer) localPlayer = p;
        }
        localPlayer.GetComponent<Player>().CmdChangeAction("drw");
    }
}
