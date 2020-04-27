//-----------------------------------------------------------------------
// <copyright file="CloudAnchorsExampleController.cs" company="Google">
//
// Copyright 2019 Google LLC. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

namespace Google.XR.ARCoreExtensions.Samples.CloudAnchors
{
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.EventSystems;
	using UnityEngine.Networking;
	using UnityEngine.SceneManagement;
	using UnityEngine.XR.ARFoundation;
	using UnityEngine.UI;

    /// <summary>
    /// Controller for the Cloud Anchors Example. Handles the ARCore lifecycle.
    /// See details in
    /// <a href="https://developers.google.com/ar/develop/unity-arf/cloud-anchors/overview">
    /// Share AR experiences with Cloud Anchors</a>
    /// </summary>
	public class CloudAnchorsExampleController : MonoBehaviour
	{
		[Header("AR Foundation")]

        /// <summary>
        /// The active AR Session Origin used in the example.
        /// </summary>
		public ARSessionOrigin SessionOrigin;

        /// <summary>
        /// The AR Session used in the example.
        /// </summary>
		public GameObject SessionCore;

        /// <summary>
        /// The AR Extentions used in the example.
        /// </summary>
		public GameObject ARExtentions;

        /// <summary>
        /// The active AR Anchor Manager used in the example.
        /// </summary>
		public ARAnchorManager AnchorManager;

        /// <summary>
        /// The active AR Raycast Manager used in the example.
        /// </summary>
		public ARRaycastManager RaycastManager;

		[Header("UI")]

        /// <summary>
        /// The network manager UI Controller.
        /// </summary>
		public NetworkManagerUIController NetworkUIController;

        /// <summary>
        /// The Lobby Screen to see Available Rooms or create a new one.
        /// </summary>
		public GameObject LobbyScreen;

        /// <summary>
        /// The Start Screen to see help information.
        /// </summary>
		public GameObject StartScreen;

        /// <summary>
        /// The AR Screen which display the AR view, return to lobby button and room number.
        /// </summary>
		public GameObject ARScreen;

        /// <summary>
        /// The Status Screen to display the connection status and cloud anchor instructions.
        /// </summary>
		public GameObject StatusScreen;

		[Header("mARble Extension")]
		public static CloudAnchorsExampleController Instance;
		public Camera ARCamera;
		[SerializeField]
		private GameObject currSelectedPrefab;
		private GameObject PlacedPrefab 
		{
			get { return currSelectedPrefab; }
			set { currSelectedPrefab = value; }
		}
		private PlacementObject[] placedObjects;
		public PlacementObject lastSelectedObject;
		[SerializeField]
		private Button DominoButton, LimaconButton, HypotrochoidButton, MarbleButton, ColaButton, Cycloid2LoopButton, CannonButton, CannonballButton, SimpleRampButton, SpiralRampButton, StarButton, ToiletRollButton, ZigzagButton;
		[SerializeField]
		private Button editTypeButton;
		enum EditType {
			EDIT_ROTATION,
			EDIT_SCALE
		}
		private EditType currEditType;

		[SerializeField]
		private Slider editSlider;

		[SerializeField]
		private Text editTextValue;

        /// <summary>
        /// The key name used in PlayerPrefs which indicates whether
        /// the start info has displayed at least one time.
        /// </summary>
		private const string k_HasDisplayedStartInfoKey = "HasDisplayedStartInfo";

        /// <summary>
        /// The time between room starts up and ARCore session starts resolving.
        /// </summary>
		private const float k_ResolvingPrepareTime = 3.0f;

        /// <summary>
        /// Record the time since the room started. If it passed the resolving prepare time,
        /// applications in resolving mode start resolving the anchor.
        /// </summary>
		private float m_TimeSinceStart = 0.0f;

        /// <summary>
        /// Indicates whether passes the resolving prepare time.
        /// </summary>
		private bool m_PassedResolvingPreparedTime = false;

        /// <summary>
        /// Indicates whether the Anchor was already instantiated.
        /// </summary>
		private bool m_AnchorAlreadyInstantiated = false;

        /// <summary>
        /// Indicates whether the Cloud Anchor finished hosting.
        /// </summary>
		private bool m_AnchorFinishedHosting = false;

        /// <summary>
        /// True if the app is in the process of quitting due to an ARCore connection error,
        /// otherwise false.
        /// </summary>
		private bool m_IsQuitting = false;

