using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum GameMode{
    PLACEMENT_MODE,
    EDIT_MODE
}

// In charge of displaying the correct UI based on the current mode
public class ModeManager : MonoBehaviour {
    public static ModeManager Instance;
    private GameMode currMode;

    [SerializeField]
    private Text currModeText;

    [Header("Placement mode UI")]
    [SerializeField]
    private GameObject[] placementmodeUI = null;

    [Header("Edit mode UI")]
    [SerializeField]
    private GameObject[] editmodeUI = null;    

    // Start is called before the first frame update
    void Start() {
        if (Instance == null) {
            Instance = this;
        } else if (Instance != this) {
            Destroy(gameObject);
        }

        currMode = GameMode.PLACEMENT_MODE;
        currModeText.text = "Placement Mode";
        ShowPlacementUI();
    }

    public void ToggleGameMode() {
        currMode = (currMode == GameMode.PLACEMENT_MODE) ? GameMode.EDIT_MODE : GameMode.PLACEMENT_MODE;

        switch (currMode) {
            case GameMode.PLACEMENT_MODE:
                currModeText.text = "Placement Mode";
                ShowPlacementUI();
                break;
            case GameMode.EDIT_MODE:
                currModeText.text = "Edit Mode";
                ShowEditUI();
                break;
        }
    }

    public GameMode GetCurrMode() {
        return currMode;
    }

    private void ShowPlacementUI() {
        foreach (GameObject go in editmodeUI) {
            go.SetActive(false);
        }

        foreach (GameObject go in placementmodeUI) {
            go.SetActive(true);
        }
    }

    private void ShowEditUI() {
        foreach (GameObject go in placementmodeUI) {
            go.SetActive(false);
        }

        foreach (GameObject go in editmodeUI) {
            go.SetActive(true);
        }
    }
}
