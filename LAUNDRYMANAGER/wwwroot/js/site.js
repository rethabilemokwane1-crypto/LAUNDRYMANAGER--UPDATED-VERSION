// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


/* =========================================
   IWS LAUNDRY | DESIGN ACTIVATION
   ========================================= */

document.addEventListener("DOMContentLoaded", function () {
    const loadingScreen = document.getElementById('loading-screen');

    // Introduce a minimal delay (e.g., 1s) to make sure 
    // the CSS fill animation (which takes 2s) is visible.
    setTimeout(function () {
        // Fade out the screen (Add a class you define in CSS for smooth transition, 
        // but for immediate hide, you can just set display: none).
        if (loadingScreen) {
            loadingScreen.style.transition = 'opacity 0.5s ease';
            loadingScreen.style.opacity = '0';

            // Wait for fade transition, then fully hide
            setTimeout(function () {
                loadingScreen.style.display = 'none';
            }, 500);
        }
    }, 1500); // Wait 1.5s (allows animation to run most of the way before fading)
});

