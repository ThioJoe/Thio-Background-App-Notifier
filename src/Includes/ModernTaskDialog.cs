using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using static System.Net.Mime.MediaTypeNames;

namespace ThioWinUtils;

/// <summary>
/// A wrapper around the native Windows TaskDialogIndirect API.
/// Provides support for modern message boxes with custom icons, buttons, footers, and expanders.
/// Includes full interaction support (Progress Bars, Dynamic Text, Events).
/// </summary>
public class ModernTaskDialog
{
    // -------------------------------------------------------------------------
    // Configuration Properties
    // -------------------------------------------------------------------------

    public string Title { get; set; }
    public string MainInstruction { get; set; }
    public string Content { get; set; }
    public string Footer { get; set; }
    public string ExpandedInformation { get; set; }
    public string VerificationText { get; set; }
    public string ExpandedControlText { get; set; }
    public string CollapsedControlText { get; set; }

    public IntPtr ParentWindowHandle { get; set; } = IntPtr.Zero;
    public IntPtr InstanceHandle { get; set; } = IntPtr.Zero;

    public TaskDialogFlags Flags { get; set; } = TaskDialogFlags.TDF_ALLOW_DIALOG_CANCELLATION | TaskDialogFlags.TDF_POSITION_RELATIVE_TO_WINDOW | TaskDialogFlags.TDF_SIZE_TO_CONTENT;
    public TaskDialogCommonButtonFlags CommonButtons { get; set; } = TaskDialogCommonButtonFlags.TDCBF_OK_BUTTON;

    // Icons
    public TaskDialogIcon MainIcon { get; set; } = TaskDialogIcon.None;
    public TaskDialogIcon FooterIcon { get; set; } = TaskDialogIcon.None;
    public TaskDialogBarColor Coloredbar { get; set; } = TaskDialogBarColor.Default;

    /// <summary>
    /// Set this to a valid HICON handle to use a custom icon. 
    /// Requires the TDF_USE_HICON_MAIN flag to be set automatically if this is not Zero.
    /// </summary>
    public IntPtr CustomMainIconHandle { get; set; } = IntPtr.Zero;

    /// <summary>
    /// Set this to a valid HICON handle to use a custom footer icon.
    /// Requires the TDF_USE_HICON_FOOTER flag to be set automatically if this is not Zero.
    /// </summary>
    public IntPtr CustomFooterIconHandle { get; set; } = IntPtr.Zero;

    // Custom Buttons
    public List<TaskDialogCustomButton> CustomButtons { get; set; } = new();
    public int DefaultButtonId { get; set; } = 0;

    // Radio Buttons
    public List<TaskDialogCustomButton> RadioButtons { get; set; } = new();
    public int DefaultRadioButtonId { get; set; } = 0;

    // -------------------------------------------------------------------------
    // Events
    // -------------------------------------------------------------------------

    public event EventHandler<TaskDialogCreatedEventArgs> Created;
    public event EventHandler<TaskDialogButtonEventArgs> ButtonClicked;
    public event EventHandler<TaskDialogButtonEventArgs> RadioButtonClicked;
    public event EventHandler<TaskDialogHyperlinkEventArgs> HyperlinkClicked;
    public event EventHandler<TaskDialogVerificationEventArgs> VerificationClicked;
    public event EventHandler<TaskDialogExpandoEventArgs> ExpandoButtonClicked;
    public event EventHandler<TaskDialogTimerEventArgs> Timer;
    public event EventHandler Destroyed;
    public event EventHandler HelpInvoked;

    // -------------------------------------------------------------------------
    // Results
    // -------------------------------------------------------------------------
    public bool VerificationChecked { get; private set; }
    public int SelectedButtonId { get; private set; }
    public int SelectedRadioButtonId { get; private set; }

    // -------------------------------------------------------------------------
    // Private State Trackers
    // -------------------------------------------------------------------------

    private IntPtr _activeDialogWindowHandle = IntPtr.Zero;
    private EventHandler<TaskDialogCreatedEventArgs> _coloredBarIconSwapHandler;
    private TASKDIALOGCONFIG _lastDialogConfig = new();

    // State preservation for TDM_NAVIGATE_PAGE
    private bool _preserveVerificationState = false;
    private int _preserveRadioButtonId = 0;
    private bool _preserveExpanderState = false;

    // Progress bar state
    private bool _hasProgressBar = false;
    private int _preserveProgressBarPosition = 0;
    private short _preserveProgressBarMin = 0;
    private short _preserveProgressBarMax = 100;
    private TaskDialogProgressBarState _preserveProgressBarState = TaskDialogProgressBarState.Normal;
    private bool _preserveProgressBarMarquee = false;
    private int _preserveMarqueeSpeed = 0;

    // Button states
    private Dictionary<int, bool> _preserveButtonEnabledStates = new();
    private Dictionary<int, bool> _preserveRadioButtonEnabledStates = new();
    private HashSet<int> _preserveButtonShieldStates = new();

    // Dynamic text (only preserved if explicitly set via SetElementText)
    private Dictionary<TaskDialogElements, string> _preserveDynamicText = new();

    // Icon state (stores the user's intended icon when using colored bars)
    private TaskDialogIcon _preservedMainIcon = TaskDialogIcon.None;

    // -------------------------------------------------------------------------
    // Public Methods - Show
    // -------------------------------------------------------------------------

