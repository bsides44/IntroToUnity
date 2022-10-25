using System;

namespace UnityEditor.MARS.Build
{
    class BuildValidationRule
    {
        /// <summary>
        /// Name of the rule that will be shown to the developer in the build validation drawer.
        /// </summary>
        public string name;

        /// <summary>
        /// Message describing the rule that will be showed to the developer if it fails.
        /// </summary>
        public string message;

        /// <summary>
        /// Lambda function that returns true if validation passes, false if validation fails.
        /// </summary>
        public Func<bool> checkPredicate;

        /// <summary>
        /// Lambda function that fixes the issue, if possible.
        /// </summary>
        public Action fixIt;

        /// <summary>
        /// Text describing how the issue is fixed, shown in a tooltip.
        /// </summary>
        public string fixItMessage;

        /// <summary>
        /// True if the fixIt Lambda function performs a function that is automatic and does not require user input.
        /// If your fixIt function requires user input, set fixitAutomatic to false to prevent the fixIt method from
        /// being executed during fixAll.
        /// </summary>
        public bool fixItAutomatic = true;

        /// <summary>
        /// If true, failing the rule is treated as an error and stops the build.
        /// If false, failing the rule is treated as a warning and it doesn't stop the build. The developer has the
        /// option to correct the problem, but is not required to.
        /// </summary>
        public bool error;

        /// <summary>
        /// Optional text to display in a help icon with the issue in the validator.
        /// </summary>
        public string helpText;

        /// <summary>
        /// Optional link that will be opened if the help icon is clicked.
        /// </summary>
        public string helpLink;

        /// <summary>
        /// If true, this build rule will only run on the currently loaded editor scenes when not in prefab isolation.
        /// If false, this build rule will always run.
        /// </summary>
        public bool sceneOnlyValidation;
    }
}
