using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SilkSong.UserInterface
{
    /// <summary>
    /// GUI rendering for the Balance tab.
    /// Handles damage multiplier system interface.
    /// </summary>
    public class BalanceTabGUI
    {
        private Vector2 balanceScrollPosition = Vector2.zero;

        /// <summary>
        /// Renders the Balance tab content.
        /// </summary>
        /// <param name="context">Shared GUI context with state and dependencies</param>
        public void Render(GuiContext context)
        {
            // Begin scroll view
            balanceScrollPosition = GUILayout.BeginScrollView(balanceScrollPosition, 
                GUILayout.Width(380), 
                GUILayout.Height(context.WindowRect.height - 120));

            RenderHeader();
            RenderMultiplierControls(context);
            RenderStatusDisplay(context);

            GUILayout.EndScrollView();
        }

        private void RenderHeader()
        {
            GUILayout.Label("Damage Balance System", GUI.skin.box);
            GUILayout.Label("Adjust damage multiplier for easier gameplay without cheats feel", GUI.skin.label);
            GUILayout.Space(10);
        }

        private void RenderMultiplierControls(GuiContext context)
        {
            // Global multiplier controls
            GUILayout.BeginHorizontal();
            GUILayout.Label("Global Multiplier:", GUILayout.Width(120));
            context.GlobalMultiplierText = GUILayout.TextField(context.GlobalMultiplierText, GUILayout.Width(60));
            if (GUILayout.Button("Apply", GUILayout.Width(60)))
            {
                if (float.TryParse(context.GlobalMultiplierText, out float multiplier))
                {
                    context.BalanceService.ApplyGlobalMultiplier(multiplier, 
                        msg => context.ToastSystem.ShowToast(msg),
                        msg => context.ToastSystem.ShowToast(msg));
                    context.GlobalMultiplier = multiplier;
                }
                else
                {
                    context.ToastSystem.ShowToast("Invalid multiplier value!");
                }
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Label("Examples: 1.0 = Normal, 1.5 = +50% damage, 2.0 = Double damage", GUI.skin.label);
            GUILayout.Label("Note: Values < 1.0 can cause invincibility bugs", GUI.skin.label);
            GUILayout.Space(15);
        }

        private void RenderStatusDisplay(GuiContext context)
        {
            // Show current status (what users care about)
            if (context.BalanceService.FieldsScanned && context.BalanceService.DamageFieldCount > 0)
            {
                if (Math.Abs(context.GlobalMultiplier - 1.0f) > 0.01f)
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label($"Currently applied: {context.GlobalMultiplier:F2}x damage multiplier", GUI.skin.box);
                    GUI.color = Color.white;
                }
                else
                {
                    GUILayout.Label("No multiplier currently applied (normal damage)", GUI.skin.label);
                }
            }
            else if (context.BalanceService.FieldsScanned)
            {
                GUILayout.Label("No damage fields found - try changing scenes", GUI.skin.label);
            }
            else
            {
                GUILayout.Label("Click 'Refresh' to scan for damage fields", GUI.skin.label);
            }

            GUILayout.Space(15);
        }

    }
}
