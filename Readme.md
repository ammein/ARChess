# AR Wizard Chess

# Import Lygia into this folder directory
```shell
# In root project folder
cd Assets/ARChess/Shader

# Import Lygia using degit format https://lygia.xyz
npx degit https://github.com/patriciogonzalezvivo/lygia.git lygia

# Prune to get only HLSL files (Must have Python installed in System)
python prune.py --all --keep hlsl
```