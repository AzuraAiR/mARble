using System.Collections;
using System.Collections.Generic;
using System.IO;
 using System.Runtime.Serialization.Formatters.Binary;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#if UNITY_IOS
using UnityEngine.XR.ARKit;
#endif

/// <summary>
/// Demonstrates the saving and loading of an
/// <a href="https://developer.apple.com/documentation/arkit/arworldmap">ARWorldMap</a>
/// </summary>
/// <remarks>
/// ARWorldMaps are only supported by ARKit, so this API is in the
/// <c>UntyEngine.XR.ARKit</c> namespace.
/// </remarks>
public class ARWorldMapController : MonoBehaviour
{
    [Tooltip("The ARSession component controlling the session from which to generate ARWorldMaps.")]
    [SerializeField]
    ARSession m_ARSession;

    /// <summary>
    /// The ARSession component controlling the session from which to generate ARWorldMaps.
    /// </summary>
    public ARSession arSession
    {
        get { return m_ARSession; }
        set { m_ARSession = value; }
    }

    [Tooltip("UI Text component to display error messages")]
    [SerializeField]
    Text m_ErrorText;

    /// <summary>
    /// The UI Text component used to display error messages
    /// </summary>
    public Text errorText
    {
        get { return m_ErrorText; }
        set { m_ErrorText = value; }
    }

    [Tooltip("The UI Text element used to display log messages.")]
    [SerializeField]
    Text m_LogText;

    /// <summary>
    /// The UI Text element used to display log messages.
    /// </summary>
    public Text logText
    {
        get { return m_LogText; }
        set { m_LogText = value; }
    }

    [Tooltip("The UI Text element used to display the current AR world mapping status.")]
    [SerializeField]
    Text m_MappingStatusText;

    /// <summary>
    /// The UI Text element used to display the current AR world mapping status.
    /// </summary>
    public Text mappingStatusText
    {
        get { return m_MappingStatusText; }
        set { m_MappingStatusText = value; }
    }

    [Tooltip("A UI button component which will generate an ARWorldMap and save it to disk.")]
    [SerializeField]
    Button m_SaveButton;

    /// <summary>
    /// A UI button component which will generate an ARWorldMap and save it to disk.
    /// </summary>
    public Button saveButton
    {
        get { return m_SaveButton; }
        set { m_SaveButton = value; }
    }

    [Tooltip("A UI button component which will load a previously saved ARWorldMap from disk and apply it to the current session.")]
    [SerializeField]
    Button m_LoadButton;

    /// <summary>
    /// A UI button component which will load a previously saved ARWorldMap from disk and apply it to the current session.
    /// </summary>
    public Button loadButton
    {
        get { return m_LoadButton; }
        set { m_LoadButton = value; }
    }

    /// <summary>
    /// Create an <c>ARWorldMap</c> and save it to disk.
    /// </summary>
    public void OnSaveButton()
    {
#if UNITY_IOS
        StartCoroutine(Save());
        SaveVirtualObjects();
#endif
    }

    /// <summary>
    /// Load an <c>ARWorldMap</c> from disk and apply it
    /// to the current session.
    /// </summary>
    public void OnLoadButton()
    {
#if UNITY_IOS
        StartCoroutine(Load());
        ClearVirtualObjects();
        LoadVirtualObjects();
#endif
    }

    /// <summary>
    /// Reset the <c>ARSession</c>, destroying any existing trackables,
    /// such as planes. Upon loading a saved <c>ARWorldMap</c>, saved
    /// trackables will be restored.
    /// </summary>
    public void OnResetButton()
    {
        m_ARSession.Reset();
        ClearVirtualObjects();
    }


    public void ClearVirtualObjects()
    {
        List<GameObject> taggedObjects = new List<GameObject>();
        taggedObjects.AddRange(GameObject.FindGameObjectsWithTag("Marble"));
        taggedObjects.AddRange(GameObject.FindGameObjectsWithTag("Domino"));
        taggedObjects.AddRange(GameObject.FindGameObjectsWithTag("Ramp"));

        foreach(GameObject tagObject in taggedObjects) {
            Destroy(tagObject);
        }
    }

    public void SaveVirtualObjects()
    {
        Log("Starting the save for virtual objects");
        Save saveFile = new Save();
        // Find all virtual objects and collate into a list
        List<GameObject> taggedObjects = new List<GameObject>();
        taggedObjects.AddRange(GameObject.FindGameObjectsWithTag("Marble"));
        taggedObjects.AddRange(GameObject.FindGameObjectsWithTag("Domino"));
        taggedObjects.AddRange(GameObject.FindGameObjectsWithTag("Ramp"));
        // Extract variables and add into save file
        foreach(GameObject tagObject in taggedObjects) {
            // Freeze before saving
            tagObject.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            tagObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
            saveFile.objectTransforms.Add(tagObject.transform.position);
            saveFile.objectRotations.Add(tagObject.transform.rotation);
            saveFile.objectScales.Add(tagObject.transform.localScale);
            saveFile.objectTags.Add(tagObject.tag);
        }
        // Format and serialize the variables
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(Application.persistentDataPath + "/objects.save");
        bf.Serialize(file, saveFile);
        file.Close();
        Log("Virtual objects successfully saved");
    }

