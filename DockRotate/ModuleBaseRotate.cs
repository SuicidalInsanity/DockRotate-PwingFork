using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using KSP.Localization;
using CompoundParts;
namespace DockRotate
{
	public abstract class ModuleBaseRotate : PartModule,
		IJointLockState, IResourceConsumer
	{
		protected const string GROUPNAME = "DockRotate";
		protected const string GROUPLABEL = "#DCKROT_rotation";
		protected const string DEBUGGROUP = "DockRotateDebug";

		protected const bool DEBUGMODE = false;


		[KSPField(isPersistant = true)]
		public int Revision = -1;

		private static int _revision = -1;
		public int getRevision()
		{
			if (_revision < 0) {
				_revision = 0;
				try {
					_revision = Assembly.GetExecutingAssembly().GetName().Version.Revision;
				} catch (Exception e) {
					string sep = new string('-', 80);
					log(sep);
					log("Exception reading revision:\n" + e.StackTrace);
					log(sep);
				}
			}
			return _revision;
		}

		private void checkRevision()
		{
			int r = getRevision();
			if (Revision != r) {
				log(desc(), ": REVISION " + Revision + " -> " + r);
				Revision = r;
			}
		}

		[KSPField(
			guiName = "#DCKROT_angle",
			groupName = GROUPNAME,
			groupDisplayName = GROUPLABEL,
			groupStartCollapsed = false,
			guiActive = true,
			guiActiveEditor = true
		)]
		public string angleInfo;
		private static string angleInfoNA = Localizer.Format("#DCKROT_n_a");

		public bool rotationEnabled = true;

		[KSPField(groupName = GROUPNAME, groupDisplayName = GROUPLABEL, groupStartCollapsed = false, guiActive = true, guiActiveEditor = true, isPersistant = true,
			guiName = "#DCKROT_rotation_step", guiUnits = "\u00b0")]
		[UI_FloatRange(minValue = 0, maxValue = 360, stepIncrement = 1, scene = UI_Scene.All, affectSymCounterparts = UI_Scene.All)]
		public float rotationStep = 15f; //make this the maxAngle?

		[KSPField(groupName = GROUPNAME, groupDisplayName = GROUPLABEL, groupStartCollapsed = false, guiActive = true, guiActiveEditor = true, isPersistant = true, guiName = "#autoLOC_8002345"), //Target Angle
UI_FloatRange(minValue = 0, maxValue = 360, stepIncrement = 1, scene = UI_Scene.All, affectSymCounterparts = UI_Scene.All)]
		public float targetAngle = 0;

		[KSPField(groupName = GROUPNAME, groupDisplayName = GROUPLABEL, groupStartCollapsed = false, guiActive = true, guiActiveEditor = true, isPersistant = true, guiName = "max Rotation Angle"), //Max Rotation Angle "#autoLOC_8200055" + "#autoLOC_8200055"
UI_FloatRange(minValue = 1, maxValue = 360, stepIncrement = 1, scene = UI_Scene.All, affectSymCounterparts = UI_Scene.All)]
		public float maxAngle = 360;

		[UI_FloatRange(minValue = 0, maxValue = 100, stepIncrement = 1, scene = UI_Scene.All, affectSymCounterparts = UI_Scene.All)]
		[KSPField(groupName = GROUPNAME, groupDisplayName = GROUPLABEL, groupStartCollapsed = false, guiActive = true, guiActiveEditor = true,
			isPersistant = true, guiName = "#DCKROT_rotation_speed", guiUnits = "\u00b0/s")]
		public float rotationSpeed = 5f;

		[KSPField(groupName = GROUPNAME, groupDisplayName = GROUPLABEL,	groupStartCollapsed = false, guiActive = true,
			guiActiveEditor = true,	isPersistant = true, advancedTweakable = true, guiName = "#DCKROT_reverse_rotation"),
        UI_Toggle(affectSymCounterparts = UI_Scene.All)]
        public bool reverseRotation = false;

		[KSPField(isPersistant = true)]
		public bool speedController = false;

		[KSPEvent(groupName = GROUPNAME, groupDisplayName = GROUPLABEL, groupStartCollapsed = false, guiActive = true,
			guiActiveEditor = true, guiName = "SpeedController", active = true)]//Toggle Controller Enabled
		public void ToggleSpeedController()
		{
			speedController = !speedController;
			if (speedController) Events["ToggleSpeedController"].guiName = "SpeedController Enabled";
            else Events["ToggleSpeedController"].guiName = "SpeedController Disabled";
            Fields["minAirspeed"].guiActive = speedController;
			Fields["minAirspeed"].guiActiveEditor = speedController;
			Fields["maxAirspeed"].guiActive = speedController;
			Fields["maxAirspeed"].guiActiveEditor = speedController;

            Events["Rotate"].guiActive = !speedController;
            Events["RotateClockwise"].guiActive = !speedController;
            Events["RotateCounterclockwise"].guiActive = !speedController;

            if (part == null || part.PartActionWindow == null) return;
            using (List<Part>.Enumerator pSym = part.symmetryCounterparts.GetEnumerator())
                while (pSym.MoveNext())
                {
                    if (pSym.Current == null) continue;
                    if (pSym.Current != part && pSym.Current.vessel == vessel)
                    {
                        var rotor = pSym.Current.FindModuleImplementing<ModuleBaseRotate>();
                        if (rotor == null) continue;
						rotor.speedController = speedController;
                        if (speedController) rotor.Events["ToggleSpeedController"].guiName = "SpeedController Enabled";
                        else rotor.Events["ToggleSpeedController"].guiName = "SpeedController Disabled";
                        rotor.Fields["minAirspeed"].guiActive = speedController;
                        rotor.Fields["minAirspeed"].guiActiveEditor = speedController;
                        rotor.Fields["maxAirspeed"].guiActive = speedController;
                        rotor.Fields["maxAirspeed"].guiActiveEditor = speedController;

                        rotor.Events["Rotate"].guiActive = !speedController;
                        rotor.Events["RotateClockwise"].guiActive = !speedController;
                        rotor.Events["RotateCounterclockwise"].guiActive = !speedController;                       
                            
                        rotor.part.PartActionWindow.UpdateWindow();
                    }
                }
            part.PartActionWindow.UpdateWindow();
		}

		[KSPField(groupName = GROUPNAME, groupDisplayName = GROUPLABEL, groupStartCollapsed = false, guiActive = true, guiActiveEditor = true, isPersistant = true, guiName = "Min Airspeed"), //Min Air Speed #autoLOC_8200054" + "#autoLOC_8012003
		UI_FloatRange(minValue = 0, maxValue = 500, stepIncrement = 10, scene = UI_Scene.All, affectSymCounterparts = UI_Scene.All)]
		public float minAirspeed = 0;
		[KSPField(groupName = GROUPNAME, groupDisplayName = GROUPLABEL, groupStartCollapsed = false, guiActive = true, guiActiveEditor = true, isPersistant = true, guiName = "max Airspeed"), //Max Air Speed #autoLOC_8200055" + "#autoLOC_8012003
		UI_FloatRange(minValue = 100, maxValue = 1000, stepIncrement = 25, scene = UI_Scene.All, affectSymCounterparts = UI_Scene.All)]
		public float maxAirspeed = 200;

		[KSPField(isPersistant = true)]
		public string soundClip = "DockRotate/DockRotateMotor";

		[KSPField(isPersistant = true)]
		public float soundVolume = 0.5f;

		[KSPField(isPersistant = true)]
		public float soundPitch = 1f;

		public bool smartAutoStruts = true;

		public float anglePosition;

		private bool needsAlignment;

		public float angleVelocity;

		public bool angleIsMoving;

		public bool verboseSetup = false;

		public bool verboseEvents = false;

		bool hasdeployed = false; //toggle direction bool, false for start pos, true for reversing from max Angle

		[KSPAction(guiName = "#autoLOC_8003282", requireFullControl = true)] //Toggle Hinge
		public void Rotate(KSPActionParam param)
		{
			if (speedController) return;
            doRotate(false, hasdeployed);
            hasdeployed = !hasdeployed;
        }

		[KSPEvent(guiName = "#autoLOC_8003282", groupName = GROUPNAME, groupDisplayName = GROUPLABEL, groupStartCollapsed = false,
			guiActive = true, guiActiveEditor = true, requireFullControl = true)] //Toggle Hinge
		public void Rotate()
		{
			doRotate(true, hasdeployed);
			hasdeployed = !hasdeployed;
		}

		[KSPAction(
			guiName = "#DCKROT_rotate_clockwise",
			requireFullControl = true
		)]
		public void RotateClockwise(KSPActionParam param)
		{
            if (speedController) return;
            if (verboseEvents)
				log(desc(), ": action " + param.desc());
			if (reverseActionRotationKey()) {
				doRotateCounterclockwise(false);
			} else {
				doRotateClockwise(false);
			}
		}

		[KSPEvent(guiName = "#DCKROT_rotate_clockwise",	groupName = GROUPNAME, groupDisplayName = GROUPLABEL, groupStartCollapsed = false,
			guiActive = true, guiActiveEditor = true, requireFullControl = true)]
		public void RotateClockwise()
		{
			doRotateClockwise(true);
		}

		[KSPAction(
			guiName = "#DCKROT_rotate_counterclockwise",
			requireFullControl = true
		)]
		public void RotateCounterclockwise(KSPActionParam param)
		{
            if (speedController) return;
            if (verboseEvents)
				log(desc(), ": action " + param.desc());
			if (reverseActionRotationKey()) {
				doRotateClockwise(false);
			} else {
				doRotateCounterclockwise(false);
			}
		}

		[KSPEvent(guiName = "#DCKROT_rotate_counterclockwise", groupName = GROUPNAME, groupDisplayName = GROUPLABEL, groupStartCollapsed = false, guiActive = true,
			guiActiveEditor = true, requireFullControl = true)]
		public void RotateCounterclockwise()
		{
			doRotateCounterclockwise(true);
		}

		public bool autoSnap = false;

		public bool hideCommands = false;

		public void doRotateClockwise(bool calledByButton)
		{
			if (!canStartRotation(true))
				return;
			if (!enqueueRotation(step(), speed(), 0, calledByButton))
				return;
		}

		public void doRotateCounterclockwise(bool calledByButton)
		{
			if (!canStartRotation(true))
				return;
			if (!enqueueRotation(-step(), speed(), 0, calledByButton))
				return;
		}

		public void doRotate(bool calledByButton, bool rotateCCW)
		{
			if (!canStartRotation(true))
				return;
			if (!enqueueRotation(maxAngle * (rotateCCW ? -1 : 1) * (reverseRotation ? -1 : 1), speed(), 0, calledByButton))
				return;
		}

		protected bool reverseActionRotationKey()
		{
			return GameSettings.MODIFIER_KEY.GetKey();
		}

		public bool IsJointUnlocked()
		{
			bool ret = currentRotation();
			if (verboseEvents || ret)
				log(desc(), ".IsJointUnlocked() is " + ret);
			return ret;
		}

		private static List<PartResourceDefinition> cached_GetConsumedResources = null;

		public List<PartResourceDefinition> GetConsumedResources()
		{
			// log(desc(), ".GetConsumedResource() called");
			if (cached_GetConsumedResources == null) {
				cached_GetConsumedResources = new List<PartResourceDefinition>();
				PartResourceDefinition ec = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");
				if (ec != null)
					cached_GetConsumedResources.Add(ec);
			}
			return cached_GetConsumedResources;
		}

		protected bool setupLocalAxisDone;
		protected abstract bool setupLocalAxis(StartState state);
		protected abstract AttachNode findMovingNodeInEditor(out Part otherPart, bool verbose);

		protected JointMotion jointMotion;
		protected bool hasJointMotion;
		protected abstract PartJoint findMovingJoint(bool verbose);

		public string nodeRole = "Init";

		protected Vector3 partNodePos; // node position, relative to part
		protected Vector3 partNodeAxis; // node rotation axis, relative to part

		[KSPField(isPersistant = true)]
		public string Axis = "Z";
		[KSPField(isPersistant = true)]
		public string AxisTransform = "";

		// if this works, see about slaving ModuleNodeRotate to ModuleTurret/ have a pair of parts/part with paired MNR modules, each with a different axis, and use ModuleTurret's axis to target check stuff to send the proper 
		// target angle to each module, set degrees per second to turret rotate speed

		// localized info cache
		protected string storedModuleDisplayName = "";
		protected string storedInfo = "";
		protected abstract void fillInfo();

		public override string GetModuleDisplayName()
		{
			if (storedModuleDisplayName == "")
				fillInfo();
			return storedModuleDisplayName;
		}

		public override string GetInfo()
		{
			if (storedInfo == "")
				fillInfo();
			return storedInfo;
		}

		private ModuleBaseRotate parentBaseRotate = null;

		private void fillParentBaseRotate()
		{
			parentBaseRotate = null;
			for (Part p = part.parent; p; p = p.parent) {
				parentBaseRotate = p.FindModuleImplementing<ModuleBaseRotate>();
				if (parentBaseRotate)
					break;
			}
		}

		private List<CModuleStrut> crossStruts = new List<CModuleStrut>();

		private void fillCrossStruts()
		{
			crossStruts.Clear();
			List<CModuleStrut> allStruts = vessel.FindPartModulesImplementing<CModuleStrut>();
			if (allStruts == null)
				return;
			PartSet rotParts = PartSet.allPartsFromHere(part);
			for (int i = 0; i < allStruts.Count; i++) {
				PartJoint sj = allStruts[i] ? allStruts[i].strutJoint : null;
				if (sj && sj.Host && sj.Target && rotParts.contains(sj.Host) != rotParts.contains(sj.Target))
					crossStruts.Add(allStruts[i]);
			}
		}

		[KSPField(isPersistant = true)]
		public Vector3 frozenRotation = Vector3.zero;

		public bool frozenFlag {
			get => !frozenAngle.isZero();
		}

		public float frozenAngle {
			get => frozenRotation[0];
			set => frozenRotation[0] = value;
		}

		public float frozenSpeed {
			get => frozenRotation[1];
			set => frozenRotation[1] = value;
		}

		public float frozenStartSpeed {
			get => frozenRotation[2];
			set => frozenRotation[2] = value;
		}

		[KSPField(isPersistant = true)]
		public float electricityRate = 1f;

		public Part getPart()
		{
			return part;
		}

		protected int setupDoneAt = 0;
		protected bool setupDone {
			get => setupDoneAt != 0;
		}

		public void onFieldChange(string name, Callback<BaseField, object> fun)
		{
			BaseField fld = Fields[name];
			if (fld == null) {
				log(desc(), ".onFieldChange(\"" + name + "\") can't find field");
				return;
			}

			if (fld.uiControlEditor != null)
				fld.uiControlEditor.onFieldChanged = fun;
			if (fld.uiControlFlight != null)
				fld.uiControlFlight.onFieldChanged = fun;
		}

		public void testCallback(BaseField fld, object oldValue)
		{
			string name = fld != null ? fld.name : "<null>";
			object newValue = fld.GetValue(this);
			log(desc(), ": CHANGED " + name + ": " + oldValue + " -> " + newValue);
		}

		protected virtual void doSetup(bool onLaunch)
		{
			if (hasJointMotion && jointMotion.rotCur) {
				log(desc(), ": skipping, is rotating");
				return;
			}

			jointMotion = null;
			hasJointMotion = false;
			nodeRole = "None";
			anglePosition = rotationAngle();
			angleVelocity = 0f;
			angleIsMoving = false;
			needsAlignment = false;
			//enabled = false;
			stagingEnabled = false;

			if (!part || !vessel || !setupLocalAxisDone) {
				log("" + GetType(), ": *** WARNING *** doSetup() called at a bad time");
				return;
			}

			if (!part.hasPhysics()) {
				log(desc(), ".doSetup(): physicsless part, disabled");
				return;
			}

			try {
				if (onLaunch)
					log(part.desc(), ".doSetup(): at launch");

				fillParentBaseRotate();
				fillCrossStruts();
				setupGuiActive();
				PartJoint rotatingJoint = findMovingJoint(verboseSetup);

				if (rotatingJoint && !rotatingJoint.safetyCheck()) {
					log(part.desc(), ": joint safety check failed for "
						+ rotatingJoint.desc());
					rotatingJoint = null;
				}

				if (rotatingJoint) {
					jointMotion = JointMotion.get(rotatingJoint);
					hasJointMotion = jointMotion;
					if (!jointMotion.hasController())
						jointMotion.controller = this;
					jointMotion.updateOrgRot();
					anglePosition = rotationAngle();
				}

				onFieldChange(nameof(rotationEnabled), testCallback);
				onFieldChange(nameof(rotationStep), testCallback);
				onFieldChange(nameof(rotationSpeed), testCallback);
			} catch (Exception e) {
				string sep = new string('-', 80);
				log(sep);
				log("Exception during setup:\n" + e.StackTrace);
				log(sep);
			}

			if (hasJointMotion) {
				nodeRole = part == jointMotion.joint.Host ? "Host"
					: part == jointMotion.joint.Target ? "Target"
					: "Unknown";
				if (jointMotion.joint.isOffTree())
					nodeRole += "OT";
			}

			log(desc(), ".doSetup(): joint " + (hasJointMotion ? jointMotion.joint.desc() : "null"));

			//setupGroup();

			setupDoneAt = Time.frameCount;

			//enabled = hasJointMotion;
		}

		public IEnumerator doSetupDelayed(bool onLaunch)
		{
			yield return new WaitForFixedUpdate();
			doSetup(onLaunch);
		}

		private bool care(Vessel v)
		{
			return v == vessel;
		}

		private bool care(Part p)
		{
			return p && p.vessel == vessel;
		}

		private bool care(GameEvents.FromToAction<Part, Part> action)
		{
			return care(action.from) || care(action.to);
		}

		private bool care(GameEvents.FromToAction<ModuleDockingNode, ModuleDockingNode> action)
		{
			// special case for same vessel dock/undock
			return action.from.part == part || action.to.part == part;
		}

		private bool care(uint id1, uint id2)
		{
			return vessel && (vessel.persistentId == id1 || vessel.persistentId == id2);
		}

		protected void scheduleDockingStatesCheck(bool verbose)
		{
			VesselMotionManager vmm = VesselMotionManager.get(vessel);
			if (vmm)
				vmm.scheduleDockingStatesCheck(verbose);
		}

		public void OnVesselGoOnRails(Vessel v)
		{
			bool c = care(v);
			evlog(nameof(OnVesselGoOnRails), v, c);
			if (!c) return;
			freezeCurrentRotation("go on rails", false);
			setupDoneAt = 0;
			VesselMotionManager vmm = VesselMotionManager.get(vessel);
			if (vmm)
				vmm.resetRotCount();
		}

		public void OnVesselGoOffRails(Vessel v)
		{
			bool c = care(v);
			evlog(nameof(OnVesselGoOffRails), v, c);
			if (!c) return;

			// start speed always 0 when going off rails
			frozenStartSpeed = 0f;

			setupDoneAt = 0;
			doSetup(justLaunched);
			justLaunched = false;

			scheduleDockingStatesCheck(false);
		}

		public void RightBeforeStructureChange_Action(GameEvents.FromToAction<Part, Part> action)
		{
			bool c = care(action);
			evlog(nameof(RightBeforeStructureChange_Action), action, c);
			if (!c) return;
			RightBeforeStructureChange();
		}

		public void RightBeforeStructureChange_Ids(uint id1, uint id2)
		{
			bool c = care(id1, id2);
			evlog(nameof(RightBeforeStructureChange_Ids), id1, id2, c);
			if (!c) return;
			RightBeforeStructureChange();
		}

		private void RightBeforeStructureChange_JointUpdate(Vessel v)
		{
			bool c = care(v);
			evlog(nameof(RightBeforeStructureChange_JointUpdate), v, c);
			if (!c) return;
			RightBeforeStructureChange();
		}

		public void RightBeforeStructureChange_Part(Part p)
		{
			bool c = care(p);
			evlog(nameof(RightBeforeStructureChange_Part), p, c);
			if (!c) return;
			RightBeforeStructureChange();
		}

		public void RightAfterStructureChange_Action(GameEvents.FromToAction<Part, Part> action)
		{
			bool c = care(action);
			evlog(nameof(RightAfterStructureChange_Action), action, c);
			if (!c) return;
			RightAfterStructureChange();
		}

		public void RightAfterStructureChange_Part(Part p)
		{
			bool c = !setupDone || care(p);
			evlog(nameof(RightAfterStructureChange_Part), p, c);
			if (!c)
				log(desc(), ": setupDoneAt=" + setupDoneAt);
			if (!c) return;
			RightAfterStructureChange();
		}

		public void RightAfterSameVesselDock(GameEvents.FromToAction<ModuleDockingNode, ModuleDockingNode> action)
		{
			bool c = care(action);
			evlog(nameof(RightAfterSameVesselDock), action, c);
			if (!c) return;
			RightAfterStructureChangeDelayed();
			scheduleDockingStatesCheck(false);
		}

		public void RightAfterSameVesselUndock(GameEvents.FromToAction<ModuleDockingNode, ModuleDockingNode> action)
		{
			bool c = care(action);
			evlog(nameof(RightAfterSameVesselUndock), action, c);
			if (!c) return;
			RightAfterStructureChangeDelayed();
			scheduleDockingStatesCheck(false);
		}

		public void RightAfterEditorChange_ShipModified(ShipConstruct ship)
		{
			RightAfterEditorChange("MODIFIED");
		}

		public void RightAfterEditorChange_Event(ConstructionEventType type, Part part)
		{
			if (type == ConstructionEventType.PartDragging
				|| type == ConstructionEventType.PartOffsetting
				|| type == ConstructionEventType.PartRotating)
				return;
			RightAfterEditorChange("EVENT " + type);
		}

		public void RightAfterEditorChange(string msg)
		{
			if (verboseEvents)
				log(desc(), ".RightAfterEditorChange(" + msg + ")"
					+ " > [" + part.children.Count + "]"
					+ " < " + part.parent.desc() + " " + part.parent.descOrg());

			float angle = rotationAngle();
			if (float.IsNaN(angle)) {
				angleInfo = angleInfoNA;
			} else {
				angleInfo = String.Format("{0:+0.00;-0.00;0.00}\u00b0", angle);
			}

			//checkGuiActive();
		}

		public void RightBeforeStructureChange()
		{
			freezeCurrentRotation("structure change", true);
			setupDoneAt = 0;
		}

		public void RightAfterStructureChange()
		{
			doSetup(false);
		}

		public void RightAfterStructureChangeDelayed()
		{
			StartCoroutine(doSetupDelayed(false));
		}

		private bool eventState = false;

		private void setEvents(bool cmd)
		{
			if (cmd == eventState) {
				if (verboseSetup)
					log(desc(), ".setEvents(" + cmd + ") repeated");
				return;
			}

			if (verboseSetup)
				log(desc(), ".setEvents(" + cmd + ")");

			if (cmd) {
				GameEvents.onActiveJointNeedUpdate.Add(RightBeforeStructureChange_JointUpdate);

				GameEvents.onEditorShipModified.Add(RightAfterEditorChange_ShipModified);
				GameEvents.onEditorPartEvent.Add(RightAfterEditorChange_Event);

				GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRails);
				GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);

				GameEvents.onPartCouple.Add(RightBeforeStructureChange_Action);
				GameEvents.onPartCoupleComplete.Add(RightAfterStructureChange_Action);
				GameEvents.onPartDeCouple.Add(RightBeforeStructureChange_Part);
				GameEvents.onPartDeCoupleComplete.Add(RightAfterStructureChange_Part);

				GameEvents.onVesselDocking.Add(RightBeforeStructureChange_Ids);
				GameEvents.onDockingComplete.Add(RightAfterStructureChange_Action);
				GameEvents.onPartUndock.Add(RightBeforeStructureChange_Part);
				GameEvents.onPartUndockComplete.Add(RightAfterStructureChange_Part);

				GameEvents.onSameVesselDock.Add(RightAfterSameVesselDock);
				GameEvents.onSameVesselUndock.Add(RightAfterSameVesselUndock);
			} else {
				GameEvents.onActiveJointNeedUpdate.Remove(RightBeforeStructureChange_JointUpdate);

				GameEvents.onEditorShipModified.Remove(RightAfterEditorChange_ShipModified);
				GameEvents.onEditorPartEvent.Remove(RightAfterEditorChange_Event);

				GameEvents.onVesselGoOnRails.Remove(OnVesselGoOnRails);
				GameEvents.onVesselGoOffRails.Remove(OnVesselGoOffRails);

				GameEvents.onPartCouple.Remove(RightBeforeStructureChange_Action);
				GameEvents.onPartCoupleComplete.Remove(RightAfterStructureChange_Action);
				GameEvents.onPartDeCouple.Remove(RightBeforeStructureChange_Part);
				GameEvents.onPartDeCoupleComplete.Remove(RightAfterStructureChange_Part);

				GameEvents.onVesselDocking.Remove(RightBeforeStructureChange_Ids);
				GameEvents.onDockingComplete.Remove(RightAfterStructureChange_Action);
				GameEvents.onPartUndock.Remove(RightBeforeStructureChange_Part);
				GameEvents.onPartUndockComplete.Remove(RightAfterStructureChange_Part);

				GameEvents.onSameVesselDock.Remove(RightAfterSameVesselDock);
				GameEvents.onSameVesselUndock.Remove(RightAfterSameVesselUndock);
			}

