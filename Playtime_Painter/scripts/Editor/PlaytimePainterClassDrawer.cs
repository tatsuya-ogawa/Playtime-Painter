﻿using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using PlayerAndEditorGUI;
#if UNITY_EDITOR

namespace Painter {

    [CustomEditor(typeof(PlaytimePainter))]
    public class PlaytimePainterClassDrawer : SceneViewEditable<PlaytimePainter> {

        static PainterConfig cfg { get { return PainterConfig.inst; } }

        public override bool AllowEditing(PlaytimePainter targ) {
            return (targ != null) && (targ.LockEditing == false) && ((targ.curImgData != null) || (targ.meshEditing));
        }
        
        public override bool OnEditorRayHit(RaycastHit hit, Ray ray) {

            Transform tf = hit.transform;
            PlaytimePainter p = tf == null ? null : tf.GetComponent<PlaytimePainter>();
            Event e = Event.current;

            if ((painter != null) && (painter.meshEditing)) {

                PlaytimePainter edited = MeshManager.inst.target;
                
                bool allowRefocusing = false;

                if (p != null) {

					if ((p != edited) && (p.meshEditing) && (p.gotMeshData()) && L_mouseDwn && (e.button == 0)) {
                        MeshManager.inst.EditMesh(p, false);
                        allowRefocusing = true;
                    }

                    if ((edited == null) || (edited != p))
                        allowRefocusing = true;
                    
                }

                if ((((e.button == 1) && (!MeshManager.inst.draggingSelected)) || (e.button == 2)) && ((e.type == EventType.mouseDown) || (e.type == EventType.mouseDrag) || (e.type == EventType.mouseUp)))
                    navigating = true;

              //  if (allowRefocusing)
                //  Debug.Log("Allowing refocus");

                return allowRefocusing;
            }

            if (L_mouseDwn) PlaytimePainter.currently_Painted_Object = null;

            if (p != null) {

                if (!p.meshEditing) {
                    StrokeVector st = p.stroke;
                    st.mouseUp = L_mouseUp;
                    st.mouseDwn = L_mouseDwn;
                    st.uvTo = hit.textureCoord;
                    st.posTo = hit.point;
                }

                p.OnMouseOver_SceneView(hit, e);

               
            }

            if (L_mouseUp) PlaytimePainter.currently_Painted_Object = null;

            if (((e.button == 1) || (e.button == 2)) && ((e.type == EventType.mouseDown) || (e.type == EventType.mouseDrag) || (e.type == EventType.mouseUp)))
                navigating = true;

            return true;
        }

        public override void getEvents(Event e, Ray ray) {
            if ((painter!= null) && (painter.meshEditing))
            MeshManager.inst.UpdateInputEditorTime(e, ray, L_mouseUp, L_mouseDwn);
        

            if ((e.type == EventType.keyDown) && (painter != null) && (painter.meshEditing == false)) {
                imgData id = painter.curImgData;
                if (id != null) {
                    if ((e.keyCode == KeyCode.Z) && (id.cache.undo.gotData()))
                        id.cache.undo.ApplyTo(id);
                    else if ((e.keyCode == KeyCode.X) && (id.cache.redo.gotData()))
                        id.cache.redo.ApplyTo(id);
                }
            }
        }
        
        public override void GridUpdate(SceneView sceneview) {

            base.GridUpdate(sceneview);

            if (!isCurrentTool()) return;

            if ((painter != null) && (painter.textureWasChanged))
                painter.Update();

        }

        static string[] texSizes;
        const int range = 9;
        const int minPow = 4;





