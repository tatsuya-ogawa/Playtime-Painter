﻿using PlayerAndEditorGUI;
using SharedTools_Stuff;
using System.Collections.Generic;

namespace STD_Logic
{

    public class LogicBranch<T> : AbstractKeepUnrecognized_STD  , IGotName , IPEGI, IAmConditional, ICanBeDefault_STD  where T: ISTD, new() {

        public string name = "no name";

        public List<LogicBranch<T>> subBranches = new List<LogicBranch<T>>();

        public ConditionBranch conditions = new ConditionBranch();

        public List<T> elements = new List<T>();

        public override bool IsDefault => subBranches.Count ==0 && conditions.IsDefault && elements.Count == 0;

        public List<T> CollectAll(ref List<T> lst) {

            lst.AddRange(elements);

            foreach (var b in subBranches)
                b.CollectAll(ref lst);

            return lst;
        }

        public bool CheckConditions(Values vals) => conditions.CheckConditions(Values.global);

        #region Encode & Decode
        public override StdEncoder Encode() => this.EncodeUnrecognized()
            .Add_String("name", name)
            .Add("cond", conditions)
            .Add_IfNotEmpty("sub", subBranches)
            .Add_IfNotEmpty("el", elements)
            .Add_IfNotNegative("ie", inspectedElement)
            .Add_IfNotNegative("is", inspectedStuff)
            .Add_IfNotNegative("br", inspectedBranch);
        
        public override bool Decode(string subtag, string data)
        {
            switch (subtag)
            {
                case "name": name = data; break;
                case "cond": conditions.Decode(data); break;
                case "sub": data.Decode_List(out subBranches); break;
                case "el": data.Decode_List(out elements); break;
                case "ie": inspectedElement = data.ToInt(); break;
                case "is": inspectedStuff = data.ToInt(); break;
                case "br": inspectedBranch = data.ToInt(); break;
                default: return false;
            }
            return true;
        }
        #endregion

        #region Inspector

        public virtual string NameForElements => typeof(T).ToPEGIstring_Type();

        public string NameForPEGI
        {
            get { return name; }
            set { name = value; }
        }

        public override void ResetInspector() {
            inspectedElement = -1;
            inspectedBranch = -1;
            base.ResetInspector();
        }

        int inspectedElement = -1;
        int inspectedBranch = -1;

        #if PEGI
        public override bool Inspect() {
            bool changed = false;

            pegi.nl();

            changed |= "Conditions".enter_Inspect(conditions, ref inspectedStuff, 2).nl();
            
            changed |= NameForElements.enter_List(ref elements, ref inspectedElement, ref inspectedStuff, 1).nl();
            
            changed |= "Sub Branches".enter_List(ref subBranches, ref inspectedBranch, ref inspectedStuff, 3).nl();

            return changed;
        }

     
#endif
        #endregion

    }



}