using UnityEngine;
using UnityEditor;

namespace razz
{
    [CustomEditor(typeof(OrbitalPositioner))]
    public class OrbitalPositionerEditor : Editor
    {
        OrbitalPositioner orbitalPositioner;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            orbitalPositioner = (OrbitalPositioner)target;
            GUILayout.Space(10);

            GUILayout.Label("Quick Settings", EditorStyles.boldLabel);
            GUILayout.Label("Distances");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("120"))
            {
                orbitalPositioner.distance = 1.2f;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("100"))
            {
                orbitalPositioner.distance = 1f;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("085"))
            {
                orbitalPositioner.distance = 0.85f;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("080"))
            {
                orbitalPositioner.distance = 0.8f;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("050"))
            {
                orbitalPositioner.distance = 0.5f;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("020"))
            {
                orbitalPositioner.distance = 0.2f;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("001"))
            {
                orbitalPositioner.distance = 0.01f;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Heights");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("200"))
            {
                float oldAngle = orbitalPositioner.angle;
                orbitalPositioner.height = 2f;
                orbitalPositioner.angle = oldAngle;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("180"))
            {
                float oldAngle = orbitalPositioner.angle;
                orbitalPositioner.height = 1.8f;
                orbitalPositioner.angle = oldAngle;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("150"))
            {
                float oldAngle = orbitalPositioner.angle;
                orbitalPositioner.height = 1.5f;
                orbitalPositioner.angle = oldAngle;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("121"))
            {
                float oldAngle = orbitalPositioner.angle;
                orbitalPositioner.height = 1.21f;
                orbitalPositioner.angle = oldAngle;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("119"))
            {
                float oldAngle = orbitalPositioner.angle;
                orbitalPositioner.height = 1.19f;
                orbitalPositioner.angle = oldAngle;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("071"))
            {
                float oldAngle = orbitalPositioner.angle;
                orbitalPositioner.height = 0.71f;
                orbitalPositioner.angle = oldAngle;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("065"))
            {
                float oldAngle = orbitalPositioner.angle;
                orbitalPositioner.height = 0.65f;
                orbitalPositioner.angle = oldAngle;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("025"))
            {
                float oldAngle = orbitalPositioner.angle;
                orbitalPositioner.height = 0.25f;
                orbitalPositioner.angle = oldAngle;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("000"))
            {
                float oldAngle = orbitalPositioner.angle;
                orbitalPositioner.height = 0f;
                orbitalPositioner.angle = oldAngle;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Right Angles");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("15"))
            {
                orbitalPositioner.angle = 15;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("30"))
            {
                orbitalPositioner.angle = 30;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("45"))
            {
                orbitalPositioner.angle = 45;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("60"))
            {
                orbitalPositioner.angle = 60;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("75"))
            {
                orbitalPositioner.angle = 75;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("90"))
            {
                orbitalPositioner.angle = 90;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("105"))
            {
                orbitalPositioner.angle = 105;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("120"))
            {
                orbitalPositioner.angle = 120;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("135"))
            {
                orbitalPositioner.angle = 135;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("150"))
            {
                orbitalPositioner.angle = 150;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("165"))
            {
                orbitalPositioner.angle = 165;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("179"))
            {
                orbitalPositioner.angle = 179;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            GUILayout.EndHorizontal();

            GUILayout.Label("Left Angles");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("15"))
            {
                orbitalPositioner.angle = 345;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("30"))
            {
                orbitalPositioner.angle = 330;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("45"))
            {
                orbitalPositioner.angle = 315;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("60"))
            {
                orbitalPositioner.angle = 300;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("75"))
            {
                orbitalPositioner.angle = 285;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("90"))
            {
                orbitalPositioner.angle = 270;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("105"))
            {
                orbitalPositioner.angle = 255;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("120"))
            {
                orbitalPositioner.angle = 240;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("135"))
            {
                orbitalPositioner.angle = 225;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("150"))
            {
                orbitalPositioner.angle = 210;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("165"))
            {
                orbitalPositioner.angle = 195;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            if (GUILayout.Button("180"))
            {
                orbitalPositioner.angle = 180;
                orbitalPositioner.SetNewOrbitalPosition();
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Set Orbital Position"))
            {
                orbitalPositioner.SetNewOrbitalPosition();
            }

            if (GUI.changed)
            {
                orbitalPositioner.SetNewOrbitalPosition();
                if (orbitalPositioner.debugOrbitalReach)
                    orbitalPositioner.debugOrbitalReach.debugDuration = orbitalPositioner.debugDuration;
            }
        }
    }
}
