using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

namespace razz
{
    [CustomEditor(typeof(OrbitalReach))]
    public class OrbitalReachEditor : Editor
    {
        private OrbitalReach _script;
        private Animator _animator;
        private AnimatorController _animatorController;
        private int _orbitalLayerIndex;
        private int _paramIndex;
        private string[] _paramaterNames;
        private bool[] _paramTypes;
        private string[] _orbitalAnimClips;
        private string _folderPath;
        private string _file;
        private AnimatorControllerLayer _orbitalLayer;
        private AnimatorStateMachine _stateMachine;

        #region ToAddNewLater
        private void CreateParameterArray()
        {
            int count = 5;
            _paramaterNames = new string[count];
            _paramaterNames[0] = OrbitalReach.OAngle;
            _paramaterNames[1] = OrbitalReach.OHeight;
            _paramaterNames[2] = OrbitalReach.OMirror;
            _paramaterNames[3] = OrbitalReach.OSpeed;
            _paramaterNames[4] = OrbitalReach.OCycle;
            
            _paramTypes = new bool[count]; //Bool or Float type
            _paramTypes[2] = true;
        }
        private void CreateOrbitalAnimArray()
        {
            _folderPath = "Assets/Interactor/Core/OrbitalAnimations/";
            _file = ".anim";
            _orbitalAnimClips = new string[20];
            _orbitalAnimClips[0] = "CrouchLB";
            _orbitalAnimClips[1] = "CrouchL";
            _orbitalAnimClips[2] = "CrouchF";
            _orbitalAnimClips[3] = "CrouchR";
            _orbitalAnimClips[4] = "CrouchRB";
            _orbitalAnimClips[5] = "HighLB";
            _orbitalAnimClips[6] = "HighL";
            _orbitalAnimClips[7] = "HighF";
            _orbitalAnimClips[8] = "HighR";
            _orbitalAnimClips[9] = "HighRB";
            _orbitalAnimClips[10] = "LowLB";
            _orbitalAnimClips[11] = "LowL";
            _orbitalAnimClips[12] = "LowF";
            _orbitalAnimClips[13] = "LowR";
            _orbitalAnimClips[14] = "LowRB";
            _orbitalAnimClips[15] = "MidLB";
            _orbitalAnimClips[16] = "MidL";
            _orbitalAnimClips[17] = "MidF";
            _orbitalAnimClips[18] = "MidR";
            _orbitalAnimClips[19] = "MidRB";
        }
        #endregion

        private void OnEnable()
        {
            if (!_script) _script = (OrbitalReach)target;
            OnStart();
            EditorApplication.update += OnUpdate;
        }
        private void OnStart()
        {
            if (!EditorApplication.isPlaying) return;

            if (_script.initiated && _script.debugOrbitalPositioner) SetupDebug();
        }
        private void OnUpdate()
        {
            if (!EditorApplication.isPlaying || _script.clipPlaying || !_script.initiated || !_script.debug) return;

            if (_script.debugOrbitalPositioner) UpdateDebug();
            else _script.debug = false;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            GUILayout.Space(10f);

            if (!Application.isPlaying)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Add OrbitalReachLayer", "This adds "))) AddOrbitalReachLayer();
                if (GUILayout.Button(new GUIContent("Remove OrbitalReachLayer", "Removes "))) RemoveOrbitalReachLayer(true);
                GUILayout.EndHorizontal();
            }

