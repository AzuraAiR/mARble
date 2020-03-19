using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARRaycastManager))]
public class PlacementWithMultipleDraggingDroppingController : MonoBehaviour {
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
    private bool onTouchHold = false;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private PlacementObject lastSelectedObject;

    [SerializeField]
    private Button DominoButton, RampButton, MarbleButton;

    private GameObject PlacedPrefab {
        get { return currSelectedPrefab; }
        set { currSelectedPrefab = value; }
    }

    private Vector3 orientation;

    void Awake() {
        arRaycastManager = GetComponent<ARRaycastManager>();
        dismissButton.onClick.AddListener(Dismiss);

        if(DominoButton != null && MarbleButton != null && RampButton != null) {
            DominoButton.onClick.AddListener(() => ChangePrefabSelection("Domino"));
            RampButton.onClick.AddListener(() => ChangePrefabSelection("SimpleRamp"));
            MarbleButton.onClick.AddListener(() => ChangePrefabSelection("LightMarble"));
        }
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

                if(Physics.Raycast(ray, out hitObject)) {
                    lastSelectedObject = hitObject.transform.GetComponent<PlacementObject>();
                    if(lastSelectedObject != null) {
                        PlacementObject[] allOtherObjects = FindObjectsOfType<PlacementObject>();
                        foreach(PlacementObject placementObject in allOtherObjects) {
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
            if(lastSelectedObject == null) {

                if (currSelectedPrefab.tag == "Domino") {
                    SpawnDomino(yOffsetPrefab, hitPose);
                } else {
                    lastSelectedObject = Instantiate(currSelectedPrefab, hitPose.position + yOffsetPrefab, hitPose.rotation).GetComponent<PlacementObject>();
                }
          

            } else {
                // Move selected object
                if(lastSelectedObject.Selected) {
                    Vector3 yOffsetSelectedObject = new Vector3(0f, lastSelectedObject.GetComponent<Renderer>().bounds.size.y/2.001f, 0f);

                    if (lastSelectedObject.tag == "Marble"){
                        float snapDist = 0.03f;
                        MoveMarble(yOffsetSelectedObject, hitPose, snapDist);
                    } else {
                        lastSelectedObject.transform.position = hitPose.position + yOffsetSelectedObject;
                    }
                }
            }
        }
    }

    private void SpawnDomino(Vector3 yOffset, Pose hitPose){
        var nearestDomino = Domino.FindClosestDomino(hitPose.position + yOffset);
        if (nearestDomino == null) {
            lastSelectedObject = Instantiate(currSelectedPrefab, hitPose.position + yOffset, hitPose.rotation).GetComponent<PlacementObject>();
        } else {            
            var nearestDominoDirection = nearestDomino.transform.position - (hitPose.position + yOffset);
            lastSelectedObject = Instantiate(currSelectedPrefab, hitPose.position + yOffset, Quaternion.LookRotation(nearestDominoDirection)).GetComponent<PlacementObject>();
        }
    }

    private void MoveMarble(Vector3 yOffset, Pose hitPose, float snapDistance){
        //find nearest ramp
        var nearestRamp = Ramp.FindClosestRamp(hitPose.position + yOffset);
        if (nearestRamp == null) {
            lastSelectedObject.transform.position = hitPose.position + yOffset;
        }
        //if within snap zone, snap to snap zone on ramp
        else {
            var nearestRampDirection = nearestRamp.transform.position - hitPose.position;
            float nearestRampHorizontalDistance = Mathf.Sqrt(Mathf.Pow(nearestRampDirection.x, 2) + Mathf.Pow(nearestRampDirection.y, 2));
            if (nearestRampHorizontalDistance < snapDistance) {
                lastSelectedObject.transform.position = nearestRamp.transform.Find("SnapZone").position; // Need to freeze Marble after it Snaps into place
            } else {
                lastSelectedObject.transform.position = hitPose.position + yOffset;
            }

        }
    }

}
