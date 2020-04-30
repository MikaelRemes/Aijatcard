using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayersDisplay : MonoBehaviour
{
    public List<GameObject> playerDisplays = new List<GameObject>();
    private float updateTimer = 0f;

    void Update()
    {
        if (updateTimer > 1)
        {
            //sorted player list
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player").OrderBy(go => go.name).ToArray();
            int currentHandledPlayer = 0;
            foreach (GameObject panel in playerDisplays)
            {
                if (panel.GetComponent<PlayerDisplay>() != null)
                {
                    if (currentHandledPlayer >= players.Length) panel.SetActive(false);
                    else
                    {
                        panel.SetActive(true);
                        panel.GetComponent<PlayerDisplay>().displayedPlayer = players[currentHandledPlayer].GetComponent<Player>();
                        currentHandledPlayer++;
                    }
                }
            }
            updateTimer = 0f;
        }
        else
        {
            updateTimer += Time.deltaTime;
        }
    }
}