        /// <summary>
        /// The world origin transform for this session.
        /// </summary>
		private Transform m_WorldOrigin = null;

        /// <summary>
        /// The current cloud anchor mode.
        /// </summary>
		private ApplicationMode m_CurrentMode = ApplicationMode.Ready;

        /// <summary>
        /// The current active UI screen.
        /// </summary>
		private ActiveScreen m_CurrentActiveScreen = ActiveScreen.LobbyScreen;

        /// <summary>
        /// The Network Manager.
        /// </summary>
#pragma warning disable 618
		private CloudAnchorsNetworkManager m_NetworkManager;
#pragma warning restore 618

        /// <summary>
        /// Enumerates modes the example application can be in.
        /// </summary>
		public enum ApplicationMode
		{
            /// <summary>
            /// Enume mode that indicate the example application is ready to host or resolve.
            /// </summary>
			Ready,

            /// <summary>
            /// Enume mode that indicate the example application is hosting cloud anchors.
            /// </summary>
			Hosting,

            /// <summary>
            /// Enume mode that indicate the example application is resolving cloud anchors.
            /// </summary>
			Resolving,
		}

        /// <summary>
        /// Enumerates the active UI screens the example application can be in.
        /// </summary>
		public enum ActiveScreen
		{
            /// <summary>
            /// Enume mode that indicate the example application is on lobby screen.
            /// </summary>
			LobbyScreen,

            /// <summary>
            /// Enume mode that indicate the example application is on start screen.
            /// </summary>
			StartScreen,

            /// <summary>
            /// Enume mode that indicate the example application is on AR screen.
            /// </summary>
			ARScreen,
		}

        /// <summary>
        /// Gets a value indicating whether the Origin of the new World Coordinate System,
        /// i.e. the Cloud Anchor was placed.
        /// </summary>
		public bool IsOriginPlaced
		{
			get;
			private set;
		}

        /// <summary>
        /// Gets or sets the world origin which will be used as the transform parent for network
        /// spawned objects.
        /// </summary>
		public Transform WorldOrigin
		{
			get
			{
				return m_WorldOrigin;
			}

			set
			{
				IsOriginPlaced = true;
				m_WorldOrigin = value;

				Pose sessionPose = _ToWorldOriginPose(new Pose(SessionOrigin.transform.position,
					SessionOrigin.transform.rotation));
				SessionOrigin.transform.SetPositionAndRotation(
					sessionPose.position, sessionPose.rotation);
			}
		}

        /// <summary>
        /// Callback handling Start Now button click event.
        /// </summary>
		public void OnStartNowButtonClicked()
		{
			_SwitchActiveScreen(ActiveScreen.ARScreen);
		}

        /// <summary>
        /// Callback handling Learn More Button click event.
        /// </summary>
		public void OnLearnMoreButtonClicked()
		{
			Application.OpenURL(
				"https://developers.google.com/ar/cloud-anchor-privacy");
		}

        /// <summary>
        /// The Unity Start() method.
        /// </summary>
		public void Start()
		{
#pragma warning disable 618
			m_NetworkManager = NetworkUIController.GetComponent<CloudAnchorsNetworkManager>();
#pragma warning restore 618
			m_NetworkManager.OnClientConnected += _OnConnectedToServer;
			m_NetworkManager.OnClientDisconnected += _OnDisconnectedFromServer;

            // A Name is provided to the Game Object so it can be found by other Scripts
            // instantiated as prefabs in the scene.
			gameObject.name = "CloudAnchorsExampleController";
			_ResetStatus();
		}

