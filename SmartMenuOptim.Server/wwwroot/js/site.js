// site.js - Custom JS interop for Blazor Server sidebar functionality
// This file provides JS functions for sidebar open/close logic and window width detection.
// Ensure this file is referenced in your main layout (e.g., <script src="js/site.js"></script> in _Host.cshtml or index.html).
// Functions here are called from C# via IJSRuntime and must match the names used in Blazor components.

// Sidebar interop object for handling outside click events
window.sidebarInterop = {
    // Adds a mouse down event listener to detect clicks outside the sidebar.
    // dotNetObjRef: Blazor .NET object reference for callback
    // sidebarSelector: CSS selector for the sidebar element
    addOutsideClickListener: function (dotNetObjRef, sidebarSelector) {
        function handler(event) {
            const sidebar = document.querySelector(sidebarSelector);
            if (sidebar && !sidebar.contains(event.target)) {
                // Calls the Blazor method 'OnOutsideSidebarClick' when clicking outside
                dotNetObjRef.invokeMethodAsync('OnOutsideSidebarClick');
            }
        }
        document.addEventListener('mousedown', handler);
        // Store handler for later removal
        window._sidebarOutsideClickHandler = handler;
    },
    // Removes the previously added outside click event listener
    removeOutsideClickListener: function () {
        if (window._sidebarOutsideClickHandler) {
            document.removeEventListener('mousedown', window._sidebarOutsideClickHandler);
            window._sidebarOutsideClickHandler = null;
        }
    }
};

// Returns the current window width (used for responsive sidebar logic)
window.getWindowWidth = function () {
    return window.innerWidth;
};