			eventState = cmd;
		}

		private static readonly string[,] guiList = {
			// flags:
			// S: is a setting
			// C: is a command
			// D: is a debug display
			// A: show with rotation disabled
			// F: show when needsAlignment
			{ "nodeRole", "S" },
			{ "rotationStep", "S" },
			{ "targetAngle", "S" },
			{ "maxAngle", "S" },
			{ "rotationSpeed", "S" },
			{ "speedController","C" },
			{ "minAirspeed","S" },
			{ "maxAirspeed","S" },
			{ "reverseRotation", "S" },
			{ "smartAutoStruts", "SD" },
			{ "Rotate", "C" },
			{ "RotateClockwise", "C" },
			{ "RotateCounterclockwise", "C" },
			{ "RotateToSnap", "CF" },
			{ "autoSnap", "D" },
			{ "hideCommands", "D" }
		};

		private struct GuiInfo {
			public string name;
			public string flags;
			public BaseField fld;
			public BaseEvent evt;
		}

		private GuiInfo[] guiInfo;

		protected void setupGuiActive()
		{
			int l = guiList.GetLength(0);

			guiInfo = new GuiInfo[l];

			for (int i = 0; i < l; i++) {
				string n = guiList[i, 0];
				string f = guiList[i, 1];
				ref GuiInfo ii = ref guiInfo[i];
				ii.name = n;
				ii.flags = f;
				ii.fld = Fields[n];
				ii.evt = Events[n];
			}
		}

		private void checkGuiActive()
		{
			if (guiInfo != null) {
				bool csr = canStartRotation(false);
				bool csra = canStartRotation(false, true);
				for (int i = 0; i < guiInfo.Length; i++) {
					ref GuiInfo ii = ref guiInfo[i];
					bool flagsCheck = !(hideCommands && ii.flags.IndexOf('C') >= 0)
						&& (DEBUGMODE || ii.flags.IndexOf('D') < 0);
					if (ii.fld != null)
						ii.fld.guiActive = ii.fld.guiActiveEditor = rotationEnabled && flagsCheck;
					if (ii.evt != null)
						ii.evt.guiActive = ii.evt.guiActiveEditor = flagsCheck
							&& (ii.flags.IndexOf('A') >= 0 ? csra : csr);
					if (needsAlignment && ii.flags.IndexOf('F') >= 0)
						ii.evt.guiActive = true;
				}
			}
			if (part.PartActionWindow != null)
				setupGroup();
		}

		private void setupGroup()
		{
			bool expanded = hasJointMotion && (rotationEnabled || needsAlignment);
			List<BasePAWGroup> l = allGroups(GROUPNAME);
			for (int i = 0; i < l.Count; i++)
				l[i].startCollapsed = !expanded;
		}

		private List<BasePAWGroup> cached_allGroups = null;

		private List<BasePAWGroup> allGroups(string name)
		{
			if (cached_allGroups == null) {
				cached_allGroups = new List<BasePAWGroup>();
				for (int i = 0; i < Fields.Count; i++)
					if (Fields[i] != null && Fields[i].group != null && Fields[i].group.name == name)
						cached_allGroups.Add(Fields[i].group);
				for (int i = 0; i < Events.Count; i++)
					if (Events[i] != null && Events[i].group != null && Events[i].group.name == name)
						cached_allGroups.Add(Events[i].group);
			}
			return cached_allGroups;
		}

		public override void OnAwake()
		{
			base.OnAwake();
			setupDoneAt = 0;
		}

		private bool justLaunched = false;

		public void Start()
		{
            UI_FloatRange angle = (UI_FloatRange)Fields["targetAngle"].uiControlEditor;
            angle.onFieldChanged = onTargetAngleUpdated;
            angle.maxValue = maxAngle;
            UI_FloatRange angleFlt = (UI_FloatRange)Fields["targetAngle"].uiControlFlight;
            angleFlt.onFieldChanged = onTargetAngleUpdated;
            angleFlt.maxValue = maxAngle;
            var angleCap = (UI_FloatRange)Fields["maxAngle"].uiControlEditor;
            angleCap.onFieldChanged = clampFields;
            angleCap = (UI_FloatRange)Fields["maxAngle"].uiControlFlight;
            angleCap.onFieldChanged = clampFields;
            var minAS = (UI_FloatRange)Fields["minAirspeed"].uiControlEditor;
            minAS.onFieldChanged = clampFields;
            minAS = (UI_FloatRange)Fields["minAirspeed"].uiControlFlight;
            minAS.onFieldChanged = clampFields;
            var maxAS = (UI_FloatRange)Fields["maxAirspeed"].uiControlEditor;
            maxAS.onFieldChanged = clampFields;
            maxAS = (UI_FloatRange)Fields["maxAirspeed"].uiControlFlight;
            maxAS.onFieldChanged = clampFields;
            ClampFields("maxAngle");
            ClampFields("minAirspeed");
            ClampFields("maxAirspeed");

            Fields["minAirspeed"].guiActive = speedController;
            Fields["minAirspeed"].guiActiveEditor = speedController;
            Fields["maxAirspeed"].guiActive = speedController;
            Fields["maxAirspeed"].guiActiveEditor = speedController;
        }
		public override void OnStart(StartState state)
		{
#if !DEBUG
			verboseSetup = verboseEvents = false;
#endif
			justLaunched = state == StartState.PreLaunch;

			if (verboseEvents)
				log(desc(), ".OnStart(" + state + ")");

			base.OnStart(state);

			checkRevision();

			setupLocalAxisDone = setupLocalAxis(state);

			//setupGuiActive();

			setEvents(true);
			if (state == StartState.Editor) {
				RightAfterEditorChange("START");
				return;
			}

			if (vessel) {
				VesselMotionManager.get(vessel); // force creation of VesselMotionManager
			} else if (state != StartState.Editor) {
				log(desc(), ".OnStart(" + state + ") with no vessel");
			}

			//checkGuiActive();
		}

		public void onTargetAngleUpdated(BaseField field, object obj)
		{
			if (targetAngle - Mathf.Abs(anglePosition) == 0) return;
			//enqueueRotation((targetAngle - Mathf.Abs(currentAngle)) * (reverseRotation ? -1 : 1), speed(), 0, !speedController);
			StartCoroutine(TargetAngleUpdate());
			//Debug.Log($"[SPDCtrlDebug] targetAngle: {targetAngle.ToString("0.00")}; anglePosition: {Mathf.Abs(anglePosition).ToString("0.00")}; currentAngle: {currentAngle}");
		}
        IEnumerator TargetAngleUpdate()
        {
            yield return new WaitForSeconds(0.25f);
            enqueueRotation((targetAngle - Mathf.Abs(currentAngle)) * (reverseRotation ? -1 : 1), speed(), 0, !speedController);
        }
        public void clampFields(BaseField field, object obj)
		{
			ClampFields(field.name);
		}
		public void ClampFields(string fieldName)
		{
			switch (fieldName)
			{
				case "maxAngle":
					{
						var angle = (UI_FloatRange)Fields["targetAngle"].uiControlEditor;
						angle.maxValue = maxAngle;
                        angle = (UI_FloatRange)Fields["targetAngle"].uiControlFlight;
                        angle.maxValue = maxAngle;
                        if (targetAngle > maxAngle) targetAngle = maxAngle;
						var step = (UI_FloatRange)Fields["rotationStep"].uiControlEditor;
						step.maxValue = maxAngle;
                        step = (UI_FloatRange)Fields["rotationStep"].uiControlFlight;
                        step.maxValue = maxAngle;
                        if (rotationStep > maxAngle) rotationStep = maxAngle;
						break;
					}
				case "minAirspeed":
					{
						UI_FloatRange max = (HighLogic.LoadedSceneIsFlight ? (UI_FloatRange)Fields["maxAirspeed"].uiControlFlight : (UI_FloatRange)Fields["maxAirspeed"].uiControlEditor);
						if (minAirspeed >= 100)
						{
							max.minValue = minAirspeed + 1;
							if (maxAirspeed < minAirspeed) maxAirspeed = minAirspeed + 1;
						}
						if (minAirspeed < 100 && max.minValue > 100) max.minValue = 100;
                        break;
					}
				case "maxAirspeed":
					{
                        UI_FloatRange min = (HighLogic.LoadedSceneIsFlight ? (UI_FloatRange)Fields["minAirspeed"].uiControlFlight : (UI_FloatRange)Fields["minAirspeed"].uiControlEditor);
                        if (maxAirspeed > 500 && min.maxValue < 500) min.maxValue = 500;
						if (maxAirspeed <= 500)
						{
							min.maxValue = maxAirspeed - 1;
							if (maxAirspeed < minAirspeed) minAirspeed = maxAirspeed - 1;
						}
						break;
					}
				default:
					Debug.LogError($"[ModuleBaseRotate]: Invalid field name {fieldName} in ClampFields.");
					break;
			}
		}
		public override void OnUpdate()
		{
			base.OnUpdate();

			JointMotionObj cr = currentRotation();

			anglePosition = rotationAngle();
			angleVelocity = cr ? cr.vel : 0f;
			angleIsMoving = cr;

			needsAlignment = hasJointMotion && !angleIsMoving
				&& Mathf.Abs(jointMotion.angleToSnap(rotationStep)) >= .5e-4f;

			if (MapView.MapIsEnabled || !part.PartActionWindow)
				return;

			bool updfrm = ((Time.frameCount + part.flightID) & 3) == 0;
			if (updfrm || cr)
				updateStatus(cr);
			//if (updfrm)
				//checkGuiActive();
		}

		public virtual void OnDestroy()
		{
			setEvents(false);
		}

		protected virtual void updateStatus(JointMotionObj cr)
		{
			if (cr) {
				angleInfo = String.Format(
					"{0:+0.00;-0.00;0.00}\u00b0 > {1:+0.00;-0.00;0.00}\u00b0 ({2:+0.00;-0.00;0.00}\u00b0/s){3}",
					anglePosition, jointMotion.rotationTarget(),
					cr.vel, (jointMotion.controller == this ? " CTL" : ""));
			} else {
				if (float.IsNaN(anglePosition)) {
					angleInfo = angleInfoNA;
				} else {
					angleInfo = String.Format(
						"{0:+0.00;-0.00;0.00}\u00b0 ({1:+0.0000;-0.0000;0.0000}\u00b0\u0394)",
						anglePosition, dynamicDeltaAngle());
				}
			}
		}

		protected virtual bool canStartRotation(bool verbose, bool ignoreDisabled = false)
		{
			string failMsg = "";

			if (HighLogic.LoadedSceneIsEditor) {
				if (!rotationEnabled) {
					failMsg = "rotation disabled";
				} else if (!findHostPartInEditor(verbose)) {
					failMsg = "can't find host part";
				}
			} else {
				if (!rotationEnabled && !ignoreDisabled) {
					failMsg = "rotation disabled";
				} else if (!setupDone) {
					failMsg = "not set up";
				} else if (!hasJointMotion) {
					failMsg = "no joint motion";
				} else if (!vessel) {
					failMsg = "no vessel";
				} else if (vessel.CurrentControlLevel != Vessel.ControlLevel.FULL) {
					failMsg = "uncontrolled vessel";
				}
			}

			if (verbose && failMsg != "")
				log(desc(), ".canStartRotation(): " + failMsg);

			return failMsg == "";
		}

		public float step()
		{
			float s = Mathf.Abs(rotationStep);
			if (s < 0.1f)
				s = SmoothMotion.CONTINUOUS;
			if (reverseRotation)
				s = -s;
			return s;
		}

		public float speed()
		{
			float s = Mathf.Abs(rotationSpeed);
			return s >= 1f ? s : 1f;
		}

		private Part findHostPartInEditor(bool verbose)
		{
			AttachNode node = findMovingNodeInEditor(out Part other, verbose);
			if (node == null || other == null)
				return null;
			return other.parent == part ? other :
				part.parent == other ? part :
				null;
		}

		protected float rotationAngle()
		{
			if (HighLogic.LoadedSceneIsEditor) {
				Part host = findHostPartInEditor(false);
				if (!host || !host.parent)
					return float.NaN;
				Part target = host.parent;
				Vector3 axis = host == part ? partNodeAxis : -partNodeAxis;
				Vector3 hostNodeAxis = axis.Td(part.T(), host.T());
				return hostNodeAxis.axisSignedAngle(hostNodeAxis.findUp(),
					hostNodeAxis.Td(host.T(), target.T()).findUp().Td(target.T(), host.T()));
			}

			return hasJointMotion ? jointMotion.rotationAngle() : float.NaN;
		}

		protected float dynamicDeltaAngle()
		{
			if (!HighLogic.LoadedSceneIsFlight)
				return 0f;
			return hasJointMotion ? jointMotion.dynamicDeltaAngle() : float.NaN;
		}

		public void putAxis(JointMotion jm)
		{
			jm.setAxis(part, partNodeAxis, partNodePos);
		}

		protected bool enqueueRotation(Vector3 frozen)
		{
			return enqueueRotation(frozen[0], frozen[1], frozen[2], false);
		}

        [KSPField(isPersistant = true, guiActive = true, guiName = "currentAngle", guiActiveEditor = true), UI_Label(affectSymCounterparts = UI_Scene.None, scene = UI_Scene.None)]//Weapon Name 
        public float currentAngle = 0;

        protected bool enqueueRotation(float angle, float speed, float startSpeed = 0f, bool doSymmetry = false) //angle is target angle to rotate to
		{
			if (reverseRotation) //something is resetting currentAngle to 0 when reverseRotation, which is causing issues with the targetAngle rotation process
			{
                if (currentAngle + angle > 0)
                {
                    angle -= (currentAngle + angle);
                }
                if (currentAngle + angle < -maxAngle)  //clamp rotation to specified min/max angle
                {
                    angle = -currentAngle;
                }
            }
			else
			{
				if (currentAngle + angle > maxAngle)
				{
					angle -= (currentAngle + angle) - maxAngle;
				}
				if (currentAngle + angle < 0) //clamp rotation to specified min/max angle
				{
					angle = -currentAngle; //null to zero to not exceed angle limit
                }
            }
            currentAngle += angle;
			if (maxAngle > 359 && currentAngle * (reverseRotation ? -1 : 1) > 359) currentAngle = 0;//reset in ase of 360deg rotation
            if (HighLogic.LoadedSceneIsEditor)
			{
				log(desc(), ".enqueueRotation(): " + angle + "\u00b0 in editor");

				Part host = findHostPartInEditor(false);
				if (!host || !host.parent)
					return false;

				Vector3 axis = host == part ? -partNodeAxis : partNodeAxis;
				axis = axis.Td(part.T(), null);
				Vector3 pos = partNodePos.Tp(part.T(), null);
				Quaternion rot = axis.rotation(angle); //this is from wherever the part currently is oriented; need a constant/base quaternion taken at start for what rest orientation should be

				Transform t = host.transform;
				t.SetPositionAndRotation(rot * (t.position - pos) + pos,
					rot * t.rotation);

				GameEvents.onEditorPartEvent.Fire(ConstructionEventType.PartRotated, host);
				GameEvents.onEditorPartEvent.Fire(ConstructionEventType.PartTweaked, host);
				if (doSymmetry)
                {
                    using (List<Part>.Enumerator pSym = part.symmetryCounterparts.GetEnumerator())
                        while (pSym.MoveNext())
                        {
                            if (pSym.Current == null) continue;
                            if (pSym.Current != part && pSym.Current.vessel == vessel)
                            {
                                var rotor = pSym.Current.FindModuleImplementing<ModuleBaseRotate>();
                                if (rotor == null) continue;
								rotor.enqueueRotation(angle, speed, 0, false);
                            }
                        }
                }
				return true;
			}

			if (!hasJointMotion)
			{
				log(desc(), ".enqueueRotation(): no rotating joint, skipped");
				return false;
			}
			//enabled = true;
			
			if (doSymmetry) 
			{
				using (List<Part>.Enumerator pSym = part.symmetryCounterparts.GetEnumerator())
					while (pSym.MoveNext())
					{
						if (pSym.Current == null) continue;
						if (pSym.Current != part && pSym.Current.vessel == vessel)
						{
							var rotor = pSym.Current.FindModuleImplementing<ModuleBaseRotate>();
							if (rotor == null) continue;
							rotor.jointMotion.enqueueRotation(rotor, angle, speed, startSpeed);
						}
                    }
			}
			
			return jointMotion.enqueueRotation(this, angle, speed, startSpeed);
		}

		protected void freezeCurrentRotation(string msg, bool keepSpeed)
		{
			JointMotionObj cr = currentRotation();
			if (!cr)
				return;
			if (cr.controller != this) {
				log(desc(), ".freezeCurrentRotation(): skipping, not controller");
				return;
			}
			log(desc(), ".freezeCurrentRotation("
				+ msg + ", " + keepSpeed + ")");
			cr.isContinuous();
			float angle = cr.tgt - cr.pos;
			enqueueFrozenRotation(angle, cr.maxvel, keepSpeed ? cr.vel : 0f);
			cr.abort();
			log(desc(), ": removing rotation (freeze)");
			jointMotion.rotCur = null;
		}

		public bool isRotating()
		{
			return currentRotation();
		}

		protected JointMotionObj currentRotation()
		{
			return hasJointMotion ? jointMotion.rotCur : null;
		}

		protected void checkFrozenRotation()
		{
			if (!setupDone)
				return;

			if (frozenFlag) {
				/* // logging disabled, it always happens during continuous rotation
				log(desc(), ": thaw frozen rotation " + frozenRotation.desc()
					+ "@" + frozenRotationControllerID);
				*/
				enqueueRotation(frozenRotation);
			}

			updateFrozenRotation("CHECK");
		}

		public void updateFrozenRotation(string context)
		{
			Vector3 prevRot = frozenRotation;

			JointMotionObj cr = currentRotation();
			if (cr && cr.isContinuous() && jointMotion.controller == this) {
				frozenRotation.Set(cr.tgt, cr.maxvel, 0f);
			} else {
				frozenRotation = Vector3.zero;
			}

			if (frozenRotation != prevRot)
				log(desc(), ".updateFrozenRotation("
					+ context + "): " + prevRot + " -> " + frozenRotation);
		}

		protected void enqueueFrozenRotation(float angle, float speed, float startSpeed = 0f)
		{
			Vector3 prev = frozenRotation;
			angle += frozenAngle;
			SmoothMotion.isContinuous(ref angle);
			frozenRotation.Set(angle, speed, startSpeed);
			log(desc(), ".enqueueFrozenRotation(): "
				+ prev.desc() + " -> " + frozenRotation.desc());
		}

		private int lastUsefulFixedUpdate = 0;

		public void FixedUpdate()
		{
			if (!HighLogic.LoadedSceneIsFlight) return;

			if(setupDone) checkFrozenRotation();
			if (speedController && this.part.vessel.situation != Vessel.Situations.PRELAUNCH)
			{
				if (vessel.speed > minAirspeed && vessel.speed < maxAirspeed)
				{
					float controllerAngle = Mathf.Clamp(maxAngle / (maxAirspeed - minAirspeed + 0.001f) * ((float)vessel.speed - minAirspeed), 0, maxAngle); // Linearly varies between two limits, clamped at limit values
					targetAngle = controllerAngle;
				}
				else
				{
					if (vessel.speed <= minAirspeed) targetAngle = 0;
					if (vessel.speed >= maxAirspeed) targetAngle = maxAngle;
				}
                if (targetAngle - Mathf.Abs(anglePosition) == 0) return;
                enqueueRotation((targetAngle - Mathf.Abs(currentAngle)) * (reverseRotation ? -1 : 1), speed(), 0, !speedController);
            }
			else return;
			/*
			if (lastUsefulFixedUpdate < setupDoneAt) 
				lastUsefulFixedUpdate = setupDoneAt;
			else if (frozenFlag || currentRotation() != null || speedController) 
				lastUsefulFixedUpdate = Time.frameCount;
			else if (Time.frameCount - lastUsefulFixedUpdate > 10) {
				// log(part.desc(), ": disabling useless MonoBehaviour updates");
				enabled = false;
			}
			*/
		}

		public string desc(bool bare = false)
		{
			return (bare ? "" : descPrefix() + ":") + part.desc(true);
		}

		public abstract string descPrefix();

		protected void evlog(string name, Vessel v, bool care)
		{
			evlog(name + "(" + v.desc() + ")", care);
		}

		protected void evlog(string name, Part p, bool care)
		{
			evlog(name + "(" + p.desc() + ")", care);
		}

		protected void evlog(string name, GameEvents.FromToAction<Part, Part> action, bool care)
		{
			evlog(name + "(" + action.from.desc() + ", " + action.to.desc() + ")", care);
		}

		protected void evlog(string name, GameEvents.FromToAction<ModuleDockingNode, ModuleDockingNode> action, bool care)
		{
			evlog(name + "(" + action.from.part.desc() + ", " + action.to.part.desc() + ")", care);
		}

		protected void evlog(string name, uint id1, uint id2, bool care)
		{
			evlog(name + "(" + id1 + ", " + id2 + ")", care);
		}

		protected void evlog(string name, bool care)
		{
			if (verboseEvents)
				log(desc(), ": *** EVENT *** " + name + ", " + (care ? "care" : "don't care"));
		}

		protected static bool log(string msg1, string msg2 = "")
		{
			return Extensions.log(msg1, msg2);
		}
	}
}