        /// <summary>
        /// The Unity Update() method.
        /// Modified for project mARble
        /// </summary>
		public void Update()
		{
			_UpdateApplicationLifecycle();

			if (m_CurrentActiveScreen != ActiveScreen.ARScreen)
			{
				return;
			}

            // If we are neither in hosting nor resolving mode then the update is complete.
			if (m_CurrentMode != ApplicationMode.Hosting &&
				m_CurrentMode != ApplicationMode.Resolving)
			{
				return;
			}

            // Give AR session some time to prepare for resolving and update the UI message
            // once the preparation time passed.
			if (m_CurrentMode == ApplicationMode.Resolving && !m_PassedResolvingPreparedTime)
			{
				m_TimeSinceStart += Time.deltaTime;

				if (m_TimeSinceStart > k_ResolvingPrepareTime)
				{
					m_PassedResolvingPreparedTime = true;
					NetworkUIController.ShowDebugMessage(
						"Waiting for Cloud Anchor to be hosted...");
				}
			}

            // If the origin anchor has not been placed yet, then update in resolving mode is
            // complete.
			if (m_CurrentMode == ApplicationMode.Resolving && !IsOriginPlaced)
			{
				return;
			}

            // If the player has not touched the screen then the update is complete.
			Touch touch;
			if (Input.touchCount < 1)
			{
				return;
			}
			touch = Input.GetTouch(0);

            // Ignore the touch if it's pointing on UI objects.
			if (EventSystem.current.IsPointerOverGameObject(touch.fingerId) || IsPointerOverUIObject())
			{
				return;
			}

			List<ARRaycastHit> hitResults = new List<ARRaycastHit>();
			RaycastManager.Raycast(touch.position, hitResults);
			if (touch.phase == TouchPhase.Began)
			{
            // First check if the player is selecting an existing item
				Ray ray = ARCamera.ScreenPointToRay(touch.position);
				RaycastHit hitObject;

            // Sets item touched as Selected Object
				if(Physics.Raycast(ray, out hitObject)) 
				{
					lastSelectedObject = hitObject.transform.GetComponent<PlacementObject>();
					if(lastSelectedObject != null) 
					{
						placedObjects = FindObjectsOfType<PlacementObject>();
						foreach(PlacementObject placementObject in placedObjects) 
						{
							placementObject.Selected = placementObject == lastSelectedObject;
						}
					}
				}
			}

            // Deselect object if the player is no longer touching
			if(touch.phase == TouchPhase.Ended && lastSelectedObject != null)
			{
				lastSelectedObject.Selected = false;
			}
			

            // If there was an anchor placed, then instantiate the corresponding object.
			if (hitResults.Count > 0)
			{
                // The first touch on the Hosting mode will instantiate the origin anchor. Any
                // subsequent touch will instantiate a star, both in Hosting and Resolving modes.
				if (_CanPlaceStars())
				{
					Pose hitPose = hitResults[0].pose;
                    // yOffset to prevent object clipping through AR plane
					
                    // If no existing object is selected, instantiate a new object
					if(lastSelectedObject == null && ModeManager.Instance.GetCurrMode() == GameMode.PLACEMENT_MODE)
					{   
                        // Vector3 yOffsetPrefab = new Vector3(0f, currSelectedPrefab.GetComponent<Renderer>().bounds.size.y/2.001f, 0f);
                        // hitPose.position += yOffsetPrefab;
						_InstantiateStar(_ToWorldOriginPose(hitPose));
					} 
					else 
					{
                        // Move the selected object
						if(lastSelectedObject.Selected) 
						{
                            //V ector3 yOffsetSelectedObject = new Vector3(0f, lastSelectedObject.GetComponent<Renderer>().bounds.size.y/2.001f, 0f);

							if (lastSelectedObject.tag == "Marble")
							{
                                float snapDist = 0.075f; //0.05f;
                                MoveMarble(hitPose, snapDist);
                            } 
                            else 
                            {
                            	lastSelectedObject.transform.position = hitPose.position;
                                //  we don't use the + yOffsetSelectedObject anymore
                            }
                        }
                    }
                }
                else if (!IsOriginPlaced && m_CurrentMode == ApplicationMode.Hosting)
                {
                	ARAnchor anchor = AnchorManager.AddAnchor(hitResults[0].pose);
                	WorldOrigin = anchor.transform;
                	_InstantiateAnchor(anchor);
                	OnAnchorInstantiated(true);
                }
            }
        }

