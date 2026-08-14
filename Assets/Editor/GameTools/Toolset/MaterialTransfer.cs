using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class MaterialTransferEditor : EditorWindow
{
    private GameObject sourceObject;
    private GameObject targetObject;
    private bool transferShader = true;
    private bool transferProperties = true;
    private bool createNewMaterials = false;

    [MenuItem("SDK/Game Tools/Material Transfer")]
    public static void ShowWindow()
    {
        GetWindow<MaterialTransferEditor>("Material Transfer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Material Transfer Tool", EditorStyles.boldLabel);

        sourceObject =
            EditorGUILayout.ObjectField("Source Object (A)", sourceObject, typeof(GameObject), true)
            as GameObject;
        targetObject =
            EditorGUILayout.ObjectField("Target Object (B)", targetObject, typeof(GameObject), true)
            as GameObject;

        transferShader = EditorGUILayout.Toggle("Transfer Shader", transferShader);
        transferProperties = EditorGUILayout.Toggle("Transfer Properties", transferProperties);
        createNewMaterials = EditorGUILayout.Toggle(
            "Create New Materials (Copy)",
            createNewMaterials
        );

        GUILayout.Space(10);

        if (GUILayout.Button("Transfer Materials", GUILayout.Height(40)))
        {
            if (sourceObject == null || targetObject == null)
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Both Source and Target objects must be assigned.",
                    "OK"
                );
                return;
            }

            TransferMaterials(
                sourceObject,
                targetObject,
                transferShader,
                transferProperties,
                createNewMaterials
            );
        }
    }

    // Public static accessor
    public static void TransferMaterials(
        GameObject source,
        GameObject target,
        bool transferShader,
        bool transferProperties,
        bool createNewMaterials
    )
    {
        if (source == null || target == null)
        {
            EditorUtility.DisplayDialog("Error", "Source and Target cannot be null.", "OK");
            return;
        }

        Dictionary<string, Material> sourceMaterialDict = new Dictionary<string, Material>();
        Renderer[] sourceRenderers = source.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer srcRenderer in sourceRenderers)
        {
            foreach (Material m in srcRenderer.sharedMaterials)
            {
                if (m != null && !sourceMaterialDict.ContainsKey(m.name))
                {
                    sourceMaterialDict.Add(m.name, m);
                }
            }
        }

        Renderer[] targetRenderers = target.GetComponentsInChildren<Renderer>(true);
        int transferCount = 0;

        foreach (Renderer targetRenderer in targetRenderers)
        {
            Material[] targetMaterials = targetRenderer.sharedMaterials;
            bool changed = false;

            for (int i = 0; i < targetMaterials.Length; i++)
            {
                Material targetMat = targetMaterials[i];
                if (targetMat == null)
                    continue;

                if (sourceMaterialDict.TryGetValue(targetMat.name, out Material sourceMat))
                {
                    Material newMat = createNewMaterials ? new Material(sourceMat) : sourceMat;

                    if (transferShader)
                    {
                        newMat.shader = sourceMat.shader;
                    }

                    if (transferProperties)
                    {
                        CopyMaterialProperties(sourceMat, newMat);
                    }

                    targetMaterials[i] = newMat;
                    changed = true;
                    transferCount++;
                }
            }

            if (changed)
            {
                targetRenderer.sharedMaterials = targetMaterials;
                EditorUtility.SetDirty(targetRenderer);
            }
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog(
            "Success",
            $"Transferred {transferCount} materials by name.",
            "OK"
        );
    }

    private static void CopyMaterialProperties(Material source, Material target)
    {
#if UNITY_EDITOR
        int propertyCount = ShaderUtil.GetPropertyCount(source.shader);

        for (int i = 0; i < propertyCount; i++)
        {
            string name = ShaderUtil.GetPropertyName(source.shader, i);
            ShaderUtil.ShaderPropertyType type = ShaderUtil.GetPropertyType(source.shader, i);

            try
            {
                switch (type)
                {
                    case ShaderUtil.ShaderPropertyType.Color:
                        target.SetColor(name, source.GetColor(name));
                        break;

                    case ShaderUtil.ShaderPropertyType.Vector:
                        target.SetVector(name, source.GetVector(name));
                        break;

                    case ShaderUtil.ShaderPropertyType.Float:
                    case ShaderUtil.ShaderPropertyType.Range:
                        target.SetFloat(name, source.GetFloat(name));
                        break;

                    case ShaderUtil.ShaderPropertyType.TexEnv:
                        target.SetTexture(name, source.GetTexture(name));
                        target.SetTextureOffset(name, source.GetTextureOffset(name));
                        target.SetTextureScale(name, source.GetTextureScale(name));
                        break;
                }
            }
            catch { }
        }
#endif
    }
}