    /// <summary>
    /// Shows the Task Dialog.
    /// </summary>
    /// <returns>The ID of the button clicked.</returns>
    public int Show()
    {
        TASKDIALOGCONFIG config = new()
        {
            cbSize = (uint)Marshal.SizeOf(typeof(TASKDIALOGCONFIG)),
            hwndParent = ParentWindowHandle,
            hInstance = InstanceHandle
        };

        // Handle Flags
        TaskDialogFlags finalFlags = this.Flags;
        if (CustomMainIconHandle != IntPtr.Zero) finalFlags |= TaskDialogFlags.TDF_USE_HICON_MAIN;
        if (CustomFooterIconHandle != IntPtr.Zero) finalFlags |= TaskDialogFlags.TDF_USE_HICON_FOOTER;

        config.dwFlags = finalFlags;
        config.dwCommonButtons = CommonButtons;

        // ---- Handle colored bar setup before populating icon config ----
        if (this.Coloredbar != TaskDialogBarColor.Default)
            SetupIconWithColoredBar(MainIcon, this.Coloredbar);

        // Strings
        config.pszWindowTitle = Title;
        config.pszMainInstruction = MainInstruction;
        config.pszContent = Content;
        config.pszFooter = Footer;
        config.pszExpandedInformation = ExpandedInformation;
        config.pszVerificationText = VerificationText;
        config.pszExpandedControlText = ExpandedControlText;
        config.pszCollapsedControlText = CollapsedControlText;

        // Icons
        config.hMainIcon = CustomMainIconHandle != IntPtr.Zero ? CustomMainIconHandle : (IntPtr)MainIcon;
        config.hFooterIcon = CustomFooterIconHandle != IntPtr.Zero ? CustomFooterIconHandle : (IntPtr)FooterIcon;

        // Marshalling Buttons
        IntPtr pButtons = IntPtr.Zero;
        IntPtr pRadioButtons = IntPtr.Zero;

        try
        {
            if (CustomButtons.Count > 0)
            {
                int structSize = Marshal.SizeOf(typeof(TASKDIALOG_BUTTON));
                pButtons = Marshal.AllocHGlobal(structSize * CustomButtons.Count);
                for (int i = 0; i < CustomButtons.Count; i++)
                {
                    var btnStruct = new TASKDIALOG_BUTTON { nButtonID = CustomButtons[i].ID, pszButtonText = CustomButtons[i].Text };
                    Marshal.StructureToPtr(btnStruct, pButtons + (i * structSize), false);
                }
                config.cButtons = (uint)CustomButtons.Count;
                config.pButtons = pButtons;
                config.nDefaultButton = DefaultButtonId;
            }

            if (RadioButtons.Count > 0)
            {
                int structSize = Marshal.SizeOf(typeof(TASKDIALOG_BUTTON));
                pRadioButtons = Marshal.AllocHGlobal(structSize * RadioButtons.Count);
                for (int i = 0; i < RadioButtons.Count; i++)
                {
                    var btnStruct = new TASKDIALOG_BUTTON { nButtonID = RadioButtons[i].ID, pszButtonText = RadioButtons[i].Text };
                    Marshal.StructureToPtr(btnStruct, pRadioButtons + (i * structSize), false);
                }
                config.cRadioButtons = (uint)RadioButtons.Count;
                config.pRadioButtons = pRadioButtons;
                config.nDefaultRadioButton = DefaultRadioButtonId;
            }

            // Callback
            TaskDialogCallbackProc callbackDelegate = InternalCallback;
            config.pfCallback = callbackDelegate;

            int result;
            int buttonPressed = 0;
            int radioButtonPressed = 0;
            bool verificationChecked = false;

            // Actually show the dialog
            try
            {
                _lastDialogConfig = config; // Store it in case we want to refresh it fully with TDM_NAVIGATE_PAGE
                result = TaskDialogIndirect(ref config, out buttonPressed, out radioButtonPressed, out verificationChecked);
            }
            catch (EntryPointNotFoundException)
            {
                // This exception occurs if the application is using the legacy (v5) ComCtl32.dll
                throw new InvalidOperationException(
                    "ThioWinUtils ModernTaskDialog failed to load. This application is likely using Common Controls v5 (default), " +
                    "but ModernTaskDialog requires Version 6.\n\n" +
                    "SOLUTION: You must add an 'app.manifest' to your project (Right Click the Project > Add > New Item > Show All Templates (if not already) > Application Manifest File). " +
                    "In app.manifest, uncomment the 'Microsoft.Windows.Common-Controls' dependency section, which may be labelled \"Enable themes for Windows common controls and dialogs\"." +
                    "\n\nSpecifically, uncomment this section:\n" +
                    "  <dependency>\n" +
                    "    <dependentAssembly>\n" +
                    "      <assemblyIdentity\n" +
                    "          type=\"win32\"\n" +
                    "          name=\"Microsoft.Windows.Common-Controls\"\n" +
                    "          version=\"6.0.0.0\"\n" +
                    "          processorArchitecture=\"*\"\n" +
                    "          publicKeyToken=\"6595b64144ccf1df\"\n" +
                    "          language=\"*\"\n" +
                    "        />\n" +
                    "    </dependentAssembly>\n" +
                    "  </dependency>"

                    );
            }

            this.SelectedButtonId = buttonPressed;
            this.SelectedRadioButtonId = radioButtonPressed;
            this.VerificationChecked = verificationChecked;

            return buttonPressed;
        }
        finally
        {
            if (pButtons != IntPtr.Zero) Marshal.FreeHGlobal(pButtons);
            if (pRadioButtons != IntPtr.Zero) Marshal.FreeHGlobal(pRadioButtons);
            _activeDialogWindowHandle = IntPtr.Zero; // Ensure cleared
        }
    }

    // -------------------------------------------------------------------------
    // Interaction Methods (Send Messages)
    // -------------------------------------------------------------------------

    // Check if dialog is active before sending messages
    private bool IsActive()
    {
        // We will use to silently fail if not active, as these might be called before creation or after destruction in race conditions
        if (_activeDialogWindowHandle != IntPtr.Zero)
            return true;
        else
            return false;
    }

    // This effectively refreshes the page.
    private void RefreshPage(ref TASKDIALOGCONFIG newConfig)
    {
        if (!IsActive()) return;
        SendMessage(_activeDialogWindowHandle, (uint)TaskDialogMessages.TDM_NAVIGATE_PAGE, IntPtr.Zero, ref newConfig);
    }

    /// <summary>
    /// Navigates to a new page with full state preservation.
    /// This is a generic method that can be used for any navigation scenario.
    /// </summary>
    private void NavigateWithStatePreservation()
    {
        if (!IsActive()) return;

        // Capture the current icon before we modify anything
        TaskDialogIcon currentIcon = MainIcon;
        bool isUsingColoredBar = (Coloredbar != TaskDialogBarColor.Default);

        // Rebuild the config based on the last one used
        TASKDIALOGCONFIG newConfig = _lastDialogConfig;

        // Preserve radio button selection by updating the default in the config
        if (_preserveRadioButtonId != 0 && RadioButtons.Count > 0)
        {
            newConfig.nDefaultRadioButton = _preserveRadioButtonId;
        }

        // Preserve verification checkbox state in flags
        if (_preserveVerificationState && !string.IsNullOrEmpty(VerificationText))
        {
            newConfig.dwFlags |= TaskDialogFlags.TDF_VERIFICATION_FLAG_CHECKED;
        }
        else
        {
            newConfig.dwFlags &= ~TaskDialogFlags.TDF_VERIFICATION_FLAG_CHECKED;
        }

        // Preserve expander state by updating the flags
        if (_preserveExpanderState)
        {
            newConfig.dwFlags |= TaskDialogFlags.TDF_EXPANDED_BY_DEFAULT;
        }
        else
        {
            newConfig.dwFlags &= ~TaskDialogFlags.TDF_EXPANDED_BY_DEFAULT;
        }

        // Handle colored bar: need to set shield variant for navigation
        if (isUsingColoredBar)
        {
            TaskDialogIcon shieldVariant = BarColorToIcon(Coloredbar);
            newConfig.hMainIcon = (IntPtr)shieldVariant;
        }
        else
        {
            // No colored bar, use the current icon
            newConfig.hMainIcon = (CustomMainIconHandle != IntPtr.Zero) ? CustomMainIconHandle : (IntPtr)currentIcon;
        }

        // Navigate to the new page
        _lastDialogConfig = newConfig;
        RefreshPage(ref newConfig);

        // Icon restoration is handled in TDN_NAVIGATED callback
    }

    /// <summary>Updates text elements of the dialog while it is open.</summary>
    public void SetElementText(TaskDialogElements element, string text)
    {
        if (!IsActive()) return;
        SendMessage(_activeDialogWindowHandle, (uint)TaskDialogMessages.TDM_SET_ELEMENT_TEXT, (IntPtr)element, text);

        // Track dynamic text changes for state preservation
        _preserveDynamicText[element] = text;
    }

    /// <summary>Clicks a button programmatically.</summary>
    public void ClickButton(int buttonId)
    {
        if (!IsActive()) return;
        SendMessage(_activeDialogWindowHandle, (uint)TaskDialogMessages.TDM_CLICK_BUTTON, (IntPtr)buttonId, IntPtr.Zero);
    }

    /// <summary>Clicks a radio button programmatically.</summary>
    public void ClickRadioButton(int radioButtonId)
    {
        if (!IsActive()) return;
        SendMessage(_activeDialogWindowHandle, (uint)TaskDialogMessages.TDM_CLICK_RADIO_BUTTON, (IntPtr)radioButtonId, IntPtr.Zero);
    }

    /// <summary>Enables or disables a button.</summary>
    public void EnableButton(int buttonId, bool enable)
    {
        if (!IsActive()) return;
        SendMessage(_activeDialogWindowHandle, (uint)TaskDialogMessages.TDM_ENABLE_BUTTON, (IntPtr)buttonId, (IntPtr)(enable ? 1 : 0));

        // Track button enabled state for preservation
        _preserveButtonEnabledStates[buttonId] = enable;
    }

