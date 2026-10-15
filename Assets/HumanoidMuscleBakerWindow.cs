using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class HumanoidMuscleBakerWindow : EditorWindow
{
    [SerializeField] private GameObject sourceRoot;
    [SerializeField] private Avatar sourceAvatar;
    [SerializeField] private AnimationClip sourceClip;
    [SerializeField] private float sampleRate = 60f;
    [SerializeField] private bool includeRootMotion = true;

    [MenuItem("Tools/Animation/Humanoid Muscle Baker")]
    private static void Open()
    {
        GetWindow<HumanoidMuscleBakerWindow>("Muscle Baker");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Transform Clip → Humanoid Muscle", EditorStyles.boldLabel);

        sourceRoot = (GameObject)EditorGUILayout.ObjectField(
            "Source Root", sourceRoot, typeof(GameObject), true);

        sourceAvatar = (Avatar)EditorGUILayout.ObjectField(
            "Source Avatar", sourceAvatar, typeof(Avatar), false);

        sourceClip = (AnimationClip)EditorGUILayout.ObjectField(
            "Source Clip", sourceClip, typeof(AnimationClip), false);

        sampleRate = EditorGUILayout.FloatField("Sample Rate", sampleRate);
        includeRootMotion = EditorGUILayout.Toggle("Include Root Motion", includeRootMotion);

        EditorGUILayout.HelpBox(
            "Source Root에는 원본 클립의 Transform 경로가 실제로 존재해야 합니다.",
            MessageType.Info);

        using (new EditorGUI.DisabledScope(
                   sourceRoot == null ||
                   sourceAvatar == null ||
                   sourceClip == null))
        {
            if (GUILayout.Button("Bake Humanoid Muscle Clip"))
                Bake();
        }
    }

    private void Bake()
    {
        if (!sourceAvatar.isValid || !sourceAvatar.isHuman)
        {
            Debug.LogError("유효한 Humanoid Avatar가 아닙니다.");
            return;
        }

        if (sampleRate <= 0f)
        {
            Debug.LogError("Sample Rate는 0보다 커야 합니다.");
            return;
        }

        string path = EditorUtility.SaveFilePanelInProject(
            "Save Humanoid Muscle Clip",
            sourceClip.name + " Humanoid",
            "anim",
            "저장 위치를 선택하세요.");

        if (string.IsNullOrEmpty(path))
            return;

        GameObject clone = null;
        HumanPoseHandler poseHandler = null;

        try
        {
            clone = Instantiate(sourceRoot);
            clone.name = sourceRoot.name;
            clone.hideFlags = HideFlags.HideAndDontSave;

            // bodyPosition에 씬 배치 위치가 섞이지 않도록 초기화
            clone.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            // 반드시 Source Root 자체를 Humanoid 기준 루트로 사용한다.
            Animator animator = clone.GetComponent<Animator>();

            if (animator == null)
                animator = clone.AddComponent<Animator>();

            animator.enabled = false;
            animator.avatar = sourceAvatar;

            poseHandler = new HumanPoseHandler(
                sourceAvatar,
                clone.transform);

            int muscleCount = HumanTrait.MuscleCount;
            var muscleCurves = new AnimationCurve[muscleCount];

            for (int i = 0; i < muscleCount; i++)
                muscleCurves[i] = new AnimationCurve();

            var rootCurves = new AnimationCurve[7];

            for (int i = 0; i < rootCurves.Length; i++)
                rootCurves[i] = new AnimationCurve();

            var pose = new HumanPose
            {
                muscles = new float[muscleCount]
            };

            int frameCount = Mathf.CeilToInt(sourceClip.length * sampleRate);

            Quaternion previousRotation = Quaternion.identity;
            bool hasPreviousRotation = false;

            for (int frame = 0; frame <= frameCount; frame++)
            {
                float time = Mathf.Min(
                    frame / sampleRate,
                    sourceClip.length);

                // 기존 Transform 곡선을 임시 캐릭터에 적용
                sourceClip.SampleAnimation(clone, time);

                // 현재 뼈 자세를 Humanoid Muscle 값으로 변환
                poseHandler.GetHumanPose(ref pose);

                for (int muscle = 0; muscle < muscleCount; muscle++)
                    muscleCurves[muscle].AddKey(time, pose.muscles[muscle]);

                if (includeRootMotion)
                {
                    Vector3 position = pose.bodyPosition;
                    Quaternion rotation = pose.bodyRotation;

                    // Quaternion 부호가 갑자기 뒤집히는 현상 방지
                    if (hasPreviousRotation &&
                        Quaternion.Dot(previousRotation, rotation) < 0f)
                    {
                        rotation = new Quaternion(
                            -rotation.x,
                            -rotation.y,
                            -rotation.z,
                            -rotation.w);
                    }

                    previousRotation = rotation;
                    hasPreviousRotation = true;

                    rootCurves[0].AddKey(time, position.x);
                    rootCurves[1].AddKey(time, position.y);
                    rootCurves[2].AddKey(time, position.z);

                    rootCurves[3].AddKey(time, rotation.x);
                    rootCurves[4].AddKey(time, rotation.y);
                    rootCurves[5].AddKey(time, rotation.z);
                    rootCurves[6].AddKey(time, rotation.w);
                }
            }

            var outputClip = new AnimationClip
            {
                name = sourceClip.name + " Humanoid",
                frameRate = sampleRate
            };

            var bindings = new List<EditorCurveBinding>();
            var curves = new List<AnimationCurve>();

            for (int i = 0; i < muscleCount; i++)
            {
                SetLinearTangents(muscleCurves[i]);

                bindings.Add(EditorCurveBinding.FloatCurve(
                    string.Empty,
                    typeof(Animator),
                    GetAnimatorMuscleProperty(HumanTrait.MuscleName[i])));

                curves.Add(muscleCurves[i]);
            }

            if (includeRootMotion)
            {
                string[] rootProperties =
                {
                    "RootT.x", "RootT.y", "RootT.z",
                    "RootQ.x", "RootQ.y", "RootQ.z", "RootQ.w"
                };

                for (int i = 0; i < rootProperties.Length; i++)
                {
                    SetLinearTangents(rootCurves[i]);

                    bindings.Add(EditorCurveBinding.FloatCurve(
                        string.Empty,
                        typeof(Animator),
                        rootProperties[i]));

                    curves.Add(rootCurves[i]);
                }
            }

            AnimationUtility.SetEditorCurves(
                outputClip,
                bindings.ToArray(),
                curves.ToArray());

            outputClip.EnsureQuaternionContinuity();

            AnimationUtility.SetAnimationEvents(
                outputClip,
                AnimationUtility.GetAnimationEvents(sourceClip));

            AssetDatabase.CreateAsset(outputClip, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorGUIUtility.PingObject(outputClip);

            Debug.Log(
                $"생성 완료: {path}\n" +
                $"Humanoid Motion: {outputClip.humanMotion}",
                outputClip);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            poseHandler?.Dispose();

            if (clone != null)
                DestroyImmediate(clone);
        }
    }

    private static void SetLinearTangents(AnimationCurve curve)
    {
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(
                curve, i, AnimationUtility.TangentMode.Linear);

            AnimationUtility.SetKeyRightTangentMode(
                curve, i, AnimationUtility.TangentMode.Linear);
        }
    }

    // 손가락 Muscle은 HumanTrait 이름과 Animator 속성 이름이 다릅니다.
    private static string GetAnimatorMuscleProperty(string muscleName)
    {
        string side;
        string remainder;

        if (muscleName.StartsWith("Left ", StringComparison.Ordinal))
        {
            side = "Left";
            remainder = muscleName.Substring(5);
        }
        else if (muscleName.StartsWith("Right ", StringComparison.Ordinal))
        {
            side = "Right";
            remainder = muscleName.Substring(6);
        }
        else
        {
            return muscleName;
        }

        string[] fingers =
        {
            "Thumb", "Index", "Middle", "Ring", "Little"
        };

        foreach (string finger in fingers)
        {
            if (!remainder.StartsWith(finger + " ", StringComparison.Ordinal))
                continue;

            remainder = remainder.Replace(finger + " 1 ", finger + ".1 ");
            remainder = remainder.Replace(finger + " 2 ", finger + ".2 ");
            remainder = remainder.Replace(finger + " 3 ", finger + ".3 ");

            return side + "Hand." + remainder;
        }

        return muscleName;
    }
}