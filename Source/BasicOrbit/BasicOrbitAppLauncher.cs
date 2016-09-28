﻿using System;
using System.Collections;
using System.Collections.Generic;
using BasicOrbit.Unity.Unity;
using KSP.UI.Screens;
using UnityEngine;

namespace BasicOrbit
{
    public class BasicOrbitAppLauncher : MonoBehaviour
    {
		private ApplicationLauncherButton button;
		private IEnumerator buttonAdder;

		private static BasicOrbitAppLauncher instance;
		private static Texture2D icon;

		private static GameObject prefab;

		private bool sticky;

		private BasicOrbit_AppLauncher launcher;

		public static BasicOrbitAppLauncher Instance
		{
			get { return instance; }
		}

		private void Start()
		{
			if (prefab == null)
				prefab = BasicOrbitLoader.Prefabs.LoadAsset<GameObject>("basicorbit_applauncher");

			if (icon == null)
				icon = GameDatabase.Instance.GetTexture("BasicOrbit/Resources/AppIcon", false);

			instance = this;

			if (buttonAdder != null)
				StopCoroutine(buttonAdder);

			buttonAdder = AddButton();
			StartCoroutine(buttonAdder);

			GameEvents.OnGameSettingsApplied.Add(Reposition);
		}

		private void OnDestroy()
		{
			if (launcher != null)
				Destroy(launcher.gameObject);

			GameEvents.onGUIApplicationLauncherUnreadifying.Remove(RemoveButton);
			GameEvents.OnGameSettingsApplied.Remove(Reposition);
		}

		private IEnumerator AddButton()
		{
			while (!ApplicationLauncher.Ready)
				yield return null;

			while (ApplicationLauncher.Instance == null)
				yield return null;

			button = ApplicationLauncher.Instance.AddModApplication(OnTrue, OnFalse, OnHover, OnHoverOut, null, null, ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW, icon);

			ApplicationLauncher.Instance.EnableMutuallyExclusive(button);

			GameEvents.onGUIApplicationLauncherUnreadifying.Add(RemoveButton);

			buttonAdder = null;
		}

		private void RemoveButton(GameScenes scene)
		{
			if (button == null)
				return;

			ApplicationLauncher.Instance.RemoveModApplication(button);
			button = null;
		}

		private void Reposition()
		{
			if (launcher == null)
				return;

			launcher.transform.position = GetAnchor();
		}

		private void OnTrue()
		{
			sticky = true;

			Open();
		}

		private void OnFalse()
		{
			Close();
		}

		private void OnHover()
		{
			if (sticky)
				return;

			Open();
		}

		private void OnHoverOut()
		{
			if (sticky)
				return;

			Close();
		}

		public Vector3 GetAnchor()
		{
			if (button == null)
				return Vector3.zero;

			Vector3 anchor = button.GetAnchor();

			anchor.x -= 3;
			anchor.y += 41;

			return anchor;
		}

		private void Open()
		{
			if (launcher != null)
				return;

			if (prefab == null)
				return;

			GameObject obj = Instantiate(prefab, GetAnchor(), Quaternion.identity) as GameObject;

			if (obj == null)
				return;

			obj.transform.SetParent(MainCanvasUtil.MainCanvas.transform);

			BasicOrbitUtilities.processComponents(obj);

			launcher = obj.GetComponent<BasicOrbit_AppLauncher>();

			if (launcher == null)
				return;

			launcher.setOrbit(BasicOrbit.Instance);
		}

		private void Close()
		{
			sticky = false;

			if (launcher == null)
				return;

			launcher.Close();

			launcher = null;
		}
    }
}
