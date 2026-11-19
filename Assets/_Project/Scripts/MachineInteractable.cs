using System.Collections.Generic;
using UnityEngine;

namespace CNC.DigitalTwin
{
    /// <summary>
    /// Marks a workshop machine/station as clickable & highlightable with a Fresnel rim shader overlay.
    /// </summary>
    public class MachineInteractable : MonoBehaviour
    {
        public int stationIndex;
        public string displayName;
        public Transform highlightRoot;

        private readonly List<GameObject> _overlayObjects = new List<GameObject>();
        private bool _overlaysBuilt;
        private bool _isHovered;
        private bool _isSelected;
        private Material _hoverMat;
        private Material _selectMat;

        public void SetHighlightMaterials(Material hoverMat, Material selectMat)
        {
            _hoverMat = hoverMat;
            _selectMat = selectMat;
            RefreshVisual();
        }

        public void SetHovered(bool hovered)
        {
            if (_isHovered == hovered) return;
            _isHovered = hovered;
            RefreshVisual();
        }

        public void SetSelected(bool selected)
        {
            if (_isSelected == selected) return;
            _isSelected = selected;
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            bool show = _isHovered || _isSelected;
            if (!show)
            {
                for (int i = 0; i < _overlayObjects.Count; i++)
                    if (_overlayObjects[i] != null) _overlayObjects[i].SetActive(false);
                return;
            }

            if (!_overlaysBuilt) BuildOverlays();

            Material mat = _isSelected ? _selectMat : _hoverMat;
            for (int i = 0; i < _overlayObjects.Count; i++)
            {
                if (_overlayObjects[i] == null) continue;
                _overlayObjects[i].SetActive(true);
                if (mat != null)
                {
                    var mr = _overlayObjects[i].GetComponent<MeshRenderer>();
                    if (mr != null) mr.sharedMaterial = mat;
                }
            }
        }

        private void BuildOverlays()
        {
            _overlaysBuilt = true;
            Transform root = highlightRoot != null ? highlightRoot : transform;
            var filters = root.GetComponentsInChildren<MeshFilter>(false);

            // Cap overlay meshes per machine to keep highlight ultra-fast even on complex assemblies
            int count = 0;
            foreach (var mf in filters)
            {
                if (count >= 24) break;
                if (mf == null || mf.sharedMesh == null) continue;
                var srcMr = mf.GetComponent<MeshRenderer>();
                if (srcMr == null || !srcMr.enabled) continue;
                string gname = mf.gameObject.name;
                if (gname.Contains("__HighlightOverlay") || gname.Contains("Beacon") || gname.Contains("Plane")) continue;
                // Exclude thin floor trigger pads so they never render as solid cyan squares
                if (srcMr.bounds.size.y < 0.35f) continue;

                var ov = new GameObject("__HighlightOverlay");
                ov.transform.SetParent(mf.transform, false);
                ov.transform.localPosition = Vector3.zero;
                ov.transform.localRotation = Quaternion.identity;
                ov.transform.localScale = Vector3.one * 1.003f;
                ov.layer = LayerMask.NameToLayer("Ignore Raycast");

                var omf = ov.AddComponent<MeshFilter>();
                omf.sharedMesh = mf.sharedMesh;
                var omr = ov.AddComponent<MeshRenderer>();
                omr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                omr.receiveShadows = false;
                omr.sharedMaterial = _selectMat != null ? _selectMat : _hoverMat;
                ov.SetActive(false);
                _overlayObjects.Add(ov);
                count++;
            }
        }
    }
}
