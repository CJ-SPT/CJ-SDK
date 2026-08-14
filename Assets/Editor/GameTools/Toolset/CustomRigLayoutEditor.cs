using System.Collections.Generic;
using System.IO;
using System.Reflection;
using EFT.UI.DragAndDrop;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class CustomRigEditor : EditorWindow
{
    private GameObject customRigGameObject;
    private List<GridView> gridViews = new List<GridView>();
    private List<int> cellWidths = new List<int>();
    private List<int> cellHeights = new List<int>();

    [MenuItem("SDK/Game Tools/Custom Rig Editor")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(CustomRigEditor));
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(
            "This tool configures the layout of your custom rig, and optionally exports the grid layout to a JSON file.",
            EditorStyles.wordWrappedLabel
        );
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(
            "Select your main Custom Rig GameObject before continuing.",
            EditorStyles.wordWrappedLabel
        );
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        customRigGameObject = Selection.activeGameObject;

        if (customRigGameObject == null)
        {
            EditorGUILayout.HelpBox(
                "Please select a Custom Rig GameObject in the Hierarchy.",
                MessageType.Info
            );
            return;
        }

        TemplatedGridsView templatedGridsView =
            customRigGameObject.GetComponent<TemplatedGridsView>();
        if (templatedGridsView == null)
        {
            EditorGUILayout.HelpBox(
                "The selected Custom Rig GameObject does not have a TemplatedGridsView component.",
                MessageType.Error
            );
            return;
        }

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(
            "Renames all grid slots to GridView (n), fills out Templated Grids View script, checks the size of the layout and compares it to the Rect Transform, setting them to the same value.",
            EditorStyles.wordWrappedLabel
        );
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        if (GUILayout.Button("Configure Rig Layout"))
        {
            ConfigureRig(customRigGameObject, gridViews, cellWidths, cellHeights);
        }

        EditorGUILayout.Space();

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(
            "Exports the grid layout to a JSON file, located in the Assets/CustomRigLayouts/Grids folder.",
            EditorStyles.wordWrappedLabel
        );
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        if (GUILayout.Button("Export Grid"))
        {
            ExportJSON(customRigGameObject, gridViews, cellWidths, cellHeights);
        }
    }

    // Public static accessor
    public static void ConfigureRig(
        GameObject customRigGameObject,
        List<GridView> gridViews,
        List<int> cellWidths,
        List<int> cellHeights
    )
    {
        if (customRigGameObject == null)
        {
            Debug.LogError("Custom Rig GameObject not set!");
            return;
        }

        gridViews.Clear();
        cellWidths.Clear();
        cellHeights.Clear();

        int childCount = customRigGameObject.transform.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = customRigGameObject.transform.GetChild(i);
            child.name = "GridView (" + (i + 1) + ")";

            GridView gridView = child.GetComponent<GridView>();
            gridViews.Add(gridView);

            RectTransform rectTransform = child.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                int cellsH = CalculateNearestValidSlotSize(
                    Mathf.RoundToInt(rectTransform.sizeDelta.x)
                );
                int cellsV = CalculateNearestValidSlotSize(
                    Mathf.RoundToInt(rectTransform.sizeDelta.y)
                );

                cellWidths.Add(cellsH);
                cellHeights.Add(cellsV);

                Debug.Log("Slot " + (i + 1) + " size: " + cellsH + "x" + cellsV);
            }
            else
            {
                Debug.LogError("RectTransform component not found on child " + child.name);
            }
        }

        TemplatedGridsView templatedGridsView =
            customRigGameObject.GetComponent<TemplatedGridsView>();
        if (templatedGridsView != null)
        {
            FieldInfo fieldInfo = typeof(TemplatedGridsView).GetField(
                "_presetGridViews",
                BindingFlags.NonPublic | BindingFlags.Instance
            );

            if (fieldInfo != null)
            {
                fieldInfo.SetValue(templatedGridsView, gridViews.ToArray());
            }
            else
            {
                Debug.LogError("_presetGridViews field not found in TemplatedGridsView!");
                return;
            }
        }
        else
        {
            Debug.LogError("TemplatedGridsView component not found on Custom Rig GameObject!");
            return;
        }

        RectTransform mainRectTransform = customRigGameObject.GetComponent<RectTransform>();
        if (mainRectTransform != null)
        {
            mainRectTransform.sizeDelta = new Vector2(
                Mathf.Round(mainRectTransform.sizeDelta.x),
                Mathf.Round(mainRectTransform.sizeDelta.y)
            );

            LayoutElement layoutElement = customRigGameObject.GetComponent<LayoutElement>();
            if (layoutElement != null)
            {
                layoutElement.minWidth = mainRectTransform.sizeDelta.x;
                layoutElement.minHeight = mainRectTransform.sizeDelta.y;
            }
        }
        else
        {
            Debug.LogError("RectTransform component not found on Custom Rig GameObject!");
            return;
        }

        EditorUtility.DisplayDialog("Success", "Rig layout configured successfully!", "OK");
    }

    private static int CalculateNearestValidSlotSize(int size)
    {
        if (size >= 60 && size <= 68)
            return 1;
        if (size >= 124 && size <= 132)
            return 2;
        if (size >= 188 && size <= 196)
            return 3;
        if (size >= 248 && size <= 256)
            return 4;
        if (size >= 312 && size <= 320)
            return 5;
        if (size >= 372 && size <= 380)
            return 6;
        if (size >= 436 && size <= 444)
            return 7;
        if (size >= 496 && size <= 504)
            return 8;
        return size;
    }

    // Public static accessor
    public static void ExportJSON(
        GameObject customRigGameObject,
        List<GridView> gridViews,
        List<int> cellWidths,
        List<int> cellHeights
    )
    {
        if (customRigGameObject == null)
        {
            EditorUtility.DisplayDialog("Error", "Custom Rig GameObject not set!", "OK");
            return;
        }

        if (gridViews == null || gridViews.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Error",
                "No grid views to export. Configure the rig layout first.",
                "OK"
            );
            return;
        }

        RigData rigData = new RigData();
        rigData.Grids = new List<GridData>();

        for (int i = 0; i < gridViews.Count; i++)
        {
            GridView gridView = gridViews[i];
            GridData gridData = new GridData
            {
                _id = GenerateObjectId(),
                _name = gridView.gameObject.name,
                _parent = customRigGameObject.name,
                _props = new Props
                {
                    cellsH = cellWidths[i].ToString(),
                    cellsV = cellHeights[i].ToString(),
                    filters = new List<FilterData>
                    {
                        new FilterData
                        {
                            ExcludedFilter = new List<string> { "5448bf274bdc2dfc2f8b456a" },
                            Filter = new List<string> { "54009119af1c881c07000029" },
                        },
                    },
                    isSortingTable = false,
                    maxCount = 0,
                    maxWeight = 0,
                    minCount = 0,
                },
                _proto = "55d329c24bdc2d892f8b4567",
            };

            rigData.Grids.Add(gridData);
        }

        string jsonData = JsonUtility.ToJson(rigData, true);

        string directoryPath = Path.Combine(Application.dataPath, "CustomRigLayouts", "Grids");
        string fileName = customRigGameObject.name + "_layout.json";
        string filePath = Path.Combine(directoryPath, fileName);

        Directory.CreateDirectory(directoryPath);

        if (File.Exists(filePath))
        {
            bool overwriteConfirmed = EditorUtility.DisplayDialog(
                "Overwrite File",
                "A layout file with the same name already exists. Do you want to overwrite it?",
                "Yes",
                "No"
            );

            if (!overwriteConfirmed)
                return;
        }

        File.WriteAllText(filePath, jsonData);

        Debug.Log("JSON file exported to: " + filePath);
        EditorUtility.DisplayDialog("Success", "Grid layout exported successfully!", "OK");
    }

    private static string GenerateObjectId()
    {
        string hexChars = "0123456789abcdef";
        char[] objectId = new char[24];

        for (int i = 0; i < 24; i++)
        {
            objectId[i] = hexChars[UnityEngine.Random.Range(0, hexChars.Length)];
        }

        return new string(objectId);
    }

    [System.Serializable]
    public class RigData
    {
        public List<GridData> Grids;
    }

    [System.Serializable]
    public class GridData
    {
        public string _id;
        public string _name;
        public string _parent;
        public Props _props;
        public string _proto;
    }

    [System.Serializable]
    public class Props
    {
        public string cellsH;
        public string cellsV;
        public List<FilterData> filters;
        public bool isSortingTable;
        public int maxCount;
        public int maxWeight;
        public int minCount;
    }

    [System.Serializable]
    public class FilterData
    {
        public List<string> ExcludedFilter;
        public List<string> Filter;
    }
}
