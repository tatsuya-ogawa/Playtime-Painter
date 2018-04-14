﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Playtime_Painter
{
    [System.Serializable]
    public class TerrainSplatTexture : PainterPluginBase
    {
        public override bool getTexture(string fieldName, ref Texture tex, PlaytimePainter painter)
        {
            if ((painter.terrain != null) && (fieldName.Contains(PainterConfig.terrainTexture)))
            {
                int no = fieldName[0].charToInt();
                tex = painter.terrain.terrainData.splatPrototypes[no].texture;
                return true;
            }
            return false;
        }

        public override void GetNonMaterialTextureNames(PlaytimePainter painter, ref List<string> dest)
        {
            if (painter.terrain != null)
            {
                SplatPrototype[] sp = painter.terrain.terrainData.splatPrototypes;
                for (int i = 0; i < sp.Length; i++)
                {
                    if (sp[i].texture != null)
                        dest.Add(i + PainterConfig.terrainTexture + sp[i].texture.name);
                }
            }
        }

        public override bool UpdateTylingFromMaterial(string fieldName, PlaytimePainter painter)
        {

            if (painter.terrain != null)
            {
                if (fieldName.Contains(PainterConfig.terrainTexture))
                {
                    int no = fieldName[0].charToInt();

                    SplatPrototype[] splats = painter.terrain.terrainData.splatPrototypes;
                    if (splats.Length <= no) return true; ;

                    SplatPrototype sp = painter.terrain.terrainData.splatPrototypes[no];

                    float width = painter.terrain.terrainData.size.x / sp.tileSize.x;
                    float length = painter.terrain.terrainData.size.z / sp.tileSize.y;

                    var id = painter.imgData;
                    id.tiling = new Vector2(width, length);
                    id.offset = sp.tileOffset;
                    return true;
                }
            }
            return false;
        }

        public override bool setTextureOnMaterial(string fieldName, ImageData id, PlaytimePainter painter)
        {
            Texture tex = id.currentTexture();
            if (painter.terrain != null)
            {
                if (fieldName.Contains(PainterConfig.terrainTexture))
                {
                    int no = fieldName[0].charToInt();
                    painter.terrain.setSplashPrototypeTexture(id.texture2D, no);
                    if (tex.GetType() != typeof(Texture2D))

                        //else
                        Debug.Log("Can only use Texture2D for Splat Prototypes. If using regular terrain may not see changes.");
                    else
                    {

#if UNITY_EDITOR
                        UnityEditor.TextureImporter timp = ((Texture2D)tex).getTextureImporter();
                        if (timp != null)
                        {
                            bool needReimport = timp.wasClamped();
                            needReimport |= timp.hadNoMipmaps();

                            if (needReimport)
                                timp.SaveAndReimport();
                        }
#endif

                    }
                    return true;
                }
            }
            return false;
        }
    }

}