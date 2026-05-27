using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UILineRenderer : MaskableGraphic
{
    [SerializeField] private List<Vector2> points = new List<Vector2>();
    public float thickness = 8f;
    public bool drawDots = true;
    public float dotRadiusMultiplier = 1.6f;

    public void SetPoints(List<Vector2> pts)
    {
        points = pts;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (points == null || points.Count < 2) return;

        float halfT = thickness * 0.5f;
        var vert = UIVertex.simpleVert;
        vert.color = color;

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector2 a = points[i];
            Vector2 b = points[i + 1];
            Vector2 dir = (b - a).sqrMagnitude < 0.0001f ? Vector2.right : (b - a).normalized;
            Vector2 nrm = new Vector2(-dir.y, dir.x) * halfT;

            int baseIdx = vh.currentVertCount;
            vert.position = a - nrm; vh.AddVert(vert);
            vert.position = a + nrm; vh.AddVert(vert);
            vert.position = b + nrm; vh.AddVert(vert);
            vert.position = b - nrm; vh.AddVert(vert);

            vh.AddTriangle(baseIdx + 0, baseIdx + 1, baseIdx + 2);
            vh.AddTriangle(baseIdx + 0, baseIdx + 2, baseIdx + 3);
        }

        if (drawDots)
        {
            float r = halfT * dotRadiusMultiplier;
            for (int i = 0; i < points.Count; i++)
                AddDot(vh, points[i], r, vert);
        }
    }

    private static void AddDot(VertexHelper vh, Vector2 center, float radius, UIVertex template)
    {
        const int segments = 18;
        int baseIdx = vh.currentVertCount;
        var v = template;
        v.position = center; vh.AddVert(v);

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments * Mathf.PI * 2f;
            v.position = new Vector3(center.x + Mathf.Cos(t) * radius, center.y + Mathf.Sin(t) * radius, 0f);
            vh.AddVert(v);
        }

        for (int i = 0; i < segments; i++)
            vh.AddTriangle(baseIdx, baseIdx + 1 + i, baseIdx + 2 + i);
    }
}
