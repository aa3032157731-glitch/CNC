using System.IO;
using System.Text;
using CNC.DigitalTwin;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CNC.EditorTools
{
    public static class DeepSceneForensicAudit
    {
        private const string OutPath = @"C:\Users\qqcom\.gemini\antigravity\brain\8cf605aa-ffcf-453b-9a7e-a7b8353edcb5\scratch\unity_forensic_probe.txt";

        [MenuItem("CNC Optimize/Run Deep Forensic Scene Probe")]
        public static void RunProbe()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            var sb = new StringBuilder();
            sb.AppendLine("=== UNITY DEEP FORENSIC SCENE PROBE ===");

            // 1. Exact Floor World Bounds for Building & Building (1)
            sb.AppendLine("\n--- 1. BUILDING FLOORS & ROOFS ---");
            foreach (var bname in new[] { "Building", "Building (1)" })
            {
                var b = GameObject.Find(bname);
                if (b == null) { sb.AppendLine($"  MISSING {bname}"); continue; }
                sb.AppendLine($"  {bname}: pos={b.transform.position}, rot={b.transform.eulerAngles}, scale={b.transform.lossyScale}");
                foreach (var tr in b.GetComponentsInChildren<Transform>(true))
                {
                    if (tr.name.Contains("Floor") || tr.name.Contains("Shutter") || tr.name.Contains("升降门"))
                    {
                        var mr = tr.GetComponent<MeshRenderer>();
                        var bc = tr.GetComponent<BoxCollider>();
                        sb.AppendLine($"    {tr.name}: mrBounds={(mr != null ? $"{mr.bounds.min}..{mr.bounds.max}" : "none")} | bc={(bc != null ? $"en={bc.enabled} trig={bc.isTrigger} size={bc.size} worldBounds={bc.bounds.min}..{bc.bounds.max}" : "none")}");
                    }
                }
            }

            // 2. ALL BoxColliders with negative lossyScale, negative size, or thin (< 0.05m world thickness)
            sb.AppendLine("\n--- 2. PROBLEMATIC BOXCOLLIDERS ---");
            int badBc = 0;
            foreach (var bc in Object.FindObjectsOfType<BoxCollider>(true))
            {
                Vector3 ls = bc.transform.lossyScale;
                Vector3 sz = bc.size;
                Vector3 wsz = new Vector3(Mathf.Abs(ls.x * sz.x), Mathf.Abs(ls.y * sz.y), Mathf.Abs(ls.z * sz.z));
                bool negScale = ls.x < 0 || ls.y < 0 || ls.z < 0;
                bool negSize = sz.x < 0 || sz.y < 0 || sz.z < 0;
                bool ultraThin = !bc.isTrigger && (wsz.x < 0.02f || wsz.y < 0.02f || wsz.z < 0.02f);
                if (negScale || negSize || ultraThin)
                {
                    badBc++;
                    sb.AppendLine($"  BC '{GetPath(bc.transform)}' active={bc.gameObject.activeInHierarchy} en={bc.enabled} trig={bc.isTrigger} ls={ls} sz={sz} worldSz={wsz} negScale={negScale} negSize={negSize} thin={ultraThin}");
                }
            }
            sb.AppendLine($"  Total problematic BoxColliders: {badBc}");

            // 3. Check ECM_FirstPerson & Camera Hierarchy
            sb.AppendLine("\n--- 3. ECM_FIRSTPERSON & CAMERA ---");
            var fps = GameObject.Find("ECM_FirstPerson");
            if (fps != null)
            {
                foreach (var tr in fps.GetComponentsInChildren<Transform>(true))
                    sb.AppendLine($"  {GetPath(tr)}: pos={tr.position} localPos={tr.localPosition} rot={tr.eulerAngles} scale={tr.localScale}");
            }

            // 4. Check ALL 9 Stations: actual target world bounds + camera LOS obstruction check!
            sb.AppendLine("\n--- 4. ALL 9 STATIONS BOUNDS & CAMERA LOS ---");
            var cat = AssetDatabase.LoadAssetAtPath<WorkshopCatalog>("Assets/_Project/Configs/WorkshopCatalog.asset");
            if (cat != null)
            {
                foreach (var st in cat.stations)
                {
                    Bounds combined = new Bounds();
                    bool hasBounds = false;
                    if (st.sceneTargetNames != null)
                    {
                        foreach (var n in st.sceneTargetNames)
                        {
                            var go = GameObject.Find(n);
                            if (go == null) continue;
                            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>(true))
                            {
                                if (!hasBounds) { combined = mr.bounds; hasBounds = true; }
                                else combined.Encapsulate(mr.bounds);
                            }
                        }
                    }

                    Vector3 dir = st.focusLookAtPos - st.focusCameraPos;
                    float dist = dir.magnitude;
                    var hits = Physics.RaycastAll(st.focusCameraPos, dir.normalized, dist * 0.92f);
                    System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                    string hitSummary = "";
                    foreach (var h in hits)
                    {
                        if (h.collider.isTrigger) continue;
                        hitSummary += $" [{h.distance:F1}m:{GetPath(h.collider.transform)}]";
                    }
                    sb.AppendLine($"  {st.code} ({st.shortName}) hall={st.hall}:");
                    sb.AppendLine($"    targetBounds={(hasBounds ? $"center={combined.center} size={combined.size} min={combined.min} max={combined.max}" : "NONE")}");
                    sb.AppendLine($"    worldPos={st.worldPosition} cam={st.focusCameraPos} lookAt={st.focusLookAtPos} dist={dist:F1}m");
                    sb.AppendLine($"    LOS solid hits (first 92%): {(string.IsNullOrEmpty(hitSummary) ? "CLEAR" : hitSummary)}");
                }
            }

            // 5. Check 末并 / 精梳 / 粗纱 / 细纱 / 络筒 & AGVs & Stray Below-Floor Objects
            sb.AppendLine("\n--- 5. FLOOR PADS (末并/精梳/粗纱/细纱/络筒) ---");
            foreach (var pname in new[] { "末并", "精梳", "粗纱", "细纱", "络筒", "抓棉机UI", "除杂、梳棉、卷棉UI" })
            {
                var go = GameObject.Find(pname);
                if (go == null) continue;
                sb.AppendLine($"  {pname}: pos={go.transform.position} active={go.activeInHierarchy} children={go.transform.childCount}");
                foreach (Transform ch in go.transform)
                {
                    var mr = ch.GetComponent<MeshRenderer>();
                    sb.AppendLine($"    child '{ch.name}' pos={ch.position} localPos={ch.localPosition} active={ch.gameObject.activeInHierarchy} mr={(mr != null ? mr.bounds.center.ToString() : "none")}");
                }
            }

            sb.AppendLine("\n--- 6. AGVs & BELOW-FLOOR OBJECTS (bounds.min.y < 0.2) ---");
            foreach (var tr in Object.FindObjectsOfType<Transform>(true))
            {
                if (tr.parent == null && tr.name.ToLowerInvariant().StartsWith("agv"))
                {
                    sb.AppendLine($"  AGV '{tr.name}': pos={tr.position} rot={tr.eulerAngles} scale={tr.localScale}");
                }
            }
            int belowCnt = 0;
            foreach (var mr in Object.FindObjectsOfType<MeshRenderer>(true))
            {
                if (!mr.gameObject.activeInHierarchy) continue;
                if (mr.bounds.max.y < 0.8f)
                {
                    belowCnt++;
                    if (belowCnt <= 30)
                        sb.AppendLine($"  BELOW_FLOOR MR '{GetPath(mr.transform)}': boundsMin={mr.bounds.min} boundsMax={mr.bounds.max}");
                }
            }
            sb.AppendLine($"  Total active MeshRenderers completely below Y=0.8: {belowCnt}");

            // 7. Check ALL hanging signs / world canvases / legacy scripts with OnCollision/Trigger
            sb.AppendLine("\n--- 7. LEGACY CONVEYOR / TRIGGER CUBES ---");
            foreach (var cname in new[] { "Cube", "Cube (1)", "Cube (2)", "BigJuan", "chun", "CNC", "货车" })
            {
                foreach (var tr in Object.FindObjectsOfType<Transform>(true))
                {
                    if (tr.parent == null && tr.name == cname)
                    {
                        var mr = tr.GetComponent<MeshRenderer>();
                        var bc = tr.GetComponent<BoxCollider>();
                        sb.AppendLine($"  Root '{tr.name}': active={tr.gameObject.activeInHierarchy} pos={tr.position} scale={tr.localScale} mr={(mr != null ? mr.bounds.size.ToString() : "noMR")} bc={(bc != null ? bc.bounds.size.ToString() : "noBC")}");
                    }
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
            File.WriteAllText(OutPath, sb.ToString(), Encoding.UTF8);
            Debug.Log("[CNC PROBE] Deep forensic probe written to " + OutPath);
        }

        private static string GetPath(Transform t)
        {
            string p = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                p = t.name + "/" + p;
            }
            return p;
        }
    }
}
