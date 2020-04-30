using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class CardDisplay : MonoBehaviour
{
    public Image cardImage;
    public Text cardIdText;
    public Text cardNameText;
    public Text cardDescriptionText;
    public Text cardFlavorText;
    public Card currentCard;
    public Button cardPlayButton;

    Player local;

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity);

            if (hit.collider != null)
            {
                if(hit.transform.gameObject.GetComponent<Card>() != null) currentCard = hit.transform.gameObject.GetComponent<Card>();
                UpdateLocal();
                UpdateCardButton();
            }
        }
        if(currentCard != null)
        {
            cardImage.sprite = currentCard.image;
            cardIdText.text = "ID: " + currentCard.cardId;
            cardNameText.text = currentCard.cardName;
            cardDescriptionText.text = currentCard.cardDescription.text;
            cardFlavorText.text = currentCard.cardFlavorText.text;
        }
    }

    public void SetCurrentCard(Card card)
    {
        currentCard = card;
        UpdateLocal();
        UpdateCardButton();
    }

    void UpdateCardButton()
    {
        if (local != null && currentCard != null)
        {
            //check if local has card
            if (local.cards.Contains(currentCard.cardId))
            {
                cardPlayButton.gameObject.SetActive(true);
            }else cardPlayButton.gameObject.SetActive(false);
        }
        else cardPlayButton.gameObject.SetActive(false);
    }

    void UpdateLocal()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            if (p.GetComponent<NetworkIdentity>().isLocalPlayer) local = p.GetComponent<Player>();
        }
    }

    public void PlayACard()
    {
        if (local != null && currentCard != null)
        {
            //check if local has card
            if (local.cards.Contains(currentCard.cardId))
            {
                local.CmdChangeAction(currentCard.cardId);
                cardPlayButton.gameObject.SetActive(false);
            }
        }
    }
}
