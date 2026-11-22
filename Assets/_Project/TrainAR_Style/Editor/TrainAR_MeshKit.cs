using System;
using System.Collections.Generic;
using UnityEngine;

namespace CNC.TrainAR.Editor
{
    /// <summary>
    /// Procedural hard-surface CAD-to-Stylized Mesh Builder for TrainAR-style industrial modeling.
    /// Produces clean beveled boxes, extruded 2D profiles (with optional cutaway faces on separate submeshes),
    /// lathes/surfaces of revolution, spoked mechanical drums, toothed gears/sprockets, swept pipes/sliver coils,
    /// automatic crease-angle normal splitting (smooth drums + razor-sharp architectural ink creases),
    /// clean box/cylindrical UVs, and vertex-color baked AO.
    /// </summary>
    public class TrainAR_MeshKit
    {
        private struct Tri
        {
            public Vector3 p0, p1, p2;
            public Vector2 uv0, uv1, uv2;
            public Color c0, c1, c2;
            public int submesh;
            public bool forceFlat;
        }

        private readonly List<Tri> _tris = new List<Tri>(4096);
        private int _submeshCount = 1;

        public void EnsureSubmesh(int submesh)
        {
            if (submesh + 1 > _submeshCount) _submeshCount = submesh + 1;
        }

        public void AddTri(Vector3 a, Vector3 b, Vector3 c,
            Vector2 uva, Vector2 uvb, Vector2 uvc,
            int submesh = 0, float aoA = 1f, float aoB = 1f, float aoC = 1f, bool forceFlat = false)
        {
            // Skip degenerate triangles
            if (Vector3.Cross(b - a, c - a).sqrMagnitude < 1e-12f) return;
            EnsureSubmesh(submesh);
            _tris.Add(new Tri
            {
                p0 = a, p1 = b, p2 = c,
                uv0 = uva, uv1 = uvb, uv2 = uvc,
                c0 = new Color(aoA, aoA, aoA, 1f),
                c1 = new Color(aoB, aoB, aoB, 1f),
                c2 = new Color(aoC, aoC, aoC, 1f),
                submesh = submesh,
                forceFlat = forceFlat
            });
        }

        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
            int submesh = 0, float aoA = 1f, float aoB = 1f, float aoC = 1f, float aoD = 1f,
            float uvScale = 1f, bool forceFlat = false)
        {
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            Vector2 uva = ProjectBoxUV(a, n, uvScale);
            Vector2 uvb = ProjectBoxUV(b, n, uvScale);
            Vector2 uvc = ProjectBoxUV(c, n, uvScale);
            Vector2 uvd = ProjectBoxUV(d, n, uvScale);

            AddTri(a, b, c, uva, uvb, uvc, submesh, aoA, aoB, aoC, forceFlat);
            AddTri(a, c, d, uva, uvc, uvd, submesh, aoA, aoC, aoD, forceFlat);
        }

        public void AddQuadUV(Vector3 a, Vector3 b, Vector3 c, Vector3 d,
            Vector2 uva, Vector2 uvb, Vector2 uvc, Vector2 uvd,
            int submesh = 0, float ao = 1f, bool forceFlat = false)
        {
            AddTri(a, b, c, uva, uvb, uvc, submesh, ao, ao, ao, forceFlat);
            AddTri(a, c, d, uva, uvc, uvd, submesh, ao, ao, ao, forceFlat);
        }

        private static Vector2 ProjectBoxUV(Vector3 p, Vector3 n, float scale)
        {
            Vector3 an = new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
            if (an.x >= an.y && an.x >= an.z) return new Vector2(p.z, p.y) * scale;
            if (an.y >= an.x && an.y >= an.z) return new Vector2(p.x, p.z) * scale;
            return new Vector2(p.x, p.y) * scale;
        }

        /// <summary>
        /// Adds a crisp 45-degree chamfered box (chunky TrainAR hard-surface silhouette).
        /// </summary>
        public void AddBeveledBox(Vector3 center, Vector3 size, float bevel = 0.025f,
            Quaternion? rotation = null, int submesh = 0, float aoBottom = 0.85f, float aoTop = 1.0f, float uvScale = 1.0f)
        {
            Quaternion rot = rotation ?? Quaternion.identity;
            float hx = size.x * 0.5f;
            float hy = size.y * 0.5f;
            float hz = size.z * 0.5f;
            float b = Mathf.Min(bevel, Mathf.Min(hx, Mathf.Min(hy, hz)) * 0.45f);

            if (b < 0.001f)
            {
                AddSimpleBox(center, size, rot, submesh, aoBottom, aoTop, uvScale);
                return;
            }

            // Build as 3-ring extruded profile along Y (bottom inset -> mid full -> top inset)
            // with chamfered X-Z corners!
            Vector2[] oct = MakeRoundedOctagon(hx, hz, b);
            Vector2[] octInset = MakeRoundedOctagon(hx - b, hz - b, b * 0.45f);

            float[] ys = { -hy, -hy + b, hy - b, hy };
            Vector2[][] rings = { octInset, oct, oct, octInset };
            float[] aos = { aoBottom, Mathf.Lerp(aoBottom, aoTop, 0.25f), Mathf.Lerp(aoBottom, aoTop, 0.9f), aoTop };

            int n = oct.Length;
            Vector3 Xf(Vector2 p2, float y) => center + rot * new Vector3(p2.x, y, p2.y);

            // Side rings (outward CCW when viewed from outside)
            for (int r = 0; r < 3; r++)
            {
                for (int i = 0; i < n; i++)
                {
                    int next = (i + 1) % n;
                    Vector3 p00 = Xf(rings[r][i], ys[r]);
                    Vector3 p10 = Xf(rings[r][next], ys[r]);
                    Vector3 p11 = Xf(rings[r + 1][next], ys[r + 1]);
                    Vector3 p01 = Xf(rings[r + 1][i], ys[r + 1]);
                    // oct is CCW in XZ (x=cos, z=sin) -> going i->next goes CCW from top (+Y),
                    // so outward normal is (p10 - p00) x (p01 - p00) -> wait: test!
                    // At angle 0: p00=(1,0,0), next at +angle: p10=(cos,0,sin) so (p10-p00)≈(0,0,+1), (p01-p00)=(0,+1,0).
                    // Cross((0,0,1), (0,1,0)) = (-1,0,0) INWARD! So reverse to (p00, p01, p11, p10)!
                    AddQuad(p00, p01, p11, p10, submesh, aos[r], aos[r + 1], aos[r + 1], aos[r], uvScale);
                }
            }

            // Top cap (+Y outward: CW in XZ -> i=0, next, prev -> center, p[next], p[i])
            Vector3 topCenter = Xf(Vector2.zero, hy);
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                Vector3 a = Xf(octInset[i], hy);
                Vector3 bPt = Xf(octInset[next], hy);
                AddTri(topCenter, bPt, a,
                    ProjectBoxUV(topCenter, Vector3.up, uvScale),
                    ProjectBoxUV(bPt, Vector3.up, uvScale),
                    ProjectBoxUV(a, Vector3.up, uvScale), submesh, aoTop, aoTop, aoTop);
            }

