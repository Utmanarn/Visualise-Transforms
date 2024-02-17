using UnityEditor;
using UnityEngine;
using Vectors;

[ExecuteAlways, RequireComponent(typeof(VectorRenderer))]
public class VectorMath : MonoBehaviour {
    
    private VectorRenderer vectors;
    [Range(0, 1)] public float Time = 0.0f;

    public bool translation, scaling, rotation;

    [SerializeField, HideInInspector] internal Matrix4x4 A, B, C;

    void OnEnable() {
        if (!TryGetComponent<VectorRenderer>(out vectors))
        {
            vectors = gameObject.AddComponent<VectorRenderer>();
        }
    }

    void Update()
    {
        using (vectors.Begin())
        {
            C = ComputeMatrix();
            Vector3 right = C.MultiplyVector(Vector3.right);
            Vector3 up = C.MultiplyVector(Vector3.up);
            Vector3 forward = C.MultiplyVector(Vector3.forward);
            Vector3 translation = C.GetTranslation();
            
            vectors.Draw(translation-forward/2f-up/2f-right/2f,translation-forward/2f-up/2f+right/2f,Color.red);
            vectors.Draw(translation-forward/2f+up/2f-right/2f,translation-forward/2f+up/2f+right/2f,Color.red);
            vectors.Draw(translation+forward/2f-up/2f-right/2f,translation+forward/2f-up/2f+right/2f,Color.red);
            vectors.Draw(translation+forward/2f+up/2f-right/2f,translation+forward/2f+up/2f+right/2f,Color.red);
            vectors.Draw(translation+forward/2f-up/2f-right/2f,translation+forward/2f+up/2f-right/2f,Color.green);
            vectors.Draw(translation+forward/2f-up/2f+right/2f,translation+forward/2f+up/2f+right/2f,Color.green);
            vectors.Draw(translation-forward/2f-up/2f-right/2f,translation-forward/2f+up/2f-right/2f,Color.green);
            vectors.Draw(translation-forward/2f-up/2f+right/2f,translation-forward/2f+up/2f+right/2f,Color.green);
            vectors.Draw(translation-forward/2f+up/2f-right/2f,translation+forward/2f+up/2f-right/2f,Color.blue);
            vectors.Draw(translation-forward/2f-up/2f-right/2f,translation+forward/2f-up/2f-right/2f,Color.blue);
            vectors.Draw(translation-forward/2f+up/2f+right/2f,translation+forward/2f+up/2f+right/2f,Color.blue);
            vectors.Draw(translation-forward/2f-up/2f+right/2f,translation+forward/2f-up/2f+right/2f,Color.blue);
        }
    }
    
    private Matrix4x4 ComputeMatrix() => Matrix4x4.identity.SetMatrixComponents(
        InterpolateVector(A.GetTranslation(), B.GetTranslation(), Time, translation),
        InterpolateQuaternion(A.GetRotation(), B.GetRotation(), Time, rotation),
        InterpolateVector(A.GetScale(), B.GetScale(), Time, scaling));

    private Vector3 InterpolateVector(Vector3 a, Vector3 b, float time, bool check) => check ? (1f - time) * a + b * time : a;

    private Quaternion InterpolateQuaternion(Quaternion a, Quaternion b, float time, bool check)
    {
        if (!check || time == 0) return a;
        if (time == 1) return b;
        Quaternion c = b * a.Inverse();
        if (c.w > 0)
        {
            c = c.InverseFull();
        }

        float angle = (Mathf.PI - Mathf.Acos(c.w)) * time;
        Vector3 rotAxis = c.Eigen().normalized * Mathf.Sin(angle);

        return new Quaternion
        {
            w = -Mathf.Cos(angle),
            x = rotAxis.x,
            y = rotAxis.y,
            z = rotAxis.z
        } * a;
    }
}

[CustomEditor(typeof(VectorMath))]
public class VectorMathEditor : Editor
{
    public void OnSceneGUI()
    {
        var vectorMath = target as VectorMath;
        if (!vectorMath) return;

        EditorGUI.BeginChangeCheck();

        Matrix4x4 a = MatrixHandles(vectorMath.A);
        Matrix4x4 b = MatrixHandles(vectorMath.B);

        if (!EditorGUI.EndChangeCheck()) return;
        Undo.RecordObject(vectorMath, "Moved position");

        vectorMath.A = a;
        vectorMath.B = b;
            
        EditorUtility.SetDirty(vectorMath);
    }

    private Matrix4x4 MatrixHandles(Matrix4x4 matrix)
    {
        Matrix4x4 result = Matrix4x4.identity;
        matrix.GetMatrixComponents(out Vector3 t, out Quaternion q, out Vector3 s);

        var td = Handles.PositionHandle(t, q);
        var rd = Handles.RotationHandle(q, t);
        var sd = Handles.ScaleHandle(s, t, q, 1);
        
        result.SetTranslation(td);
        result.SetRotation(rd);
        result.SetScale(sd);
        return result;
    }
    private Matrix4x4 DisplayMatrix(Matrix4x4 matrix, string n)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PrefixLabel(n);
        EditorGUILayout.BeginVertical();

        Matrix4x4 result = Matrix4x4.identity;
        for (int i = 0; i < 4; i++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int j = 0; j < 4; j++)
            {
                result[i, j] = EditorGUILayout.FloatField(matrix[i, j]);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Determinant:");
        EditorGUILayout.FloatField(result.GetDeterminant());
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
        return result;
    }

