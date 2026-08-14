# Unity Methods

Methods Unity calls by name on a MonoBehaviour or an editor window. Unity fixes
these names, so the casing check skips them the same way it skips an override.
A plain dotnet project has no such file, so the set is empty and nothing is
skipped.

+ Awake
+ Start
+ Update
+ FixedUpdate
+ LateUpdate
+ OnEnable
+ OnDisable
+ OnDestroy
+ OnGUI
+ OnValidate
+ OnSceneOpened
+ OnSceneGUI
+ OnInspectorGUI
+ OnAnimatorIK
