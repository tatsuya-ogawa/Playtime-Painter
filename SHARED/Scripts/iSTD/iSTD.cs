﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;
#if UNITY_EDITOR
using UnityEditor;
#endif

// This interface works for simple data and for complex classes
// Usually the base class for comples classes will have 

namespace SharedTools_Stuff
{

    public interface iSTD {
        StdEncoder Encode(); 
        iSTD Decode(string data);
        bool Decode(string tag, string data);
    }

    public interface iSTD_SerializeNestedReferences
    {
        int GetISTDreferenceIndex(UnityEngine.Object obj);
        T GetISTDreferenced<T>(int index) where T: UnityEngine.Object;
    }

    ///<summary> This class can be used for some backwards compatibility. </summary>
    public interface iKeepUnrecognizedSTD : iSTD {
        void Unrecognized(string tag, string data);
        
        StdEncoder EncodeUnrecognized();
    }


    ///<summary>For runtime initialization.
    ///<para> Usage [DerrivedListAttribute(derrivedClass1, DerrivedClass2, DerrivedClass3 ...)] </para>
    ///<seealso cref="StdEncoder"/>
    ///</summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DerrivedListAttribute : Attribute
    {
        List<Type> types;
        public List<Type> DerrivedTypes  { get  {  return types; } }

        public DerrivedListAttribute(params Type[] ntypes) 
        {
            types = new List<Type>(ntypes);
        }
    }

    [Serializable]
    public abstract class AbstractKeepUnrecognized_STD : Abstract_STD, iKeepUnrecognizedSTD
    {
     
        protected List<string> unrecognizedTags = new List<string>();
        protected List<string> unrecognizedData = new List<string>();
        
        public void Unrecognized(string tag, string data) {
            this.Unrecognized(tag, data, ref unrecognizedTags, ref unrecognizedData);
        }
        
        public virtual StdEncoder EncodeUnrecognized()
        {
            var cody = new StdEncoder();
            for (int i = 0; i < unrecognizedTags.Count; i++)
                cody.Add_String(unrecognizedTags[i], unrecognizedData[i]);
            return cody;
        }
        
        public ISTD_ExplorerData explorer = new ISTD_ExplorerData();

        public override StdEncoder Encode() => EncodeUnrecognized();

        public override bool Decode(string tag, string data) => false;
        
#if PEGI
        public bool showDebug;
        public static int inspectedUnrecognized = -1;
        
        public virtual bool PEGI() {
            bool changed = false;
           
            if (!showDebug && icon.Config.Click())
                showDebug = true;

            if (showDebug)
            {
                if (icon.Exit.Click("Back to element inspection").nl())
                    showDebug = false;

                explorer.PEGI(this);

                changed |= this.PEGI(ref unrecognizedTags, ref unrecognizedData, ref inspectedUnrecognized);
            }

            return changed;
        }
#endif
    }


    [Serializable]
    public abstract class Abstract_STD : iSTD
    {

        public abstract StdEncoder Encode();
        public virtual iSTD Decode(string data) => data.DecodeInto(this);
        public virtual iSTD Decode(StdEncoder cody) {
           
            new StdDecoder(cody.ToString()).DecodeTagsFor(this);
            return this;
        }

        public abstract bool Decode(string tag, string data);
    }

    public abstract class ComponentSTD : MonoBehaviour, iKeepUnrecognizedSTD, iSTD_SerializeNestedReferences
#if PEGI
        , IPEGI, IGotDisplayName
#endif
    {

        public override string ToString() => gameObject.name;
        
        public virtual void Reboot() { }

        public abstract bool Decode(string tag, string data);

        public abstract StdEncoder Encode();
        
        [SerializeField]protected List<UnityEngine.Object> _nestedReferences = new List<UnityEngine.Object>();
        protected UnnullableSTD<ElementData> nestedReferenceDatas = new UnnullableSTD<ElementData>();

        public int GetISTDreferenceIndex(UnityEngine.Object obj) 
            => _nestedReferences.TryGetIndexOrAdd(obj);

        public T GetISTDreferenced<T>(int index) where T : UnityEngine.Object
            => _nestedReferences.TryGet(index) as T;


        protected List<string> unrecognizedTags = new List<string>();
        protected List<string> unrecognizedData = new List<string>();

        public void Unrecognized(string tag, string data)
        {
            this.Unrecognized(tag, data, ref unrecognizedTags, ref unrecognizedData);
        }

        public virtual StdEncoder EncodeUnrecognized()
        {
            var cody = new StdEncoder();
            for (int i = 0; i < unrecognizedTags.Count; i++)
                cody.Add_String(unrecognizedTags[i], unrecognizedData[i]);

            return cody;
        }

        public ISTD_ExplorerData explorer = new ISTD_ExplorerData();
        public bool showDebug;

#if PEGI
        
        public string NameForPEGIdisplay() => gameObject.name;

        [SerializeField] int inspectedStuff = -1;
        [SerializeField] int inspectedReference = -1;
        [SerializeField] int inspectedUnrecognized = -1;
        public virtual bool PEGI()
        {

            bool changed = false;

            if (!showDebug && icon.Config.Click())
                showDebug = true;

            if (showDebug)
            {
                if (icon.Edit.Click("Back to element inspection").nl())
                    showDebug = false;

                if (("STD Saves: " + explorer.states.Count).fold_enter_exit(ref inspectedStuff, 0))
                    explorer.PEGI(this);

                pegi.nl();

                if (("Object References: " + _nestedReferences.Count).fold_enter_exit(ref inspectedStuff, 1))
                    "References".edit_List_Obj(_nestedReferences, ref inspectedReference, nestedReferenceDatas);

                pegi.nl();

                if (("Unrecognized Tags " + unrecognizedTags.Count).fold_enter_exit(ref inspectedStuff, 2))
                    changed |= this.PEGI(ref unrecognizedTags, ref unrecognizedData, ref inspectedUnrecognized);

                pegi.nl();

            }
            return changed;
        }
#endif

        public virtual iSTD Decode(string data)
        {
            Reboot();
            new StdDecoder(data).DecodeTagsFor(this);
            return this;
        }

    }

