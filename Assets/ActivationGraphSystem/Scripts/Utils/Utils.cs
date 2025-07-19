using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System;
using UnityEngine;
using System.Linq;

namespace ActivationGraphSystem {
    public class Utils : MonoBehaviour {

        /// <summary>
        /// Recusivelly clones a GameObject with all the components (scripts) and 
        /// children GameObjects. Be carefully on what top GameObject do you use
        /// it. It has some limitations, but it works for the ActivationGraphSystem nodes.
        /// 
        /// Clone the GameObject cloneThis to the same parent as the original is, or 
        /// it clones to the second GameObject parametar if given. So the parent can 
        /// be another one after cloning the GameObject.
        /// </summary>
        /// <param name="cloneThis"></param>
        /// <param name="cloneParent"></param>
        static public GameObject CloneGameObject(GameObject cloneThis, GameObject cloneParent = null) {
            GameObject clone = new GameObject(cloneThis.name);

            if (cloneParent != null) {
                clone.transform.parent = cloneParent.transform;
            } else {
                clone.transform.parent = cloneThis.transform.parent;
            }

            foreach (Component c in cloneThis.GetComponents<Component>()) {
                Utils.AddComponentCloneOf(clone, c);
            }

            for (int i = 0; i < cloneThis.transform.childCount; i++) {
                Transform child = cloneThis.transform.GetChild(i);
                CloneGameObject(child.gameObject, clone);
            }

            return clone;
        }

        public static T GetCopyOf<T>(UnityEngine.Component comp, T other) where T : UnityEngine.Component {
            Type type = comp.GetType();
            if (type != other.GetType())
                return null;

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.Default;
            PropertyInfo[] pinfos = type.GetProperties(flags);

            if (comp is MeshFilter) {
                foreach (var pinfo in pinfos) {
                    if (Application.isEditor) {
                        if (pinfo.Name == "mesh") {
                            continue;
                        }
                    }

                    if (pinfo.CanWrite && pinfo.CanRead && !pinfo.PropertyType.IsArray) {
                        try {
                            pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                        } catch { }
                    }
                }

            } else if (comp is MeshRenderer) {

                foreach (var pinfo in pinfos) {
                    if (Application.isEditor) {
                        if (pinfo.Name == "material" || pinfo.Name == "materials") {
                            continue;
                        }
                    }

                    if (pinfo.CanWrite && pinfo.CanRead && !pinfo.PropertyType.IsArray) {
                        try {
                            pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                        } catch { }
                    }
                }

            } else if (comp is SkinnedMeshRenderer) {

                foreach (var pinfo in pinfos) {
                    if (Application.isEditor) {
                        if (pinfo.Name == "material" || pinfo.Name == "materials" || pinfo.Name == "mesh") {
                            continue;
                        }
                    }
                    if (pinfo.CanWrite && pinfo.CanRead && !pinfo.PropertyType.IsArray) {
                        try {
                            pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                        } catch { }
                    }
                }

            } else {
                foreach (var pinfo in pinfos) {
                    // Needed for debugging.
                    /*if (pinfo.Name == "Conditions") {
                        Debug.Log("pinfo: " + pinfo.Name + " Value: " + pinfo.GetValue(other, null) + " type: " + type);
                    }*/
                    if (pinfo.CanWrite && pinfo.CanRead && !pinfo.PropertyType.IsArray) {
                        try {
                            pinfo.SetValue(comp, pinfo.GetValue(other, null), null);
                        } catch { }
                    }
                }

            }

            FieldInfo[] finfos = type.GetFields(flags);
            foreach (var finfo in finfos) {
                if (finfo.FieldType.IsGenericType) {

                    if (finfo.GetValue(other) is IList) {
                        var copyThisList = (IList)finfo.GetValue(other);
                        var copyToList = (IList)finfo.GetValue(comp);

                        if (copyToList == null)
                            continue;

                        foreach (var item in copyThisList) {
                            copyToList.Add(item);
                        }

                    } else if (finfo.GetValue(other) is IDictionary) {
                        var copyThisList = (IDictionary)finfo.GetValue(other);
                        var copyToList = (IDictionary)finfo.GetValue(comp);

                        if (copyToList == null)
                            continue;

                        foreach (var item in copyThisList.Keys) {
                            copyToList.Add(item, copyThisList[item]);
                        }
                    } else {
                        // For debuging. All the actions won't be copied, but they will be assigned at runtime,
                        // so thats okay.
                        //Debug.Log("Not copied: " + finfo.Name + " type: " + type);
                    }
                } else {
                    finfo.SetValue(comp, finfo.GetValue(other));
                }
            }

            return comp as T;
        }

        /// <summary>
        /// It is a generic copy method for components (scripts) on GameObjects.
        /// However, it supports limited components, which is enough for the
        /// ActivationGraphEditor nodes.
        /// List and dictionary needs a special treatment, because else they would point
        /// to the original container, so the copy and the original would have a shared list.
        /// The field material, materials and mesh must be ignored, if the nodes are
        /// copied in editor mode (not at running the game), else there would be warnings
        /// like do not use mesh in editor mode, use sharedMesh. Unfortunatelly they are not 
        /// exceptions...
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="go"></param>
        /// <param name="toAdd"></param>
        /// <returns></returns>
        public static T AddComponentCloneOf<T>(GameObject go, T toAdd) where T : UnityEngine.Component {
            if (toAdd is Transform) {
                // Transform cannot be copies by reflection, e.g. children count will be wrong.
                Transform other = toAdd as Transform;
                Transform src = go.GetComponent<Transform>();
                src.localPosition = other.localPosition;
                src.localRotation = other.localRotation;
                src.localScale = other.localScale;
                return src as T;
            } else {
                T target = go.AddComponent(toAdd.GetType()) as T;
                return GetCopyOf(target, toAdd);
            }
        }
    }

    // Workaround for Windows Store .Net.
#if NETFX_CORE 
    public static class GetPropertyHelper
    {
        public static PropertyInfo[] GetProperties(this Type type, BindingFlags flags)
        {
            List<PropertyInfo> infos = new List<PropertyInfo>();
            var props = type.GetTypeInfo().DeclaredProperties;
            foreach (PropertyInfo info in props) {
                infos.Add(info);
            }

            PropertyInfo[] retTypeInfos = infos.ToArray();
            return retTypeInfos;
        }
    }

    public static class GetFieldHelper
    {
        public static FieldInfo[] GetFields(this Type type, BindingFlags flags)
        {
            List<FieldInfo> infos = new List<FieldInfo>();
            var fields = type.GetTypeInfo().DeclaredFields;
            foreach (FieldInfo field in fields) {
                infos.Add(field);
            }

            FieldInfo[] retTypeInfos = infos.ToArray();
            return retTypeInfos;
        }
    }
#endif

}
