using System.Runtime.CompilerServices;
#if UNITY_EDITOR
[assembly: InternalsVisibleTo("Unity.MARS.Editor")]
// Shared test assembly used as part of Unity testing conventions.
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor-testable")]
[assembly: InternalsVisibleTo("Assembly-CSharp-testable")]
#endif
