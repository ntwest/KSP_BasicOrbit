﻿using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;

namespace BasicOrbit
{
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class BasicSettings : MonoBehaviour
	{
		[Persistent]
		public bool showApoapsis = true;
		[Persistent]
		public bool showApoapsisAlways = true;
		[Persistent]
		public bool showPeriapsis = true;
		[Persistent]
		public bool showPeriapsisAlways = true;
		[Persistent]
		public bool showInclination = true;
		[Persistent]
		public bool showInclinationAlways = true;
		[Persistent]
		public bool showEccentricity = true;
		[Persistent]
		public bool showEccentricityAlways = true;
		[Persistent]
		public bool showPeriod = true;
		[Persistent]
		public bool showPeriodAlways = true;
		[Persistent]
		public bool showRadar = true;
		[Persistent]
		public bool showRadarAlways = true;
		[Persistent]
		public bool showTerrain = true;
		[Persistent]
		public bool showTerrainAlways = true;
		[Persistent]
		public bool showLAN = false;
		[Persistent]
		public bool showLANAlways = true;
		[Persistent]
		public bool showClosestApproach = true;
		[Persistent]
		public bool showClosestApproachAlways = true;
		[Persistent]
		public bool showDistance = true;
		[Persistent]
		public bool showDistanceAlways = true;
		[Persistent]
		public bool showRelInclination = true;
		[Persistent]
		public bool showRelInclinationAlways = true;
		[Persistent]
		public bool showRelVelocity = true;
		[Persistent]
		public bool showRelVelocityAlways = true;
		[Persistent]
		public bool showOrbitPanel = true;
		[Persistent]
		public bool showTargetPanel = true;
		[Persistent]
		public float panelAlpha = 0.5f;
		[Persistent]
		public float UIScale = 1;
		[Persistent]
		public Vector2 orbitPosition = new Vector2(100, 20);
		[Persistent]
		public Vector2 targetPosition = new Vector2(200, 20);

		private const string fileName = "PluginData/Settings.cfg";
		private string fullPath;

		private static bool loaded;
		private static BasicSettings instance;

		public static BasicSettings Instance
		{
			get { return instance; }
		}

		private void Awake()
		{
			if (loaded)
				Destroy(gameObject);

			DontDestroyOnLoad(gameObject);

			loaded = true;

			instance = this;

			fullPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), fileName).Replace("\\", "/");

			if (Load())
				BasicOrbit.BasicLogging("Settings file loaded");
		}

		public bool Load()
		{
			bool b = false;

			try
			{
				if (File.Exists(fullPath))
				{
					ConfigNode node = ConfigNode.Load(fullPath);
					ConfigNode unwrapped = node.GetNode(GetType().Name);
					ConfigNode.LoadObjectFromConfig(this, unwrapped);
					b = true;
				}
				else
				{
					BasicOrbit.BasicLogging("Settings file could not be found [{0}]", fullPath);
					b = false;
				}
			}
			catch (Exception e)
			{
				BasicOrbit.BasicLogging("Error while loading settings file from [{0}]\n{1}", fullPath, e);
				b = false;
			}

			return b;
		}

		public bool Save()
		{
			bool b = false;

			try
			{
				ConfigNode node = AsConfigNode();
				ConfigNode wrapper = new ConfigNode(GetType().Name);
				wrapper.AddNode(node);
				wrapper.Save(fullPath);
				b = true;
			}
			catch (Exception e)
			{
				BasicOrbit.BasicLogging("Error while saving settings file from [{0}]\n{1}", fullPath, e);
				b = false;
			}

			return b;
		}

		private ConfigNode AsConfigNode()
		{
			try
			{
				ConfigNode node = new ConfigNode(GetType().Name);

				node = ConfigNode.CreateConfigFromObject(this, node);
				return node;
			}
			catch (Exception e)
			{
				BasicOrbit.BasicLogging("Failed to generate settings file node...\n{0}", e);
				return new ConfigNode(GetType().Name);
			}
		}
	}
}