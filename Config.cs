using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VRCTelekinesis
{
    internal static class Config
    {
        internal static bool enabled = false;
        internal static bool smooth = true;
        internal static bool alignHead = true;
        internal static GameObject ball;
        internal static float ballSize = 1;
        internal static List<GameObject> syncObjs = new List<GameObject>();
        internal static Dictionary<GameObject, GameObject> RaycastPointObjects = new Dictionary<GameObject, GameObject>();


        /*internal static GameObject point;
        internal static GameObject head;
        internal static Dictionary<GameObject, bool> list = new Dictionary<GameObject, bool>();*/

        
    }
}
