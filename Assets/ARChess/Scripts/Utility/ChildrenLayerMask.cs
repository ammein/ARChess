using System.Collections.Generic;
using UnityEngine;

namespace ARChess.Scripts.Utility
{
    public static class ChildrenLayerMask
    {
        public static void All(GameObject parentObject, string layerName)
        {
            // Convert the string name to the integer layer index
            int targetLayer = LayerMask.NameToLayer(layerName); 

            // Get all transforms in the hierarchy, including inactive ones
            Transform[] allChildren = parentObject.GetComponentsInChildren<Transform>(true);

            foreach (Transform child in allChildren)
            {
                child.gameObject.layer = targetLayer;
            }
        }

        public static void Chess(GameObject parentObject, string targetLayerName, string layerName)
        {
            int targetLayer = LayerMask.NameToLayer(layerName);
            
            Transform[] allChildren = parentObject.GetComponentsInChildren<Transform>(true);
            
            foreach (Transform child in allChildren)
            {
                if (child.gameObject.layer == LayerMask.NameToLayer(targetLayerName))
                {
                    child.gameObject.layer = targetLayer;
                }
            }
        }
    }
}