        /// <summary>
        /// The Unity Awake() method.
        /// </summary>
        public void Awake() 
        {
            // For Singleton
        	if (Instance == null) 
        	{
        		Instance = this;
        	}
        	else if (Instance != this) 
        	{
        		Destroy(gameObject);
        	}

            // Add any additional buttons for objects here
        	if(DominoButton != null && MarbleButton != null && LimaconButton != null && HypotrochoidButton != null && ColaButton != null 
        		&& Cycloid2LoopButton != null && CannonButton != null && CannonballButton != null && SimpleRampButton != null && SpiralRampButton != null
        		&& StarButton != null && ToiletRollButton != null && ZigzagButton != null) 
        	{
        		DominoButton.onClick.AddListener(() => ChangePrefabSelection("Domino"));
        		LimaconButton.onClick.AddListener(() => ChangePrefabSelection("Limacon"));
        		HypotrochoidButton.onClick.AddListener(() => ChangePrefabSelection("Hypotrochoid"));
        		MarbleButton.onClick.AddListener(() => ChangePrefabSelection("Marble"));
        		ColaButton.onClick.AddListener(() => ChangePrefabSelection("Cola Can"));
        		Cycloid2LoopButton.onClick.AddListener(() => ChangePrefabSelection("Cycloid2Loop"));
        		CannonButton.onClick.AddListener(() => ChangePrefabSelection("Cannon"));
        		CannonballButton.onClick.AddListener(() => ChangePrefabSelection("Cannonball"));
        		SimpleRampButton.onClick.AddListener(() => ChangePrefabSelection("SimpleRamp"));
        		SpiralRampButton.onClick.AddListener(() => ChangePrefabSelection("SpiralRamp"));
        		StarButton.onClick.AddListener(() => ChangePrefabSelection("Star"));
        		ToiletRollButton.onClick.AddListener(() => ChangePrefabSelection("ToiletRoll"));
        		ZigzagButton.onClick.AddListener(() => ChangePrefabSelection("Zigzag"));
        	}

        	currEditType = EditType.EDIT_ROTATION;
        	editTypeButton.onClick.AddListener(SwitchEditType);
        	editSlider.onValueChanged.AddListener(EditSliderChanged);
        }

        /// <summary>
        /// Handles the events of the slider in Edit Mode
        /// </summary>
        private void EditSliderChanged(float sliderValue) 
        {
        	switch (currEditType) 
        	{
        		case EditType.EDIT_ROTATION:
        		int rotationDegree = (int)(editSlider.normalizedValue * 360);
        		if(lastSelectedObject != null) 
        		{
        			lastSelectedObject.transform.localEulerAngles = new Vector3(0, rotationDegree, 0);
        		}
        		editTextValue.text = $"Rotation: {rotationDegree} degrees";
        		break;

        		case EditType.EDIT_SCALE:
        		float zHeight = (float)(editSlider.normalizedValue * 0.5);
        		if(lastSelectedObject != null) 
        		{
        			lastSelectedObject.transform.position = new Vector3(lastSelectedObject.transform.position.x, zHeight, lastSelectedObject.transform.position.z);
        		}
        		editTextValue.text = $"zHeight: {zHeight} units";
        		break;

                // TODO: all prefabs must be in a parent gameobject with scale (1,1,1)
                // in order to scale properly while maintaining prefabs's current scale ratios
                // case EditType.EDIT_SCALE:
                // /* if(lastSelectedObject != null) {
                //     lastSelectedObject.transform.localScale = new Vector3(sliderValue, sliderValue, sliderValue));
                //     } */
                //     editTextValue.text = $"Scale: {sliderValue}";
                //     break;
        	}
        }

        /// <summary>
        /// Handles the switching of modes
        /// </summary>
        private void SwitchEditType() 
        {
        	switch (currEditType) 
        	{
        		case EditType.EDIT_ROTATION:
        		currEditType = EditType.EDIT_SCALE;
        		editTypeButton.GetComponentInChildren<Text>().text = "Change Height";
        		break;
        		case EditType.EDIT_SCALE:
        		currEditType = EditType.EDIT_ROTATION;
        		editTypeButton.GetComponentInChildren<Text>().text = "Change Rotation";
        		break;
        	}
        }

            /// <summary>
            /// Handles the loading of prefabs for instantiation
            /// </summary>
            /// <param name="name">String of the name to look for in Resources</param>
        private void ChangePrefabSelection(string name)
        {
        	GameObject loadedGameObject = Resources.Load<GameObject>($"Prefabs/{name}");
        	if(loadedGameObject != null) 
        	{
        		PlacedPrefab = loadedGameObject;
        		Debug.Log($"Game object with name {name} was loaded");
        	} 
        	else 
        	{
        		Debug.Log($"Unable to find a game object with name {name}");
        	}
        }

