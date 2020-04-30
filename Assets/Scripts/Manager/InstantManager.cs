using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class InstantManager : NetworkBehaviour
{
    public CardDatabase cardDatabase;
    public void DoCardEffect(Card card, Player cardOwner)
    {
        if (card.instantEffect.Equals(InstantEffect.MENONETSINTA))
        {
            //draw 3 cards
            cardOwner.DrawCard();
            cardOwner.DrawCard();
            cardOwner.DrawCard();
            cardOwner.RpcChangeCards(cardOwner.cards);
            cardOwner.RpcAddLog("<color=yellow>In the search of meno, you have found three cards</color>");
        }
        if (card.instantEffect.Equals(InstantEffect.PYHA_ARTEFAKTI))
        {
            cardOwner.money = cardOwner.money*2;
            cardOwner.RpcChangeMoney(cardOwner.money);
            cardOwner.RpcAddLog("<color=yellow> THE HOLY ARTEFACT HAS DOUBLED YOUR MONIES</color>");
        }
        if (card.instantEffect.Equals(InstantEffect.DOUBLE_STEAKHOUSE))
        {
            //add 5 life
            cardOwner.life += 5;
            cardOwner.RpcChangeLife(cardOwner.life);
            cardOwner.RpcAddLog("<color=yellow> THE SUPERGOD OF ALL BORGERS HAS GIVEN YOU THE LYF </color>");
        }
        if (card.instantEffect.Equals(InstantEffect.ABSOLUT))
        {
            cardOwner.DrawSpecificCard("001");
            cardOwner.DrawSpecificCard("003");
            cardOwner.DrawSpecificCard("018");
            cardOwner.RpcChangeCards(cardOwner.cards);
            cardOwner.RpcAddLog("<color=yellow> THE ABSOLUT HAS DRAWN OUT THE DUDES </color>");
        }
        if (card.instantEffect.Equals(InstantEffect.COOL_GUYS_CLUB))
        {
            cardOwner.DrawSpecificCard("020");
            cardOwner.DrawSpecificCard("003");
            cardOwner.RpcChangeCards(cardOwner.cards);
            cardOwner.RpcAddLog("<color=yellow> COOL GUYS CLUB APPROVES </color>");
        }
    }
}
