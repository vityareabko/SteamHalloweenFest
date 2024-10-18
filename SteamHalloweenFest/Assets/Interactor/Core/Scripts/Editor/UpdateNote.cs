using UnityEngine;
using UnityEditor;
using System;

namespace razz
{
    public class UpdateNote : EditorWindow
    {
        private static UpdateNote _instance;
        private bool _init;
        private float _bigLogoX;
        private float _bigLogoY = 20f;
        private static Vector2 _size = new Vector2(600, 480);
        private float _padding = 10f;

        private static readonly string PlayCountReg = "Interactor_PlayCount_" + Interactor.Version.ToString();
        private static readonly string ImportDateReg = "Interactor_ImportDate_" + Interactor.Version.ToString();
        private static readonly string ReceivedReg = "Interactor_Received_" + Interactor.Version.ToString();
        private static readonly int ReceiveCount = 1;
        private static int _receiveCount;

        private GUISkin _skin;
        private GUIStyle _background;
        private GUIStyle _label;
        private GUIStyle _text;
        private GUIStyle _text2;
        private GUIStyle _textHeader;
        private GUIStyle _textSmall;
        private GUIStyle _buttonStyle;
        private GUIStyle _linkStyle;
        private GUIStyle _linkStyle2;
        private Texture2D _logoBig;
        private Texture2D _logoSmall;
        private GUIContent _buttonContent;