        /// <summary>
        /// Handles the movement of a marble object
        /// </summary>
        private void MoveMarble(Pose hitPose, float snapDistance) 
        {
                //find nearest ramp
        	var nearestRamp = Ramp.FindClosestRamp(hitPose.position);
        	if (nearestRamp == null) 
        	{
        		lastSelectedObject.transform.position = hitPose.position;
        	}
                 //if within snap zone, snap to snap zone on ramp
        	else 
        	{
        		var nearestRampDirection = nearestRamp.transform.position - hitPose.position;
        		float nearestRampHorizontalDistance = Mathf.Sqrt(Mathf.Pow(nearestRampDirection.x, 2) + Mathf.Pow(nearestRampDirection.z, 2));
        		if (nearestRampHorizontalDistance < snapDistance) 
        		{
        			lastSelectedObject.transform.position = nearestRamp.transform.Find("SnapZone").position;
        			lastSelectedObject.transform.rotation = nearestRamp.transform.Find("SnapZone").rotation; 
                        // Need to freeze Marble after it Snaps into place
        			lastSelectedObject.GetComponent<Rigidbody>().Sleep();
        		} 
        		else 
        		{
        			lastSelectedObject.transform.position = hitPose.position;
        		}
        	}
        }

        /// <summary>
        /// Helper method to check if touch is over a UI object
        /// </summary>
        /// <returns><c>true</c>, if touch is over a UI object, <c>false</c> otherwise.</returns>
        private bool IsPointerOverUIObject() {
        	PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        	eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        	List<RaycastResult> results = new List<RaycastResult>();
        	EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        	return results.Count > 0;
        }

        /// <summary>
        /// Indicates whether the resolving prepare time has passed so the AnchorController
        /// can start to resolve the anchor.
        /// </summary>
        /// <returns><c>true</c>, if resolving prepare time passed, otherwise returns <c>false</c>.
        /// </returns>
        public bool IsResolvingPrepareTimePassed()
        {
        	return m_CurrentMode != ApplicationMode.Ready &&
        	m_TimeSinceStart > k_ResolvingPrepareTime;
        }

        /// <summary>
        /// Callback called when the lobby screen's visibility is changed.
        /// </summary>
        /// <param name="visible">If set to <c>true</c> visible.</param>
        public void OnLobbyVisibilityChanged(bool visible)
        {
        	if (visible)
        	{
        		_SwitchActiveScreen(ActiveScreen.LobbyScreen);
        	}
        	else if (PlayerPrefs.HasKey(k_HasDisplayedStartInfoKey))
        	{
        		_SwitchActiveScreen(ActiveScreen.ARScreen);
        	}
        	else
        	{
        		_SwitchActiveScreen(ActiveScreen.StartScreen);
        	}
        }

        /// <summary>
        /// Callback called when the resolving timeout is passed.
        /// </summary>
        public void OnResolvingTimeoutPassed()
        {
        	if (m_CurrentMode == ApplicationMode.Ready)
        	{
        		Debug.LogWarning("OnResolvingTimeoutPassed shouldn't be called" +
        			"when the application is in ready mode.");
        		return;
        	}

        	NetworkUIController.ShowDebugMessage("Still resolving the anchor." +
        		"Please make sure you're looking at where the Cloud Anchor was hosted." +
        		"Or, try to re-join the room.");
        }

        /// <summary>
        /// Handles user intent to enter a mode where they can place an anchor to host or to exit
        /// this mode if already in it.
        /// </summary>
        public void OnEnterHostingModeClick()
        {
        	if (m_CurrentMode == ApplicationMode.Hosting)
        	{
        		m_CurrentMode = ApplicationMode.Ready;
        		_ResetStatus();
        		Debug.Log("Reset ApplicationMode from Hosting to Ready.");
        	}

        	m_CurrentMode = ApplicationMode.Hosting;
        }

        /// <summary>
        /// Handles a user intent to enter a mode where they can input an anchor to be resolved or
        /// exit this mode if already in it.
        /// </summary>
        public void OnEnterResolvingModeClick()
        {
        	if (m_CurrentMode == ApplicationMode.Resolving)
        	{
        		m_CurrentMode = ApplicationMode.Ready;
        		_ResetStatus();
        		Debug.Log("Reset ApplicationMode from Resolving to Ready.");
        	}

        	m_CurrentMode = ApplicationMode.Resolving;
        }

