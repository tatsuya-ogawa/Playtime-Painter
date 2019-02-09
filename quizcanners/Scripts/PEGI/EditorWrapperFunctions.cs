﻿#if PEGI && UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
#if PEGI
using PlayerAndEditorGUI;
#endif
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor.SceneManagement;
using QuizCannersUtilities;
using UnityEngine.Events;
using UnityEditorInternal;

#pragma warning disable IDE1006 // Naming Styles

namespace PlayerAndEditorGUI {

    public static class ef {


        enum EditorType { Mono, ScriptableObject, Material, Unknown }

        static EditorType editorType = EditorType.Unknown;

        static bool lineOpen = false;
        static int selectedFold = -1;
        static int elementIndex;
        public static SerializedObject serObj;
        static Editor _editor;
        static PEGI_Inspector_Material _materialEditor;

        public static bool DefaultInspector() {
            newLine();

            EditorGUI.BeginChangeCheck();
            switch (editorType) {
                case EditorType.Material: if (_materialEditor != null)  _materialEditor.DrawDefaultInspector(); break;
                default: if (_editor!= null) _editor.DrawDefaultInspector(); break;
            }
            return EditorGUI.EndChangeCheck();

        }

        public static bool Inspect<T>(Editor editor) where T : MonoBehaviour
        {
            _editor = editor;

            editorType = EditorType.Mono;

            var o = (T)editor.target;
            var so = editor.serializedObject;

            if (o.gameObject.IsPrefab())
                return false;

            var pgi = o as IPEGI;
            if (pgi != null)
            {
                start(so);
                var changed = pgi.Inspect();
                end(o.gameObject);
                return changed;
            }
            else editor.DrawDefaultInspector();

            return false;
        }

        public static bool Inspect_so<T>(Editor editor) where T : ScriptableObject
        {
            _editor = editor;

            editorType = EditorType.ScriptableObject;

            var o = (T)editor.target;
            var so = editor.serializedObject;

            var pgi = o as IPEGI;
            if (pgi != null)
            {
                start(so);
                bool changed = pgi.Inspect();
                end(o);
                return changed;
            }
            else editor.DrawDefaultInspector();

            return false;
        }
        
        public static bool Inspect_Material(PEGI_Inspector_Material editor) {

            _materialEditor = editor;

            editorType = EditorType.Material;

            var mat = editor.unityMaterialEditor.target as Material;

            start();

            var changed = editor.Inspect(mat);

            end(mat);

            return changes;
        }

        static void start(SerializedObject so = null) {
            elementIndex = 0;
            PEGI_Extensions.focusInd = 0;
            lineOpen = false;
            serObj = so;
            pegi.globChanged = false;
        }

        static bool end(Material mat)
        {

            if (changes)
            {
                if (!Application.isPlaying)
                    EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

                EditorUtility.SetDirty(mat);
            }
            newLine();

            return changes;
        }

        static bool end(GameObject go)
        {
            if (changes)
            {
                if (!Application.isPlaying)
                    EditorSceneManager.MarkSceneDirty((go == null) ? EditorSceneManager.GetActiveScene() : go.scene);

                EditorUtility.SetDirty(go);
            }

            newLine();
            return changes;
        }

        static bool end<T>(T obj) where T : UnityEngine.Object
        {
            if (changes)
                EditorUtility.SetDirty(obj);

            newLine();
            return changes;
        }
        
        static bool change { get { pegi.globChanged = true; return true; } }

        static bool Dirty(this bool val) { pegi.globChanged |= val; return val; }

        static bool changes => pegi.globChanged;

        static void BeginCheckLine() { checkLine(); EditorGUI.BeginChangeCheck(); }

        static bool EndCheckLine() => EditorGUI.EndChangeCheck().Dirty(); 

        public static void checkLine()
        {
            if (!lineOpen)
            {
                pegi.tabIndex = 0;
                EditorGUILayout.BeginHorizontal();
                lineOpen = true;
            }
        }

        public static void newLine()
        {
            if (lineOpen)
            {
                lineOpen = false;
                EditorGUILayout.EndHorizontal();
            }
        }
        
