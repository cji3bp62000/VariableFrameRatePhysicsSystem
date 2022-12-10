using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VariableFrameRatePhysics
{
    public class VariableFrameRateSettings : ScriptableObject
    {
        [SerializeField] private VariableFrameRatePhysicsSystem.FixedDeltaTimeType fixedDeltaTimeType;

        public static VariableFrameRatePhysicsSystem.FixedDeltaTimeType GetFixedDeltaTimeType()
        {
            var fixedDeltaTimeType = VariableFrameRatePhysicsSystem.FixedDeltaTimeType.Fixed;

            var instance = Resources.Load<VariableFrameRateSettings>("VariableFrameRateSettings");
            if (instance != null) {
                fixedDeltaTimeType = instance.fixedDeltaTimeType;
#if UNITY_EDITOR
                // Editor の場合、エディターに Unload のタイミングを任せる (セーブも兼ねて)
#else
            Resources.UnloadAsset(instance);
#endif
            }

            return fixedDeltaTimeType;
        }

        public static float GetDefaultFixedDeltaTimeStep()
        {
#if UNITY_EDITOR
            var timeManager = AssetDatabase.LoadMainAssetAtPath("ProjectSettings/TimeManager.asset");
            var timeManagerSo = new SerializedObject(timeManager);
            var fixedTimestep = timeManagerSo.FindProperty("Fixed Timestep").floatValue;
            timeManagerSo.Dispose();
            return fixedTimestep;
#else
        return Time.fixedDeltaTime;
#endif
        }
    }
}

#if UNITY_EDITOR
namespace VariableFrameRatePhysics
{
    using System.Text;

    [CustomEditor(typeof(VariableFrameRateSettings))]
    public class VariableFrameRateSettingsEditor : Editor
    {
        private string description =
            "それぞれの説明：\n" +
            "\n" +
            "Fixed:\n" +
            "  fixedDeltaTime 固定値; 既存方法。\n" +
            "  1 フレームで 0~ 回 FixedUpdate, 物理演算が安定している。\n" +
            "\n" +
            "Variable:\n" +
            "  fixedDeltaTime = deltaTime。\n" +
            "  1 フレームで必ず 1 回 FixedUpdate。\n" +
            "  ゲームの他部分と協調性が良い。\n" +
            "\n" +
            "VariableWithSubStep:\n" +
            "  deltaTime を元々の fixedDeltaTime の値で分割して、\n" +
            "  fixedDeltaTime に設定。（例：0.033 = 0.02 + 0.013）\n" +
            "  1フレームで 1~ 回 FixedUpdate。\n" +
            "  Fixed と Variable の折衷 (安定 & 協調性)\n";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space(10);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextArea(description);
            EditorGUI.EndDisabledGroup();
        }
    }
}
#endif
