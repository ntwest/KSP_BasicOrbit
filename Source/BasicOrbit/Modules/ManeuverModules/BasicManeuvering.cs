﻿#region License
/*
 * Basic Orbit
 * 
 * BasicManeuvering - Static class for calculating maneuver node information
 * 
 * Copyright (C) 2016 DMagic
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by 
 * the Free Software Foundation, either version 3 of the License, or 
 * (at your option) any later version. 
 * 
 * This program is distributed in the hope that it will be useful, 
 * but WITHOUT ANY WARRANTY; without even the implied warranty of 
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
 * GNU General Public License for more details. 
 * 
 * You should have received a copy of the GNU General Public License 
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. 
 * 
 * 
 */
#endregion

using System;
using System.Collections.Generic;
using UnityEngine;
using BasicOrbit.Modules.TargetModules;

namespace BasicOrbit.Modules.ManeuverModules
{
	public static class BasicManeuvering
	{
		private const string _bbModuleName = "BetterBurnTimeData";
		private const string _bbTypeName = "burnType";
		private const string _bbLengthName = "burnTime";
		private const string _bbTimeName = "timeUntil";

		private static bool _updated;
		private static bool _update;

		private static bool _vesselIntersect;
		private static bool _bodyIntersect;
		private static double _closestDist;
		private static double _closestTime;
		private static double _closestRelVel;

		private static double _maneuverRemaining;
		private static double _maneuverTotal;
		private static double _burnTime;
		private static double _burnLength;
		private static bool _bbTimeLoaded;

		private static VesselModule _bbVesselModule;
		private static Vessel _bbVesselReference;

		private static ManeuverNode _node;

		public static bool UpdateOn
		{
			get { return _update; }
			set { _update = value; }
		}

		public static bool Updated
		{
			get { return _updated; }
		}

		public static bool VesselIntersect
		{
			get { return _vesselIntersect; }
		}

		public static bool BodyIntersect
		{
			get { return _bodyIntersect; }
		}

		public static double ManeuverRemaining
		{
			get { return _maneuverRemaining; }
		}

		public static double ManeuverTotal
		{
			get { return _maneuverTotal; }
		}

		public static double BurnTime
		{
			get { return _burnTime; }
		}

		public static double BurnLength
		{
			get { return _burnLength; }
		}

		public static bool BBTimeLoaded
		{
			get { return _bbTimeLoaded; }
		}

		public static double ClosestDistance
		{
			get { return _closestDist; }
		}

		public static double ClosestTime
		{
			get { return _closestTime; }
		}

		public static double ClosestRelVelocity
		{
			get { return _closestRelVel; }
		}