        private static bool IsFoldedOut { get { return pegi.isFoldedOut_or_Entered; } set { pegi.isFoldedOut_or_Entered = value; } }

        private static bool StylizedFoldOut(bool foldedOut, string txt, string hint = "FoldIn/FoldOut") {

            var cnt = new GUIContent
            {
                text = txt,
                tooltip = hint
            };

            return EditorGUILayout.Foldout(foldedOut, cnt, true);
        }

        public static bool foldout(string txt, ref bool state)
        {
            checkLine();
            state = StylizedFoldOut(state, txt);
            if (IsFoldedOut != state)
                Dirty(true);
            IsFoldedOut = state;
            return IsFoldedOut;
        }

        public static bool foldout(string txt, ref int selected, int current)
        {
            checkLine();

            IsFoldedOut = (selected == current);

            if (StylizedFoldOut(IsFoldedOut, txt))
                selected = current;
            else
                if (IsFoldedOut) selected = -1;


            if (IsFoldedOut != (selected == current))
                Dirty(true);

            IsFoldedOut = selected == current;

            return IsFoldedOut;
        }
        
        public static bool foldout(string txt)
        {
            checkLine();

            IsFoldedOut = foldout(txt, ref selectedFold, elementIndex);

            elementIndex++;

            return IsFoldedOut;
        }

        public static bool select<T>(ref int no, List<T> lst, int width)
        {
            checkLine();

            var lnms = new List<string>();
            var indxs = new List<int>();

            int jindx = -1;

            for (int j = 0; j < lst.Count; j++)
            {
                if (lst[j] != null)
                {
                    if (no == j)
                        jindx = indxs.Count;
                    lnms.Add("{0}: {1}".F(j,lst[j].ToPEGIstring()));
                    indxs.Add(j);

                }
            }



            if (select(ref jindx, lnms.ToArray(), width))
            {
                no = indxs[jindx];
                return change;
            }

            return false;

        }

        public static bool select<T>(ref int no, CountlessSTD<T> tree) where T : ISTD
            , new()
        {
            List<int> inds;
            List<T> objs = tree.GetAllObjs(out inds);
            List<string> filtered = new List<string>();
            int tmpindex = -1;
            for (int i = 0; i < objs.Count; i++)
            {
                if (no == inds[i])
                    tmpindex = i;
                filtered.Add("{0}: {1}".F(i, objs[i].ToPEGIstring()));
            }

            if (select(ref tmpindex, filtered.ToArray()))
            {
                no = inds[tmpindex];
                return true;
            }
            return false;
        }

        public static bool select<T>(ref int no, Countless<T> tree)
        {
            List<int> inds;
            List<T> objs = tree.GetAllObjs(out inds);
            List<string> filtered = new List<string>();
            int tmpindex = -1;
            for (int i = 0; i < objs.Count; i++)
            {
                if (no == inds[i])
                    tmpindex = i;
                filtered.Add(objs[i].ToPEGIstring());
            }

            if (select(ref tmpindex, filtered.ToArray()))
            {
                no = inds[tmpindex];
                return true;
            }
            return false;
        }
        
        public static bool select(ref int no, string[] from, int width)
        {
            checkLine();

            int newNo = EditorGUILayout.Popup(no, from, GUILayout.MaxWidth(width));
            if (newNo != no)
            {
                no = newNo;
                return change;
            }
            return false;
        }

        public static bool select(ref int no, string[] from)
        {
            checkLine();

            int newNo = EditorGUILayout.Popup(no, from);
            if (newNo != no)
            {
                no = newNo;
                return change;
            }
            return false;
            //to = from[repName];
        }
        
        public static bool select(ref int no, Dictionary<int, string> from)
        {
            checkLine();
            string[] options = new string[from.Count];

            int ind = -1;

            for (int i = 0; i < from.Count; i++)
            {
                var e = from.ElementAt(i);
                options[i] = e.Value;
                if (no == e.Key)
                    ind = i;
            }

            int newInd = EditorGUILayout.Popup(ind, options, EditorStyles.toolbarDropDown);
            if (newInd != ind)
            {
                no = from.ElementAt(newInd).Key;
                return change;
            }
            return false;
        }

