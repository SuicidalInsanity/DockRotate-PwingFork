using System;
using System.Collections.Generic;
using UnityEngine;
using KSP.UI.Screens;
using KSP.Localization;

namespace DockRotate
{
	public abstract class ModuleBaseRotate: PartModule, IJointLockState, IStructureChangeListener
	{
		[UI_Toggle()]
		[KSPField(
			guiName = "#DCKROT_rotation",
			guiActive = true,
			guiActiveEditor = true,
			isPersistant = true
		)]
		public bool rotationEnabled = false;

		[UI_FloatEdit(
			minValue = 0f, maxValue = 360f,
			incrementSlide = 0.5f, incrementSmall = 5f, incrementLarge = 30f,
			sigFigs = 1, unit = "\u00b0"
		)]
		[KSPField(
			guiActive = true,
			guiActiveEditor = true,
			isPersistant = true,
			guiName = "#DCKROT_rotation_step",
			guiUnits = "\u00b0"
		)]
		public float rotationStep = 15f;

		[UI_FloatEdit(
			minValue = 1, maxValue = 8f * 360f,
			incrementSlide = 1f, incrementSmall = 15f, incrementLarge = 180f,
			sigFigs = 0, unit = "\u00b0/s"
		)]
		[KSPField(
			guiActive = true,
			guiActiveEditor = true,
			isPersistant = true,
			guiName = "#DCKROT_rotation_speed",
			guiUnits = "\u00b0/s"
		)]
		public float rotationSpeed = 5f;

		[UI_Toggle(affectSymCounterparts = UI_Scene.None)]
		[KSPField(
			guiActive = true,
			guiActiveEditor = true,
			isPersistant = true,
			advancedTweakable = true,
			guiName = "#DCKROT_reverse_rotation"
		)]
		public bool reverseRotation = false;

