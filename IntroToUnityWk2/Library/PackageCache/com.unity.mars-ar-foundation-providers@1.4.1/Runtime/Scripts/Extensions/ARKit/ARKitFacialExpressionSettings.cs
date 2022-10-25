using Unity.MARS.Settings;
using Unity.XRTools.Utils;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.MARS.Data.ARFoundation
{
    [ScriptableSettingsPath(MARSCore.SettingsFolder)]
    [MovedFrom("Unity.MARS.Providers")]
    public class ARKitFacialExpressionSettings : ScriptableSettings<ARKitFacialExpressionSettings>
    {
        static class Defaults
        {
            public const float EventCooldownInSeconds = 0.15f;
            public const float ExpressionChangeSmoothingFilter = 0.9f;

            public static readonly float[] Thresholds =
            {
                0.4f,   // LeftEyeClose
                0.4f,   // RightEyeClose
                0.65f,  // LeftEyebrowRaise
                0.65f,  // RightEyebrowRaise
                0.2f,   // BothEyebrowsRaise
                0.2f,   // LeftLipCornerRaise
                0.6f,   // RightLipCornerRaise
                0.45f,  // Smile
                0.45f   // MouthOpen
            };
        }

        [SerializeField]
        float[] m_Thresholds = Defaults.Thresholds;

        [SerializeField]
        float m_EventCooldownInSeconds = Defaults.EventCooldownInSeconds;

        [SerializeField]
        float m_ExpressionChangeSmoothingFilter = Defaults.ExpressionChangeSmoothingFilter;

        public float[] thresholds
        {
            get { return m_Thresholds; }
            set { m_Thresholds = value; }
        }

        public float eventCooldownInSeconds
        {
            get => m_EventCooldownInSeconds;
            set => m_EventCooldownInSeconds = value;
        }

        public float expressionChangeSmoothingFilter
        {
            get => m_ExpressionChangeSmoothingFilter;
            set => m_ExpressionChangeSmoothingFilter = value;
        }

        public ARKitFacialExpressionSettings()
        {
            var length = EnumValues<MRFaceExpression>.Values.Length;
            if (m_Thresholds == null)
                m_Thresholds = new float[length];
        }
    }
}
