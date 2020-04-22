using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SettingsOpener : MonoBehaviour
{
    public GameObject menuPanel;

    public void OpenPanel()
    {
        if (menuPanel != null)
        {
            Animator animator = menuPanel.GetComponent<Animator>();
            if (animator != null)
            {
                bool isOpen = animator.GetBool("open");

                animator.SetBool("open", !isOpen);
            }
        }
    }
}
