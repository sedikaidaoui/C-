using Guna.UI2.WinForms;
using ImGuiNET;
using Memory;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using AotForms.Components;
using static AotForms.Config;
using static AotForms.WinAPI;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Color = System.Drawing.Color;
using Size = System.Drawing.Size;

namespace AotForms
{
    internal partial class ESP
    {
        public void RenderUI()
        {
            if (_uiAlpha <= 0.001f && !_uiVisible)
                return;

            ApplyThemeOverlay();
            DrawWatermark();

            if (_flowPhase == FlowPhase.Main)
                DrawBottomCenterTextSafe();

            ImGui.SetNextWindowSize(new Vector2(UiMetrics.WindowW, UiMetrics.WindowH), ImGuiCond.Always);
            ImGui.SetNextWindowPos(new Vector2(50, 50), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowBgAlpha(0.08f * _uiAlpha);

            ImGui.Begin("##External",
                ImGuiWindowFlags.NoResize |
                ImGuiWindowFlags.NoCollapse |
                ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoTitleBar |
                ImGuiWindowFlags.NoBringToFrontOnFocus);

            var wdraw = ImGui.GetWindowDrawList();
            Vector2 wpos = ImGui.GetWindowPos();
            Vector2 wsize = ImGui.GetWindowSize();

            float innerW = wsize.X - UiMetrics.OuterPad * 2f;
            float innerH = wsize.Y - UiMetrics.OuterPad * 2f;

            wdraw.AddRectFilled(wpos, wpos + wsize,
                ImGui.GetColorU32(new Vector4(PanelBg.X, PanelBg.Y, PanelBg.Z, UiMetrics.WindowGlassAlpha * _uiAlpha)),
                12f);

            wdraw.AddRect(
                wpos + new Vector2(0.5f, 0.5f),
                wpos + wsize - new Vector2(0.5f, 0.5f),
                ImGui.GetColorU32(new Vector4(24f / 255f, 25f / 255f, 31f / 255f, 0.85f * _uiAlpha)),
                12f,
                ImDrawFlags.None,
                1f);

            uint cornerGlow = ImGui.GetColorU32(new Vector4(ThemeColor.X, ThemeColor.Y, ThemeColor.Z, 0.06f * _uiAlpha));
            wdraw.AddCircleFilled(wpos + new Vector2(0f, wsize.Y), 36f, cornerGlow);
            wdraw.AddCircleFilled(wpos + new Vector2(wsize.X, wsize.Y), 36f, cornerGlow);

            DrawWindowChrome(wpos, wsize);

            float bodyY = UiMetrics.OuterPad + UiMetrics.ChromeH + UiMetrics.ChromeBodyGap;
            float bodyH = MathF.Max(80f, innerH - UiMetrics.ChromeH - UiMetrics.ChromeBodyGap);

            ImGui.SetCursorPos(new Vector2(UiMetrics.OuterPad, bodyY));
            ImGui.BeginChild("##body_cs", new Vector2(innerW, bodyH), ImGuiChildFlags.None,
                ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar);

            if (_flowPhase == FlowPhase.Login || _flowPhase == FlowPhase.PostLoginLoad)
            {
                FillPanelBackground(6f);
                ImGui.SetCursorPos(new Vector2(UiMetrics.PanelPad, UiMetrics.PanelPad));
                ImGui.BeginChild("##login_cs",
                    new Vector2(innerW - UiMetrics.PanelPad * 2f, bodyH - UiMetrics.PanelPad * 2f),
                    ImGuiChildFlags.None,
                    ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar);

                if (_flowPhase == FlowPhase.PostLoginLoad)
                {
                    if (!_showHookSuccessScreen && _emulatorInitTask != null && _emulatorInitTask.IsCompleted)
                    {
                        if (_emulatorInitTask.IsFaulted)
                        {
                            _loginError = _emulatorInitTask.Exception?.GetBaseException().Message ?? "Init failed";
                            ResetFlowToLogin();
                        }
                        else
                        {
                            string? err = _emulatorInitTask.Result;
                            if (!string.IsNullOrEmpty(err))
                            {
                                _loginError = err;
                                ResetFlowToLogin();
                            }
                            else
                            {
                                _showHookSuccessScreen = true;
                                _hookSuccessAnimTime = 0f;
                            }
                        }
                    }

                    if (_showHookSuccessScreen)
                        DrawHookSuccessScreen();
                    else
                        DrawEmperorLoader();
                }
                else
                    DrawMainContent();

                ImGui.EndChild();
            }
            else
            {
                ImGui.BeginChild("##layout_cs", new Vector2(innerW, bodyH), ImGuiChildFlags.None,
                    ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar);

                ImGui.BeginChild("##sidebar_cs", new Vector2(UiMetrics.SidebarW, bodyH), ImGuiChildFlags.None,
                    ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar);
                DrawSidebarPanel();
                ImGui.EndChild();

                ImGui.SameLine(0f, 0f);

                float mainW = MathF.Max(120f, innerW - UiMetrics.SidebarW);
                ImGui.BeginChild("##content_cs", new Vector2(mainW, bodyH), ImGuiChildFlags.None,
                    ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar);
                FillPanelBackground(6f, ImDrawFlags.RoundCornersRight);
                PushPanelContentPadding();
                DrawMainContent();
                ImGui.EndChild();

                ImGui.EndChild();
            }

            ImGui.EndChild();

            if (_flowPhase == FlowPhase.Main)
            {
                _mainPanelPos = wpos;
                _mainPanelSize = wsize;
            }

            DrawTabPopup();
            SubmitTitleBarDragHitbox(wsize);

            ImGui.End();

            if (_flowPhase == FlowPhase.Main && _activeTab == 1 && Config.ESPEditor && _uiAlpha > 0.001f)
            {
                EnsureEspEditorPreview();
                const float gap = 12f;
                ImGui.SetNextWindowPos(new Vector2(_mainPanelPos.X + _mainPanelSize.X + gap, _mainPanelPos.Y), ImGuiCond.Always);
                ImGui.SetNextWindowSize(new Vector2(Math.Clamp(ESP.EspEditorWinW, 280f, 640f), _mainPanelSize.Y), ImGuiCond.Always);
                float panelA = 0.78f * Math.Clamp(_uiAlpha, 0f, 1f);
                Vector4 panelBg = new Vector4(0.09f, 0.09f, 0.1f, panelA);
                ImGui.PushStyleColor(ImGuiCol.WindowBg, panelBg);
                ImGui.PushStyleColor(ImGuiCol.TitleBg, panelBg);
                ImGui.PushStyleColor(ImGuiCol.TitleBgActive, panelBg);
                ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.04f, 0.04f, 0.05f, panelA));
                ImGui.Begin("ESP EDITOR", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);
                DrawEspEditorPreviewPanel();
                ImGui.End();
                ImGui.PopStyleColor(4);
            }
        }
    }
}
