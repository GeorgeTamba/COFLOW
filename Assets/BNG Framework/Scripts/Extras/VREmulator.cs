using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BNG {

    public class VREmulator : MonoBehaviour {

        [Header("Enable / Disable : ")]
        [Tooltip("Use Emulator if true and HMDIsActive is false")]
        public bool EmulatorEnabled = true;

        [Tooltip("Set to false if you want to use in standalone builds as well as the editor")]
        public bool EditorOnly = true;

        [Tooltip("If true the game window must have focus for the emulator to be active")]
        public bool RequireGameFocus = true;

        [Tooltip("Set to true if you want the cursor to be locked on Start. Otherwise the cursor will be locked with right click / lock screen input action")]
        public bool AutoLockCursor = false;
        private bool _cursorLocked = false;

        [Tooltip("AutoLockCursor must be enabled. If set to true, right click will act as right side grip. This way you can use right click for grab, and left click for trigger.")]
        public bool RightClickGrab = false;

        [Header("Input : ")]
        [SerializeField]
        [Tooltip("Action set used specifically to mimic or supplement a vr setup")]
        public InputActionAsset EmulatorActionSet;

        [Header("Player Teleportation")]
        [Tooltip("Will set the PlayerTeleport component's ForceStraightArrow = true while the emulator is active.")]
        public bool ForceStraightTeleportRotation = true;

        [Header("Move Player Up / Down")]
        [Tooltip("If true, move the player eye offset up / down whenever PlayerUpAction / PlayerDownAction is called.")]
        public bool AllowUpDownControls = true;

        [Tooltip("If true, the players height will be elevated in the BNGPlayerController")]
        public bool ElevatePlayerHeightOnDisconnect = true;

        [Tooltip("Unity Input Action used to move the player up")]
        public InputActionReference PlayerUpAction;

        [Tooltip("Unity Input Action used to move the player down")]
        public InputActionReference PlayerDownAction;

        [Tooltip("Minimum height in meters the player can shrink to when using the PlayerDownAction")]
        public float MinPlayerHeight = 0.2f;

        [Tooltip("Maximum height in meters the player can grow to when using the PlayerUpAction")]
        public float MaxPlayerHeight = 5f;

        [Header("Head Look")]
        [Tooltip("Unity Input Action used to lock the camera in game mode to look around")]
        public InputActionReference LockCameraAction;

        [Tooltip("Unity Input Action used to lock the camera in game mode to look around")]
        public InputActionReference CameraLookAction;

        [Tooltip("Multiply the CameraLookAction by this amount")]
        public float CameraLookSensitivityX = 0.1f;

        [Tooltip("Multiply the CameraLookAction by this amount")]
        public float CameraLookSensitivityY = 0.1f;

        [Tooltip("Minimum local Eulers degrees the camera can rotate")]
        public float MinimumCameraY = -90f;

        [Tooltip("Minimum local Eulers degrees the camera can rotate")]
        public float MaximumCameraY = 90f;

        [Header("Controller Emulation")]
        [Tooltip("Unity Input Action used to mimic holding the Left Grip")]
        public InputActionReference LeftGripAction;

        [Tooltip("Unity Input Action used to mimic holding the Left Trigger")]
        public InputActionReference LeftTriggerAction;

        [Tooltip("Unity Input Action used to mimic having your thumb near a button")]
        public InputActionReference LeftThumbNearAction;

        [Tooltip("Unity Input Action used to move mimic holding the Right Grip")]
        public InputActionReference RightGripAction;

        [Tooltip("Unity Input Action used to move mimic holding the Right Grip")]
        public InputActionReference RightTriggerAction;

        [Tooltip("Unity Input Action used to mimic having your thumb near a button")]
        public InputActionReference RightThumbNearAction;

        [Header("Remote Grabber Visualization")]
        [Tooltip("Enable this object when remote grabber is enabled and nothing held in LeftPlayerGrabber. Leave empty to disable this feature.")]
        public GameObject LeftRemoteGrabberPreviewObject;

        [Tooltip("Enable this object when remote grabber is enabled and nothing held in RightPlayerGrabber. Leave empty to disable this feature.")]
        public GameObject RightRemoteGrabberPreviewObject;

        float mouseRotationX;
        float mouseRotationY;

        Transform mainCameraTransform;
        Transform leftControllerTranform;
        Transform rightControllerTranform;

        Transform leftHandAnchor;
        Transform rightHandAnchor;

        BNGPlayerController player;
        SmoothLocomotion smoothLocomotion;
        PlayerTeleport playerTeleport;
        bool didFirstActivate = false;

        // The head / controller anchors keep applying their last tracked pose after a headset goes
        // away, which fights the emulator for control of those transforms. Cached so we can switch
        // them off while running on desktop.
        List<TrackedDevice> trackedDevices = new List<TrackedDevice>();

        // Once a physical controller is detected these tilt the controller transforms to line up with it.
        // That tilt has to come back off on desktop, or it leaves the UI pointer aiming at the floor.
        List<ControllerOffsetHelper> controllerOffsets = new List<ControllerOffsetHelper>();

        Grabber grabberLeft;
        Grabber grabberRight;

        private float _originalPlayerYOffset = 1.65f;

        // Last real eye height reported by the HMD while tracked. Used as a more accurate
        // fallback than the fixed ElevateCameraHeight default when the HMD disconnects.
        private float _lastKnownRealEyeHeight = -1f;

        [Header("Shown for Debug : ")]
        public bool HMDIsActive;

        public Vector3 LeftControllerPosition = new Vector3(-0.2f, -0.2f, 0.5f);
        public Vector3 RightControllerPosition = new Vector3(0.2f, -0.2f, 0.5f);

        [Header("Mirrored Eye Compensation : ")]
        [Tooltip("If true, shift both emulated controllers sideways while the XR display is still running. The desktop window mirrors one headset eye in that case, which isn't centred, so the hands look shifted. Has no effect in the editor or in a build that never connected to a headset.")]
        public bool CompensateForMirroredEye = true;

        [Tooltip("How far sideways to shift both controllers while compensating, in meters. Negative moves them left. The two controllers sit 0.4m apart, so if the pair still looks off centre by a quarter of the gap between them, adjust this by 0.1.")]
        public float MirroredEyeOffsetX = -0.12f;

        bool priorStraightSetting;

        bool emulatorWasActive = false;
        bool emulatorActivatedBefore = false;

        void Start() {

            if (GameObject.Find("CameraRig")) {
                mainCameraTransform = GameObject.Find("CameraRig").transform;
            }
            // Oculus Rig Setup
            else if (GameObject.Find("OVRCameraRig")) {
                mainCameraTransform = GameObject.Find("OVRCameraRig").transform;
            }

            leftHandAnchor = GameObject.Find("LeftHandAnchor").transform;
            rightHandAnchor = GameObject.Find("RightHandAnchor").transform;

            leftControllerTranform = GameObject.Find("LeftControllerAnchor").transform;
            rightControllerTranform = GameObject.Find("RightControllerAnchor").transform;

            player = FindObjectOfType<BNGPlayerController>();

            cacheTrackedDevice(player != null ? player.CenterEyeAnchor : null);
            cacheTrackedDevice(leftControllerTranform);
            cacheTrackedDevice(rightControllerTranform);

            cacheControllerOffset(leftControllerTranform);
            cacheControllerOffset(rightControllerTranform);

            if (player) {
                // Use this to keep our head up high
                player.ElevateCameraIfNoHMDPresent = ElevatePlayerHeightOnDisconnect;
                _originalPlayerYOffset = player.ElevateCameraHeight;

                smoothLocomotion = player.GetComponentInChildren<SmoothLocomotion>(true);

                // initialize component if it's currently disabled
                if (smoothLocomotion != null && !smoothLocomotion.isActiveAndEnabled) {
                    smoothLocomotion.CheckControllerReferences();
                }

                playerTeleport = player.GetComponentInChildren<PlayerTeleport>(true);
                if (playerTeleport) {
                    priorStraightSetting = playerTeleport.ForceStraightArrow;
                }

                if (smoothLocomotion == null) {
                    Debug.Log("No Smooth Locomotion component found. Will not be able to use SmoothLocomotion without calling it manually.");
                } else if (smoothLocomotion.MoveAction == null) {
                    Debug.Log("Smooth Locomotion Move Action has not been assigned. Make sure to assign this in the inspector if you want to be able to move around using the VR Emulator.");
                }
            }
        }

        /// <summary>
        /// Hand the UI pointer system a clean slate. Its caster parent, canvas event cameras and pointer
        /// event data are cached at startup, so state carried over from a VR session can otherwise leave
        /// the UI raycast dead - and with it the pointer line used to click things.
        /// </summary>
        public virtual void RefreshUISystem() {
            VRUISystem uiSystem = FindObjectOfType<VRUISystem>();

            if (uiSystem != null) {
                uiSystem.ReinitializeUISystem();
            }
        }

        void cacheControllerOffset(Transform anchor) {
            if (anchor == null) {
                return;
            }

            ControllerOffsetHelper offsetHelper = anchor.GetComponentInChildren<ControllerOffsetHelper>(true);
            if (offsetHelper != null) {
                controllerOffsets.Add(offsetHelper);
            }
        }

        /// <summary>
        /// Add or remove the physical controller alignment offsets. Left applied on desktop they tilt the
        /// controllers - and the UI pointer parented to them - away from where the player is looking.
        /// </summary>
        public virtual void SetControllerOffsetsApplied(bool offsetsApplied) {
            for (var x = 0; x < controllerOffsets.Count; x++) {
                if (controllerOffsets[x] != null) {
                    controllerOffsets[x].ApplyOffset(offsetsApplied);
                }
            }
        }

        void cacheTrackedDevice(Transform anchor) {
            if (anchor == null) {
                return;
            }

            TrackedDevice device = anchor.GetComponent<TrackedDevice>();
            if (device != null) {
                trackedDevices.Add(device);
            }
        }

        /// <summary>
        /// Hand the head / controller anchors back to their tracked devices, or take them over for the
        /// emulator. While disabled the anchors are returned to their initial pose so a stale tracked
        /// pose left over from VR doesn't stay stuck on them.
        /// </summary>
        public virtual void SetTrackedDevicesEnabled(bool devicesEnabled) {
            for (var x = 0; x < trackedDevices.Count; x++) {
                if (trackedDevices[x] == null) {
                    continue;
                }

                trackedDevices[x].enabled = devicesEnabled;

                if (!devicesEnabled) {
                    trackedDevices[x].ResetToInitialPose();
                }
            }
        }

        public virtual void LockCursor() {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            _cursorLocked = true;
        }

        public virtual void UnlockCursor() {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            _cursorLocked = false;
        }

        public void OnBeforeRender() {
            HMDIsActive = InputBridge.Instance.HMDActive;

            // Ready to go
            if (EmulatorEnabled && !HMDIsActive) {
                UpdateControllerPositions();
            }
        }

        void onFirstActivate() {

            // Make sure the emulator owns the anchors, not a headset that isn't there
            SetTrackedDevicesEnabled(false);
            SetControllerOffsetsApplied(false);

            UpdateControllerPositions();

            didFirstActivate = true;
        }

        void Update() {

            //// Considerd absent if specified or unknown status
            // bool userAbsent = XRDevice.userPresence == UserPresenceState.NotPresent || XRDevice.userPresence == UserPresenceState.Unknown;
            // Updated to show in Debug Settings
            HMDIsActive = InputBridge.Instance.HMDActive;

            // Keep track of the real tracked eye height while the HMD is active so we have
            // an accurate fallback to use if/when it disconnects (instead of a fixed guess)
            if (HMDIsActive && player != null) {
                _lastKnownRealEyeHeight = player.CameraHeight;
            }

            if (emulatorWasActive != HMDIsActive) {
                OnEmulatorStateChange();
            }

            // Ready to go
            if (EmulatorEnabled && !HMDIsActive) {

                // Check cursor lock
                // Toggle cursor lock state when Escape is pressed
                if (_cursorLocked && Input.GetKeyDown(KeyCode.Escape)) {
                    UnlockCursor();
                } 
                // Lock cursor when clicked in again
                else if (AutoLockCursor && !_cursorLocked && Input.GetMouseButtonDown(0)) {
                    // Wait briefly so any UI events can fire
                    Invoke("LockCursor", 0.1f);
                }

                if (!didFirstActivate) {
                    onFirstActivate();
                }

                // Require focus
                if (HasRequiredFocus()) {
                    CheckHeadControls();

                    UpdateControllerPositions();

                    CheckPlayerControls();
                }
            }

            UpdateRemoteGrabberPreviews();

            // Device came online after emulator had started
            if (EmulatorEnabled && didFirstActivate && HMDIsActive) {
                ResetAll();
            }

            /// Update our state
            emulatorWasActive = HMDIsActive;
        }

        void OnEmulatorStateChange() {
            // Switched from vr back to flat screen
            if (!HMDIsActive) {
                OnSwitchToPC();
            }
            // Flat screen back to VR
            else {
                OnSwitchToVR();
            }
        }

        public virtual void OnSwitchToVR() {

            // Give the anchors back to the headset
            SetTrackedDevicesEnabled(true);
            SetControllerOffsetsApplied(true);

            mainCameraTransform.localPosition = Vector3.zero;
            mainCameraTransform.localEulerAngles = Vector3.zero;

            RefreshUISystem();

            // No longer elevate camera now that hmd is back online
            if(emulatorActivatedBefore || didFirstActivate) {
                player.ElevateCameraIfNoHMDPresent = false;
            }
        }

        public virtual void OnSwitchToPC() {
            emulatorActivatedBefore = true;

            // Stop the anchors from replaying the last pose the headset reported, which would otherwise
            // leave the view tilted and drag the controllers back out of reach of the emulator
            SetTrackedDevicesEnabled(false);
            SetControllerOffsetsApplied(false);

            mainCameraTransform.localEulerAngles = Vector3.zero;
            ResetHands();

            RefreshUISystem();

            if (player != null) {
                // OnSwitchToVR turns this off, so turn it back on or the rig will sit on the
                // floor once the headset goes away again
                player.ElevateCameraIfNoHMDPresent = ElevatePlayerHeightOnDisconnect;

                // Prefer the last real tracked eye height over the fixed ElevateCameraHeight
                // guess so the view doesn't jump when the headset disconnects
                if (_lastKnownRealEyeHeight > 0.1f) {
                    player.ElevateCameraHeight = _lastKnownRealEyeHeight;
                }
            }
        }

        public virtual bool HasRequiredFocus() {

            // No Focus Required
            if(RequireGameFocus == false) {
                return true;
            }

            return Application.isFocused;
        }

        

        public void CheckHeadControls() {

            // Hold LockCameraAction (example : right mouse button down ) to move camera around
            if (LockCameraAction != null) {

                bool doMouseLook = (AutoLockCursor && _cursorLocked) || (!AutoLockCursor && LockCameraAction.action.ReadValue<float>() == 1);

                // Lock
                if (doMouseLook) {

                    // Lock Camera and cursor
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                   
                    // Make sure cursor is indeed locked / invisible
                    if(!Cursor.visible && Cursor.lockState == CursorLockMode.Locked) {
                        Vector3 mouseLook = Vector2.zero;
                        if (CameraLookAction != null) {
                            mouseLook = CameraLookAction.action.ReadValue<Vector2>();
                        }
                        // Fall back to mouse
                        else {
                            mouseLook = Mouse.current.delta.ReadValue();
                        }
                        // Rotation Y
                        mouseRotationY += mouseLook.y * CameraLookSensitivityY;

                        mouseRotationY = Mathf.Clamp(mouseRotationY, MinimumCameraY, MaximumCameraY);
                        mainCameraTransform.localEulerAngles = new Vector3(-mouseRotationY, mainCameraTransform.localEulerAngles.y, 0);

                        // Move PLayer on X Axis
                        player.transform.Rotate(0, mouseLook.x * CameraLookSensitivityX, 0);
                    }
                   
                }
                // Unlock Camera
                else if(!AutoLockCursor && LockCameraAction.action.ReadValue<float>() == 0) {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }        

        float prevVal;
        /// <summary>
        /// Overwrite InputBridge inputs with our own bindings
        /// </summary>
        public void UpdateInputs() {

            // This is driven by a static event, so a destroyed emulator can still be called here if it
            // ever fails to unsubscribe. Its HMDIsActive is frozen at whatever it last saw, so it would
            // happily go on overwriting the live controller inputs with zeroes.
            if (this == null) {
                return;
            }

            // Only override controls if no hmd is active and this script is enabled
            if (EmulatorEnabled == false || HMDIsActive) {
                return;
            }

            // Window doesn't have focus
            if(!HasRequiredFocus()) {
                return;
            }

            // Make sure grabbers are assigned
            checkGrabbers();

            // Simulate Left Controller states
            if (LeftTriggerAction != null) {
                prevVal = InputBridge.Instance.LeftTrigger;
                InputBridge.Instance.LeftTrigger = LeftTriggerAction.action.ReadValue<float>();
                InputBridge.Instance.LeftTriggerDown = prevVal < InputBridge.Instance.DownThreshold && InputBridge.Instance.LeftTrigger >= InputBridge.Instance.DownThreshold;
                InputBridge.Instance.LeftTriggerUp = prevVal > InputBridge.Instance.DownThreshold && InputBridge.Instance.LeftTrigger < InputBridge.Instance.DownThreshold;
            }

            if (LeftGripAction != null) {
                prevVal = InputBridge.Instance.LeftGrip;
                InputBridge.Instance.LeftGrip = LeftGripAction.action.ReadValue<float>();
                InputBridge.Instance.LeftGripDown = prevVal < InputBridge.Instance.DownThreshold && InputBridge.Instance.LeftGrip >= InputBridge.Instance.DownThreshold;
            }

            if(LeftThumbNearAction != null) {
                InputBridge.Instance.LeftThumbNear = LeftThumbNearAction.action.ReadValue<float>() == 1;
            }

            // Simulate Right Controller states
            if (RightTriggerAction!= null) {
                float rightTriggerVal = RightTriggerAction.action.ReadValue<float>();

                prevVal = InputBridge.Instance.RightTrigger;
                InputBridge.Instance.RightTrigger = RightTriggerAction.action.ReadValue<float>();
                InputBridge.Instance.RightTriggerDown = prevVal < InputBridge.Instance.DownThreshold && InputBridge.Instance.RightTrigger >= InputBridge.Instance.DownThreshold;
                InputBridge.Instance.RightTriggerUp = prevVal > InputBridge.Instance.DownThreshold && InputBridge.Instance.RightTrigger < InputBridge.Instance.DownThreshold;
            }

            if (RightGripAction != null) {
                prevVal = InputBridge.Instance.RightGrip;
                InputBridge.Instance.RightGrip = RightGripAction.action.ReadValue<float>();

                // Simulate grip with right click if option is enabled
                if(AutoLockCursor && RightClickGrab && Input.GetMouseButton(1)) {
                    InputBridge.Instance.RightGrip = 1f;
                }

                InputBridge.Instance.RightGripDown = prevVal < InputBridge.Instance.DownThreshold && InputBridge.Instance.RightGrip >= InputBridge.Instance.DownThreshold;
            }

            if(RightThumbNearAction) {
                InputBridge.Instance.RightThumbNear = RightThumbNearAction.action.ReadValue<float>() == 1;
            }
        }

        public void CheckPlayerControls() {

            // Require focus
            if(EditorOnly && !Application.isEditor) {
                return;
            }

            // Player Up / Down
            if(AllowUpDownControls) {
                if (PlayerUpAction != null && PlayerUpAction.action.ReadValue<float>() == 1) {
                    player.ElevateCameraHeight = Mathf.Clamp(player.ElevateCameraHeight + Time.deltaTime, MinPlayerHeight, MaxPlayerHeight);
                }
                else if (PlayerDownAction != null && PlayerDownAction.action.ReadValue<float>() == 1) {
                    player.ElevateCameraHeight = Mathf.Clamp(player.ElevateCameraHeight - Time.deltaTime, MinPlayerHeight, MaxPlayerHeight);
                }
            }

            // Force Forward Arrow
            if(ForceStraightTeleportRotation && playerTeleport != null && playerTeleport.ForceStraightArrow == false) {
                playerTeleport.ForceStraightArrow = true;
            }

            // Player Move Forward / Back, Snap Turn
            if (smoothLocomotion != null && smoothLocomotion.enabled == false) {
                // Manually allow player movement if the smooth locomotion component is disabled
                smoothLocomotion.CheckControllerReferences();
                smoothLocomotion.UpdateInputs();

                if(smoothLocomotion.ControllerType == PlayerControllerType.CharacterController) {
                    smoothLocomotion.MoveCharacter();
                }
                else if (smoothLocomotion.ControllerType == PlayerControllerType.Rigidbody) {
                    smoothLocomotion.MoveRigidCharacter();
                }
            }
        }

        //void FixedUpdate() {
        //    // Player Move Forward / Back, Snap Turn
        //    //if (smoothLocomotion != null && smoothLocomotion.enabled == false && smoothLocomotion.ControllerType == PlayerControllerType.Rigidbody) {
        //    //    smoothLocomotion.MoveRigidCharacter();
        //    //}
        //}

        public virtual void UpdateControllerPositions() {

            Vector3 mirroredEyeOffset = new Vector3(GetMirroredEyeOffsetX(), 0f, 0f);

            leftControllerTranform.localPosition = LeftControllerPosition + mirroredEyeOffset;
            leftControllerTranform.localEulerAngles = Vector3.zero;

            rightControllerTranform.localPosition = RightControllerPosition + mirroredEyeOffset;
            rightControllerTranform.localEulerAngles = Vector3.zero;
        }

        /// <summary>
        /// While the XR display is still running, the desktop window shows a mirrored headset eye rather
        /// than a centred view, so everything in it - the hands included - appears shifted sideways.
        /// Returns a sideways offset to compensate, or zero when XR isn't running and the window is
        /// already showing a centred view (the editor, or a build that never connected to a headset).
        /// </summary>
        public virtual float GetMirroredEyeOffsetX() {

            if (!CompensateForMirroredEye || !UnityEngine.XR.XRSettings.isDeviceActive) {
                return 0f;
            }

            return MirroredEyeOffsetX;
        }


        public virtual void UpdateRemoteGrabberPreviews() {

            // Only show preview in Emulator Mode
            if(HMDIsActive) {
                if(LeftRemoteGrabberPreviewObject != null && LeftRemoteGrabberPreviewObject.activeSelf) {
                    LeftRemoteGrabberPreviewObject.SetActive(false);
                }
                if (RightRemoteGrabberPreviewObject != null && RightRemoteGrabberPreviewObject.activeSelf) {
                    RightRemoteGrabberPreviewObject.SetActive(false);
                }
                return;
            }

            // Object was specified so we can check to activate it or not
            if(LeftRemoteGrabberPreviewObject != null && grabberLeft != null) {
                LeftRemoteGrabberPreviewObject.SetActive(!grabberLeft.HoldingItem && !grabberLeft.RemoteGrabbingItem);
            }
            if (RightRemoteGrabberPreviewObject != null && grabberRight != null) {
                RightRemoteGrabberPreviewObject.SetActive(!grabberRight.HoldingItem && !grabberRight.RemoteGrabbingItem);
            }
        }


        void checkGrabbers() {
            // Find Grabber Left
            if (grabberLeft == null || !grabberLeft.isActiveAndEnabled) {
                Grabber[] grabbers = FindObjectsOfType<Grabber>();

                for (var x = 0; x < grabbers.Length; x++) {
                    if (grabbers[x] != null && grabbers[x].isActiveAndEnabled && grabbers[x].HandSide == ControllerHand.Left) {
                        grabberLeft = grabbers[x];
                    }
                }
            }

            // Find Grabber Right
            if (grabberRight == null || !grabberRight.isActiveAndEnabled) {
                Grabber[] grabbers = FindObjectsOfType<Grabber>();
                for (var x = 0; x < grabbers.Length; x++) {
                    if (grabbers[x] != null && grabbers[x].isActiveAndEnabled && grabbers[x].HandSide == ControllerHand.Right) {
                        grabberRight = grabbers[x];
                    }
                }
            }
        }

        public virtual void ResetHands() {

            // Null checks matter here : this also runs from OnDisable, by which point a scene change
            // may already have destroyed the anchors
            if (leftControllerTranform != null) {
                leftControllerTranform.localPosition = Vector3.zero;
                leftControllerTranform.localEulerAngles = Vector3.zero;
            }

            if (rightControllerTranform != null) {
                rightControllerTranform.localPosition = Vector3.zero;
                rightControllerTranform.localEulerAngles = Vector3.zero;
            }
        }

        public virtual void ResetAll() {

            // Always hand the anchors back, so tearing down the emulator can't leave them switched off
            SetTrackedDevicesEnabled(true);
            SetControllerOffsetsApplied(true);

            ResetHands();

            // Reset Camera
            if (mainCameraTransform != null) {
                mainCameraTransform.localEulerAngles = Vector3.zero;
            }

            // Reset Player
            if (player) {
                player.ElevateCameraHeight = _originalPlayerYOffset;
            }

            // Reset Teleport Status
            if(ForceStraightTeleportRotation && playerTeleport) {
                playerTeleport.ForceStraightArrow = priorStraightSetting;
            }

            didFirstActivate = false;
        }

        void OnEnable() {

            if (EmulatorActionSet != null) {
                foreach (var map in EmulatorActionSet.actionMaps) {
                    foreach (var action in map) {
                        if(action != null) {
                            action.Enable();
                        }
                    }
                }
            }

            // Subscribe to input events
            InputBridge.OnInputsUpdated += UpdateInputs;

            Application.onBeforeRender += OnBeforeRender;
        }

        void OnDisable() {

            // Disable Input Actions
            if (EmulatorActionSet != null) {
                foreach (var map in EmulatorActionSet.actionMaps) {
                    foreach (var action in map) {
                        if (action != null) {
                            action.Disable();
                        }
                    }
                }
            }

            Application.onBeforeRender -= OnBeforeRender;

            // Unsubscribe from input events first. OnInputsUpdated is a static event, so if anything
            // below throws and skips this, a destroyed emulator stays subscribed for the rest of the
            // session and keeps overwriting the real controller inputs with its own emulated ones.
            InputBridge.OnInputsUpdated -= UpdateInputs;

            if (isQuitting) {
                return;
            }

            // Reset Hand Positions
            ResetAll();
        }

        bool isQuitting = false;
        void OnApplicationQuit() {
            isQuitting = true;
        }
    }
}
