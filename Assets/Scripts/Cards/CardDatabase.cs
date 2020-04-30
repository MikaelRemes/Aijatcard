using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardDatabase : MonoBehaviour
{
    public List<GameObject> cards;

    public GameObject getCardById(string id)
    {
        foreach (GameObject card in cards)
        {
            if (id.Equals(card.GetComponent<Card>().cardId))
            {
                return card;
            }
        }
        return null;
    }

    public GameObject getCardByName(string name)
    {
        foreach (GameObject card in cards)
        {
            if (name.Equals(card.GetComponent<Card>().cardName))
            {
                return card;
            }
        }
        return getCardById("001");
    }

    public GameObject getRandomCard()
    {
        return cards[Random.Range(0,cards.Count)];
    }
}