        /// <summary>
        /// Callback indicating that the Cloud Anchor was instantiated and the host request was
        /// made.
        /// </summary>
        /// <param name="isHost">Indicates whether this player is the host.</param>
        public void OnAnchorInstantiated(bool isHost)
        {
        	if (m_AnchorAlreadyInstantiated)
        	{
        		return;
        	}

        	m_AnchorAlreadyInstantiated = true;
        	NetworkUIController.OnAnchorInstantiated(isHost);
        }

        /// <summary>
        /// Callback indicating that the Cloud Anchor was hosted.
        /// </summary>
        /// <param name="success">If set to <c>true</c> indicates the Cloud Anchor was hosted
        /// successfully.</param>
        /// <param name="response">The response string received.</param>
        public void OnAnchorHosted(bool success, string response)
        {
        	m_AnchorFinishedHosting = success;
        	NetworkUIController.OnAnchorHosted(success, response);
        }

        /// <summary>
        /// Callback indicating that the Cloud Anchor was resolved.
        /// </summary>
        /// <param name="success">If set to <c>true</c> indicates the Cloud Anchor was resolved
        /// successfully.</param>
        /// <param name="response">The response string received.</param>
        public void OnAnchorResolved(bool success, string response)
        {
        	NetworkUIController.OnAnchorResolved(success, response);
        }

        /// <summary>
        /// Callback that happens when the client successfully connected to the server.
        /// </summary>
        private void _OnConnectedToServer()
        {
        	if (m_CurrentMode == ApplicationMode.Hosting)
        	{
        		NetworkUIController.ShowDebugMessage(
        			"Find a plane, tap to create a Cloud Anchor.");
        	}
        	else if (m_CurrentMode == ApplicationMode.Resolving)
        	{
        		NetworkUIController.ShowDebugMessage(
        			"Look at the same scene as the hosting phone.");
        	}
        	else
        	{
        		_ReturnToLobbyWithReason(
        			"Connected to server with neither Hosting nor Resolving" +
        			"mode. Please start the app again.");
        	}
        }

        /// <summary>
        /// Callback that happens when the client disconnected from the server.
        /// </summary>
        private void _OnDisconnectedFromServer()
        {
        	_ReturnToLobbyWithReason("Network session disconnected! " +
        		"Please start the app again and try another room.");
        }

        private Pose _ToWorldOriginPose(Pose pose)
        {
        	if (!IsOriginPlaced)
        	{
        		return pose;
        	}

        	Matrix4x4 anchorTWorld = Matrix4x4.TRS(
        		m_WorldOrigin.position, m_WorldOrigin.rotation, Vector3.one).inverse;
        	Quaternion rotation = Quaternion.LookRotation(
        		anchorTWorld.GetColumn(2),
        		anchorTWorld.GetColumn(1));
        	return new Pose(
        		anchorTWorld.MultiplyPoint(pose.position),
        		pose.rotation * rotation);
        }

        /// <summary>
        /// Instantiates the anchor object at the pose of the m_WorldOrigin.
        /// This will host the Cloud Anchor.
        /// </summary>
        /// <param name="anchor">The ARAnchor object holding the anchor.</param>
        private void _InstantiateAnchor(ARAnchor anchor)
        {
            // The anchor will be spawned by the host, so no networking Command is needed.
        	GameObject.Find("LocalPlayer").GetComponent<LocalPlayerController>()
        	.SpawnAnchor(anchor);
        }

        /// <summary>
        /// Instantiates a star object that will be synchronized over the network to other clients.
        /// Modified for project mARble.
        /// </summary>
        /// <param name="hitPose">The hit position.</param>
        private void _InstantiateStar(Pose hitPose)
        {
        // Star must be spawned in the server so a networking Command is used.
        	if(currSelectedPrefab.tag == "Domino") 
        	{
        		var nearestDomino = Domino.FindClosestDomino(hitPose.position);
        		if (nearestDomino != null) 
        		{
        			var nearestDominoDirection = nearestDomino.transform.position - (hitPose.position);
        			GameObject.Find("LocalPlayer").GetComponent<LocalPlayerController>()
        			.CmdSpawnStar(hitPose.position, Quaternion.LookRotation(nearestDominoDirection), currSelectedPrefab);
        			return;
        		}
        	} 
        	
        	GameObject.Find("LocalPlayer").GetComponent<LocalPlayerController>()
        	.CmdSpawnStar(hitPose.position, hitPose.rotation, currSelectedPrefab);
        }