		[UI_Toggle()]
		[KSPField(
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

		[UI_Toggle()]
		[KSPField(
			guiActive = true,
			guiActiveEditor = true,
			isPersistant = true,
			advancedTweakable = true,
			guiName = "#DCKROT_smart_autostruts"
		)]
		public bool smartAutoStruts = false;

		[KSPField(
			guiName = "#DCKROT_angle",
			guiActive = true,
			guiActiveEditor = false
		)]
		public string angleInfo;

#if DEBUG
		[KSPField(
			guiName = "#DCKROT_status",
			guiActive = true,
			guiActiveEditor = false
		)]
		public string nodeStatus = "";
#endif

#if DEBUG
		[UI_Toggle()]
		[KSPField(
			guiName = "Verbose Events",
			guiActive = true,
			guiActiveEditor = true,
			isPersistant = true
		)]
#endif
		public bool verboseEvents = false;
		public bool verboseEventsPrev = false;
		public bool wantsVerboseEvents() { return verboseEvents; }

		[KSPAction(
			guiName = "#DCKROT_stop_rotation",
			requireFullControl = true
		)]
		public void StopRotation(KSPActionParam param)
		{
			doStopRotation();
		}

		[KSPEvent(
			guiName = "#DCKROT_stop_rotation",
			guiActive = false,
			guiActiveEditor = false,
			requireFullControl = true
		)]
		public void StopRotation()
		{
			doStopRotation();
		}

		[KSPAction(
			guiName = "#DCKROT_rotate_clockwise",
			requireFullControl = true
		)]
		public void RotateClockwise(KSPActionParam param)
		{
			if (reverseActionRotationKey()) {
				doRotateCounterclockwise();
			} else {
				doRotateClockwise();
			}
		}

		[KSPEvent(
			guiName = "#DCKROT_rotate_clockwise",
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
			if (reverseActionRotationKey()) {
				doRotateClockwise();
			} else {
				doRotateCounterclockwise();
			}
		}

		[KSPEvent(
			guiName = "#DCKROT_rotate_counterclockwise",
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
			doRotateToSnap();
		}

		[KSPEvent(
			guiName = "#DCKROT_rotate_to_snap",
			guiActive = false,
			guiActiveEditor = false,
			requireFullControl = true
		)]
		public void RotateToSnap()
		{
			doRotateToSnap();
		}

#if DEBUG
		[KSPEvent(
			guiName = "Toggle Autostrut Display",
			guiActive = true,
			guiActiveEditor = false
		)]
		public void ToggleAutoStrutDisplay()
		{
			PhysicsGlobals.AutoStrutDisplay = !PhysicsGlobals.AutoStrutDisplay;
		}
#endif

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
			JointMotionObj r = currentRotation();
			if (r)
				r.brake();
		}

		protected bool reverseActionRotationKey()
		{
			return GameSettings.MODIFIER_KEY.GetKey();
		}

		public bool IsJointUnlocked()
		{
			bool ret = currentRotation();
			// log(desc(), ".IsJointUnlocked() is " + ret);
			return ret;
		}

		protected JointMotion jointMotion;

		public string nodeRole = "Init";

		protected Vector3 partNodePos; // node position, relative to part
		protected Vector3 partNodeAxis; // node rotation axis, relative to part
		protected bool geometryOk;
		protected abstract bool setupLocalAxis(StartState state);
		protected abstract AttachNode referenceNode();

		// localized info cache
		protected string cached_moduleDisplayName = "";
		protected string cached_info = "";

		[KSPField(isPersistant = true)]
		public Vector3 frozenRotation = Vector3.zero;

		private bool frozenFlag {
			get => !Mathf.Approximately(frozenAngle, 0f);
		}
		private float frozenAngle {
			get => frozenRotation[0];
			set => frozenRotation[0] = value;
		}

		private float frozenSpeed {
			get => frozenRotation[1];
			set => frozenRotation[1] = value;
		}

		private float frozenStartSpeed {
			get => frozenRotation[2];
			set => frozenRotation[2] = value;
		}

		[KSPField(isPersistant = true)]
		public float electricityRate = 1f;

		public Part getPart()
		{
			return part;
		}

		protected bool setupDone = false;
		protected abstract void setup();

		private void doSetup()
		{
			if (!part || !vessel || !geometryOk) {
				log("" + GetType(), ": *** WARNING *** doSetup() called at a bad time");
				return;
			}

			try {
				setupGuiActive();
				setup();
			} catch (Exception e) {
				string sep = new string('-', 80);
				log(sep);
				log("Exception during setup:\n" + e.StackTrace);
				log(sep);
			}

			log(desc(), ".doSetup(): joint " + (jointMotion ? jointMotion.joint.desc() : "null"));

			setupDone = true;
		}

		public void OnVesselGoOnRails()
		{
			if (verboseEvents)
				log(desc(), ".OnVesselGoOnRails()");
			freezeCurrentRotation("go on rails", false);
			setupDone = false;
		}

		public void OnVesselGoOffRails()
		{
			if (verboseEvents)
				log(desc(), ".OnVesselGoOffRails()");
			setupDone = false;
			// start speed always 0 when going off rails
			frozenStartSpeed = 0f;
			doSetup();
		}

		public void RightAfterEditorEvent(ConstructionEventType type, Part part)
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

			BaseField f = Fields["angleInfo"];
			if (f == null) {
				log(desc(), ".RightAfterEditorChange(): no angleInfo");
				return;
			}
			f.guiActiveEditor = false;

			AttachNode node = referenceNode();
			if (node == null) {
				log(desc(), ".RightAfterEditorChange(): no node");
				return;
			}

			Part other = node.attachedPart;
			if (!other)
				return;

			float angle = partNodeAxis.axisSignedAngle(part.up(partNodeAxis),
				other.up(partNodeAxis.Td(part.T(), other.T())).Td(other.T(), part.T()));

			angleInfo = String.Format("{0:+0.00;-0.00;0.00}\u00b0", angle);
			f.guiActiveEditor = true;
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
			setupDone = false;

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
				if (true || verboseEvents)
					log(desc(), ".setEvents(" + cmd + ") repeated");
				return;
			}

			if (true || verboseEvents)
				log(desc(), ".setEvents(" + cmd + ")");

			if (cmd) {
				GameEvents.onEditorPartEvent.Add(RightAfterEditorEvent);
			} else {
				GameEvents.onEditorPartEvent.Remove(RightAfterEditorEvent);
			}

			eventState = cmd;
		}

		protected static string[] guiList = {
			"nodeRole",
			"angleInfo",
			"rotationStep",
			"rotationSpeed",
			"reverseRotation",
			"RotateClockwise",
			"RotateCounterclockwise",
			"RotateToSnap"
		};

		private BaseField[] fld;
		private BaseEvent[] evt;

		protected void setupGuiActive()
		{
			fld = null;
			evt = null;

			List<BaseField> fl = new List<BaseField>();
			List<BaseEvent> el = new List<BaseEvent>();

			for (int i = 0; i < guiList.Length; i++) {
				string n = guiList[i];
				BaseField f = Fields[n];
				if (f != null)
					fl.Add(f);
				BaseEvent e = Events[n];
				if (e != null)
					el.Add(e);
			}

			fld = fl.ToArray();
			evt = el.ToArray();

			// log(desc(), ": " + fld.Length + " fields, " + evt.Length + " events");
		}

		private void checkGuiActive()
		{
			bool newGuiActive = FlightGlobals.ActiveVessel == vessel && canStartRotation();
			bool newGuiActiveEditor = rotationEnabled;

			if (fld != null) {
				for (int i = 0; i < fld.Length; i++) {
					if (fld[i] == null)
						continue;
					fld[i].guiActive = newGuiActive;
					fld[i].guiActiveEditor = newGuiActiveEditor;
				}
			}

			if (evt != null) {
				for (int i = 0; i < evt.Length; i++) {
					if (evt[i] == null)
						continue;
					evt[i].guiActive = newGuiActive;
					evt[i].guiActiveEditor = newGuiActiveEditor;
				}
			}
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			geometryOk = setupLocalAxis(state);

			setupGuiActive();

			if (state == StartState.Editor) {
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

			if (MapView.MapIsEnabled)
				return;

			bool guiActive = canStartRotation();
			JointMotionObj cr = currentRotation();

#if DEBUG
			int nJoints = jointMotion ? jointMotion.joint.joints.Count : 0;
			nodeStatus = part.flightID + ":" + nodeRole + "[" + nJoints + "]";
			if (frozenFlag)
				nodeStatus += " [F]";
			if (cr)
				nodeStatus += " " + cr.pos + "\u00b0 -> "+ cr.tgt + "\u00b0";
#endif

			if (cr) {
				angleInfo = String.Format("{0:+0.00;-0.00;0.00}\u00b0 ({1:+0.00;-0.00;0.00}\u00b0/s){2}",
					rotationAngle(true), cr.vel,
					(jointMotion.controller == this ? " CTL" : ""));
			} else {
				angleInfo = String.Format("{0:+0.00;-0.00;0.00}\u00b0 ({1:+0.0000;-0.0000;0.0000}\u00b0\u0394)",
					rotationAngle(false), dynamicDeltaAngle());
			}

			Events["StopRotation"].guiActive = cr;

			checkGuiActive();

#if DEBUG
			Events["ToggleAutoStrutDisplay"].guiName = PhysicsGlobals.AutoStrutDisplay ? "Hide Autostruts" : "Show Autostruts";
#endif
		}

		protected bool canStartRotation()
		{
			return rotationEnabled
				&& setupDone && jointMotion
				&& vessel && vessel.CurrentControlLevel == Vessel.ControlLevel.FULL;
		}

		public float step()
		{
			float s = rotationStep;
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

		protected float rotationAngle(bool dynamic)
		{
			return jointMotion ? jointMotion.rotationAngle(dynamic) : float.NaN;
		}

		protected float dynamicDeltaAngle()
		{
			return jointMotion ? jointMotion.dynamicDeltaAngle() : float.NaN;
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
			if (!jointMotion) {
				log(desc(), ".enqueueRotation(): no rotating joint, skipped");
				return false;
			}
			return jointMotion.enqueueRotation(this, angle, speed, startSpeed);
		}

		protected bool enqueueRotationToSnap(float snap, float speed)
		{
			if (!jointMotion)
				return false;
			if (snap < 0.1f)
				snap = 15f;
			return enqueueRotation(jointMotion.angleToSnap(snap), speed);
		}

		protected void freezeCurrentRotation(string msg, bool keepSpeed)
		{
			JointMotionObj r = currentRotation();
			if (!r)
				return;
			log(desc(), ".freezeCurrentRotation("
				+ msg + ", " + keepSpeed + ")");
			r.isContinuous();
			float angle = r.tgt - r.pos;
			enqueueFrozenRotation(angle, r.maxvel, keepSpeed ? r.vel : 0f);
			r.abort();
			log(desc(), ": removing rotation (2)");
			jointMotion.rotCur = null;
		}

		protected JointMotionObj currentRotation()
		{
			return jointMotion ? jointMotion.rotCur : null;
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

			JointMotionObj r = currentRotation();
			if (r && r.isContinuous() && jointMotion.controller == this) {
				frozenRotation.Set(r.tgt, r.maxvel, 0f);
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

		public void FixedUpdate()
		{
			if (!setupDone || HighLogic.LoadedScene != GameScenes.FLIGHT)
				return;
			if (verboseEvents != verboseEventsPrev) {
				VesselMotionManager.get(vessel).listeners();
				verboseEventsPrev = verboseEvents;
			}
			checkFrozenRotation();
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

		public AttachNode rotatingNode;

		public override string GetModuleDisplayName()
		{
			if (cached_moduleDisplayName == "")
				cached_moduleDisplayName = Localizer.Format("#DCKROT_node_displayname");
			return cached_moduleDisplayName;
		}

		public override string GetInfo()
		{
			if (cached_info == "")
				cached_info = Localizer.Format("#DCKROT_node_info", rotatingNodeName);
			return cached_info;
		}

		protected override AttachNode referenceNode()
		{
			return rotatingNode;
		}

		protected override bool setupLocalAxis(StartState state)
		{
			rotatingNode = part.FindAttachNode(rotatingNodeName);

			if (rotatingNode == null) {
				log(desc(), ".setupGeometry(" + state + "): "
					+ "no node \"" + rotatingNodeName + "\"");
				AttachNode[] nodes = part.FindAttachNodes("");
				string nodeHelp = desc() + " available nodes:";
				for (int i = 0; i < nodes.Length; i++)
					nodeHelp += " \"" + nodes[i].id + "\"";
				log(desc(), nodeHelp);
				return false;
			}

			partNodePos = rotatingNode.position;
			partNodeAxis = rotatingNode.orientation;
			if (verboseEvents)
				log(desc(), ".setupGeometry(" + state + ") done: "
					+ partNodeAxis + "@" + partNodePos);
			return true;
		}

		private PartJoint nodeJoint(AttachNode node, bool verbose)
		{
			if (node == null || !node.owner) {
				if (verbose)
					log(nameof(ModuleNodeRotate), ".nodeJoint(): no node");
				return null;
			}

			Part part = node.owner;
			Part other = node.attachedPart;
			if (!other) {
				if (verbose)
					log(node.owner.desc(), ".nodeJoint(" + node.id + "): no attachedPart");
				return null;
			}
			if (verbose)
				log(node.owner.desc(), ".nodeJoint(" + node.id + "): attachedPart is " + other.desc());

			if (part.parent == other) {
				PartJoint ret = part.attachJoint;
				if (verbose)
					log(node.owner.desc(), ".nodeJoint(" + node.id + "): child " + ret.desc());
				return ret;
			}

			if (other.parent == part) {
				PartJoint ret = other.attachJoint;
				if (verbose)
					log(node.owner.desc(), ".nodeJoint(" + node.id + "): parent " + ret.desc());
				return ret;
			}

			if (verbose)
				log(node.owner.desc(), ".nodeJoint(" + node.id + "): nothing");
			return null;
		}

		protected override void setup()
		{
			jointMotion = null;
			nodeRole = "None";

			if (part.FindModuleImplementing<ModuleDockRotate>()) {
				log(desc(), ": has DockRotate, NodeRotate disabled");
				return;
			}

			if (!part.hasPhysics()) {
				log(desc(), ": physicsless, NodeRotate disabled");
				return;
			}

			if (rotatingNode == null) {
				log(desc(), ".setup(): no rotatingNode");
				return;
			}

			Part other = rotatingNode.attachedPart;
			if (!other)
				return;

			other.forcePhysics();

			PartJoint rotatingJoint = nodeJoint(rotatingNode, verboseEvents);
			if (rotatingJoint) {
				nodeRole = part == rotatingJoint.Host ? "Host"
					: part == rotatingJoint.Target ? "Target"
					: "Unknown";
				if (verboseEvents)
					log(desc(), ".setup(): on " + rotatingJoint.desc());
				jointMotion = JointMotion.get(rotatingJoint);
				jointMotion.controller = this;
				putAxis(jointMotion);
			}
		}

		public override string descPrefix()
		{
			return "MNR";
		}
	}

	public class ModuleDockRotate: ModuleBaseRotate
	{
		/*

			the active module of the couple is the farthest from the root part
			the proxy module of the couple is the closest to the root part

			docking node states:

			* PreAttached
			* Docked (docker/same vessel/dockee) - (docker) and (same vessel) are coupled with (dockee)
			* Ready
			* Disengage
			* Acquire
			* Acquire (dockee)

		*/

		private ModuleDockingNode dockingNode;

		public override string GetModuleDisplayName()
		{
			if (cached_moduleDisplayName == "")
				cached_moduleDisplayName = Localizer.Format("#DCKROT_port_displayname");
			return cached_moduleDisplayName;
		}

		public override string GetInfo()
		{
			if (cached_info == "")
				cached_info = Localizer.Format("#DCKROT_port_info");
			return cached_info;
		}

		protected override AttachNode referenceNode()
		{
			return dockingNode ? dockingNode.referenceNode : null;
		}

		protected override bool setupLocalAxis(StartState state)
		{
			dockingNode = part.FindModuleImplementing<ModuleDockingNode>();

			if (!dockingNode) {
				log(desc(), ".setupGeometry(" + state + "): no docking node");
				return false;
			}

			partNodePos = Vector3.zero.Tp(dockingNode.T(), part.T());
			partNodeAxis = Vector3.forward.Td(dockingNode.T(), part.T());
			if (verboseEvents)
				log(desc(), ".setupGeometry(" + state + ") done: "
					+ partNodeAxis + "@" + partNodePos);
			return true;
		}

		private PartJoint dockingJoint(ModuleDockingNode node, bool verbose)
		{
			if (!node || !node.part) {
				if (verbose)
					log(node.part.desc(), ".dockingJoint(): no node");
				return null;
			}

			if (verbose && node.state != "PreAttached" && !node.state.StartsWith("Docked"))
				log(node.part.desc(), ".dockingJoint(): unconnected state " + node.state);

			ModuleDockingNode other = node.otherNode;
			if (other) {
				if (verbose)
					log(node.part.desc(), ".dockingJoint(): other is " + other.part.desc());
			} else if (node.dockedPartUId > 0) {
				other = node.FindOtherNode();
				if (verbose && other)
					log(node.part.desc(), ".dockingJoint(): other found " + other.part.desc());
			}

			if (!other || !other.part) {
				if (verbose)
					log(node.part.desc(), ".dockingJoint(): no other, id = " + node.dockedPartUId);
				return null;
			}

			ModuleBaseRotate otherModule = other.part.FindModuleImplementing<ModuleBaseRotate>();
			if (otherModule) {
				if (!smartAutoStruts && otherModule.smartAutoStruts) {
					smartAutoStruts = true;
					log(desc(), ": smartAutoStruts activated by " + otherModule.desc());
				}
			}

			PartJoint ret = node.sameVesselDockJoint;
			if (ret && ret.Target == other.part) {
				if (verbose)
					log(node.part.desc(), ".dockingJoint(): to same vessel " + ret.desc());
				return ret;
			}

			ret = other.sameVesselDockJoint;
			if (ret && ret.Target == node.part) {
				if (verbose)
					log(node.part.desc(), ".dockingJoint(): from same vessel " + ret.desc());
				return ret;
			}

			if (node.part.parent == other.part) {
				ret = node.part.attachJoint;
				if (verbose)
					log(node.part.desc(), ".dockingJoint(): to parent " + ret.desc());
				return ret;
			}

			for (int i = 0; i < node.part.children.Count; i++) {
				Part child = node.part.children[i];
				if (child == other.part) {
					ret = child.attachJoint;
					if (verbose)
						log(node.part.desc(), ".dockingJoint(): to child " + ret.desc());
					return ret;
				}
			}

			if (verbose)
				log(node.part.desc(), ".dockingJoint(): nothing");
			return null;
		}

		protected override void setup()
		{
			jointMotion = null;
			nodeRole = "None";

			if (!dockingNode) {
				log(desc(), ".setup(): no dockingNode");
				return;
			}

			PartJoint rotatingJoint = dockingJoint(dockingNode, verboseEvents);
			if (rotatingJoint) {
				nodeRole = part == rotatingJoint.Host ? "Host"
					: part == rotatingJoint.Target ? "Target"
					: "Unknown";
				if (rotatingJoint.Host.parent != rotatingJoint.Target)
					nodeRole += "NoTree";
				if (verboseEvents)
					log(desc(), ".setup(): on " + rotatingJoint.desc());
				jointMotion = JointMotion.get(rotatingJoint);
				if (part == rotatingJoint.Host) {
					jointMotion.controller = this;
					putAxis(jointMotion);
				}
			}

			if (dockingNode.snapRotation && dockingNode.snapOffset > 0f
				&& jointMotion && rotatingJoint.Host == part && rotationEnabled) {
				enqueueFrozenRotation(jointMotion.angleToSnap(dockingNode.snapOffset), speed());
			}
		}

		public override string descPrefix()
		{
			return "MDR";
		}
	}
}

