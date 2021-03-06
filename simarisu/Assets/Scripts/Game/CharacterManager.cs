using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CharacterManager : GameMonoBehaviour
{
	[SerializeField]
	private GameObject characterPrefab;
	[SerializeField]
	private GameObject hpLabelPrefab;
	[SerializeField]
	private Transform baseHpLabelTransform;
	private Camera canvasCamera
	{
		get {return baseHpLabelTransform.GetComponent<Canvas>().worldCamera;}
	}

	private UserCharacter userCharacter;
	private List<MonsterCharacter> monsters = new List<MonsterCharacter>();
	private List<BaseCharacter> allCharacters
	{
		get
		{
			List<BaseCharacter> list = monsters.ConvertAll(c => (BaseCharacter)c);
			list.Add(userCharacter);
			return list;
		}
	}

	private int sortingOrder = 0;

	private System.Action<Vector2, Card> showRanges;
	private System.Action hideRanges;

	private const float MOVE_SPEED = 0.25f;
	private const float ATTACK_INTERVAL = 0.25f;

	public void Init(System.Action<Vector2, Card> showRanges, System.Action hideRanges)
	{
		this.showRanges = showRanges;
		this.hideRanges = hideRanges;

		PrepareGame();
	}

	public void PrepareGame()
	{
		DestroyAll();
		sortingOrder = 0;
	}

#region CharacterStatus
	public bool UserCharacterDead()
	{
		return userCharacter.isDead;
	}

	public int GetUserCharacterMove()
	{
		return userCharacter.GetMove();
	}

	public bool MonsterAllDead()
	{
		foreach (MonsterCharacter monster in monsters)
		{
			if (monster == null) {continue;}
			if (!monster.isDead) {return false;}
		}
		return true;
	}
#endregion

#region CharacterCell
	public List<StageCell> GetCharacterCells()
	{
		List<StageCell> cells = new List<StageCell>();
		foreach (BaseCharacter character in allCharacters)
		{
			cells.Add(character.GetCurrentCell());
		}

		return cells;
	}

	public bool IsCellAvilable(StageCell cell)
	{
		foreach (BaseCharacter character in allCharacters)
		{
			if (character.GetCurrentCell() == cell) {return false;}
		}
		return true;
	}

	public StageCell GetUserCharacterCell()
	{
		return userCharacter.GetCurrentCell();
	}
#endregion

#region CharacterAction
	public void MoveUserCharacter(StageCell cell, System.Action callback)
	{
		LeanTween.move(userCharacter.gameObject, cell.PositionInWorld(), MOVE_SPEED).setOnComplete(
			()=> {
				userCharacter.MoveTo(cell, GetCanvasPosition, false);
				callback();
			}
		);

		userCharacter.ResetMoveUp();
	}

	public IEnumerator UserCharacterAction(Card card, bool resetStatus = true)
	{
		yield return StartCoroutine(ActionCharacter(userCharacter, card, resetStatus));
	}

	public IEnumerator MonsterActions(bool resetStatus)
	{
		foreach (MonsterCharacter monster in monsters)
		{
			if (monster == null) {continue;}
			Card card = monster.SelectCard();
			yield return StartCoroutine(ActionCharacter(monster, card, resetStatus));
		}
	}

	public IEnumerator ActionCharacter(BaseCharacter character, Card card, bool resetStatus)
	{
		if (card == null) {yield break;}

		showRanges(character.GetCurrentCell().Position(), card);

		switch (card.type)
		{
			case Card.Type.Attack:
				Attack(character, card, resetStatus);
			break;
			case Card.Type.Cure:
				character.Cure(CalculateCure(character, card));
				if (card.hasSpecialEffect)
				{
					ActivateSpecialEffect(character, card);
				}
			break;
			case Card.Type.Support:
			break;
			case Card.Type.Other:
			break;
		}

		yield return new WaitForSeconds(ATTACK_INTERVAL);

		hideRanges();
	}

	private void Attack(BaseCharacter character, Card card, bool resetStatus)
	{
		character.GetDamage();
		bool isMonster = character is MonsterCharacter;

		Vector2 currentPosition = character.Position();
		List<BaseCharacter> characterList = allCharacters;
		foreach (Vector2 range in card.ranges)
		{
			foreach (BaseCharacter target in characterList)
			{
				if (target == null || target.isDead) {continue;}
				if ((isMonster && target is MonsterCharacter) || (!isMonster && target is UserCharacter)) {continue;}

				if (target.Position() == currentPosition + range)
				{
					target.Damage(CalculateDamage(character, card));
					if (card.hasSpecialEffect)
					{
						ActivateSpecialEffect(target, card);
					}
				}
			}
		}

		if (resetStatus)
		{
			character.ResetDamageUp();
		}
	}

	private void ActivateSpecialEffect(BaseCharacter character, Card card)
	{
		switch (card.specialEffect)
		{
			case Card.SpecialEffect.AttackUp:
				character.AddDamageUp(card.effectValue);
			break;
			case Card.SpecialEffect.MoveUp:
				character.AddMoveUp(card.effectValue);
			break;
		}
	}

	private int CalculateDamage(BaseCharacter character, Card card)
	{
		return character.GetDamage() + card.value;
	}

	private int CalculateCure(BaseCharacter character, Card card)
	{
		return character.GetCure() + card.value;
	}
#endregion

#region AddCharacter
	private T AddCharacter<T>(string spriteId)
		where T : BaseCharacter
	{
		GameObject characterGameObject = Instantiate(characterPrefab);
		characterGameObject.transform.SetParent(transform);

		T character = characterGameObject.AddComponent<T>();
		character.SetSprite(GetSprite(spriteId));

		GameObject hpLabelGameObject = Instantiate(hpLabelPrefab);
		hpLabelGameObject.transform.SetParent(baseHpLabelTransform);
		HpLabelParts hpLabel = hpLabelGameObject.GetComponent<HpLabelParts>();
		character.SetHpLabel(hpLabel);

		return character;
	}

	public void AddUserCharacter(StageCell cell)
	{
		userCharacter = AddUserCharacter(User.GetUser());
		userCharacter.MoveTo(cell, GetCanvasPosition);
	}

	private UserCharacter AddUserCharacter(User data)
	{
		UserCharacter character = AddCharacter<UserCharacter>(data.sprite);
		character.Init(data, sortingOrder);
		sortingOrder++;
		return character;
	}

	public void AddMonster(List<Monster> monsterList, List<StageCell> cells)
	{
		int count = monsterList.Count;
		System.Random random = new System.Random();
		List<StageCell> cellList = cells.OrderBy(x => random.Next()).Take(count).ToList();

		foreach (var monster in monsterList.Select((x,i) => new {Value = x, Index = i}))
		{
			MonsterCharacter mc = AddMonster(monster.Value);
			mc.MoveTo(cellList[monster.Index], GetCanvasPosition);
			monsters.Add(mc);
		}
	}

	private MonsterCharacter AddMonster(Monster data)
	{
		MonsterCharacter monster = AddCharacter<MonsterCharacter>(data.sprite);
		monster.Init(data, sortingOrder);
		monster.onDead = DestroyMonster;
		sortingOrder++;
		return monster;
	}

	private Sprite GetSprite(string spriteId)
	{
		string path = string.Format("Characters/Images/{0}", spriteId);
		return Resources.Load<Sprite>(path);
	}
#endregion

#region DestoryCharacter
	public void DestroyAll()
	{
		DestroyUser();
		DestroyAllMonster();
	}

	private void DestroyUser()
	{
		if (userCharacter == null) {return;}
		userCharacter.DestroyIfExist();
	}

	private void DestroyAllMonster()
	{
		if (monsters == null) {return;}
		foreach (MonsterCharacter monster in monsters)
		{
			if (monster == null) {continue;}
			monster.DestroyIfExist();
		}
		monsters = new List<MonsterCharacter>();
	}

	private void DestroyMonster(MonsterCharacter monster)
	{
		monsters.Remove(monster);
		monster.DestroyIfExist();
	}
#endregion

	public void ShowUserCharacterHpLabel()
	{
		userCharacter.ShowHpLabel();
	}

	public void HideUserCharacterHpLabel()
	{
		userCharacter.HideHpLabel();
	}

#region CanvasPosition
	private Vector2 GetCanvasPosition(Vector3 position)
	{
		Vector2 screenPosition = Camera.main.WorldToScreenPoint(position);
		Vector2 uiPosition = Vector2.zero;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(baseHpLabelTransform as RectTransform, screenPosition, canvasCamera, out uiPosition);
		return uiPosition;
	}
#endregion
}
