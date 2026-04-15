using System;
using System.Collections;
using ARChess.Scripts.Utility;
using UnityEngine;
using UnityEngine.Rendering;

namespace ARChess.Scripts.Chess.Pieces
{
    public enum ChessPieceType
    {
        None = 0,
        Pawn = 1,
        Rook = 2,
        Knight = 3,
        Bishop = 4,
        Queen = 5,
        King = 6,
    }
    
    public enum Appearance
    {
        Dissappear = 0,
        Appear = 1
    }

    public class ChessPiece : MonoBehaviour
    {
        
        [Header("Chess Piece")]
        [Tooltip("The index of team piece")]
        public int team;
        [Tooltip("The current X position")]
        public int currentX;
        [Tooltip("The current Y position")]
        public int currentY;
        [Tooltip("The type of chess piece")]
        public ChessPieceType type;
        
        [Header("Material Piece Shader Animation")]
        [Tooltip("Wait duration before animate")]
        public float waitDuration;
        [Tooltip("Duration of material shader for '_Appear' reference")]
        public float duration;
    
        private Vector3 _desiredPosition;
        private Vector3 _desiredScale;
        private Renderer _renderer;
        private Coroutine _appearCoroutine;
        private Appearance _appear = Appearance.Dissappear;

        public Appearance Appearance
        {
            get => _appear;
            set => _appear = value;
        }

        public void Start()
        {
            _renderer = GetComponent<Renderer>();
        }

        public void AppearPiece(string propertyName)
        {
            if(_appearCoroutine != null)
                StopCoroutine(_appearCoroutine);

            _appear = Appearance.Appear;
            _appearCoroutine = StartCoroutine(AppearPieceAnimation(propertyName));
        }

        private IEnumerator AppearPieceAnimation(string propertyName)
        {
            yield return new WaitForEndOfFrame();
            // Set enum as a float value instead of keyword
            SetKeyWord("_APPEARANCE_STATE", Appearance.Appear);
            CheckShaderKeywordState();
            yield return new WaitForSeconds(waitDuration);
            float time = 0;
            while (time < duration)
            {
                float lerpValue = Mathf.Lerp(0f, 1f, time / duration);
                _renderer.material.SetFloat(propertyName, Mathf.Clamp(lerpValue, 0f ,1f));
                time += Time.deltaTime;
                yield return null;
            }
            
            _renderer.material.SetFloat(propertyName, 1f);
        }
        
        private void SetKeyWord<T>(string parameterBaseName, T selectedKeyword) where T: Enum
        {
            var shader = _renderer.material.shader;
            var keywordSpace = shader.keywordSpace;
            var keywords = Enum.GetValues(typeof(T));

            // Step 1: Disable ALL keywords first
            foreach (LocalKeyword keyword in keywordSpace.keywords)
            {
                if (keyword.name.StartsWith(parameterBaseName))
                {
                    _renderer.material.SetKeyword(keyword, false);
                }
            }

            // Step 2: Enable ONLY the selected one
            foreach (T suffix in keywords)
            {
                if (suffix.Equals(selectedKeyword))
                {
                    var keywordName = $"{parameterBaseName}_{suffix.ToString().ToUpperInvariant()}";
                    var localKeyword = new LocalKeyword(shader, keywordName);
            
                    _renderer.material.SetKeyword(localKeyword, true);
                }
            }
        }

        
        void CheckShaderKeywordState()
        {
            // Get the instance of the Shader class that the material uses
            var shader = _renderer.material.shader;

            // Get all the local keywords that affect the Shader
            var keywordSpace = shader.keywordSpace;

            // Iterate over the local keywords
            foreach (var localKeyword in keywordSpace.keywords)
            {
                // If the local keyword is overridable (i.e., it was declared with a global scope),
                // and a global keyword with the same name exists and is enabled,
                // then Unity uses the global keyword state
                if (localKeyword.isOverridable && Shader.IsKeywordEnabled(localKeyword.name))
                {
                    Log.LogThis("Local keyword with name of " + localKeyword.name + " is overridden by a global keyword, and is enabled", this);
                }
                // Otherwise, Unity uses the local keyword state
                else
                {
                    var state = _renderer.material.IsKeywordEnabled(localKeyword) ? "enabled" : "disabled";
                    Log.LogThis("Local keyword with name of " + localKeyword.name + " is " + state, this);
                }            
            }
        }
    }
}