            // Bottom cap (-Y outward: CCW in XZ -> center, p[i], p[next])
            Vector3 botCenter = Xf(Vector2.zero, -hy);
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                Vector3 a = Xf(octInset[i], -hy);
                Vector3 bPt = Xf(octInset[next], -hy);
                AddTri(botCenter, a, bPt,
                    ProjectBoxUV(botCenter, Vector3.down, uvScale),
                    ProjectBoxUV(a, Vector3.down, uvScale),
                    ProjectBoxUV(bPt, Vector3.down, uvScale), submesh, aoBottom, aoBottom, aoBottom);
            }
        }

        private static Vector2[] MakeRoundedOctagon(float hx, float hz, float c)
        {
            c = Mathf.Clamp(c, 0.001f, Mathf.Min(hx, hz) * 0.9f);
            // CCW in (x, z)
            return new Vector2[]
            {
                new Vector2( hx, -hz + c),
                new Vector2( hx,  hz - c),
                new Vector2( hx - c,  hz),
                new Vector2(-hx + c,  hz),
                new Vector2(-hx,  hz - c),
                new Vector2(-hx, -hz + c),
                new Vector2(-hx + c, -hz),
                new Vector2( hx - c, -hz),
            };
        }

        public void AddSimpleBox(Vector3 center, Vector3 size, Quaternion rot, int submesh = 0, float aoBot = 0.88f, float aoTop = 1f, float uvScale = 1f)
        {
            Vector3 h = size * 0.5f;
            Vector3 P(float sx, float sy, float sz) => center + rot * new Vector3(sx * h.x, sy * h.y, sz * h.z);

            // +Y
            AddQuad(P(-1, 1, -1), P(-1, 1, 1), P(1, 1, 1), P(1, 1, -1), submesh, aoTop, aoTop, aoTop, aoTop, uvScale);
            // -Y
            AddQuad(P(-1, -1, 1), P(-1, -1, -1), P(1, -1, -1), P(1, -1, 1), submesh, aoBot, aoBot, aoBot, aoBot, uvScale);
            // +Z
            AddQuad(P(-1, -1, 1), P(1, -1, 1), P(1, 1, 1), P(-1, 1, 1), submesh, aoBot, aoBot, aoTop, aoTop, uvScale);
            // -Z
            AddQuad(P(1, -1, -1), P(-1, -1, -1), P(-1, 1, -1), P(1, 1, -1), submesh, aoBot, aoBot, aoTop, aoTop, uvScale);
            // +X
            AddQuad(P(1, -1, 1), P(1, -1, -1), P(1, 1, -1), P(1, 1, 1), submesh, aoBot, aoBot, aoTop, aoTop, uvScale);
            // -X
            AddQuad(P(-1, -1, -1), P(-1, -1, 1), P(-1, 1, 1), P(-1, 1, -1), submesh, aoBot, aoBot, aoTop, aoTop, uvScale);
        }

        /// <summary>
        /// Extrudes a closed 2D polygon (in XY plane, ordered CCW) along Z from zBack to zFront.
        /// Front cap can use a separate submesh (perfect for TrainAR architectural cutaway section faces!).
        /// Uses Ear-Clipping triangulation so concave mechanical profiles & L-shapes work cleanly!
        /// </summary>
        public void AddExtrudedPolygonXY(Vector2[] polyCCW, float zBack, float zFront,
            int sideSubmesh = 0, int frontCapSubmesh = 0, int backCapSubmesh = 0,
            float ao = 1f, float uvScale = 1f, Matrix4x4? transform = null)
        {
            if (polyCCW == null || polyCCW.Length < 3) return;
            Matrix4x4 M = transform ?? Matrix4x4.identity;

            // Ensure CCW winding in XY
            List<Vector2> pts = new List<Vector2>(polyCCW);
            if (SignedArea2D(pts) < 0f) pts.Reverse();

            int n = pts.Count;
            List<int> capIndices = EarClipTriangulate(pts);

            Vector3 Tp(Vector2 p, float z) => M.MultiplyPoint3x4(new Vector3(p.x, p.y, z));

            // Back cap (-Z outward: when looking from -Z, CCW in XY is CW from -Z -> so (i0, i1, i2) points +Z!
            // Let's verify: a=(0,0,0), b=(1,0,0), c=(0,1,0) -> (b-a)x(c-a) = (0,0,+1) = +Z!
            // Unity left-handed cross product: (1,0,0) x (0,1,0) = (0,0,+1)!
            // WAIT: In Unity LEFT-HANDED coordinates: X right, Y up, Z forward!
            // Vector3.Cross(Vector3.right, Vector3.up) = Vector3.forward = (0,0,+1)!
            // AND Unity front-face winding is CLOCKWISE when looking against the normal!
            // Let's verify Unity CW winding: looking at front face whose normal is -Z (towards camera at -Z looking +Z):
            // (0,0,0) -> (0,1,0) -> (1,0,0) is CLOCKWISE! Cross((0,1,0), (1,0,0)) = (0,0,-1) = OUTWARD NORMAL!
            // YES!! In Unity left-handed space: normal = Cross(p1 - p0, p2 - p0) points OUTWARD for Unity's CW front-face!
            // Double check: AddSimpleBox +Z face: (-1,-1,1)->(1,-1,1)->(1,1,1): e1=(2,0,0), e2=(2,2,0), Cross(e1,e2)=(0,0,+4) = +Z! CORRECT!
            // Double check: AddSimpleBox +Y face: (-1,1,-1)->(-1,1,1)->(1,1,1): e1=(0,0,2), e2=(2,0,2), Cross(e1,e2)=(0,+4,0) = +Y! CORRECT!

            // Therefore for XY CCW polygon (where (b-a)x(c-a) = +Z):
            // +Z face (zFront): (a, b, c) has normal +Z!
            for (int t = 0; t < capIndices.Count; t += 3)
            {
                Vector3 a = Tp(pts[capIndices[t]], zFront);
                Vector3 b = Tp(pts[capIndices[t + 1]], zFront);
                Vector3 c = Tp(pts[capIndices[t + 2]], zFront);
                AddTri(a, b, c,
                    new Vector2(pts[capIndices[t]].x, pts[capIndices[t]].y) * uvScale,
                    new Vector2(pts[capIndices[t + 1]].x, pts[capIndices[t + 1]].y) * uvScale,
                    new Vector2(pts[capIndices[t + 2]].x, pts[capIndices[t + 2]].y) * uvScale,
                    frontCapSubmesh, ao, ao, ao);
            }

            // -Z face (zBack): reverse (a, c, b) has normal -Z!
            for (int t = 0; t < capIndices.Count; t += 3)
            {
                Vector3 a = Tp(pts[capIndices[t]], zBack);
                Vector3 b = Tp(pts[capIndices[t + 1]], zBack);
                Vector3 c = Tp(pts[capIndices[t + 2]], zBack);
                AddTri(a, c, b,
                    new Vector2(pts[capIndices[t]].x, pts[capIndices[t]].y) * uvScale,
                    new Vector2(pts[capIndices[t + 2]].x, pts[capIndices[t + 2]].y) * uvScale,
                    new Vector2(pts[capIndices[t + 1]].x, pts[capIndices[t + 1]].y) * uvScale,
                    backCapSubmesh, ao, ao, ao);
            }

            // Side walls: edge p[i] -> p[next] goes CCW around XY. Outward normal is (dy, -dx, 0).
            // Check: e1 = pNext_back - pI_back = (dx, dy, 0); e2 = pI_front - pI_back = (0, 0, +dz).
            // Cross(e1, e2) = (dy*dz, -dx*dz, 0) = OUTWARD!!
            // So quad (pI_back, pNext_back, pNext_front, pI_front) has outward normal!
            for (int i = 0; i < n; i++)
            {
                int next = (i + 1) % n;
                Vector3 b0 = Tp(pts[i], zBack);
                Vector3 b1 = Tp(pts[next], zBack);
                Vector3 f1 = Tp(pts[next], zFront);
                Vector3 f0 = Tp(pts[i], zFront);
                AddQuad(b0, b1, f1, f0, sideSubmesh, ao, ao, ao, ao, uvScale);
            }
        }

        /// <summary>
        /// Extrudes an annular / arched thick shell (outer arc + inner arc) in XY along Z.
        /// Ideal for cutaway cylindrical machine hoods, curved flexible bends (曲轨), and engine-style thick cutaway walls!
        /// </summary>
        public void AddCurvedArchShellXY(Vector2 centerXY, float innerRadius, float outerRadius,
            float startAngleDeg, float endAngleDeg, int segments, float zBack, float zFront,
            int outerSubmesh = 0, int cutFaceSubmesh = 1, int innerSubmesh = 0, float aoInner = 0.78f, float uvScale = 1f)
        {
            float a0 = startAngleDeg * Mathf.Deg2Rad;
            float a1 = endAngleDeg * Mathf.Deg2Rad;
            bool fullCircle = Mathf.Abs(endAngleDeg - startAngleDeg) >= 359.9f;

            for (int i = 0; i < segments; i++)
            {
                float t0 = i / (float)segments;
                float t1 = (i + 1) / (float)segments;
                float ang0 = Mathf.Lerp(a0, a1, t0);
                float ang1 = Mathf.Lerp(a0, a1, t1);

                Vector2 d0 = new Vector2(Mathf.Cos(ang0), Mathf.Sin(ang0));
                Vector2 d1 = new Vector2(Mathf.Cos(ang1), Mathf.Sin(ang1));

                Vector3 o0B = new Vector3(centerXY.x + d0.x * outerRadius, centerXY.y + d0.y * outerRadius, zBack);
                Vector3 o1B = new Vector3(centerXY.x + d1.x * outerRadius, centerXY.y + d1.y * outerRadius, zBack);
                Vector3 o1F = new Vector3(centerXY.x + d1.x * outerRadius, centerXY.y + d1.y * outerRadius, zFront);
                Vector3 o0F = new Vector3(centerXY.x + d0.x * outerRadius, centerXY.y + d0.y * outerRadius, zFront);

                Vector3 i0B = new Vector3(centerXY.x + d0.x * innerRadius, centerXY.y + d0.y * innerRadius, zBack);
                Vector3 i1B = new Vector3(centerXY.x + d1.x * innerRadius, centerXY.y + d1.y * innerRadius, zBack);
                Vector3 i1F = new Vector3(centerXY.x + d1.x * innerRadius, centerXY.y + d1.y * innerRadius, zFront);
                Vector3 i0F = new Vector3(centerXY.x + d0.x * innerRadius, centerXY.y + d0.y * innerRadius, zFront);

                // Outer curved wall (CCW angle -> o0B->o1B x o0B->o0F points OUTWARD radially!)
                AddQuadUV(o0B, o1B, o1F, o0F,
                    new Vector2(t0 * 2f, 0f), new Vector2(t1 * 2f, 0f), new Vector2(t1 * 2f, 1f), new Vector2(t0 * 2f, 1f),
                    outerSubmesh, 1.0f);

                // Inner curved wall (points INWARD toward center -> reverse!)
                AddQuadUV(i1B, i0B, i0F, i1F,
                    new Vector2(t1 * 2f, 0f), new Vector2(t0 * 2f, 0f), new Vector2(t0 * 2f, 1f), new Vector2(t1 * 2f, 1f),
                    innerSubmesh, aoInner);

                // Front cutaway ring cap (+Z outward: i0F->o0F->o1F->i1F: check Cross(o0-i0, o1-i0) = Cross(radial, tangent) = +Z!)
                AddQuad(i0F, o0F, o1F, i1F, cutFaceSubmesh, aoInner, 1f, 1f, aoInner, uvScale);

                // Back ring cap (-Z outward)
                AddQuad(o0B, i0B, i1B, o1B, outerSubmesh, 1f, aoInner, aoInner, 1f, uvScale);
            }

            if (!fullCircle)
            {
                // Start cap (at a0)
                Vector2 d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0));
                Vector3 o0B = new Vector3(centerXY.x + d0.x * outerRadius, centerXY.y + d0.y * outerRadius, zBack);
                Vector3 o0F = new Vector3(centerXY.x + d0.x * outerRadius, centerXY.y + d0.y * outerRadius, zFront);
                Vector3 i0B = new Vector3(centerXY.x + d0.x * innerRadius, centerXY.y + d0.y * innerRadius, zBack);
                Vector3 i0F = new Vector3(centerXY.x + d0.x * innerRadius, centerXY.y + d0.y * innerRadius, zFront);
                AddQuad(i0B, o0B, o0F, i0F, cutFaceSubmesh, aoInner, 1f, 1f, aoInner, uvScale);

                // End cap (at a1)
                Vector2 d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
                Vector3 o1B = new Vector3(centerXY.x + d1.x * outerRadius, centerXY.y + d1.y * outerRadius, zBack);
                Vector3 o1F = new Vector3(centerXY.x + d1.x * outerRadius, centerXY.y + d1.y * outerRadius, zFront);
                Vector3 i1B = new Vector3(centerXY.x + d1.x * innerRadius, centerXY.y + d1.y * innerRadius, zBack);
                Vector3 i1F = new Vector3(centerXY.x + d1.x * innerRadius, centerXY.y + d1.y * innerRadius, zFront);
                AddQuad(o1B, i1B, i1F, o1F, cutFaceSubmesh, 1f, aoInner, aoInner, 1f, uvScale);
            }
        }

        /// <summary>
        /// Builds a surface of revolution (Lathe) around local Y axis, transformed by center & rot.
        /// profileRY is an ordered list of (radius, y) from bottom to top.
        /// </summary>
        public void AddLathe(Vector2[] profileRY, int radialSegments, Vector3 center, Quaternion rot,
            int submesh = 0, float aoMin = 0.86f, float aoMax = 1.0f, float uRepeat = 1f, float vRepeat = 1f,
            float startAngleDeg = 0f, float endAngleDeg = 360f, bool capCutEnds = false, int cutSubmesh = 1)
        {
            if (profileRY == null || profileRY.Length < 2) return;
            float a0 = startAngleDeg * Mathf.Deg2Rad;
            float a1 = endAngleDeg * Mathf.Deg2Rad;
            bool partial = Mathf.Abs(endAngleDeg - startAngleDeg) < 359.5f;

            // Compute arc length along profile for uniform V UVs
            float[] vCoord = new float[profileRY.Length];
            float totalLen = 0f;
            for (int p = 1; p < profileRY.Length; p++)
            {
                totalLen += Vector2.Distance(profileRY[p - 1], profileRY[p]);
                vCoord[p] = totalLen;
            }
            if (totalLen < 1e-4f) totalLen = 1f;
            for (int p = 0; p < profileRY.Length; p++) vCoord[p] = (vCoord[p] / totalLen) * vRepeat;

            Vector3 Pt(int pIdx, float ang)
            {
                float r = profileRY[pIdx].x;
                float y = profileRY[pIdx].y;
                return center + rot * new Vector3(Mathf.Cos(ang) * r, y, Mathf.Sin(ang) * r);
            }

            for (int p = 0; p < profileRY.Length - 1; p++)
            {
                float ao0 = Mathf.Lerp(aoMin, aoMax, p / (float)(profileRY.Length - 1));
                float ao1 = Mathf.Lerp(aoMin, aoMax, (p + 1) / (float)(profileRY.Length - 1));

                for (int s = 0; s < radialSegments; s++)
                {
                    float t0 = s / (float)radialSegments;
                    float t1 = (s + 1) / (float)radialSegments;
                    float ang0 = Mathf.Lerp(a0, a1, t0);
                    float ang1 = Mathf.Lerp(a0, a1, t1);

                    Vector3 p00 = Pt(p, ang0);
                    Vector3 p10 = Pt(p, ang1);
                    Vector3 p11 = Pt(p + 1, ang1);
                    Vector3 p01 = Pt(p + 1, ang0);

                    // Outward winding: verified earlier for (cos, y, sin) increasing angle -> (p00, p01, p11, p10) points radially OUTWARD!
                    Vector2 uv00 = new Vector2(t0 * uRepeat, vCoord[p]);
                    Vector2 uv10 = new Vector2(t1 * uRepeat, vCoord[p]);
                    Vector2 uv11 = new Vector2(t1 * uRepeat, vCoord[p + 1]);
                    Vector2 uv01 = new Vector2(t0 * uRepeat, vCoord[p + 1]);

                    AddTri(p00, p01, p11, uv00, uv01, uv11, submesh, ao0, ao1, ao1);
                    AddTri(p00, p11, p10, uv00, uv11, uv10, submesh, ao0, ao1, ao0);
                }
            }

            // Optional cutaway caps if partial lathe (supports solid profiles OR hollow ring profiles!)
            if (partial && capCutEnds)
            {
                // Detect closed hollow ring profile (profileRY[0] == profileRY[last] and all radii > 0)
                bool isHollowClosed = profileRY.Length >= 4 &&
                    Vector2.Distance(profileRY[0], profileRY[profileRY.Length - 1]) < 1e-4f;

                if (isHollowClosed)
                {
                    // Triangulate ONLY the 2D wall cross-section polygon in (r, y) plane!
                    var wallPoly = new List<Vector2>(profileRY.Length - 1);
                    for (int p = 0; p < profileRY.Length - 1; p++) wallPoly.Add(profileRY[p]);
                    if (SignedArea2D(wallPoly) < 0f) wallPoly.Reverse();
                    var capTris = EarClipTriangulate(wallPoly);

                    Vector3 CapPt(Vector2 ry, float ang) =>
                        center + rot * new Vector3(Mathf.Cos(ang) * ry.x, ry.y, Mathf.Sin(ang) * ry.x);

                    // Start cut face at a0 (outward normal points in -tangent(a0) direction)
                    Vector3 tang0 = rot * new Vector3(-Mathf.Sin(a0), 0f, Mathf.Cos(a0));
                    for (int t = 0; t < capTris.Count; t += 3)
                    {
                        Vector3 pa = CapPt(wallPoly[capTris[t]], a0);
                        Vector3 pb = CapPt(wallPoly[capTris[t + 1]], a0);
                        Vector3 pc = CapPt(wallPoly[capTris[t + 2]], a0);
                        if (Vector3.Dot(Vector3.Cross(pb - pa, pc - pa), -tang0) < 0f) { var tmp = pb; pb = pc; pc = tmp; }
                        AddTri(pa, pb, pc, wallPoly[capTris[t]] * 2f, wallPoly[capTris[t + 1]] * 2f, wallPoly[capTris[t + 2]] * 2f, cutSubmesh, 0.94f, 0.94f, 0.94f, true);
                    }

                    // End cut face at a1 (outward normal points in +tangent(a1) direction)
                    Vector3 tang1 = rot * new Vector3(-Mathf.Sin(a1), 0f, Mathf.Cos(a1));
                    for (int t = 0; t < capTris.Count; t += 3)
                    {
                        Vector3 pa = CapPt(wallPoly[capTris[t]], a1);
                        Vector3 pb = CapPt(wallPoly[capTris[t + 1]], a1);
                        Vector3 pc = CapPt(wallPoly[capTris[t + 2]], a1);
                        if (Vector3.Dot(Vector3.Cross(pb - pa, pc - pa), tang1) < 0f) { var tmp = pb; pb = pc; pc = tmp; }
                        AddTri(pa, pb, pc, wallPoly[capTris[t]] * 2f, wallPoly[capTris[t + 1]] * 2f, wallPoly[capTris[t + 2]] * 2f, cutSubmesh, 0.94f, 0.94f, 0.94f, true);
                    }
                }
                else
                {
                    for (int p = 0; p < profileRY.Length - 1; p++)
                    {
                        Vector3 c0 = center + rot * new Vector3(0, profileRY[p].y, 0);
                        Vector3 c1 = center + rot * new Vector3(0, profileRY[p + 1].y, 0);
                        Vector3 e0 = Pt(p, a0);
                        Vector3 e1 = Pt(p + 1, a0);
                        AddQuad(c0, e0, e1, c1, cutSubmesh, aoMin, 1f, 1f, aoMin, 1.5f);

                        Vector3 f0 = Pt(p, a1);
                        Vector3 f1 = Pt(p + 1, a1);
                        AddQuad(f0, c0, c1, f1, cutSubmesh, 1f, aoMin, aoMin, 1f, 1.5f);
                    }
                }
            }
        }

        /// <summary>
        /// Convenience: beveled cylinder along arbitrary axis (default Y).
        /// </summary>
        public void AddBeveledCylinder(Vector3 center, float radius, float height, int segments = 32,
            float bevel = 0.015f, Quaternion? rot = null, int submesh = 0, float uRepeat = 1f, float vRepeat = 1f)
        {
            float hh = height * 0.5f;
            float b = Mathf.Min(bevel, Mathf.Min(radius * 0.35f, hh * 0.4f));
            Vector2[] profile = new Vector2[]
            {
                new Vector2(0f, -hh),
                new Vector2(radius - b, -hh),
                new Vector2(radius, -hh + b),
                new Vector2(radius,  hh - b),
                new Vector2(radius - b,  hh),
                new Vector2(0f,  hh)
            };
            AddLathe(profile, segments, center, rot ?? Quaternion.identity, submesh, 0.90f, 1.0f, uRepeat, vRepeat);
        }

        /// <summary>
        /// Builds an iconic mechanical industrial drum (Main Cylinder / Doffer / Licker-in) aligned along Z axis:
        /// Outer beveled card-clothing drum (submesh 0) + recessed spoked structural end-hubs (submesh 1) + stepped steel shaft (submesh 2).
        /// </summary>
        public void AddIndustrialDrumZ(Vector3 center, float radius, float length, int radialSegments = 48,
            int spokeCount = 6, float shaftRadius = 0.08f, float shaftOverhang = 0.22f,
            int drumSubmesh = 0, int webSubmesh = 1, int shaftSubmesh = 2, bool toothedGrooves = false)
        {
            Quaternion axisZ = Quaternion.Euler(90f, 0f, 0f); // maps Lathe Y -> +Z
            float hl = length * 0.5f;
            float rimThick = Mathf.Clamp(radius * 0.14f, 0.04f, 0.13f);
            float recessDepth = Mathf.Clamp(length * 0.10f, 0.04f, 0.12f);
            float hubR = Mathf.Max(shaftRadius * 2.0f, radius * 0.24f);
            float bevel = Mathf.Min(0.018f, rimThick * 0.3f);

            // 1. Outer cylindrical card-clothing barrel shell (hollow recessed ends)
            var barrelProfile = new List<Vector2>();
            barrelProfile.Add(new Vector2(radius - rimThick, -hl + recessDepth));
            barrelProfile.Add(new Vector2(radius - rimThick, -hl + bevel));
            barrelProfile.Add(new Vector2(radius - rimThick + bevel, -hl));
            barrelProfile.Add(new Vector2(radius - bevel, -hl));
            barrelProfile.Add(new Vector2(radius, -hl + bevel));

            if (toothedGrooves)
            {
                // Distinct stylized saw-tooth ribs along barrel (for Licker-in 刺辊!)
                int ribs = 14;
                float span = length - bevel * 2f;
                for (int r = 0; r < ribs; r++)
                {
                    float y0 = -hl + bevel + (r / (float)ribs) * span;
                    float yMid = -hl + bevel + ((r + 0.5f) / (float)ribs) * span;
                    float y1 = -hl + bevel + ((r + 1f) / (float)ribs) * span;
                    barrelProfile.Add(new Vector2(radius - 0.014f, y0));
                    barrelProfile.Add(new Vector2(radius + 0.008f, yMid));
                    barrelProfile.Add(new Vector2(radius - 0.014f, y1));
                }
            }

            barrelProfile.Add(new Vector2(radius, hl - bevel));
            barrelProfile.Add(new Vector2(radius - bevel, hl));
            barrelProfile.Add(new Vector2(radius - rimThick + bevel, hl));
            barrelProfile.Add(new Vector2(radius - rimThick, hl - bevel));
            barrelProfile.Add(new Vector2(radius - rimThick, hl - recessDepth));

            AddLathe(barrelProfile.ToArray(), radialSegments, center, axisZ, drumSubmesh, 0.92f, 1.0f, 3f, 2f);

            // 2. Recessed inner cast-iron end plates (darker AO in recess!)
            AddBeveledCylinder(center, radius - rimThick + 0.004f, length - recessDepth * 2f,
                Mathf.Max(24, radialSegments / 2), 0.008f, axisZ, webSubmesh);

            // 3. Central structural boss + radial structural spokes on both ends (makes rotation visibly clear!)
            AddBeveledCylinder(center, hubR, length - recessDepth * 0.45f, 24, 0.012f, axisZ, webSubmesh);

            float spokeLen = (radius - rimThick) - hubR + 0.02f;
            float spokeW = Mathf.Clamp(radius * 0.13f, 0.03f, 0.09f);
            float spokeDepth = recessDepth * 0.72f;
            for (int side = -1; side <= 1; side += 2)
            {
                float zSpoke = side * (hl - recessDepth + spokeDepth * 0.45f);
                for (int k = 0; k < spokeCount; k++)
                {
                    float angDeg = k * (360f / spokeCount);
                    float ang = angDeg * Mathf.Deg2Rad;
                    float rMid = hubR + spokeLen * 0.5f - 0.01f;
                    Vector3 spPos = center + new Vector3(Mathf.Cos(ang) * rMid, Mathf.Sin(ang) * rMid, zSpoke);
                    AddBeveledBox(spPos, new Vector3(spokeLen, spokeW, spokeDepth), 0.008f,
                        Quaternion.Euler(0f, 0f, angDeg), webSubmesh, 0.80f, 0.96f);
                }

                // Decorative bolt heads around hub face (classic stylized engineering charm!)
                int bolts = Mathf.Max(4, spokeCount);
                float boltR = hubR * 0.72f;
                for (int bIdx = 0; bIdx < bolts; bIdx++)
                {
                    float ang = (bIdx + 0.5f) * (360f / bolts) * Mathf.Deg2Rad;
                    Vector3 bPos = center + new Vector3(Mathf.Cos(ang) * boltR, Mathf.Sin(ang) * boltR, side * (hl - recessDepth * 0.15f));
                    AddBeveledCylinder(bPos, Mathf.Clamp(radius * 0.035f, 0.01f, 0.024f), 0.022f, 6, 0.003f, axisZ, shaftSubmesh);
                }
            }

            // 4. Stepped machined central drive shaft
            AddBeveledCylinder(center, shaftRadius, length + shaftOverhang * 2f, 20, 0.01f, axisZ, shaftSubmesh);
        }

        /// <summary>
        /// Builds a notched industrial gear / sprocket aligned along Z axis.
        /// </summary>
        public void AddGearZ(Vector3 center, float pitchRadius, float thickness, int teeth,
            float toothHeight = 0.028f, float hubRadius = 0.06f, int submesh = 0)
        {
            int ptsCount = teeth * 4;
            var poly = new Vector2[ptsCount];
            float rRoot = pitchRadius - toothHeight * 0.45f;
            float rTip = pitchRadius + toothHeight * 0.55f;

            for (int i = 0; i < teeth; i++)
            {
                float baseAng = (i / (float)teeth) * Mathf.PI * 2f;
                float step = (Mathf.PI * 2f) / teeth;
                float a0 = baseAng;
                float a1 = baseAng + step * 0.18f;
                float a2 = baseAng + step * 0.50f;
                float a3 = baseAng + step * 0.68f;

                poly[i * 4 + 0] = new Vector2(Mathf.Cos(a0) * rRoot, Mathf.Sin(a0) * rRoot);
                poly[i * 4 + 1] = new Vector2(Mathf.Cos(a1) * rTip, Mathf.Sin(a1) * rTip);
                poly[i * 4 + 2] = new Vector2(Mathf.Cos(a2) * rTip, Mathf.Sin(a2) * rTip);
                poly[i * 4 + 3] = new Vector2(Mathf.Cos(a3) * rRoot, Mathf.Sin(a3) * rRoot);
            }

            Matrix4x4 M = Matrix4x4.Translate(center);
            AddExtrudedPolygonXY(poly, -thickness * 0.5f, thickness * 0.5f, submesh, submesh, submesh, 0.92f, 1.5f, M);

            if (hubRadius > 0.01f)
            {
                AddBeveledCylinder(center, hubRadius, thickness * 1.45f, 18, 0.008f, Quaternion.Euler(90f, 0f, 0f), submesh);
            }
        }

        /// <summary>
        /// Sweeps a smooth circular or elliptical tube along a 3D polyline (for suction ducts, V-belts, and coiled cotton sliver!).
        /// </summary>
        public void AddSweptTube(Vector3[] path, float[] radii, int radialSegments = 16,
            int submesh = 0, bool closedLoop = false, bool capEnds = true, float uvVScale = 1f, float ao = 1f)
        {
            if (path == null || path.Length < 2) return;
            int count = path.Length;

            // Compute parallel-transport frames (prevents twisting on 3D helices!)
            Vector3[] tangents = new Vector3[count];
            Vector3[] normals = new Vector3[count];
            Vector3[] binormals = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                Vector3 prev = closedLoop ? path[(i - 1 + count) % count] : path[Mathf.Max(0, i - 1)];
                Vector3 next = closedLoop ? path[(i + 1) % count] : path[Mathf.Min(count - 1, i + 1)];
                tangents[i] = (next - prev).normalized;
                if (tangents[i].sqrMagnitude < 1e-6f) tangents[i] = Vector3.up;
            }

            Vector3 refUp = Mathf.Abs(Vector3.Dot(tangents[0], Vector3.up)) > 0.92f ? Vector3.right : Vector3.up;
            normals[0] = Vector3.Cross(tangents[0], refUp).normalized;
            binormals[0] = Vector3.Cross(tangents[0], normals[0]).normalized;

            for (int i = 1; i < count; i++)
            {
                Quaternion dq = Quaternion.FromToRotation(tangents[i - 1], tangents[i]);
                normals[i] = (dq * normals[i - 1]).normalized;
                normals[i] = (normals[i] - tangents[i] * Vector3.Dot(normals[i], tangents[i])).normalized;
                binormals[i] = Vector3.Cross(tangents[i], normals[i]).normalized;
            }

            float[] accLen = new float[count];
            for (int i = 1; i < count; i++)
                accLen[i] = accLen[i - 1] + Vector3.Distance(path[i - 1], path[i]);

            int segLimit = closedLoop ? count : count - 1;
            for (int i = 0; i < segLimit; i++)
            {
                int iNext = (i + 1) % count;
                float r0 = radii[Mathf.Min(i, radii.Length - 1)];
                float r1 = radii[Mathf.Min(iNext, radii.Length - 1)];
                float v0 = accLen[i] * uvVScale;
                float v1 = (iNext == 0 ? accLen[count - 1] + Vector3.Distance(path[count - 1], path[0]) : accLen[iNext]) * uvVScale;

                for (int s = 0; s < radialSegments; s++)
                {
                    float u0 = s / (float)radialSegments;
                    float u1 = (s + 1) / (float)radialSegments;
                    float a0 = u0 * Mathf.PI * 2f;
                    float a1 = u1 * Mathf.PI * 2f;

                    Vector3 p00 = path[i] + (normals[i] * Mathf.Cos(a0) + binormals[i] * Mathf.Sin(a0)) * r0;
                    Vector3 p10 = path[i] + (normals[i] * Mathf.Cos(a1) + binormals[i] * Mathf.Sin(a1)) * r0;
                    Vector3 p11 = path[iNext] + (normals[iNext] * Mathf.Cos(a1) + binormals[iNext] * Mathf.Sin(a1)) * r1;
                    Vector3 p01 = path[iNext] + (normals[iNext] * Mathf.Cos(a0) + binormals[iNext] * Mathf.Sin(a0)) * r1;

                    // Verify outward normal: normal x binormal = -tangent or +tangent?
                    // binormal = tangent x normal -> so in right/left handed Unity:
                    // tangent = +Z, normal = +X => binormal = Cross(Z, X) = +Y.
                    // Going a0->a1 goes +X -> +Y (dA ≈ +Y), going i->iNext goes +Z.
                    // Cross(p01 - p00, p10 - p00) = Cross(+Z, +Y) = -X (inward)!
                    // Cross(p10 - p00, p01 - p00) = Cross(+Y, +Z) = +X (OUTWARD)!
                    AddQuadUV(p00, p10, p11, p01,
                        new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1),
                        submesh, ao);
                }
            }

            if (capEnds && !closedLoop)
            {
                // Start cap (normal -tangent[0])
                float rStart = radii[0];
                for (int s = 0; s < radialSegments; s++)
                {
                    float a0 = (s / (float)radialSegments) * Mathf.PI * 2f;
                    float a1 = ((s + 1) / (float)radialSegments) * Mathf.PI * 2f;
                    Vector3 p0 = path[0] + (normals[0] * Mathf.Cos(a0) + binormals[0] * Mathf.Sin(a0)) * rStart;
                    Vector3 p1 = path[0] + (normals[0] * Mathf.Cos(a1) + binormals[0] * Mathf.Sin(a1)) * rStart;
                    AddTri(path[0], p1, p0, Vector2.zero, Vector2.right, Vector2.up, submesh, ao, ao, ao);
                }
                // End cap (normal +tangent[last])
                int last = count - 1;
                float rEnd = radii[Mathf.Min(last, radii.Length - 1)];
                for (int s = 0; s < radialSegments; s++)
                {
                    float a0 = (s / (float)radialSegments) * Mathf.PI * 2f;
                    float a1 = ((s + 1) / (float)radialSegments) * Mathf.PI * 2f;
                    Vector3 p0 = path[last] + (normals[last] * Mathf.Cos(a0) + binormals[last] * Mathf.Sin(a0)) * rEnd;
                    Vector3 p1 = path[last] + (normals[last] * Mathf.Cos(a1) + binormals[last] * Mathf.Sin(a1)) * rEnd;
                    AddTri(path[last], p0, p1, Vector2.zero, Vector2.right, Vector2.up, submesh, ao, ao, ao);
                }
            }
        }

        /// <summary>
        /// Builds a thick ribbon / continuous carded cotton web sheet curving along a 2D XY path, spanning Z in [-halfWidth, +halfWidth].
        /// Width can taper smoothly along the path (wide web on doffer -> narrow funnel into condenser trumpet!).
        /// </summary>
        public void AddTaperedRibbonXY(Vector2[] pathXY, float[] halfWidthsZ, float thickness, int submesh = 0)
        {
            int count = pathXY.Length;
            if (count < 2) return;

            float[] accLen = new float[count];
            Vector2[] normals = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                if (i > 0) accLen[i] = accLen[i - 1] + Vector2.Distance(pathXY[i - 1], pathXY[i]);
                Vector2 prev = pathXY[Mathf.Max(0, i - 1)];
                Vector2 next = pathXY[Mathf.Min(count - 1, i + 1)];
                Vector2 t = (next - prev).normalized;
                normals[i] = new Vector2(-t.y, t.x); // upward/left normal in XY
            }

            float ht = thickness * 0.5f;
            for (int i = 0; i < count - 1; i++)
            {
                float w0 = halfWidthsZ[Mathf.Min(i, halfWidthsZ.Length - 1)];
                float w1 = halfWidthsZ[Mathf.Min(i + 1, halfWidthsZ.Length - 1)];
                float v0 = accLen[i] * 1.4f;
                float v1 = accLen[i + 1] * 1.4f;

                Vector2 t0 = pathXY[i] + normals[i] * ht;
                Vector2 b0 = pathXY[i] - normals[i] * ht;
                Vector2 t1 = pathXY[i + 1] + normals[i + 1] * ht;
                Vector2 b1 = pathXY[i + 1] - normals[i + 1] * ht;

                Vector3 T0L = new Vector3(t0.x, t0.y, -w0);
                Vector3 T0R = new Vector3(t0.x, t0.y,  w0);
                Vector3 T1L = new Vector3(t1.x, t1.y, -w1);
                Vector3 T1R = new Vector3(t1.x, t1.y,  w1);

                Vector3 B0L = new Vector3(b0.x, b0.y, -w0);
                Vector3 B0R = new Vector3(b0.x, b0.y,  w0);
                Vector3 B1L = new Vector3(b1.x, b1.y, -w1);
                Vector3 B1R = new Vector3(b1.x, b1.y,  w1);

                // Top sheet (+normal)
                AddQuadUV(T0L, T0R, T1R, T1L,
                    new Vector2(0, v0), new Vector2(1, v0), new Vector2(1, v1), new Vector2(0, v1), submesh, 1.0f);
                // Bottom sheet (-normal)
                AddQuadUV(B0R, B0L, B1L, B1R,
                    new Vector2(1, v0), new Vector2(0, v0), new Vector2(0, v1), new Vector2(1, v1), submesh, 0.85f);
                // Front edge (+Z)
                AddQuad(B0R, B1R, T1R, T0R, submesh, 0.92f, 0.92f, 1f, 1f, 1.5f);
                // Back edge (-Z)
                AddQuad(T0L, T1L, B1L, B0L, submesh, 1f, 1f, 0.92f, 0.92f, 1.5f);
            }
        }

        /// <summary>
        /// Builds a soft sculpted stack of organic billowing cotton batt fleece folds
        /// (continuous smooth superellipsoid cushions — zero discrete bead spheres!).
        /// </summary>
        public void AddFluffyCottonSlab(Vector3 center, Vector3 size, int submesh = 0)
        {
            int layers = 6;
            float layerH = size.y / layers;
            float bottomY = center.y - size.y * 0.5f;

            for (int l = 0; l < layers; l++)
            {
                float cy = bottomY + (l + 0.5f) * layerH;
                float phase = l * 1.37f;
                Vector3 c = new Vector3(
                    center.x + Mathf.Sin(phase) * 0.008f,
                    cy,
                    center.z + Mathf.Cos(phase * 1.1f) * 0.006f);
                Vector3 radii = new Vector3(
                    size.x * (0.50f + 0.025f * Mathf.Sin(phase * 1.9f)),
                    layerH * 0.62f,
                    size.z * (0.49f + 0.015f * Mathf.Cos(phase * 1.4f)));

                AddOrganicPillow(c, radii, phase, submesh);
            }
        }

        public void AddOrganicPillow(Vector3 center, Vector3 radii, float seed, int submesh)
        {
            int segU = 22;
            int segV = 10;

            Vector3 Eval(float u, float v)
            {
                float theta = u * Mathf.PI * 2f; // around Y
                float phi = (v - 0.5f) * Mathf.PI; // -pi/2 .. +pi/2

                float cp = Mathf.Cos(phi);
                float sp = Mathf.Sin(phi);
                float ct = Mathf.Cos(theta);
                float st = Mathf.Sin(theta);

                // Superellipsoid exponent (p = 0.55 gives soft boxy cushion that fills the chute snugly!)
                float sx = Mathf.Sign(ct * cp) * Mathf.Pow(Mathf.Abs(ct * cp) + 1e-5f, 0.58f);
                float sy = Mathf.Sign(sp) * Mathf.Pow(Mathf.Abs(sp) + 1e-5f, 0.65f);
                float sz = Mathf.Sign(st * cp) * Mathf.Pow(Mathf.Abs(st * cp) + 1e-5f, 0.58f);

                // Gentle organic fleece undulation (billowing soft tufts without sharp creases)
                float puff = 1f
                    + 0.055f * Mathf.Sin(theta * 3f + seed) * cp
                    + 0.035f * Mathf.Cos(theta * 5f - phi * 2f + seed * 1.7f) * cp;

                return center + new Vector3(sx * radii.x * puff, sy * radii.y * puff, sz * radii.z * puff);
            }

            for (int iv = 0; iv < segV; iv++)
            {
                float v0 = iv / (float)segV;
                float v1 = (iv + 1) / (float)segV;
                float ao0 = Mathf.Lerp(0.84f, 1.0f, v0);
                float ao1 = Mathf.Lerp(0.84f, 1.0f, v1);

                for (int iu = 0; iu < segU; iu++)
                {
                    float u0 = iu / (float)segU;
                    float u1 = (iu + 1) / (float)segU;

                    Vector3 p00 = Eval(u0, v0);
                    Vector3 p10 = Eval(u1, v0);
                    Vector3 p11 = Eval(u1, v1);
                    Vector3 p01 = Eval(u0, v1);

                    // Verified outward winding for (cos(theta), sin(phi), sin(theta)): (p00, p01, p11, p10) points OUTWARD!
                    AddTri(p00, p01, p11, new Vector2(u0 * 2f, v0), new Vector2(u0 * 2f, v1), new Vector2(u1 * 2f, v1), submesh, ao0, ao1, ao1);
                    AddTri(p00, p11, p10, new Vector2(u0 * 2f, v0), new Vector2(u1 * 2f, v1), new Vector2(u1 * 2f, v0), submesh, ao0, ao1, ao0);
                }
            }
        }

        public void AddIcosphere(Vector3 center, Vector3 radii, int subdivisions = 1, int submesh = 0, float ao = 1f)
        {
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var verts = new List<Vector3>
            {
                new Vector3(-1,  t, 0).normalized, new Vector3( 1,  t, 0).normalized,
                new Vector3(-1, -t, 0).normalized, new Vector3( 1, -t, 0).normalized,
                new Vector3( 0, -1,  t).normalized, new Vector3( 0,  1,  t).normalized,
                new Vector3( 0, -1, -t).normalized, new Vector3( 0,  1, -t).normalized,
                new Vector3( t,  0, -1).normalized, new Vector3( t,  0,  1).normalized,
                new Vector3(-t,  0, -1).normalized, new Vector3(-t,  0,  1).normalized
            };

            var faces = new List<(int a, int b, int c)>
            {
                (0,11,5),(0,5,1),(0,1,7),(0,7,10),(0,10,11),
                (1,5,9),(5,11,4),(11,10,2),(10,7,6),(7,1,8),
                (3,9,4),(3,4,2),(3,2,6),(3,6,8),(3,8,9),
                (4,9,5),(2,4,11),(6,2,10),(8,6,7),(9,8,1)
            };

            for (int s = 0; s < subdivisions; s++)
            {
                var nextFaces = new List<(int, int, int)>(faces.Count * 4);
                var cache = new Dictionary<long, int>();
                int Mid(int i1, int i2)
                {
                    long key = i1 < i2 ? ((long)i1 << 32) | (uint)i2 : ((long)i2 << 32) | (uint)i1;
                    if (cache.TryGetValue(key, out int idx)) return idx;
                    idx = verts.Count;
                    verts.Add(((verts[i1] + verts[i2]) * 0.5f).normalized);
                    cache[key] = idx;
                    return idx;
                }
                foreach (var (a, b, c) in faces)
                {
                    int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                    nextFaces.Add((a, ab, ca));
                    nextFaces.Add((b, bc, ab));
                    nextFaces.Add((c, ca, bc));
                    nextFaces.Add((ab, bc, ca));
                }
                faces = nextFaces;
            }

            foreach (var (a, b, c) in faces)
            {
                Vector3 pa = center + Vector3.Scale(verts[a], radii);
                Vector3 pb = center + Vector3.Scale(verts[b], radii);
                Vector3 pc = center + Vector3.Scale(verts[c], radii);
                // Ensure outward winding relative to center
                Vector3 n = Vector3.Cross(pb - pa, pc - pa);
                Vector3 radial = ((pa + pb + pc) / 3f) - center;
                if (Vector3.Dot(n, radial) < 0f)
                {
                    var tmp = pb; pb = pc; pc = tmp;
                }
                AddTri(pa, pb, pc,
                    ProjectBoxUV(pa, radial, 1.5f),
                    ProjectBoxUV(pb, radial, 1.5f),
                    ProjectBoxUV(pc, radial, 1.5f), submesh, ao, ao, ao);
            }
        }

        /// <summary>
        /// Compiles triangles into a clean Unity Mesh with automatic crease-angle normal splitting:
        /// smooth curved surfaces stay silky smooth, while sharp mechanical edges > creaseAngleDeg
        /// split cleanly so URP DepthNormals draws razor-sharp TrainAR ink crease lines!
        /// </summary>
        public Mesh BuildMesh(string name, float creaseAngleDeg = 38f)
        {
            float cosThresh = Mathf.Cos(creaseAngleDeg * Mathf.Deg2Rad);
            int triCount = _tris.Count;

            // Compute face normals
            Vector3[] faceNormals = new Vector3[triCount];
            for (int i = 0; i < triCount; i++)
            {
                var t = _tris[i];
                faceNormals[i] = Vector3.Cross(t.p1 - t.p0, t.p2 - t.p0).normalized;
            }

            // Spatial bucket for vertex welding & crease-aware normal smoothing
            const float quant = 2000f; // 0.5mm precision
            var posBuckets = new Dictionary<Vector3Int, List<(int triIdx, int corner)>>(triCount);

            Vector3Int Key(Vector3 p) => new Vector3Int(
                Mathf.RoundToInt(p.x * quant),
                Mathf.RoundToInt(p.y * quant),
                Mathf.RoundToInt(p.z * quant));

            for (int i = 0; i < triCount; i++)
            {
                var t = _tris[i];
                AddBucket(posBuckets, Key(t.p0), i, 0);
                AddBucket(posBuckets, Key(t.p1), i, 1);
                AddBucket(posBuckets, Key(t.p2), i, 2);
            }

            var outPos = new List<Vector3>(triCount * 3);
            var outNrm = new List<Vector3>(triCount * 3);
            var outUv = new List<Vector2>(triCount * 3);
            var outCol = new List<Color>(triCount * 3);
            var subIndices = new List<int>[_submeshCount];
            for (int s = 0; s < _submeshCount; s++) subIndices[s] = new List<int>();

            for (int i = 0; i < triCount; i++)
            {
                var t = _tris[i];
                Vector3 fn = faceNormals[i];
                Vector3[] ps = { t.p0, t.p1, t.p2 };
                Vector2[] uvs = { t.uv0, t.uv1, t.uv2 };
                Color[] cols = { t.c0, t.c1, t.c2 };

                for (int c = 0; c < 3; c++)
                {
                    Vector3 nSum = fn;
                    if (!t.forceFlat)
                    {
                        var list = posBuckets[Key(ps[c])];
                        for (int k = 0; k < list.Count; k++)
                        {
                            int otherTri = list[k].triIdx;
                            if (otherTri == i) continue;
                            if (_tris[otherTri].forceFlat) continue;
                            Vector3 on = faceNormals[otherTri];
                            if (Vector3.Dot(fn, on) >= cosThresh)
                                nSum += on;
                        }
                    }

                    int vIdx = outPos.Count;
                    outPos.Add(ps[c]);
                    outNrm.Add(nSum.normalized);
                    outUv.Add(uvs[c]);
                    outCol.Add(cols[c]);
                    subIndices[t.submesh].Add(vIdx);
                }
            }

            var mesh = new Mesh { name = name };
            if (outPos.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.SetVertices(outPos);
            mesh.SetNormals(outNrm);
            mesh.SetUVs(0, outUv);
            mesh.SetColors(outCol);
            mesh.subMeshCount = _submeshCount;
            for (int s = 0; s < _submeshCount; s++)
                mesh.SetTriangles(subIndices[s], s);

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        private static void AddBucket(Dictionary<Vector3Int, List<(int, int)>> dict, Vector3Int key, int tri, int corner)
        {
            if (!dict.TryGetValue(key, out var list))
            {
                list = new List<(int, int)>(8);
                dict[key] = list;
            }
            list.Add((tri, corner));
        }

        private static float SignedArea2D(List<Vector2> pts)
        {
            float a = 0f;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector2 p0 = pts[i];
                Vector2 p1 = pts[(i + 1) % pts.Count];
                a += (p0.x * p1.y - p1.x * p0.y);
            }
            return a * 0.5f;
        }

        private static List<int> EarClipTriangulate(List<Vector2> pts)
        {
            var indices = new List<int>();
            var avail = new List<int>(pts.Count);
            for (int i = 0; i < pts.Count; i++) avail.Add(i);

            int guard = 0;
            while (avail.Count > 3 && guard < 10000)
            {
                guard++;
                bool earFound = false;
                for (int i = 0; i < avail.Count; i++)
                {
                    int prev = avail[(i - 1 + avail.Count) % avail.Count];
                    int curr = avail[i];
                    int next = avail[(i + 1) % avail.Count];

                    Vector2 a = pts[prev], b = pts[curr], c = pts[next];
                    if (Cross2D(b - a, c - a) <= 1e-6f) continue; // reflex vertex

                    bool inside = false;
                    for (int j = 0; j < avail.Count; j++)
                    {
                        int vi = avail[j];
                        if (vi == prev || vi == curr || vi == next) continue;
                        if (PointInTri2D(pts[vi], a, b, c)) { inside = true; break; }
                    }
                    if (!inside)
                    {
                        indices.Add(prev);
                        indices.Add(curr);
                        indices.Add(next);
                        avail.RemoveAt(i);
                        earFound = true;
                        break;
                    }
                }
                if (!earFound) break;
            }
            if (avail.Count == 3)
            {
                indices.Add(avail[0]);
                indices.Add(avail[1]);
                indices.Add(avail[2]);
            }
            return indices;
        }

        private static float Cross2D(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        private static bool PointInTri2D(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float c1 = Cross2D(b - a, p - a);
            float c2 = Cross2D(c - b, p - b);
            float c3 = Cross2D(a - c, p - c);
            return c1 >= -1e-6f && c2 >= -1e-6f && c3 >= -1e-6f;
        }
    }
}
