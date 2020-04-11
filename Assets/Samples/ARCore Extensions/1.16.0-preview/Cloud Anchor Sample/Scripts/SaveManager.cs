using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;

namespace Google.XR.ARCoreExtensions.Samples.CloudAnchors
{
    public class SaveManager : MonoBehaviour
    {	
    // Start is called before the first frame update
        void Start()
        {

        }

    // Update is called once per frame
        void Update()
        {

        }

        private List<GameObject> FindAllVirtualObjects()
        {
            List<GameObject> taggedObjects = new List<GameObject>();
            taggedObjects.AddRange(GameObject.FindGameObjectsWithTag("Marble"));
            taggedObjects.AddRange(GameObject.FindGameObjectsWithTag("Domino"));
            taggedObjects.AddRange(GameObject.FindGameObjectsWithTag("Ramp"));

            return taggedObjects;
        }


        public void ClearVirtualObjects()
        {
            GameObject.Find("LocalPlayer").GetComponent<LocalPlayerController>().CmdDespawnAll();
        }

        public void SaveVirtualObjects()
        {
            Debug.Log("Starting the save for virtual objects");
            Save saveFile = new Save();
        // Extract variables and add into save file
            foreach(GameObject tagObject in FindAllVirtualObjects()) 
            {
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
            Debug.Log("Virtual objects successfully saved");
        }

        public void LoadVirtualObjects()
        { 
            ClearVirtualObjects();
            Debug.Log("Starting the load for virtual objects");
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
                    if (saveFile.objectTags[i] != name) 
                    {
                        name = saveFile.objectTags[i];
                        loadedGameObject = Resources.Load<GameObject>($"Prefabs/{name}");
                    }
            // Create game object, resize and freeze it
                    GameObject.Find("LocalPlayer").GetComponent<LocalPlayerController>().CmdSpawnStar(saveFile.objectTransforms[i], saveFile.objectRotations[i], loadedGameObject);
                    GameObject newObject = CloudAnchorsExampleController.Instance.lastSelectedObject.gameObject;
                    newObject.transform.localScale = saveFile.objectScales[i];
                    newObject.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
                    newObject.GetComponent<Rigidbody>().velocity = Vector3.zero;
                }
                Debug.Log("Successfully replicated virtual objects");
            }
            else
            {
                Debug.Log("No save file found!");
            }
        }


    }
}