        public static bool select(ref int no, Dictionary<int, string> from, int width)
        {
            checkLine();
            string[] options = new string[from.Count];

            int ind = -1;

            for (int i = 0; i < from.Count; i++)
            {
                var e = from.ElementAt(i);
                options[i] = e.Value;
                if (no == e.Key)
                    ind = i;
            }

            int newInd = EditorGUILayout.Popup(ind, options, EditorStyles.toolbarDropDown, GUILayout.MaxWidth(width));
            if (newInd != ind)
            {
                no = from.ElementAt(newInd).Key;
                return change;
            }
            return false;
        }
        
        public static bool select(ref int no, Texture[] tex)
        {
            if (tex.Length == 0) return false;

            checkLine();
            int before = no;
            List<string> tnames = new List<string>();
            List<int> tnumbers = new List<int>();

            int curno = 0;
            for (int i = 0; i < tex.Length; i++)
                if (tex[i])
                {
                    tnumbers.Add(i);
                    tnames.Add("{0}: {1}".F(i, tex[i].name));
                    if (no == i) curno = tnames.Count - 1;
                }

            curno = EditorGUILayout.Popup(curno, tnames.ToArray());

            if ((curno >= 0) && (curno < tnames.Count))
                no = tnumbers[curno];

            return (before != no);
        }
   
        static bool select_Type(ref Type current, List<Type> others, Rect rect)
        {

            string[] names = new string[others.Count];

            int ind = -1;

            for (int i = 0; i < others.Count; i++)
            {
                var el = others[i];
                names[i] = el.ToPEGIstring_Type();
                if (el != null && el.Equals(current))
                    ind = i;
            }

            int newNo = EditorGUI.Popup(rect, ind, names);
            if (newNo != ind)
            {
                current = others[newNo];
                return change;
            }

            return false;
        }

        public static void Space()
        {
            checkLine();
            EditorGUILayout.Separator();
            newLine();
        }

        public static bool edit<T>(ref T field) where T : UnityEngine.Object
        {
            checkLine();
            T tmp = field;
            field = (T)EditorGUILayout.ObjectField(field, typeof(T), true);
            return tmp != field;
        }

        public static bool edit<T>(ref T field, bool allowDrop) where T : UnityEngine.Object
        {
            checkLine();
            T tmp = field;
            field = (T)EditorGUILayout.ObjectField(field, typeof(T), allowDrop);
            return tmp != field;
        }
        
        public static bool edit(string label, ref float val)
        {
            checkLine();
            float before = val;
            val = EditorGUILayout.FloatField(label, val);
            return (val != before) ? change : false;
        }

        public static bool edit(ref float val)
        {
            checkLine();
            float before = val;
            val = EditorGUILayout.FloatField(val);
            return (val != before).Dirty(); 
        }
        
        public static bool edit(ref float val, int width)
        {
            checkLine();
            float before = val;
            val = EditorGUILayout.FloatField(val, GUILayout.MaxWidth(width));
            return (val != before).Dirty();
        }

        public static bool edit(ref double val, int width)
        {
            checkLine();
            double before = val;
            val = EditorGUILayout.DoubleField(val, GUILayout.MaxWidth(width));
            return (val != before).Dirty();
        }

        public static bool edit(ref double val)
        {
            checkLine();
            double before = val;
            val = EditorGUILayout.DoubleField(val);
            return (val != before).Dirty();
        }
        
        public static bool edit(ref int val, int min, int max)
        {
            checkLine();
            float before = val;
            val = EditorGUILayout.IntSlider(val, min, max); //Slider(val, min, max);
            return (val != before).Dirty();
        }

        public static bool edit(ref uint val, uint min, uint max)
        {
            checkLine();
            int before = (int)val;
            val = (uint)EditorGUILayout.IntSlider(before, (int)min, (int)max); //Slider(val, min, max);
            return (val != before).Dirty();
        }


