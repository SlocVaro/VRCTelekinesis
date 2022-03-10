using MelonLoader;
using MLInput;
using System.Collections.Generic;
using UnityEngine;
using VRC.Networking;
using VRC.SDK3.Components;
using VRC.SDKBase;

[assembly: MelonInfo(typeof(VRCTelekinesis.VRCTelekinesis), "VRCTelekinesis", "1.0.1", "$locVar0", "https://github.com/SlocVaro")]
[assembly: MelonGame("VRChat", "VRChat")]
[assembly: MelonColor(System.ConsoleColor.Red)]

namespace VRCTelekinesis
{
    public class VRCTelekinesis : MelonMod
    {
        private static float time;
        public override void OnApplicationStart()
        {
            time = 0;
            MLIHandler.AddMod("VRCTelekinesis", new List<MLICommand>()
            {
                new MLICommand("Toggle VRCTelekinesis", "Toggle on/off mod.", delegate(List<string> args)
                {
                    Config.enabled = !Config.enabled;
                    string txt = "disabled";
                    if (Config.enabled)
                    {
                        txt = "enabled";
                    }
                    else
                    {
                        ClearRaycastPointObjs();
                        Config.ball.SetActive(false);
                    }
                    MelonLogger.Msg($"VRCTelekinesis {txt}.");
                }),
                new MLICommand("Smooth movement", "Toggle smooth on/off smooth movement.", delegate(List<string> args)
                {
                    Config.smooth = !Config.smooth;
                    string txt = "disabled";
                    if (Config.smooth)
                    {
                        txt = "enabled";
                    }
                    MelonLogger.Msg($"Smooth {txt}.");
                }),
                new MLICommand("Update objects", "Update the list of all objects that can be grabbed.", delegate(List<string> args)
                {
                    UpdateObjs();
                }),
                new MLICommand("Align head", "Toggle on/off alignment of object's rotation with head.", delegate(List<string> args)
                {
                    Config.alignHead = !Config.alignHead;
                    string txt = "disabled";
                    if (Config.alignHead)
                    {
                        txt = "enabled";
                    }
                    MelonLogger.Msg($"Align head {txt}.");
                }),
                new MLICommand("Ball size", "Change ball size. Usage: (Command ID) (size).", delegate(List<string> args)
                {
                    if (args.Count == 0)
                    {
                        MelonLogger.Error("Must enter a int/decimal/number value");
                        return;
                    }
                    if (args.Count > 1)
                    {
                        MelonLogger.Error("This command allows only 1 argument.");
                        return;
                    }
                    bool flag1 = float.TryParse(args[0], out float val);
                    if (!flag1)
                    {
                        MelonLogger.Error("Value must be a int/decimal/number value.");
                        return;
                    }
                    Config.ballSize = val;
                    Config.ball.transform.localScale = Vector3.one * Config.ballSize;
                    MelonLogger.Msg($"Ball size set to {val}");
                }),
            });
        }
        public override void OnUpdate()
        {
            time += Time.deltaTime;
            if (Config.enabled)
            {
                if (Config.ball != null)
                {
                    if (Input.GetMouseButton(1))
                    {
                        if (Config.RaycastPointObjects.Count == 0 && Physics.Raycast(Camera.current.transform.position, Camera.current.transform.forward, out var hit, short.MaxValue))
                        {
                            Config.ball.transform.position = hit.point;
                            Config.ball.SetActive(true);

                            foreach (GameObject syncObj in Config.syncObjs)
                            {
                                if (Config.ball.GetComponent<Collider>().bounds.Contains(syncObj.transform.position))
                                {
                                    var RaycastPointObject = new GameObject();
                                    RaycastPointObject.transform.SetPositionAndRotation(syncObj.transform.position, syncObj.transform.rotation);
                                    RaycastPointObject.transform.parent = Camera.current.transform;
                                    Config.RaycastPointObjects.Add(syncObj.gameObject, RaycastPointObject);
                                }
                            }
                        }
                        else
                        {
                            if (Input.GetAxis("Mouse ScrollWheel") > 0f)
                            {
                                foreach (var RaycastPointObject in Config.RaycastPointObjects)
                                {
                                    RaycastPointObject.Value.transform.position += Camera.current.transform.forward * (Vector3.Distance(Camera.current.transform.position, RaycastPointObject.Value.transform.position) / 4);
                                }
                            }
                            if (Input.GetAxis("Mouse ScrollWheel") < 0f)
                            {
                                foreach (var RaycastPointObject in Config.RaycastPointObjects)
                                {
                                    RaycastPointObject.Value.transform.position = Vector3.Lerp(Camera.current.transform.position, RaycastPointObject.Value.transform.position, .9f);
                                }
                            }

                            Config.ball.SetActive(false);
                            foreach (var GrabbedObject in Config.RaycastPointObjects)
                            {
                                Networking.SetOwner(Networking.LocalPlayer, GrabbedObject.Key);

                                if (GrabbedObject.Key.GetComponent<Rigidbody>() != null)
                                {
                                    GrabbedObject.Key.GetComponent<Rigidbody>().velocity = Vector3.zero;
                                    GrabbedObject.Key.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
                                }
                                if (Config.smooth)
                                {
                                    GrabbedObject.Key.transform.position = Vector3.Lerp(GrabbedObject.Key.transform.position, GrabbedObject.Value.transform.position, .1f);
                                    if (Config.alignHead)
                                    {
                                        GrabbedObject.Key.transform.rotation = Quaternion.Lerp(GrabbedObject.Key.transform.rotation, GrabbedObject.Value.transform.rotation, .1f);
                                    }
                                }
                                else
                                {
                                    GrabbedObject.Key.transform.position = GrabbedObject.Value.transform.position;
                                    if (Config.alignHead)
                                    {
                                        GrabbedObject.Key.transform.rotation = GrabbedObject.Value.transform.rotation;
                                    }
                                }
                            }
                        }
                    }
                    if (Input.GetMouseButtonUp(1))
                    {
                        Config.ball.SetActive(false);
                        if (Config.smooth)
                        {
                            foreach (var obj in Config.RaycastPointObjects)
                            {
                                if (obj.Key.GetComponent<Rigidbody>() != null)
                                {
                                    obj.Key.GetComponent<Rigidbody>().AddForce((obj.Value.transform.position - obj.Key.transform.position) * 15, ForceMode.VelocityChange);
                                }
                            }
                        }
                        ClearRaycastPointObjs();
                    }
                }
            }
        }
        private void ClearRaycastPointObjs()
        {
            foreach (var obj in Config.RaycastPointObjects)
            {
                GameObject.Destroy(obj.Value);
            }
            Config.RaycastPointObjects.Clear();
        }
        private void UpdateObjs()
        {
            Config.syncObjs.Clear();
            foreach (VRCObjectSync obj in Resources.FindObjectsOfTypeAll<VRCObjectSync>())
            {
                if (obj != null)
                {
                    Config.syncObjs.Add(obj.gameObject);
                }
            }
        }
        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            Config.ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Config.ball.name = "[VRCTK Ball]";
            Config.ball.transform.localScale = Vector3.one * Config.ballSize;
            Config.ball.GetComponent<Collider>().isTrigger = true;
            Config.ball.layer = LayerMask.NameToLayer("Ignore Raycast");
            Config.ball.SetActive(false);

            Material mat = new Material(Shader.Find("Standard"));
            mat.SetColor("_Color", new Color(1, 1, 1, 0.5f));
            mat.SetFloat("_Mode", 2);
            mat.SetInt("_SrcBlend", 5);
            mat.SetInt("_DstBlend", 10);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            Config.ball.GetComponent<Renderer>().material = mat;

            UpdateObjs();
        }

        /*
        public override void OnSceneWasInitialized(int buildIndex, string sceneName)
        {
            Config.point = new GameObject();
            Config.head = new GameObject();
            Config.point.transform.SetParent(Config.head.transform);
            Config.ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Config.ball.SetActive(false);
            Config.ball.layer = LayerMask.NameToLayer("Ignore Raycast");

            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.red;
            mat.SetColor("_Color", new Color(0.5f, 0.5f, 1, 0.5f));
            mat.SetFloat("_Mode", 2);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);

            Config.ball.GetComponent<Renderer>().material = mat;
            Config.ball.GetComponent<Collider>().isTrigger = true;

            UpdateObjs();

            Config.list.Clear();
        }
        */
    }
}
