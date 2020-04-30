using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;


public class CombatManager : NetworkBehaviour
{
    public CardDatabase cardDatabase;
    public int roundNum = 0;

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
            if (p.Equals(player)) p.GetComponent<Player>().RpcAddLog(logText);
        }
    }

    public void StartCombat()
    {
        AddLogForAllPlayers($"<b><color=red>ROUND " + roundNum + " COMBAT START!</color></b>");

        Dictionary<Player, List<Card>> playerBoards = InitializePlayerBoards();

        CastBoardSpells(playerBoards);

        CombatStart(playerBoards);

        DoShutDowns(playerBoards);
        DoContaminations(playerBoards);

        List<Player> winners = CombatWinLoss(playerBoards);
        CombatEnd(playerBoards, winners);

        ClearBoards(playerBoards);

        EndCombat();

    }

    public Dictionary<Player, List<Card>> InitializePlayerBoards()
    {
        Dictionary<Player, List<Card>> playersAndBoards = new Dictionary<Player, List<Card>>();
        // get players and cards and add to playersAndBoards
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            List<Card> boardCards = new List<Card>();
            List<string> board = p.GetComponent<Player>().boardAsList();
            foreach (string card in board)
            {
                // check that player can play card cost before instantiating
                if (p.GetComponent<Player>().money >= cardDatabase.getCardById(card).GetComponent<Card>().cost)
                {
                    GameObject currentCard = Instantiate(cardDatabase.getCardById(card));
                    AddLogForAllPlayers($"<b>" + p.GetComponent<Player>().playerName + "</b> played " + currentCard.GetComponent<Card>().cardName + "<color=orange> " + currentCard.GetComponent<Card>().cardId + " </color>");
                    currentCard.GetComponent<Card>().ownerPlayer = p;
                    boardCards.Add(currentCard.GetComponent<Card>());
                    p.GetComponent<Player>().money -= cardDatabase.getCardById(card).GetComponent<Card>().cost;
                    p.GetComponent<Player>().RpcChangeMoney(p.GetComponent<Player>().money);
                }
                else
                {
                    AddLogForPlayer("Can't afford " + cardDatabase.getCardById(card).GetComponent<Card>().cardName + "<color=orange> "
                                + cardDatabase.getCardById(card).GetComponent<Card>().cardId + " </color>" + ", it has been discarded", p);
                }
            }
            playersAndBoards.Add(p.GetComponent<Player>(), boardCards);
        }
        return playersAndBoards;
    }

    public void CombatStart(Dictionary<Player, List<Card>> playerBoards)
    {
        foreach (KeyValuePair<Player, List<Card>> playerBoard in playerBoards)
        {
            foreach (Card card in playerBoard.Value)
            {
                if (card.GetComponent<CardEffect>() != null)
                {
                    CardEffect effect = card.GetComponent<CardEffect>();
                    if (effect.condition.Equals(CardEffect.EffectCondition.ON_START))
                    {
                        if (!effect.effectPlayer.Equals(CardEffect.EffectOnPlayer.NONE))
                        {
                            List<Player> targets = GetPlayerTargets(playerBoards,playerBoard,effect.playerTarget);
                            DoDudeEffectsOnPlayers(targets, playerBoards, playerBoard, effect.effectPlayer, card, effect.amount);
                        }
                        if (!effect.effectDude.Equals(CardEffect.EffectOnDude.NONE))
                        {
                            List<Card> targets = GetDudeTargets(playerBoards,playerBoard,effect.dudeTarget,card);
                            DoDudeEffectsOnDudes(targets, playerBoards, playerBoard, effect.effectDude, card,effect.amount);
                        }
                    }
                    if (effect.condition.Equals(CardEffect.EffectCondition.IF_ONLY_UNIT_ON_START) && playerBoard.Value.Count==1)
                    {
                        if (!effect.effectPlayer.Equals(CardEffect.EffectOnPlayer.NONE))
                        {
                            List<Player> targets = GetPlayerTargets(playerBoards, playerBoard, effect.playerTarget);
                            DoDudeEffectsOnPlayers(targets, playerBoards, playerBoard, effect.effectPlayer, card, effect.amount);
                        }
                        if (!effect.effectDude.Equals(CardEffect.EffectOnDude.NONE))
                        {
                            List<Card> targets = GetDudeTargets(playerBoards, playerBoard, effect.dudeTarget,card);
                            DoDudeEffectsOnDudes(targets, playerBoards, playerBoard, effect.effectDude, card, effect.amount);
                        }
                    }
                }
            }
        }
    }

    public void DoShutDowns(Dictionary<Player, List<Card>> playerBoards)
    {
        foreach (KeyValuePair<Player, List<Card>> playerBoard in playerBoards)
        {
            //foreach (Card card in playerBoard.Value)
            for(int i=0; i<playerBoard.Value.Count; i++)
            {
                Card currentCard = playerBoard.Value[i];
                if (currentCard.shutdown)
                {
                    playerBoard.Value.Remove(currentCard);
                    Destroy(currentCard);
                }
            }
        }
    }

    public void DoContaminations(Dictionary<Player, List<Card>> playerBoards)
    {
        foreach (KeyValuePair<Player, List<Card>> playerBoard in playerBoards)
        {
            //foreach (Card card in playerBoard.Value)
            for (int i = 0; i < playerBoard.Value.Count; i++)
            {
                Card currentCard = playerBoard.Value[i];
                if (currentCard.contaminated)
                {
                    playerBoard.Key.DrawSpecificCard(currentCard.cardId);
                    playerBoard.Value.Remove(currentCard);
                    Destroy(currentCard);
                }
            }
        }
    }

    //calculate strenghts and deal damages to players, returns winners
    public List<Player> CombatWinLoss(Dictionary<Player, List<Card>> playerBoards)
    {
        //order players by the strength of their boards
        List<Player> winners = new List<Player>();
        List<Player> basicLosers = new List<Player>();
        List<Player> worstLosers = new List<Player>();

        //calculate highest and lowest power
        int highestPower = 0;
        int lowestPower = 100000;
        foreach (KeyValuePair<Player, List<Card>> playerBoard in playerBoards)
        {
            int powerSum = 0;
            foreach (Card c in playerBoard.Value)
            {
                powerSum += c.power;
            }

            if (powerSum > highestPower) highestPower = powerSum;
            if (powerSum < lowestPower) lowestPower = powerSum;
        }

        //add players to lists according to power
        foreach (KeyValuePair<Player, List<Card>> playerBoard in playerBoards)
        {
            int powerSum = 0;
            foreach (Card c in playerBoard.Value)
            {
                powerSum += c.power;
            }
            AddLogForAllPlayers($"<b>" + playerBoard.Key.playerName + "</b>'s board power is " + powerSum);
            if (powerSum.Equals(lowestPower)) worstLosers.Add(playerBoard.Key);
            else if (powerSum.Equals(highestPower)) winners.Add(playerBoard.Key);
            else basicLosers.Add(playerBoard.Key);
        }

        //Damage them accordingly
        foreach (Player p in winners)
        {
            p.life += 1;
            p.RpcChangeLife(p.life);
            AddLogForAllPlayers($"<b>" + p.playerName + "</b> won the round");
            AddLogForPlayer("You gained 1 Life", p.gameObject);
        }
        foreach (Player p in basicLosers)
        {
            p.life -= 1;
            p.RpcChangeLife(p.life);
            AddLogForAllPlayers($"<b>" + p.playerName + "</b> lost the round");
            AddLogForPlayer("You lost 1 Life", p.gameObject);
        }
        foreach (Player p in worstLosers)
        {
            p.life -= roundNum;
            p.RpcChangeLife(p.life);
            AddLogForAllPlayers($"<b>" + p.playerName + "</b> was one of the worst losers");
            AddLogForPlayer("You lost " + roundNum + " Life", p.gameObject);
        }
        return winners;
    }

    public void CombatEnd(Dictionary<Player, List<Card>> playerBoards, List<Player> winners)
    {
        foreach (KeyValuePair<Player, List<Card>> playerBoard in playerBoards)
        {
            foreach (Card card in playerBoard.Value)
            {
                if (card.GetComponent<CardEffect>() != null)
                {
                    CardEffect effect = card.GetComponent<CardEffect>();
                    if (effect.condition.Equals(CardEffect.EffectCondition.ON_END))
                    {
                        //do card end effects
                        if (!effect.effectPlayer.Equals(CardEffect.EffectOnPlayer.NONE))
                        {
                            List<Player> targets = GetPlayerTargets(playerBoards,playerBoard,effect.playerTarget, winners);
                            DoDudeEffectsOnPlayers(targets, playerBoards, playerBoard, effect.effectPlayer, card, effect.amount);
                        }
                    }
                    if (effect.condition.Equals(CardEffect.EffectCondition.ON_VICTORY) && winners.Contains(playerBoard.Key))
                    {
                        //do card end effects
                        if (!effect.effectPlayer.Equals(CardEffect.EffectOnPlayer.NONE))
                        {
                            List<Player> targets = GetPlayerTargets(playerBoards, playerBoard, effect.playerTarget, winners);
                            DoDudeEffectsOnPlayers(targets, playerBoards, playerBoard, effect.effectPlayer, card, effect.amount);
                        }
                    }
                    if (effect.condition.Equals(CardEffect.EffectCondition.ON_LOSS) && !winners.Contains(playerBoard.Key))
                    {
                        //do card end effects
                        if (!effect.effectPlayer.Equals(CardEffect.EffectOnPlayer.NONE))
                        {
                            List<Player> targets = GetPlayerTargets(playerBoards, playerBoard, effect.playerTarget, winners);
                            DoDudeEffectsOnPlayers(targets, playerBoards, playerBoard, effect.effectPlayer, card, effect.amount);
                        }
                    }
                }
            }
        }
    }
    
    public void ClearBoards(Dictionary<Player, List<Card>> playerBoards)
    {
        // player list
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        // clear each players board
        foreach (GameObject p in players)
        {
            List<Card> specificBoard = playerBoards[p.GetComponent<Player>()];
            foreach (Card c in specificBoard)
            {
                Destroy(c.gameObject);
            }
        }
        foreach (GameObject p in players)
        {
            //clear board
            p.GetComponent<Player>().board = "";
            p.GetComponent<Player>().RpcChangeBoard("");
        }
    }

    public void EndCombat()
    {
        AddLogForAllPlayers($"<b><color=red>ROUND " + roundNum + " COMBAT END!</color></b>");
    }

    public void CastBoardSpells(Dictionary<Player, List<Card>> playerBoards)
    {
        foreach (KeyValuePair<Player, List<Card>> playerBoard in playerBoards)
        {
            for (int i=0;i<playerBoard.Value.Count;i++)
            {
                if (playerBoard.Value[i].type.Equals(CardType.BOARDLOITSU))
                {
                    DoSpell(playerBoards,playerBoard, playerBoard.Value[i]);
                }
            }
        }
        foreach (KeyValuePair<Player, List<Card>> playerBoard in playerBoards)
        {
            playerBoard.Value.RemoveAll(isSpell);
        }
    }

    public void DoSpell(Dictionary<Player, List<Card>> playerBoards, KeyValuePair<Player, List<Card>> playerBoard,Card spell)
    {
        if (spell.boardSpellEffect.Equals(BoardSpellEffect.GIN_LEMON))
        {
            AddLogForAllPlayers($"<color=yellow>GIN LEMON HAS BEEN ACTIVATED!</color>");
            List<Card> targets = GetDudeTargets(playerBoards, playerBoard, CardEffect.DudeTarget.RANDOM_DUDE_ON_ALL_BOARDS, spell);
            DoDudeEffectsOnDudes(targets,playerBoards,playerBoard,CardEffect.EffectOnDude.SHUTDOWN,spell,1);
        }
        else if (spell.boardSpellEffect.Equals(BoardSpellEffect.KAIKKI_LIITTYY_KAIKKEEN))
        {
            AddLogForAllPlayers($"<color=yellow>KAIKKI LIITTYY KAIKKEEN HAS BEEN ACTIVATED!</color>");
            List<Player> shuffledPlayers = new List<Player>();
            foreach (KeyValuePair<Player, List<Card>> playerBoardX in playerBoards)
            {
                shuffledPlayers.Add(playerBoardX.Key);
            }
            shuffledPlayers = Shuffle(shuffledPlayers);
            List<string> shuffledCards = new List<string>();
            foreach (Player shuffledPlayer in shuffledPlayers)
            {
                shuffledCards.Add(shuffledPlayer.cards);
            }
            for (int i = 0; i < playerBoards.Count; i++)
            {
                AddLogForPlayer($"You got the hand of <b>" + shuffledPlayers[i].playerName + "</b>", playerBoards.ElementAt(i).Key.gameObject);
                playerBoards.ElementAt(i).Key.cards = shuffledCards[i];
                playerBoards.ElementAt(i).Key.RpcChangeCards(playerBoards.ElementAt(i).Key.cards);
            }
        }
        else if (spell.boardSpellEffect.Equals(BoardSpellEffect.MUSTALAISLEIRI))
        {
            AddLogForAllPlayers($"<color=yellow>MUSTALAISLEIRI HAS BEEN ACTIVATED!</color>");
            List<Player> owner = GetPlayerTargets(playerBoards, playerBoard, CardEffect.PlayerTarget.OWNER);
            DoDudeEffectsOnPlayers(owner, playerBoards, playerBoard, CardEffect.EffectOnPlayer.DRAW_CARDS, spell, 1);

            List<Player> targets = GetPlayerTargets(playerBoards, playerBoard, CardEffect.PlayerTarget.ALL_PLAYERS);
            foreach(Player target in targets)
            {
                List<Player> targetAsList = new List<Player>();
                targetAsList.Add(target);
                int rng = UnityEngine.Random.Range(0, 3);
                if(rng==0)DoDudeEffectsOnPlayers(targetAsList, playerBoards, playerBoard, CardEffect.EffectOnPlayer.DRAW_CARDS, spell, 1);
                else if (rng == 1) DoDudeEffectsOnPlayers(targetAsList, playerBoards, playerBoard, CardEffect.EffectOnPlayer.SET_MONEY, spell, 6);
                else if (rng == 2) DoDudeEffectsOnPlayers(targetAsList, playerBoards, playerBoard, CardEffect.EffectOnPlayer.HEAL, spell, 1);
            }
        }
        else if (spell.boardSpellEffect.Equals(BoardSpellEffect.AINA_FLASYIS))
        {
            AddLogForAllPlayers($"<color=yellow>JÄBÄT ON AINA FLÄSYIS!</color>");
            List<Card> targets = GetDudeTargets(playerBoards, playerBoard, CardEffect.DudeTarget.STRONGEST_FOE_ON_ALL_BOARDS, spell);
            DoDudeEffectsOnDudes(targets, playerBoards, playerBoard, CardEffect.EffectOnDude.SET_POWER, spell, 1);
        }
        else if (spell.boardSpellEffect.Equals(BoardSpellEffect.WATER_GLASS))
        {
            List<Card> targets = GetDudeTargets(playerBoards, playerBoard, CardEffect.DudeTarget.ALL_ALLIES, spell);
            DoDudeEffectsOnDudes(targets, playerBoards, playerBoard, CardEffect.EffectOnDude.GIVE_ABSOLUTIST, spell, 1);
            List<Player> targetsX = GetPlayerTargets(playerBoards, playerBoard, CardEffect.PlayerTarget.OWNER);
            DoDudeEffectsOnPlayers(targetsX, playerBoards, playerBoard, CardEffect.EffectOnPlayer.SET_MONEY, spell, 0);
        }
        else if (spell.boardSpellEffect.Equals(BoardSpellEffect.KONTAMINAATIO))
        {
            AddLogForAllPlayers($"<color=yellow>THE KANAPYÖRYKÄT HAVE KONTAMINATED THE BOARDS!</color>");
            List<Card> targets = GetDudeTargets(playerBoards, playerBoard, CardEffect.DudeTarget.RANDOM_FOE_ON_ALL_BOARDS, spell);
            DoDudeEffectsOnDudes(targets, playerBoards, playerBoard, CardEffect.EffectOnDude.CONTAMINATE, spell, 1);
        }
    }

    public List<Player> GetPlayerTargets(Dictionary<Player, List<Card>> playerBoards, KeyValuePair<Player, List<Card>> playerBoard, CardEffect.PlayerTarget target, List<Player> winners = null)
    {
        List<Player> targets = new List<Player>();
        if (target.Equals(CardEffect.PlayerTarget.OWNER))
        {
            targets.Add(playerBoard.Key);
        }
        else if (target.Equals(CardEffect.PlayerTarget.RANDOM_ENEMY))
        {
            List<Player> potentialTargets = new List<Player>();
            foreach (KeyValuePair<Player, List<Card>> playerBoardX in playerBoards)
            {
                if (!playerBoardX.Key.Equals(playerBoard.Key)) potentialTargets.Add(playerBoardX.Key);
            }
            if(potentialTargets.Count>0) targets.Add(potentialTargets[UnityEngine.Random.Range(0, potentialTargets.Count)]);
        }
        else if (target.Equals(CardEffect.PlayerTarget.ALL_PLAYERS))
        {
            foreach (KeyValuePair<Player, List<Card>> playerBoardX in playerBoards)
            {
                targets.Add(playerBoardX.Key);
            }
        }
        else if (target.Equals(CardEffect.PlayerTarget.ALL_ENEMIES))
        {
            foreach (KeyValuePair<Player, List<Card>> playerBoardX in playerBoards)
            {
                if (!playerBoardX.Key.Equals(playerBoard.Key)) targets.Add(playerBoardX.Key);
            }
        }
        else if (target.Equals(CardEffect.PlayerTarget.RANDOM_PLAYER))
        {
            targets.Add(playerBoards.Keys.ToArray()[UnityEngine.Random.Range(0, playerBoards.Keys.ToArray().Length)]);
        }
        else if(winners != null && target.Equals(CardEffect.PlayerTarget.ALL_WINNERS))
        {
            foreach(Player winner in winners)
            {
                targets.Add(winner);
            }
        }
        else if (winners != null && target.Equals(CardEffect.PlayerTarget.ALL_LOSERS))
        {
            foreach (KeyValuePair<Player, List<Card>> playerBoardX in playerBoards)
            {
                if(!winners.Contains(playerBoardX.Key))targets.Add(playerBoardX.Key);
            }
        }
        return targets;
    }

    public void DoDudeEffectsOnPlayers(List<Player> targets, Dictionary<Player, List<Card>> playerBoards, KeyValuePair<Player, List<Card>> playerBoard, CardEffect.EffectOnPlayer playerEffect, Card effectCard, int effectAmount)
    {
        foreach (Player target in targets)
        {
            // Do effect on player and announce it
            if (playerEffect.Equals(CardEffect.EffectOnPlayer.DEAL_DAMAGE))
            {
                target.life -= effectAmount;
                target.RpcChangeLife(target.life);
                AddLogForPlayer($"<b>" + playerBoard.Key.playerName + "</b>'s " + effectCard.cardName + "<color=orange> " + effectCard.cardId + " </color> did " + effectAmount + " damage to you.", target.gameObject);
            }
            else if (playerEffect.Equals(CardEffect.EffectOnPlayer.HEAL))
            {
                target.life += effectAmount;
                target.RpcChangeLife(target.life);
                AddLogForPlayer($"<b>" + playerBoard.Key.playerName + "</b>'s " + effectCard.cardName + "<color=orange> " + effectCard.cardId + " </color> healed you for " + effectAmount + ".", target.gameObject);
            }
            else if (playerEffect.Equals(CardEffect.EffectOnPlayer.DRAW_CARDS))
            {
                for (int i = 0; i < effectAmount; i++)
                {
                    target.DrawCard();
                }
                target.RpcChangeCards(target.cards);
                AddLogForPlayer($"<b>" + playerBoard.Key.playerName + "</b>'s " + effectCard.cardName + "<color=orange> " + effectCard.cardId + " </color> drew you " + effectAmount + " cards.", target.gameObject);
            }
            else if (playerEffect.Equals(CardEffect.EffectOnPlayer.GAIN_MONEY))
            {
                target.money += effectAmount;
                target.RpcChangeMoney(target.money);
                AddLogForPlayer($"<b>" + playerBoard.Key.playerName + "</b>'s " + effectCard.cardName + "<color=orange> " + effectCard.cardId + " </color> gained you " + effectAmount + " monies.", target.gameObject);
            }
            else if (playerEffect.Equals(CardEffect.EffectOnPlayer.SET_MONEY))
            {
                target.money = effectAmount;
                target.RpcChangeMoney(target.money);
                AddLogForPlayer($"<b>" + playerBoard.Key.playerName + "</b>'s " + effectCard.cardName + "<color=orange> " + effectCard.cardId + " </color> set your moeny to " + effectAmount + ".", target.gameObject);
            }
            else if (playerEffect.Equals(CardEffect.EffectOnPlayer.MUGGER))
            {
                List<string> targetHand = target.cardsAsList();
                if (targetHand.Count > 0)
                {
                    // remove target card
                    string sourceString = target.GetComponent<Player>().cards;
                    string cardString = targetHand[UnityEngine.Random.Range(0, targetHand.Count)];
                    int index = sourceString.IndexOf(cardString);
                    string cleanPath = (index < 0) ? sourceString : sourceString.Remove(index, cardString.Length);
                    target.GetComponent<Player>().cards = cleanPath;
                    target.GetComponent<Player>().RpcChangeCards(target.GetComponent<Player>().cards);

                    //add card to hand if space in hand
                    if (playerBoard.Key.GetComponent<Player>().cards.Length < playerBoard.Key.GetComponent<Player>().maxHandSize * 3)
                    {
                        playerBoard.Key.GetComponent<Player>().cards += playerBoard.Key.GetComponent<Player>().cards + cardString;
                        playerBoard.Key.GetComponent<Player>().RpcChangeCards(playerBoard.Key.GetComponent<Player>().cards);
                    }

                    AddLogForPlayer($"<b>" + playerBoard.Key.playerName + "</b>'s " + effectCard.cardName + "<color=orange> " + effectCard.cardId + " </color> muggered <b>" + target.playerName + "</b>", target.gameObject);
                    AddLogForPlayer($"<b>" + playerBoard.Key.playerName + "</b>'s " + effectCard.cardName + "<color=orange> " + effectCard.cardId + " </color> muggered <b>" + target.playerName + "</b>", playerBoard.Key.gameObject);
                }
                else
                {
                    AddLogForPlayer($"<b>" + playerBoard.Key.playerName + "</b>'s " + effectCard.cardName + "<color=orange> " + effectCard.cardId + " </color> tried to mugger <b>" + target.playerName + "</b>, but they had no cards.", target.gameObject);
                    AddLogForPlayer($"<b>" + playerBoard.Key.playerName + "</b>'s " + effectCard.cardName + "<color=orange> " + effectCard.cardId + " </color> tried to mugger <b>" + target.playerName + "</b>, but they had no cards.", playerBoard.Key.gameObject);
                }
            }
        }
    }

    public List<Card> GetDudeTargets(Dictionary<Player, List<Card>> playerBoards, KeyValuePair<Player, List<Card>> playerBoard, CardEffect.DudeTarget target, Card effectCard)
    {
        List<Card> targets = new List<Card>();
        if (target.Equals(CardEffect.DudeTarget.SELF))
        {
            targets.Add(effectCard);
        }
        else if (target.Equals(CardEffect.DudeTarget.RANDOM_ALLY))
        {
            List<Card> targetBoard = playerBoard.Value;
            targetBoard.RemoveAll(isSpell);
            targets.Add(targetBoard[UnityEngine.Random.Range(0, targetBoard.Count)]);
        }
        else if (target.Equals(CardEffect.DudeTarget.RANDOM_DUDE))
        {
            List<Card> targetBoard = playerBoards.Values.ToArray()[UnityEngine.Random.Range(0, playerBoards.Values.ToArray().Length)];
            targetBoard.RemoveAll(isSpell);
            targets.Add(targetBoard[UnityEngine.Random.Range(0, targetBoard.Count)]);
        }
        else if (target.Equals(CardEffect.DudeTarget.RANDOM_FOE))
        {
            List<List<Card>> targetBoards = playerBoards.Values.ToList();
            targetBoards.Remove(playerBoard.Value);
            if (targetBoards.Count > 0)
            {
                List<Card> targetBoard = targetBoards[UnityEngine.Random.Range(0, targetBoards.Count)];
                targetBoard.RemoveAll(isSpell);
                targets.Add(targetBoard[UnityEngine.Random.Range(0, targetBoard.Count)]);
            }
        }
        else if (target.Equals(CardEffect.DudeTarget.ALL_ALLIES))
        {
            foreach (Card c in playerBoard.Value)
            {
                if(!isSpell(c))targets.Add(c);
            }
        }
        else if (target.Equals(CardEffect.DudeTarget.ALL_FOES))
        {
            List<List<Card>> targetBoards = playerBoards.Values.ToList();
            targetBoards.Remove(playerBoard.Value);
            foreach (List<Card> targetBoard in targetBoards)
            {
                targetBoard.RemoveAll(isSpell);
                foreach (Card c in targetBoard)
                {
                    targets.Add(c);
                }
            }
        }
        else if (target.Equals(CardEffect.DudeTarget.RANDOM_FOE_ON_ALL_BOARDS))
        {
            List<List<Card>> targetBoards = playerBoards.Values.ToList();
            targetBoards.Remove(playerBoard.Value);
            foreach (List<Card> targetBoard in targetBoards)
            {
                targetBoard.RemoveAll(isSpell);
                if (targetBoard.Count > 0) targets.Add(targetBoard[UnityEngine.Random.Range(0, targetBoard.Count)]);
            }
        }
        else if (target.Equals(CardEffect.DudeTarget.RANDOM_DUDE_ON_ALL_BOARDS))
        {
            List<List<Card>> targetBoards = playerBoards.Values.ToList();
            foreach (List<Card> targetBoard in targetBoards)
            {
                targetBoard.RemoveAll(isSpell);
                if(targetBoard.Count>0)targets.Add(targetBoard[UnityEngine.Random.Range(0, targetBoard.Count)]);
            }
        }
        else if (target.Equals(CardEffect.DudeTarget.STRONGEST_FOE))
        {
            List<List<Card>> targetBoards = playerBoards.Values.ToList();
            targetBoards.Remove(playerBoard.Value);
            if (targetBoards.Count > 0)
            {
                int strongestPower = -1;
                Card strongest = null;
                foreach (List<Card> targetBoard in targetBoards)
                {
                    targetBoard.RemoveAll(isSpell);
                    foreach (Card c in targetBoard)
                    {
                        if (c.power >= strongestPower)
                        {
                            strongest = c;
                            strongestPower = c.power;
                        }
                    }
                }
                if(strongest != null)targets.Add(strongest);
            }
        }
        else if (target.Equals(CardEffect.DudeTarget.STRONGEST_FOE_ON_ALL_BOARDS))
        {
            List<List<Card>> targetBoards = playerBoards.Values.ToList();
            targetBoards.Remove(playerBoard.Value);
            if (targetBoards.Count > 0)
            {
                foreach (List<Card> targetBoard in targetBoards)
                {
                    targetBoard.RemoveAll(isSpell);
                    int strongestPower = -1;
                    Card strongest = null;
                    foreach (Card c in targetBoard)
                    {
                        if (c.power >= strongestPower)
                        {
                            strongest = c;
                            strongestPower = c.power;
                        }
                    }
                    if (strongest != null) targets.Add(strongest);
                }
            }
        }
        targets.RemoveAll(isSpell);
        return targets;
    }

    public void DoDudeEffectsOnDudes(List<Card> targets, Dictionary<Player, List<Card>> playerBoards, KeyValuePair<Player, List<Card>> playerBoard, CardEffect.EffectOnDude dudeEffect, Card effectCard, int effectAmount)
    {
        foreach (Card target in targets)
        {
            if (dudeEffect.Equals(CardEffect.EffectOnDude.BOOST))
            {
                target.power += effectAmount;
                if (target.ownerPlayer != null) AddLogForPlayer($"<b>" + playerBoard.Key.playerName + "</b>'s " + effectCard.cardName + "<color=orange> " + effectCard.cardId + " </color> boosted your "
                          + target.cardName + "<color=orange> " + target.cardId + " </color> by " + effectAmount, target.ownerPlayer);
            }
            else if (dudeEffect.Equals(CardEffect.EffectOnDude.FIFTY_FIFTY_BOOST))
            {
                int rng = UnityEngine.Random.Range(0,2);
                if (rng > 0)
                {
                    target.power += effectAmount;
                    if (target.ownerPlayer != null) AddLogForPlayer($"<b>" + playerBoard.Key.playerName + "</b>'s " + effectCard.cardName + "<color=orange> " + effectCard.cardId + " </color> boosted your "
                              + target.cardName + "<color=orange> " + target.cardId + " </color> by " + effectAmount, target.ownerPlayer);
                }
            }
            else if (dudeEffect.Equals(CardEffect.EffectOnDude.RANDOM_BOOST_ZERO_TO_AMOUNT))
            {
                int boostAmount = UnityEngine.Random.Range(0, effectAmount + 1);
                target.power += boostAmount;
                if (target.ownerPlayer != null) AddLogForPlayer($"<b>" + playerBoard.Key.playerName + "</b>'s " + effectCard.cardName + "<color=orange> " + effectCard.cardId + " </color> boosted your "
                              + target.cardName + "<color=orange> " + target.cardId + " </color> by " + boostAmount, target.ownerPlayer);
            }
            else if (dudeEffect.Equals(CardEffect.EffectOnDude.DOUBLE_POWER))
            {
                target.power = target.power*2;
                if (target.ownerPlayer != null) AddLogForPlayer($"<b>" + playerBoard.Key.playerName + "</b>'s " + effectCard.cardName + "<color=orange> " + effectCard.cardId + " </color> doubled the power of your "
                          + target.cardName + "<color=orange> " + target.cardId + " </color>", target.ownerPlayer);
            }
            else if (dudeEffect.Equals(CardEffect.EffectOnDude.SHUTDOWN))
            {
                if (target.hasAbsolutist)
                {
                    if (target.ownerPlayer != null) AddLogForPlayer($"<b>" + playerBoard.Key.playerName + "</b>'s " + effectCard.cardName + "<color=orange> " + effectCard.cardId + " </color> tried to shut down your "
                              + target.cardName + "<color=orange> " + target.cardId + " </color> ,but it was absolutist.", target.ownerPlayer);
                }
                else
                {
                    target.shutdown = true;
                    if (target.ownerPlayer != null) AddLogForPlayer($"<b>" + playerBoard.Key.playerName + "</b>'s " + effectCard.cardName + "<color=orange> " + effectCard.cardId + " </color> shut down your "
                              + target.cardName + "<color=orange> " + target.cardId + " </color>", target.ownerPlayer);
                }
            }
            else if (dudeEffect.Equals(CardEffect.EffectOnDude.CONTAMINATE))
            {
                if (target.hasImmune)
                {
                    if (target.ownerPlayer != null) AddLogForPlayer($"<b>" + playerBoard.Key.playerName + "</b>'s " + effectCard.cardName + "<color=orange> " + effectCard.cardId + " </color> tried to contaminate your "
                              + target.cardName + "<color=orange> " + target.cardId + " </color> ,but it was immune.", target.ownerPlayer);
                }
                else
                {
                    target.contaminated = true;
                    if (target.ownerPlayer != null) AddLogForPlayer($"<b>" + playerBoard.Key.playerName + "</b>'s " + effectCard.cardName + "<color=orange> " + effectCard.cardId + " </color> contaminated your "
                              + target.cardName + "<color=orange> " + target.cardId + " </color>", target.ownerPlayer);
                }
            }
            else if (dudeEffect.Equals(CardEffect.EffectOnDude.GIVE_ABSOLUTIST))
            {
                target.hasAbsolutist=true;
                target.shutdown = false;
                if (target.ownerPlayer != null) AddLogForPlayer($"<b>" + playerBoard.Key.playerName + "</b>'s " + effectCard.cardName + "<color=orange> " + effectCard.cardId + " </color> gave your "
                              + target.cardName + "<color=orange> " + target.cardId + " </color> absolutist", target.ownerPlayer);
            }
            else if (dudeEffect.Equals(CardEffect.EffectOnDude.SET_POWER))
            {
                target.power = effectAmount;
                if (target.ownerPlayer != null) AddLogForPlayer($"<b>" + playerBoard.Key.playerName + "</b>'s " + effectCard.cardName + "<color=orange> " + effectCard.cardId + " </color> set the power of your "
                          + target.cardName + "<color=orange> " + target.cardId + " </color> to " + effectAmount, target.ownerPlayer);
            }
        }
    }

    public static bool isSpell(Card c)
    {
        return !c.type.Equals(CardType.JABA);
    }

    public static List<Player> Shuffle(List<Player> ts)
    {
        var count = ts.Count;
        var last = count - 1;
        for (var i = 0; i < last; ++i)
        {
            var r = UnityEngine.Random.Range(i, count);
            var tmp = ts[i];
            ts[i] = ts[r];
            ts[r] = tmp;
        }
        return ts;
    }
}