    public void LoadVirtualObjects()
    { 
      Log("Starting the load for virtual objects");
      if (File.Exists(Application.persistentDataPath + "/objects.save"))
      {
        // Deserialize save file
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Open(Application.persistentDataPath + "/objects.save", FileMode.Open);
        Save saveFile = (Save) bf.Deserialize(file);
        file.Close();
        if (saveFile.objectTransforms.Count == 0){
            return;
        }
        // Recreate virtual objects
        string name = saveFile.objectTags[0];
        GameObject loadedGameObject = Resources.Load<GameObject>($"Prefabs/{name}");
        for(int i = 0; i< saveFile.objectTransforms.Count; i++)
        {
            // If tag is not the same, load new prefab
            if (saveFile.objectTags[i] != name) {
                name = saveFile.objectTags[i];
                loadedGameObject = Resources.Load<GameObject>($"Prefabs/{name}");
            }
            // Create game object, resize and freeze it
            GameObject newObject = Instantiate(loadedGameObject, saveFile.objectTransforms[i], saveFile.objectRotations[i]);
            newObject.transform.localScale = saveFile.objectScales[i];
            newObject.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
            newObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
        Log("Successfully replicated Dominoes");
  }
  else
    {
    Log("No save file found!");
    }
}


#if UNITY_IOS
IEnumerator Save()
{
    var sessionSubsystem = (ARKitSessionSubsystem)m_ARSession.subsystem;
    if (sessionSubsystem == null)
    {
        Log("No session subsystem available. Could not save.");
        yield break;
    }

    var request = sessionSubsystem.GetARWorldMapAsync();

    while (!request.status.IsDone())
    yield return null;

    if (request.status.IsError())
    {
        Log(string.Format("Session serialization failed with status {0}", request.status));
        yield break;
    }

    var worldMap = request.GetWorldMap();
    request.Dispose();

    SaveAndDisposeWorldMap(worldMap);
}

IEnumerator Load()
{
    var sessionSubsystem = (ARKitSessionSubsystem)m_ARSession.subsystem;
    if (sessionSubsystem == null)
    {
        Log("No session subsystem available. Could not load.");
        yield break;
    }

    var file = File.Open(path, FileMode.Open);
    if (file == null)
    {
        Log(string.Format("File {0} does not exist.", path));
        yield break;
    }

    Log(string.Format("Reading {0}...", path));

    int bytesPerFrame = 1024 * 10;
    var bytesRemaining = file.Length;
    var binaryReader = new BinaryReader(file);
    var allBytes = new List<byte>();
    while (bytesRemaining > 0)
    {
        var bytes = binaryReader.ReadBytes(bytesPerFrame);
        allBytes.AddRange(bytes);
        bytesRemaining -= bytesPerFrame;
        yield return null;
    }

    var data = new NativeArray<byte>(allBytes.Count, Allocator.Temp);
    data.CopyFrom(allBytes.ToArray());

    Log(string.Format("Deserializing to ARWorldMap...", path));
    ARWorldMap worldMap;
    if (ARWorldMap.TryDeserialize(data, out worldMap))
    data.Dispose();

    if (worldMap.valid)
    {
        Log("Deserialized successfully.");
        List<GameObject> taggedObjects = new List<GameObject>();
        taggedObjects.AddRange(GameObject.FindGameObjectsWithTag("Marble"));
        taggedObjects.AddRange(GameObject.FindGameObjectsWithTag("Domino"));
        taggedObjects.AddRange(GameObject.FindGameObjectsWithTag("Ramp"));

        foreach(GameObject tagObject in taggedObjects) {
            Log("Found object");
        }
    }
    else
    {
        Debug.LogError("Data is not a valid ARWorldMap.");
        yield break;
    }

    Log("Apply ARWorldMap to current session.");
    sessionSubsystem.ApplyWorldMap(worldMap);
}

void SaveAndDisposeWorldMap(ARWorldMap worldMap)
{
    Log("Serializing ARWorldMap to byte array...");
    var data = worldMap.Serialize(Allocator.Temp);
    Log(string.Format("ARWorldMap has {0} bytes.", data.Length));

    var file = File.Open(path, FileMode.Create);
    var writer = new BinaryWriter(file);
    writer.Write(data.ToArray());
    writer.Close();
    data.Dispose();
    worldMap.Dispose();
    Log(string.Format("ARWorldMap written to {0}", path));
}
#endif

string path
{
    get
    {
        return Path.Combine(Application.persistentDataPath, "my_session.worldmap");
    }
}

bool supported
{
    get
    {
#if UNITY_IOS
        return m_ARSession.subsystem is ARKitSessionSubsystem && ARKitSessionSubsystem.worldMapSupported;
#else
        return false;
#endif
    }
}

void Awake()
{
    m_LogMessages = new List<string>();
}

void Log(string logMessage)
{
    m_LogMessages.Add(logMessage);
}

static void SetActive(Button button, bool active)
{
    if (button != null)
    button.gameObject.SetActive(active);
}

static void SetActive(Text text, bool active)
{
    if (text != null)
    text.gameObject.SetActive(active);
}

static void SetText(Text text, string value)
{
    if (text != null)
    text.text = value;
}

void Update()
{
    if (supported)
    {
        SetActive(errorText, false);
        SetActive(saveButton, true);
        SetActive(loadButton, true);
        SetActive(mappingStatusText, true);
    }
    else
    {
        SetActive(errorText, true);
        SetActive(saveButton, false);
        SetActive(loadButton, false);
        SetActive(mappingStatusText, false);
    }

#if UNITY_IOS
    var sessionSubsystem = (ARKitSessionSubsystem)m_ARSession.subsystem;
#else
    XRSessionSubsystem sessionSubsystem = null;
#endif
    if (sessionSubsystem == null)
    return;

    var numLogsToShow = 20;
    string msg = "";
    for (int i = Mathf.Max(0, m_LogMessages.Count - numLogsToShow); i < m_LogMessages.Count; ++i)
    {
        msg += m_LogMessages[i];
        msg += "\n";
    }
    SetText(logText, msg);

#if UNITY_IOS
    SetText(mappingStatusText, string.Format("Mapping Status: {0}", sessionSubsystem.worldMappingStatus));
#endif
}

List<string> m_LogMessages;
}