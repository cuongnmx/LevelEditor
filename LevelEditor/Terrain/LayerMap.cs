﻿//Copyright © 2014 Sony Computer Entertainment America LLC. See License.txt.

using System;
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using System.Drawing;

using Sce.Atf.Adaptation;
using Sce.Atf.Dom;
using Sce.Atf.VectorMath;

using LevelEditorCore;
using LevelEditorCore.VectorMath;
using RenderingInterop;

namespace LevelEditor.Terrain
{

    public abstract class TerrainMap : DomNodeAdapter, INameable, IEditableResourceOwner
    {
        protected override void OnNodeSet()
        {
            base.OnNodeSet();

            var annotations = Annotations.GetAllAnnotation(DomNode.Type, "scea.dom.editors.attribute");            
            //could be automated 
            m_textureInfos.Add(new TerrainMapTextureInfo(DomNode, Schema.terrainMapType.diffuseAttribute, annotations));
            m_textureInfos.Add(new TerrainMapTextureInfo(DomNode, Schema.terrainMapType.normalAttribute, annotations));
            m_textureInfos.Add(new TerrainMapTextureInfo(DomNode, Schema.terrainMapType.specularAttribute, annotations));

        }

        public ulong GetMaskInstanceId()
        {
            unsafe
            {
                INativeObject nobj = this.As<INativeObject>();
                IntPtr retVal = IntPtr.Zero;
                nobj.InvokeFunction("GetMaskMapInstanceId", IntPtr.Zero, out retVal);                
                if (retVal == IntPtr.Zero) return 0;
                return (*(ulong*)retVal.ToPointer());               
            }
        }
        public ImageData GetMaskMap()
        {
            ulong instId = GetMaskInstanceId();
            return instId != 0 ? new ImageData(instId) : null;            
        }

        public void ApplyDirtyRegion(Bound2di box)
        {
            unsafe
            {
                INativeObject nobj = this.As<INativeObject>();
                IntPtr argPtr = new IntPtr(&box);
                IntPtr retVal = IntPtr.Zero;
                nobj.InvokeFunction("ApplyDirtyRegion", argPtr, out retVal);
                Dirty = true;
            }

        }

        public float MinHeight
        {
            get { return GetAttribute<float>(Schema.terrainMapType.minHeightAttribute); }
        }
        public float MaxHeight
        {
            get {return GetAttribute<float>(Schema.terrainMapType.maxHeightAttribute); }
        }

        public float MinSlope
        {
            get {return GetAttribute<float>(Schema.terrainMapType.minSlopeAttribute);}
        }

        public float MaxSlope
        {
            get {return GetAttribute<float>(Schema.terrainMapType.maxSlopeAttribute);}
        }


        public TerrainGob Parent
        {
            get { return this.GetParentAs<TerrainGob>(); }
        }
        public Point WorldToMapSpace(Vec3F posW)
        {
            Point result = new Point();
            TerrainGob terrain = this.GetParentAs<TerrainGob>();
            ImageData hmImg = terrain.GetHeightMap();
            ImageData mpImg = GetMaskMap();
            Point posH = terrain.WorldToHmapSpace(posW);

            float dx = (float)mpImg.Width / (float)hmImg.Width;
            float dy = (float)mpImg.Height / (float)hmImg.Height;

            result.X = (int)Math.Round(posH.X * dx);
            result.Y = (int)Math.Round(posH.Y * dy);
            return result;
        }
             
        #region INameable Members
        public string Name
        {
            get { return GetAttribute<string>(Schema.terrainMapType.nameAttribute); }
            set { SetAttribute(Schema.terrainMapType.nameAttribute, value); }
        }
        #endregion

        #region IEditableResourceOwner Members

        public bool Dirty
        {
            get;
            private set;
        }

        public void Save()
        {
            if (Dirty)
            {
                Uri maskuri = GetAttribute<Uri>(Schema.terrainMapType.maskAttribute);
                using (ImageData img = GetMaskMap())
                {
                    img.Save(maskuri);
                }                
                Dirty = false;
            }
        }

        #endregion

        public virtual IEnumerable<TerrainMapTextureInfo> TextureInfos
        {
            get { return m_textureInfos; }
        }

        private List<TerrainMapTextureInfo> m_textureInfos = new List<TerrainMapTextureInfo>();
    }

    public class LayerMap : TerrainMap
    {        
        public static LayerMap Create(Uri maskuri)
        {
            DomNode node = new DomNode(Schema.layerMapType.Type);
            node.SetAttribute(Schema.layerMapType.maskAttribute, maskuri);
            node.InitializeExtensions();
            LayerMap map = node.As<LayerMap>();
            map.Name = "LayerMap";
            return map;
        }
                
    }

    public class DecorationMap : TerrainMap
    {
        public static DecorationMap Create(Uri maskuri)
        {
            DomNode node = new DomNode(Schema.decorationMapType.Type);
            node.SetAttribute(Schema.decorationMapType.maskAttribute, maskuri);
            node.InitializeExtensions();
            DecorationMap map = node.As<DecorationMap>();
            map.Name = "DecorationMap";
            return map;
        }
    }


    /// <summary>
    /// Encapsulates texture info for terrainMap.</summary>
    public class TerrainMapTextureInfo
    {
        public TerrainMapTextureInfo(DomNode node, AttributeInfo attrInfo, IEnumerable<XmlElement> annotations)
        {
            if (node == null || attrInfo == null)
                throw new ArgumentNullException();

            m_attrInfo = attrInfo;
            m_node = node;

            XmlElement elm = annotations.First(annot => annot.GetAttribute("name") == attrInfo.Name);
            if (elm != null)
            {

                m_displayName = elm.GetAttribute("displayName");
                m_description = elm.GetAttribute("description");
            }
            else
            {
                m_displayName = attrInfo.Name;
                m_description = attrInfo.Name;
            }                        
        }

        public string Name
        {
            get { return m_displayName; }
        }

        public Uri Uri
        {
            get { return (Uri)m_node.GetAttribute(m_attrInfo); }
        }

        public string Description
        {
            get { return m_description; }
        }

        private string m_displayName;
        private string m_description;
        private AttributeInfo m_attrInfo;
        private DomNode m_node;
    }
}