    /// <summary>Enables or disables a radio button.</summary>
    public void EnableRadioButton(int buttonId, bool enable)
    {
        if (!IsActive()) return;
        SendMessage(_activeDialogWindowHandle, (uint)TaskDialogMessages.TDM_ENABLE_RADIO_BUTTON, (IntPtr)buttonId, (IntPtr)(enable ? 1 : 0));

        // Track radio button enabled state for preservation
        _preserveRadioButtonEnabledStates[buttonId] = enable;
    }

    /// <summary>Updates the check state of the verification checkbox.</summary>
    public void ClickVerification(bool check, bool setFocus = false)
    {
        if (!IsActive()) return;
        SendMessage(_activeDialogWindowHandle, (uint)TaskDialogMessages.TDM_CLICK_VERIFICATION, (IntPtr)(check ? 1 : 0), (IntPtr)(setFocus ? 1 : 0));
    }

    /// <summary>Sets the progress bar position (0-100 by default).</summary>
    public void SetProgressBarPosition(int position)
    {
        if (!IsActive()) return;
        SendMessage(_activeDialogWindowHandle, (uint)TaskDialogMessages.TDM_SET_PROGRESS_BAR_POS, (IntPtr)position, IntPtr.Zero);

        // Track progress bar position for preservation
        _preserveProgressBarPosition = position;
    }

    /// <summary>Sets the progress bar range.</summary>
    public void SetProgressBarRange(short min, short max)
    {
        if (!IsActive()) return;
        // MAKELPARAM logic: low word is min, high word is max
        int range = (max << 16) | (ushort)min;
        SendMessage(_activeDialogWindowHandle, (uint)TaskDialogMessages.TDM_SET_PROGRESS_BAR_RANGE, IntPtr.Zero, (IntPtr)range);

        // Track progress bar range for preservation
        _preserveProgressBarMin = min;
        _preserveProgressBarMax = max;
    }

    /// <summary>Sets the state (Normal, Error, Paused) of the progress bar.</summary>
    public void SetProgressBarState(TaskDialogProgressBarState state)
    {
        if (!IsActive()) return;
        SendMessage(_activeDialogWindowHandle, (uint)TaskDialogMessages.TDM_SET_PROGRESS_BAR_STATE, (IntPtr)state, IntPtr.Zero);

        // Track progress bar state for preservation
        _preserveProgressBarState = state;
    }

    /// <summary>Sets marquee mode on/off.</summary>
    public void SetProgressBarMarquee(bool isMarquee, int animationSpeedMilliseconds = 0)
    {
        if (!IsActive()) return;
        SendMessage(_activeDialogWindowHandle, (uint)TaskDialogMessages.TDM_SET_MARQUEE_PROGRESS_BAR, (IntPtr)(isMarquee ? 1 : 0), IntPtr.Zero);
        SendMessage(_activeDialogWindowHandle, (uint)TaskDialogMessages.TDM_SET_PROGRESS_BAR_MARQUEE, (IntPtr)(isMarquee ? 1 : 0), (IntPtr)animationSpeedMilliseconds);

        // Track marquee state for preservation
        _preserveProgressBarMarquee = isMarquee;
        _preserveMarqueeSpeed = animationSpeedMilliseconds;
    }

    /// <summary>Updates the main or footer icon.</summary>
    public void UpdateIcon(TaskDialogIconElement element, TaskDialogIcon icon)
    {
        if (!IsActive()) return;
        SendMessage(_activeDialogWindowHandle, (uint)TaskDialogMessages.TDM_UPDATE_ICON, (IntPtr)element, (IntPtr)icon);

        // Track main icon changes for preservation during colored bar updates
        if (element == TaskDialogIconElement.Main)
        {
            _preservedMainIcon = icon;
        }
    }

    /// <summary>Updates the main or footer icon.</summary>
    public void UpdateIcon(TaskDialogIconElement element, TaskDialogBarColor barColor)
    {
        if (!IsActive()) return;
        SendMessage(_activeDialogWindowHandle, (uint)TaskDialogMessages.TDM_UPDATE_ICON, (IntPtr)element, (IntPtr)barColor);
    }

    /// <summary>Updates the main or footer icon using a handle.</summary>
    public void UpdateIcon(TaskDialogIconElement element, IntPtr iconHandle)
    {
        if (!IsActive()) return;
        SendMessage(_activeDialogWindowHandle, (uint)TaskDialogMessages.TDM_UPDATE_ICON, (IntPtr)element, iconHandle);
    }

    /// <summary>Adds a shield icon to the button (UAC).</summary>
    public void SetButtonElevationRequiredState(int buttonId, bool required)
    {
        if (!IsActive()) return;
        SendMessage(_activeDialogWindowHandle, (uint)TaskDialogMessages.TDM_SET_BUTTON_ELEVATION_REQUIRED_STATE, (IntPtr)buttonId, (IntPtr)(required ? 1 : 0));

        // Track button shield state for preservation
        if (required)
            _preserveButtonShieldStates.Add(buttonId);
        else
            _preserveButtonShieldStates.Remove(buttonId);
    }

    /// <summary>
    /// Updates the colored bar while preserving the icon (if one was set).
    /// Can be called while the dialog is active.
    /// This recreates the dialog using TDM_NAVIGATE_PAGE to change the colored bar.
    /// Note: User interaction state (checkboxes, radio buttons, expander, etc.) is preserved.
    /// </summary>
    /// <param name="color">The new bar color to display.</param>
    public void UpdateColoredBar(TaskDialogBarColor color)
    {
        if (!IsActive()) return;

        // Update the bar color property
        Coloredbar = color;

        // Use the generic navigation method which handles all state preservation
        NavigateWithStatePreservation();
    }

    /// <summary>
    /// Internal method to set up the colored bar during dialog initialization.
    /// Sets MainIcon to the shield variant and registers a handler to swap to the desired icon on creation.
    /// </summary>
    private void SetupIconWithColoredBar(TaskDialogIcon icon, TaskDialogBarColor color)
    {
        // Store the user's intended icon for restoration after navigation
        _preservedMainIcon = icon;

        // Map color to shield variant
        TaskDialogIcon shieldVariant = BarColorToIcon(color);

        // Set MainIcon to the shield variant for initial display
        MainIcon = shieldVariant;

        // Remove old handler if it exists to prevent multiple subscriptions
        if (_coloredBarIconSwapHandler != null)
            Created -= _coloredBarIconSwapHandler;

        // Create and store new handler
        _coloredBarIconSwapHandler = (sender, e) =>
        {
            // Restore the true icon immediately after creation
            UpdateIcon(TaskDialogIconElement.Main, icon);
            // Update the property to reflect the user's actual icon
            MainIcon = icon;
        };

        Created += _coloredBarIconSwapHandler;
    }

    // -------------------------------------------------------------------------
    // Internal Callback Logic
    // -------------------------------------------------------------------------

