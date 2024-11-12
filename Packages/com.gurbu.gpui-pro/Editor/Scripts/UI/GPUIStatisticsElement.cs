// GPU Instancer Pro
// Copyright (c) GurBu Technologies

using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GPUInstancerPro
{
#if UNITY_6000_0_OR_NEWER
    [UxmlElement]
#endif
    public partial class GPUIStatisticsElement : VisualElement
    { 
        public static readonly string ussClassName = "gpui-statistics-element";
        public static readonly string iconUssClassName = ussClassName + "__icon";
        public static readonly string titleUssClassName = ussClassName + "__title";
        public static readonly string countUssClassName = ussClassName + "__count";
        public static readonly string lgdUssClassName = ussClassName + "__lgd";
        public static readonly string visibilityDataUssClassName = ussClassName + "__visibilityData";
        public static readonly string visibilityDataLODUssClassName = ussClassName + "__visibilityData-lod";

        private VisualElement _iconVE;
        private Label _titleLabel;
        private Label _countLabel;
        private ObjectField _lgdField;
        private VisualElement _detailsVE;
        private VisualElement _visibilityDataVE;
        private Label _drawCallsLabel;
        private Label _vertsLabel;

        private int _renderKey;
        private int _lodCount;
        private Label[] _lodLabels;

#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public string title
        {
            get
            {
                return _titleLabel.text;
            }
            set
            {
                _titleLabel.text = value;
            }
        }

#if UNITY_6000_0_OR_NEWER
        [UxmlAttribute]
#endif
        public string countText
        {
            get
            {
                return _countLabel.text;
            }
            set
            {
                _countLabel.text = value;
            }
        }

        public Texture2D icon
        {
            set
            {
                _iconVE.style.backgroundImage = value;
            }
        }

        public GPUIStatisticsElement()
        {
            AddToClassList(ussClassName);

            VisualElement horizontalVE = new VisualElement();
            horizontalVE.style.flexDirection = FlexDirection.Row;
            Add(horizontalVE);

            _iconVE = new VisualElement();
            _iconVE.name = "IconVE";
            _iconVE.AddToClassList(iconUssClassName);
            horizontalVE.Add(_iconVE);

            VisualElement verticalVE = new VisualElement();
            verticalVE.style.flexGrow = 1;
            horizontalVE.Add(verticalVE);

            _titleLabel = new Label();
            _titleLabel.name = "TitleLabel";
            _titleLabel.AddToClassList(titleUssClassName);
            verticalVE.Add(_titleLabel);

            _countLabel = new Label();
            _countLabel.name = "CountLabel";
            _countLabel.AddToClassList(countUssClassName);
            verticalVE.Add(_countLabel);

            _lgdField = new ObjectField();
            _lgdField.name = "LGDObjectField";
            _lgdField.AddToClassList(lgdUssClassName);
            _lgdField.SetEnabled(false);
            //_lgdField.SetVisible(false);
            verticalVE.Add(_lgdField);

            VisualElement horizontal2VE = new VisualElement();
            horizontal2VE.style.flexDirection = FlexDirection.Row;
            verticalVE.Add(horizontal2VE);

            _detailsVE = new VisualElement();
            _detailsVE.name = "DetailsVE";
            _detailsVE.style.minWidth = 100;
            _detailsVE.style.paddingRight = 10;
            horizontal2VE.Add(_detailsVE);

            _drawCallsLabel = new Label("DrawCalls: X");
            _detailsVE.Add(_drawCallsLabel);
            _vertsLabel = new Label("Verts: X");
            _detailsVE.Add(_vertsLabel);

            _visibilityDataVE = new VisualElement();
            _visibilityDataVE.name = "VisibilityDataVE";
            _visibilityDataVE.AddToClassList(visibilityDataUssClassName);
            _visibilityDataVE.style.flexGrow = 1;
            //_visibilityDataVE.SetVisible(false);
            horizontal2VE.Add(_visibilityDataVE);
        }

        public void SetData(GPUIPrototype prototype, string countText, int renderKey, bool showVisibilityData)
        {
            this.title = prototype.ToString();
            this._renderKey = renderKey;

            if (GPUIRenderingSystem.TryGetLODGroupData(prototype, out GPUILODGroupData lodGroupData))
                _lodCount = lodGroupData.Length;
            else
                _lodCount = prototype.GetLODCount();

            _lodLabels = new Label[_lodCount];
            _visibilityDataVE.Clear();
            VisualElement ve = null;
            int divider = 1;
            if (_lodCount > 2)
                divider++;
            if (_lodCount > 4)
                divider++;
            if (_lodCount > 6)
                divider++;
            for (int i = 0; i < _lodCount; i++)
            {
                if (i % divider == 0)
                {
                    ve = new VisualElement();
                    ve.AddToClassList(visibilityDataLODUssClassName);
                    _visibilityDataVE.Add(ve);
                }

                Label lodVD = new Label("");
                lodVD.enableRichText = true;
                lodVD.style.width = 75;
                lodVD.style.unityTextAlign = TextAnchor.LowerLeft;
                lodVD.style.fontSize = 12;
                ve.Add(lodVD);
                _lodLabels[i] = lodVD;
            }

            UpdateVisibilityData(showVisibilityData);
            if (!string.IsNullOrEmpty(countText))
                this.countText = countText;
        }

        public void UpdateVisibilityData(bool showVisibilityData = true)
        {
            _lgdField.SetVisible(false);
            _visibilityDataVE.SetVisible(false);
            _detailsVE.SetVisible(false);
            _vertsLabel.SetVisible(false);
            if (_renderKey != 0)
            {
                if (GPUIRenderingSystem.TryGetRenderSourceGroup(_renderKey, out GPUIRenderSourceGroup rsg))
                {
                    GPUILODGroupData lgd = rsg.LODGroupData;
                    if (lgd == null || rsg.lodRenderStatistics == null || lgd.Length != rsg.lodRenderStatistics.Length) return;

                    _lgdField.value = lgd;
                    _lgdField.SetVisible(true);
                    countText = rsg.InstanceCount.FormatNumberWithSuffix();

                    int drawCallCount = 0;
                    int shadowDC = 0;
                    if (showVisibilityData)
                    {
                        long vertCount = 0;

                        GPUICameraData cameraData = GPUIRenderingSystem.Instance.CameraDataProvider.GetSceneViewCameraData();
                        if (Application.isPlaying)
                            cameraData = GPUIRenderingSystem.Instance.CameraDataProvider.GetFirstValue();

                        if (cameraData != null)
                        {
                            var visibilityArray = cameraData.GetVisibilityBuffer().GetRequestedData();
                            if (visibilityArray.IsCreated && cameraData.TryGetVisibilityBufferIndex(rsg, out int index))
                            {
                                for (int l = 0; l < _lodCount; l++)
                                {
                                    GPUIVisibilityData visibilityData = visibilityArray[index + l];
                                    long visibleCount = visibilityData.visibleCount;
                                    _lodLabels[l].text = "<size=10>LOD" + l + ": </size>" + visibleCount.FormatNumberWithSuffix();

                                    drawCallCount += (int)rsg.lodRenderStatistics[l].drawCount;
                                    shadowDC += (int)rsg.lodRenderStatistics[l].shadowDrawCount;
                                    vertCount += visibleCount * rsg.lodRenderStatistics[l].vertexCount;
                                    if (rsg.Profile != null && rsg.Profile.isShadowCasting)
                                        vertCount += visibilityArray[index + _lodCount + l].visibleCount * rsg.lodRenderStatistics[l].shadowVertexCount;
                                }
                            }
                        }

                        _visibilityDataVE.SetVisible(true);
                        _vertsLabel.text = "<size=10>Verts: </size>" + vertCount.FormatNumberWithSuffix();
                        _vertsLabel.SetVisible(true);
                    }
                    else
                    {
                        for (int l = 0; l < _lodCount; l++)
                        {
                            drawCallCount += (int)rsg.lodRenderStatistics[l].drawCount;
                            shadowDC += (int)rsg.lodRenderStatistics[l].shadowDrawCount;
                        }
                    }
                    _drawCallsLabel.text = "<size=10>Draw: </size>" + (drawCallCount + shadowDC) + (shadowDC == 0 ? "" : " <size=8>[S: </size><size=10>" + shadowDC + "</size><size=8>]</size>");
                    _detailsVE.SetVisible(true);
                }
            }
        }

#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<GPUIStatisticsElement, UxmlTraits> { }

        public new class UxmlTraits : VisualElement.UxmlTraits
        {
            private UxmlStringAttributeDescription _title = new UxmlStringAttributeDescription
            {
                name = "title",
                defaultValue = "Object Name"
            };

            private UxmlStringAttributeDescription _count = new UxmlStringAttributeDescription
            {
                name = "count",
                defaultValue = "1234"
            };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                GPUIStatisticsElement element = ve as GPUIStatisticsElement;
                element.title = _title.GetValueFromBag(bag, cc);
                element.countText = _count.GetValueFromBag(bag, cc);
            }
        }
#endif
    }
}