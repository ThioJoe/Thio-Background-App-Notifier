using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ThioWinUtils;
using static ThioWinUtils.TrayContextMenu;

#nullable enable

namespace ThioWinUtils
{
    /// <summary>
    /// Provides context menu functionality for system tray applications.
    /// Creates and manages popup menus with customizable menu items.
    /// </summary>
    public class TrayContextMenu
    {
        // Primary constructor properties
        private readonly MenuItemSet _menuItemSet = new MenuItemSet();
        private readonly string _checkUpdatesURL = string.Empty;
        private readonly string _aboutMessage = string.Empty;
        private readonly string _helpMessage = string.Empty;
        private readonly Action? _restartProcessAction = null;
        private readonly Action? _exitAction = null;
        private readonly Dictionary<string, Action> _customMenuItems = new();
        private bool _hasCustomItems = false;

        // Win32 System Tray constants related to clicking the tray
        private const uint TPM_RETURNCMD = 0x0100;
        private const uint TPM_RIGHTBUTTON = 0x0002;
        private const uint TPM_LEFTBUTTON = 0x0000;
        private const uint TPM_RIGHTALIGN = 0x0008;
        private const uint TPM_LEFTALIGN = 0x0000;
        private const uint TPM_BOTTOMALIGN = 0x0020;
        // Win32 Menu item constants. See: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-appendmenua
        private const uint MF_SEPARATOR = 0x00000800;
        private const uint MF_DISABLED = 0x00000002;
        private const uint MF_STRING = 0x00000000;

