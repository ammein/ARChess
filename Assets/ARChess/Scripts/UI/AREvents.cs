using ARChess.Scripts.Project;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace ARChess.Scripts.UI
{
    public class AREvents : MonoBehaviour
    {
        [FormerlySerializedAs("_projectStateOptions")]
        [SerializeField]
        [Tooltip("Project State Options")]
        private ProjectStateOptions projectStateOptions;
        [SerializeField]
        [Tooltip("Tutorial Checkbox")]
        private Toggle tutorial;
        [SerializeField]
        [Tooltip("Tutorial Checkbox")]
        private Toggle dynamicLighting;

        void Start()
        {
            tutorial.isOn = projectStateOptions.tutorialsEnabled;
            dynamicLighting.isOn = projectStateOptions.dynamicLighting;
        }

        public void ToggleDynamicLighting(bool value)
        {
            projectStateOptions.dynamicLighting = value;
        }

        public void ToggleTutorial(bool value)
        {
            projectStateOptions.tutorialsEnabled = value;
        }
    }
}