            if (GUI.changed && _script.debugOrbitalPositioner)
            {
                _script.debugOrbitalPositioner.debugDuration = _script.debugDuration;
            }
        }
        private bool Init()
        {
            if (!_animator) _animator = _script.animator;
            if (_animator == null)
            {
                Debug.LogWarning("Animator is not assigned on OrbitalReach component!", _script);
                return false;
            }
            if (!_animator.isHuman)
            {
                Debug.LogWarning("OrbitalReach only works for humanoid characters.", _animator);
                return false;
            }

            _animatorController = _animator.runtimeAnimatorController as AnimatorController;
            if (_animatorController == null)
            {
                Debug.LogWarning("Animator Controller is not assigned on Animator component!", _animator);
                return false;
            }
            return true;
        }
        private bool CheckOrbitalParameters(string paramName)
        {
            _paramIndex = -1;
            AnimatorControllerParameter[] parameters = _animatorController.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == paramName)
                {
                    _paramIndex = i;
                    return true;
                }
            }
            return false;
        }
        private bool CheckOrbitalReachLayer()
        {
            _orbitalLayerIndex = -1;
            for (int i = 0; i < _animatorController.layers.Length; i++)
            {
                if (_animatorController.layers[i].name == OrbitalReach.OrbitalLayer)
                {
                    _orbitalLayerIndex = i;
                    break;
                }
            }
            return (_orbitalLayerIndex >= 0);
        }
        private void AddOrbitalReachLayer()
        {
            if (!Init()) return;
            if (!CheckOrbitalAnimClips()) return;

            AddLayer();
            FillOrbitalReachLayer();
        }
        private void AddLayer()
        {
            RemoveOrbitalReachLayer(false);
            CreateParameterArray();
            for (int i = 0; i < _paramaterNames.Length; i++)
            {
                if (!CheckOrbitalParameters(_paramaterNames[i])) AddParameter(i);
            }

            _animatorController.AddLayer(OrbitalReach.OrbitalLayer);
            _orbitalLayerIndex = _animatorController.layers.Length - 1;
            _script.orbitalLayerIndex = _orbitalLayerIndex;
            _orbitalLayer = _animatorController.layers[_orbitalLayerIndex];
            _stateMachine = _orbitalLayer.stateMachine; 
            Debug.Log("OrbitalReachLayer is added to Animator Controller.");

            EditorUtility.SetDirty(_animatorController);
            AssetDatabase.SaveAssets();
        }
        private void AddParameter(int paramNameIndex)
        {
            AnimatorControllerParameterType paramType = AnimatorControllerParameterType.Float;
            if (_paramTypes[paramNameIndex])
                paramType = AnimatorControllerParameterType.Bool;

            AnimatorControllerParameter parameter = new AnimatorControllerParameter
            {
                name = _paramaterNames[paramNameIndex],
                type = paramType
            };
            parameter.defaultFloat = 1f;
            _animatorController.AddParameter(parameter);
        }
        private void FillOrbitalReachLayer()
        {
            Vector3 layerOffset = new Vector3(250f, 125f, 0);
            AnimatorState defaultState = _stateMachine.AddState("Wait for Interaction", layerOffset);
            _stateMachine.defaultState = defaultState;

            AnimatorState standState = _stateMachine.AddState(OrbitalReach.StandStateName, layerOffset + new Vector3(275, 0, 0f));
            standState.speed = 1f;
            standState.speedParameterActive = true;
            standState.speedParameter = OrbitalReach.OSpeed;
            standState.mirrorParameterActive = true;
            standState.mirrorParameter = OrbitalReach.OMirror;
            standState.cycleOffsetParameterActive = true;
            standState.cycleOffsetParameter = OrbitalReach.OCycle;
            standState.iKOnFeet = true;

            AnimatorState crouchState = _stateMachine.AddState(OrbitalReach.CrouchStateName, layerOffset + new Vector3(275, 100, 0f));
            crouchState.speed = 1f;
            crouchState.speedParameterActive = true;
            crouchState.speedParameter = OrbitalReach.OSpeed;
            crouchState.mirrorParameterActive = true;
            crouchState.mirrorParameter = OrbitalReach.OMirror;
            crouchState.cycleOffsetParameterActive = true;
            crouchState.cycleOffsetParameter = OrbitalReach.OCycle;
            crouchState.iKOnFeet = true;

            float crouchWeight = -Mathf.InverseLerp(_script.midHeight, _script.minHeight, _script.crouchSetStart);

            AnimatorStateTransition transition = standState.AddTransition(defaultState);
            transition.hasExitTime = true;
            transition = crouchState.AddTransition(defaultState);
            transition.hasExitTime = true;
            transition = standState.AddTransition(crouchState);
            transition.hasExitTime = false;
            transition.AddCondition(AnimatorConditionMode.Less, crouchWeight, OrbitalReach.OHeight);

            BlendTree standBlendTree = new BlendTree();
            standBlendTree.blendType = BlendTreeType.FreeformCartesian2D;
            standBlendTree.blendParameter = OrbitalReach.OAngle;
            standBlendTree.blendParameterY = OrbitalReach.OHeight;
            standState.motion = standBlendTree;

            BlendTree crouchBlendTree = new BlendTree();
            crouchBlendTree.blendType = BlendTreeType.Simple1D;
            crouchBlendTree.useAutomaticThresholds = false;
            crouchBlendTree.blendParameter = OrbitalReach.OAngle;
            crouchState.motion = crouchBlendTree;

            float[] PosX = new float[] {-1f, -0.5f, 0f, 0.5f, 1f};
            float[] PosY = new float[] {1f, crouchWeight, 0f};
            for (int i = 0; i < _orbitalAnimClips.Length; i++)
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(_folderPath + _orbitalAnimClips[i] + _file);

                if (i < 5) crouchBlendTree.AddChild(clip, PosX[i % PosX.Length]);
                else standBlendTree.AddChild(clip, new Vector2(PosX[i % PosX.Length], PosY[(i/5)-1]));
            }

            AssetDatabase.AddObjectToAsset(standBlendTree, AssetDatabase.GetAssetPath(_stateMachine));
            AssetDatabase.AddObjectToAsset(crouchBlendTree, AssetDatabase.GetAssetPath(_stateMachine));
            EditorUtility.SetDirty(_animatorController);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        private bool CheckOrbitalAnimClips()
        {
            CreateOrbitalAnimArray();
            if (!Directory.Exists(_folderPath))
            {
                Debug.LogWarning("OrbitalAnimations folder is not exist on required path: " + _folderPath);
                return false;
            }
            bool success = true;
            for (int i = 0; i < _orbitalAnimClips.Length; i++)
            {
                if (!File.Exists(_folderPath + _orbitalAnimClips[i] + _file))
                {
                    Debug.LogWarning("Orbital Animation Clip is not exist at OrbitalAnimations folder: " + _orbitalAnimClips[i] + _file);
                    success = false;
                }
            }
            return success;
        }
        private void RemoveOrbitalReachLayer(bool button)
        {
            if (!Init()) return;

            if (CheckOrbitalReachLayer())
            {
                //Blendtree removal from its AnimationController before deleting the layer
                _orbitalLayer = _animatorController.layers[_orbitalLayerIndex];
                _stateMachine = _orbitalLayer.stateMachine;
                foreach (ChildAnimatorState state in _stateMachine.states)
                {
                    if (state.state.motion is BlendTree)
                    {
                        BlendTree blendTree = state.state.motion as BlendTree;
                        AssetDatabase.RemoveObjectFromAsset(blendTree);
                    }
                }

                _animatorController.RemoveLayer(_orbitalLayerIndex);
                _script.orbitalLayerIndex = -1;
                Debug.Log("OrbitalReachLayer is removed from Animator Controller.");
            }
            else if(button) Debug.Log("OrbitalReachLayer could not found.");

            CreateParameterArray();
            for (int i = 0; i < _paramaterNames.Length; i++)
            {
                if (CheckOrbitalParameters(_paramaterNames[i]))
                {
                    _animatorController.RemoveParameter(_paramIndex);
                }
            }
            EditorUtility.SetDirty(_animatorController);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void SetupDebug()
        {
            _script.debugOrbitalPositioner.debugOrbitalReach = _script;
            _script.debugOrbitalPositioner.debugPlayerTransform = _script.interactor.playerTransform;
            _script.debugOrbitalPositioner.debugTarget = _script.debugOrbitalPositioner.GetComponentInChildren<InteractorTarget>().transform;
            _script.debugOrbitalPositioner.debugDuration = _script.debugDuration;
        }
        private void UpdateDebug()
        {
            if (!_script.debugOrbitalPositioner.debugOrbitalReach) OnStart();

            _script.debugOrbitalValues = _script.debugOrbitalPositioner.DebugCalcOrbital();

            float orbitAngle = _script.debugOrbitalValues.y;
            float orbitalCycle = 0.5f;
            if (!_script.debugMirror)
            {
                orbitAngle = -orbitAngle;
                orbitalCycle = 0f;
            }
            _script.animator.SetFloat(OrbitalReach.OrbitalAngle, orbitAngle);
            _script.animator.SetFloat(OrbitalReach.OrbitalHeight, _script.debugOrbitalValues.x);
            _script.animator.SetFloat(OrbitalReach.OrbitalCycle, orbitalCycle);
            _script.animator.SetBool(OrbitalReach.OrbitalMirror, _script.debugMirror);
            _script.animator.SetLayerWeight(_script.orbitalLayerIndex, _script.debugOrbitalValues.z); //Disable this for OrbitalWeightCalculator.cs
            if (_script.debugOrbitalValues.x < _script.crouchWeightNormalized)
                _script.animator.CrossFade(OrbitalReach.CrouchState, 0f, _script.orbitalLayerIndex, _script.debugDuration);
            else _script.animator.CrossFade(OrbitalReach.StandState, 0f, _script.orbitalLayerIndex, _script.debugDuration);
            _script.animator.speed = 0f;
        }
    }
}
