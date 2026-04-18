using System;
using System.Collections;
using System.Collections.Generic;
using ARChess.Scripts.Chess;
using ARChess.Scripts.Project;
using ARChess.Scripts.Utility;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;

namespace ARChess.Scripts.UI
{
    
    /// <summary>
    /// Onboarding goal to be achieved as part of the <see cref="GoalManager"/>.
    /// </summary>
    public struct Goal
    {
        /// <summary>
        /// Goal state this goal represents.
        /// </summary>
        public TutorialSteps.OnboardingGoals CurrentGoal;

        /// <summary>
        /// This denotes whether a goal has been completed.
        /// </summary>
        public bool Completed;

        /// <summary>
        /// Creates a new Goal with the specified <see cref="GoalManager.OnboardingGoals"/>.
        /// </summary>
        /// <param name="goal">The <see cref="GoalManager.OnboardingGoals"/> state to assign to this Goal.</param>
        public Goal(TutorialSteps.OnboardingGoals goal)
        {
            CurrentGoal = goal;
            Completed = false;
        }
    }
    
    public class TutorialSteps : MonoBehaviour
    {
        
        /// <summary>
        /// State representation for the onboarding goals for the GoalManager.
        /// </summary>
        public enum OnboardingGoals
        {
            /// <summary>
            /// Current empty scene
            /// </summary>
            Empty,

            /// <summary>
            /// Find/scan for AR surfaces
            /// </summary>
            FindSurfaces,

            /// <summary>
            /// Tap a surface to spawn an object
            /// </summary>
            TapSurface,

            /// <summary>
            /// Show movement hints
            /// </summary>
            Hints,

            /// <summary>
            /// Show scale and rotate hints
            /// </summary>
            Scale
        }

        [Serializable]
        public class Step
        {
            public GameObject stepObject;
            public float waitForNextStepSeconds;
            public OnboardingGoals CurrentGoal;
            public bool includeSkipButton;
        }

        [Header("Tutorial Settings")] [Tooltip("Tutorial steps")] [SerializeField]
        List<Step> _stepsList = new List<Step>();
        public ProjectStateOptions projectStateOptions;
        
        [Header("Object Spawning")]
        [Tooltip("Object spawned")] [SerializeField]
        public PlaceObject m_ObjectSpawner;
        
        Queue<Goal> m_OnboardingGoals;
        Coroutine m_CurrentCoroutine;
        Goal m_CurrentGoal;
        bool m_AllGoalsFinished;
        int m_SurfacesTapped;
        int m_CurrentGoalIndex = 0;
        
        private int k_NumberOfSurfacesTappedToCompleteGoal;

        private ARPlaneManager _arPlaneManager;

        public List<Step> stepList
        {
            get => _stepsList;
            set => _stepsList = value;
        }

        private IEnumerator WaitForNextStep(float seconds)
        {
            yield return new WaitForSeconds(seconds);

            if (!Pointer.current.press.wasPressedThisFrame)
            {
                StopCoroutine(m_CurrentCoroutine);
            }
        }

        public void EnableCoaching(bool enable)
        {
            if (!projectStateOptions.tutorialPlayed && projectStateOptions.tutorialsEnabled && enable)
            {
                StartCoaching();
            }

            if (!enable && _stepsList.Count > 0)
            {
                _stepsList[0].stepObject.SetActive(false);
            }
        }

        void Start()
        {
            if (!_arPlaneManager)
            {
                _arPlaneManager = FindFirstObjectByType<ARPlaneManager>();
            }

            // Reset tutorials played on every attached script in the scene is loaded.
            if (projectStateOptions.tutorialsEnabled)
            {
                projectStateOptions.tutorialPlayed = false;
            }
        }

        // Update is called once per frame
        void Update()
        {
            // On FindSurfaces, Complete the Goal
            if (m_CurrentGoal.CurrentGoal == OnboardingGoals.FindSurfaces)
            {
                if (_arPlaneManager.trackables.count > 0)
                {
                    CompleteGoal();
                }
            }
            
            // Click on the screen and if the goal is other than find surfaces
            if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame && !m_AllGoalsFinished && (m_CurrentGoal.CurrentGoal == OnboardingGoals.Hints || m_CurrentGoal.CurrentGoal == OnboardingGoals.Scale))
            {
                if (m_CurrentCoroutine != null)
                {
                    StopCoroutine(m_CurrentCoroutine);
                }
                CompleteGoal();
            }
        }
        