        private void OnEnable()
        {
            GetStyles();
        }
        private void OnGUI()
        {
            if (!_init) GetStyles();
            if (!_instance) GetWindow<UpdateNote>(true, "Interactor v" + Interactor.Version + " Update Note", false);

            Rect windowRect = new Rect(0, 0, _size.x, _size.y);
            GUI.Box(windowRect, "", _background);

            Rect logo = new Rect(_bigLogoX, _bigLogoY, _logoBig.width, _logoBig.height);
            GUI.DrawTexture(logo, _logoBig);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.BeginArea(new Rect(_padding, 110f, _size.x - 2f * _padding, 300f));
            EditorGUILayout.LabelField("v0.98: New Pick Up System & Inventories", _textHeader);
            GUILayout.Space(6f);

            EditorGUILayout.LabelField("One-handed pickables have been overhauled and are now more powerful.", _text);
            EditorGUILayout.LabelField("Explore options with the new inventory system. Refer to the example scene.", _text);
            EditorGUILayout.LabelField("As a final note, Full Body IK will be included by default in version 0.99!", _text);
            GUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
#if UNITY_2019_3_OR_NEWER
            _linkStyle2.normal.textColor = new Color(0.16f, 0.76f, 0.87f);
#else
            Rect changelog = new Rect(230f, 103f, 115f, 20f);
            if (changelog.Contains(Event.current.mousePosition))
                _linkStyle2.normal.textColor = Color.white;
            else _linkStyle2.normal.textColor = new Color(0.16f, 0.76f, 0.87f);
#endif
            if (GUILayout.Button("Full Changelog", _linkStyle2))
            {
                Links.Changelog();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(20f);

            EditorGUILayout.LabelField("Your reviews play a crucial role in allowing me to allocate more time to", _text2);
            EditorGUILayout.LabelField(" Interactor instead of taking on additional freelance projects to support myself.", _text2);
            EditorGUILayout.LabelField(" Every single user counts, including you. Thank you.", _text2);
            GUILayout.Space(2f);

            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();

            GUILayout.FlexibleSpace();
            GUILayout.BeginArea(new Rect(_padding * 2f, 338f, _size.x - _padding * 4f, 200f));
            if (GUILayout.Button(_buttonContent, _buttonStyle))
            {
                Links.Store(true);
                Close();
            }
            GUILayout.Space(5f);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Also, if you have issues or feedbacks about Interactor: ", _text);
#if UNITY_2019_3_OR_NEWER
            _linkStyle.normal.textColor = new Color(1f, 0.7f, 0.7f);
#else
            Rect support = new Rect(445f, 62f, 100f, 20f);
            if (support.Contains(Event.current.mousePosition))
                _linkStyle.normal.textColor = Color.white;
            else _linkStyle.normal.textColor = new Color(1f, 0.7f, 0.7f);
#endif
            if (GUILayout.Button("Support Page", _linkStyle))
            {
                Links.Support();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(22f);

            if (_receiveCount == ReceiveCount)
                EditorGUILayout.LabelField("This note will not show up again for this version.", _textSmall);
            else EditorGUILayout.LabelField("This note will show up only twice in total, now and a while later, after you use Interactor.", _textSmall);
            GUILayout.EndArea();

            if (windowRect.Contains(Event.current.mousePosition))
                this.Repaint();
        }
        private void GetStyles()
        {
            _skin = Resources.Load<GUISkin>("InteractorGUISkin");
            _logoBig = Resources.Load<Texture2D>("Images/TopLogoResized");
            _logoSmall = Resources.Load<Texture2D>("Images/TopLogoSmall");
            _background = _skin.GetStyle("BackgroundStyle");
            _label = _skin.GetStyle("Label");

            _buttonStyle = new GUIStyle(_skin.GetStyle("Button"));
            _buttonStyle.fixedWidth = _size.x - 4f * _padding;
            _buttonStyle.border.left = 2;
            _buttonStyle.border.right = 2;

            _text = new GUIStyle(_label);
            _text.normal.textColor = Color.white;
            _text.padding = new RectOffset(0, 0, 0, 0);
            _text.margin = new RectOffset(8, 8, 8, 8);
            _text.alignment = TextAnchor.MiddleCenter;
            _text.fontSize = 15;
            _text.wordWrap = true;
            _text.richText = true;

            _text2 = new GUIStyle(_label);
            _text2.normal.textColor = Color.white;
            _text2.padding = new RectOffset(0, 0, 0, 0);
            _text2.margin = new RectOffset(8, 8, 8, 8);
            _text2.alignment = TextAnchor.MiddleCenter;
            _text2.fontSize = 13;
            _text2.wordWrap = true;
            _text2.richText = true;

            _textHeader = new GUIStyle(_text);
            _textHeader.fontSize = 18;
            _textHeader.normal.textColor = new Color(0.8f, 1f, 1f);

            _textSmall = new GUIStyle(_text);
            _textSmall.margin = new RectOffset(0, 0, 0, 0);
            _textSmall.fontSize = 12;

            _linkStyle = new GUIStyle(_text);
            _linkStyle.normal.textColor = new Color(1f, 0.7f, 0.7f);
            _linkStyle.onNormal.textColor = Color.white;
            _linkStyle.active.textColor = Color.white;
            _linkStyle.onActive.textColor = Color.white;
            _linkStyle.hover.textColor = Color.white;
            _linkStyle.onHover.textColor = Color.white;
            _linkStyle.border = new RectOffset(10, 10, 10, 10);
            _linkStyle.overflow = new RectOffset(10, 10, 10, 10);
            _linkStyle.imagePosition = ImagePosition.TextOnly;

            _linkStyle2 = new GUIStyle(_text);
            _linkStyle2.normal.textColor = new Color(0.47f, 0.68f, 0.97f);
            _linkStyle2.onNormal.textColor = Color.white;
            _linkStyle2.active.textColor = Color.white;
            _linkStyle2.onActive.textColor = Color.white;
            _linkStyle2.hover.textColor = Color.white;
            _linkStyle2.onHover.textColor = Color.white;
            _linkStyle2.border = new RectOffset(10, 10, 10, 10);
            _linkStyle2.overflow = new RectOffset(10, 10, 10, 10);
            _linkStyle2.imagePosition = ImagePosition.TextOnly;

            _buttonContent = new GUIContent("on <b>Unity Asset Store</b>", _logoSmall);
            _buttonContent.tooltip = "Clicking will open Interactor Asset Store page and will close this note.";

            _bigLogoX = (_size.x - _logoBig.width) * 0.5f;
            _init = true;
        }

        [InitializeOnLoadMethod]
        private static void OnInit()
        {
            _receiveCount = EditorPrefs.GetInt(ReceivedReg, 0);
            if (_receiveCount == 0) OpenNote();
            else if (_receiveCount >= ReceiveCount) return;

            EditorApplication.playModeStateChanged += CheckNoteState;
        }

        private static void CheckNoteState(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode) return;

            int plays = EditorPrefs.GetInt(PlayCountReg, 0);
            EditorPrefs.SetInt(PlayCountReg, ++plays);

            string checkDate = EditorPrefs.GetString(ImportDateReg, string.Empty);
            if (string.IsNullOrEmpty(checkDate))
            {
                checkDate = DateTime.Now.ToString("g");
                EditorPrefs.SetString(ImportDateReg, checkDate);
            }

            DateTime importDate = DateTime.Parse(checkDate);
            double hours = (DateTime.Now - importDate).TotalHours;

            if (hours > 120 && plays > 50) OpenNote();
        }

        [MenuItem("Window/Interactor/Update Note")]
        public static void OpenNoteMenu()
        {
            _instance = GetWindow<UpdateNote>(true, "Interactor v" + Interactor.Version + " Update Note", false);

            _instance.minSize = _size;
            _instance.maxSize = _size;
            _instance.Show();
        }

        public static void OpenNote()
        {
            _instance = GetWindow<UpdateNote>(true, "Interactor v" + Interactor.Version + " Update Note", false);

            _instance.minSize = _size;
            _instance.maxSize = _size;
            _instance.Show();

            _receiveCount = EditorPrefs.GetInt(ReceivedReg, 0);
            EditorPrefs.SetInt(ReceivedReg, ++_receiveCount);
        }
    }
}
