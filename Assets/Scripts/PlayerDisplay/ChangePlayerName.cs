using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class ChangePlayerName : NetworkBehaviour
{
    public Text newNameTextBox;
    private string playerTag = "Player";

    public void ChangePlayerNameMethod()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
        foreach (GameObject p in players)
        {
            if (p.GetComponent<NetworkIdentity>().isLocalPlayer) p.GetComponent<Player>().CmdChangeName(newNameTextBox.text);
        }
    }
}
