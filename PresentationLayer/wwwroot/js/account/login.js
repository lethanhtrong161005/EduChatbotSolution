/**
 * EduChatAI - Login Page Interactions
 * Handles form submission, loading state, input focus effects,
 * background bubble animation, and double-submit prevention.
 */

document.addEventListener('DOMContentLoaded', function () {
    const loginForm = document.getElementById('loginForm');
    const loginBtn = document.getElementById('loginBtn');
    const emailInput = document.getElementById('email');
    const passwordInput = document.getElementById('password');

    // ── Loading animation on form submit ──────────────────────
    loginForm.addEventListener('submit', function (e) {
        if (loginForm.checkValidity() === false) {
            e.preventDefault();
            e.stopPropagation();
        } else {
            loginBtn.classList.add('loading');
            loginBtn.disabled = true;
        }
    });

    // ── Input focus effects ──────────────────────────────────
    [emailInput, passwordInput].forEach(function (input) {
        input.addEventListener('focus', function () {
            this.parentElement.style.borderColor = 'var(--color-primary)';
        });
        input.addEventListener('blur', function () {
            this.parentElement.style.borderColor = '';
        });
    });

    // ── Background bubbles ───────────────────────────────────
    createBubbles();

    // ── Prevent double form submission ───────────────────────
    var isSubmitting = false;
    loginForm.addEventListener('submit', function (e) {
        if (isSubmitting) {
            e.preventDefault();
            return;
        }
        isSubmitting = true;
    });
});

/**
 * Creates animated bubble elements inside the background container.
 * Generates fewer bubbles on mobile for performance.
 */
function createBubbles() {
    var bgAnimation = document.querySelector('.bg-animation');
    if (!bgAnimation) return;

    var bubbleCount = window.innerWidth > 768 ? 5 : 2;

    for (var i = 0; i < bubbleCount; i++) {
        var bubble = document.createElement('div');
        bubble.className = 'bubble';

        var size = Math.random() * 100 + 50;
        bubble.style.width = size + 'px';
        bubble.style.height = size + 'px';

        bubble.style.left = Math.random() * 100 + '%';
        bubble.style.top = Math.random() * 100 + '%';

        bubble.style.setProperty('--float-duration', Math.random() * 10 + 8 + 's');

        bgAnimation.appendChild(bubble);
    }
}