        public static bool editPOW(ref float val, float min, float max)
        {

            checkLine();
            float before = Mathf.Sqrt(val);
            float after = EditorGUILayout.Slider(before, min, max);
            if (before != after)
            {
                val = after * after;
                return change;
            }
            return false;
        }

        public static bool edit(ref float val, float min, float max)
        {
            checkLine();
            float before = val;
            val = EditorGUILayout.Slider(val, min, max);
            return (val != before).Dirty();
        }

        public static bool edit(ref Color col)
        {

            checkLine();
            Color before = col;
            col = EditorGUILayout.ColorField(col);

            return (before != col).Dirty();

        }

        public static bool editKey(ref Dictionary<int, string> dic, int key)
        {
            checkLine();
            int before = key;

            if (editDelayed(ref key, 40))
                return dic.TryChangeKey(before, key) ? change : false;
  
            return false;
        }
        
        public static bool edit(ref Dictionary<int, string> dic, int atKey)
        {
            string before = dic[atKey];
            if (editDelayed(ref before))
            {
                dic[atKey] = before;
                return change;
            }
            return false;
        }

        public static bool edit(ref int val)
        {
            checkLine();
            int pre = val;
            val = EditorGUILayout.IntField(val);
            return (val != pre).Dirty();
        }

        public static bool edit(ref uint val)
        {
            checkLine();
            int pre = (int)val;
            val = (uint)EditorGUILayout.IntField(pre);
            return (val != pre).Dirty();
        }

        public static bool edit(ref int val, int width)
        {
            checkLine();
            int pre = val;
            val = EditorGUILayout.IntField(val, GUILayout.MaxWidth(width));
            return (val != pre).Dirty();
        }
        
        public static bool edit(ref uint val, int width)
        {
            checkLine();
            int pre = (int)val;
            val = (uint)EditorGUILayout.IntField(pre, GUILayout.MaxWidth(width));
            return (val != pre).Dirty();
        }
        
        public static bool edit(string name, ref AnimationCurve val) {

            BeginCheckLine();
            val = EditorGUILayout.CurveField(name,val);
            return EndCheckLine();
        }

        public static bool edit(string label, ref Vector4 val)
        {
            checkLine();

            var oldVal = val;
            val = EditorGUILayout.Vector2Field(label, val);

            return (oldVal != val).Dirty(); ;
        }

        public static bool edit(string label, ref Vector2 val) {
            checkLine();

            var oldVal = val;
            val = EditorGUILayout.Vector2Field(label, val);

            return (oldVal != val).Dirty();
        }

        public static bool edit(ref Vector2 val)
        {
            checkLine();
            bool modified = false;
            
            modified |= edit(ref val.x);
            modified |= edit(ref val.y);
            return modified.Dirty();
        }

        public static bool edit(ref MyIntVec2 val)
        {
            checkLine();
            bool modified = false;
            modified |= edit(ref val.x);
            modified |= edit(ref val.y);
            return modified.Dirty();
        }

        public static bool edit(ref MyIntVec2 val, int min, int max)
        {
            checkLine();
            bool modified = false;
            modified |= edit(ref val.x, min, max);
            modified |= edit(ref val.y, min, max);
            return modified.Dirty();
        }

        public static bool edit(ref MyIntVec2 val, int min, MyIntVec2 max)
        {
            checkLine();
            bool modified = false;
            modified |= edit(ref val.x, min, max.x);
            modified |= edit(ref val.y, min, max.y);
            return modified.Dirty();
        }

        public static bool edit(ref Vector4 val)
        {
            checkLine();
            bool modified = false;
            modified |= "X".edit(ref val.x).nl() | "Y".edit(ref val.y).nl() | "Z".edit(ref val.z).nl() | "W".edit(ref val.w).nl();
            return modified.Dirty();
        }

