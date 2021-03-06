using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class GameManager : GameMonoBehaviour
{
	[SerializeField]
	private StageManager stageManager;
	[SerializeField]
	private CharacterManager characterManager;
	[SerializeField]
	private CardManager cardManager;

	private int point;
	private int turn;

	private GameStatus gameStatus = GameStatus.Standby;
	private enum GameStatus
	{
		Standby,
		Battle,
		Lose,
		Win,
	}

	public void InitGame()
	{
		ResetGameStatus();

		stageManager.Init(OnCellPointerEnter);
		characterManager.Init(ShowRanges, HideRanges);
		cardManager.Init(CardPartsPushDown, CardPartsPushUp);

		PrepareGame();
	}

#region Game
	private void PrepareGame()
	{
		stageManager.LoadStage();
		characterManager.PrepareGame();
	}

	public void StartGame()
	{
		characterManager.AddUserCharacter(stageManager.GetDefaultUserCharacterCell());
		characterManager.AddMonster(stageManager.PickMonster(), stageManager.GetAvailableCells(characterManager.GetCharacterCells()));

		PrepareForNextTurn();
	}

	private void PrepareForNextTurn()
	{
		turn++;
		stageManager.CreateRoute(characterManager.GetUserCharacterCell());
		stageManager.ResetAllCellColor();
	}

	private void StartBattle()
	{
		BeforeBattleStart();
		StartCoroutine(BattleCoroutine(AfterBattle));
	}

	private void BeforeBattleStart()
	{
		gameStatus = GameStatus.Battle;
		cardManager.EnableTouchEvent(false);
	}

	private void AfterBattle()
	{
		gameStatus = GameStatus.Standby;
		cardManager.EnableTouchEvent(true);
		cardManager.UpdateParts();

		if (IsGameFinish())
		{
			if (gameStatus == GameStatus.Win)
			{
				Win();
			}
			else
			{
				Lose();
			}
		}

		PrepareForNextTurn();
	}

	private IEnumerator BattleCoroutine(System.Action callback)
	{
		yield return StartCoroutine(UserCharacterBattleCoroutine());
		if (IsGameFinish())
		{
			callback();
			yield break;
		}
		yield return new WaitForSeconds(0.5f);

		yield return StartCoroutine(MonsterBattleCoroutine());

		callback();
	}

	private IEnumerator UserCharacterBattleCoroutine()
	{
		List<Card> selectedCards = cardManager.GetSelectedCards();

		// Action Before Move
		yield return StartCoroutine(characterManager.UserCharacterAction(selectedCards[0]));

		characterManager.HideUserCharacterHpLabel();

		List<StageCell> route = stageManager.GetRoute();
		int lastIndex = route.Count - 1;
		foreach (var cell in route.Select((x,i) => new {Value = x, Index = i}))
		{
			bool isMoveDone = false;

			// Move Character
			characterManager.MoveUserCharacter(
				cell.Value,
				()=>{isMoveDone = true;}
			);

			// Wait til move finish
			while (!isMoveDone)
			{
				yield return null;
			}

			// Action while moving
			yield return StartCoroutine(characterManager.UserCharacterAction(selectedCards[1], cell.Index == lastIndex));
		}

		characterManager.ShowUserCharacterHpLabel();

		// Action after move
		yield return StartCoroutine(characterManager.UserCharacterAction(selectedCards[2]));

		yield return null;
	}

	private IEnumerator MonsterBattleCoroutine()
	{
		yield return null;
	}

	private void Win()
	{
		point++;
		stageManager.NextStage();
		PrepareGame();
		StartGame();
	}

	private void Lose()
	{
		ResetGameStatus();
		PrepareGame();
		StartGame();
	}
#endregion

#region GameStatus
	private void ResetGameStatus()
	{
		gameStatus = GameStatus.Standby;

		point = 0;
		turn = 0;
		stageManager.ResetStatus();
	}

	private bool IsGameFinish()
	{
		if (characterManager.UserCharacterDead())
		{
			gameStatus = GameStatus.Lose;
		}
		else if (characterManager.MonsterAllDead())
		{
			gameStatus = GameStatus.Win;
		}

		return gameStatus == GameStatus.Win || gameStatus == GameStatus.Lose;
	}

	private bool isStandby
	{
		get {return gameStatus == GameStatus.Standby;}
	}

	private bool isBattle
	{
		get {return gameStatus == GameStatus.Battle;}
	}
#endregion

#region UIParts
	public void InitUI(CardListParts cardListParts, ButtonParts startBattleButtonParts)
	{
		startBattleButtonParts.buttonClick = StartBattleButtonClick;
		cardManager.SetUIParts(cardListParts, startBattleButtonParts);
	}

	private void HideRanges()
	{
		stageManager.ResetAllRangeCellColor();
	}

	private void ShowRanges(Vector2 pivot, Card card)
	{
		foreach (Vector2 range in card.ranges)
		{
			Vector2 position = range + pivot;
			StageCell cell = stageManager.GetCell(position);
			if (cell != null)
			{
				cell.SetRangeColor();
			}
		}
	}
#endregion

#region Event
	private void StartBattleButtonClick(ButtonParts button)
	{
		StartBattle();
	}

	private void OnCellPointerEnter(StageCell cell)
	{
		if (!isStandby) {return;}
		if (!characterManager.IsCellAvilable(cell)) {return;}

		if (stageManager.routeCount >= characterManager.GetUserCharacterMove())
		{
			stageManager.RemoveLastRouteIfCan(cell);
		}
		else
		{
			stageManager.AddRoute(cell);
		}
	}

	private void CardPartsPushDown(Card card)
	{
		if (card.isAttack || card.isCure)
		{
			ShowRanges(characterManager.GetUserCharacterCell().Position(), card);
		}
	}

	private void CardPartsPushUp()
	{
		HideRanges();
	}
#endregion
}
