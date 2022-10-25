using Unity.MARS.Settings;
using Unity.XRTools.Utils;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.MARS.Data.ARFoundation
{
    [ScriptableSettingsPath(MARSCore.SettingsFolder)]
    [MovedFrom("Unity.MARS.Providers")]
    public class ARCoreFacialExpressionSettings : ScriptableSettings<ARCoreFacialExpressionSettings>
    {
        static class Defaults
        {
            public const float EventCooldownInSeconds = 0.15f;
            public const float ExpressionChangeSmoothingFilter = 0.9f;
            public const float MaxHeadAngleX = 34f;
            public const float MaxHeadAngleY = 22f;
            public const float MaxHeadAngleZ = 45f;

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

            public static readonly float[] ExpressionDistanceMinimums =
            {
                0.0056f,    // LeftEyeClose
                0.0056f,    // RightEyeClose
                0.023f,     // LeftEyebrowRaise
                0.022f,     // RightEyebrowRaise
                0.022f,     // BothEyebrowsRaise
                0.025f,     // LeftLipCornerRaise
                0.025f,     // RightLipCornerRaise
                0f,         // Smile
                0.02f       // MouthOpen
            };

            public static readonly float[] ExpressionDistanceMaximums =
            {
                0.001f,     // LeftEyeClose
                0.001f,     // RightEyeClose
                0.028f,     // LeftEyebrowRaise
                0.027f,     // RightEyebrowRaise
                0.027f,     // BothEyebrowsRaise
                0.036f,     // LeftLipCornerRaise
                0.036f,     // RightLipCornerRaise
                0f,         // Smile
                0.045f      // MouthOpen
            };

            public static readonly bool[] ExpressionDistanceReverseStates =
            {
                true,   // LeftEyeClose
                true,   // RightEyeClose
                false,  // LeftEyebrowRaise
                false,  // RightEyebrowRaise
                false,  // BothEyebrowsRaise
                false,  // LeftLipCornerRaise
                false,  // RightLipCornerRaise
                false,  // Smile
                false   // MouthOpen
            };
        }

        const float k_MaxHeadAngleXMin = 15f;
        const float k_MaxHeadAngleXMax = 40f;
        const float k_MaxHeadAngleYMin = 12f;
        const float k_MaxHeadAngleYMax = 25f;
        const float k_MaxHeadAngleZMin = 30f;
        const float k_MaxHeadAngleZMax = 60f;

        [SerializeField]
        float[] m_Thresholds = Defaults.Thresholds;

        [SerializeField]
        float[] m_ExpressionDistanceMinimums = Defaults.ExpressionDistanceMinimums;

        [SerializeField]
        float[] m_ExpressionDistanceMaximums = Defaults.ExpressionDistanceMaximums;

        [SerializeField]
        bool[] m_ExpressionDistanceReverseStates = Defaults.ExpressionDistanceReverseStates;

        [SerializeField]
        float m_EventCooldownInSeconds = Defaults.EventCooldownInSeconds;

        [SerializeField]
        float m_ExpressionChangeSmoothingFilter = Defaults.ExpressionChangeSmoothingFilter;

        [SerializeField]
        [Range(k_MaxHeadAngleXMin, k_MaxHeadAngleXMax)]
        float m_MaxHeadAngleX = Defaults.MaxHeadAngleX;

        [SerializeField]
        [Range(k_MaxHeadAngleYMin, k_MaxHeadAngleYMax)]
        float m_MaxHeadAngleY = Defaults.MaxHeadAngleY;

        [SerializeField]
        [Range(k_MaxHeadAngleZMin, k_MaxHeadAngleZMax)]
        float m_MaxHeadAngleZ = Defaults.MaxHeadAngleZ;

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

        public float[] expressionDistanceMinimums
        {
            get { return m_ExpressionDistanceMinimums; }
            set { m_ExpressionDistanceMinimums = value; }
        }

        public float[] expressionDistanceMaximums
        {
            get { return m_ExpressionDistanceMaximums; }
            set { m_ExpressionDistanceMaximums = value; }
        }

        public float maxHeadAngleX
        {
            get { return m_MaxHeadAngleX; }
            set { m_MaxHeadAngleX = Mathf.Clamp(value, k_MaxHeadAngleXMin, k_MaxHeadAngleXMax); }
        }

        public float maxHeadAngleY
        {
            get { return m_MaxHeadAngleY; }
            set { m_MaxHeadAngleY = Mathf.Clamp(value, k_MaxHeadAngleYMin, k_MaxHeadAngleYMax); }
        }

        public float maxHeadAngleZ
        {
            get { return m_MaxHeadAngleZ; }
            set { m_MaxHeadAngleZ = Mathf.Clamp(value, k_MaxHeadAngleZMin, k_MaxHeadAngleZMax); }
        }

        public ARCoreFacialExpressionSettings()
        {
            var length = EnumValues<MRFaceExpression>.Values.Length;

            if (m_Thresholds == null)
                m_Thresholds = new float[length];

            if (m_ExpressionDistanceMinimums == null)
                m_ExpressionDistanceMinimums = new float[length];

            if (m_ExpressionDistanceMaximums == null)
                m_ExpressionDistanceMaximums = new float[length];

            if (m_ExpressionDistanceReverseStates == null)
                m_ExpressionDistanceReverseStates = new bool[length];
        }
    }
}