        /// <summary>
        /// Indicates whether a star can be placed.
        /// </summary>
        /// <returns><c>true</c>, if stars can be placed, <c>false</c> otherwise.</returns>
        public bool _CanPlaceStars()
        {
        	if (m_CurrentMode == ApplicationMode.Resolving)
        	{
        		return IsOriginPlaced;
        	}

        	if (m_CurrentMode == ApplicationMode.Hosting)
        	{
        		return IsOriginPlaced && m_AnchorFinishedHosting;
        	}

        	return false;
        }

        /// <summary>
        /// Resets the internal status.
        /// </summary>
        private void _ResetStatus()
        {
            // Reset internal status.
        	m_CurrentMode = ApplicationMode.Ready;
        	m_WorldOrigin = null;
        	IsOriginPlaced = false;
        }

        /// <summary>
        /// Handles active screen switch events.
        /// </summary>
        /// <param name="activeScreen">Current active screen.</param>
        private void _SwitchActiveScreen(ActiveScreen activeScreen)
        {
        	LobbyScreen.SetActive(activeScreen == ActiveScreen.LobbyScreen);
        	StatusScreen.SetActive(activeScreen != ActiveScreen.StartScreen);
        	StartScreen.SetActive(activeScreen == ActiveScreen.StartScreen);

        	bool switchToARScreen = activeScreen == ActiveScreen.ARScreen;
        	ARScreen.SetActive(switchToARScreen);
        	SessionCore.SetActive(switchToARScreen);
        	ARExtentions.SetActive(switchToARScreen);

        	m_CurrentActiveScreen = activeScreen;

        	if (m_CurrentActiveScreen == ActiveScreen.StartScreen)
        	{
        		PlayerPrefs.SetInt(k_HasDisplayedStartInfoKey, 1);
        	}

        	if (m_CurrentActiveScreen == ActiveScreen.ARScreen)
        	{
                // Set platform active when switching to AR Screen so the camera permission only
                // shows after Start Screen.
        		m_TimeSinceStart = 0.0f;
        	}
        }

        /// <summary>
        /// Check and update the application lifecycle.
        /// </summary>
        private void _UpdateApplicationLifecycle()
        {
            // Exit the app when the 'back' button is pressed.
        	if (Input.GetKey(KeyCode.Escape))
        	{
        		Application.Quit();
        	}

        	var sleepTimeout = SleepTimeout.NeverSleep;
        	if (ARSession.state != ARSessionState.SessionTracking)
        	{
        		sleepTimeout = SleepTimeout.SystemSetting;
        	}

        	Screen.sleepTimeout = sleepTimeout;

        	if (m_IsQuitting)
        	{
        		return;
        	}

        	if (ARSession.state == ARSessionState.Unsupported)
        	{
        		_QuitWithReason("AR Experience is unsupported on this devices.");
        	}
        }

        /// <summary>
        /// Quits the application after 5 seconds for the toast to appear.
        /// </summary>
        /// <param name="reason">The reason of quitting the application.</param>
        private void _QuitWithReason(string reason)
        {
        	if (m_IsQuitting)
        	{
        		return;
        	}

        	NetworkUIController.ShowDebugMessage(reason);
        	m_IsQuitting = true;
        	Invoke("_DoQuit", 5.0f);
        }

        /// <summary>
        /// Returns to lobby after 3 seconds for the reason message to appear.
        /// </summary>
        /// <param name="reason">The reason of returning to lobby.</param>
        private void _ReturnToLobbyWithReason(string reason)
        {
            // No need to return if the application is currently quitting.
        	if (m_IsQuitting)
        	{
        		return;
        	}

        	NetworkUIController.ShowDebugMessage(reason);
        	Invoke("_DoReturnToLobby", 3.0f);
        }

        /// <summary>
        /// Actually quit the application.
        /// </summary>
        private void _DoQuit()
        {
        	Application.Quit();
        }

        /// <summary>
        /// Actually return to lobby scene.
        /// </summary>
        private void _DoReturnToLobby()
        {
#pragma warning disable 618
        	NetworkManager.Shutdown();
#pragma warning restore 618
        	SceneManager.LoadScene("CloudAnchors");
        }
    }
}