        void CompleteGoal()
        {
            if (m_CurrentGoal.CurrentGoal == OnboardingGoals.TapSurface)
                m_ObjectSpawner.objectSpawned -= OnObjectSpawned;

            m_CurrentGoal.Completed = true;
            m_CurrentGoalIndex++;
            
            // If Queue is still available
            if (m_OnboardingGoals.Count > 0)
            {
                Animator animPrevious = _stepsList[m_CurrentGoalIndex - 1].stepObject.GetComponent<Animator>();
                Animator animNext = _stepsList[m_CurrentGoalIndex].stepObject.GetComponent<Animator>();
                animPrevious.SetTrigger("Hide");
                m_CurrentGoal = m_OnboardingGoals.Dequeue();
                _stepsList[m_CurrentGoalIndex - 1].stepObject.SetActive(false);
                _stepsList[m_CurrentGoalIndex].stepObject.SetActive(true);
                animNext.SetTrigger("Show");
            }
            else
            {
                Animator animPrevious = _stepsList[m_CurrentGoalIndex - 1].stepObject.GetComponent<Animator>();
                animPrevious.SetTrigger("Hide");
                _stepsList[m_CurrentGoalIndex - 1].stepObject.SetActive(false);
                m_AllGoalsFinished = true;
                projectStateOptions.tutorialPlayed = true;
                return;
            }

            PreprocessGoal();
        }
        
        void PreprocessGoal()
        {
            if (m_CurrentGoal.CurrentGoal == OnboardingGoals.FindSurfaces)
            {
                m_CurrentCoroutine = StartCoroutine(WaitForNextStep(_stepsList[m_CurrentGoalIndex].waitForNextStepSeconds));
            }
            else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.Hints)
            {
                m_CurrentCoroutine = StartCoroutine(WaitForNextStep(_stepsList[m_CurrentGoalIndex].waitForNextStepSeconds));
            }
            else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.Scale)
            {
                m_CurrentCoroutine = StartCoroutine(WaitForNextStep(_stepsList[m_CurrentGoalIndex].waitForNextStepSeconds));
            }
            else if (m_CurrentGoal.CurrentGoal == OnboardingGoals.TapSurface)
            {
                m_SurfacesTapped = 0;
                m_ObjectSpawner.objectSpawned += OnObjectSpawned;
            }
        }
        
        public void OnObjectSpawned(GameObject spawnedObject)
        {
            m_SurfacesTapped++;
            if (m_CurrentGoal.CurrentGoal == OnboardingGoals.TapSurface && m_SurfacesTapped >= k_NumberOfSurfacesTappedToCompleteGoal)
            {
                CompleteGoal();
            }
        }

        public void StartCoaching()
        {
            if (m_OnboardingGoals != null)
            {
                m_OnboardingGoals.Clear();
            }
            
            // More like a OrderedLists
            m_OnboardingGoals = new Queue<Goal>();

            // Check for all scan surfaces and assign to the int
            for (int i = 0; i < _stepsList.Count; i++)
            {
                if (_stepsList[i].CurrentGoal == OnboardingGoals.TapSurface)
                {
                    k_NumberOfSurfacesTappedToCompleteGoal+= 1;
                }
                
                Goal goal = new Goal(_stepsList[i].CurrentGoal);
                m_OnboardingGoals.Enqueue(goal);
            }
            
            // If Tap
            int startingStep = m_AllGoalsFinished ? 1 : 0;

            m_CurrentGoal = m_OnboardingGoals.Dequeue();
            m_AllGoalsFinished = false;
            m_CurrentGoalIndex = startingStep;

            for (int i = startingStep; i < _stepsList.Count; i++)
            {
                
                // get the animation clip and add the AnimationEvent
                Animator anim = _stepsList[i].stepObject.GetComponent<Animator>();
                
                if (i == startingStep)
                {
                    _stepsList[i].stepObject.SetActive(true);
                    anim.SetTrigger("Show");
                    PreprocessGoal();
                }
                else
                {
                    _stepsList[i].stepObject.SetActive(false);
                }
            }
        }
    }

}