        static string editedText;
        static string editedHash = "";
        public static bool editDelayed(ref string text)
        {
            checkLine();
            
            if (KeyCode.Return.IsDown())
            {
                if (text.GetHashCode().ToString() == editedHash)
                {
                    EditorGUILayout.TextField(text);
                    text = editedText;
                    return change;
                }
            }

            string tmp = text;
            if (edit(ref tmp))
            {
                editedText = tmp;
                editedHash = text.GetHashCode().ToString();
                pegi.globChanged = false;
            }
            
            return false;//(String.Compare(before, text) != 0);
        }

        public static bool editDelayed(ref string text, int width)
        {
            checkLine();

            if (KeyCode.Return.IsDown() && (text.GetHashCode().ToString() == editedHash))
            {
                EditorGUILayout.TextField(text);
                text = editedText;
                return change;
            }

            string tmp = text;
            if (edit(ref tmp, width))
            {
                editedText = tmp;
                editedHash = text.GetHashCode().ToString();
                pegi.globChanged = false;
            }



            return false;//(String.Compare(before, text) != 0);
        }
        
        static int editedIntegerIndex;
        static int editedInteger;
        public static bool editDelayed(ref int val, int width)
        {
            checkLine();

            if (KeyCode.Return.IsDown() && (elementIndex == editedIntegerIndex))
            {
                EditorGUILayout.IntField(val, GUILayout.Width(width));
                val = editedInteger;
                elementIndex++; editedIntegerIndex = -1;
                return change;
            }

            int tmp = val;
            if (edit(ref tmp))
            {
                editedInteger = tmp;
                editedIntegerIndex = elementIndex;
            }

            elementIndex++;

            return false;//(String.Compare(before, text) != 0);
        }

        static int editedFloatIndex;
        static float editedFloat;
        public static bool editDelayed(ref float val, int width)
        {
            checkLine();

            if (KeyCode.Return.IsDown() && (elementIndex == editedFloatIndex))
            {
                EditorGUILayout.FloatField(val, GUILayout.Width(width));
                val = editedFloat;
                elementIndex++;
                editedFloatIndex = -1;
                return change;
            }

            var tmp = val;
            if (edit(ref tmp))
            {
                editedFloat = tmp;
                editedFloatIndex = elementIndex;
            }

            elementIndex++;

            return false;//(String.Compare(before, text) != 0);
        }

        public static bool edit(ref string text)
        {
            BeginCheckLine();
            text = EditorGUILayout.TextField(text);
            return EndCheckLine();
        }

        public static bool edit(ref string text, int width)
        {
            BeginCheckLine();
            text = EditorGUILayout.TextField(text, GUILayout.MaxWidth(width));
            return EndCheckLine();
        }

        public static bool editBig(ref string text)
        {
            BeginCheckLine();
            text = EditorGUILayout.TextArea(text, GUILayout.MaxHeight(100));
            return EndCheckLine();
        }

        public static bool edit(ref string[] texts, int no)
        {
            BeginCheckLine();
            texts[no] = EditorGUILayout.TextField(texts[no]);
            return EndCheckLine();
        }

        public static bool edit(List<string> texts, int no)
        {
            BeginCheckLine();
            texts[no] = EditorGUILayout.TextField(texts[no]);
            return EndCheckLine();
        }

        public static bool editPowOf2(ref int i, int min, int max)
        {
            checkLine();
            int before = i;
            i = Mathf.ClosestPowerOfTwo((int)Mathf.Clamp(EditorGUILayout.IntField(i), min, max));
            return (i != before).Dirty();
        }

        public static bool toggleInt(ref int val)
        {
            checkLine();
            bool before = val > 0;
            if (toggle(ref before))
            {
                val = before ? 1 : 0;
                return change;
            }
            return false;
        }

        public static bool toggle(ref bool val)
        {
            checkLine();
            bool before = val;
            val = EditorGUILayout.Toggle(val, GUILayout.MaxWidth(40));
            return (before != val).Dirty();
        }

        public static bool toggle(int ind, CountlessBool tb)
        {
            bool has = tb[ind];
            if (toggle(ref has))
            {
                tb.Toggle(ind);
                return true;
            }
            return false;
        }

