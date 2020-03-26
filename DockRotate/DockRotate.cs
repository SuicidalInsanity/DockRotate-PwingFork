using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using KSP.Localization;
using CompoundParts;

namespace DockRotate
{
	public abstract class ModuleBaseRotate: PartModule,
		IJointLockState, IStructureChangeListener, IResourceConsumer
	{
		const string GROUP = "DockRotate";
		const string GROUPNAME = "#DCKROT_rotation";
		const string DEBUGGROUP = "DockRotateDebug";
#if DEBUG
		const bool DEBUGMODE = true;
#else
		const bool DEBUGMODE = false;
#endif

		[KSPField(isPersistant = true)]
		public int Revision = -1;

		private static int _revision = -1;
		private static int getRevision()
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

		[UI_Toggle]
		[KSPField(
			groupName = GROUP,
			groupDisplayName = GROUPNAME,
			groupStartCollapsed = true,
			guiName = "#DCKROT_rotation",
			guiActive = true,
			guiActiveEditor = true,
			isPersistant = true
		)]
		public bool rotationEnabled = false;

		[UI_FloatEdit(
			incrementSlide = 0.5f, incrementSmall = 5f, incrementLarge = 30f,
			sigFigs = 1, unit = "\u00b0",
			minValue = 0f, maxValue = 360f
		)]
		[KSPField(
			groupName = GROUP,
			groupDisplayName = GROUPNAME,
			groupStartCollapsed = true,
			guiActive = true,
			guiActiveEditor = true,
			isPersistant = true,
			guiName = "#DCKROT_rotation_step",
			guiUnits = "\u00b0"
		)]
		public float rotationStep = 15f;

		[UI_FloatEdit(
			incrementSlide = 1f, incrementSmall = 15f, incrementLarge = 180f,
			sigFigs = 0, unit = "\u00b0/s",
			minValue = 1, maxValue = 8f * 360f
		)]
		[KSPField(
			groupName = GROUP,
			groupDisplayName = GROUPNAME,
			groupStartCollapsed = true,
			guiActive = true,
			guiActiveEditor = true,
			isPersistant = true,
			guiName = "#DCKROT_rotation_speed",
			guiUnits = "\u00b0/s"
		)]
		public float rotationSpeed = 5f;

		[UI_Toggle(affectSymCounterparts = UI_Scene.None)]
		[KSPField(
			groupName = GROUP,
			groupDisplayName = GROUPNAME,
			groupStartCollapsed = true,
			guiActive = true,
			guiActiveEditor = true,
			isPersistant = true,
			advancedTweakable = true,
			guiName = "#DCKROT_reverse_rotation"
		)]
		public bool reverseRotation = false;

		[UI_Toggle]
		[KSPField(
			groupName = GROUP,
			groupDisplayName = GROUPNAME,
			groupStartCollapsed = true,
			guiActive = true,
			guiActiveEditor = true,
			isPersistant = true,
			advancedTweakable = true,
			guiName = "#DCKROT_flip_flop_mode"
		)]
		public bool flipFlopMode = false;

		[KSPField(isPersistant = true)]
		public string soundClip = "DockRotate/DockRotateMotor";

		[KSPField(isPersistant = true)]
		public float soundVolume = 0.5f;

		[KSPField(isPersistant = true)]
		public float soundPitch = 1f;

		[UI_Toggle]
		[KSPField(
			groupName = DEBUGGROUP,
			groupDisplayName = DEBUGGROUP,
			guiActive = true,
			guiActiveEditor = true,
			isPersistant = true,
			advancedTweakable = true,
			guiName = "#DCKROT_smart_autostruts"
		)]
		public bool smartAutoStruts = false;

		[KSPField(
			guiActive = DEBUGMODE,
			groupName = DEBUGGROUP,
			groupDisplayName = DEBUGGROUP,
			groupStartCollapsed = true
		)]
		public float anglePosition;

		[KSPField(
			guiActive = DEBUGMODE,
			groupName = DEBUGGROUP,
			groupDisplayName = DEBUGGROUP,
			groupStartCollapsed = true
		)]
		public float angleVelocity;

		[KSPField(
			guiActive = DEBUGMODE,
			groupName = DEBUGGROUP,
			groupDisplayName = DEBUGGROUP,
			groupStartCollapsed = true
		)]
		public bool angleIsMoving;

		[KSPField(
			guiName = "#DCKROT_angle",
			groupName = GROUP,
			groupDisplayName = GROUPNAME,
			groupStartCollapsed = true,
			guiActive = true,
			guiActiveEditor = false
		)]
		public string angleInfo;

#if DEBUG
		[KSPField(
			guiName = "#DCKROT_status",
			guiActive = true,
			guiActiveEditor = false,
			groupName = DEBUGGROUP,
			groupDisplayName = DEBUGGROUP,
			groupStartCollapsed = true
		)]
		public string nodeStatus = "";
#endif

#if DEBUG
		[UI_Toggle]
		[KSPField(
			guiName = "Verbose Events",
			guiActive = true,
			guiActiveEditor = true,
			isPersistant = true,
			groupName = DEBUGGROUP,
			groupDisplayName = DEBUGGROUP,
			groupStartCollapsed = true
		)]
#endif
		public bool verboseEvents = false;
		public bool verboseEventsPrev = false;
		public bool wantsVerboseEvents() { return verboseEvents; }

		[KSPAction(
			guiName = "#DCKROT_enable_rotation",
			requireFullControl = true
		)]
		public void EnableRotation(KSPActionParam param)
		{
			if (verboseEvents)
				log(desc(), ": action " + param.desc());
			rotationEnabled = true;
		}

		[KSPAction(
			guiName = "#DCKROT_disable_rotation",
			requireFullControl = true
		)]
		public void DisableRotation(KSPActionParam param)
		{
			if (verboseEvents)
				log(desc(), ": action " + param.desc());
			rotationEnabled = false;
		}

		[KSPAction(
			guiName = "#DCKROT_toggle_rotation",
			requireFullControl = true
		)]
		public void ToggleRotation(KSPActionParam param)
		{
			if (verboseEvents)
				log(desc(), ": action " + param.desc());
			rotationEnabled = !rotationEnabled;
		}

		[KSPAction(
			guiName = "#DCKROT_rotate_clockwise",
			requireFullControl = true
		)]
		public void RotateClockwise(KSPActionParam param)
		{
			if (verboseEvents)
				log(desc(), ": action " + param.desc());
			if (reverseActionRotationKey()) {
				doRotateCounterclockwise();
			} else {
				doRotateClockwise();
			}
		}

		[KSPEvent(
			guiName = "#DCKROT_rotate_clockwise",
			groupName = GROUP,
			groupDisplayName = GROUP,
			groupStartCollapsed = true,
			guiActive = false,
			guiActiveEditor = false,
			requireFullControl = true
		)]
		public void RotateClockwise()
		{
			doRotateClockwise();
		}

		[KSPAction(
			guiName = "#DCKROT_rotate_counterclockwise",
			requireFullControl = true
		)]
		public void RotateCounterclockwise(KSPActionParam param)
		{
			if (verboseEvents)
				log(desc(), ": action " + param.desc());
			if (reverseActionRotationKey()) {
				doRotateClockwise();
			} else {
				doRotateCounterclockwise();
			}
		}

		[KSPEvent(
			guiName = "#DCKROT_rotate_counterclockwise",
			groupName = GROUP,
			groupDisplayName = GROUP,
			groupStartCollapsed = true,
			guiActive = false,
			guiActiveEditor = false,
			requireFullControl = true
		)]
		public void RotateCounterclockwise()
		{
			doRotateCounterclockwise();
		}

		[KSPAction(
			guiName = "#DCKROT_rotate_to_snap",
			requireFullControl = true
		)]
		public void RotateToSnap(KSPActionParam param)
		{
			if (verboseEvents)
				log(desc(), ": action " + param.desc());
			doRotateToSnap();
		}

		[KSPEvent(
			guiName = "#DCKROT_rotate_to_snap",
			groupName = GROUP,
			groupDisplayName = GROUP,
			groupStartCollapsed = true,
			guiActive = false,
			guiActiveEditor = false,
			requireFullControl = true
		)]
		public void RotateToSnap()
		{
			doRotateToSnap();
		}

		[KSPAction(
			guiName = "#DCKROT_stop_rotation",
			requireFullControl = true
		)]
		public void StopRotation(KSPActionParam param)
		{
			if (verboseEvents)
				log(desc(), ": action " + param.desc());
			doStopRotation();
		}

		[KSPEvent(
			guiName = "#DCKROT_stop_rotation",
			groupName = GROUP,
			groupDisplayName = GROUP,
			groupStartCollapsed = true,
			guiActive = false,
			guiActiveEditor = false,
			requireFullControl = true
		)]
		public void StopRotation()
		{
			doStopRotation();
		}

#if DEBUG
		[UI_Toggle]
#endif
		[KSPField(
			guiName = "autoSnap",
			isPersistant = true,
			guiActive = DEBUGMODE,
			guiActiveEditor = DEBUGMODE,
			groupName = DEBUGGROUP,
			groupDisplayName = DEBUGGROUP,
			groupStartCollapsed = true
		)]
		public bool autoSnap = false;

#if DEBUG
		[UI_Toggle]
#endif
		[KSPField(
			guiName = "hideCommands",
			isPersistant = true,
			guiActive = DEBUGMODE,
			guiActiveEditor = DEBUGMODE,
			groupName = DEBUGGROUP,
			groupDisplayName = DEBUGGROUP,
			groupStartCollapsed = true
		)]
		public bool hideCommands = false;

#if DEBUG
		[KSPEvent(
			guiActive = true,
			groupName = DEBUGGROUP,
			groupDisplayName = DEBUGGROUP,
			groupStartCollapsed = true
		)]
		public void DumpToLog()
		{
			log(desc(), ": BEGIN DUMP");

			AttachNode[] nodes = part.allAttachNodes();
			string nodeHelp = ": available nodes:";
			for (int i = 0; i < nodes.Length; i++)
				if (nodes[i] != null)
					nodeHelp += " \"" + nodes[i].id + "\"";
			log(desc(), nodeHelp);

			if (hasJointMotion && jointMotion.joint)
				jointMotion.joint.dump();
			else
				log(desc(), ": no jointMotion");

			log(desc(), ": END DUMP");
		}
#endif

		[KSPEvent(
			guiName = "Toggle Autostrut Display",
			guiActive = true,
			guiActiveEditor = true,
			groupName = DEBUGGROUP,
			groupDisplayName = DEBUGGROUP,
			groupStartCollapsed = true
		)]
		public void ToggleAutoStrutDisplay()
		{
			PhysicsGlobals.AutoStrutDisplay = !PhysicsGlobals.AutoStrutDisplay;
			if (HighLogic.LoadedSceneIsEditor)
				GameEvents.onEditorPartEvent.Fire(ConstructionEventType.PartTweaked, part);
		}

		public void doRotateClockwise()
		{
			if (!canStartRotation())
				return;
			if (!enqueueRotation(step(), speed()))
				return;
			if (flipFlopMode)
				reverseRotation = !reverseRotation;
		}

		public void doRotateCounterclockwise()
		{
			if (!canStartRotation())
				return;
			if (!enqueueRotation(-step(), speed()))
				return;
			if (flipFlopMode)
				reverseRotation = !reverseRotation;
		}

		public void doRotateToSnap()
		{
			if (!canStartRotation())
				return;
			enqueueRotationToSnap(rotationStep, speed());
		}

		public void doStopRotation()
		{
			JointMotionObj cr = currentRotation();
			if (cr)
				cr.brake();
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

		private static List<PartResourceDefinition> GetConsumedResourcesCache = null;

		public List<PartResourceDefinition> GetConsumedResources()
		{
			// log(desc(), ".GetConsumedResource() called");
			if (GetConsumedResourcesCache == null) {
				GetConsumedResourcesCache = new List<PartResourceDefinition>();
				PartResourceDefinition ec = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");
				if (ec != null)
					GetConsumedResourcesCache.Add(ec);
			}
			return GetConsumedResourcesCache;
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

		private ModuleBaseRotate[] moversToRoot;

		private void fillMoversToRoot()
		{
			List<ModuleBaseRotate> rtr = new List<ModuleBaseRotate>();
			for (Part p = part.parent; p; p = p.parent) {
				List<ModuleBaseRotate> mbr = p.FindModulesImplementing<ModuleBaseRotate>();
				rtr.AddRange(mbr);
			}
			moversToRoot = rtr.ToArray();
		}

		private CModuleStrut[] crossStruts;

		private void fillCrossStruts()
		{
			List<CModuleStrut> allStruts = vessel.FindPartModulesImplementing<CModuleStrut>();
			if (allStruts == null)
				return;
			PartSet rotParts = PartSet.allPartsFromHere(part);
			List<CModuleStrut> justCrossStruts = new List<CModuleStrut>();
			for (int i = 0; i < allStruts.Count; i++) {
				PartJoint sj = allStruts[i] ? allStruts[i].strutJoint : null;
				if (sj && sj.Host && sj.Target && rotParts.contains(sj.Host) != rotParts.contains(sj.Target))
					justCrossStruts.Add(allStruts[i]);
			}
			crossStruts = justCrossStruts.ToArray();
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

		protected virtual void doSetup()
		{
			jointMotion = null;
			hasJointMotion = false;
			nodeRole = "None";
			anglePosition = rotationAngle();
			angleVelocity = 0f;
			angleIsMoving = false;
			enabled = false;

			if (!part || !vessel || !setupLocalAxisDone) {
				log("" + GetType(), ": *** WARNING *** doSetup() called at a bad time");
				return;
			}

			if (!part.hasPhysics()) {
				log(desc(), ".doSetup(): physicsless part, disabled");
				return;
			}

			try {
				fillMoversToRoot();
				fillCrossStruts();
				setupGuiActive();
				PartJoint rotatingJoint = findMovingJoint(verboseEvents);
				if (rotatingJoint) {
					jointMotion = JointMotion.get(rotatingJoint);
					hasJointMotion = jointMotion;
					if (!jointMotion.hasController())
						jointMotion.controller = this;
					jointMotion.updateOrgRot();
				}
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

			setupDoneAt = Time.frameCount;

			enabled = hasJointMotion;
		}

		public void OnVesselGoOnRails()
		{
			if (verboseEvents)
				log(desc(), ".OnVesselGoOnRails()");
			freezeCurrentRotation("go on rails", false);
			setupDoneAt = 0;
		}

		public void OnVesselGoOffRails()
		{
			if (verboseEvents)
				log(desc(), ".OnVesselGoOffRails()");
			setupDoneAt = 0;
			// start speed always 0 when going off rails
			frozenStartSpeed = 0f;
			doSetup();
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
			if (rotationEnabled && !float.IsNaN(angle)) {
				angleInfo = String.Format("{0:+0.00;-0.00;0.00}\u00b0", angle);
			} else {
				angleInfo = "";
			}

			checkGuiActive();
		}

		public void RightBeforeStructureChange()
		{
			if (verboseEvents)
				log(desc(), ".RightBeforeStructureChange()");
			freezeCurrentRotation("structure change", true);
		}

		public void RightAfterStructureChange()
		{
			if (verboseEvents)
				log(desc(), ".RightAfterStructureChange()");
			doSetup();
		}

		public override void OnAwake()
		{
			verboseEventsPrev = verboseEvents;
			setupDoneAt = 0;

			base.OnAwake();
		}

		public virtual void OnDestroy()
		{
			setEvents(false);
		}

		private bool eventState = false;

		private void setEvents(bool cmd)
		{
			if (cmd == eventState) {
				if (verboseEvents)
					log(desc(), ".setEvents(" + cmd + ") repeated");
				return;
			}

			if (verboseEvents)
				log(desc(), ".setEvents(" + cmd + ")");

			if (cmd) {
				GameEvents.onEditorShipModified.Add(RightAfterEditorChange_ShipModified);
				GameEvents.onEditorPartEvent.Add(RightAfterEditorChange_Event);
			} else {
				GameEvents.onEditorShipModified.Remove(RightAfterEditorChange_ShipModified);
				GameEvents.onEditorPartEvent.Remove(RightAfterEditorChange_Event);
			}

			eventState = cmd;
		}

		private static readonly string[,] guiList = {
			// flags:
			// S: is a setting
			// C: is a command
			// D: is a debug display
			{ "nodeRole", "S" },
			{ "rotationStep", "S" },
			{ "rotationSpeed", "S" },
			{ "reverseRotation", "S" },
			{ "flipFlopMode", "S" },
			{ "smartAutoStruts", "S" },
			{ "RotateClockwise", "C" },
			{ "RotateCounterclockwise", "C" },
			{ "RotateToSnap", "C" },
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

		private BaseEvent StopRotationEvent;
		private BaseField angleInfoField;

		[KSPField(guiActive = false, guiActiveEditor = false, isPersistant = true)]
		public bool showToggleAutoStrutDisplay = false;

		private BaseEvent ToggleAutoStrutDisplayEvent;

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

			StopRotationEvent = Events["StopRotation"];
			angleInfoField = Fields["angleInfo"];
			ToggleAutoStrutDisplayEvent = Events["ToggleAutoStrutDisplay"];
			if (ToggleAutoStrutDisplayEvent != null)
				ToggleAutoStrutDisplayEvent.guiActive = ToggleAutoStrutDisplayEvent.guiActiveEditor
					= (DEBUGMODE || showToggleAutoStrutDisplay);
#if !DEBUG
			autoSnap = false;
#endif
		}

		private void checkGuiActive()
		{
			if (guiInfo != null) {
				bool csr = canStartRotation();
				for (int i = 0; i < guiInfo.Length; i++) {
					ref GuiInfo ii = ref guiInfo[i];
					bool flagsCheck = !(hideCommands && ii.flags.IndexOf('C') >= 0)
						&& (DEBUGMODE || ii.flags.IndexOf('D') < 0);
					if (ii.fld != null)
						ii.fld.guiActive = ii.fld.guiActiveEditor = rotationEnabled && flagsCheck;
					if (ii.evt != null)
						ii.evt.guiActive = ii.evt.guiActiveEditor = csr && flagsCheck;
				}
			}

			if (angleInfoField != null)
				angleInfoField.guiActive = angleInfoField.guiActiveEditor = rotationEnabled && angleInfo != "";

			if (StopRotationEvent != null)
				StopRotationEvent.guiActive = currentRotation();

			if (ToggleAutoStrutDisplayEvent != null)
				ToggleAutoStrutDisplayEvent.guiName = PhysicsGlobals.AutoStrutDisplay ?
					"Hide Autostruts" : "Show Autostruts";
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			checkRevision();

			setupLocalAxisDone = setupLocalAxis(state);

			setupGuiActive();

			if (state == StartState.Editor) {
				if (verboseEvents)
					log(desc(), ".OnStart(" + state + ")");
				setEvents(true);
				RightAfterEditorChange("START");
				return;
			}

			if (vessel) {
				VesselMotionManager.get(vessel); // force creation of VesselMotionManager
			} else if (state != StartState.Editor) {
				log(desc(), ".OnStart(" + state + ") with no vessel");
			}

			checkGuiActive();
		}

		public override void OnUpdate()
		{
			base.OnUpdate();

			if (verboseEvents != verboseEventsPrev) {
				VesselMotionManager.get(vessel).listeners();
				verboseEventsPrev = verboseEvents;
			}

			JointMotionObj cr = currentRotation();

			anglePosition = rotationAngle();
			angleVelocity = cr ? cr.vel : 0f;
			angleIsMoving = cr;

			if (MapView.MapIsEnabled || !part.PartActionWindow)
				return;

			bool updfrm = (Time.frameCount & 3) == 0;
			if (updfrm || cr)
				updateStatus(cr);
			if (updfrm)
				checkGuiActive();
		}

		private void updateStatus(JointMotionObj cr)
		{
			if (cr) {
				angleInfo = String.Format("{0:+0.00;-0.00;0.00}\u00b0 ({1:+0.00;-0.00;0.00}\u00b0/s){2}",
					anglePosition, cr.vel, (jointMotion.controller == this ? " CTL" : ""));
			} else {
				if (float.IsNaN(anglePosition)) {
					angleInfo = "";
				} else {
					angleInfo = String.Format(
						"{0:+0.00;-0.00;0.00}\u00b0 ({1:+0.0000;-0.0000;0.0000}\u00b0\u0394)",
						anglePosition, dynamicDeltaAngle());
				}
			}
#if DEBUG
			int nJoints = hasJointMotion ? jointMotion.joint.joints.Count : 0;
			nodeStatus = part.flightID + ":" + nodeRole + "[" + nJoints + "]";
			if (frozenFlag)
				nodeStatus += " [F]";
			if (cr)
				nodeStatus += " " + cr.pos + "\u00b0 -> " + cr.tgt + "\u00b0";
#endif
		}

		protected bool canStartRotation()
		{
			if (HighLogic.LoadedSceneIsEditor)
				return rotationEnabled && findHostPartInEditor(verboseEvents);

			return rotationEnabled
				&& setupDone && hasJointMotion
				&& vessel && vessel.CurrentControlLevel == Vessel.ControlLevel.FULL;
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
			Part other;
			AttachNode node = findMovingNodeInEditor(out other, verbose);
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
			return enqueueRotation(frozen[0], frozen[1], frozen[2]);
		}

		protected bool enqueueRotation(float angle, float speed, float startSpeed = 0f)
		{
			if (HighLogic.LoadedSceneIsEditor) {
				log(desc(), ".enqueueRotation(): " + angle + "\u00b0 in editor");

				Part host = findHostPartInEditor(false);
				if (!host || !host.parent)
					return false;

				Vector3 axis = host == part ? -partNodeAxis : partNodeAxis;
				axis = axis.Td(part.T(), null);
				Vector3 pos = partNodePos.Tp(part.T(), null);
				Quaternion rot = axis.rotation(angle);

				Transform t = host.transform;
				t.SetPositionAndRotation(rot * (t.position - pos) + pos,
					rot * t.rotation);

				GameEvents.onEditorPartEvent.Fire(ConstructionEventType.PartRotated, host);
				return true;
			}

			if (!hasJointMotion) {
				log(desc(), ".enqueueRotation(): no rotating joint, skipped");
				return false;
			}
			enabled = true;
			return jointMotion.enqueueRotation(this, angle, speed, startSpeed);
		}

		protected void enqueueRotationToSnap(float snap, float speed)
		{
			if (snap < 0.1f)
				snap = 15f;

			if (HighLogic.LoadedSceneIsEditor) {
				float a = rotationAngle();
				if (float.IsNaN(a))
					return;
				float f = snap * Mathf.Floor(a / snap + 0.5f);
				enqueueRotation(f - a, speed);
				return;
			}

			if (!hasJointMotion)
				return;
			enqueueRotation(jointMotion.angleToSnap(snap), speed);
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
			if (setupDone && HighLogic.LoadedSceneIsFlight)
				checkFrozenRotation();

			if (lastUsefulFixedUpdate < setupDoneAt) {
				lastUsefulFixedUpdate = setupDoneAt;
			} else if (frozenFlag || currentRotation() != null) {
				lastUsefulFixedUpdate = Time.frameCount;
			} else if (Time.frameCount - lastUsefulFixedUpdate > 10) {
				log(part.desc(), ": disabling useless MonoBehaviour updates");
				enabled = false;
			}
		}

		public string desc(bool bare = false)
		{
			return (bare ? "" : descPrefix() + ":") + part.desc(true);
		}

		public abstract string descPrefix();

		protected static bool log(string msg1, string msg2 = "")
		{
			return Extensions.log(msg1, msg2);
		}
	}

	public class ModuleNodeRotate: ModuleBaseRotate
	{
		[KSPField(isPersistant = true)]
		public string rotatingNodeName = "";

		[KSPField(isPersistant = true)]
		public uint otherPartFlightID = 0;

		public AttachNode rotatingNode;

		private bool isSrfAttach()
		{
			return rotatingNodeName == "srfAttach";
		}

		protected override void fillInfo()
		{
			storedModuleDisplayName = Localizer.Format("#DCKROT_node_displayname");
			storedInfo = Localizer.Format("#DCKROT_node_info", rotatingNodeName);
		}

		protected override void doSetup()
		{
			base.doSetup();
			// TODO: change groupDisplayName to "NodeRotate <node name>"
		}

		protected override AttachNode findMovingNodeInEditor(out Part otherPart, bool verbose)
		{
			otherPart = null;
			if (rotatingNode == null)
				return null;
			if (verbose)
				log(desc(), ".findMovingNodeInEditor(): rotatingNode = " + rotatingNode.desc());
			otherPart = rotatingNode.attachedPart;
			if (verbose)
				log(desc(), ".findMovingNodeInEditor(): otherPart = " + otherPart.desc());
			if (!otherPart)
				return null;
			if (verbose)
				log(desc(), ".findMovingNodeInEditor(): attachedPart = " + rotatingNode.attachedPart.desc());
			return rotatingNode;
		}

		protected override bool setupLocalAxis(StartState state)
		{
			rotatingNode = part.FindAttachNode(rotatingNodeName);
			if (rotatingNode == null && isSrfAttach())
				rotatingNode = part.srfAttachNode;

			if (rotatingNode == null) {
				log(desc(), ".setupLocalAxis(" + state + "): "
					+ "no node \"" + rotatingNodeName + "\"");

				AttachNode[] nodes = part.allAttachNodes();
				for (int i = 0; i < nodes.Length; i++)
					log(desc(), ": node[" + i + "] = " + nodes[i].desc());
				return false;
			}

			partNodePos = rotatingNode.position;
			partNodeAxis = rotatingNode.orientation;
			if (verboseEvents)
				log(desc(), ".setupLocalAxis(" + state + ") done: "
					+ partNodeAxis + "@" + partNodePos);
			return true;
		}

		protected override PartJoint findMovingJoint(bool verbose)
		{
			uint prevOtherPartFlightID = otherPartFlightID;
			otherPartFlightID = 0;

			if (rotatingNode == null || !rotatingNode.owner) {
				if (verbose)
					log(desc(), ".findMovingJoint(): no node");
				return null;
			}

			if (part.FindModuleImplementing<ModuleDockRotate>()) {
				log(desc(), ".findMovingJoint(): has DockRotate, NodeRotate disabled");
				return null;
			}

			Part owner = rotatingNode.owner;
			Part other = rotatingNode.attachedPart;
			if (!other) {
				if (verbose)
					log(desc(), ".findMovingJoint(" + rotatingNode.id + "): attachedPart is null, try by id = "
						+ prevOtherPartFlightID);
				other = findOtherById(prevOtherPartFlightID);
			}
			if (!other) {
				if (verbose)
					log(desc(), ".findMovingJoint(" + rotatingNode.id + "): no attachedPart");
				return null;
			}
			if (verbose && other.flightID != prevOtherPartFlightID)
				log(desc(), ".findMovingJoint(" + rotatingNode.id + "): otherFlightID "
					+ prevOtherPartFlightID + " -> " + other.flightID);
			if (verbose)
				log(desc(), ".findMovingJoint(" + rotatingNode.id + "): attachedPart is " + other.desc());
			other.forcePhysics();
			JointLockStateProxy.register(other, this);

			if (owner.parent == other) {
				PartJoint ret = owner.attachJoint;
				if (verbose)
					log(desc(), ".findMovingJoint(" + rotatingNode.id + "): child " + ret.desc());
				otherPartFlightID = other.flightID;
				return ret;
			}

			if (other.parent == owner) {
				PartJoint ret = other.attachJoint;
				if (verbose)
					log(desc(), ".findMovingJoint(" + rotatingNode.id + "): parent " + ret.desc());
				otherPartFlightID = other.flightID;
				return ret;
			}

			if (verbose)
				log(desc(), ".findMovingJoint(" + rotatingNode.id + "): nothing");
			return null;
		}

		private Part findOtherById(uint id)
		{
			if (id == 0)
				return null;
			if (part.parent && part.parent.flightID == id)
				return part.parent;
			if (part.children != null)
				for (int i = 0; i < part.children.Count; i++)
					if (part.children[i] && part.children[i].flightID == id)
						return part.children[i];
			return null;
		}

		public override string descPrefix()
		{
			return "MNR";
		}
	}

	public class ModuleDockRotate: ModuleBaseRotate
	{
		/*

			docking node states:

			* PreAttached
			* Docked (docker/same vessel/dockee) - (docker) and (same vessel) are coupled with (dockee)
			* Ready
			* Disengage
			* Acquire
			* Acquire (dockee)

		*/

		private ModuleDockingNode dockingNode;

		protected override void fillInfo()
		{
			storedModuleDisplayName = Localizer.Format("#DCKROT_port_displayname");
			storedInfo = Localizer.Format("#DCKROT_port_info");
		}

		protected override AttachNode findMovingNodeInEditor(out Part otherPart, bool verbose)
		{
			otherPart = null;
			if (!dockingNode || dockingNode.referenceNode == null)
				return null;
			if (verbose)
				log(desc(), ".findMovingNodeInEditor(): referenceNode = " + dockingNode.referenceNode.desc());
			AttachNode otherNode = dockingNode.referenceNode.findConnectedNode(verboseEvents);
			if (verbose)
				log(desc(), ".findMovingNodeInEditor(): otherNode = " + otherNode.desc());
			if (otherNode == null)
				return null;
			otherPart = otherNode.owner;
			if (verbose)
				log(desc(), ".findMovingNodeInEditor(): otherPart = " + otherPart.desc());
			if (!otherPart)
				return null;
			ModuleDockingNode otherDockingNode = otherPart.FindModuleImplementing<ModuleDockingNode>();
			if (verbose)
				log(desc(), ".findMovingNodeInEditor(): otherDockingNode = "
					+ (otherDockingNode ? otherDockingNode.part.desc() : "null"));
			if (!otherDockingNode)
				return null;
			if (verbose)
				log(desc(), ".findMovingNodeInEditor(): otherDockingNode.referenceNode = "
					+ otherDockingNode.referenceNode.desc());
			if (otherDockingNode.referenceNode == null)
				return null;
			if (!otherDockingNode.matchType(dockingNode)) {
				if (verbose)
					log(desc(), ".findMovingNodeInEditor(): mismatched node types "
						+ dockingNode.nodeType + " != " + otherDockingNode.nodeType);
				return null;
			}
			if (verbose)
				log(desc(), ".findMovingNodeInEditor(): node test is "
					+ (otherDockingNode.referenceNode.FindOpposingNode() == dockingNode.referenceNode));

			return dockingNode.referenceNode;
		}

		protected override bool setupLocalAxis(StartState state)
		{
			dockingNode = part.FindModuleImplementing<ModuleDockingNode>();

			if (!dockingNode) {
				log(desc(), ".setupLocalAxis(" + state + "): no docking node");
				return false;
			}

			partNodePos = Vector3.zero.Tp(dockingNode.T(), part.T());
			partNodeAxis = Vector3.forward.Td(dockingNode.T(), part.T());
			if (verboseEvents)
				log(desc(), ".setupLocalAxis(" + state + ") done: "
					+ partNodeAxis + "@" + partNodePos);
			return true;
		}

		protected override PartJoint findMovingJoint(bool verbose)
		{
			if (!dockingNode || !dockingNode.part) {
				if (verbose)
					log(desc(), ".findMovingJoint(): no docking node");
				return null;
			}

			ModuleDockingNode other = dockingNode.otherNode;
			if (other) {
				if (verbose)
					log(desc(), ".findMovingJoint(): other is " + other.part.desc());
			} else if (dockingNode.dockedPartUId > 0) {
				other = dockingNode.FindOtherNode();
				if (verbose && other)
					log(desc(), ".findMovingJoint(): other found " + other.part.desc());
			}

			if (!other || !other.part) {
				if (verbose)
					log(desc(), ".findMovingJoint(): no other, id = " + dockingNode.dockedPartUId);
				return null;
			}

			if (!dockingNode.matchType(other)) {
				if (verbose)
					log(desc(), ".findMovingJoint(): mismatched node types");
				return null;
			}

			if (verbose && dockingNode.state != "PreAttached"
				&& !dockingNode.state.StartsWith("Docked", StringComparison.InvariantCulture))
				log(desc(), ".findMovingJoint(): unconnected state \"" + dockingNode.state + "\"");

			ModuleBaseRotate otherModule = other.part.FindModuleImplementing<ModuleBaseRotate>();
			if (otherModule) {
				if (!smartAutoStruts && otherModule.smartAutoStruts) {
					smartAutoStruts = true;
					log(desc(), ".findMovingJoint(): smartAutoStruts activated by " + otherModule.desc());
				}
			}

			PartJoint ret = dockingNode.sameVesselDockJoint;
			if (ret && ret.Target == other.part) {
				if (verbose)
					log(desc(), ".findMovingJoint(): to same vessel " + ret.desc());
				return ret;
			}

			ret = other.sameVesselDockJoint;
			if (ret && ret.Target == dockingNode.part) {
				if (verbose)
					log(desc(), ".findMovingJoint(): from same vessel " + ret.desc());
				return ret;
			}

			if (dockingNode.part.parent == other.part) {
				ret = dockingNode.part.attachJoint;
				if (verbose)
					log(desc(), ".findMovingJoint(): to parent " + ret.desc());
				return ret;
			}

			for (int i = 0; i < dockingNode.part.children.Count; i++) {
				Part child = dockingNode.part.children[i];
				if (child == other.part) {
					ret = child.attachJoint;
					if (verbose)
						log(desc(), ".findMovingJoint(): to child " + ret.desc());
					return ret;
				}
			}

			if (verbose)
				log(desc(), ".findMovingJoint(): nothing");
			return null;
		}

		protected override void doSetup()
		{
			base.doSetup();

			if (hasJointMotion && jointMotion.joint.Host == part && !frozenFlag) {
				float snap = autoSnapStep();
				if (verboseEvents)
					log(desc(), ".autoSnapStep() = " + snap);
				ModuleDockRotate other = jointMotion.joint.Target.FindModuleImplementing<ModuleDockRotate>();
				if (other) {
					float otherSnap = other.autoSnapStep();
					if (verboseEvents)
						log(other.desc(), ".autoSnapStep() = " + otherSnap);
					if (otherSnap > 0f && (snap.isZero() || otherSnap < snap))
						snap = otherSnap;
				}
				if (!snap.isZero()) {
					if (verboseEvents)
						log(jointMotion.desc(), ": autosnap at " + snap);
					enqueueFrozenRotation(jointMotion.angleToSnap(snap), 5f);
				}
			}
		}

		private float autoSnapStep()
		{
			if (!hasJointMotion || !dockingNode)
				return 0f;

			float step = 0f;
			string source = "no source";
			if (dockingNode.snapRotation && dockingNode.snapOffset > 0.01f) {
				step = dockingNode.snapOffset;
				source = "snapOffset";
			} else if (autoSnap && rotationEnabled && rotationStep > 0.01f) {
				step = rotationStep;
				source = "rotationStep";
			}
			if (verboseEvents)
				log(desc(), ".autoSnapStep() = " + step + " from " + source);
			if (step >= 360f)
				step = 0f;
			return step;
		}

		public override string descPrefix()
		{
			return "MDR";
		}
	}
}

