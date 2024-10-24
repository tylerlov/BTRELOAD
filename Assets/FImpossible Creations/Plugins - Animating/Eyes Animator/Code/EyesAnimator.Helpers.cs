using System;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.FEyes
{
    public partial class FEyesAnimator : UnityEngine.EventSystems.IDropHandler, IFHierarchyIcon
    {
        public bool debugSwitch = false;
        public string EditorIconPath { get { if (PlayerPrefs.GetInt("EyesH", 1) == 0) return ""; else return "Eyes Animator/EyesAnimator_IconSmall"; } }
        public void OnDrop(UnityEngine.EventSystems.PointerEventData data) { }

        void Reset()
        {
            _baseTransform = transform;
        }

        /// <summary>
        /// TODO: Full implementation of all arrays inside just this class
        /// </summary>
        [System.Serializable]
        public class Eye
        {
            // Info
            public Vector3 forward;
            public Quaternion initLocalRotation;
            public Quaternion lerpRotation;

            // Random motion
            public Vector3 randomDir;
            public float randomTimer;

            // Lag motion
            public float lagTimer;
            public float lagProgress;
            public Quaternion lagStartRotation;
            public void SetLagStartRotation(Transform baseTr, Quaternion worldRot) 
            { lagStartRotation = previousLookRotBase; }
            //{ lagStartRotation = FEngineering.QToLocal(baseTr.rotation, worldRot); }
            public Quaternion GetLagStartRotation(Transform baseTr) 
            { return lagStartRotation; }
            //{ return FEngineering.QToWorld(baseTr.rotation, lagStartRotation); }


            public float changeSmoother;

            /// <summary> Mainly for supporting eye lags </summary>
            public Quaternion previousLookRotBase;
        }

        [System.Serializable]
        public class EyeSetup
        {
            public SkinnedMeshRenderer BlendshapeMesh = null;
            public int EyeRightShape = 0;
            public int EyeLeftShape = 0;
            public int EyeUpShape = 0;
            public int EyeDownShape = 0;
            public Vector2 MinMaxValue = new Vector2(0, 100);

            public Vector2 IndividualClampingHorizontal = new Vector2(-60, 60);
            public Vector2 IndividualClampingVertical = new Vector2(-20,20);

            public enum EEyeControlType
            {
                RotateBone, Blendshape
            }

            public EEyeControlType ControlType = EEyeControlType.RotateBone;

            [HideInInspector] public bool _BlendFoldout = false;

            internal void EyeLeftX(float x)
            {
                float value = Mathf.Lerp(MinMaxValue.x, MinMaxValue.y, Mathf.Abs( Mathf.Min(0f, x)));
                BlendshapeMesh.SetBlendShapeWeight(EyeLeftShape, value);
            }
            internal void EyeRightX(float x)
            {
                float value = Mathf.Lerp(MinMaxValue.x, MinMaxValue.y, Mathf.Max(0f, x));
                BlendshapeMesh.SetBlendShapeWeight(EyeRightShape, value);
            }
            internal void EyeDownY(float y)
            {
                float value = Mathf.Lerp(MinMaxValue.x, MinMaxValue.y, Mathf.Abs(Mathf.Min(0f, y)));
                BlendshapeMesh.SetBlendShapeWeight(EyeDownShape, value);
            }
            internal void EyeUpY(float y)
            {
                float value = Mathf.Lerp(MinMaxValue.x, MinMaxValue.y, Mathf.Max(0f, y));
                BlendshapeMesh.SetBlendShapeWeight(EyeUpShape, value);
            }
        }

        [HideInInspector] public List<EyeSetup> EyeSetups = new List<EyeSetup>();




        /// <summary>
        /// Searching through component's owner to find head or neck bone
        /// </summary>
        public virtual void FindHeadBone()
        {
            // First let's check if it's humanoid character, then we can get head bone transform from it
            Animator animator = GetComponent<Animator>();
            Transform animatorHeadBone = null;
            if( animator )
            {
                if( animator.isHuman )
                {
                    animatorHeadBone = animator.GetBoneTransform( HumanBodyBones.Head );
                }
            }


            Transform headBone = null;
            Transform probablyWrongTransform = null;

            foreach( Transform t in GetComponentsInChildren<Transform>() )
            {
                if( t == transform ) continue;

                if( t.name.ToLower().Contains( "head" ) )
                {
                    if( t.GetComponent<SkinnedMeshRenderer>() )
                    {
                        if( t.parent == transform ) continue; // If it's just mesh object from first depths
                        probablyWrongTransform = t;
                        continue;
                    }

                    headBone = t;
                    break;
                }
            }

            if( !headBone )
                foreach( Transform t in GetComponentsInChildren<Transform>() )
                {
                    if( t.name.ToLower().Contains( "neck" ) )
                    {
                        headBone = t;
                        break;
                    }
                }

            if( headBone == null && animatorHeadBone != null )
                headBone = animatorHeadBone;
            else
            if( headBone != null && animatorHeadBone != null )
            {
                if( animatorHeadBone.name.ToLower().Contains( "head" ) ) headBone = animatorHeadBone;
                else
                    if( !headBone.name.ToLower().Contains( "head" ) ) headBone = animatorHeadBone;
            }

            if( headBone )
            {
                HeadReference = headBone;
                FindingEyes();
            }
            else
            {
                if( probablyWrongTransform ) HeadReference = probablyWrongTransform;
                Debug.LogWarning( "Found " + probablyWrongTransform + " but it's probably wrong transform" );
            }
        }

        /// <summary> Searching through component's owner to find eye bones </summary>
        public virtual void FindingEyes()
        {
            if( HeadReference == null ) return;

            // Trying to find eye bones inside head bone
            Transform[] children = HeadReference.GetComponentsInChildren<Transform>();

            for( int i = 0; i < children.Length; i++ )
            {
                string lowerName = children[i].name.ToLower();
                if( lowerName.Contains( "eye" ) )
                {
                    if( lowerName.Contains( "brow" ) || lowerName.Contains( "lid" ) || lowerName.Contains( "las" ) ) continue;

                    if( lowerName.Contains( "left" ) ) { if( !Eyes.Contains( children[i] ) ) Eyes.Add( children[i] ); continue; }
                    else
                        if( lowerName.Contains( "l" ) ) { if( !Eyes.Contains( children[i] ) ) Eyes.Add( children[i] ); continue; }

                    if( lowerName.Contains( "right" ) ) { if( !Eyes.Contains( children[i] ) ) Eyes.Add( children[i] ); continue; }
                    else
                        if( lowerName.Contains( "r" ) ) { if( !Eyes.Contains( children[i] ) ) Eyes.Add( children[i] ); continue; }
                }
            }


            if( HeadReference )
            {
                if( HeadReference == null ) return;

                if( EyeLids == null ) EyeLids = new List<Transform>();
                if( DownEyelids == null ) DownEyelids = new List<Transform>();
                if( UpEyelids == null ) UpEyelids = new List<Transform>();

                // Trying to find eyelid bones inside eyes bones
                for( int e = 0; e < Eyes.Count; e++ )
                {
                    children = Eyes[e].GetComponentsInChildren<Transform>();

                    for( int i = 0; i < children.Length; i++ )
                    {
                        string lowerName = children[i].name.ToLower();
                        if( lowerName.Contains( "lid" ) )
                        {
                            if( lowerName.Contains( "low" ) || lowerName.Contains( "down" ) || lowerName.Contains( "bot" ) )
                            {
                                if( !DownEyelids.Contains( children[i] ) ) DownEyelids.Add( children[i] );
                            }
                            else
                            if( lowerName.Contains( "up" ) || lowerName.Contains( "top" ) )
                            {
                                if( !UpEyelids.Contains( children[i] ) ) UpEyelids.Add( children[i] );
                            }
                            else
                            {
                                if( !EyeLids.Contains( children[i] ) ) EyeLids.Add( children[i] );
                            }
                        }
                    }

                    UpdateLists();
                }
            }
        }

    }
}