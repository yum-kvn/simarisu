using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChipSelectParts : BaseUIParts
{
	[SerializeField]
	private List<ChipSelectFrameParts> chipSelectFrames;

	private ChipSelectFrameParts focusParts;
	private int indexOfFocusParts
	{
		get {return chipSelectFrames.IndexOf(focusParts);}
	}
	private bool isLast
	{
		get {return chipSelectFrames.Count - 1 == indexOfFocusParts;}
	}
	public bool isSetComplete
	{
		get {
			bool isAllSet = true;
			foreach (ChipSelectFrameParts parts in chipSelectFrames)
			{
				if (!parts.isSet)
				{
					isAllSet = false;
					break;
				}
			}
			return isAllSet;
		}
	}

	public List<int> GetSelectedChipIndexList()
	{
		List<int> indexList = new List<int>();

		foreach (ChipSelectFrameParts parts in chipSelectFrames)
		{
			indexList.Add(parts.GetChipIndex());
		}

		return indexList;
	}

	private void UpdateFocusParts(ChipSelectFrameParts parts)
	{
		if (focusParts != null)
		{
			focusParts.Unfocus();
		}

		focusParts = parts;
		focusParts.Focus();
	}

	public void ResetFocus()
	{
		UpdateFocusParts(chipSelectFrames[0]);
	}

	public void SetChipToFocusSelectParts(int chipIndex, BaseChip chip)
	{
		focusParts.SetChip(chipIndex, chip);
		ChangeFocusToNextParts();
	}

	private void ChangeFocusToNextParts()
	{
		if (isLast) {return;}
		UpdateFocusParts(chipSelectFrames[indexOfFocusParts + 1]);
	}

	public void ChipSelectClick(ChipSelectFrameParts parts)
	{
		UpdateFocusParts(parts);
	}
}
