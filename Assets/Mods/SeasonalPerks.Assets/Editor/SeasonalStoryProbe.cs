using System.IO;
using System.Linq;
using Unity.Plastic.Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
namespace SeasonalPerks.Tools
{
public static class SeasonalStoryProbe
{
 public static void Run()
 {
  string root="Assets/Mods/SeasonalPerks.Assets/";
  var shader=AssetDatabase.LoadAssetAtPath<Shader>(root+"StoryNativeShaders/84a698562226aa4b4d0583769a77e662e12e0f3710845d89afa90ecdc967b002.asset");
  Debug.Log("STORYPROBE shader: "+shader);
  if(shader) {AssetDatabase.TryGetGUIDAndLocalFileIdentifier(shader,out string guid,out long id);Debug.Log("STORYPROBE "+guid+" "+id+" supported="+shader.isSupported);}
  var mat=AssetDatabase.LoadAssetAtPath<Material>(root+"StoryRecovered/Material/steel_dye01.mat");
  Debug.Log("STORYPROBE material: "+mat+" shader="+mat.shader+" textures="+string.Join(",",mat.GetTexturePropertyNames()));
  var saved=new SerializedObject(mat).FindProperty("m_SavedProperties").FindPropertyRelative("m_TexEnvs");
  Debug.Log("STORYPROBE saved textures: "+saved.arraySize);
  var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(root+"StoryTraders/54cb50c76803fa8b248b4571.prefab");
  var audit=new JArray();
  foreach(var r in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
  {
   audit.Add(new JObject { ["name"]=r.name,["parent"]=r.transform.parent.name,["mesh"]=r.sharedMesh.name,["position"]=r.transform.position.ToString("F4"),["rotation"]=r.transform.eulerAngles.ToString("F4"),["bones"]=new JArray(r.bones.Select(b=>b ? b.name : "null")),["materials"]=new JArray(r.sharedMaterials.Select(m=>m?m.name:"null"))});
  }
  var instance=Object.Instantiate(prefab);
  instance.SetActive(true);
  foreach(var animator in instance.GetComponentsInChildren<Animator>(true))
  {
   if(animator.runtimeAnimatorController)
   {
    Debug.Log("STORYPROBE ANIM "+animator.name+" clips "+string.Join(",",animator.runtimeAnimatorController.animationClips.Select(c=>c.name)));
    var idle=animator.runtimeAnimatorController.animationClips.FirstOrDefault(c=>c.name.ToLowerInvariant().Contains("idle"));
    if(idle) idle.SampleAnimation(animator.gameObject,1);
   }
  }
  var camera=instance.GetComponentsInChildren<Camera>(true).Single();
  Debug.Log("STORYPROBE camera "+camera.transform.position+" "+camera.transform.eulerAngles);
  var rt=new RenderTexture(1280,720,24);camera.targetTexture=rt;camera.enabled=false;camera.Render();
  RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();
  File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,"../../SeasonalPerks/Research/Story/prapor-probe.png")),image.EncodeToPNG());
  RenderTexture.active=null;Object.DestroyImmediate(image);Object.DestroyImmediate(rt);Object.DestroyImmediate(instance);
  File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath,"../../SeasonalPerks/Research/Story/prapor-rig.json")),audit.ToString());
 }
}
}
