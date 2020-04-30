using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerDisplay : MonoBehaviour
{
    public Player displayedPlayer;
    public Image backGround;
    public Text playerNameText;
    public Text playerLifeText;
    public Text playerMoniesText;
    public Text playerCardsText;

    void Update()
    {
        if (displayedPlayer != null)
        {
            playerNameText.text = displayedPlayer.playerName;
            playerLifeText.text = "Life: " +displayedPlayer.life;
            playerMoniesText.text = "Monies: " + displayedPlayer.money;
            playerCardsText.text = "Cards: " + displayedPlayer.cards.Length / 3;
            if (!displayedPlayer.zombie)
            {
                backGround.color = new Color(1f,1f,1f,0.39f);
            }
            else
            {
                backGround.color = new Color(0.1357245f, 0.5754717f, 0.3921569f, 0.39f);
            }
        }
    }
}