		public static void Update()
		{
			_update = false;

			if (FlightGlobals.ActiveVessel == null
				|| FlightGlobals.ActiveVessel.patchedConicSolver == null)
			{
				_updated = false;
				return;
			}

			PatchedConicSolver solver = FlightGlobals.ActiveVessel.patchedConicSolver;

			_node = solver.maneuverNodes[0];

			if (_node != null)
			{
				_maneuverTotal = _node.DeltaV.magnitude;

				_maneuverRemaining = _node.GetBurnVector(_node.patch).magnitude;

				if (BasicOrbitReflection.BetterBurnTimeLoaded)
				{
					if (_bbVesselModule == null || _bbVesselReference != FlightGlobals.ActiveVessel)
					{
						for (int i = FlightGlobals.ActiveVessel.vesselModules.Count - 1; i >= 0; i--)
						{
							VesselModule vMod = FlightGlobals.ActiveVessel.vesselModules[i];

							if (vMod == null)
								continue;

							if (vMod.GetType().Name != _bbModuleName)
								continue;

							_bbVesselModule = vMod;
							_bbVesselReference = FlightGlobals.ActiveVessel;
							break;
						}
					}

					if (_bbVesselModule != null)
					{
						string type = _bbVesselModule.Fields[_bbTypeName].GetValue(_bbVesselModule).ToString();

						if (type == "Maneuver")
						{
							_burnLength = _bbVesselModule.Fields[_bbLengthName].GetValue<double>(_bbVesselModule);
							_burnTime = _bbVesselModule.Fields[_bbTimeName].GetValue<double>(_bbVesselModule);

							if (double.IsNaN(_burnLength) || double.IsNaN(_burnTime))
							{
								_bbTimeLoaded = false;
								_burnTime = _node.UT;
								_burnLength = 0;
							}
							else
							{
								double half = _burnLength / 2;
								_burnTime -= half;
								_bbTimeLoaded = true;
							}
						}
						else
						{
							_bbTimeLoaded = false;
							_burnTime = _node.UT;
						}
					}
					else
					{
						_bbTimeLoaded = false;
						_burnTime = _node.UT;
					}
				}
				else
					_burnTime = _node.UT;

				if (!BasicTargetting.IsVessel && !BasicTargetting.IsCelestial)
				{
					_vesselIntersect = false;
					_bodyIntersect = false;
				}
				else
				{
					Vessel.Situations sit = FlightGlobals.ActiveVessel.situation;

					if ((sit |= Vessel.Situations.LANDED | Vessel.Situations.SPLASHED | Vessel.Situations.PRELAUNCH) == 0)
					{
						_vesselIntersect = false;
						_bodyIntersect = false;
					}
					else
					{
						OrbitTargeter oTargeter = FlightGlobals.ActiveVessel.orbitTargeter;

						if (oTargeter == null || solver == null)
						{
							_vesselIntersect = false;
							_bodyIntersect = false;
						}
						else if (!MapView.MapIsEnabled)
						{
							if (BasicTargetting.IsVessel)
							{
								_bodyIntersect = false;

								Orbit _refPatch = BasicOrbitReflection.GetRefPatch(oTargeter);
								Orbit _tgtRefPatch = BasicOrbitReflection.GetTargetRefPatch(oTargeter);

								_vesselIntersect = GetClosestVessel(_refPatch, _tgtRefPatch);
							}
							else
							{
								_vesselIntersect = false;

								double Pe = GetLowestPeA(solver, BasicTargetting.TargetBody, _node.patch);

								if (Pe < double.MaxValue - 1000)
								{
									_closestDist = Pe;
									_bodyIntersect = true;
								}
								else
								{
									Orbit _refPatch = BasicOrbitReflection.GetRefPatch(oTargeter);
									Orbit _tgtRefPatch = BasicOrbitReflection.GetTargetRefPatch(oTargeter);

									_bodyIntersect = GetClosestCelestial(_refPatch, _tgtRefPatch);
								}
							}
						}
						else
						{
							if (BasicTargetting.Markers == null || BasicTargetting.Markers.Count <= 0)
								BasicTargetting.Markers = BasicOrbitReflection.GetOrbitMarkers(oTargeter);

							if (BasicTargetting.IsVessel)
							{
								_bodyIntersect = false;

								OrbitTargeter.ISectMarker _intersectOne = null;
								OrbitTargeter.ISectMarker _intersectTwo = null;

								for (int i = BasicTargetting.Markers.Count - 1; i >= 0; i--)
								{
									OrbitTargeter.Marker m = BasicTargetting.Markers[i];

									if (m == null)
										continue;

									if (!(m is OrbitTargeter.ISectMarker))
										continue;

									int num = ((OrbitTargeter.ISectMarker)m).num;

									if (num == 1)
										_intersectOne = m as OrbitTargeter.ISectMarker;
									else if (num == 2)
										_intersectTwo = m as OrbitTargeter.ISectMarker;
								}

								OrbitTargeter.ISectMarker _closestIntersect = null;

								if (_intersectOne != null && _intersectTwo != null)
									_closestIntersect = _intersectOne.separation > _intersectTwo.separation ? _intersectTwo : _intersectOne;
								else if (_intersectOne != null)
									_closestIntersect = _intersectOne;
								else if (_intersectTwo != null)
									_closestIntersect = _intersectTwo;
								else
									_closestIntersect = null;

								if (_closestIntersect == null)
									_vesselIntersect = false;
								else
								{
									_vesselIntersect = true;
									_closestDist = _closestIntersect.separation * 1000;
									_closestRelVel = _closestIntersect.relSpeed;
									_closestTime = _closestIntersect.UT;
								}
							}
							else
							{
								_vesselIntersect = false;

								double Pe = GetLowestPeA(solver, BasicTargetting.TargetBody, _node.patch);

								if (Pe < double.MaxValue - 1000)
								{
									_closestDist = Pe;
									_bodyIntersect = true;
								}
								else
								{
									OrbitTargeter.ClApprMarker _approach = null;

									for (int i = BasicTargetting.Markers.Count - 1; i >= 0; i--)
									{
										OrbitTargeter.Marker m = BasicTargetting.Markers[i];

										if (m == null)
											continue;

										if (!(m is OrbitTargeter.ClApprMarker))
											continue;

										_approach = m as OrbitTargeter.ClApprMarker;
									}

									if (_approach == null)
										_bodyIntersect = false;
									else
									{
										_bodyIntersect = true;
										_closestDist = _approach.separation * 1000;
										_closestTime = (_approach.dT * -1) + Planetarium.GetUniversalTime();
									}
								}
							}
						}
					}
				}

				_updated = true;
			}
		}

