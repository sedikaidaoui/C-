using AotForms.Components;
using ImGuiNET;
using System;
using System.IO;
using System.Numerics;
using System.Reflection;
using static AotForms.Config;

namespace AotForms
{
    internal partial class ESP
    {
        private static void GetTwoColumnLayout(out float colW, out float spacing, out float innerW)
        {
            GetSingleColumnLayout(out colW, out innerW);
            spacing = 0f;
        }

        private static void GetSingleColumnLayout(out float colW, out float innerW)
        {
            float avail = ImGui.GetContentRegionAvail().X;
            colW = MathF.Max(120f, avail);
            innerW = MathF.Max(100f, colW - 20f);
        }

        private void DrawCard(string title, Vector2 size, Action drawContent)
        {
            bool dummy = true;
            DrawCard(title, "", "", size, ref dummy, drawContent, showToggle: false);
        }

        private void DrawCard(string title, string description, string faIcon, Vector2 size, ref bool cardEnabled, Action drawContent, bool showToggle = true)
        {
            float headerH = string.IsNullOrEmpty(description) ? 30f : 42f;
            const float padX = 10f;
            const float bodyPadY = 8f;

            var draw = ImGui.GetWindowDrawList();
            Vector2 originalCursor = ImGui.GetCursorPos();
            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 bodyOrigin = pos + new Vector2(padX, headerH + bodyPadY);

            draw.ChannelsSplit(2);
            draw.ChannelsSetCurrent(1);

            ImGui.SetCursorScreenPos(bodyOrigin);
            ImGui.PushID(title);
            ImGui.BeginGroup();
            drawContent();
            ImGui.EndGroup();
            ImGui.PopID();

            float contentH = MathF.Max(0f, ImGui.GetItemRectMax().Y - bodyOrigin.Y);
            float totalH = MathF.Max(size.Y, headerH + contentH + bodyPadY * 2f);
            Vector2 cardSize = new Vector2(size.X, totalH);
            Vector2 endPos = pos + cardSize;

            draw.ChannelsSetCurrent(0);
            
            // Dark solid card color matching the screenshot
            uint bodyBg = ImGui.GetColorU32(new Vector4(12f / 255f, 13f / 255f, 18f / 255f, _uiAlpha));
            uint headerBg = ImGui.GetColorU32(new Vector4(16f / 255f, 17f / 255f, 24f / 255f, _uiAlpha));
            uint cardBorder = ImGui.GetColorU32(new Vector4(24f / 255f, 25f / 255f, 31f / 255f, _uiAlpha));

            draw.AddRectFilled(pos, endPos, bodyBg, 6f);
            draw.AddRectFilled(pos, pos + new Vector2(cardSize.X, headerH), headerBg, 6f, ImDrawFlags.RoundCornersTop);
            draw.AddRect(pos, endPos, cardBorder, 6f, ImDrawFlags.None, 1f);

            // Draw Icon on the left (blue circular badge)
            float textOffsetLeft = padX;
            if (!string.IsNullOrEmpty(faIcon) && TextFont.IsLoaded())
            {
                ImGui.PushFont(TextFont);
                Vector2 iconSz = ImGui.CalcTextSize(faIcon);
                float badgeR = MathF.Max(iconSz.X, iconSz.Y) * 0.55f + 4f;
                float iconY = pos.Y + (headerH - iconSz.Y) * 0.5f;
                Vector2 badgeCenter = new Vector2(pos.X + padX + badgeR, pos.Y + headerH * 0.5f);
                draw.AddCircleFilled(
                    badgeCenter,
                    badgeR,
                    ImGui.GetColorU32(new Vector4(ESP.ThemeColor.X, ESP.ThemeColor.Y, ESP.ThemeColor.Z, 0.18f * _uiAlpha)));
                draw.AddText(
                    new Vector2(badgeCenter.X - iconSz.X * 0.5f, iconY),
                    ImGui.GetColorU32(new Vector4(ESP.ThemeColor.X, ESP.ThemeColor.Y, ESP.ThemeColor.Z, _uiAlpha)),
                    faIcon);
                ImGui.PopFont();
                textOffsetLeft += badgeR * 2f + 8f;
            }

            // Draw Title and Description
            if (LebelFont.IsLoaded())
                ImGui.PushFont(LebelFont);
            
            Vector2 titleSz = ImGui.CalcTextSize(title);
            if (string.IsNullOrEmpty(description))
            {
                float titleY = pos.Y + (headerH - titleSz.Y) * 0.5f;
                draw.AddText(new Vector2(pos.X + textOffsetLeft, titleY), ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.96f * _uiAlpha)), title);
            }
            else
            {
                float titleY = pos.Y + 4f;
                draw.AddText(new Vector2(pos.X + textOffsetLeft, titleY), ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.96f * _uiAlpha)), title);
                
                // Draw Description
                if (TextFont.IsLoaded()) ImGui.PushFont(TextFont);
                Vector2 descSz = ImGui.CalcTextSize(description);
                float descY = pos.Y + headerH - descSz.Y - 5f;
                draw.AddText(new Vector2(pos.X + textOffsetLeft, descY), ImGui.GetColorU32(new Vector4(0.55f, 0.55f, 0.58f, 0.85f * _uiAlpha)), description);
                if (TextFont.IsLoaded()) ImGui.PopFont();
            }

            if (LebelFont.IsLoaded())
                ImGui.PopFont();

            // Draw Toggle switch on the right side of the header
            if (showToggle)
            {
                float toggleW = 30f;
                float toggleH = 16f;
                Vector2 togglePos = new Vector2(endPos.X - toggleW - 12f, pos.Y + (headerH - toggleH) * 0.5f);
                ImGui.SetCursorScreenPos(togglePos);
                CustomToggle.CaxCustomToggle($"##toggle_{title}", ref cardEnabled);
            }

            draw.ChannelsMerge();

            ImGui.SetCursorPos(originalCursor);
            ImGui.Dummy(cardSize);
        }

        private static string FormatPremiumSliderValue(float value, float max)
        {
            if (max >= 1000f)
                return ((int)MathF.Round(value)).ToString();
            if (MathF.Abs(value - MathF.Round(value)) < 0.05f)
                return ((int)MathF.Round(value)).ToString();
            return value.ToString("0.#");
        }

        private bool DrawPremiumSlider(string label, ref float value, float min, float max, float width)
        {
            const float labelRowH = 17f;
            const float labelToBarGap = 6f;
            const float barH = 10f;
            const float hitPadY = 6f;
            const float bottomGap = 6f;

            var draw = ImGui.GetWindowDrawList();
            Vector2 blockStart = ImGui.GetCursorScreenPos();

            if (TextFont.IsLoaded())
                ImGui.PushFont(TextFont);

            string valStr = FormatPremiumSliderValue(value, max);
            Vector2 labelSz = ImGui.CalcTextSize(label);
            Vector2 valSz = ImGui.CalcTextSize(valStr);

            Vector4 labelMuted = new(0.78f, 0.78f, 0.82f, _uiAlpha);
            Vector4 valueBright = new(0.95f, 0.95f, 0.97f, _uiAlpha);
            draw.AddText(blockStart, ImGui.GetColorU32(labelMuted), label);
            draw.AddText(
                blockStart + new Vector2(width - valSz.X, 0f),
                ImGui.GetColorU32(valueBright),
                valStr);

            if (TextFont.IsLoaded())
                ImGui.PopFont();

            Vector2 barMin = blockStart + new Vector2(0f, labelRowH + labelToBarGap);
            Vector2 barMax = barMin + new Vector2(width, barH);
            float barRound = barH * 0.5f;

            ImGui.SetCursorScreenPos(barMin - new Vector2(0f, hitPadY));
            ImGui.InvisibleButton($"##prem_slider_{label}", new Vector2(width, barH + hitPadY * 2f));

            bool isChanged = false;
            if (ImGui.IsItemActive() || ImGui.IsItemClicked())
            {
                float mouseX = ImGui.GetMousePos().X;
                float pct = Math.Clamp((mouseX - barMin.X) / width, 0f, 1f);
                float newVal = min + pct * (max - min);
                if (MathF.Abs(newVal - value) > 0.01f)
                {
                    value = newVal;
                    isChanged = true;
                }
            }

            uint trackBg = ImGui.GetColorU32(new Vector4(0.04f, 0.04f, 0.06f, 0.90f * _uiAlpha));
            uint trackBorder = ImGui.GetColorU32(new Vector4(0f, 229f / 255f, 1f, 0.15f * _uiAlpha));
            draw.AddRectFilled(barMin, barMax, trackBg, barRound);
            draw.AddRect(barMin, barMax, trackBorder, barRound, ImDrawFlags.None, 1f);

            float t = (max > min) ? Math.Clamp((value - min) / (max - min), 0f, 1f) : 0f;
            float filledW = width * t;
            if (filledW > 0.5f)
            {
                Vector2 fillMax = barMin + new Vector2(filledW, barH);
                Vector4 fill = new(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 0.98f * _uiAlpha);
                draw.AddRectFilled(barMin, fillMax, ImGui.GetColorU32(fill), barRound);

                float highlightH = MathF.Max(2f, barH * 0.35f);
                Vector2 hiMin = barMin + new Vector2(1f, 1f);
                Vector2 hiMax = fillMax - new Vector2(1f, barH - highlightH - 1f);
                if (hiMax.X > hiMin.X + 2f)
                {
                    draw.AddRectFilled(
                        hiMin, hiMax,
                        ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 0.12f * _uiAlpha)),
                        barRound);
                }
            }

            float blockH = labelRowH + labelToBarGap + barH + bottomGap;
            ImGui.SetCursorScreenPos(blockStart + new Vector2(0f, blockH));
            ImGui.Dummy(new Vector2(width, 0f));
            return isChanged;
        }

        private static int _espSubTab = 0; // 0 = ESP/Eye display, 1 = ESP Settings

        private void DrawEspSubTab(int idx, string label, float w)
        {
            const float h = 34f;
            bool active = _espSubTab == idx;
            var dl = ImGui.GetWindowDrawList();
            Vector2 sp = ImGui.GetCursorScreenPos();
            ImGui.InvisibleButton("##esp_sub_" + idx, new Vector2(w, h));
            if (ImGui.IsItemClicked()) _espSubTab = idx;
            bool hov = ImGui.IsItemHovered();
            uint bg = active
                ? ImGui.GetColorU32(new Vector4(14f / 255f, 15f / 255f, 21f / 255f, _uiAlpha))
                : ImGui.GetColorU32(new Vector4(1f, 1f, 1f, (hov ? 0.04f : 0.02f) * _uiAlpha));
            dl.AddRectFilled(sp, sp + new Vector2(w, h), bg, 4f);
            if (active)
                dl.AddRectFilled(sp, sp + new Vector2(w, 2.5f),
                    ImGui.GetColorU32(new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 0.95f * _uiAlpha)), 1.5f);
            if (TextFont.IsLoaded()) ImGui.PushFont(TextFont);
            Vector2 tsz = ImGui.CalcTextSize(label);
            Vector2 tp = new Vector2(sp.X + (w - tsz.X) * 0.5f, sp.Y + (h - tsz.Y) * 0.5f);
            Vector4 col = active ? new Vector4(1f, 1f, 1f, _uiAlpha) : new Vector4(0.55f, 0.55f, 0.58f, _uiAlpha);
            dl.AddText(tp, ImGui.GetColorU32(col), label);
            if (TextFont.IsLoaded()) ImGui.PopFont();
        }

        void RenderVisualsTab()
        {
            GetSingleColumnLayout(out float colW, out float innerW);
            BeginFeatureScrollArea("esp");
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4f, 5f));

            float subW = (innerW - 12f) / 3f;
            DrawEspSubTab(0, "\uf06e ESP", subW);
            ImGui.SameLine(0f, 6f);
            DrawEspSubTab(1, "\uf013 Settings", subW);
            ImGui.SameLine(0f, 6f);
            DrawEspSubTab(2, "\uf140 Weapon", subW);
            ImGui.Dummy(new Vector2(0f, 2f));

            if (_espSubTab == 0)
                RenderEspDisplayTab(colW, innerW);
            else if (_espSubTab == 1)
                RenderEspSettingsTab(colW, innerW);
            else
                RenderEspWeaponTab(colW, innerW);

            ImGui.PopStyleVar();
            EndFeatureScrollArea();
        }

        // ESP / Eye: display toggles + their colors only. No keybind pickers here.
        private void RenderEspDisplayTab(float colW, float innerW)
        {
            DrawCard("ESP Line", new Vector2(colW, 1f), () =>
            {
                float rowX = ImGui.GetCursorPosX();
                FeatureCheckboxStackRow(rowX, innerW, "ESP Line", ref Config.ESPLine);
                DrawColorRow("Line", Config.ESPLineColor, v => Config.ESPLineColor = v);
                string[] lineStyles = { "Straight", "Curved", "Zigzag" };
                int lsi = Math.Clamp(Config.ESPLineStyle, 0, 2);
                if (ImGui.Combo("Line design", ref lsi, lineStyles, lineStyles.Length))
                    Config.ESPLineStyle = Math.Clamp(lsi, 0, 2);
                string[] lineEnds = { "Head", "Body", "Feet" };
                int lei = Math.Clamp(Config.ESPLineEnd, 0, 2);
                if (ImGui.Combo("Line target", ref lei, lineEnds, lineEnds.Length))
                    Config.ESPLineEnd = Math.Clamp(lei, 0, 2);
            });

            DrawCard("ESP Box", new Vector2(colW, 1f), () =>
            {
                float rowX = ImGui.GetCursorPosX();
                FeatureCheckboxStackRow(rowX, innerW, "ESP Box", ref Config.ESPBox);
                DrawColorRow("Box", Config.ESPBoxColor, v => Config.ESPBoxColor = v);
                string[] boxStyles = { "Cornered", "Full 2D", "3D Box" };
                int bsi = Math.Clamp(Config.ESPBoxStyle, 0, 2);
                if (ImGui.Combo("Box design", ref bsi, boxStyles, boxStyles.Length))
                    Config.ESPBoxStyle = Math.Clamp(bsi, 0, 2);
            });

            DrawCard("ESP Name", new Vector2(colW, 1f), () =>
            {
                float rowX = ImGui.GetCursorPosX();
                FeatureCheckboxStackRow(rowX, innerW, "ESP Name", ref Config.ESPName);
                FeatureCheckboxStackRow(rowX, innerW, "Esp Distance", ref Config.espdistance);
                DrawColorRow("Name", Config.ESPNameColor, v => Config.ESPNameColor = v);
                ImGui.SliderFloat("Name size", ref Config.ESPNameSize, 0.5f, 3f, "%.1fx");
            });

            DrawCard("ESP Health", new Vector2(colW, 1f), () =>
            {
                float rowX = ImGui.GetCursorPosX();
                FeatureCheckboxStackRow(rowX, innerW, "Health Bar", ref Config.ESPHealth);
                FeatureCheckboxStackRow(rowX, innerW, "Health Text", ref Config.ESPHealthText);
                DrawColorRow("Health", Config.ESPHealthColor, v => Config.ESPHealthColor = v);
            });

            DrawCard("ESP Weapon", new Vector2(colW, 1f), () =>
            {
                float rowX = ImGui.GetCursorPosX();
                FeatureCheckboxStackRow(rowX, innerW, "Show Weapon", ref Config.espweapon);
                DrawColorRow("Weapon", System.Drawing.Color.FromArgb(
                    (int)(Config.ICONCOLOR.W * 255), (int)(Config.ICONCOLOR.X * 255),
                    (int)(Config.ICONCOLOR.Y * 255), (int)(Config.ICONCOLOR.Z * 255)),
                    v => Config.ICONCOLOR = new Vector4(v.R / 255f, v.G / 255f, v.B / 255f, v.A / 255f));
            });

            DrawCard("ESP Skeleton", new Vector2(colW, 1f), () =>
            {
                float rowX = ImGui.GetCursorPosX();
                FeatureCheckboxStackRow(rowX, innerW, "Skeleton (lines only)", ref Config.ESPSkeleton);
                DrawColorRow("Skeleton", Config.ESPSkeletonColor, v => Config.ESPSkeletonColor = v);
            });
        }

        // ESP Settings: every detailed ESP option (positions, sizes, styles). No keybind pickers here.
        private void RenderEspSettingsTab(float colW, float innerW)
        {
            DrawCard("Box Style", new Vector2(colW, 1f), () =>
            {
                float rowX = ImGui.GetCursorPosX();
                FeatureCheckboxStackRow(rowX, innerW, "Full Box (2D)", ref Config.ESPFullBox);
                FeatureCheckboxStackRow(rowX, innerW, "Fill Box", ref Config.ESPFillBox);
                CustomCheckbox.CaxCustomCheckbox("Esp RGB", ref Config.ESPRGB);
                ImGui.SliderFloat("Thickness", ref Config.ESPThickness, 1f, 5f, "%.1f");
                DrawColorRow("Fill", Config.ESPFillBoxColor, v => Config.ESPFillBoxColor = v);
            });

            DrawCard("Positions", new Vector2(colW, 1f), () =>
            {
                string[] boxPos = { "Top Center", "Bottom Center", "Top Left", "Top Right", "Bottom Left", "Bottom Right", "Middle Left", "Middle Right" };
                int ni = (int)Config.EspNameAnchor;
                if (ImGui.Combo("Name pos", ref ni, boxPos, boxPos.Length) && ni >= 0 && ni < 8)
                    Config.EspNameAnchor = (EspNameBoxAnchor)ni;
                string[] sidePos = { "Right", "Left", "Top", "Bottom" };
                int hi = (int)Config.EspHealthAnchor;
                if (ImGui.Combo("Health bar pos", ref hi, sidePos, sidePos.Length) && hi >= 0 && hi < 4)
                    Config.EspHealthAnchor = (EspHealthBoxAnchor)hi;
                int wi = (int)Config.EspWeaponAnchor;
                if (ImGui.Combo("Weapon pos", ref wi, boxPos, boxPos.Length) && wi >= 0 && wi < 8)
                    Config.EspWeaponAnchor = (EspWeaponBoxAnchor)wi;
                string[] linePos = { "Top", "Center", "Bottom" };
                int li = (int)Config.ESPLinePosition;
                if (ImGui.Combo("Snap line from", ref li, linePos, linePos.Length) && li >= 0 && li < 3)
                    Config.ESPLinePosition = (LinePosition)li;
            });

            DrawCard("Name / Information", new Vector2(colW, 1f), () =>
            {
                float rowX = ImGui.GetCursorPosX();
                FeatureCheckboxStackRow(rowX, innerW, "Esp Information plate", ref Config.ESPInformation);
            });

            DrawCard("Weapon Details", new Vector2(colW, 1f), () =>
            {
                float rowX = ImGui.GetCursorPosX();
                FeatureCheckboxStackRow(rowX, innerW, "Player Weapon Icon", ref Config.ESPWeaponIcon);
                FeatureCheckboxStackRow(rowX, innerW, "Player Weapon Text", ref Config.ESPWeaponName);
                ImGui.SliderFloat("Icon size", ref Config.ESPWeaponIconSize, 0.5f, 3f, "%.1fx");
                ImGui.SliderFloat("Icon scale", ref Config.iconsize, 0.3f, 2f, "%.2f");
            });

            DrawCard("Skeleton / Extras", new Vector2(colW, 1f), () =>
            {
                float rowX = ImGui.GetCursorPosX();
                FeatureCheckboxStackRow(rowX, innerW, "Bunny-ears marker", ref Config.ESPBunnyEars);
                FeatureCheckboxStackRow(rowX, innerW, "Radar", ref Config.ESPRadar);
                FeatureCheckboxStackRow(rowX, innerW, "Aim Track Line", ref Config.AimTrackLine);
                FeatureCheckboxStackRow(rowX, innerW, "Detect Hacker", ref Config.HackerTag);
                ImGui.SliderFloat("Radar size", ref Config.ESPRadarSize, 80f, 400f, "%.0f");
                ImGui.SliderFloat("Radar range", ref Config.ESPRadarRange, 20f, 400f, "%.0fm");
                ImGui.TextUnformatted("Move radar: keyboard arrows");
            });

            DrawCard("ESP Misc", new Vector2(colW, 1f), () =>
            {
                float rowX = ImGui.GetCursorPosX();
                FeatureCheckboxStackRow(rowX, innerW, "Refresh ESP", ref Config.RefreshESP);
                CustomCheckbox.CaxCustomCheckbox("Esp Editor", ref Config.ESPEditor);
                CustomCheckbox.CaxCustomCheckbox("Show Dragon Logo", ref Config.EspShowDragonLogo);
                CustomCheckbox.CaxCustomCheckbox("Count Valid Match (180s)", ref Config.EspTimer);
            });
        }

        // ESP WEAPON sub-tab: ground-loot weapons only (never held guns).
        private void RenderEspWeaponTab(float colW, float innerW)
        {
            DrawCard("Loot Weapons ESP", new Vector2(colW, 1f), () =>
            {
                float rowX = ImGui.GetCursorPosX();
                FeatureCheckboxStackRow(rowX, innerW, "Loot Weapons ESP (200m)", ref Config.LootWeaponsESP);
                FeatureCheckboxStackRow(rowX, innerW, "Loot Icon", ref Config.LootShowIcon);
                FeatureCheckboxStackRow(rowX, innerW, "Loot Name", ref Config.LootShowName);
                FeatureCheckboxStackRow(rowX, innerW, "Loot Distance", ref Config.LootShowDist);
                FeatureCheckboxStackRow(rowX, innerW, "Loot Line", ref Config.LootShowLine);
                FeatureCheckboxStackRow(rowX, innerW, "Loot Box", ref Config.LootShowBox);
            });

            DrawCard("Weapons (check + color each)", new Vector2(colW, 1f), () =>
            {
                float rowX = ImGui.GetCursorPosX();
                foreach (var w in WeaponEspConfig.List())
                {
                    bool en = WeaponEspConfig.IsEnabled(w.key);
                    bool cur = en;
                    FeatureCheckboxStackRow(rowX, innerW, w.pretty, ref cur);
                    if (cur != en) WeaponEspConfig.SetEnabled(w.key, cur);
                    DrawColorRow(w.pretty, WeaponEspConfig.GetColor(w.key), v => WeaponEspConfig.SetColor(w.key, v));
                }
            });
        }

        void RenderConfigTab() { }

        private void SaveSingleConfigFromUi()
        {
            if (Config.SaveSingleConfig(out string error))
                CustomNotification.Notify("Config", "Configuration saved.");
            else
                CustomNotification.Notify("Config", $"Save failed: {error}");
        }

        private void LoadSingleConfigFromUi()
        {
            if (Config.LoadSingleConfig(out string error))
            {
                ApplyLoadedFeatureRuntimeState();
                CustomNotification.Notify("Config", "Configuration loaded.");
            }
            else
                CustomNotification.Notify("Config", $"Load failed: {error}");
        }

        private void DeleteSingleConfigFromUi()
        {
            if (Config.DeleteSingleConfig(out string error))
                CustomNotification.Notify("Config", "Configuration deleted.");
            else
                CustomNotification.Notify("Config", $"Delete failed: {error}");
        }

        // Interactive ESP editor figure (Rouk-style soldier): drag it, drawing follows.
        private static Vector2 _espFigOffset = Vector2.Zero;
        private static float _espFigScale = 1f;
        private static float _espEditorCanvasH = 340f;
        internal static float EspEditorWinW = 340f;

                private static IntPtr _bunnyTex = IntPtr.Zero;
        private static uint _bunnyTexW, _bunnyTexH;
        private static bool _bunnyTexTried;
        private static Vector2 _bunnyOffEditor = Vector2.Zero;
                private static float _bunnyScale = 1f;
        private static float _bunnyYawDeg;
        private static float _bunnyYawCur;

        private void EnsureBunnyTex()
        {
            if (_bunnyTex != IntPtr.Zero || _bunnyTexTried) return;
            _bunnyTexTried = true;
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                Stream? s = null;
                foreach (var n in asm.GetManifestResourceNames())
                {
                    if (n.EndsWith("Resources.Editor.bunny444.png", StringComparison.OrdinalIgnoreCase)) { s = asm.GetManifestResourceStream(n); break; }
                }
                if (s == null) return;
                using (s)
                using (var bmp = new System.Drawing.Bitmap(s))
                {
                    string tmp = TempImages.PathFor("bunny_preview");
                    bmp.Save(tmp);
                    AddOrGetImagePointer(tmp, true, out var ptr, out var w, out var h);
                    if (ptr != IntPtr.Zero) { _bunnyTex = ptr; _bunnyTexW = w; _bunnyTexH = h; }
                }
            }
            catch { }
        }

        private void DrawBunnyFigureCanvas(string id, ref Vector2 off, Vector2 size)
        {
            EnsureBunnyTex();
            ImGui.TextWrapped("Drag to move - turn with yaw - ESP follows the bunny.");
            ImGui.SetNextItemWidth(200f);
            ImGui.SliderFloat("Bunny size", ref _bunnyScale, 0.4f, 2f, "%.2fx");
            ImGui.SetNextItemWidth(200f);
            ImGui.SliderFloat("Turn (yaw)", ref _bunnyYawDeg, -60f, 60f, "%.0f");
            if (ImGui.Button("Reset bunny")) { off = Vector2.Zero; _bunnyYawDeg = 0f; _bunnyYawCur = 0f; }
            ImGui.SetNextItemAllowOverlap();
            ImGui.InvisibleButton(id, size);
            if (ImGui.IsItemActive()) off += ImGui.GetIO().MouseDelta;
            Vector2 cMin = ImGui.GetItemRectMin();
            var dl = ImGui.GetWindowDrawList();
            dl.AddRectFilled(cMin, cMin + size, ImGui.GetColorU32(new Vector4(0.05f, 0.05f, 0.07f, 1f)), 6f);
            dl.AddRect(cMin, cMin + size, ImGui.GetColorU32(new Vector4(0.2f, 0.2f, 0.25f, 1f)), 6f);
            if (_bunnyTex == IntPtr.Zero || _bunnyTexW == 0)
            {
                dl.AddText(cMin + new Vector2(8, 30), 0xFFFFFFFF, "bunny444.png missing - rebuild project");
                return;
            }
            float sc = Math.Clamp(_bunnyScale, 0.4f, 2f);
            float yawTarget = Math.Clamp(_bunnyYawDeg, -60f, 60f);
            float dt = Math.Max(0.0001f, ImGui.GetIO().DeltaTime);
            _bunnyYawCur += (yawTarget - _bunnyYawCur) * Math.Min(1f, dt * 8f);
            float yaw = _bunnyYawCur * MathF.PI / 180f;
            float aspect = (float)_bunnyTexH / Math.Max(1f, (float)_bunnyTexW);
            float wdt = Math.Max(26f, 130f * sc * MathF.Abs(MathF.Cos(yaw)));
            float hgt = wdt * aspect;
            float maxH = size.Y - 84f;
            if (hgt > maxH && maxH > 60f) { hgt = maxH; wdt = hgt / aspect; }
            float cx = Math.Clamp(cMin.X + size.X * 0.5f + off.X, cMin.X + wdt * 0.5f + 4f, cMin.X + size.X - wdt * 0.5f - 4f);
            float cy = Math.Clamp(cMin.Y + size.Y * 0.5f + off.Y, cMin.Y + hgt * 0.5f + 22f, cMin.Y + size.Y - hgt * 0.5f - 40f);
            off = new Vector2(cx - (cMin.X + size.X * 0.5f), cy - (cMin.Y + size.Y * 0.5f));
            Vector2 mn = new Vector2(cx - wdt / 2f, cy - hgt / 2f), mx = new Vector2(cx + wdt / 2f, cy + hgt / 2f);
            dl.AddImage(_bunnyTex, mn, mx);
            uint boxCol = ColorToUint32(Config.ESPBoxColor);
            uint dark = 0xFF000000;
            if (Config.ESPLine)
                dl.AddLine(new Vector2(cMin.X + size.X / 2f, cMin.Y + 4f), new Vector2(cx, mn.Y), ColorToUint32(Config.ESPLineColor), 1.5f);
            if (Config.ESPFillBox)
                dl.AddRectFilled(mn, mx, ColorToUint32(Config.ESPFillBoxColor));
            if (Config.ESPFullBox)
                dl.AddRect(mn, mx, boxCol, 0f, ImDrawFlags.None, 1.5f);
            if (Config.ESPBox)
                DrawCorneredBoxDrawList(dl, mn.X, mn.Y, wdt, hgt, boxCol, 1.5f);
            if (Config.ESPName)
            {
                string nm = "Bunny 444";
                Vector2 nsz = ImGui.CalcTextSize(nm);
                Vector2 np = new Vector2(cx - nsz.X / 2f, mn.Y - nsz.Y - 4f);
                dl.AddText(np + new Vector2(1, 1), dark, nm);
                dl.AddText(np, ColorToUint32(Config.ESPNameColor), nm);
            }
            if (Config.espdistance)
            {
                Vector2 dp = new Vector2(cx - 12f, mx.Y + 4f);
                dl.AddText(dp + new Vector2(1, 1), dark, "25m");
                dl.AddText(dp, 0xFFFFFF00, "25m");
            }
            if (Config.ESPHealth)
            {
                float hpF = 0.7f + 0.25f * MathF.Sin((float)ImGui.GetTime());
                HealthGradientColors(hpF, false, out uint hCol, out uint hDark);
                Vector2 hp0 = new Vector2(mx.X + 5f, mn.Y);
                dl.AddRectFilled(hp0, hp0 + new Vector2(5f, hgt), 0x90000000);
                dl.AddRectFilled(new Vector2(hp0.X, hp0.Y + hgt * (1f - hpF)), hp0 + new Vector2(5f, hgt), hCol);
            }
            if (Config.espweapon && (Config.ESPWeaponIcon || Config.ESPWeaponName))
            {
                string wn = "AK47";
                Vector2 wp = new Vector2(mn.X, mx.Y + 4f);
                Vector2 isz = new Vector2(52f, 18f);
                dl.AddText(new Vector2(wp.X + 8f, wp.Y - 14f), 0xFF00FF00, "200");
                if (WeaponCatalog.TryGetIcon(this, wn, out var tex) && tex != IntPtr.Zero && Config.ESPWeaponIcon)
                {
                    dl.AddImage(tex, wp, wp + isz);
                    if (Config.ESPWeaponName)
                        dl.AddText(new Vector2(wp.X + isz.X + 4f, wp.Y + 2f), ColorToUint32(Config.ESPNameColor), wn);
                }
                else if (Config.ESPWeaponName || Config.ESPWeaponIcon)
                    dl.AddText(wp, ColorToUint32(Config.ESPNameColor), wn);
            }
        }

        private void DrawEspEditorPreviewPanel()
        {
            ImGui.SetNextItemWidth(220f);
            ImGui.SliderFloat("Editor height", ref _espEditorCanvasH, 220f, 600f, "%.0f");
            ImGui.SetNextItemWidth(220f);
            ImGui.SliderFloat("Editor width", ref EspEditorWinW, 280f, 640f, "%.0f");
            Vector2 avail = ImGui.GetContentRegionAvail();
            DrawBunnyFigureCanvas("##bunny_editor", ref _bunnyOffEditor, new Vector2(Math.Max(200f, avail.X), Math.Clamp(_espEditorCanvasH, 220f, 600f)));
            ImGui.Dummy(new Vector2(1, 4));
        }


        
        
        void EnsureEspEditorPreview()
        {
            if (_espEditorPreviewPtr != IntPtr.Zero)
                return;
            if (_espEditorPreviewLoadAttempted)
                return;
            _espEditorPreviewLoadAttempted = true;
            const string embeddedName = "pixel.Resources.Icons.esp_editor_preview.png";
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                Stream? stream = asm.GetManifestResourceStream(embeddedName);
                if (stream == null)
                {
                    foreach (string name in asm.GetManifestResourceNames())
                    {
                        if (name.EndsWith("Resources.Icons.esp_editor_preview.png", StringComparison.OrdinalIgnoreCase))
                        {
                            stream = asm.GetManifestResourceStream(name);
                            if (stream != null) break;
                        }
                    }
                }
                if (stream == null)
                {
                    string? dir = Path.GetDirectoryName(asm.Location);
                    if (dir != null)
                    {
                        string[] candidates =
                        {
                            Path.Combine(dir, "Resources", "Icons", "esp_editor_preview.png"),
                            Path.Combine(AppContext.BaseDirectory, "Resources", "Icons", "esp_editor_preview.png"),
                        };
                        foreach (string p in candidates)
                        {
                            if (!File.Exists(p)) continue;
                            stream = File.OpenRead(p);
                            break;
                        }
                    }
                }
                if (stream == null) return;
                using (stream)
                using (var bitmap = new System.Drawing.Bitmap(stream))
                {
                    string tempPath = TempImages.PathFor("esp_editor");
                    bitmap.Save(tempPath);
                    AddOrGetImagePointer(tempPath, true, out var ptr, out var w, out var h);
                    if (ptr != IntPtr.Zero)
                    {
                        _espEditorPreviewPtr = ptr;
                        _espEditorPreviewW = w;
                        _espEditorPreviewH = h;
                    }
                }
            }
            catch { }
        }

        private static void DrawColorRow(string label, System.Drawing.Color current, Action<System.Drawing.Color> set)
        {
            try
            {
                var v = new Vector4(current.R / 255f, current.G / 255f, current.B / 255f, current.A / 255f);
                ImGui.Text(label);
                ImGui.SameLine(110f);
                ImGui.SetNextItemWidth(160f);
                if (ImGui.ColorEdit4($"##espc_{label}", ref v, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.DisplayHex))
                {
                    v.X = Math.Clamp(v.X, 0f, 1f); v.Y = Math.Clamp(v.Y, 0f, 1f);
                    v.Z = Math.Clamp(v.Z, 0f, 1f); v.W = Math.Clamp(v.W, 0f, 1f);
                    set(System.Drawing.Color.FromArgb((int)(v.W * 255), (int)(v.X * 255), (int)(v.Y * 255), (int)(v.Z * 255)));
                }
            }
            catch { }
        }

        /// <summary>Animated ESP preview: dummy bunny-like character moves with sine, all enabled ESP styles follow it.</summary>
                
    }
}
