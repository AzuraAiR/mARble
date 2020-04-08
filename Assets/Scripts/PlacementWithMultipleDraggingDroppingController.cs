using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARRaycastManager))]
public class PlacementWithMultipleDraggingDroppingController : MonoBehaviour {
    public static PlacementWithMultipleDraggingDroppingController Instance;

    [SerializeField]
    private GameObject currSelectedPrefab;

    [SerializeField]
    private GameObject welcomePanel;

    [SerializeField]
    private Button dismissButton;

    [SerializeField]
    private Camera arCamera;

    private PlacementObject[] placedObjects;
    private Vector2 touchPosition = default;
    private ARRaycastManager arRaycastManager;
    private ARReferencePointManager arReferencePointManager;
    private bool onTouchHold = false;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private PlacementObject lastSelectedObject;

    [SerializeField]
    private Button DominoButton, RampButton, SpiralRampButton, MarbleButton;

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

    private GameObject PlacedPrefab {
        get { return currSelectedPrefab; }
        set { currSelectedPrefab = value; }
    }

    private Vector3 orientation;

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else if (Instance != this) {
            Destroy(gameObject);
        }

        arRaycastManager = GetComponent<ARRaycastManager>();
        arReferencePointManager = GetComponent<ARReferencePointManager>();
        dismissButton.onClick.AddListener(Dismiss);

        if(DominoButton != null && MarbleButton != null && RampButton != null) {
            DominoButton.onClick.AddListener(() => ChangePrefabSelection("Domino"));
            RampButton.onClick.AddListener(() => ChangePrefabSelection("Limacon"));
            SpiralRampButton.onClick.AddListener(() => ChangePrefabSelection("Hypotrochoid"));
            MarbleButton.onClick.AddListener(() => ChangePrefabSelection("Marble"));
        }

