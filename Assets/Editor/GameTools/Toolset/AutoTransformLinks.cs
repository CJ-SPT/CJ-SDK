using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class TransformLinksAutomation : EditorWindow
{
    private GameObject mainObject;
    private bool showErrors = false;

    [MenuItem("SDK/Game Tools/Transform Links Automation")]
    public static void ShowWindow()
    {
        GetWindow<TransformLinksAutomation>("Transform Links Automation");
    }

    private void OnGUI()
    {
        GUILayout.Label("Select the Main GameObject", EditorStyles.boldLabel);
        mainObject = (GameObject)EditorGUILayout.ObjectField(mainObject, typeof(GameObject), true);
        GUILayout.Space(10);

        if (GUILayout.Button("Apply Transform Links"))
        {
            ApplyTransformLinks(mainObject, showErrors);
        }

        showErrors = EditorGUILayout.Toggle("Show Errors", showErrors);
    }

    // Public static accessor
    public static void ApplyTransformLinks(GameObject mainObject, bool showErrors)
    {
        if (mainObject == null)
        {
            ShowErrorMessage("Please select a main GameObject.", showErrors);
            return;
        }

        TransformLinks transformLinks = mainObject.GetComponent<TransformLinks>();
        if (transformLinks == null)
        {
            transformLinks = mainObject.AddComponent<TransformLinks>();
        }

        List<string> boneNames = new List<string>
        {
            "Base HumanLCollarbone",
            "Base HumanRCollarbone",
            "Base HumanLPalm",
            "Base HumanRPalm",
            "Weapon_root_anim",
            "Weapon_root",
            "Bend_Goal_Left",
            "Bend_Goal_Right",
            "weapon_R_hand_marker",
            "weapon_R_IK_marker",
            "weapon_L_hand_marker",
            "weapon_L_IK_marker",
            "Camera_animated",
            "weapon",
            "Base HumanLUpperarm",
            "Base HumanLForearm1",
            "Base HumanRUpperarm",
            "Base HumanRForearm1",
            "weapon_vest_IK_marker",
        };

        Transform propBone = FindChildRecursively(mainObject.transform, "prop");
        if (propBone != null)
        {
            boneNames.Add("prop");
        }

        int transformCount = boneNames.Count;
        if (transformLinks.Transforms == null || transformLinks.Transforms.Length != transformCount)
        {
            transformLinks.Transforms = new Transform[transformCount];
        }

        for (int i = 0; i < transformCount; i++)
        {
            Transform bone = FindChildRecursively(mainObject.transform, boneNames[i]);
            if (bone != null)
            {
                transformLinks.Transforms[i] = bone;
            }
            else
            {
                transformLinks.Transforms[i] = null;
            }
        }

        transformLinks.Self = mainObject.transform;

        FieldInfo cachedTransformsField = typeof(TransformLinks).GetField(
            "_cachedTransforms",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        if (cachedTransformsField != null)
        {
            cachedTransformsField.SetValue(transformLinks, new TransformLinks.CachedTransform[3]);
        }
        else
        {
            ShowErrorMessage(
                "Failed to set _cachedTransforms field through reflection.",
                showErrors
            );
            return;
        }

        SetCachedTransform(mainObject, "Weapon_root", 0, showErrors);
        SetCachedTransform(mainObject, "Weapon_root_anim", 1, showErrors);
        SetCachedTransform(mainObject, "weapon", 2, showErrors);

        EditorUtility.SetDirty(transformLinks);
        EditorUtility.DisplayDialog(
            "Transform Links Applied",
            "Transform links have been configured successfully.",
            "OK"
        );
    }

    private static void SetCachedTransform(
        GameObject rootObject,
        string boneName,
        int index,
        bool showErrors
    )
    {
        Transform bone = FindChildRecursively(rootObject.transform, boneName);
        if (bone != null)
        {
            var cachedTransform = new TransformLinks.CachedTransform
            {
                Position = bone.localPosition,
                Transform = bone,
            };

            if (bone.localEulerAngles != Vector3.zero)
            {
                cachedTransform.Rotation = Quaternion.Euler(bone.localEulerAngles);
            }
            else
            {
                cachedTransform.Rotation = Quaternion.identity;
            }

            TransformLinks transformLinks = rootObject.GetComponent<TransformLinks>();
            if (transformLinks != null)
            {
                FieldInfo cachedTransformsField = typeof(TransformLinks).GetField(
                    "_cachedTransforms",
                    BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (cachedTransformsField != null)
                {
                    TransformLinks.CachedTransform[] cachedTransforms =
                        (TransformLinks.CachedTransform[])
                            cachedTransformsField.GetValue(transformLinks);

                    cachedTransforms[index] = cachedTransform;
                    cachedTransformsField.SetValue(transformLinks, cachedTransforms);
                }
                else
                {
                    ShowErrorMessage(
                        "Failed to set _cachedTransforms field through reflection.",
                        showErrors
                    );
                }
            }
        }
        else
        {
            ShowErrorMessage($"Bone '{boneName}' not found.", showErrors);
        }
    }

    private static Transform FindChildRecursively(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            foreach (Transform subChild in parent)
            {
                child = FindChildRecursively(subChild, name);
                if (child != null)
                {
                    break;
                }
            }
        }
        return child;
    }

    private static void ShowErrorMessage(string message, bool showErrors)
    {
        if (showErrors)
        {
            EditorUtility.DisplayDialog("Error", message, "OK");
        }
        else
        {
            Debug.LogError(message);
        }
    }
}