        public override void OnInspectorGUI() {

            bool changes = false;

            ef.start(serializedObject);
            painter = (PlaytimePainter)target;

            if (!PlaytimePainter.isCurrent_Tool()) {
                if (pegi.Click(icon.Off, "Click to Enable Tool", 25)) {
                    PlaytimeToolComponent.enabledTool = typeof(PlaytimePainter);//  customTools.Painter;
                    CloseAllButThis(painter);

                    painter.CheckPreviewShader();
                }
                painter.gameObject.end();
                return;
            } else {

                if ((isCurrentTool() && (painter.terrain != null) && (Application.isPlaying == false) && (UnityEditorInternal.InternalEditorUtility.GetIsInspectorExpanded(painter.terrain) == true)) ||
                    (pegi.Click(icon.On.getIcon(), "Click to Disable Tool", 25))) {
                    PlaytimeToolComponent.enabledTool = null; //customTools.Disabled;
                    MeshManager.inst.DisconnectMesh();
                    painter.SetOriginalShader();
                    painter.UpdateOrSetTexTarget(texTarget.Texture2D);
                }
            }

            painter.InitIfNotInited();





            PainterManager rtp = PainterManager.inst;
            BrushConfig brush = PlaytimePainter.brush;
            imgData image = painter.curImgData;

            Texture tex = painter.getTexture();
            if ((!painter.meshEditing) && ((tex != null) && (image == null)) || ((image != null) && (tex == null)) || ((image != null) && (tex != image.texture2D) && (tex != image.currentTexture())))
                painter.textureWasChanged = true;


            changes = painter.management_PEGI();

            if (painter.meshEditing) { painter.gameObject.end();  return; } 

            if ((painter.meshRenderer != null) || (painter.terrain != null)) {

                if ((!cfg.showConfig) && (!painter.LockEditing)) {
                    if (image != null) {

                        ef.newLine();

                        if (!painter.isTerrainControlTexture()) {

                            string Orig = "";

                            if (image.texture2D != null) {
                                Orig = painter.curImgData.texture2D.GetPathWithout_Assets_Word();
                                if ((pegi.Click(icon.Load, "Will reload " + Orig, 25))) {
                                    painter.ForceReimportMyTexture(Orig);
                                    painter.curImgData.SaveName = painter.curImgData.texture2D.name;
                                    GUI.FocusControl("dummy");
                                    if (painter.terrain != null)
                                        painter.UpdateShaderGlobalVariables();
                                }
                            }


                            "Texture Name: ".edit(70, ref painter.curImgData.SaveName);

                            if (image.texture2D != null) {

                                string Dest = painter.GenerateTextureSavePath();
                                bool existsAtDestination = painter.textureExistsAtDestinationPath();
                                bool originalExists = (Orig != null);
                                bool sameTarget = originalExists && (Orig.Equals(Dest));
                                bool sameTextureName = originalExists && image.texture2D.name.Equals(image.SaveName);


                                if ((existsAtDestination == false) || sameTextureName) {
                                    if (ef.Click(Icons_MGMT.getIcon(sameTextureName ? icon.save : icon.saveAsNew), (sameTextureName ? "Will Update " + Orig : "Will save as " + Dest), 25)) {
                                        if (sameTextureName)
                                            painter.RewriteOriginalTexture();
                                        else
                                            painter.SaveTextureAsAsset(false);

                                        painter.OnChangedTexture();
                                    }
                                } else if (existsAtDestination && (pegi.Click(icon.save, "Will replace " + Dest, 25)))
                                    painter.SaveTextureAsAsset(false);


                                ef.newLine();

                                if ((!sameTarget) && (!sameTextureName) && (string.IsNullOrEmpty(Orig) == false) && (ef.Click("Replace", "Will replace " + Orig + " with " + Dest)))
                                    painter.RewriteOriginalTexture_Rename(image.SaveName);

                            }
                        }
                        ef.newLine();


                        if (changes)
                            painter.Update_Brush_Parameters_For_Preview_Shader();

                    }
                    ef.newLine();

                    pegi.Space();
                    pegi.newLine();

                    ef.write("Mat:", 30);
                    List<string> mats = painter.getMaterials();
                    if ((mats != null) && (mats.Count > 0)) {
                        if (ef.select(ref painter.selectedMaterial, mats.ToArray())) {
                            painter.OnChangedTexture();
                            image = painter.curImgData;
                        }
                    }


                    Material mater = painter.getMaterial(false);

                    pegi.write(mater);
         

                    if ((cfg.moreOptions || (mater == null) || (mater == rtp.defaultMaterial) || (image == null))
                        && (icon.NewMaterial.Click("Instantiate Material", 25).nl()))
                        painter.InstantiateMaterial(true);



                    if ((mats != null) && (mats.Count > 1)) {
                        "Auto Select Material:".toggle("Material will be changed based on the submesh you are painting on", 120,
                                                       ref painter.autoSelectMaterial_byNumberOfPointedSubmesh).nl();
                    }

                    pegi.nl();
                    ef.Space();
                    ef.newLine();

                    pegi.write("Tex:", "Texture field on the material", 30);

                    if (painter.SelectTexture_PEGI()) {
                        image = painter.curImgData;
                        if (image == null) painter.nameHolder = painter.gameObject.name + "_" + painter.getMaterialTextureName();
                    }

                    if (image != null)
                        painter.UpdateTyling();

                    textureSetterField();

                    if (((image == null) || (cfg.moreOptions)) &&
                            (painter.isTerrainControlTexture() == false)) {

                        bool isTerrainHeight = painter.isTerrainHeightTexture();

                        int texScale = (!isTerrainHeight) ?
                             ((int)Mathf.Pow(2, PainterConfig.inst.selectedSize + minPow))
                            
                            : (painter.terrain.terrainData.heightmapResolution - 1);

                        List<string> texNames = painter.getMaterialTextureNames();

                        if (texNames.Count > painter.selectedTexture) {
                            string param = painter.getMaterialTextureName();

                            if (pegi.Click(icon.NewTexture, (image == null) ? "Create new texture2D for " + param : "Replace " + param + " with new Texture2D " + texScale + "*" + texScale, 25).nl())
                                if (isTerrainHeight)
                                    painter.createTerrainHeightTexture(painter.nameHolder);
                                else
                                    painter.createTexture2D(texScale, painter.nameHolder, cfg.newTextureIsColor);



                            if ((image == null) && (cfg.moreOptions) && ("Create Render Texture".Click()))
                                painter.CreateRenderTexture(texScale, painter.nameHolder);

                            if ((image != null) && (cfg.moreOptions)) {
                                if ((image.renderTexture == null) && ("Add Render Tex".Click()))
                                    image.AddRenderTexture();
                                if (image.renderTexture != null) {

                                    if ("Replace RendTex".Click("Replace " + param + " with Rend Tex size: " + texScale))
                                        painter.CreateRenderTexture(texScale, painter.nameHolder);
                                    if ("Remove RendTex".Click().nl()) {
                                        if (image.texture2D != null) {
                                            painter.UpdateOrSetTexTarget(texTarget.Texture2D);
                                            image.renderTexture = null;
                                        } else {
                                            painter.curImgData = null;
                                            painter.setTextureOnMaterial();
                                        }

                                    }
                                }
                            }
                        } else
                            "No Material's Texture selected".nl();

                        pegi.nl();

                        if (image == null)
                            "_Name:".edit("Name for new texture", 40, ref painter.nameHolder);


                        if (!isTerrainHeight) {
                            "Color:".toggle("Will the new texture be a Color Texture", 40, ref cfg.newTextureIsColor);
                            ef.write("Size:", "Size of the new Texture", 40);
                            if ((texSizes == null) || (texSizes.Length != range))
                            {
                                texSizes = new string[range];
                                for (int i = 0; i < range; i++)
                                    texSizes[i] = Mathf.Pow(2, i + minPow).ToString();
                            }

                            ef.select(ref PainterConfig.inst.selectedSize, texSizes, 60);
                        }
                        ef.newLine();
                    }

                    ef.newLine();
                    ef.tab();
                    ef.newLine();

                    List<Texture> recentTexs;

                    string texName = painter.getMaterialTextureName();

                    if ((texName != null) && (rtp.recentTextures.TryGetValue(texName, out recentTexs))
                        && ((recentTexs.Count > 1) || (painter.curImgData == null))) {
                        ef.write("Recent Texs:", 60);
                        Texture tmp = painter.curImgData.exclusiveTexture();
                        if (pegi.select(ref tmp, recentTexs))
                            painter.ChangeTexture(tmp);
                    }

                    ef.Space();
                    ef.newLine();
                    ef.Space();
                }

            } else {
                painter.meshRenderer = (Renderer)EditorGUILayout.ObjectField(painter.meshRenderer, typeof(Renderer), true);
                if ((painter.meshRenderer != null) && (painter.meshRenderer.gameObject != painter.gameObject)) {
                    painter.meshRenderer = null;
                    Debug.Log("Attach directly to GameObject with Renderer");
                }

            }


            painter.gameObject.end();

        }

        bool textureSetterField() {
            string field = painter.getMaterialTextureName();
            if ((field == null) || (field.Length == 0)) return false;

            Texture tex = painter.getTexture();
            Texture after = (Texture)EditorGUILayout.ObjectField(tex, typeof(Texture), true);

            if (tex != after) {
                painter.ChangeTexture(after);
                return true;
            }

            return false;

        }

     



    }
#endif
}