        currEditType = EditType.EDIT_ROTATION;
        editTypeButton.onClick.AddListener(SwitchEditType);
        editSlider.onValueChanged.AddListener(EditSliderChanged);
    }

    private void ChangePrefabSelection(string name) {
        GameObject loadedGameObject = Resources.Load<GameObject>($"Prefabs/{name}");
        if(loadedGameObject != null) {
            PlacedPrefab = loadedGameObject;
            Debug.Log($"Game object with name {name} was loaded");
        } else {
            Debug.Log($"Unable to find a game object with name {name}");
        }
    }

    private void Dismiss() => welcomePanel.SetActive(false);

    void Update() {
        // do not capture events unless the welcome panel is hidden
        if(welcomePanel.activeSelf) {
            return;
        }

        if(Input.touchCount > 0) {
            Touch touch = Input.GetTouch(0);
            touchPosition = touch.position;

            if(touch.phase == TouchPhase.Began) {
                Ray ray = arCamera.ScreenPointToRay(touch.position);
                RaycastHit hitObject;

                // Sets item touched as Selected Object
                if(Physics.Raycast(ray, out hitObject)) {
                    lastSelectedObject = hitObject.transform.GetComponent<PlacementObject>();
                    if(lastSelectedObject != null) {
                        placedObjects = FindObjectsOfType<PlacementObject>();
                        foreach(PlacementObject placementObject in placedObjects) {
                            placementObject.Selected = placementObject == lastSelectedObject;
                        }
                    }
                }
            }

            if(touch.phase == TouchPhase.Ended) {
                lastSelectedObject.Selected = false;
            }
        }

        if(arRaycastManager.Raycast(touchPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon)) {
            Pose hitPose = hits[0].pose;
            // yOffset to prevent object clipping through AR plane
            Vector3 yOffsetPrefab = new Vector3(0f, currSelectedPrefab.GetComponent<Renderer>().bounds.size.y/2.001f, 0f);
            
            
            


            // Instantiate selected object
            if(lastSelectedObject == null && ModeManager.Instance.GetCurrMode() == GameMode.PLACEMENT_MODE) {

                if (currSelectedPrefab.tag == "Domino") {
                    SpawnDomino(yOffsetPrefab, hitPose);
                } else {
                    lastSelectedObject = Instantiate(currSelectedPrefab, hitPose.position, hitPose.rotation).GetComponent<PlacementObject>();
                }
                arReferencePointManager.AddReferencePoint(hitPose);
        //  we don't use the + yOffsetPrefab anymore

            } else {
                // Move selected object
                if(lastSelectedObject.Selected) {
                    Vector3 yOffsetSelectedObject = new Vector3(0f, lastSelectedObject.GetComponent<Renderer>().bounds.size.y/2.001f, 0f);

                    if (lastSelectedObject.tag == "Marble"){
                        float snapDist = 0.05f;
                        MoveMarble(yOffsetSelectedObject, hitPose, snapDist);
                    } else {
                        lastSelectedObject.transform.position = hitPose.position;
                        //  we don't use the + yOffsetSelectedObject anymore
                    }
                }
            }
        }

    }

    private void SwitchEditType() {
        switch (currEditType) {
            case EditType.EDIT_ROTATION:
                currEditType = EditType.EDIT_SCALE;
                editTypeButton.GetComponentInChildren<Text>().text = "Change Scale";
                break;
            case EditType.EDIT_SCALE:
                currEditType = EditType.EDIT_ROTATION;
                editTypeButton.GetComponentInChildren<Text>().text = "Change Rotation";
                break;
        }
    }
    private void EditSliderChanged(float sliderValue) {
        switch (currEditType) {
            case EditType.EDIT_ROTATION:
                int rotationDegree = (int)(editSlider.normalizedValue * 360);
                if(lastSelectedObject != null) {
                    lastSelectedObject.transform.localEulerAngles = new Vector3(0, rotationDegree, 0);
                }
                editTextValue.text = $"Rotation: {rotationDegree} degrees";
                break;

                // TODO: all prefabs must be in a parent gameobject with scale (1,1,1)
                // in order to scale properly while maintaining prefabs's current scale ratios
            case EditType.EDIT_SCALE:
                /* if(lastSelectedObject != null) {
                    lastSelectedObject.transform.localScale = new Vector3(sliderValue, sliderValue, sliderValue));
                } */
                editTextValue.text = $"Scale: {sliderValue}";
                break;
        }
    }

    private void SpawnDomino(Vector3 yOffset, Pose hitPose){
        var nearestDomino = Domino.FindClosestDomino(hitPose.position);
        if (nearestDomino == null) {
            lastSelectedObject = Instantiate(currSelectedPrefab, hitPose.position, hitPose.rotation).GetComponent<PlacementObject>();
        } else {            
            var nearestDominoDirection = nearestDomino.transform.position - (hitPose.position);
            lastSelectedObject = Instantiate(currSelectedPrefab, hitPose.position, Quaternion.LookRotation(nearestDominoDirection)).GetComponent<PlacementObject>();
        }
    }

    private void MoveMarble(Vector3 yOffset, Pose hitPose, float snapDistance){
        //find nearest ramp
        var nearestRamp = Ramp.FindClosestRamp(hitPose.position);
        if (nearestRamp == null) {
            lastSelectedObject.transform.position = hitPose.position;
        }
        //if within snap zone, snap to snap zone on ramp
        else {
            var nearestRampDirection = nearestRamp.transform.position - hitPose.position;
            float nearestRampHorizontalDistance = Mathf.Sqrt(Mathf.Pow(nearestRampDirection.x, 2) + Mathf.Pow(nearestRampDirection.y, 2));
            if (nearestRampHorizontalDistance < snapDistance) {
                lastSelectedObject.transform.position = nearestRamp.transform.Find("SnapZone").position;
                lastSelectedObject.transform.rotation = nearestRamp.transform.Find("SnapZone").rotation; // Need to freeze Marble after it Snaps into place
            } else {
                lastSelectedObject.transform.position = hitPose.position;
            }

        }
    }

}
