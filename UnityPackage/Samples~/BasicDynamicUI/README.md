# Basic Dynamic UI Sample

This sample demonstrates how to cache a global UniRx reactive property (`GlobalScore`) on a root object, and dynamically instantiate a prefab (`ScoreDisplay`) that automatically resolves the dependency and subscribes to it.

## How to set up the scene:
1. Create a new Scene.
2. Create an empty GameObject named "GameRoot".
3. Attach the `GameRoot` script to it.
4. Create another empty GameObject, name it "ScoreDisplayPrefab", and attach the `ScoreDisplay` script to it.
5. Drag "ScoreDisplayPrefab" into your Project view to make it a prefab, then delete it from the scene.
6. Select "GameRoot" in the hierarchy, and assign your new "ScoreDisplayPrefab" to the `scoreDisplayPrefab` field in the inspector.
7. Press Play! You will see the score incrementing in the console, and after 2 seconds, the prefab will instantiate and immediately log the current score using DI.
