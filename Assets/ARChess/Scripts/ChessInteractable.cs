using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace ARChess.Scripts
{
    public class ChessInteractable : XRGrabInteractable
    {
        [SerializeField]
        private Transform bitTransform;

        [SerializeField] 
        private float speed = 100.0f;

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic) // Called every frame. Corresponds with the MonoBehaviour.Update method.
            {
                if (isSelected)
                    RotateChess();   
            }
        }
    
        private void RotateChess()
        {
        
            if (firstInteractorSelecting is XRBaseInputInteractor interactor)
            {
                XRInputButtonReader activateState = interactor.activateInput;
                bitTransform.Rotate((Vector3.forward * speed * activateState.ReadValue()) * Time.deltaTime);
            }
        
        }
    }
}
