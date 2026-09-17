using UnityEngine;

namespace ImmersiveFrameworkQA.Camera
{
    /// <summary>
    /// Focused CAMERA-028-C probe that treats an authored Camera.rect as externally
    /// owned state and reports any mutation while Framework Camera runtime is active.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class QaCamera028CPhysicalPresentationFixture : MonoBehaviour
    {
        private const float RectTolerance = 0.000001f;

        [SerializeField] private UnityEngine.Camera targetCamera;
        [SerializeField] private Rect expectedRect;

        public static bool IsMonitoring { get; private set; }
        public static bool HasViolation { get; private set; }
        public static int SampleCount { get; private set; }
        public static string Diagnostic { get; private set; } = string.Empty;
        public static Rect ExpectedRect { get; private set; }
        public static Rect LastObservedRect { get; private set; }

        public static bool CurrentRectMatchesExpected =>
            IsMonitoring &&
            !HasViolation &&
            RectMatches(LastObservedRect, ExpectedRect);

        public void Configure(UnityEngine.Camera camera, Rect rect)
        {
            targetCamera = camera;
            expectedRect = rect;
        }

        private void Awake()
        {
            ResetEvidence();
            ExpectedRect = expectedRect;
            IsMonitoring = true;
            Sample("Awake");
        }

        private void OnEnable()
        {
            Sample("OnEnable");
        }

        private void Update()
        {
            Sample("Update");
        }

        private void LateUpdate()
        {
            Sample("LateUpdate");
        }

        private void OnPreCull()
        {
            Sample("OnPreCull");
        }

        private void Sample(string stage)
        {
            if (!IsMonitoring || HasViolation)
            {
                return;
            }

            if (targetCamera == null)
            {
                Fail(stage, "CAMERA-028-C probe lost its explicitly authored Unity Camera.");
                return;
            }

            LastObservedRect = targetCamera.rect;
            SampleCount++;
            if (!RectMatches(LastObservedRect, ExpectedRect))
            {
                Fail(
                    stage,
                    "Externally authored Camera.rect was mutated. " +
                    $"expected='{Describe(ExpectedRect)}' actual='{Describe(LastObservedRect)}'.");
            }
        }

        private static void Fail(string stage, string reason)
        {
            if (HasViolation)
            {
                return;
            }

            HasViolation = true;
            Diagnostic =
                $"stage='{stage}' sample='{SampleCount}' {reason}";
            Debug.LogError(
                $"[CAMERA-028-C] status='Violation' {Diagnostic}");
        }

        private static void ResetEvidence()
        {
            IsMonitoring = false;
            HasViolation = false;
            SampleCount = 0;
            Diagnostic = string.Empty;
            ExpectedRect = default;
            LastObservedRect = default;
        }

        private static bool RectMatches(Rect left, Rect right) =>
            Mathf.Abs(left.x - right.x) <= RectTolerance &&
            Mathf.Abs(left.y - right.y) <= RectTolerance &&
            Mathf.Abs(left.width - right.width) <= RectTolerance &&
            Mathf.Abs(left.height - right.height) <= RectTolerance;

        private static string Describe(Rect value) =>
            $"x={value.x:F6},y={value.y:F6},w={value.width:F6},h={value.height:F6}";
    }
}
