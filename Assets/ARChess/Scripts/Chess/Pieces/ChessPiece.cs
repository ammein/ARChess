using System;
using System.Collections;
using System.Collections.Generic;
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
        Disappear = 0,
        Appear = 1,
        Destroyed = 2
    }

    public class ChessPiece : MonoBehaviour
    {

        [Serializable]
        public class AppearanceState
        {
            public Appearance appearance;
            public float duration;
        }
        
        [Header("Chess Piece")]
        [Tooltip("The index of team piece")]
        public int team;
        [Tooltip("The current X position")]
        public int currentX;
        [Tooltip("The current Y position")]
        public int currentY;
        [Tooltip("The type of chess piece")]
        public ChessPieceType type;
        [HideInInspector]
        public List<AppearanceState> appearance = new List<AppearanceState>();

        [Header("Lerp Animation")]
        [Tooltip("Lerp duration to animate moving piece")]
        public float movingDuration;
        [Tooltip("Lerp duration to animate scale piece")]
        public float movingScale;
    
        private Vector3 _desiredPosition;
        private Vector3 _desiredScale = Vector3.one;
        private Renderer _renderer;
        private Coroutine _appearCoroutine;
        private Coroutine _disappearCoroutine;
        private Coroutine _destroyedCoroutine;
        private Appearance _appear = Appearance.Disappear;

        public Appearance GetAppearance
        {
            get => _appear;
            set => _appear = value;
        }

        public void Start()
        {
            _renderer = GetComponent<Renderer>();
            
            // When a piece is instantiated, always set material to 'Appear' so that the transparent material shader is initialize...
            SetKeyWord("_APPEARANCE_STATE", Appearance.Appear);
        }

        private void Update()
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, _desiredPosition, Time.deltaTime * movingDuration);
            transform.localScale = Vector3.Lerp(transform.localScale, _desiredScale, Time.deltaTime * movingScale);
        }
        
        // Operations
        public virtual void SetPosition(Vector3 position, bool force = false)
        {
            _desiredPosition = position;
            
            if(force)
                transform.localPosition = _desiredPosition;
        }
        
        public virtual void SetScale(Vector3 scale, bool force = false)
        {
            _desiredScale = scale;
            
            if(force)
                transform.localScale = _desiredScale;
        }
        
        // Animations
        public void AppearPiece(string propertyName, float duration, System.Action<bool> callback)
        {
            if(_appearCoroutine != null)
                StopCoroutine(_appearCoroutine);
            
            _appearCoroutine = StartCoroutine(AppearPieceAnimation(propertyName,  duration, callback));
        }

        private IEnumerator AppearPieceAnimation(string propertyName, float duration, System.Action<bool> callback)
        {
            callback?.Invoke(false);
            yield return new WaitForEndOfFrame();
            CheckShaderKeywordState("_APPEARANCE_STATE");
            float time = 0;
            while (time < duration)
            {
                float lerpValue = Mathf.Lerp(0f, 1f, time / duration);
                _renderer.material.SetFloat(propertyName, Mathf.Clamp(lerpValue, 0f ,1f));
                time += Time.deltaTime;
                yield return null;
            }
            
            _renderer.material.SetFloat(propertyName, 1f);
            
            callback?.Invoke(true);
        }

        public void DisappearPiece(string propertyName, float duration, System.Action<bool> callback)
        {
            if(_disappearCoroutine != null)
                StopCoroutine(_disappearCoroutine);
            
            _disappearCoroutine = StartCoroutine(DisappearPieceAnimation(propertyName, duration, callback));
        }

        private IEnumerator DisappearPieceAnimation(string propertyName, float duration, System.Action<bool> callback)
        {
            callback?.Invoke(false);
            yield return new WaitForEndOfFrame();
            CheckShaderKeywordState("_APPEARANCE_STATE");
            float time = 0;
            while (time < duration)
            {
                float lerpValue = Mathf.Lerp(1f, 0f, time / duration);
                _renderer.material.SetFloat(propertyName, Mathf.Clamp(lerpValue, 0f, 1f));
                time += Time.deltaTime;
                yield return null;
            }
            _renderer.material.SetFloat(propertyName, 0f);
            
            callback?.Invoke(true);
        }

        public void DestroyPiece(string propertyName, float duration, System.Action<bool> callback)
        {
            if(_destroyedCoroutine != null)
                StopCoroutine(_destroyedCoroutine);

            _destroyedCoroutine = StartCoroutine(DestroyPieceAnimation(propertyName, duration, callback));
        }

        private IEnumerator DestroyPieceAnimation(string propertyName, float duration, System.Action<bool> callback)
        {
            callback?.Invoke(false);
            yield return new WaitForEndOfFrame();
            CheckShaderKeywordState("_APPEARANCE_STATE");
            float time = 0;
            while (time < duration)
            {
                float lerpValue = Mathf.Lerp(1f, 0f, time / duration);
                _renderer.material.SetFloat(propertyName, Mathf.Clamp(lerpValue, 0f, 1f));
                time += Time.deltaTime;
                yield return null;
            }
            
            _renderer.material.SetFloat(propertyName, 0f);

            callback?.Invoke(true);
        }
        
        // Utility
        private void SetKeyWord<T>(string parameterBaseName, T selectedKeyword) where T: Enum
        {
            var shader = _renderer.material.shader;
            var keywordSpace = shader.keywordSpace;
            var keywords = Enum.GetValues(typeof(T));
            
            _appear = Enum.Parse<Appearance>(selectedKeyword.ToString());

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
        
        void CheckShaderKeywordState(string keyword = "")
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
                    var log = "Local keyword with name of <color=\"yellow\">" + localKeyword.name +
                                 "</color> is overridden by a global keyword, and is <color=\"green\">enabled</color>";
                    if (keyword.Length > 0)
                    {
                        if(localKeyword.name.Contains(keyword))
                            Log.LogThis(log, this);
                        return;
                    }
                    
                    Log.LogThis(log, this);
                }
                // Otherwise, Unity uses the local keyword state
                else
                {
                    var state = _renderer.material.IsKeywordEnabled(localKeyword) ? "enabled" : "disabled";
                    var color = _renderer.material.IsKeywordEnabled(localKeyword) ? "green" : "red";
                    var log = $"Local keyword with name of <color=\"yellow\">{localKeyword.name}</color> is <color=\"{color}\">{state}</color>";
                    if (keyword.Length > 0)
                    {
                        if(localKeyword.name.Contains(keyword))
                            Log.LogThis(log, this);
                        return;
                    }
                    
                    Log.LogThis(log, this);
                }            
            }
        }
    }
}
