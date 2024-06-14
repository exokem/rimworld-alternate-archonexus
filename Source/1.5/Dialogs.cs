using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RimWorld;
using Verse.Sound;

using Verse;

namespace AlternateArchonexus
{
    public class Dialog_ChooseSoldSettlement : Window
    {
        private Action<Map> postSelected;
        private Action cancel;

        private Map selectedMap;

        private Vector2 scrollPosition;

        public bool isFinal = false;

        private AcceptanceReport AcceptanceReport 
        {
            get 
            {
                if (selectedMap == null)
                    return AcceptanceReport.WasRejected;
                
                else if (!ValidateSelection())
                    return $"Insufficient wealth in selected settlement: {selectedMap.wealthWatcher.WealthTotal}";
                
                else return AcceptanceReport.WasAccepted;
            }
        }

        public Dialog_ChooseSoldSettlement(Action<Map> postSelected, Action cancel = null)
        {
            if (ModLister.CheckIdeology("Choose new colony"))
            {
                this.postSelected = postSelected;
                this.cancel = cancel;

                forcePause = true;
                closeOnCancel = isFinal;
                absorbInputAroundWindow = true;
                forceCatchAcceptAndCancelEventEvenIfUnfocused = true;
                preventSave = true;
            }
        }

        public override void PostOpen()
        {
            selectedMap = null;
        }

        public override void DoWindowContents(Rect rect)
        {
            float w = rect.width;

            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0F, 0F, w, 40F), "Settlement Selection");
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0F, 40F, w, 25F), "Designate which settlement should be sold.");
            
            List<Map> playerMaps = new List<Map>();

            foreach (Map map in Find.Maps)
            {
                if (map.IsPlayerHome)
                    playerMaps.Add(map);
            }

            Rect outRect = new Rect(rect.x, rect.y + 40F + 25F + 10F, w, rect.height - 40F - 25F - 70F);
            float scrollHeight = 40F + (float) playerMaps.Count * 40F;

            if (outRect.height < scrollHeight)
                scrollHeight -= 16F;

            Rect scrollArea = new Rect(0F, 0F, w, scrollHeight);

            Widgets.BeginScrollView(outRect, ref scrollPosition, scrollArea);
            
            if (0 < playerMaps.Count)
                DrawMapList(playerMaps, scrollArea);

            Widgets.EndScrollView();

            if (selectedMap == null && 0 < playerMaps.Count)
                selectedMap = playerMaps[0];

            float buttonWidth = 160F;
            float buttonHeight = 40F;

            Rect buttonArea = new Rect(0f, rect.yMax - 70f, rect.width, 70f);
		    Rect acceptButtonArea = new Rect(buttonArea.xMax - buttonWidth, buttonArea.yMax - buttonHeight, buttonWidth, buttonHeight);

            if (Widgets.ButtonText(acceptButtonArea, "AcceptButton".Translate()))
            {
                if (AcceptanceReport.Accepted && selectedMap != null)
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation($"{selectedMap.Parent.Label} will be sold, and its contents will become inaccessible. Anything within other settlements or caravans will be untouched.", FinalizeDecision, false, "ConfirmDecisionsTitle".Translate()));
                }
                else
                {
                    Messages.Message(AcceptanceReport.Reason, MessageTypeDefOf.RejectInput, historical: false);
                }
            }

            // Hide cancel button for the final part so you can't just cancel to find the archonexus for free
            if (!isFinal)
            {
                Rect cancelButtonArea = acceptButtonArea;
                cancelButtonArea.x = buttonArea.x;
                if (Widgets.ButtonText(cancelButtonArea, "Cancel".Translate()))
                {
                    if (cancel != null)
                    {
                        cancel();
                    }
                    Close();
                }
            }
        }

        private bool ValidateSelection()
        {
            Log.Message($"Wealth of selected settlement: {selectedMap.wealthWatcher.WealthTotal}");
            return QuestLimits.WealthRequired <= selectedMap.wealthWatcher.WealthTotal;
        }

        private void FinalizeDecision()
        {
            Close();
            postSelected(selectedMap);
        }

        private void DrawMapList(List<Map> maps, Rect rect)
        {
            Text.Anchor = TextAnchor.UpperLeft;
            int count = maps.Count;
            float startY = rect.y + 30f;
            for (int i = 0; i < count; i++)
            {
                Rect rowArea = new Rect(rect.x, startY + (float) i * 30f, rect.width, 30f);
                Map map = maps[i];

                if (i % 2 == 1)
                    Widgets.DrawLightHighlight(rowArea);

                GUI.BeginGroup(rowArea);

                // Display hover effects
                Rect tooltipArea = new Rect(0F, 0F, 80F + 196F, rowArea.height);
                if (Mouse.IsOver(tooltipArea))
                {
                    Widgets.DrawHighlight(tooltipArea);
                }

                // Add info button
                Widgets.InfoCardButton(0F, 0F, map.Parent);

                // Add settlement label
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.WordWrap = false;
                Widgets.Label(new Rect(40F, 0F, 196F, rowArea.height), map.Parent.Label);
                Text.WordWrap = true;

                // Add highlighting for button area
                Rect buttonLargeArea = new Rect(tooltipArea.x + tooltipArea.width, 0F, rowArea.width - tooltipArea.width, tooltipArea.height);
                if (Mouse.IsOver(buttonLargeArea))
                    Widgets.DrawHighlight(buttonLargeArea);

                // Add radio button
                float dim = rowArea.height;
                Rect buttonArea = new Rect(rowArea.width - dim, 3F, dim, dim);
                if (Widgets.RadioButton(buttonArea.x, buttonArea.y, map == selectedMap))
                {
                    selectedMap = map;
                    SoundDefOf.Checkbox_TurnedOn.PlayOneShotOnCamera();
                }

                GenUI.ResetLabelAlign();
                GUI.EndGroup();
            }
        }
    }
}