		private static double GetLowestPeA(PatchedConicSolver s, CelestialBody tgt, Orbit startPatch)
		{
			double Pe = double.MaxValue;

			int l = s.flightPlan.Count;

			for (int i = 0; i < l; i++)
			{
				Orbit o = s.flightPlan[i];

				if (o == null)
					continue;

				if (o.StartUT < startPatch.StartUT)
					continue;

				//if (!o.activePatch)
				//	continue;

				if (o.timeToPe > 0 && o.referenceBody == tgt && (o.StartUT + o.timeToPe < o.EndUT) && o.PeR > o.referenceBody.Radius && o.PeR < o.referenceBody.sphereOfInfluence)
				{
					if (Pe > o.PeA)
					{
						Pe = o.PeA;
						_closestTime = o.StartUT + o.timeToPe;
					}
				}
			}

			return Pe;
		}

		private static bool GetClosestCelestial(Orbit refP, Orbit tgtP)
		{
			if (refP == null || tgtP == null)
				return false;

			Vector3d refClosest = refP.getRelativePositionAtUT(refP.closestTgtApprUT);
			Vector3d tgtClosest = tgtP.getRelativePositionAtUT(refP.closestTgtApprUT);

			_closestDist = (refClosest - tgtClosest).magnitude;
			_closestTime = refP.closestTgtApprUT;

			return true;
		}

		private static bool GetClosestVessel(Orbit refP, Orbit tgtP)
		{
			if (refP == null || tgtP == null)
				return false;

			if (refP.referenceBody != tgtP.referenceBody)
				return false;

			if (!Orbit.PeApIntersects(refP, tgtP, 20000))
				return false;

			double d1 = 0;
			double d2 = 0;
			double dT1 = 0;
			double d4 = 0;
			double dT2 = 0;
			double d6 = 0;
			int i1 = 0;

			int intersects = Orbit.FindClosestPoints(refP, tgtP, ref d1, ref d2, ref dT1, ref d4, ref dT2, ref d6, 0.001, 10, ref i1);

			double UT1 = refP.StartUT + refP.GetDTforTrueAnomaly(dT1, 0);
			double UT2 = refP.StartUT + refP.GetDTforTrueAnomaly(dT2, 0);

			if (intersects > 1)
			{
				double dist1 = double.MaxValue;

				if (PatchedConics.TAIsWithinPatchBounds(UT1, refP))
				{
					Vector3d refClosest1 = refP.getRelativePositionAtUT(UT1);
					Vector3d tgtClosest1 = tgtP.getRelativePositionAtUT(UT1);

					dist1 = (refClosest1 - tgtClosest1).magnitude;
				}

				double dist2 = double.MaxValue;

				if (PatchedConics.TAIsWithinPatchBounds(UT2, refP))
				{
					Vector3d refClosest2 = refP.getRelativePositionAtUT(UT2);
					Vector3d tgtClosest2 = tgtP.getRelativePositionAtUT(UT2);

					dist2 = (refClosest2 - tgtClosest2).magnitude;
				}

				if (dist1 > double.MaxValue - 1000 && dist2 > double.MaxValue - 1000)
					return false;

				bool first = dist1 < dist2;

				if (first)
				{
					_closestDist = dist1;
					_closestTime = UT1;

					Vector3d refVel = refP.getOrbitalVelocityAtUT(UT1);
					Vector3d tgtVel = tgtP.getOrbitalVelocityAtUT(UT1);

					_closestRelVel = (refVel - tgtVel).magnitude;
				}
				else
				{
					_closestDist = dist2;
					_closestTime = UT2;

					Vector3d refVel = refP.getOrbitalVelocityAtUT(UT2);
					Vector3d tgtVel = tgtP.getOrbitalVelocityAtUT(UT2);

					_closestRelVel = (refVel - tgtVel).magnitude;
				}

				return true;
			}
			else
			{
				if (!PatchedConics.TAIsWithinPatchBounds(UT1, refP))
					UT1 = double.MaxValue;

				if (!PatchedConics.TAIsWithinPatchBounds(UT2, refP))
					UT2 = double.MaxValue;

				if (UT1 > double.MaxValue - 1000 && UT2 > double.MaxValue - 1000)
					return false;

				double useUT = UT1 < UT2 ? UT1 : UT2;

				Vector3d refClosest = refP.getRelativePositionAtUT(useUT);
				Vector3d tgtClosest = tgtP.getRelativePositionAtUT(useUT);

				Vector3d refVel = refP.getOrbitalVelocityAtUT(useUT);
				Vector3d tgtVel = tgtP.getOrbitalVelocityAtUT(useUT);

				_closestDist = (refClosest - tgtClosest).magnitude;
				_closestRelVel = (refVel - tgtVel).magnitude;
				_closestTime = useUT;

				return true;
			}
		}

	}
}
