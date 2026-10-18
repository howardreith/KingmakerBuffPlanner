using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KingmakerBuffPlanner.UI
{
    internal static class PlannerWorkspaceTextProbe
    {
        internal static void Sample(Transform root, string phase, Action<string> log)
        {
            if (root == null) return;
            string[] names = { "Title", "BuffName", "Name", "LaneTitle", "Headline", "SaveStatus", "Result", "SourceDetail", "ResourcesTitle", "Text", "Label", "Capacity" };
            foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true)
                .Where(t => names.Contains(t.name)).GroupBy(t => t.name).Select(g => g.First()))
            {
                try
                {
                    RectTransform r = label.rectTransform;
                    Material m = label.fontSharedMaterial;
                    Canvas canvas = label.canvas;
                    if (label.gameObject.activeInHierarchy) label.ForceMeshUpdate();
                    log("[KBP-WORKSPACE-TEXT] phase=" + phase + ";name=" + label.name + ";text=" + label.text +
                        ";active=" + label.gameObject.activeInHierarchy + ";enabled=" + label.enabled + ";alpha=" + label.alpha +
                        ";rect=" + r.rect + ";anchors=" + r.anchorMin + "/" + r.anchorMax + ";pivot=" + r.pivot +
                        ";scale=" + r.lossyScale + ";margin=" + label.margin + ";autoContainer=" + label.autoSizeTextContainer +
                        ";culled=" + label.canvasRenderer.cull + ";rendererAlpha=" + label.canvasRenderer.GetAlpha() +
                        ";glyphs=" + (label.textInfo == null ? 0 : label.textInfo.characterCount) +
                        ";preferred=" + label.preferredWidth + "/" + label.preferredHeight + ";bounds=" + label.textBounds +
                        ";overflow=" + label.overflowMode + ";font=" + (label.font == null ? "none" : label.font.name) +
                        ";material=" + (m == null ? "none" : m.name) + ";atlas=" + (m == null || m.mainTexture == null ? 0 : m.mainTexture.GetInstanceID()) +
                        ";clip=" + (m != null && m.HasProperty("_ClipRect") ? m.GetVector("_ClipRect").ToString() : "none") +
                        ";stencil=" + (m != null && m.HasProperty("_StencilComp") ? m.GetFloat("_StencilComp").ToString() : "none") +
                        ";keywords=" + (m == null ? "none" : string.Join(",", m.shaderKeywords)) +
                        ";mask=" + (label.GetComponentInParent<Mask>() != null) + ";rectMask=" + (label.GetComponentInParent<RectMask2D>() != null) +
                        ";layout=" + r.GetComponents<Component>().OfType<ILayoutController>().Count() +
                        ";canvas=" + (canvas == null ? "none" : canvas.renderMode + "/" + canvas.scaleFactor + "/" + canvas.sortingOrder) +
                        ";screen=" + Screen.width + "x" + Screen.height);
                }
                catch (Exception exception) { log("[KBP-WORKSPACE-TEXT] phase=" + phase + ";unavailable=" + exception.GetType().Name); }
            }
        }
    }

    internal sealed class PlannerWorkspaceTextFrameProbe : MonoBehaviour
    {
        internal void Arm(Transform root, Action<string> log, string reason)
        {
            StopAllCoroutines(); StartCoroutine(Capture(root, log, reason));
        }
        private IEnumerator Capture(Transform root, Action<string> log, string reason)
        {
            yield return new WaitForEndOfFrame();
            PlannerWorkspaceTextProbe.Sample(root, reason + ":first-frame-end", log);
            yield return new WaitForEndOfFrame();
            PlannerWorkspaceTextProbe.Sample(root, reason + ":settled-frame-end", log);
        }
    }
}
