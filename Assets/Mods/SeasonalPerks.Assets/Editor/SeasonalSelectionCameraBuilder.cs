using System.IO;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

public static class SeasonalSelectionCameraBuilder
{
    private const string Root = "Assets/Mods/SeasonalPerks.Assets";

    public static string Build()
    {
        var root = Recover(
            JObject.Parse(File.ReadAllText(Root + "/Recovered/selection-camera.json")),
            null
        );
        var path = Root + "/UI/selection-camera.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return path;
    }

    private static GameObject Recover(JObject node, Transform parent)
    {
        var go = new GameObject((string)node["name"]);
        go.transform.SetParent(parent, false);
        var transform = node["transform"];
        go.transform.localPosition = V(transform["m_LocalPosition"]);
        go.transform.localScale = V(transform["m_LocalScale"]);
        var rotation = transform["m_LocalRotation"];
        go.transform.localRotation = new Quaternion(
            (float)rotation["x"],
            (float)rotation["y"],
            (float)rotation["z"],
            (float)rotation["w"]
        );
        foreach (var component in node["components"])
        {
            var fields = component["fields"];
            if ((string)component["type"] == "Camera")
            {
                var camera = go.AddComponent<Camera>();
                camera.clearFlags = (CameraClearFlags)(int)fields["m_ClearFlags"];
                camera.backgroundColor = C(fields["m_BackGroundColor"]);
                camera.nearClipPlane = (float)fields["near clip plane"];
                camera.farClipPlane = (float)fields["far clip plane"];
                camera.fieldOfView = (float)fields["field of view"];
                camera.cullingMask = (int)fields["m_CullingMask"]["m_Bits"];
                camera.allowHDR = false;
                camera.allowMSAA = false;
                camera.useOcclusionCulling = false;
                camera.enabled = false;
            }
            if ((string)component["type"] == "Light")
            {
                var light = go.AddComponent<Light>();
                light.type = (LightType)(int)fields["m_Type"];
                light.color = C(fields["m_Color"]);
                light.intensity = (float)fields["m_Intensity"];
                light.range = (float)fields["m_Range"];
                light.spotAngle = (float)fields["m_SpotAngle"];
                light.cullingMask = 1 << 19;
                light.shadows = LightShadows.None;
            }
        }
        foreach (JObject child in node["children"])
            Recover(child, go.transform);
        return go;
    }

    private static Vector3 V(JToken value) =>
        new Vector3((float)value["x"], (float)value["y"], (float)value["z"]);

    private static Color C(JToken value) =>
        new Color((float)value["r"], (float)value["g"], (float)value["b"], (float)value["a"]);
}
