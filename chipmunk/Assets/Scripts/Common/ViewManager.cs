using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ViewManager : GameMonoBehaviour
{
	protected MasterManager masterManager;

	public virtual void Init(MasterManager masterManager)
	{
		this.masterManager = masterManager;
	}

	public void Show()
	{
		BeforeShow();
		ShowView();
		OnShow();
		AfterShow();
	}

	private void ShowView()
	{
		gameObject.SetActive(true);
	}

	protected virtual void BeforeShow()
	{

	}

	protected virtual void OnShow()
	{

	}

	protected virtual void AfterShow()
	{

	}

	public void Hide()
	{
		BeforeHide();
		HideView();
		OnHide();
		AfterHide();
	}

	private void HideView()
	{
		gameObject.SetActive(false);
	}

	protected virtual void BeforeHide()
	{

	}

	protected virtual void OnHide()
	{

	}

	protected virtual void AfterHide()
	{

	}

	public void GameStartCoroutine(IEnumerator routine)
	{
		masterManager.StartCoroutine(routine);
	}
}