    private int InternalCallback(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, IntPtr lpRefData)
    {
        var notification = (TaskDialogNotifications)msg;

        // Update internal handle tracker
        if (notification == TaskDialogNotifications.TDN_CREATED)
            _activeDialogWindowHandle = hwnd;
        else if (notification == TaskDialogNotifications.TDN_DESTROYED)
            _activeDialogWindowHandle = IntPtr.Zero;

        // Dispatch Events
        switch (notification)
        {
            case TaskDialogNotifications.TDN_CREATED:
                // Initialize state preservation with default/initial values
                if (RadioButtons.Count > 0 && DefaultRadioButtonId != 0)
                {
                    _preserveRadioButtonId = DefaultRadioButtonId;
                }
                if (!string.IsNullOrEmpty(VerificationText) && (Flags & TaskDialogFlags.TDF_VERIFICATION_FLAG_CHECKED) != 0)
                {
                    _preserveVerificationState = true;
                }
                if (!string.IsNullOrEmpty(ExpandedInformation) && (Flags & TaskDialogFlags.TDF_EXPANDED_BY_DEFAULT) != 0)
                {
                    _preserveExpanderState = true;
                }

                // Track if dialog has a progress bar
                _hasProgressBar = (Flags & TaskDialogFlags.TDF_SHOW_PROGRESS_BAR) != 0 || 
                                  (Flags & TaskDialogFlags.TDF_SHOW_MARQUEE_PROGRESS_BAR) != 0;

                Created?.Invoke(this, new TaskDialogCreatedEventArgs());
                break;

            case TaskDialogNotifications.TDN_NAVIGATED:
                // Restore all preserved states after navigation

                // Restore progress bar state if present
                if (_hasProgressBar)
                {
                    if (_preserveProgressBarMarquee)
                    {
                        SetProgressBarMarquee(true, _preserveMarqueeSpeed);
                    }
                    else
                    {
                        SetProgressBarRange(_preserveProgressBarMin, _preserveProgressBarMax);
                        SetProgressBarState(_preserveProgressBarState);
                        SetProgressBarPosition(_preserveProgressBarPosition);
                    }
                }

                // Restore button enabled/disabled states
                // Create a snapshot to avoid collection modification during enumeration
                foreach (var kvp in _preserveButtonEnabledStates.ToList())
                {
                    EnableButton(kvp.Key, kvp.Value);
                }

                // Restore radio button enabled/disabled states
                // Create a snapshot to avoid collection modification during enumeration
                foreach (var kvp in _preserveRadioButtonEnabledStates.ToList())
                {
                    EnableRadioButton(kvp.Key, kvp.Value);
                }

                // Restore UAC shield icons
                // Create a snapshot to avoid collection modification during enumeration
                foreach (int buttonId in _preserveButtonShieldStates.ToList())
                {
                    SetButtonElevationRequiredState(buttonId, true);
                }

                // Restore dynamic text that was set via SetElementText
                // Create a snapshot to avoid collection modification during enumeration
                foreach (var kvp in _preserveDynamicText.ToList())
                {
                    SetElementText(kvp.Key, kvp.Value);
                }

                // Restore the main icon if using colored bar
                if (Coloredbar != TaskDialogBarColor.Default)
                {
                    UpdateIcon(TaskDialogIconElement.Main, _preservedMainIcon);
                }

                break;

            case TaskDialogNotifications.TDN_BUTTON_CLICKED:
                var btnArgs = new TaskDialogButtonEventArgs((int)wParam);
                ButtonClicked?.Invoke(this, btnArgs);
                if (btnArgs.CancelClose) return 1; // S_FALSE prevents close
                break;

            case TaskDialogNotifications.TDN_RADIO_BUTTON_CLICKED:
                _preserveRadioButtonId = (int)wParam; // Track for navigation
                RadioButtonClicked?.Invoke(this, new TaskDialogButtonEventArgs((int)wParam));
                break;

            case TaskDialogNotifications.TDN_HYPERLINK_CLICKED:
                string url = Marshal.PtrToStringUni(lParam);
                HyperlinkClicked?.Invoke(this, new TaskDialogHyperlinkEventArgs(url));
                break;

            case TaskDialogNotifications.TDN_VERIFICATION_CLICKED:
                _preserveVerificationState = wParam != IntPtr.Zero; // Track for navigation
                VerificationClicked?.Invoke(this, new TaskDialogVerificationEventArgs(wParam != IntPtr.Zero));
                break;

            case TaskDialogNotifications.TDN_EXPANDO_BUTTON_CLICKED:
                _preserveExpanderState = wParam != IntPtr.Zero; // Track for navigation
                ExpandoButtonClicked?.Invoke(this, new TaskDialogExpandoEventArgs(wParam != IntPtr.Zero));
                break;

            case TaskDialogNotifications.TDN_TIMER:
                var timerArgs = new TaskDialogTimerEventArgs((uint)wParam);
                Timer?.Invoke(this, timerArgs);
                if (timerArgs.ResetTickCount) return 1; // S_FALSE resets tick
                break;

            case TaskDialogNotifications.TDN_HELP:
                HelpInvoked?.Invoke(this, EventArgs.Empty);
                break;

            case TaskDialogNotifications.TDN_DESTROYED:
                Destroyed?.Invoke(this, EventArgs.Empty);
                break;
        }

        return 0; // S_OK
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private TaskDialogIcon BarColorToIcon(TaskDialogBarColor color)
    {
        return color switch
        {
            TaskDialogBarColor.Blue => TaskDialogIcon.ShieldBlueBar,
            TaskDialogBarColor.Yellow => TaskDialogIcon.ShieldYellowBar,
            TaskDialogBarColor.Red => TaskDialogIcon.ShieldRedBar,
            TaskDialogBarColor.Green => TaskDialogIcon.ShieldGreenBar,
            TaskDialogBarColor.Gray => TaskDialogIcon.ShieldGrayBar,
            _ => TaskDialogIcon.None
        };
    }

    // -------------------------------------------------------------------------
    // Event Arguments
    // -------------------------------------------------------------------------

    public class TaskDialogCreatedEventArgs : EventArgs { }

    public class TaskDialogButtonEventArgs : EventArgs
    {
        public int ButtonId { get; }
        public bool CancelClose { get; set; } = false;
        public TaskDialogButtonEventArgs(int id) => ButtonId = id;
    }

    public class TaskDialogHyperlinkEventArgs : EventArgs
    {
        public string Url { get; }
        public TaskDialogHyperlinkEventArgs(string url) => Url = url;
    }

    public class TaskDialogVerificationEventArgs : EventArgs
    {
        public bool Checked { get; }
        public TaskDialogVerificationEventArgs(bool isChecked) => Checked = isChecked;
    }

    public class TaskDialogExpandoEventArgs : EventArgs
    {
        public bool Expanded { get; }
        public TaskDialogExpandoEventArgs(bool expanded) => Expanded = expanded;
    }

    public class TaskDialogTimerEventArgs : EventArgs
    {
        public uint TickCount { get; }
        public bool ResetTickCount { get; set; } = false;
        public TaskDialogTimerEventArgs(uint tick) => TickCount = tick;
    }

    // -------------------------------------------------------------------------
    // Native Structures & Enums
    // -------------------------------------------------------------------------

    // NOTE: Pack=1 is indeed required for these particular structs.
    // In CommCtrl.h in the Windows SDK where they are defined, they are defined between:
    //      "#include <pshpack1.h>" which turns out 1-byte packing
    //          and
    //      "#include <poppack.h>" which turns 1-byte packing off again.
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    private struct TASKDIALOGCONFIG
    {
        public uint cbSize;
        public IntPtr hwndParent;
        public IntPtr hInstance;
        public TaskDialogFlags dwFlags;
        public TaskDialogCommonButtonFlags dwCommonButtons;
        public string pszWindowTitle;
        public IntPtr hMainIcon;
        public string pszMainInstruction;
        public string pszContent;
        public uint cButtons;
        public IntPtr pButtons;
        public int nDefaultButton;
        public uint cRadioButtons;
        public IntPtr pRadioButtons;
        public int nDefaultRadioButton;
        public string pszVerificationText;
        public string pszExpandedInformation;
        public string pszExpandedControlText;
        public string pszCollapsedControlText;
        public IntPtr hFooterIcon;
        public string pszFooter;
        public TaskDialogCallbackProc pfCallback;
        public IntPtr lpCallbackData;
        public uint cxWidth;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    private struct TASKDIALOG_BUTTON
    {
        public int nButtonID;
        public string pszButtonText;
    }

    public class TaskDialogCustomButton(int id, string text)
    {
        public int ID { get; set; } = id;
        public string Text { get; set; } = text;
    }

    // -------------------------------------------------------------------------
    // P/Invoke
    // -------------------------------------------------------------------------

    [DllImport("comctl32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "TaskDialogIndirect"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int TaskDialogIndirect(ref TASKDIALOGCONFIG pTaskConfig, out int pnButton, out int pnRadioButton, out bool pfVerificationFlagChecked);

    // Standard overload
    [DllImport("user32.dll", EntryPoint = "SendMessageW"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    // TASKDIALOGCONFIG overload for TDM_NAVIGATE_PAGE (requires pointer to struct)
    [DllImport("user32.dll", EntryPoint = "SendMessageW"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, ref TASKDIALOGCONFIG lParam);

    // String overload for sending text
    [DllImport("user32.dll", EntryPoint = "SendMessageW", CharSet = CharSet.Unicode), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, string lParam);

    public delegate int TaskDialogCallbackProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam, IntPtr lpRefData);

    // -------------------------------------------------------------------------
    // Constants & Enums
    // -------------------------------------------------------------------------

    [Flags]
    public enum TaskDialogFlags : uint
    {
        TDF_ENABLE_HYPERLINKS = 0x0001,
        TDF_USE_HICON_MAIN = 0x0002,
        TDF_USE_HICON_FOOTER = 0x0004,
        TDF_ALLOW_DIALOG_CANCELLATION = 0x0008,
        TDF_USE_COMMAND_LINKS = 0x0010,
        TDF_USE_COMMAND_LINKS_NO_ICON = 0x0020,
        TDF_EXPAND_FOOTER_AREA = 0x0040,
        TDF_EXPANDED_BY_DEFAULT = 0x0080,
        TDF_VERIFICATION_FLAG_CHECKED = 0x0100,
        TDF_SHOW_PROGRESS_BAR = 0x0200,
        TDF_SHOW_MARQUEE_PROGRESS_BAR = 0x0400,
        TDF_CALLBACK_TIMER = 0x0800,
        TDF_POSITION_RELATIVE_TO_WINDOW = 0x1000,
        TDF_RTL_LAYOUT = 0x2000,
        TDF_NO_DEFAULT_RADIO_BUTTON = 0x4000,
        TDF_CAN_BE_MINIMIZED = 0x8000,
        // TDF_NO_SET_FOREGROUND = 0x00010000 // Windows 8+
        TDF_SIZE_TO_CONTENT = 0x01000000
    }

    [Flags]
    public enum TaskDialogCommonButtonFlags : uint
    {
        TDCBF_OK_BUTTON = 0x0001,
        TDCBF_YES_BUTTON = 0x0002,
        TDCBF_NO_BUTTON = 0x0004,
        TDCBF_CANCEL_BUTTON = 0x0008,
        TDCBF_RETRY_BUTTON = 0x0010,
        TDCBF_CLOSE_BUTTON = 0x0020
    }

    public enum TaskDialogIcon : int
    {
        None = 0,
        Warning = 65535,
        Error = 65534,
        Information = 65533,
        Shield = 65532,
        ShieldBlueBar = 65531,
        ShieldYellowBar = 65530,
        ShieldRedBar = 65529,
        ShieldGreenBar = 65528,
        ShieldGrayBar = 65527
    }

    public enum TaskDialogBarColor : int
    {
        Default = TaskDialogIcon.None,
        Blue = TaskDialogIcon.ShieldBlueBar,
        Yellow = TaskDialogIcon.ShieldYellowBar,
        Red = TaskDialogIcon.ShieldRedBar,
        Green = TaskDialogIcon.ShieldGreenBar,
        Gray = TaskDialogIcon.ShieldGrayBar
    }

    public enum TaskDialogElements : int
    {
        Content = 0,
        ExpandedInformation = 1,
        Footer = 2,
        MainInstruction = 3
    }

    public enum TaskDialogIconElement : int
    {
        Main = 0,
        Footer = 1
    }

    public enum TaskDialogProgressBarState : int
    {
        Normal = 1, // PBST_NORMAL
        Error = 2,  // PBST_ERROR
        Paused = 3  // PBST_PAUSED
    }

    public enum TaskDialogMessages : uint
    {
        TDM_NAVIGATE_PAGE = 0x0400 + 101,
        TDM_CLICK_BUTTON = 0x0400 + 102,
        TDM_SET_MARQUEE_PROGRESS_BAR = 0x0400 + 103,
        TDM_SET_PROGRESS_BAR_STATE = 0x0400 + 104,
        TDM_SET_PROGRESS_BAR_RANGE = 0x0400 + 105,
        TDM_SET_PROGRESS_BAR_POS = 0x0400 + 106,
        TDM_SET_PROGRESS_BAR_MARQUEE = 0x0400 + 107,
        TDM_SET_ELEMENT_TEXT = 0x0400 + 108,
        TDM_CLICK_RADIO_BUTTON = 0x0400 + 110,
        TDM_ENABLE_BUTTON = 0x0400 + 111,
        TDM_ENABLE_RADIO_BUTTON = 0x0400 + 112,
        TDM_CLICK_VERIFICATION = 0x0400 + 113,
        TDM_UPDATE_ELEMENT_TEXT = 0x0400 + 114,
        TDM_SET_BUTTON_ELEVATION_REQUIRED_STATE = 0x0400 + 115,
        TDM_UPDATE_ICON = 0x0400 + 116
    }

    public enum TaskDialogNotifications : uint
    {
        TDN_CREATED = 0,
        TDN_NAVIGATED = 1,
        TDN_BUTTON_CLICKED = 2,
        TDN_HYPERLINK_CLICKED = 3,
        TDN_TIMER = 4,
        TDN_DESTROYED = 5,
        TDN_RADIO_BUTTON_CLICKED = 6,
        TDN_DIALOG_CONSTRUCTED = 7,
        TDN_VERIFICATION_CLICKED = 8,
        TDN_HELP = 9,
        TDN_EXPANDO_BUTTON_CLICKED = 10
    }

    // -------------------------------------------------------------------------
    // Template Factory Methods
    // -------------------------------------------------------------------------

    /// <summary>
    /// Provides factory methods and templates for common TaskDialog configurations.
    /// </summary>
    public static class Template
    {
        // -------------------------------------------------------------------------
        // Common Button IDs (for reference when handling results)
        // -------------------------------------------------------------------------

        /// <summary>Standard button IDs returned by common buttons.</summary>
        public static class ButtonIds
        {
            public const int Ok = 1;
            public const int Cancel = 2;
            public const int Abort = 3;
            public const int Retry = 4;
            public const int Ignore = 5;
            public const int Yes = 6;
            public const int No = 7;
            public const int Close = 8;
        }

        // -------------------------------------------------------------------------
        // Simple Message Dialogs
        // -------------------------------------------------------------------------

        /// <summary>
        /// Shows an information dialog with an OK button.
        /// </summary>
        /// <param name="title">The window title.</param>
        /// <param name="mainInstruction">The main instruction (bold header text).</param>
        /// <param name="content">Optional additional content text.</param>
        /// <param name="parentHandle">Optional parent window handle.</param>
        /// <returns>The button ID clicked (always OK = 1).</returns>
        public static int ShowInfo(string title, string mainInstruction, string content = null, IntPtr parentHandle = default)
        {
            var dialog = new ModernTaskDialog
            {
                Title = title,
                MainInstruction = mainInstruction,
                Content = content,
                MainIcon = TaskDialogIcon.Information,
                CommonButtons = TaskDialogCommonButtonFlags.TDCBF_OK_BUTTON,
                Flags = TaskDialogFlags.TDF_ALLOW_DIALOG_CANCELLATION |
                        TaskDialogFlags.TDF_POSITION_RELATIVE_TO_WINDOW |
                        TaskDialogFlags.TDF_SIZE_TO_CONTENT,
                ParentWindowHandle = parentHandle
            };

            return dialog.Show();
        }

        /// <summary>
        /// Shows an information dialog with an OK button, with hyperlinks enabled in the content by using html "<a href>" syntax.
        /// </summary>
        /// <param name="title">The window title.</param>
        /// <param name="mainInstruction">The main instruction (bold header text).</param>
        /// <param name="content">Optional additional content text. Can contain hyperlinks with HTML "a href" syntax. </param>
        /// <param name="parentHandle">Optional parent window handle.</param>
        /// <returns>The button ID clicked (always OK = 1).</returns>
        public static int ShowInfoWithHyperlinks(string title, string mainInstruction, string content = null, IntPtr parentHandle = default)
        {
            Console.WriteLine("Test 14: Dialog with Hyperlinks");

            ModernTaskDialog dialog = new()
            {
                Title = title,
                MainInstruction = mainInstruction,
                Content = content,
                MainIcon = TaskDialogIcon.Information,
                Flags = 
                    ModernTaskDialog.TaskDialogFlags.TDF_ENABLE_HYPERLINKS |
                    ModernTaskDialog.TaskDialogFlags.TDF_ALLOW_DIALOG_CANCELLATION |
                    ModernTaskDialog.TaskDialogFlags.TDF_POSITION_RELATIVE_TO_WINDOW |
                    ModernTaskDialog.TaskDialogFlags.TDF_SIZE_TO_CONTENT,
                CommonButtons = ModernTaskDialog.TaskDialogCommonButtonFlags.TDCBF_OK_BUTTON
            };

            dialog.HyperlinkClicked += (s, e) =>
            {
                Console.WriteLine($"Hyperlink clicked: {e.Url}");
                System.Diagnostics.Process.Start(e.Url);
            };

            return dialog.Show();
        }

        /// <summary>
        /// Shows a warning dialog with an OK button.
        /// </summary>
        /// <param name="title">The window title.</param>
        /// <param name="mainInstruction">The main instruction (bold header text).</param>
        /// <param name="content">Optional additional content text.</param>
        /// <param name="parentHandle">Optional parent window handle.</param>
        /// <returns>The button ID clicked (always OK = 1).</returns>
        public static int ShowWarning(string title, string mainInstruction, string content = null, IntPtr parentHandle = default)
        {
            var dialog = new ModernTaskDialog
            {
                Title = title,
                MainInstruction = mainInstruction,
                Content = content,
                MainIcon = TaskDialogIcon.Warning,
                CommonButtons = TaskDialogCommonButtonFlags.TDCBF_OK_BUTTON,
                Flags = TaskDialogFlags.TDF_ALLOW_DIALOG_CANCELLATION |
                        TaskDialogFlags.TDF_POSITION_RELATIVE_TO_WINDOW |
                        TaskDialogFlags.TDF_SIZE_TO_CONTENT,
                ParentWindowHandle = parentHandle
            };

            return dialog.Show();
        }

        /// <summary>
        /// Shows an error dialog with an OK button.
        /// </summary>
        /// <param name="title">The window title.</param>
        /// <param name="mainInstruction">The main instruction (bold header text).</param>
        /// <param name="content">Optional additional content text.</param>
        /// <param name="parentHandle">Optional parent window handle.</param>
        /// <returns>The button ID clicked (always OK = 1).</returns>
        public static int ShowError(string title, string mainInstruction, string content = null, IntPtr parentHandle = default)
        {
            var dialog = new ModernTaskDialog
            {
                Title = title,
                MainInstruction = mainInstruction,
                Content = content,
                MainIcon = TaskDialogIcon.Error,
                CommonButtons = TaskDialogCommonButtonFlags.TDCBF_OK_BUTTON,
                Flags = TaskDialogFlags.TDF_ALLOW_DIALOG_CANCELLATION |
                        TaskDialogFlags.TDF_POSITION_RELATIVE_TO_WINDOW |
                        TaskDialogFlags.TDF_SIZE_TO_CONTENT,
                ParentWindowHandle = parentHandle
            };

            return dialog.Show();
        }

        /// <summary>
        /// Shows a success dialog with a green bar and OK button.
        /// </summary>
        /// <param name="title">The window title.</param>
        /// <param name="mainInstruction">The main instruction (bold header text).</param>
        /// <param name="content">Optional additional content text.</param>
        /// <param name="parentHandle">Optional parent window handle.</param>
        /// <returns>The button ID clicked (always OK = 1).</returns>
        public static int ShowSuccess(string title, string mainInstruction, string content = null, IntPtr parentHandle = default)
        {
            var dialog = new ModernTaskDialog
            {
                Title = title,
                MainInstruction = mainInstruction,
                Content = content,
                MainIcon = TaskDialogIcon.Information,
                Coloredbar = TaskDialogBarColor.Green,
                CommonButtons = TaskDialogCommonButtonFlags.TDCBF_OK_BUTTON,
                Flags = TaskDialogFlags.TDF_ALLOW_DIALOG_CANCELLATION |
                        TaskDialogFlags.TDF_POSITION_RELATIVE_TO_WINDOW |
                        TaskDialogFlags.TDF_SIZE_TO_CONTENT,
                ParentWindowHandle = parentHandle
            };

            return dialog.Show();
        }

        // -------------------------------------------------------------------------
        // Confirmation Dialogs
        // -------------------------------------------------------------------------

        /// <summary>
        /// Shows a Yes/No confirmation dialog.
        /// </summary>
        /// <param name="title">The window title.</param>
        /// <param name="mainInstruction">The main instruction (bold header text).</param>
        /// <param name="content">Optional additional content text.</param>
        /// <param name="icon">The icon to display (default: Warning).</param>
        /// <param name="parentHandle">Optional parent window handle.</param>
        /// <returns>True if Yes was clicked, false if No was clicked.</returns>
        public static bool ShowYesNo(string title, string mainInstruction, string content = null,
            TaskDialogIcon icon = TaskDialogIcon.Warning, IntPtr parentHandle = default)
        {
            var dialog = new ModernTaskDialog
            {
                Title = title,
                MainInstruction = mainInstruction,
                Content = content,
                MainIcon = icon,
                CommonButtons = TaskDialogCommonButtonFlags.TDCBF_YES_BUTTON |
                               TaskDialogCommonButtonFlags.TDCBF_NO_BUTTON,
                Flags = TaskDialogFlags.TDF_ALLOW_DIALOG_CANCELLATION |
                        TaskDialogFlags.TDF_POSITION_RELATIVE_TO_WINDOW |
                        TaskDialogFlags.TDF_SIZE_TO_CONTENT,
                ParentWindowHandle = parentHandle
            };

            return dialog.Show() == ButtonIds.Yes;
        }

        /// <summary>
        /// Shows a Yes/No/Cancel confirmation dialog.
        /// </summary>
        /// <param name="title">The window title.</param>
        /// <param name="mainInstruction">The main instruction (bold header text).</param>
        /// <param name="content">Optional additional content text.</param>
        /// <param name="icon">The icon to display (default: Warning).</param>
        /// <param name="parentHandle">Optional parent window handle.</param>
        /// <returns>Yes = 6, No = 7, Cancel = 2</returns>
        public static int ShowYesNoCancel(string title, string mainInstruction, string content = null,
            TaskDialogIcon icon = TaskDialogIcon.Warning, IntPtr parentHandle = default)
        {
            var dialog = new ModernTaskDialog
            {
                Title = title,
                MainInstruction = mainInstruction,
                Content = content,
                MainIcon = icon,
                CommonButtons = TaskDialogCommonButtonFlags.TDCBF_YES_BUTTON |
                               TaskDialogCommonButtonFlags.TDCBF_NO_BUTTON |
                               TaskDialogCommonButtonFlags.TDCBF_CANCEL_BUTTON,
                Flags = TaskDialogFlags.TDF_ALLOW_DIALOG_CANCELLATION |
                        TaskDialogFlags.TDF_POSITION_RELATIVE_TO_WINDOW |
                        TaskDialogFlags.TDF_SIZE_TO_CONTENT,
                ParentWindowHandle = parentHandle
            };

            return dialog.Show();
        }

        /// <summary>
        /// Shows an OK/Cancel confirmation dialog.
        /// </summary>
        /// <param name="title">The window title.</param>
        /// <param name="mainInstruction">The main instruction (bold header text).</param>
        /// <param name="content">Optional additional content text.</param>
        /// <param name="icon">The icon to display (default: Information).</param>
        /// <param name="parentHandle">Optional parent window handle.</param>
        /// <returns>True if OK was clicked, false if Cancel was clicked.</returns>
        public static bool ShowOkCancel(string title, string mainInstruction, string content = null,
            TaskDialogIcon icon = TaskDialogIcon.Information, IntPtr parentHandle = default)
        {
            var dialog = new ModernTaskDialog
            {
                Title = title,
                MainInstruction = mainInstruction,
                Content = content,
                MainIcon = icon,
                CommonButtons = TaskDialogCommonButtonFlags.TDCBF_OK_BUTTON |
                               TaskDialogCommonButtonFlags.TDCBF_CANCEL_BUTTON,
                Flags = TaskDialogFlags.TDF_ALLOW_DIALOG_CANCELLATION |
                        TaskDialogFlags.TDF_POSITION_RELATIVE_TO_WINDOW |
                        TaskDialogFlags.TDF_SIZE_TO_CONTENT,
                ParentWindowHandle = parentHandle
            };

            return dialog.Show() == ButtonIds.Ok;
        }

        /// <summary>
        /// Shows a Retry/Cancel dialog, typically for recoverable errors.
        /// </summary>
        /// <param name="title">The window title.</param>
        /// <param name="mainInstruction">The main instruction (bold header text).</param>
        /// <param name="content">Optional additional content text.</param>
        /// <param name="parentHandle">Optional parent window handle.</param>
        /// <returns>True if Retry was clicked, false if Cancel was clicked.</returns>
        public static bool ShowRetryCancel(string title, string mainInstruction, string content = null, IntPtr parentHandle = default)
        {
            var dialog = new ModernTaskDialog
            {
                Title = title,
                MainInstruction = mainInstruction,
                Content = content,
                MainIcon = TaskDialogIcon.Warning,
                CommonButtons = TaskDialogCommonButtonFlags.TDCBF_RETRY_BUTTON |
                               TaskDialogCommonButtonFlags.TDCBF_CANCEL_BUTTON,
                Flags = TaskDialogFlags.TDF_ALLOW_DIALOG_CANCELLATION |
                        TaskDialogFlags.TDF_POSITION_RELATIVE_TO_WINDOW |
                        TaskDialogFlags.TDF_SIZE_TO_CONTENT,
                ParentWindowHandle = parentHandle
            };

            return dialog.Show() == ButtonIds.Retry;
        }

        // -------------------------------------------------------------------------
        // Dialog with Verification Checkbox
        // -------------------------------------------------------------------------

        /// <summary>
        /// Result from a dialog with a verification checkbox.
        /// </summary>
        public class VerificationResult
        {
            /// <summary>The button ID that was clicked.</summary>
            public int ButtonId { get; set; }

            /// <summary>Whether the verification checkbox was checked.</summary>
            public bool VerificationChecked { get; set; }
        }

        /// <summary>
        /// Shows a confirmation dialog with a "Don't show again" checkbox.
        /// </summary>
        /// <param name="title">The window title.</param>
        /// <param name="mainInstruction">The main instruction (bold header text).</param>
        /// <param name="content">Optional additional content text.</param>
        /// <param name="verificationText">Text for the checkbox (default: "Don't show this message again").</param>
        /// <param name="icon">The icon to display (default: Information).</param>
        /// <param name="parentHandle">Optional parent window handle.</param>
        /// <returns>A result containing the button clicked and checkbox state.</returns>
        public static VerificationResult ShowWithDontShowAgain(
            string title,
            string mainInstruction,
            string content = null,
            string verificationText = "Don't show this message again",
            TaskDialogIcon icon = TaskDialogIcon.Information,
            IntPtr parentHandle = default
        )
        {
            var dialog = new ModernTaskDialog
            {
                Title = title,
                MainInstruction = mainInstruction,
                Content = content,
                VerificationText = verificationText,
                MainIcon = icon,
                CommonButtons = TaskDialogCommonButtonFlags.TDCBF_OK_BUTTON,
                Flags = TaskDialogFlags.TDF_ALLOW_DIALOG_CANCELLATION |
                        TaskDialogFlags.TDF_POSITION_RELATIVE_TO_WINDOW |
                        TaskDialogFlags.TDF_SIZE_TO_CONTENT,
                ParentWindowHandle = parentHandle
            };

            int buttonId = dialog.Show();

            return new VerificationResult
            {
                ButtonId = buttonId,
                VerificationChecked = dialog.VerificationChecked
            };
        }

        /// <summary>
        /// Shows a Yes/No dialog with a verification checkbox.
        /// </summary>
        /// <param name="title">The window title.</param>
        /// <param name="mainInstruction">The main instruction (bold header text).</param>
        /// <param name="content">Optional additional content text.</param>
        /// <param name="verificationText">Text for the checkbox.</param>
        /// <param name="icon">The icon to display (default: Warning).</param>
        /// <param name="parentHandle">Optional parent window handle.</param>
        /// <returns>A result containing the button clicked and checkbox state.</returns>
        public static VerificationResult ShowYesNoWithVerification(
            string title,
            string mainInstruction,
            string content = null,
            string verificationText = "Don't ask me again",
            TaskDialogIcon icon = TaskDialogIcon.Warning,
            IntPtr parentHandle = default
        )
        {
            var dialog = new ModernTaskDialog
            {
                Title = title,
                MainInstruction = mainInstruction,
                Content = content,
                VerificationText = verificationText,
                MainIcon = icon,
                CommonButtons = TaskDialogCommonButtonFlags.TDCBF_YES_BUTTON |
                               TaskDialogCommonButtonFlags.TDCBF_NO_BUTTON,
                Flags = TaskDialogFlags.TDF_ALLOW_DIALOG_CANCELLATION |
                        TaskDialogFlags.TDF_POSITION_RELATIVE_TO_WINDOW |
                        TaskDialogFlags.TDF_SIZE_TO_CONTENT,
                ParentWindowHandle = parentHandle
            };

            int buttonId = dialog.Show();

            return new VerificationResult
            {
                ButtonId = buttonId,
                VerificationChecked = dialog.VerificationChecked
            };
        }

        // -------------------------------------------------------------------------
        // Command Link Dialogs
        // -------------------------------------------------------------------------

        /// <summary>
        /// Represents a command link option with ID, text, and optional description.
        /// </summary>
        public class CommandLinkOption
        {
            /// <summary>The unique ID for this command link (used to identify the result).</summary>
            public int Id { get; set; }

            /// <summary>The main text of the command link.</summary>
            public string Text { get; set; }

            /// <summary>Optional description shown below the main text.</summary>
            public string Description { get; set; }

            public CommandLinkOption(int id, string text, string description = null)
            {
                Id = id;
                Text = text;
                Description = description;
            }

            internal string GetFullText()
            {
                return string.IsNullOrEmpty(Description) ? Text : $"{Text}\n{Description}";
            }
        }

        /// <summary>
        /// Shows a dialog with command link buttons.
        /// </summary>
        /// <param name="title">The window title.</param>
        /// <param name="mainInstruction">The main instruction (bold header text).</param>
        /// <param name="options">The command link options to display.</param>
        /// <param name="content">Optional additional content text.</param>
        /// <param name="icon">The icon to display (default: Information).</param>
        /// <param name="allowCancel">Whether to include a Cancel button.</param>
        /// <param name="parentHandle">Optional parent window handle.</param>
        /// <returns>The ID of the selected command link, or Cancel button ID if cancelled.</returns>
        public static int ShowCommandLinks(
            string title,
            string mainInstruction,
            IEnumerable<CommandLinkOption> options,
            string content = null,
            TaskDialogIcon icon = TaskDialogIcon.Information,
            bool allowCancel = true,
            IntPtr parentHandle = default
        )
        {
            var dialog = new ModernTaskDialog
            {
                Title = title,
                MainInstruction = mainInstruction,
                Content = content,
                MainIcon = icon,
                Flags = TaskDialogFlags.TDF_USE_COMMAND_LINKS |
                        TaskDialogFlags.TDF_ALLOW_DIALOG_CANCELLATION |
                        TaskDialogFlags.TDF_POSITION_RELATIVE_TO_WINDOW |
                        TaskDialogFlags.TDF_SIZE_TO_CONTENT,
                ParentWindowHandle = parentHandle
            };

            foreach (var option in options)
            {
                dialog.CustomButtons.Add(new TaskDialogCustomButton(option.Id, option.GetFullText()));
            }

            if (allowCancel)
            {
                dialog.CommonButtons = TaskDialogCommonButtonFlags.TDCBF_CANCEL_BUTTON;
            }

            return dialog.Show();
        }

        // -------------------------------------------------------------------------
        // Radio Button Selection Dialog
        // -------------------------------------------------------------------------

        /// <summary>
        /// Result from a radio button selection dialog.
        /// </summary>
        public class RadioSelectionResult
        {
            /// <summary>The button ID that was clicked.</summary>
            public int ButtonId { get; set; }

            /// <summary>The ID of the selected radio button.</summary>
            public int SelectedRadioId { get; set; }

            /// <summary>True if OK was clicked, false otherwise.</summary>
            public bool Accepted => ButtonId == ButtonIds.Ok;
        }

        /// <summary>
        /// Shows a dialog with radio button options.
        /// </summary>
        /// <param name="title">The window title.</param>
        /// <param name="mainInstruction">The main instruction (bold header text).</param>
        /// <param name="radioOptions">Dictionary of radio button ID to display text.</param>
        /// <param name="defaultRadioId">The ID of the default selected radio button.</param>
        /// <param name="content">Optional additional content text.</param>
        /// <param name="icon">The icon to display (default: Information).</param>
        /// <param name="parentHandle">Optional parent window handle.</param>
        /// <returns>A result containing the button clicked and selected radio ID.</returns>
        public static RadioSelectionResult ShowRadioSelection(
            string title,
            string mainInstruction,
            IDictionary<int, string> radioOptions,
            int defaultRadioId,
            string content = null,
            TaskDialogIcon icon = TaskDialogIcon.Information,
            IntPtr parentHandle = default
        )
        {
            var dialog = new ModernTaskDialog
            {
                Title = title,
                MainInstruction = mainInstruction,
                Content = content,
                MainIcon = icon,
                CommonButtons = TaskDialogCommonButtonFlags.TDCBF_OK_BUTTON |
                               TaskDialogCommonButtonFlags.TDCBF_CANCEL_BUTTON,
                DefaultRadioButtonId = defaultRadioId,
                Flags = TaskDialogFlags.TDF_ALLOW_DIALOG_CANCELLATION |
                        TaskDialogFlags.TDF_POSITION_RELATIVE_TO_WINDOW |
                        TaskDialogFlags.TDF_SIZE_TO_CONTENT,
                ParentWindowHandle = parentHandle
            };

            foreach (var option in radioOptions)
            {
                dialog.RadioButtons.Add(new TaskDialogCustomButton(option.Key, option.Value));
            }

            int buttonId = dialog.Show();

            return new RadioSelectionResult
            {
                ButtonId = buttonId,
                SelectedRadioId = dialog.SelectedRadioButtonId
            };
        }

        // -------------------------------------------------------------------------
        // Error Dialog with Details (Expander)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Shows an error dialog with expandable details section.
        /// </summary>
        /// <param name="title">The window title.</param>
        /// <param name="mainInstruction">The main instruction (bold header text).</param>
        /// <param name="content">The content text describing the error.</param>
        /// <param name="details">Technical details shown in the expandable section.</param>
        /// <param name="expandedByDefault">Whether the details are expanded by default.</param>
        /// <param name="parentHandle">Optional parent window handle.</param>
        /// <returns>The button ID clicked (always OK = 1).</returns>
        public static int ShowErrorWithDetails(
            string title,
            string mainInstruction,
            string content,
            string details,
            bool expandedByDefault = false,
            IntPtr parentHandle = default
        )
        {
            var flags = TaskDialogFlags.TDF_ALLOW_DIALOG_CANCELLATION |
                        TaskDialogFlags.TDF_POSITION_RELATIVE_TO_WINDOW |
                        TaskDialogFlags.TDF_SIZE_TO_CONTENT;

            if (expandedByDefault)
            {
                flags |= TaskDialogFlags.TDF_EXPANDED_BY_DEFAULT;
            }

            var dialog = new ModernTaskDialog
            {
                Title = title,
                MainInstruction = mainInstruction,
                Content = content,
                MainIcon = TaskDialogIcon.Error,
                ExpandedInformation = details,
                CollapsedControlText = "Show details",
                ExpandedControlText = "Hide details",
                Flags = flags,
                CommonButtons = TaskDialogCommonButtonFlags.TDCBF_OK_BUTTON,
                ParentWindowHandle = parentHandle
            };

            return dialog.Show();
        }

        /// <summary>
        /// Shows an error dialog for an exception with expandable stack trace.
        /// </summary>
        /// <param name="title">The window title.</param>
        /// <param name="exception">The exception to display.</param>
        /// <param name="additionalInfo">Optional additional context about what was happening.</param>
        /// <param name="parentHandle">Optional parent window handle.</param>
        /// <returns>The button ID clicked (always OK = 1).</returns>
        public static int ShowException(string title, Exception exception, string additionalInfo = null, IntPtr parentHandle = default)
        {
            string content = additionalInfo ?? "An unexpected error occurred.";
            string details = $"Exception Type: {exception.GetType().FullName}\n\n" +
                            $"Message: {exception.Message}\n\n" +
                            $"Stack Trace:\n{exception.StackTrace}";

            if (exception.InnerException != null)
            {
                details += $"\n\nInner Exception: {exception.InnerException.GetType().FullName}\n" +
                          $"Inner Message: {exception.InnerException.Message}";
            }

            return ShowErrorWithDetails(title, exception.Message, content, details, expandedByDefault: false, parentHandle);
        }
    }
}