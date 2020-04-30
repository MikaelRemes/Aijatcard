using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ServerRoundManager : NetworkBehaviour
{
    public float updateRate = 0.5f;
    private float updateTimer = 0f;
    public int roundNum = 0;
    public CardDatabase cardDatabase;
    public CombatManager combatManager;
    public InstantManager instantManager;

    private bool inCombat = false;

    private void Start()
    {
        cardDatabase = GameObject.FindGameObjectWithTag("CardDataBase").GetComponent<CardDatabase>();
    }

    private void ResetGame()
    {
        if (!isServer) return;
        inCombat = false;
        AddLogForAllPlayers("<size=40>GAME RESET!</size>");
        // player reset
        foreach (GameObject player in GameObject.FindGameObjectsWithTag("Player"))
        {
            player.GetComponent<Player>().ResetPlayer();
            player.GetComponent<Player>().RpcReset();
        }
        roundNum = 0;
    }

    public void StartGame()
    {
        if (!isServer) return;
        AddLogForAllPlayers("<size=40>GAME START!</size>");
        
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            //reset players
            p.GetComponent<Player>().ResetPlayer();
            p.GetComponent<Player>().RpcReset();
            //draw 3 cards
            p.GetComponent<Player>().DrawCard();
            p.GetComponent<Player>().DrawCard();
            p.GetComponent<Player>().DrawCard();
            p.GetComponent<Player>().RpcChangeCards(p.GetComponent<Player>().cards);
        }
        roundNum = 1;
    }

    public void AddLogForAllPlayers(string logText)
    {
        // player list
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            p.GetComponent<Player>().RpcAddLog(logText);
        }
    }

    public void AddLogForPlayer(string logText, GameObject player)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            if(p.Equals(player))p.GetComponent<Player>().RpcAddLog(logText);
        }
    }

    public void StartRound()
    {
        inCombat = true;
        combatManager.roundNum = roundNum;
        combatManager.StartCombat();
        EndRound();
    }

    public void EndRound()
    {
        inCombat = false;
        // player list
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        //non-zombies
        List<Player> nonZombies = new List<Player>();
        List<Player> allPlayers = new List<Player>();
        foreach (GameObject p in players)
        {
            if (p.GetComponent<Player>().life <= 0)
            {
                p.GetComponent<Player>().zombie = true;
                p.GetComponent<Player>().RpcChangeZombie(p.GetComponent<Player>().zombie);
            }
            allPlayers.Add(p.GetComponent<Player>());
            if (!p.GetComponent<Player>().zombie) nonZombies.Add(p.GetComponent<Player>());
            //clear board
            p.GetComponent<Player>().board = "";
            p.GetComponent<Player>().RpcChangeBoard("");
            //add money = round + 3
            p.GetComponent<Player>().money += 3 + roundNum;
            p.GetComponent<Player>().RpcChangeMoney(p.GetComponent<Player>().money);
            AddLogForPlayer("You gained " + (3 + roundNum) + " money", p);
            p.GetComponent<Player>().ready = false;
            p.GetComponent<Player>().RpcChangeReady(false);
        }
        if(nonZombies.Count <= 1)
        {
            allPlayers = allPlayers.OrderByDescending(x => x.life).ToList();
            Player winner = allPlayers[0];
            if (nonZombies.Count == 1)
            {
                winner=nonZombies[0];
                allPlayers.Remove(winner);
                allPlayers.Insert(0, winner);
            }
            AddLogForAllPlayers($"<color=green><size=60><b>THE GAME HAS ENDED </b></size></color>");
            for (int i=0;i<allPlayers.Count;i++)
            {
                if(i==0)AddLogForAllPlayers($"<color=green><size=60><b>" + allPlayers[0].playerName.ToUpper() + " HAS WON THE GAME!</b> </size></color>");
                else if(i==1)
                {
                    AddLogForAllPlayers($"<color=green>" + allPlayers[1].playerName + " came in second!</color>");
                }
                else if (i == 2)
                {
                    AddLogForAllPlayers($"<color=green>" + allPlayers[2].playerName + " came in third!</color>");
                }
            }
            AddLogForAllPlayers($"You are free to continue playing or reset the game.");
        }
        roundNum++;
    }

    private void Update()
    {
        if (!isServer) return;
        if (updateTimer > updateRate && !inCombat)
        {
            //sorted player list
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player").OrderBy(go => go.name).ToArray();

            //check actions
            foreach(GameObject p in players)
            {
                if (!p.GetComponent<Player>().currentAction.Equals("") || p.GetComponent<Player>().currentAction != null) {

                    // ACTION IS A CARD PLAY
                    List<string> playerHand = (from Match m in Regex.Matches(p.GetComponent<Player>().cards, @"\d{3}") select m.Value).ToList();
                    if (playerHand.Contains(p.GetComponent<Player>().currentAction))
                    {
                        Card playedCard = cardDatabase.getCardById(p.GetComponent<Player>().currentAction).GetComponent<Card>();

                        if (playedCard.type.Equals(CardType.JABA))
                        {
                            if(p.GetComponent<Player>().board.Length < p.GetComponent<Player>().maxBoardSize * 3)
                            {
                                p.GetComponent<Player>().board += playedCard.cardId;
                                p.GetComponent<Player>().RpcChangeBoard(p.GetComponent<Player>().board);

                                AddLogForPlayer($"You played " + cardDatabase.getCardById(p.GetComponent<Player>().currentAction).GetComponent<Card>().cardName + "<color=orange> "
                                + cardDatabase.getCardById(p.GetComponent<Player>().currentAction).GetComponent<Card>().cardId + " </color>", p);

                                //remove card
                                string sourceString = p.GetComponent<Player>().cards;
                                string removeString = p.GetComponent<Player>().currentAction;
                                int index = sourceString.IndexOf(removeString);
                                string cleanPath = (index < 0) ? sourceString : sourceString.Remove(index, removeString.Length);
                                p.GetComponent<Player>().cards = cleanPath;
                                p.GetComponent<Player>().RpcChangeCards(p.GetComponent<Player>().cards);
                            }
                            else
                            {
                                AddLogForPlayer("No room on your board", p);
                            }
                        }
                        if (playedCard.type.Equals(CardType.BOARDLOITSU))
                        {
                            if (p.GetComponent<Player>().board.Length < p.GetComponent<Player>().maxBoardSize * 3)
                            {
                                p.GetComponent<Player>().board += playedCard.cardId;
                                p.GetComponent<Player>().RpcChangeBoard(p.GetComponent<Player>().board);

                                AddLogForPlayer($"You played " + cardDatabase.getCardById(p.GetComponent<Player>().currentAction).GetComponent<Card>().cardName + "<color=yellow> "
                                + cardDatabase.getCardById(p.GetComponent<Player>().currentAction).GetComponent<Card>().cardId + " </color>", p);

                                //remove card
                                string sourceString = p.GetComponent<Player>().cards;
                                string removeString = p.GetComponent<Player>().currentAction;
                                int index = sourceString.IndexOf(removeString);
                                string cleanPath = (index < 0) ? sourceString : sourceString.Remove(index, removeString.Length);
                                p.GetComponent<Player>().cards = cleanPath;
                                p.GetComponent<Player>().RpcChangeCards(p.GetComponent<Player>().cards);
                            }
                            else
                            {
                                AddLogForPlayer("No room on your board", p);
                            }
                        }
                        if (playedCard.type.Equals(CardType.INSTANT))
                        {
                            if (p.GetComponent<Player>().money >= playedCard.cost)
                            {

                                AddLogForPlayer($"You played " + cardDatabase.getCardById(p.GetComponent<Player>().currentAction).GetComponent<Card>().cardName + "<color=orange> "
                                + cardDatabase.getCardById(p.GetComponent<Player>().currentAction).GetComponent<Card>().cardId + " </color>", p);

                                p.GetComponent<Player>().money -= playedCard.cost;
                                p.GetComponent<Player>().RpcChangeMoney(p.GetComponent<Player>().money);

                                instantManager.DoCardEffect(playedCard, p.GetComponent<Player>());

                                //remove card
                                string sourceString = p.GetComponent<Player>().cards;
                                string removeString = p.GetComponent<Player>().currentAction;
                                int index = sourceString.IndexOf(removeString);
                                string cleanPath = (index < 0) ? sourceString : sourceString.Remove(index, removeString.Length);
                                p.GetComponent<Player>().cards = cleanPath;
                                p.GetComponent<Player>().RpcChangeCards(p.GetComponent<Player>().cards);
                            }
                            else AddLogForPlayer($"You can't afford to play " + cardDatabase.getCardById(p.GetComponent<Player>().currentAction).GetComponent<Card>().cardName + "<color=orange> "
                                + cardDatabase.getCardById(p.GetComponent<Player>().currentAction).GetComponent<Card>().cardId + " </color>", p);

                        }
                    }

                    // ACTION IS CARD DRAW
                    if (p.GetComponent<Player>().currentAction.Equals("drw"))
                    {
                        //can afford and room in hand
                        if (p.GetComponent<Player>().money >= 3 && p.GetComponent<Player>().cards.Length < p.GetComponent<Player>().maxHandSize*3)
                        {
                            AddLogForPlayer("You drew a card for 3 monies", p);
                            //draw 1 card
                            p.GetComponent<Player>().DrawCard();
                            p.GetComponent<Player>().RpcChangeCards(p.GetComponent<Player>().cards);
                            //remove 3 money
                            p.GetComponent<Player>().money -= 3;
                            p.GetComponent<Player>().RpcChangeMoney(p.GetComponent<Player>().money);
                        }
                        else
                        {
                            AddLogForPlayer("You cannot draw cards (no money or hand space)", p);
                        }
                    }

                    p.GetComponent<Player>().currentAction = "";
                    p.GetComponent<Player>().RpcChangeAction("");
                }
            }

            //check ready
            bool allReady = true;
            foreach (GameObject p in players)
            {
                if (!p.GetComponent<Player>().ready) allReady = false;
            }
            if (allReady)
            {
                if (roundNum <= 0)
                {
                    StartGame();
                }else StartRound();
            }

            //check reset
            bool allReset = true;
            foreach (GameObject p in players)
            {
                if (!p.GetComponent<Player>().reset) allReset = false;
            }
            if (allReset)
            {
                ResetGame();
            }
            updateTimer = 0f;
        }
        else
        {
            updateTimer += Time.deltaTime;
        }
    }
}