using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class Player : NetworkBehaviour
{
    [SyncVar] public string playerName = "noname";
    [SyncVar] public int life;
    [SyncVar] public int money;
    [SyncVar] public bool ready;
    [SyncVar] public bool reset;
    [SyncVar] public bool zombie = false;
    [SyncVar] public string currentAction = "";
    [SyncVar] public string cards="";
    [SyncVar] public string board = "";
    public Log log;

    public int startlife=12;
    public int startmoney=3;
    public int maxHandSize = 7;
    public int maxBoardSize = 5;

    public CardDatabase cardDatabase;

    private void Start()
    {
        cardDatabase = GameObject.FindGameObjectWithTag("CardDataBase").GetComponent<CardDatabase>();
        log = GameObject.FindGameObjectWithTag("Log").GetComponent<Log>();
    }

    //displays hand when card is removed/added to hand
    void UpdateCardAndBoardDisplay()
    { 
        if(!isLocalPlayer)return;
        //clear hand
        foreach (GameObject c in GameObject.FindGameObjectsWithTag("Card"))
        {
            Destroy(c);
        }

        //instantiate cards
        List<string> cardIds = (from Match m in Regex.Matches(cards, @"\d{3}")select m.Value).ToList();
        int count = 0;
        foreach (string id in cardIds)
        {
            Instantiate(cardDatabase.getCardById(id), new Vector2(-5.2f + count * 2.05f, -4.3f), Quaternion.identity);
            count++;
        }

        //instantiate board
        List<string> boardIds = (from Match m in Regex.Matches(board, @"\d{3}") select m.Value).ToList();
        int countX = 0;
        foreach (string idX in boardIds)
        {
            Instantiate(cardDatabase.getCardById(idX), new Vector2(3f - countX * 2.05f, -0.5f), Quaternion.identity);
            countX++;
        }
    }
    
    public void ResetPlayer()
    {
        life = startlife;
        money = startmoney;
        ready = false;
        reset = false;
        zombie = false;
        currentAction = "";
        cards = "";
        board = "";
        UpdateCardAndBoardDisplay();
    }

    public void DrawCard()
    {
        if (cards.Length < maxHandSize * 3) cards = cards + cardDatabase.getRandomCard().GetComponent<Card>().cardId;
    }

    public void DrawSpecificCard(string cardId)
    {
        if (cards.Length < maxHandSize * 3) cards = cards + cardId;
    }

    #region cmd commands
    [Command]
    public void CmdChangeReady(bool newReady)
    {
        ready = newReady;
    }
    [Command]
    public void CmdChangeReset(bool newReset)
    {
        reset = newReset;
    }
    [Command]
    public void CmdChangeAction(string newAction)
    {
        currentAction = newAction;
    }
    [Command]
    public void CmdChangeName(string newName)
    {
        playerName = newName;
    }
    #endregion

    [ClientRpc]
    public void RpcChangeCards(string newCards)
    {
        cards = newCards;
        UpdateCardAndBoardDisplay();
    }

    [ClientRpc]
    public void RpcChangeBoard(string newBoard)
    {
        board = newBoard;
        UpdateCardAndBoardDisplay();
    }

    [ClientRpc]
    public void RpcChangeAction(string newAction)
    {
        currentAction = newAction;
    }

    [ClientRpc]
    public void RpcChangeReady(bool newReady)
    {
        ready = newReady;
    }

    [ClientRpc]
    public void RpcChangeReset(bool newReset)
    {
        reset = newReset;
    }

    [ClientRpc]
    public void RpcChangeZombie(bool newZombie)
    {
        zombie = newZombie;
    }

    [ClientRpc]
    public void RpcChangeLife(int newLife)
    {
        life = newLife;
    }

    [ClientRpc]
    public void RpcChangeMoney(int newMoney)
    {
        money = newMoney;
    }

    [ClientRpc]
    public void RpcReset()
    {
        ResetPlayer();
    }

    [ClientRpc]
    public void RpcAddLog(string text)
    {
        if (!isLocalPlayer) return;
        log.AddLogToBuffer(text);
    }

    public List<string> cardsAsList()
    {
        return (from Match m in Regex.Matches(cards, @"\d{3}") select m.Value).ToList(); 
    }

    public List<string> boardAsList()
    {
        return (from Match m in Regex.Matches(board, @"\d{3}") select m.Value).ToList();
    }
}
