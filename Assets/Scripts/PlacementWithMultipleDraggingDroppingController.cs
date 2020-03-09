using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(ARRaycastManager))]
public class PlacementWithMultipleDraggingDroppingController : MonoBehaviour
{
    [SerializeField]
    private GameObject placedPrefab;

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
    private Button redButton, greenButton, blueButton;

    private GameObject PlacedPrefab 
    {
        get 
        {
            return placedPrefab;
        }
        set 
        {
            placedPrefab = value;
        }
    }

    private Vector3 pushup;
    private Vector3 orientation;

    void Awake() 
    {
        arRaycastManager = GetComponent<ARRaycastManager>();
        dismissButton.onClick.AddListener(Dismiss);

        if(redButton != null && greenButton != null && blueButton != null) 
        {
            redButton.onClick.AddListener(() => ChangePrefabSelection("Domino"));
            greenButton.onClick.AddListener(() => ChangePrefabSelection("LightMarble"));
            blueButton.onClick.AddListener(() => ChangePrefabSelection("LowRamp"));
        }
    }

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

    private void Dismiss() => welcomePanel.SetActive(false);

    void Update()
    {
        // do not capture events unless the welcome panel is hidden
        if(welcomePanel.activeSelf)
            return;

        if(Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            touchPosition = touch.position;

            if(touch.phase == TouchPhase.Began)
            {
                Ray ray = arCamera.ScreenPointToRay(touch.position);
                RaycastHit hitObject;
                if(Physics.Raycast(ray, out hitObject))
                {
                    lastSelectedObject = hitObject.transform.GetComponent<PlacementObject>();
                    if(lastSelectedObject != null)
                    {
                        PlacementObject[] allOtherObjects = FindObjectsOfType<PlacementObject>();
                        foreach(PlacementObject placementObject in allOtherObjects)
                        {
                            placementObject.Selected = placementObject == lastSelectedObject;
                        }
                    }
                }
            }

            if(touch.phase == TouchPhase.Ended)
            {
                lastSelectedObject.Selected = false;
            }
        }

        if(arRaycastManager.Raycast(touchPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;
            pushup = new Vector3(0f,placedPrefab.GetComponent<Renderer>().bounds.size.y/2.001f,0f);

            var nearestDomino = Domino.FindClosestDomino(hitPose.position+pushup);
            
            
            if(lastSelectedObject == null)
            {   
                
                if(nearestDomino == null) {
                
                lastSelectedObject = Instantiate(placedPrefab, hitPose.position+pushup, hitPose.rotation).GetComponent<PlacementObject>();

                }
                else{
                
                var nearestDominoDirection = nearestDomino.transform.position - (hitPose.position+pushup);
                lastSelectedObject = Instantiate(placedPrefab, hitPose.position+pushup, Quaternion.LookRotation(nearestDominoDirection)).GetComponent<PlacementObject>();
                
                }
            }
            else 
            {
                if(lastSelectedObject.Selected)
                {
                    lastSelectedObject.transform.position = hitPose.position+pushup;

                }
            }
        }
    }
}