        // Win32 API structures
        /// <summary>
        /// Represents a point in screen coordinates used for menu positioning.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        // Win32 API functions
        [DllImport("user32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Unicode), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern bool InsertMenu(IntPtr hMenu, uint uPosition, uint uFlags, uint uIDNewItem, string lpNewItem);

        [DllImport("user32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern bool DestroyMenu(IntPtr hMenu);

        [DllImport("user32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern uint TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hwnd, IntPtr lprc);

        [DllImport("user32.dll"), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        // ------------------- Constructor -----------------
        /// <summary>
        /// Creates a new context menu for system tray applications.
        /// </summary>
        /// <param name="updateURL">URL for checking updates. If provided, adds 'Check for Updates' menu item.</param>
        /// <param name="appVersion">Application version to display in the menu (as disabled item).</param>
        /// <param name="aboutMessage">If provided, adds "About" menu option which displays the string in a message box.</param>
        /// <param name="helpMessage">If provided, adds "Help" menu option which displays the string in a message box.</param>
        /// <param name="processRestartMenuOption">Whether to include the 'Restart Process' menu option. Not necessary if processRestartAction is supplied, otherwise it will enable default restart action.</param>
        /// <param name="exitAppMenuOption">Whether to include the 'Exit' menu option. Not necessary if exitAction is supplied, otherwise it will enable default exit action.</param>
        /// <param name="processRestartAction">Custom action to execute when restart is selected. If null, default restart behavior is used.</param>
        /// <param name="exitAction">Custom action to execute when exit is selected. If null, default exit behavior is used.</param>
        public TrayContextMenu(
            string? updateURL = null,
            string? appVersion = null,
            string? aboutMessage = null,
            string? helpMessage = null,
            bool processRestartMenuOption = false,
            bool exitAppMenuOption = false,
            Action? processRestartAction = null,
            Action? exitAction = null
            )
        {
            if (updateURL != null && !string.IsNullOrWhiteSpace(updateURL))
            {
                _checkUpdatesURL = updateURL;
            }

            if (processRestartAction != null)
            {
                _restartProcessAction = processRestartAction;
                processRestartMenuOption = true;
            }

            if (exitAction != null)
            {
                _exitAction = exitAction;
                exitAppMenuOption = true;
            }

            if (aboutMessage != null && !string.IsNullOrWhiteSpace(aboutMessage))
            {
                _aboutMessage = aboutMessage;
            }

            if (helpMessage != null && !string.IsNullOrWhiteSpace(helpMessage))
            {
                _helpMessage = helpMessage;
            }

            _menuItemSet = CreateMenu(
                checkUpdatesURL: updateURL,
                appVersion: appVersion,
                aboutMessage: aboutMessage,
                helpMessage: helpMessage,
                restart: processRestartMenuOption,
                exit: exitAppMenuOption
                );
        }

        /// <summary>
        /// Displays the context menu and returns the ID of the selected menu item.
        /// </summary>
        /// <param name="hwnd">Window handle to which the context menu is attached.</param>
        /// <param name="menuItemSet">Set of menu items to display.</param>
        /// <returns>Index of the selected menu item, or 0 if no selection was made.</returns>
        private static uint ShowContextMenuAndGetResponse(IntPtr hwnd, MenuItemSet menuItemSet)
        {
            MenuItem[] menuItems = menuItemSet.GetMenuItems();

            // Create the popup menu
            IntPtr hMenu = CreatePopupMenu();

            try
            {
                // Add menu items
                uint itemId = 1;
                foreach (var item in menuItems)
                {
                    if (item.IsSeparator)
                    {
                        InsertMenu(hMenu, itemId, MF_SEPARATOR, itemId, string.Empty);
                    }
                    else if (item.IsDisabled)
                    {
                        InsertMenu(hMenu, itemId, MF_STRING | MF_DISABLED, itemId, item.Text);
                    }
                    else
                    {
                        InsertMenu(hMenu, itemId, MF_STRING, itemId, item.Text);
                    }
                    itemId++;
                }

                // Get the current cursor position to display the menu at that location, result comes out as pt parameter
                GetCursorPos(out POINT pt);

                // This is necessary to ensure the menu will close when the user clicks elsewhere
                SetForegroundWindow(hwnd);

                // Tells the OS to show the context menu and wait for a selection. But if the user clicks elsewhere, it will return 0.
                uint flags = TPM_RIGHTBUTTON | TPM_LEFTBUTTON | TPM_RETURNCMD | TPM_LEFTALIGN | TPM_BOTTOMALIGN;
                uint clickedItem = TrackPopupMenu(hMenu, flags, pt.X, pt.Y, 0, hwnd, IntPtr.Zero);

                return clickedItem;
            }
            finally
            {
                // Clean up
                DestroyMenu(hMenu);
            }
        }

        /// <summary>
        /// Represents a single menu item in the context menu.
        /// </summary>
        /// <param name="text">Text displayed for the menu item.</param>
        /// <param name="index">Unique index for the menu item.</param>
        /// <param name="isDisabled">Whether the menu item is disabled.</param>
        internal class MenuItem(string text, int index, bool isDisabled)
        {
            /// <summary>
            /// The text displayed for this menu item.
            /// </summary>
            public string Text { get; set; } = text;

            /// <summary>
            /// Whether this menu item is a separator.
            /// </summary>
            public bool IsSeparator { get; set; } = false;

            /// <summary>
            /// Whether this menu item is disabled (grayed out).
            /// </summary>
            public bool IsDisabled { get; set; } = isDisabled;

            /// <summary>
            /// Unique index for this menu item.
            /// </summary>
            public int Index { get; set; } = index;

            /// <summary>
            /// Creates a separator menu item.
            /// </summary>
            /// <param name="index">Index for the separator item.</param>
            /// <returns>A separator menu item.</returns>
            public static MenuItem Separator(int index)
            {
                return new MenuItem(string.Empty, index, false) { IsSeparator = true };
            }
        }

        /// <summary>
        /// Collection of menu items that form a context menu.
        /// </summary>
        internal class MenuItemSet
        {
            internal readonly List<MenuItem> _menuItems = [];

            /// <summary>
            /// Whether the set contains any menu items.
            /// </summary>
            internal bool AnyItems => _menuItems.Count > 0;

            /// <summary>
            /// Adds a new menu item to the set.
            /// </summary>
            /// <param name="text">Text to display for the menu item.</param>
            /// <param name="isDisabled">Whether the menu item should be disabled.</param>
            internal void AddMenuItem(string text, bool isDisabled = false)
            {
                _menuItems.Add(
                    new MenuItem(
                        text,
                        _menuItems.Count + 1,  // 1-based index because 0 is reserved for no selection
                        isDisabled
                    )
                );
            }

            /// <summary>
            /// Inserts a menu item at a specific position.
            /// </summary>
            /// <param name="position">Zero-based position to insert at.</param>
            /// <param name="text">Text to display for the menu item.</param>
            /// <param name="isDisabled">Whether the menu item should be disabled.</param>
            internal void InsertMenuItem(int position, string text, bool isDisabled = false)
            {
                var newItem = new MenuItem(
                    text,
                    0,  // Temporary index, will be recalculated
                    isDisabled
                );
                _menuItems.Insert(position, newItem);

                // Recalculate all indices
                for (int i = 0; i < _menuItems.Count; i++)
                {
                    _menuItems[i].Index = i + 1;
                }
            }

            /// <summary>
            /// Inserts a separator at a specific position.
            /// </summary>
            /// <param name="position">Zero-based position to insert at.</param>
            internal void InsertSeparator(int position)
            {
                var separator = MenuItem.Separator(0);  // Temporary index, will be recalculated
                _menuItems.Insert(position, separator);

                // Recalculate all indices
                for (int i = 0; i < _menuItems.Count; i++)
                {
                    _menuItems[i].Index = i + 1;
                }
            }

            /// <summary>
            /// Adds a separator line to the menu.
            /// </summary>
            internal void AddSeparator()
            {
                _menuItems.Add(MenuItem.Separator(_menuItems.Count + 1)); // 1-based index because 0 is reserved for no selection
            }

            /// <summary>
            /// Gets all menu items as an array.
            /// </summary>
            /// <returns>Array of menu items.</returns>
            internal MenuItem[] GetMenuItems()
            {
                return _menuItems.ToArray();
            }

            /// <summary>
            /// Finds the index of a menu item by its display text.
            /// </summary>
            /// <param name="text">Text to search for.</param>
            /// <returns>Index of the menu item, or -1 if not found.</returns>
            internal int GetMenuItemIndex_ByText(string text)
            {
                return _menuItems.FindIndex(x => x.Text == text);
            }

            /// <summary>
            /// Gets the text of a menu item by its index.
            /// </summary>
            /// <param name="index">Index to look up.</param>
            /// <returns>Text of the menu item, or null if not found.</returns>
            internal string? GetMenuItemText_ByIndex(int index)
            {
                return _menuItems.Find(x => x.Index == index)?.Text;
            }
        }

        /// <summary>
        /// Default text values for standard menu items.
        /// </summary>
        internal static class DefaultMenuItemNames
        {
            public const string CheckUpdates = "Check for Updates";
            public const string Restore = "Restore";
            public const string Restart = "Restart Process";
            public const string Exit = "Exit";
            public const string About = "About";
            public const string Help = "Help";
        }

        /// <summary>
        /// Creates a menu with standard options based on the provided parameters.
        /// </summary>
        /// <param name="checkUpdatesURL">URL for checking updates. If provided, adds 'Check for Updates' menu item.</param>
        /// <param name="appVersion">Application version to display in the menu (as disabled item).</param>
        /// <param name="aboutMessage">If provided, adds "About" menu option which displays the string in a message box.</param>
        /// <param name="helpMessage">If provided, adds "Help" menu option which displays the string in a message box.</param>
        /// <param name="restart">Whether to include the 'Restart Process' menu option.</param>
        /// <param name="exit">Whether to include the 'Exit' menu option.</param>
        /// <returns>A configured MenuItemSet.</returns>
        internal static MenuItemSet CreateMenu(
            string? checkUpdatesURL = null,
            string? appVersion = null,
            string? aboutMessage = null,
            string? helpMessage = null,
            bool restart = false,
            bool exit = false
            )
        {
            MenuItemSet menuItemSet = new MenuItemSet();

            if (checkUpdatesURL != null && !string.IsNullOrWhiteSpace(checkUpdatesURL))
                menuItemSet.AddMenuItem(DefaultMenuItemNames.CheckUpdates);

            if (appVersion != null && !string.IsNullOrWhiteSpace(appVersion))
                menuItemSet.AddMenuItem(appVersion, isDisabled: true);

            if (!string.IsNullOrWhiteSpace(aboutMessage))
                menuItemSet.AddMenuItem(DefaultMenuItemNames.About);

            if (!string.IsNullOrWhiteSpace(helpMessage))
                menuItemSet.AddMenuItem(DefaultMenuItemNames.Help);

            if (!string.IsNullOrWhiteSpace(appVersion) || !string.IsNullOrWhiteSpace(checkUpdatesURL))
                menuItemSet.AddSeparator();

            if (restart)
                menuItemSet.AddMenuItem(DefaultMenuItemNames.Restart);

            if (exit)
                menuItemSet.AddMenuItem(DefaultMenuItemNames.Exit);

            return menuItemSet;
        }

        /// <summary>
        /// Adds a custom menu item with an associated action.
        /// </summary>
        /// <param name="text">The text to display for the menu item. If an empty string, will add a separator.</param>
        /// <param name="action">The action to execute when the menu item is clicked. Can be null ONLY if adding a separator, where <paramref name="text"/> parameter is an empty string.</param>
        /// <exception cref="ArgumentException">Thrown when text is null or whitespace.</exception>
        /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
        public void AddCustomMenuItem(string text, Action? action)
        {
            // Null text parameter is never allowed
            if (text == null)
            {
                throw new ArgumentException("AddCustomMenuItem: Menu item text cannot be null. However an empty string is allowed to add a separator.", nameof(text));
            }
            // If adding a separator by passing an empty string, the action MUST be null
            else if (text == String.Empty && action != null)
            {
                throw new ArgumentException("AddCustomMenuItem: Cannot assign an action to a separator menu item. If 'text' parameter is empty, 'action' must be null.", nameof(action));
            }

            if (action == null && text != String.Empty)
            {
                throw new ArgumentNullException("AddCustomMenuItem: The 'action' parameter cannot be null unless the 'text' parameter is an empty string, in which case a separator will be added.", nameof(action));
            }

            bool isSeparator = (text == String.Empty);

            // Only add to custom menu items dictionary if it's not a separator
            if (!isSeparator)
            {
                _customMenuItems[text] = action!;
            }

            // Find the insertion point (before Restart or Exit if they exist)
            int insertPosition = _menuItemSet._menuItems.Count;

            // Look for Restart or Exit menu items
            int restartIndex = _menuItemSet.GetMenuItemIndex_ByText(DefaultMenuItemNames.Restart);
            int exitIndex = _menuItemSet.GetMenuItemIndex_ByText(DefaultMenuItemNames.Exit);

            // Find the earliest position of Restart or Exit
            if (restartIndex >= 0)
                insertPosition = Math.Min(insertPosition, restartIndex);
            if (exitIndex >= 0)
                insertPosition = Math.Min(insertPosition, exitIndex);

            // If this is the first custom item and we're inserting before Restart/Exit, add the item and then a separator
            if (!_hasCustomItems && insertPosition < _menuItemSet._menuItems.Count)
            {
                if (isSeparator)
                {
                    _menuItemSet.InsertSeparator(insertPosition);
                }
                else
                {
                    _menuItemSet.InsertMenuItem(insertPosition, text);
                    _menuItemSet.InsertSeparator(insertPosition + 1);
                }
                _hasCustomItems = true;
            }
            else if (_hasCustomItems && insertPosition < _menuItemSet._menuItems.Count)
            {
                // If we already have custom items, insert before the separator (which is at insertPosition - 1)
                if (isSeparator)
                    _menuItemSet.InsertSeparator(insertPosition - 1);
                else
                    _menuItemSet.InsertMenuItem(insertPosition - 1, text);
            }
            else
            {
                // No Restart/Exit options, just add to the end
                if (isSeparator)
                    _menuItemSet.AddSeparator();
                else
                    _menuItemSet.AddMenuItem(text);
            }
        }

        /// <summary>
        /// Shows the context menu and handles the selected action.
        /// </summary>
        /// <param name="systemTrayAttachedHwnd">Window handle to which the system tray is attached.</param>
        /// <param name="systemTray">SystemTray instance used for restoration functionality.</param>
        public void Show(IntPtr systemTrayAttachedHwnd, SystemTray systemTray)
        {
            if (!_menuItemSet.AnyItems)
            {
                Trace.WriteLine("Error: No menu items available.");
                return;
            }

            // Show menu and get selection
            uint selected = ShowContextMenuAndGetResponse(systemTrayAttachedHwnd, _menuItemSet);

            // Handle the selected item
            if (selected > 0)
            {
                string? selectedText = _menuItemSet.GetMenuItemText_ByIndex((int)selected);

                //Call the appropriate function based on the selected menu item
                switch (selectedText)
                {
                    case DefaultMenuItemNames.Restore:
                        systemTray.RestoreFromTray();
                        break;
                    case DefaultMenuItemNames.Restart:
                        RestartApplication();
                        break;
                    case DefaultMenuItemNames.CheckUpdates:
                        OpenUpdatesWebsite();
                        break;
                    case DefaultMenuItemNames.Exit:
                        ExitApp();
                        break;
                    case DefaultMenuItemNames.About:
                        NativeMessageBox.ShowInfoMessage(_aboutMessage, "About");
                        break;
                    case DefaultMenuItemNames.Help:
                        NativeMessageBox.ShowInfoMessage(_helpMessage, "Help");
                        break;

                    case null:
                        Trace.WriteLine("Error: Selected item not found.");
                        break;
                    default:
                        if (selectedText != null && _customMenuItems.TryGetValue(selectedText, out var customAction))
                        {
                            customAction.Invoke();
                        }
                        else
                        {
                            Trace.WriteLine("Error: Selected item not handled.");
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Opens the update website in the default browser.
        /// </summary>
        private void OpenUpdatesWebsite()
        {
            if (_checkUpdatesURL == null || string.IsNullOrWhiteSpace(_checkUpdatesURL))
                return;

            string updateURL = _checkUpdatesURL;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = updateURL,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Failed to open the updates website: " + e.Message);
            }
        }

        /// <summary>
        /// Restarts the application using the provided restart action or by default behavior.
        /// </summary>
        private void RestartApplication()
        {
            if (_restartProcessAction is Action restartAction)
            {
                // Call the restart action if provided
                restartAction.Invoke();
                return;
            }
            else
            {
                // Get the path of the current executable
                string? executablePath = Process.GetCurrentProcess().MainModule?.FileName;

                if (executablePath == null)
                {
                    Trace.WriteLine("Error: Executable path not found.");
                    return;
                }
                Process.Start(executablePath);
                Environment.Exit(0);
            }
        }

        /// <summary>
        /// Exits the application using the provided exit action or by default behavior.
        /// </summary>
        private void ExitApp()
        {
            if (_exitAction is Action exitAction)
            {
                // Call the exit action if provided
                exitAction.Invoke();
            }
            else
            {
                // Logic to exit the application
                Trace.WriteLine("Exiting application...");
                Environment.Exit(0);
            }
        }

    } // End of ContextMenu class


    /// <summary>
    /// Provides native Windows message box functionality.
    /// </summary>
    internal class NativeMessageBox
    {
        // Import the MessageBox function from user32.dll
        [DllImport("user32.dll", CharSet = CharSet.Unicode), DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

        // MB_OK constant from WinUser.h
        private const uint MB_OK = 0x00000000;

        /// <summary>
        /// Shows an information message box with an OK button.
        /// </summary>
        /// <param name="message">The message to display.</param>
        /// <param name="title">The title of the message box.</param>
        public static void ShowInfoMessage(string message, string title)
        {
            // Show message box with MB_OK style (just OK button)
            // First parameter is IntPtr.Zero for no parent window
            _ = MessageBox(IntPtr.Zero, message, title, MB_OK);
        }

        /// <summary>
        /// Shows an error message box with an OK button and error icon.
        /// </summary>
        /// <param name="message">The error message to display.</param>
        /// <param name="title">The title of the message box.</param>
        public static void ShowErrorMessage(string message, string title)
        {
            // Show message box with MB_ICONERROR style (error icon)
            // First parameter is IntPtr.Zero for no parent window
            const uint MB_ICONERROR = 0x00000010;
            _ = MessageBox(IntPtr.Zero, message, title, MB_OK | MB_ICONERROR);
        }
    } // --------------- End of NativeMessageBox class ---------------


} // --------------- End of Namespace ---------------
