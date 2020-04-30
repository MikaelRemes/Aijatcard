using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardEffect : MonoBehaviour
{
    public EffectCondition condition;
    public EffectOnPlayer effectPlayer;
    public EffectOnDude effectDude;
    public PlayerTarget playerTarget;
    public DudeTarget dudeTarget;
    public int amount = 0;

    public enum DudeTarget
    {
        NONE, SELF, RANDOM_ALLY, RANDOM_DUDE, ALL_ALLIES, RANDOM_FOE, ALL_FOES, RANDOM_FOE_ON_ALL_BOARDS, STRONGEST_FOE, STRONGEST_FOE_ON_ALL_BOARDS, RANDOM_DUDE_ON_ALL_BOARDS
        //, ALL_DUDES, STRONGEST_DUDE, WEAKEST_FOE_ON_ALL_BOARDS, WEAKEST_DUDE
    }

    public enum EffectOnDude
    {
        NONE, BOOST, SHUTDOWN, FIFTY_FIFTY_BOOST, RANDOM_BOOST_ZERO_TO_AMOUNT, GIVE_ABSOLUTIST, DOUBLE_POWER, SET_POWER, CONTAMINATE
        //,WEAKEN
    }

    public enum PlayerTarget
    {
        NONE, OWNER, RANDOM_ENEMY, ALL_PLAYERS, ALL_ENEMIES, RANDOM_PLAYER, ALL_WINNERS, ALL_LOSERS
        // RANDOM_LOSER
    }
    
    public enum EffectOnPlayer
    {
        NONE, DEAL_DAMAGE, HEAL, DRAW_CARDS, GAIN_MONEY, MUGGER, SET_MONEY
    }

    public enum EffectCondition
    {
        NONE, ON_START, ON_END, ON_VICTORY, ON_LOSS, IF_ONLY_UNIT_ON_START
        //,IF_STRONGEST_BOARD, IF_HAVE_STRONGEST_UNIT, IF_IS_STRONGEST_UNIT, IF_MOST_UNITS, IF_SHUTDOWN_ON_END
    }
}
