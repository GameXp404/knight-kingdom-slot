using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIDebugDumper : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(DumpAfterDelay());
    }

    IEnumerator DumpAfterDelay()
    {
        yield return new WaitForSeconds(0.5f);

        var sb = new StringBuilder();
        sb.AppendLine("=== UI Element Dump ===");
        sb.AppendLine($"Screen: {Screen.width}x{Screen.height}");
        sb.AppendLine($"Time: {System.DateTime.Now}");
        sb.AppendLine();
        sb.AppendLine("--- Top-Left Area (left 40% width, top 30% height) ---");

        var allRT = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        foreach (var rt in allRT)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            float left = corners[0].x;
            float top = corners[1].y;
            float right = corners[2].x;
            float bottom = corners[0].y;

            bool inTopLeft = (left < Screen.width * 0.45f) && (top > Screen.height * 0.65f);
            if (!inTopLeft) continue;

            string text = "";
            var tmp = rt.GetComponent<TextMeshProUGUI>();
            if (tmp != null && !string.IsNullOrEmpty(tmp.text)) text = $" TEXT='{tmp.text.Replace("\n", "\\n")}'";

            string image = "";
            var img = rt.GetComponent<Image>();
            if (img != null && img.sprite != null) image = $" SPRITE='{img.sprite.name}'";
            var raw = rt.GetComponent<RawImage>();
            if (raw != null && raw.texture != null) image = $" TEXTURE='{raw.texture.name}'";

            bool active = rt.gameObject.activeInHierarchy;
            float alpha = 1f;
            var cg = rt.GetComponent<CanvasGroup>();
            if (cg != null) alpha = cg.alpha;
            if (img != null) alpha *= img.color.a;
            if (tmp != null) alpha *= tmp.color.a;

            string parent = rt.parent != null ? rt.parent.name : "<root>";
            sb.AppendLine($"[{rt.name}] parent={parent} active={active} alpha={alpha:0.00} screen=({left:0},{top:0})-({right:0},{bottom:0}) size=({rt.sizeDelta.x:0}x{rt.sizeDelta.y:0}){text}{image}");
            count++;
        }

        sb.AppendLine();
        sb.AppendLine($"Found {count} elements in top-left area.");

        string path = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop), "ui_dump.txt");
        System.IO.File.WriteAllText(path, sb.ToString());
        Debug.Log($"[UIDumper] Saved to: {path}");
    }
}
