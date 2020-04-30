using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Card : MonoBehaviour
{
    [Header("IDENTIFICATION")]
    public string cardId = "001";

    [Header("UNIVERSAL ATTRIBUTES")]
    public CardType type = CardType.JABA;
    public int cost = 0;
    public Sprite image;
    public string cardName;

    [Header("DUDE ATTRIBUTES")]
    public int power = 0;
    public bool shutdown = false;
    public bool contaminated = false;
    public bool hasAbsolutist=false;
    public bool hasImmune = false;

    [Header("INSTANT ATTRIBUTES")]
    public InstantEffect instantEffect = InstantEffect.NONE;


    [Header("BOARD SPELL ATTRIBUTES")]
    public BoardSpellEffect boardSpellEffect = BoardSpellEffect.NONE;

    [Header("don't touch")]
    public Text cardDescription;
    public Text cardFlavorText;
    public GameObject ownerPlayer;
}