        public static bool toggle(ref bool val, Texture2D TrueIcon, Texture2D FalseIcon, string tip, int width, GUIStyle style = null) {
            bool before = val;

            if (style == null)
                style = PEGI_Styles.ImageButton;


            if (val)
            {
                if (Click(TrueIcon, tip, width, style)) val = false;
            }
            else
            {
                if (Click(FalseIcon, tip, width, style)) val = true;
            }

            return (before != val);
        }

        public static bool toggle(ref bool val, string text)
        {
            checkLine();
            bool before = val;
            val = EditorGUILayout.Toggle(text, val);
            return (before != val) ? change : false;
        }

        public static bool toggle(ref bool val, string text, string tip)
        {
            checkLine();

            bool before = val;
            GUIContent cont = new GUIContent
            {
                text = text,
                tooltip = tip
            };
            val = EditorGUILayout.Toggle(cont, val);
            return (before != val) ? change : false;
        }
        
        public static bool Click(string txt, int width)
        {
            checkLine();
            return GUILayout.Button(txt, GUILayout.MaxWidth(width)) ? change : false;
        }

        public static bool Click(string txt)
        {
            checkLine();
            return GUILayout.Button(txt) ? change : false;
        }

        public static bool Click(string txt, string tip)
        {
            checkLine();
            GUIContent cont = new GUIContent
            {
                text = txt,
                tooltip = tip
            };
            return GUILayout.Button(cont) ? change : false;
        }
        
        public static bool Click(string txt, string tip, GUIStyle style)
        {
            checkLine();
            GUIContent cont = new GUIContent
            {
                text = txt,
                tooltip = tip
            };
            return GUILayout.Button(cont, style) ? change : false;
        }

        public static bool Click(string txt, string tip, int width, GUIStyle style)
        {
            checkLine();
            GUIContent cont = new GUIContent
            {
                text = txt,
                tooltip = tip
            };
            return GUILayout.Button(cont, style, GUILayout.MaxWidth(width)) ? change : false;
        }


        public static bool Click(string txt, string tip, int width)
        {
            checkLine();
            GUIContent cont = new GUIContent
            {
                text = txt,
                tooltip = tip
            };
            return GUILayout.Button(cont, GUILayout.MaxWidth(width)) ? change : false;
        }

        public static bool Click(Texture img, int width, GUIStyle style = null)   {
            if (style == null)
                style = PEGI_Styles.ImageButton;
            checkLine();
            return GUILayout.Button(img, style, GUILayout.MaxHeight(width), GUILayout.MaxWidth(width + 10)) ? change : false;
        }

        public static bool Click(Texture img, string tip, int width, GUIStyle style = null) => Click(img, tip, width, width, style);

        public static bool Click(Texture img, string tip, int width, int height, GUIStyle style = null)
        {
            if (style == null)
                style = PEGI_Styles.ImageButton;

            checkLine();
            GUIContent cont = new GUIContent
            {
                tooltip = tip,
                image = img
            };
            return GUILayout.Button(cont, style, GUILayout.MaxWidth(width+10), GUILayout.MaxHeight(height)).Dirty(); 
        }

        public static void write<T>(T field) where T : UnityEngine.Object
        {
            checkLine();
            EditorGUILayout.ObjectField(field, typeof(T), false);
        }

        public static void write<T>(T field, int width) where T : UnityEngine.Object
        {
            checkLine();
            EditorGUILayout.ObjectField(field, typeof(T), false, GUILayout.MaxWidth(width));
        }

        public static void write(Texture icon, int width) => write(icon, icon ? icon.name : "Null Icon", width, width);
 
        public static void write(Texture icon, string tip, int width) => write(icon, tip, width, width);
      
        public static void write(Texture icon, string tip, int width, int height)
        {

            checkLine();

            GUIContent c = new GUIContent
            {
                image = icon,
                tooltip = tip
            };

            GUI.enabled = false;
            pegi.SetBgColor(Color.clear);
            GUILayout.Button(c, PEGI_Styles.ImageButton ,GUILayout.MaxWidth(width+10), GUILayout.MaxHeight(height));
            pegi.PreviousBGcolor();
            GUI.enabled = true;
            
        }
        
