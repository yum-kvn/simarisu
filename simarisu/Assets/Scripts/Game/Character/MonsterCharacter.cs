using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class MonsterCharacter : BaseCharacter
{
	private Monster monster;

	public void Init(Monster data, int order)
	{
		this.monster = data;
		base.Init(monster.hp, monster.damage, order);
	}

	public Card SelectCard()
	{
		List<Card> cards = monster.card;

		// return null; //For Debug
		return cards[Random.Range(0, cards.Count - 1)];
	}
}