    public static class STDExtensions {

        public static List<Type> TryGetDerrivedClasses (this Type t)
        {
            List<Type> tps = null;
            var att = t.ClassAttribute<DerrivedListAttribute>();
            if (att != null)
                tps = att.DerrivedTypes;

            return tps;
        }

        public static string copyBufferValue;

        public static iSTD RefreshAssetDatabase(this iSTD s) {
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
            return s;
        }

        public static bool LoadOnDrop<T>(this T obj) where T: iSTD
        {

#if PEGI
            UnityEngine.Object myType = null;
            if (pegi.edit(ref myType)) {
                obj.Decode(ResourceLoader.LoadStory(myType));

                ("Loaded " + myType.name).showNotification();

                return true;
            }
#endif
            return false;
        }

        public static iSTD SaveToResources(this iSTD s, string ResFolderPath, string InsideResPath, string filename) {
            ResourceSaver.SaveToResources(ResFolderPath, InsideResPath, filename, s.Encode().ToString());
            return s;
        }
        
        public static iSTD SaveToAssets(this iSTD s, string Path, string filename) {
            ResourceSaver.Save(Application.dataPath + Path.RemoveAssetsPart().AddPreSlashIfNotEmpty().AddPostSlashIfNone(), filename, s.Encode().ToString());
            return s;
        }

        public static iSTD SaveProgress(this iSTD s, string Path, string filename) {
            ResourceSaver.Save(Application.persistentDataPath + Path.RemoveAssetsPart().AddPreSlashIfNotEmpty().AddPostSlashIfNone(), filename, s.Encode().ToString());
            return s;
        }

		public static T LoadFromAssets<T>(this T s, string fullPath, string name) where T:iSTD, new() {
			if (s == null)
				s = new T ();
            s.Decode(ResourceLoader.LoadStoryFromAssets(fullPath, name));
			return s;
        }

		public static T LoadSavedProgress<T>(this T s, string Folder, string fileName)where T:iSTD, new() {
			if (s == null)
				s = new T ();
            s.Decode(ResourceLoader.Load(Application.persistentDataPath + Folder.AddPreSlashIfNotEmpty().AddPostSlashIfNone() + fileName + ResourceSaver.fileType));
			return s;
		}

		public static T LoadFromResources<T>(this T s, string resFolderLocation, string subFolder, string file)where T:iSTD, new() {
			if (s == null)
				s = new T ();
			s.Decode(ResourceLoader.LoadStoryFromResource(resFolderLocation, subFolder, file));
			return s;
		}

		public static T LoadFromResources<T>(this T s, string subFolder, string file)where T:iSTD, new() {
			if (s == null)
				s = new T ();
			s.Decode(ResourceLoader.LoadStoryFromResource(subFolder, file));
			return s;
		}
        /*
        public static bool PEGI <T>(this T mono, ref iSTD_Explorer exp) where T:MonoBehaviour, iSTD {
            bool changed = false;
            #if PEGI
            if (!exp) {
                exp = mono.GetComponent<iSTD_Explorer>();
                if (!exp && "Add iSTD Explorer".Click())
                    exp = mono.gameObject.AddComponent<iSTD_Explorer>();

                changed |= exp != null;
            }
            else
            {
                exp.ConnectSTD = mono;
                changed |=exp.PEGI();
            }  
#endif

            return changed;
        }
        */
        #if PEGI
        public static bool PEGI(this iKeepUnrecognizedSTD el, ref List<string> tags, ref List<string> data, ref int inspected)  {
            bool changed = false;

            pegi.nl();

            var cnt = tags.Count;

            if (cnt > 0 && ("Unrecognized for " + el.ToPEGIstring() + "[" + cnt + "]").nl())
               
               
            if (tags != null && tags.Count > 0) {

                if (inspected < 0)
                {

                    for (int i = 0; i < tags.Count; i++)
                    {
                        if (icon.Delete.Click())
                        {
                            changed = true;
                            tags.RemoveAt(i);
                            data.RemoveAt(i);
                            i--;
                        }
                        else
                        {
                            pegi.write(tags[i]);
                            if (icon.Edit.Click().nl())
                                inspected = i;
                        }
                    }
                }
                else
                {
                    if (inspected >= tags.Count || icon.Back.Click())
                        inspected = -1;
                    else
                    {
                        int i = inspected;
                        var t = tags[i];
                        if ("Tag".edit(40, ref t).nl())
                            tags[i] = t;
                        var d = data[i];
                        if ("Data".edit(50, ref d).nl())
                            data[i] = d;
                    }
                }
            }

            pegi.nl();

            return changed;
        }
#endif
        public static void Unrecognized (this iKeepUnrecognizedSTD el, string tag, string data, ref List<string> unrecognizedTags, 
            ref List<string> unrecognizedData) {
          
                if (unrecognizedTags.Contains(tag))
                {
                    int ind = unrecognizedTags.IndexOf(tag);
                    unrecognizedTags[ind] = tag;
                    unrecognizedData[ind] = data;
                }
                else
                {
                    unrecognizedTags.Add(tag);
                    unrecognizedData.Add(data);
                }
            
        }

    }
}