        public static void write(string text, int width)
        {

            checkLine();

            EditorGUILayout.LabelField(text, EditorStyles.miniLabel, GUILayout.MaxWidth(width));
        }

        public static void write(string text, string tip)
        {

            checkLine();
            GUIContent cont = new GUIContent
            {
                text = text,
                tooltip = tip
            };
            EditorGUILayout.LabelField(cont, PEGI_Styles.WrappingText);
        }

        public static void write(string text, string tip, int width)
        {

            checkLine();
            GUIContent cont = new GUIContent
            {
                text = text,
                tooltip = tip
            };
            EditorGUILayout.LabelField(cont, PEGI_Styles.WrappingText, GUILayout.MaxWidth(width));
        }

        public static void write(string text) => write(text, PEGI_Styles.WrappingText);

        public static void write(string text, GUIStyle style )  {
            checkLine();
            EditorGUILayout.LabelField(text, style);
        }

        public static void write(string text, int width, GUIStyle style) {
            checkLine();
            EditorGUILayout.LabelField(text, style, GUILayout.MaxWidth(width));
        }

        public static void write(string text, string hint, GUIStyle style)
        {
            checkLine();
            EditorGUILayout.LabelField(new GUIContent(text, hint), style);
        }

        public static void write(string text, string hint, int width , GUIStyle style)
        {
            checkLine();
            EditorGUILayout.LabelField(new GUIContent(text, hint), style, GUILayout.MaxWidth(width));
        }

        public static void writeHint(string text, MessageType type)  {
            checkLine();
            EditorGUILayout.HelpBox(text, type);
        }
  
