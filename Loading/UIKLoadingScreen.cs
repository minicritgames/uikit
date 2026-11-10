using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UIKit
{
    public class UIKLoadingScreen : UIKCurtain
    {
        [SerializeField] private TMP_Text statusText;
        // Shown only while something loads, so a plain cover is just the curtain
        [SerializeField] private List<GameObject> loadingContent = new();


        public void SetStatus(string _status)
        {
            if (statusText)
            {
                statusText.text = _status ?? string.Empty;
            }
        }

        public void SetLoadingContentShown(bool _shown)
        {
            foreach (GameObject content in loadingContent)
            {
                if (content)
                {
                    content.SetActive(_shown);
                }
            }
        }
    }
} // UIKit namespace