    private Matrix4x4 DisplayMatrix(ref Matrix4x4 matrix, string n)
    {
        Matrix4x4 result = DisplayMatrix(matrix, n);
        if(GUILayout.Button("Reset Matrix")) result = Matrix4x4.identity;
        if(GUILayout.Button("Reset Translation")) result.SetTranslation(Vector3.zero);
        if(GUILayout.Button("Reset Rotation")) result.SetRotation(Quaternion.identity);
        if(GUILayout.Button("Reset Scale")) result.SetScale(Vector3.one);
        EditorGUILayout.Space();
        return result;
    }
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        EditorGUILayout.Space();
        
        var vectorMath = target as VectorMath;
        if (!vectorMath) return;
        
        // Matrix A
        EditorGUI.BeginChangeCheck();

        Matrix4x4 a = DisplayMatrix(ref vectorMath.A, "Matrix A");
        Matrix4x4 b = DisplayMatrix(ref vectorMath.B, "Matrix B");
        DisplayMatrix(vectorMath.C, "Matrix C");

        if (!EditorGUI.EndChangeCheck()) return;
        Undo.RecordObject(vectorMath, "Changed matrix");
        vectorMath.A = a;
        vectorMath.B = b;
        EditorUtility.SetDirty(vectorMath);
    }
}

public static class MatrixExtentionMethods
{
    public static Vector3 GetTranslation(this Matrix4x4 matrix) => matrix.GetColumn(3);

    public static Quaternion GetRotation(this Matrix4x4 matrix)
    {
        matrix.SetScale(Vector3.one);
        Vector3 result = Vector3.Cross(matrix.GetColumn(0), Vector3.right) +
                         Vector3.Cross(matrix.GetColumn(1), Vector3.up) +
                         Vector3.Cross(matrix.GetColumn(2), Vector3.forward);
        if(result == Vector3.zero) return Quaternion.identity;
        float trace = matrix.m00 + matrix.m11 + matrix.m22;
        float cosAngle = Mathf.Clamp((trace-1f) / 2f, -1, 1);
        float cosHalfAngle = Mathf.Sqrt((1 + cosAngle) / 2f);
        float sinHalfAngle = Mathf.Sqrt((1 - cosAngle) / 2f);
        Vector3 rotAxis = result.normalized * -sinHalfAngle;

        return new Quaternion
        {
            w = cosHalfAngle,
            x = rotAxis.x,
            y = rotAxis.y,
            z = rotAxis.z
        }.normalized;
    }

    public static void SetScale(ref this Matrix4x4 matrix, Vector3 scale)
    {
        matrix.SetColumn(0, matrix.GetColumn(0).normalized * scale.x);
        matrix.SetColumn(1, matrix.GetColumn(1).normalized * scale.y);
        matrix.SetColumn(2, matrix.GetColumn(2).normalized * scale.z);
    }

    public static Vector3 GetScale(this Matrix4x4 matrix) => new Vector3(matrix.GetColumn(0).magnitude, matrix.GetColumn(1).magnitude, matrix.GetColumn(2).magnitude);


    public static void SetTranslation(ref this Matrix4x4 matrix, Vector3 translation) =>
        matrix.SetColumn(3, new Vector4(translation.x, translation.y, translation.z, 1));
    
    public static void SetRotation(ref this Matrix4x4 matrix, Quaternion rotation)
    {
        Vector3 scale = matrix.GetScale();
        matrix.SetColumn(0, rotation * Vector3.right * scale.x);
        matrix.SetColumn(1, rotation * Vector3.up * scale.y);
        matrix.SetColumn(2, rotation * Vector3.forward * scale.z);
    }

    public static void GetMatrixComponents(this Matrix4x4 matrix, out Vector3 t, out Quaternion q, out Vector3 s)
    {
        t = matrix.GetTranslation();
        q = matrix.GetRotation();
        s = matrix.GetScale();
    }

    public static Matrix4x4 SetMatrixComponents(this Matrix4x4 matrix, Vector3 t, Quaternion q, Vector3 s)
    {
        matrix.SetTranslation(t);
        matrix.SetRotation(q);
        matrix.SetScale(s);
        return matrix;
    }

    public static float GetDeterminant(this Matrix4x4 matrix)
    {
        matrix.SetScale(Vector3.one);
        return matrix.GetDiagonalP(0) + matrix.GetDiagonalP(1) + matrix.GetDiagonalP(2) - 
               matrix.GetDiagonalR(0) - matrix.GetDiagonalR(1) - matrix.GetDiagonalR(2);
    }

    private static float GetDiagonalP(this Matrix4x4 matrix, int xShift)
    {
        return xShift switch
        {
            (0) => matrix.m00 * matrix.m11 * matrix.m22,
            (1) => matrix.m01 * matrix.m12 * matrix.m20,
            (2) => matrix.m02 * matrix.m10 * matrix.m21,
            _ => 1f
        };
    }
    
    private static float GetDiagonalR(this Matrix4x4 matrix, int xShift)
    {
        return xShift switch
        {
            (0) => matrix.m00 * matrix.m12 * matrix.m21,
            (1) => matrix.m01 * matrix.m10 * matrix.m22,
            (2) => matrix.m02 * matrix.m11 * matrix.m20,
            _ => 1f
        };
    }

    public static Vector3 Eigen(this Quaternion q) => new Vector3(q.x, q.y, q.z);

    public static Quaternion Inverse(this Quaternion q) => new Quaternion
    {
        w = -q.w,
        x = q.x,
        y = q.y,
        z = q.z
    };
    
    public static Quaternion InverseFull(this Quaternion q) => new Quaternion
    {
        w = -q.w,
        x = -q.x,
        y = -q.y,
        z = -q.z
    };
}