        public static IEnumerable<T> DropAreaGUI<T>() where T : UnityEngine.Object
        {
            newLine();

            Event evt = Event.current;
            Rect drop_area = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            
            GUILayout.Box("Drag & Drop");

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!drop_area.Contains(evt.mousePosition))
                        yield break;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (UnityEngine.Object o in DragAndDrop.objectReferences)
                        {
                            var cnvrt = o as T;
                            if (cnvrt)
                                yield return cnvrt;
                        }
                    }
                    break;
            }
            yield break;
        }
        
        #region Reordable List
        
        static Dictionary<IList, ReorderableList> reorderableList = new Dictionary<IList, ReorderableList>();

        static ReorderableList GetReordable<T>(this List<T> list, List_Data datas)
        {
            ReorderableList rl;
            reorderableList.TryGetValue(list, out rl);

            if (rl == null)  {
                rl = new ReorderableList(list, typeof(T), datas == null || datas.allowReorder, true, false, datas == null || datas.allowDelete);
                reorderableList.Add(list, rl);

                rl.drawHeaderCallback += DrawHeader;
                rl.drawElementCallback += DrawElement;
                rl.onRemoveCallback += RemoveItem;
            }

            return rl;
        }
        
        static IList current_Reordered_List;
        static Type current_Reordered_Type;
        static List<Type> current_Reordered_ListTypes;
        static TaggedTypes_STD current_TaggedTypes;
        static List_Data listData;

        static bool GetIsSelected(int ind) => (listData != null) ? listData.GetIsSelected(ind) : pegi.selectedEls[ind];

        static void SetIsSelected(int ind, bool val) {
            if (listData != null)
                listData.SetIsSelected(ind, val);
            else
                pegi.selectedEls[ind] = val;
        }
        
        public static bool reorder_List<T>(List<T> l, List_Data datas)
        {
            listData = datas;
            //keepTypeDatas = datas != null ? datas._keepTypeData : false;
            
            EditorGUI.BeginChangeCheck();

            if (current_Reordered_List != l) {

                current_Reordered_ListTypes = typeof(T).TryGetDerrivedClasses();

                if (current_Reordered_ListTypes == null)
                {
                    current_TaggedTypes = typeof(T).TryGetTaggetClasses();
                    if (current_TaggedTypes != null)
                        current_Reordered_ListTypes = current_TaggedTypes.Types;
                }
                else current_TaggedTypes = null;

                current_Reordered_Type = typeof(T);
                current_Reordered_List = l;
                if (datas == null)
                    pegi.selectedEls.Clear();

            }

            l.GetReordable(datas).DoLayoutList();
            return EditorGUI.EndChangeCheck();
        }
        
        static void DrawHeader(Rect rect) => GUI.Label(rect, "Ordering {0} {1}s".F(current_Reordered_List.Count.ToString(), current_Reordered_Type.ToPEGIstring_Type()));

        static void DrawElement(Rect rect, int index, bool active, bool focused) {
            
            var el = current_Reordered_List[index];

            var selected = GetIsSelected(index);

            var after = EditorGUI.Toggle(new Rect(rect.x, rect.y, 30, rect.height), selected);

            if (after != selected)
                SetIsSelected(index, after);

            rect.x += 30;
            rect.width -= 30;

            if (el != null) {

                if (el != null && current_Reordered_ListTypes != null) {
                    var ty = el.GetType();

                    GUIContent cont = new GUIContent {
                        tooltip = ty.ToString(),
                        text = el.ToPEGIstring()
                    };

                    var uo = el as UnityEngine.Object;
                    if (uo)
                        EditorGUI.ObjectField(rect, cont, uo, current_Reordered_Type, true);
                    else
                    {
                        rect.width = 100;
                        EditorGUI.LabelField(rect, cont);
                        rect.x += 100;
                        rect.width = 100;

                        if (select_Type(ref ty, current_Reordered_ListTypes, rect)) {

                            current_Reordered_List.TryChangeObjectType(index, ty, listData);

                           /* var ed = listData.TryGetElement(index);

                            var iTag = el as IGotClassTag;

                            var std = (el as ISTD);

                            if (keepTypeDatas && iTag != null && ed != null) {

                                if (std != null)
                                    ed.perTypeConfig[iTag.ClassTag] = std.Encode().ToString();
                                
                                string data;

                                if (ed.perTypeConfig.TryGetValue(iTag.AllTypes.Tag(ty), out data)) {
                                    el = Activator.CreateInstance(ty);
                                    current_Reordered_List[index] = el;
                                    (el as ISTD).Decode_ifNotNull(data);
                                }
                                else current_Reordered_List[index] = std.TryDecodeInto<object>(ty);

                            }
                            else
                                current_Reordered_List[index] = std.TryDecodeInto<object>(ty);*/
                        }
                    }
                }
                else
                {
                    rect.width = 200;
                    EditorGUI.LabelField(rect, el.ToPEGIstring());
                }
            }
            else
            {
                var ed = listData.TryGetElement(index);
                
                if (ed != null && ed.unrecognized) {

                    if (current_TaggedTypes != null) {

                        GUIContent cont = new GUIContent {
                            tooltip = "Select New Class",
                            text = "UNREC {0}".F(ed.unrecognizedUnderTag)
                        };

                        rect.width = 100;
                        EditorGUI.LabelField(rect, cont);
                        rect.x += 100;
                        rect.width = 100;

                        Type ty = null;

                        if (select_Type(ref ty, current_Reordered_ListTypes, rect)) {
                            el = Activator.CreateInstance(ty);
                            current_Reordered_List[index] = el;

                            var std = el as ISTD;

                            if (std != null) 
                                 std.Decode(ed.SetRecognized().std_dta);
                            
                        }
                    }
                } else 
                EditorGUI.LabelField(rect, "Empty {0}".F(current_Reordered_Type.ToPEGIstring_Type()));
            }
        }
        
        static void RemoveItem(ReorderableList list)
        {
            int i = list.index;
            var el = current_Reordered_List[i];
            if (el != null && current_Reordered_Type.IsUnityObject())
                current_Reordered_List[i] = null;
            else
                current_Reordered_List.RemoveAt(i);
        }
        
        #endregion

    }
}
#pragma warning restore IDE1006 // Naming Styles
#endif